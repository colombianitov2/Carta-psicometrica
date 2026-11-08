using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.12.1 - Capa gráfica de puntos ------------------------------

public static class PointLayer
{

    // ------------------------------ Fin de sección 2.12.1 ------------------------------


    // ------------------------------ Sección 2.12.2 - Dibujado de puntos ------------------------------

    public static void Draw(
        Canvas canvas,
        ChartGeometry geometry,
        IReadOnlyList<ChartPoint> points)
    {
        if (canvas.ActualWidth <= 0.0
            || canvas.ActualHeight <= 0.0
            || points.Count == 0)
        {
            return;
        }

        Rect plotRect =
            geometry.GetPlotRect(
                canvas.ActualWidth,
                canvas.ActualHeight);

        if (plotRect.IsEmpty)
        {
            return;
        }

        var visible=new List<ChartPoint>();
        foreach (ChartPoint point
                 in points)
        {
            if (point.DryBulbC
                    < geometry.MinDryBulbC
                || point.DryBulbC
                    > geometry.MaxDryBulbC
                || point.HumidityRatioKgPerKg
                    < geometry.MinHumidityRatioKgPerKg
                || point.HumidityRatioKgPerKg
                    > geometry.MaxHumidityRatioKgPerKg)
            {
                continue;
            }

            visible.Add(point);
            DrawPoint(canvas,geometry,plotRect,point);
        }
        DrawLabels(canvas,geometry,plotRect,visible);
    }

    // ------------------------------ Fin de sección 2.12.2 ------------------------------


    // ------------------------------ Sección 2.12.3 - Marcador y nombre ------------------------------

    private static void DrawPoint(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect,
        ChartPoint point)
    {
        double x =
            geometry.XFromDryBulbC(
                point.DryBulbC,
                plotRect);

        double y =
            geometry.YFromHumidityRatioKgPerKg(
                point.HumidityRatioKgPerKg,
                plotRect);

        Brush pointBrush =
            CreateBrush(
                point.ColorHex);

        const double diameter =
            12.0;

        Ellipse marker =
            new()
            {
                Width = diameter,
                Height = diameter,
                Fill = pointBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1.8,
                IsHitTestVisible = false
            };

        Canvas.SetLeft(
            marker,
            x - diameter / 2.0);

        Canvas.SetTop(
            marker,
            y - diameter / 2.0);

        Panel.SetZIndex(
            marker,
            1000);

        canvas.Children.Add(
            marker);


    }

    // ------------------------------ Fin de sección 2.12.3 ------------------------------


    // [2.12.3.1] Distribución de etiquetas sin superponer nombres ni
    // ocultar marcadores adyacentes. También opera con zoom y presión distintos.
    private static void DrawLabels(Canvas canvas,ChartGeometry geometry,
        Rect plotRect,IReadOnlyList<ChartPoint> visible)
    {
        var occupied=new List<Rect>();
        foreach(var point in visible)
        {
            double px=geometry.XFromDryBulbC(point.DryBulbC,plotRect);
            double py=geometry.YFromHumidityRatioKgPerKg(point.HumidityRatioKgPerKg,plotRect);
            occupied.Add(new Rect(px-14,py-14,28,28));
        }

        foreach(var point in visible)
        {
            double x=geometry.XFromDryBulbC(point.DryBulbC,plotRect);
            double y=geometry.YFromHumidityRatioKgPerKg(point.HumidityRatioKgPerKg,plotRect);
            var label=new TextBlock
            {
                Text=point.Name,
                FontSize=10.5,
                FontWeight=FontWeights.SemiBold,
                Foreground=CreateBrush(point.ColorHex),
                Background=Brushes.White,
                Padding=new Thickness(2,0,2,0),
                MaxWidth=Math.Max(40,plotRect.Width*.35),
                TextTrimming=TextTrimming.CharacterEllipsis,
                ToolTip=point.Name,
                IsHitTestVisible=false
            };
            label.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            double w=label.DesiredSize.Width,h=label.DesiredSize.Height;
            var positions=new[]
            {
                new Point(x+15,y-h-9), new Point(x+15,y+9),
                new Point(x-w-15,y-h-9), new Point(x-w-15,y+9),
                new Point(x-w/2,y-h-22), new Point(x-w/2,y+22),
                new Point(x+25,y-h/2), new Point(x-w-25,y-h/2)
            };
            double bestScore=double.PositiveInfinity;
            Rect best=Rect.Empty;
            for(int index=0;index<positions.Length;index++)
            {
                double left=Math.Clamp(positions[index].X,plotRect.Left+2,
                    Math.Max(plotRect.Left+2,plotRect.Right-w-2));
                double top=Math.Clamp(positions[index].Y,plotRect.Top+2,
                    Math.Max(plotRect.Top+2,plotRect.Bottom-h-2));
                var proposed=new Rect(left,top,w,h);
                // Prioridad: sin colisiones. Penalizar mínimamente desplazar
                // el nombre lejos de su posición habitual.
                double score=index*.01;
                foreach(var prior in occupied)
                {
                    double overlapX=Math.Max(0,Math.Min(proposed.Right,prior.Right)
                        -Math.Max(proposed.Left,prior.Left));
                    double overlapY=Math.Max(0,Math.Min(proposed.Bottom,prior.Bottom)
                        -Math.Max(proposed.Top,prior.Top));
                    score+=overlapX*overlapY;
                }
                if(score<bestScore)
                {
                    bestScore=score;
                    best=proposed;
                    if(score<.05)break;
                }
            }
            if(best.IsEmpty)continue;
            Canvas.SetLeft(label,best.Left);
            Canvas.SetTop(label,best.Top);
            Panel.SetZIndex(label,3000);
            canvas.Children.Add(label);
            occupied.Add(new Rect(best.Left-2,best.Top-2,best.Width+4,best.Height+4));
        }
    }

    // ------------------------------ Sección 2.12.4 - Conversión de color ------------------------------

    private static Brush CreateBrush(
        string colorHex)
    {
        try
        {
            return
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        colorHex));
        }
        catch
        {
            return Brushes.Red;
        }
    }

    // ------------------------------ Fin de sección 2.12.4 ------------------------------


    // ------------------------------ Sección 2.12.5 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.12.5 ------------------------------
