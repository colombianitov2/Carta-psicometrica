using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// [2.15.1] Curvas isoentrópicas del modelo de gas ideal, referencia explícita.
// Marching-squares: cada celda se interpola únicamente con 4 estados válidos.
// Evita prolongar curvas dentro del dominio sobresaturado o fuera del viewport.
public static class SpecificEntropyLayer
{
    public static void Draw(Canvas canvas,ChartGeometry geometry,double pressureKPa)
    {
        if(canvas.ActualWidth<220 || canvas.ActualHeight<170 || pressureKPa<=0) return;
        Rect rect=geometry.GetPlotRect(canvas.ActualWidth,canvas.ActualHeight);
        if(rect.IsEmpty) return;
        int nx=Math.Clamp((int)Math.Ceiling((geometry.MaxDryBulbC-geometry.MinDryBulbC)/0.6),45,240);
        int ny=Math.Clamp((int)Math.Ceiling((geometry.MaxHumidityRatioKgPerKg-
                 geometry.MinHumidityRatioKgPerKg)/0.00050),35,150);
        double[,] samples=new double[nx+1,ny+1];
        double min=double.PositiveInfinity,max=double.NegativeInfinity;
        for(int x=0;x<=nx;x++)
        {
            double t=geometry.MinDryBulbC+
                (geometry.MaxDryBulbC-geometry.MinDryBulbC)*x/nx;
            for(int y=0;y<=ny;y++)
            {
                double w=geometry.MinHumidityRatioKgPerKg+
                    (geometry.MaxHumidityRatioKgPerKg-geometry.MinHumidityRatioKgPerKg)*y/ny;
                double s=MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(t,w,pressureKPa);
                samples[x,y]=s;
                if(double.IsFinite(s)){min=Math.Min(min,s);max=Math.Max(max,s);}
            }
        }
        if(!double.IsFinite(min)||!double.IsFinite(max))return;
        double step=geometry.IsStandardChartMode?0.04:0.08;
        var labels=new List<double>();
        var color=new SolidColorBrush(Color.FromRgb(101,67,163));
        color.Freeze();
        for(double level=Math.Ceiling(min/step)*step;level<=max+1e-10;level+=step)
        {
            var pathData=new StreamGeometry();
            Point? caption=null;
            double distance=double.PositiveInfinity;
            int lines=0;
            using(var g=pathData.Open())
            {
                for(int i=0;i<nx;i++)
                for(int j=0;j<ny;j++)
                {
                    double a=samples[i,j],b=samples[i+1,j],cc=samples[i+1,j+1],d=samples[i,j+1];
                    if(!double.IsFinite(a)||!double.IsFinite(b)||!double.IsFinite(cc)||!double.IsFinite(d))
                        continue;
                    double x0=rect.Left+rect.Width*i/nx, x1=rect.Left+rect.Width*(i+1)/nx;
                    double y0=rect.Bottom-rect.Height*j/ny, y1=rect.Bottom-rect.Height*(j+1)/ny;
                    var p=new List<Point>(4);
                    AddCrossing(p,a,b,level,x0,y0,x1,y0);
                    AddCrossing(p,b,cc,level,x1,y0,x1,y1);
                    AddCrossing(p,cc,d,level,x1,y1,x0,y1);
                    AddCrossing(p,d,a,level,x0,y1,x0,y0);
                    for(int k=0;k+1<p.Count;k+=2)
                    {
                        g.BeginFigure(p[k],false,false);
                        g.LineTo(p[k+1],true,false);
                        lines++;
                        Point middle=new Point((p[k].X+p[k+1].X)/2,
                            (p[k].Y+p[k+1].Y)/2);
                        double dist=Math.Abs(middle.X-(rect.Left+rect.Width*.66))
                            + Math.Abs(middle.Y-(rect.Top+rect.Height*.40))*.05;
                        if(dist<distance && middle.X>rect.Left+35 && middle.X<rect.Right-50
                            && middle.Y>rect.Top+12 && middle.Y<rect.Bottom-12)
                        {distance=dist;caption=middle;}
                    }
                }
            }
            if(lines<3)continue;
            pathData.Freeze();
            var path=new Path {Data=pathData,Stroke=color,StrokeThickness=1.0,
                StrokeDashArray=new DoubleCollection {4,3},Opacity=.85,
                Clip=new RectangleGeometry(rect),IsHitTestVisible=false};
            canvas.Children.Add(path);
            if(caption.HasValue && labels.TrueForAll(y=>Math.Abs(y-caption.Value.Y)>18))
            {
                labels.Add(caption.Value.Y);
                string units=geometry.DisplayUnits==UnitSystem.SI?"kJ/(kgda·K)":"Btu/(lbda·°F)";
                double value=PsychrometricUnits.EntropyFromSI(level,geometry.DisplayUnits);
                string shown=(Math.Abs(value)<0.00001?0.0:value).ToString(
                    geometry.DisplayUnits==UnitSystem.SI?"0.00":"0.000",CultureInfo.CurrentCulture);
                var label=new TextBlock{Text="s*="+shown,
                    ToolTip=$"Entropía relativa del modelo ideal, {units}. No equivale a Δh.",
                    FontSize=10,Foreground=color,Background=Brushes.White,IsHitTestVisible=false};
                Canvas.SetLeft(label,caption.Value.X+5);
                Canvas.SetTop(label,caption.Value.Y-13);
                canvas.Children.Add(label);
            }
        }
        var title=new TextBlock
        {
            Text=geometry.DisplayUnits==UnitSystem.SI ? "s* (modelo ideal, kJ/(kgda·K))" :
                "s* (modelo ideal, Btu/(lbda·°F))",
            FontSize=9.5,FontWeight=FontWeights.SemiBold,
            Foreground=color,Background=Brushes.White,
            ToolTip="Entropía relativa al conjunto de referencias especificado; no es entropía absoluta de ASHRAE."
        };
        Canvas.SetLeft(title,rect.Left+8);
        Canvas.SetTop(title,rect.Top+20);
        canvas.Children.Add(title);
    }

    private static void AddCrossing(List<Point> points,double first,double second,
        double iso,double x0,double y0,double x1,double y1)
    {
        if((first<iso && second>=iso)||(second<iso && first>=iso))
        {
            double t=(iso-first)/(second-first);
            if(double.IsFinite(t))points.Add(new Point(x0+(x1-x0)*t,y0+(y1-y0)*t));
        }
    }
}
