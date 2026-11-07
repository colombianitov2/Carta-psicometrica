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
            for (int i = 0; i < states.Count - 1; i++)
            {
                ChartPoint first = states[i]!, last = states[i + 1]!;
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
}
