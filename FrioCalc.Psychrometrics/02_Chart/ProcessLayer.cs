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
            var color = (Color)ColorConverter.ConvertFromString(process.ColorHex);
            Brush brush = new SolidColorBrush(color);
            var states = process.PointIds.Select(id => points.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null).ToList();
            if (states.Count < 2) continue;
            var edges = new List<(ChartPoint First, ChartPoint Last)>();
            if (process.Type == ProcessType.AdiabaticMixing && states.Count >= 3)
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
    // [2.13.2] En la segunda carta, los miembros adoptan el color del proceso.
    // Un punto compartido por varios procesos se conserva como un estado único:
    // cada proceso sigue representado por su línea y leyenda propia.
    public static void DrawMemberColors(Canvas canvas, ChartGeometry geometry,
        IReadOnlyList<ChartPoint> points, IReadOnlyList<PsychrometricProcess> processes)
    {
        Rect rect = geometry.GetPlotRect(canvas.ActualWidth,canvas.ActualHeight);
        if (rect.IsEmpty) return;
        foreach (var process in processes)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(process.ColorHex));
            foreach (int id in process.PointIds)
            {
                ChartPoint? point = points.FirstOrDefault(p => p.Id == id);
                if (point == null || point.DryBulbC < geometry.MinDryBulbC ||
                    point.DryBulbC > geometry.MaxDryBulbC ||
                    point.HumidityRatioKgPerKg < geometry.MinHumidityRatioKgPerKg ||
                    point.HumidityRatioKgPerKg > geometry.MaxHumidityRatioKgPerKg) continue;
                double x=geometry.XFromDryBulbC(point.DryBulbC,rect);
                double y=geometry.YFromHumidityRatioKgPerKg(point.HumidityRatioKgPerKg,rect);
                var marker=new Ellipse {Width=15,Height=15,Fill=brush,Stroke=Brushes.White,
                    StrokeThickness=2,IsHitTestVisible=false,
                    ToolTip=$"{process.Name} · {point.Name}"};
                Canvas.SetLeft(marker,x-7.5); Canvas.SetTop(marker,y-7.5);
                Panel.SetZIndex(marker,2000); canvas.Children.Add(marker);
            }
        }
    }

}
