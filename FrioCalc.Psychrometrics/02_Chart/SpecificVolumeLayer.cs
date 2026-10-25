using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.9.1 - Capa gráfica de volumen específico ------------------------------

public static class SpecificVolumeLayer
{

    // ------------------------------ Fin de sección 2.9.1 ------------------------------


    // ------------------------------ Sección 2.9.2 - Dibujado principal ------------------------------

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

        double minimumVolume =
            GetMinimumVisibleSpecificVolume(
                geometry,
                totalPressureKPa);

        double maximumVolume =
            GetMaximumVisibleSpecificVolume(
                geometry,
                totalPressureKPa);

        double volumeStep =
            GetNiceStep(
                (maximumVolume - minimumVolume)
                / 12.0);

        double firstVolume =
            Math.Ceiling(
                minimumVolume / volumeStep)
            * volumeStep;

        for (
            double specificVolume = firstVolume;
            specificVolume <= maximumVolume + 1e-9;
            specificVolume += volumeStep)
        {
            DrawSpecificVolumeLine(
                canvas,
                geometry,
                plotRect,
                totalPressureKPa,
                specificVolume);
        }
    }

    // ------------------------------ Fin de sección 2.9.2 ------------------------------


    // ------------------------------ Sección 2.9.3 - Construcción de líneas de volumen específico ------------------------------

    private static void DrawSpecificVolumeLine(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect,
        double totalPressureKPa,
        double specificVolumeM3PerKgDryAir)
    {
        Polyline line =
            new()
            {
                Stroke = Brushes.SlateGray,
                StrokeThickness = 0.65,
                StrokeLineJoin = PenLineJoin.Round,
                Clip = new RectangleGeometry(plotRect)
            };

        double dryBulbStepC =
            geometry.MaxDryBulbC
            - geometry.MinDryBulbC
            > 90.0
                ? 0.50
                : 0.25;

        for (
            double dryBulbC = geometry.MinDryBulbC;
            dryBulbC <= geometry.MaxDryBulbC + 1e-9;
            dryBulbC += dryBulbStepC)
        {
            double humidityRatio =
                MoistAirSpecificVolume
                    .HumidityRatioFromDryBulbAndSpecificVolume(
                        dryBulbC,
                        specificVolumeM3PerKgDryAir,
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

            double saturationPressureKPa =
                SaturationVaporPressure.GetPressureKPa(
                    dryBulbC);

            if (saturationPressureKPa
                < totalPressureKPa)
            {
                double saturatedHumidityRatio =
                    SaturationHumidityRatio.FromPressuresKPa(
                        totalPressureKPa,
                        saturationPressureKPa);

                if (humidityRatio
                    > saturatedHumidityRatio + 1e-9)
                {
                    continue;
                }
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

        AddBottomIntersectionPoint(
            line,
            geometry,
            plotRect,
            totalPressureKPa,
            specificVolumeM3PerKgDryAir);

        canvas.Children.Add(line);

        DrawBottomLabel(
            canvas,
            geometry,
            plotRect,
            totalPressureKPa,
            specificVolumeM3PerKgDryAir);
    }

    // ------------------------------ Fin de sección 2.9.3 ------------------------------


    // ------------------------------ Sección 2.9.4 - Rango dinámico de volumen específico ------------------------------

    private static double GetMinimumVisibleSpecificVolume(
        ChartGeometry geometry,
        double totalPressureKPa)
    {
        double v1 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MinDryBulbC,
                geometry.MinHumidityRatioKgPerKg,
                totalPressureKPa);

        double v2 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MaxDryBulbC,
                geometry.MinHumidityRatioKgPerKg,
                totalPressureKPa);

        double v3 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MinDryBulbC,
                geometry.MaxHumidityRatioKgPerKg,
                totalPressureKPa);

        double v4 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MaxDryBulbC,
                geometry.MaxHumidityRatioKgPerKg,
                totalPressureKPa);

        return Math.Min(
            Math.Min(v1, v2),
            Math.Min(v3, v4));
    }

    private static double GetMaximumVisibleSpecificVolume(
        ChartGeometry geometry,
        double totalPressureKPa)
    {
        double v1 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MinDryBulbC,
                geometry.MinHumidityRatioKgPerKg,
                totalPressureKPa);

        double v2 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MaxDryBulbC,
                geometry.MinHumidityRatioKgPerKg,
                totalPressureKPa);

        double v3 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MinDryBulbC,
                geometry.MaxHumidityRatioKgPerKg,
                totalPressureKPa);

        double v4 =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                geometry.MaxDryBulbC,
                geometry.MaxHumidityRatioKgPerKg,
                totalPressureKPa);

        return Math.Max(
            Math.Max(v1, v2),
            Math.Max(v3, v4));
    }

    // ------------------------------ Fin de sección 2.9.4 ------------------------------


    // ------------------------------ Sección 2.9.5 - Etiquetas ASHRAE en el borde inferior y paso de escala ------------------------------

    private static void AddBottomIntersectionPoint(
        Polyline line,
        ChartGeometry geometry,
        Rect plotRect,
        double totalPressureKPa,
        double specificVolumeM3PerKgDryAir)
    {
        double bottomDryBulbC =
            GetDryBulbAtHumidityRatio(
                specificVolumeM3PerKgDryAir,
                geometry.MinHumidityRatioKgPerKg,
                totalPressureKPa);

        if (!double.IsFinite(bottomDryBulbC)
            || bottomDryBulbC < geometry.MinDryBulbC
            || bottomDryBulbC > geometry.MaxDryBulbC)
        {
            return;
        }

        Point bottomPoint =
            new(
                geometry.XFromDryBulbC(
                    bottomDryBulbC,
                    plotRect),
                plotRect.Bottom);

        if (line.Points.Count == 0)
        {
            line.Points.Add(bottomPoint);
            return;
        }

        Point lastPoint =
            line.Points[^1];

        if (Math.Abs(lastPoint.X - bottomPoint.X) > 0.25
            || Math.Abs(lastPoint.Y - bottomPoint.Y) > 0.25)
        {
            line.Points.Add(bottomPoint);
        }
    }

    private static void DrawBottomLabel(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect,
        double totalPressureKPa,
        double specificVolumeM3PerKgDryAir)
    {
        double bottomDryBulbC =
            GetDryBulbAtHumidityRatio(
                specificVolumeM3PerKgDryAir,
                geometry.MinHumidityRatioKgPerKg,
                totalPressureKPa);

        if (!double.IsFinite(bottomDryBulbC)
            || bottomDryBulbC < geometry.MinDryBulbC
            || bottomDryBulbC > geometry.MaxDryBulbC)
        {
            return;
        }

        double x =
            geometry.XFromDryBulbC(
                bottomDryBulbC,
                plotRect);

        TextBlock label =
            new()
            {
                Text =
                    specificVolumeM3PerKgDryAir.ToString(
                        "0.00",
                        CultureInfo.CurrentCulture),
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
            x - (label.DesiredSize.Width / 2.0));

        Canvas.SetTop(
            label,
            plotRect.Bottom + 38.0);
    }

    private static double GetDryBulbAtHumidityRatio(
        double specificVolumeM3PerKgDryAir,
        double humidityRatioKgPerKg,
        double totalPressureKPa)
    {
        double pressurePa =
            totalPressureKPa * 1000.0;

        double temperatureK =
            specificVolumeM3PerKgDryAir
            * pressurePa
            / (
                MoistAirSpecificVolume.DryAirGasConstantJPerKgK
                * (
                    1.0
                    + MoistAirSpecificVolume.WaterVaporVolumeFactor
                    * humidityRatioKgPerKg));

        return
            temperatureK
            - MoistAirSpecificVolume.KelvinOffset;
    }

    private static double GetNiceStep(
        double roughStep)
    {
        if (!double.IsFinite(roughStep)
            || roughStep <= 0.0)
        {
            return 0.05;
        }

        double power =
            Math.Pow(
                10.0,
                Math.Floor(
                    Math.Log10(roughStep)));

        double normalized =
            roughStep / power;

        double factor =
            normalized <= 1.0
                ? 1.0
                : normalized <= 2.0
                    ? 2.0
                    : normalized <= 5.0
                        ? 5.0
                        : 10.0;

        return factor * power;
    }

    // ------------------------------ Fin de sección 2.9.5 ------------------------------


    // ------------------------------ Sección 2.9.6 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.9.6 ------------------------------