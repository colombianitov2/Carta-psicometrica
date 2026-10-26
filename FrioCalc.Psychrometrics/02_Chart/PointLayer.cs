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

            DrawPoint(
                canvas,
                geometry,
                plotRect,
                point);
        }
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

        TextBlock label =
            new()
            {
                Text = point.Name,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = pointBrush,
                Background = Brushes.White,
                Padding = new Thickness(2, 0, 2, 0),
                IsHitTestVisible = false
            };

        canvas.Children.Add(
            label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Canvas.SetLeft(
            label,
            x + 8.0);

        Canvas.SetTop(
            label,
            y
            - label.DesiredSize.Height
            - 5.0);

        Panel.SetZIndex(
            label,
            1001);
    }

    // ------------------------------ Fin de sección 2.12.3 ------------------------------


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
