using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// [2.10.18] Contornos Δh por marching squares en (Tbs,W/Wlim(Tbs)).
// La función NO es globalmente monótona respecto de W; buscar una única
// raíz por Tbs suprimía ramas físicas y generaba fragmentos artificiales.
// Contornos basados en el modelo de EnthalpyDeviation, no entropía.
public static class EnthalpyDeviationLayer
{
    private static readonly double[] Levels={-.05,-.10,-.20,-.40,-.60,-.80,-1.00,-1.20};
    private static readonly Brush CurveBrush=new SolidColorBrush(Color.FromRgb(0,112,116));
    private const int WIntervals=44;
    private const double MaxJumpPerSample=.30; // Evitar interpolar discontinuidad hielo/agua.
    private readonly record struct Node(double Value,Point Position);
    private readonly record struct Crossing(string Key,Point Position);
    private readonly record struct Segment(Crossing A,Crossing B);

    public static void Draw(Canvas canvas,ChartGeometry geometry,double pressureKPa)
    {
        if(canvas.ActualWidth<=0||canvas.ActualHeight<=0||
           !double.IsFinite(pressureKPa)||pressureKPa<=0)return;
        var plot=geometry.GetPlotRect(canvas.ActualWidth,canvas.ActualHeight);
        if(plot.IsEmpty)return;

        double step=.6;
        int tx=Math.Clamp((int)Math.Ceiling(
            (geometry.MaxDryBulbC-geometry.MinDryBulbC)/step),10,400);
        var nodes=new Node[tx+1,WIntervals+1];
        for(int i=0;i<=tx;i++)
        {
            double t=geometry.MinDryBulbC+
                (geometry.MaxDryBulbC-geometry.MinDryBulbC)*i/tx;
            double pws=SaturationVaporPressure.GetPressureKPa(t);
            double ws=pws<pressureKPa?
                SaturationHumidityRatio.FromPressuresKPa(pressureKPa,pws):double.NaN;
            double topW=Math.Min(ws,geometry.MaxHumidityRatioKgPerKg);
            bool valid=double.IsFinite(topW)&&topW>=geometry.MinHumidityRatioKgPerKg;
            for(int j=0;j<=WIntervals;j++)
            {
                if(!valid)
                {
                    nodes[i,j]=new Node(double.NaN,new Point(double.NaN,double.NaN));
                    continue;
                }
                double w=geometry.MinHumidityRatioKgPerKg+
                    (topW-geometry.MinHumidityRatioKgPerKg)*j/WIntervals;
                double deviation=double.NaN;
                try
                {
                    deviation=EnthalpyDeviation.FromDryBulbAndHumidityRatio(t,w,pressureKPa);
                }
                catch(ArgumentException) { }
                nodes[i,j]=new Node(deviation,new Point(
                    geometry.XFromDryBulbC(t,plot),
                    geometry.YFromHumidityRatioKgPerKg(w,plot)));
            }
        }

        foreach(double level in Levels)
            DrawLevel(canvas,plot,nodes,tx,level,geometry.DisplayUnits);

        var title=new TextBlock
        {
            Text="Δh = h − hsat(Tbh) ["+(geometry.DisplayUnits==UnitSystem.SI?
                "kJ/kgda":"Btu/lbda")+"]",
            FontSize=10,FontWeight=FontWeights.SemiBold,Foreground=CurveBrush,
            Background=Brushes.White,
            ToolTip="Desviación de entalpía aproximada, distinta de entropía relativa s*."
        };
        canvas.Children.Add(title);
        Canvas.SetLeft(title,plot.Left+7);
        Canvas.SetTop(title,Math.Max(0,plot.Top-15));
    }

