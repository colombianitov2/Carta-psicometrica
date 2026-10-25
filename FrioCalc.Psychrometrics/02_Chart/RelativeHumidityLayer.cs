using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.7.1 - Capa gráfica de humedad relativa ------------------------------

public static class RelativeHumidityLayer
{

    // ------------------------------ Fin de sección 2.7.1 ------------------------------


    // ------------------------------ Sección 2.7.2 - Curvas de humedad relativa ------------------------------

    private static readonly double[] RelativeHumidityFractions =
    {
        0.10,
        0.20,
        0.30,
        0.40,
        0.50,
        0.60,
        0.70,
        0.80,
        0.90
    };

    public static void Draw(
        Canvas canvas,
        ChartGeometry geometry,
        double totalPressureKPa)
    {
        if (canvas.ActualWidth <= 0.0
            || canvas.ActualHeight <= 0.0
            || !double.IsFinite(totalPressureKPa)
            || totalPressureKPa <= 0.0)
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

        foreach (double relativeHumidityFraction
                 in RelativeHumidityFractions)
        {
            DrawRelativeHumidityCurve(
                canvas,
                geometry,
                plotRect,
                totalPressureKPa,
                relativeHumidityFraction);
        }
    }

    // ------------------------------ Fin de sección 2.7.2 ------------------------------


    // ------------------------------ Sección 2.7.3 - Construcción de cada curva ------------------------------

    private static void DrawRelativeHumidityCurve(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect,
        double totalPressureKPa,
        double relativeHumidityFraction)
    {
        Polyline curve =
            new()
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 0.8,
                StrokeLineJoin = PenLineJoin.Round,
                Clip = new RectangleGeometry(plotRect)
            };

        const double temperatureStepC =
            0.20;

        for (
            double dryBulbC = geometry.MinDryBulbC;
            dryBulbC <= geometry.MaxDryBulbC + 1e-9;
            dryBulbC += temperatureStepC)
        {
            double humidityRatio =
                RelativeHumidity
                    .HumidityRatioFromDryBulbAndRelativeHumidity(
                        dryBulbC,
                        relativeHumidityFraction,
                        totalPressureKPa);

            if (!double.IsFinite(humidityRatio))
            {
                break;
            }

            if (humidityRatio
                < geometry.MinHumidityRatioKgPerKg)
            {
                continue;
            }

            if (humidityRatio
                > geometry.MaxHumidityRatioKgPerKg)
            {
                break;
            }

            curve.Points.Add(
                new Point(
                    geometry.XFromDryBulbC(
                        dryBulbC,
                        plotRect),
                    geometry.YFromHumidityRatioKgPerKg(
                        humidityRatio,
                        plotRect)));
        }

        if (curve.Points.Count < 2)
        {
            return;
        }

        canvas.Children.Add(curve);

        double labelFraction =
            0.78
            - relativeHumidityFraction * 0.48;

        int labelIndex =
            Math.Clamp(
                (int)Math.Round(
                    (curve.Points.Count - 2)
                    * labelFraction),
                1,
                curve.Points.Count - 2);

        DrawLabel(
            canvas,
            relativeHumidityFraction,
            curve.Points[labelIndex],
            curve.Points[labelIndex + 1]);
    }

    // ------------------------------ Fin de sección 2.7.3 ------------------------------


    // ------------------------------ Sección 2.7.4 - Etiquetas de porcentaje ------------------------------

    private static void DrawLabel(
        Canvas canvas,
        double relativeHumidityFraction,
        Point point,
        Point nextPoint)
    {
        TextBlock label =
            new()
            {
                Text =
                    (relativeHumidityFraction * 100.0)
                    .ToString(
                        "0",
                        CultureInfo.CurrentCulture)
                    + " %",
                FontSize = 10.0,
                Foreground = Brushes.DimGray,
                Background = Brushes.White
            };

        double deltaX =
            nextPoint.X - point.X;

        double deltaY =
            nextPoint.Y - point.Y;

        double length =
            Math.Sqrt(
                deltaX * deltaX
                + deltaY * deltaY);

        double normalX =
            length > 1e-9
                ? deltaY / length
                : 0.0;

        double normalY =
            length > 1e-9
                ? -deltaX / length
                : -1.0;

        if (normalY > 0.0)
        {
            normalX =
                -normalX;

            normalY =
                -normalY;
        }

        const double offset =
            9.0;

        double anchorX =
            point.X
            + normalX * offset;

        double anchorY =
            point.Y
            + normalY * offset;

        canvas.Children.Add(label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Canvas.SetLeft(
            label,
            anchorX
            - label.DesiredSize.Width / 2.0);

        Canvas.SetTop(
            label,
            anchorY
            - label.DesiredSize.Height
            - 1.0);
    }

    // ------------------------------ Fin de sección 2.7.4 ------------------------------


    // ------------------------------ Sección 2.7.5 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.7.5 ------------------------------