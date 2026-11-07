using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics.Chart;

// [2.13] Representación de procesos ya definidos por estados físicos.
public static class ProcessLayer
{
    public static void Draw(Canvas canvas, ChartGeometry geometry,
        IReadOnlyList<ChartPoint> points, IReadOnlyList<PsychrometricProcess> processes)
    {
        Rect plot = geometry.GetPlotRect(canvas.ActualWidth, canvas.ActualHeight);
        if (plot.IsEmpty) return;
        foreach (var process in processes)
        {
            if (!process.Visible) continue;
            var color = (Color)ColorConverter.ConvertFromString(process.ColorHex);
            Brush brush = new SolidColorBrush(color);
            var states = process.PointIds.Select(id => points.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null).ToList();
            if (states.Count < 2) continue;
            var edges = new List<(ChartPoint First, ChartPoint Last)>();
            if (process.Type == ProcessType.Branching && states.Count == 3)
            {
                edges.Add((states[0]!,states[1]!));
                edges.Add((states[0]!,states[2]!));
            }
            else if (process.Type == ProcessType.AdiabaticMixing && states.Count >= 3)
            {
                for (int i = 0; i < states.Count - 1; i++)
                    edges.Add((states[i]!, states[^1]!)); // dos entradas convergen al nodo mezcla
            }
            else
            {
                for (int i = 0; i < states.Count - 1; i++)
                    edges.Add((states[i]!, states[i + 1]!));
            }
            foreach (var (first,last) in edges)
            {
                double x1 = geometry.XFromDryBulbC(first.DryBulbC, plot),
                    y1 = geometry.YFromHumidityRatioKgPerKg(first.HumidityRatioKgPerKg, plot),
                    x2 = geometry.XFromDryBulbC(last.DryBulbC, plot),
                    y2 = geometry.YFromHumidityRatioKgPerKg(last.HumidityRatioKgPerKg, plot);
                if (!double.IsFinite(x1 + x2 + y1 + y2)) continue;
                var line = new Line {X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=brush,StrokeThickness=3,
                    StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,
                    ToolTip=$"{process.Name}: {first.Name} → {last.Name}"};
                canvas.Children.Add(line);
                double dx=x2-x1,dy=y2-y1,mag=Math.Sqrt(dx*dx+dy*dy);
                if (mag > 18)
                {
                    double ux=dx/mag,uy=dy/mag;
                    var arrow=new Polygon {Fill=brush,Points=new PointCollection {
                        new Point(x2-12*ux,y2-12*uy),
                        new Point(x2-23*ux-6*uy,y2-23*uy+6*ux),
                        new Point(x2-23*ux+6*uy,y2-23*uy-6*ux)
                    }};
                    canvas.Children.Add(arrow);
                }
            }
        }
    }
    // [2.13.2] Los procesos determinan el color de sus miembros.
    // Para estados compartidos, aros concéntricos permiten distinguir procesos
    // sin sobrescribir el color propio del estado.
    public static void DrawMemberColors(Canvas canvas, ChartGeometry geometry,
        IReadOnlyList<ChartPoint> points, IReadOnlyList<PsychrometricProcess> processes)
    {
        Rect plot = geometry.GetPlotRect(canvas.ActualWidth,canvas.ActualHeight);
        if (plot.IsEmpty) return;
        var members = processes.Where(p => p.Visible)
            .SelectMany(p => p.PointIds.Distinct().Select(id => (Id:id, Process:p)))
            .GroupBy(x => x.Id);
        foreach (var group in members)
        {
            ChartPoint? state=points.FirstOrDefault(p=>p.Id==group.Key);
            if(state is null||state.DryBulbC<geometry.MinDryBulbC||
               state.DryBulbC>geometry.MaxDryBulbC||
               state.HumidityRatioKgPerKg<geometry.MinHumidityRatioKgPerKg||
               state.HumidityRatioKgPerKg>geometry.MaxHumidityRatioKgPerKg) continue;
            double x=geometry.XFromDryBulbC(state.DryBulbC,plot);
            double y=geometry.YFromHumidityRatioKgPerKg(state.HumidityRatioKgPerKg,plot);
            var processList=group.Select(g=>g.Process).ToList();
            for(int i=0;i<processList.Count;i++)
            {
                var p=processList[i];
                var color=(Color)ColorConverter.ConvertFromString(p.ColorHex);
                double diameter=17+6*i;
                var circle=new Ellipse
                {
                    Width=diameter,Height=diameter,
                    Fill=processList.Count==1 ? new SolidColorBrush(color) : Brushes.Transparent,
                    Stroke=processList.Count==1 ? Brushes.White : new SolidColorBrush(color),
                    StrokeThickness=processList.Count==1 ? 2 : 3,
                    ToolTip=$"{p.Name} · {state.Name}",
                    IsHitTestVisible=false
                };
                Canvas.SetLeft(circle,x-diameter/2);
                Canvas.SetTop(circle,y-diameter/2);
                Panel.SetZIndex(circle,2000+i);
                canvas.Children.Add(circle);
            }
        }
    }

}