    private static void DrawLevel(Canvas canvas,Rect plot,Node[,] samples,
        int countT,double target,UnitSystem units)
    {
        var segments=new List<Segment>();
        for(int i=0;i<countT;i++)
        for(int j=0;j<WIntervals;j++)
        {
            var a=samples[i,j];
            var b=samples[i+1,j];
            var c=samples[i+1,j+1];
            var d=samples[i,j+1];
            var crossings=new List<Crossing>(4);
            TryCross(a,b,target,$"H:{i}:{j}",crossings);
            TryCross(b,c,target,$"V:{i+1}:{j}",crossings);
            TryCross(d,c,target,$"H:{i}:{j+1}",crossings);
            TryCross(a,d,target,$"V:{i}:{j}",crossings);
            if(crossings.Count==2)
                segments.Add(new Segment(crossings[0],crossings[1]));
            else if(crossings.Count==4)
            {
                // Saddle cell: choose shortest noncrossing pairing.
                double p01=Distance(crossings[0].Position,crossings[1].Position)
                           +Distance(crossings[2].Position,crossings[3].Position);
                double p03=Distance(crossings[0].Position,crossings[3].Position)
                           +Distance(crossings[1].Position,crossings[2].Position);
                if(p01<=p03)
                {
                    segments.Add(new Segment(crossings[0],crossings[1]));
                    segments.Add(new Segment(crossings[2],crossings[3]));
                }
                else
                {
                    segments.Add(new Segment(crossings[0],crossings[3]));
                    segments.Add(new Segment(crossings[1],crossings[2]));
                }
            }
        }
        if(segments.Count==0)return;

        var adjacency=new Dictionary<string,List<(int Index,string Next)>>();
        var positions=new Dictionary<string,Point>();
        for(int i=0;i<segments.Count;i++)
        {
            var seg=segments[i];
            positions[seg.A.Key]=seg.A.Position;
            positions[seg.B.Key]=seg.B.Position;
            AddNeighbor(seg.A.Key,(i,seg.B.Key));
            AddNeighbor(seg.B.Key,(i,seg.A.Key));
        }
        void AddNeighbor(string key,(int Index,string Next) edge)
        {
            if(!adjacency.TryGetValue(key,out var list))
                adjacency[key]=list=new List<(int,string)>();
            list.Add(edge);
        }
        var used=new bool[segments.Count];
        var routes=new List<List<Point>>();
        foreach(string start in adjacency.Keys.OrderBy(k=>
            adjacency[k].Count==1?0:1))
        {
            while(adjacency[start].Any(e=>!used[e.Index]))
            {
                var route=new List<Point>{positions[start]};
                string current=start;
                for(int steps=0;steps<=segments.Count;steps++)
                {
                    var next=adjacency[current].FirstOrDefault(e=>!used[e.Index]);
                    if(next.Next is null)break;
                    used[next.Index]=true;
                    current=next.Next;
                    route.Add(positions[current]);
                }
                if(route.Count>=3)
                    routes.Add(route);
            }
        }
        double bestLength=0;
        List<Point>? bestRoute=null;
        foreach(var route in routes)
        {
            double length=0;
            for(int i=1;i<route.Count;i++)
                length+=Distance(route[i-1],route[i]);
            // Two adjacent grid lines can form valid small branches. We draw
            // contours >= 7px; smaller fragments are numerical noise.
            if(length<7)continue;
            var polyline=new Polyline
            {
                Stroke=CurveBrush,StrokeThickness=1.5,StrokeLineJoin=PenLineJoin.Round,
                StrokeDashArray=new DoubleCollection{4,2.3},
                Clip=new RectangleGeometry(plot),
                IsHitTestVisible=false
            };
            foreach(var point in route)polyline.Points.Add(point);
            canvas.Children.Add(polyline);
            if(length>bestLength){bestLength=length;bestRoute=route;}
        }
        if(bestRoute is null||bestLength<30)return;
        int labelIndex=Math.Clamp(bestRoute.Count/2,0,bestRoute.Count-1);
        var position=bestRoute[labelIndex];
        var label=new TextBlock
        {
            Text="Δh "+PsychrometricUnits.EnthalpyFromSI(target,units).ToString(
                units==UnitSystem.SI?"0.00":"0.000",CultureInfo.CurrentCulture),
            FontSize=9.5,Foreground=CurveBrush,Background=Brushes.White,
            IsHitTestVisible=false
        };
        label.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
        canvas.Children.Add(label);
        Canvas.SetLeft(label,Math.Clamp(position.X+4,plot.Left+3,
            Math.Max(plot.Left+3,plot.Right-label.DesiredSize.Width-3)));
        Canvas.SetTop(label,Math.Clamp(position.Y-label.DesiredSize.Height-7,
            plot.Top+2,Math.Max(plot.Top+2,plot.Bottom-label.DesiredSize.Height-2)));
    }

    private static double Distance(Point a,Point b)
    {
        double dx=a.X-b.X,dy=a.Y-b.Y;
        return Math.Sqrt(dx*dx+dy*dy);
    }

    private static void TryCross(Node a,Node b,double target,string id,
        List<Crossing> crossings)
    {
        if(!double.IsFinite(a.Value)||!double.IsFinite(b.Value)||
           !double.IsFinite(a.Position.X)||!double.IsFinite(b.Position.X)||
           Math.Abs(a.Value-b.Value)>MaxJumpPerSample)return;
        double da=a.Value-target,db=b.Value-target;
        if(!(da<=0&&db>0||db<=0&&da>0))return;
        double denominator=b.Value-a.Value;
        if(Math.Abs(denominator)<1e-14)return;
        double f=Math.Clamp((target-a.Value)/denominator,0,1);
        var pt=new Point(a.Position.X+(b.Position.X-a.Position.X)*f,
            a.Position.Y+(b.Position.Y-a.Position.Y)*f);
        crossings.Add(new Crossing(id,pt));
    }
}
