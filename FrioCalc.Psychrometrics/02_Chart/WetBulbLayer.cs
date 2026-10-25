using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.8.1 - Capa gráfica de temperatura de bulbo húmedo ------------------------------

public static class WetBulbLayer
{

    // ------------------------------ Fin de sección 2.8.1 ------------------------------


    // ------------------------------ Sección 2.8.2 - Dibujado principal ------------------------------

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

        double visibleSpan =
            geometry.MaxDryBulbC
            - geometry.MinDryBulbC;

        double wetBulbStepC =
            visibleSpan > 90.0
                ? 10.0
                : 5.0;

        double minimumWetBulbC =
            Math.Max(
                SaturationVaporPressure.MinSupportedTemperatureC,
                geometry.MinDryBulbC - 30.0);

        double maximumWetBulbC =
            Math.Min(
                SaturationVaporPressure.MaxSupportedTemperatureC,
                geometry.MaxDryBulbC);

        double firstWetBulbC =
            Math.Ceiling(
                minimumWetBulbC / wetBulbStepC)
            * wetBulbStepC;

        for (
            double wetBulbC = firstWetBulbC;
            wetBulbC <= maximumWetBulbC + 1e-9;
            wetBulbC += wetBulbStepC)
        {
            DrawWetBulbLine(
                canvas,
                geometry,
                plotRect,
                totalPressureKPa,
                wetBulbC);
        }
    }

    // ------------------------------ Fin de sección 2.8.2 ------------------------------


    // ------------------------------ Sección 2.8.3 - Construcción de líneas de bulbo húmedo ------------------------------

    private static void DrawWetBulbLine(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect,
        double totalPressureKPa,
        double wetBulbC)
    {
        double saturationPressureAtWetBulbKPa =
            SaturationVaporPressure.GetPressureKPa(
                wetBulbC);

        if (saturationPressureAtWetBulbKPa
            >= totalPressureKPa)
        {
            return;
        }

        Polyline line =
            new()
            {
                Stroke = Brushes.DarkGray,
                StrokeThickness = 0.7,
                StrokeLineJoin = PenLineJoin.Round,
                Clip = new RectangleGeometry(plotRect)
            };

        const double dryBulbStepC =
            0.25;

        double firstDryBulbC =
            Math.Max(
                wetBulbC,
                geometry.MinDryBulbC);

        for (
            double dryBulbC = firstDryBulbC;
            dryBulbC <= geometry.MaxDryBulbC + 1e-9;
            dryBulbC += dryBulbStepC)
        {
            double humidityRatio =
                WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                    dryBulbC,
                    wetBulbC,
                    totalPressureKPa);

            if (!double.IsFinite(humidityRatio))
            {
                continue;
            }

            if (humidityRatio
                > geometry.MaxHumidityRatioKgPerKg)
            {
                continue;
            }

            if (humidityRatio
                < geometry.MinHumidityRatioKgPerKg)
            {
                break;
            }

            line.Points.Add(
                new Point(
                    geometry.XFromDryBulbC(
                        dryBulbC,
                        plotRect),
                    geometry.YFromHumidityRatioKgPerKg(
                        humidityRatio,
                        plotRect)));
        }

        if (line.Points.Count < 2)
        {
            return;
        }

        canvas.Children.Add(line);

        DrawLabel(
            canvas,
            wetBulbC,
            line.Points[0]);
    }

    // ------------------------------ Fin de sección 2.8.3 ------------------------------


    // ------------------------------ Sección 2.8.4 - Etiquetas de bulbo húmedo ------------------------------

    private static void DrawLabel(
        Canvas canvas,
        double wetBulbC,
        Point point)
    {
        TextBlock label =
            new()
            {
                Text =
                    wetBulbC.ToString(
                        "0",
                        CultureInfo.CurrentCulture)
                    + " °C",
                FontSize = 9.5,
                Foreground = Brushes.DimGray,
                Background = Brushes.White
            };

        canvas.Children.Add(label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Canvas.SetLeft(
            label,
            point.X + 3.0);

        Canvas.SetTop(
            label,
            point.Y
            - label.DesiredSize.Height
            - 1.0);
    }

    // ------------------------------ Fin de sección 2.8.4 ------------------------------


    // ------------------------------ Sección 2.8.5 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.8.5 ------------------------------