using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.10.1 - Capa gráfica de desviación de entalpía ------------------------------

public static class EnthalpyDeviationLayer
{

    // ------------------------------ Fin de sección 2.10.1 ------------------------------


    // ------------------------------ Sección 2.10.2 - Valores de desviación ------------------------------

    private static readonly double[] DeviationValuesKJPerKg =
    {
        -0.20,
        -0.40,
        -0.60,
        -0.80,
        -1.00,
        -1.20
    };

    // ------------------------------ Fin de sección 2.10.2 ------------------------------


    // ------------------------------ Sección 2.10.3 - Dibujado principal ------------------------------

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

        foreach (double deviationKJPerKg
                 in DeviationValuesKJPerKg)
        {
            DrawDeviationCurve(
                canvas,
                geometry,
                plotRect,
                totalPressureKPa,
                deviationKJPerKg);
        }

        DrawScaleTitle(
            canvas,
            plotRect);
    }

    // ------------------------------ Fin de sección 2.10.3 ------------------------------


    // ------------------------------ Sección 2.10.4 - Construcción de curvas ------------------------------

    private static void DrawDeviationCurve(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect,
        double totalPressureKPa,
        double targetDeviationKJPerKg)
    {
        Polyline? currentSegment =
            null;

        bool labelDrawn =
            false;

        double dryBulbStepC =
            geometry.MaxDryBulbC
            - geometry.MinDryBulbC
            > 90.0
                ? 1.0
                : 0.5;

        for (
            double dryBulbC = geometry.MinDryBulbC;
            dryBulbC <= geometry.MaxDryBulbC + 1e-9;
            dryBulbC += dryBulbStepC)
        {
            if (!TryGetHumidityRatioForDeviation(
                    dryBulbC,
                    targetDeviationKJPerKg,
                    geometry,
                    totalPressureKPa,
                    out double humidityRatio))
            {
                AddCompletedSegment(
                    canvas,
                    targetDeviationKJPerKg,
                    ref currentSegment,
                    ref labelDrawn);

                continue;
            }

            currentSegment ??=
                CreateCurve(
                    plotRect);

            currentSegment.Points.Add(
                new Point(
                    geometry.XFromDryBulbC(
                        dryBulbC,
                        plotRect),
                    geometry.YFromHumidityRatioKgPerKg(
                        humidityRatio,
                        plotRect)));
        }

        AddCompletedSegment(
            canvas,
            targetDeviationKJPerKg,
            ref currentSegment,
            ref labelDrawn);
    }

    // ------------------------------ Fin de sección 2.10.4 ------------------------------


    // ------------------------------ Sección 2.10.5 - Resolución directa sobre bulbo húmedo ------------------------------

    private static bool TryGetHumidityRatioForDeviation(
        double dryBulbC,
        double targetDeviationKJPerKg,
        ChartGeometry geometry,
        double totalPressureKPa,
        out double humidityRatioKgPerKg)
    {
        humidityRatioKgPerKg =
            double.NaN;

        double maximumWetBulbC =
            Math.Min(
                dryBulbC,
                GetMaximumWetBulbBelowPressure(
                    totalPressureKPa));

        double minimumCandidateWetBulbC =
            Math.Min(
                maximumWetBulbC,
                SaturationVaporPressure.MinSupportedTemperatureC);

        if (!TrySolveWetBulbForHumidityRatio(
                dryBulbC,
                geometry.MinHumidityRatioKgPerKg,
                totalPressureKPa,
                minimumCandidateWetBulbC,
                maximumWetBulbC,
                out double minimumVisibleWetBulbC))
        {
            return false;
        }

        double maximumHumidityAtWetBulb =
            WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                dryBulbC,
                maximumWetBulbC,
                totalPressureKPa);

        if (!double.IsFinite(maximumHumidityAtWetBulb)
            || maximumHumidityAtWetBulb < 0.0)
        {
            return false;
        }

        double maximumVisibleWetBulbC =
            maximumWetBulbC;

        if (geometry.MaxHumidityRatioKgPerKg
            < maximumHumidityAtWetBulb)
        {
            if (!TrySolveWetBulbForHumidityRatio(
                    dryBulbC,
                    geometry.MaxHumidityRatioKgPerKg,
                    totalPressureKPa,
                    minimumVisibleWetBulbC,
                    maximumWetBulbC,
                    out maximumVisibleWetBulbC))
            {
                return false;
            }
        }

        double minimumDeviation =
            EnthalpyDeviation.FromDryBulbAndWetBulb(
                dryBulbC,
                minimumVisibleWetBulbC,
                totalPressureKPa);

        double maximumDeviation =
            EnthalpyDeviation.FromDryBulbAndWetBulb(
                dryBulbC,
                maximumVisibleWetBulbC,
                totalPressureKPa);

        if (!double.IsFinite(minimumDeviation)
            || !double.IsFinite(maximumDeviation))
        {
            return false;
        }

        double rangeMinimum =
            Math.Min(
                minimumDeviation,
                maximumDeviation);

        double rangeMaximum =
            Math.Max(
                minimumDeviation,
                maximumDeviation);

        if (targetDeviationKJPerKg
                < rangeMinimum - 1e-6
            || targetDeviationKJPerKg
                > rangeMaximum + 1e-6)
        {
            return false;
        }

        double lowerWetBulbC =
            minimumVisibleWetBulbC;

        double upperWetBulbC =
            maximumVisibleWetBulbC;

        for (int iteration = 0;
             iteration < 34;
             iteration++)
        {
            double middleWetBulbC =
                (lowerWetBulbC + upperWetBulbC)
                / 2.0;

            double middleDeviation =
                EnthalpyDeviation.FromDryBulbAndWetBulb(
                    dryBulbC,
                    middleWetBulbC,
                    totalPressureKPa);

            if (!double.IsFinite(middleDeviation))
            {
                return false;
            }

            if (middleDeviation
                < targetDeviationKJPerKg)
            {
                lowerWetBulbC =
                    middleWetBulbC;
            }
            else
            {
                upperWetBulbC =
                    middleWetBulbC;
            }
        }

        double wetBulbC =
            (lowerWetBulbC + upperWetBulbC)
            / 2.0;

        humidityRatioKgPerKg =
            WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                dryBulbC,
                wetBulbC,
                totalPressureKPa);

        return
            double.IsFinite(
                humidityRatioKgPerKg)
            && humidityRatioKgPerKg
                >= geometry.MinHumidityRatioKgPerKg - 1e-9
            && humidityRatioKgPerKg
                <= geometry.MaxHumidityRatioKgPerKg + 1e-9;
    }

    private static bool TrySolveWetBulbForHumidityRatio(
        double dryBulbC,
        double targetHumidityRatioKgPerKg,
        double totalPressureKPa,
        double minimumWetBulbC,
        double maximumWetBulbC,
        out double wetBulbC)
    {
        wetBulbC =
            double.NaN;

        if (targetHumidityRatioKgPerKg < 0.0
            || maximumWetBulbC < minimumWetBulbC)
        {
            return false;
        }

        double lower =
            minimumWetBulbC;

        double upper =
            maximumWetBulbC;

        double lowerHumidity =
            WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                dryBulbC,
                lower,
                totalPressureKPa);

        double upperHumidity =
            WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                dryBulbC,
                upper,
                totalPressureKPa);

        if (!double.IsFinite(lowerHumidity)
            || !double.IsFinite(upperHumidity))
        {
            return false;
        }

        if (targetHumidityRatioKgPerKg
                < lowerHumidity - 1e-9
            || targetHumidityRatioKgPerKg
                > upperHumidity + 1e-9)
        {
            return false;
        }

        for (int iteration = 0;
             iteration < 40;
             iteration++)
        {
            double middle =
                (lower + upper) / 2.0;

            double middleHumidity =
                WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                    dryBulbC,
                    middle,
                    totalPressureKPa);

            if (!double.IsFinite(middleHumidity))
            {
                return false;
            }

            if (middleHumidity
                < targetHumidityRatioKgPerKg)
            {
                lower =
                    middle;
            }
            else
            {
                upper =
                    middle;
            }
        }

        wetBulbC =
            (lower + upper) / 2.0;

        return true;
    }

    private static double GetMaximumWetBulbBelowPressure(
        double totalPressureKPa)
    {
        double maximumSupportedC =
            SaturationVaporPressure.MaxSupportedTemperatureC;

        if (SaturationVaporPressure.GetPressureKPa(
                maximumSupportedC)
            < totalPressureKPa)
        {
            return maximumSupportedC;
        }

        double lower =
            SaturationVaporPressure.MinSupportedTemperatureC;

        double upper =
            maximumSupportedC;

        double targetPressureKPa =
            totalPressureKPa * 0.999999;

        for (int iteration = 0;
             iteration < 60;
             iteration++)
        {
            double middle =
                (lower + upper) / 2.0;

            double saturationPressureKPa =
                SaturationVaporPressure.GetPressureKPa(
                    middle);

            if (saturationPressureKPa
                < targetPressureKPa)
            {
                lower =
                    middle;
            }
            else
            {
                upper =
                    middle;
            }
        }

        return lower;
    }

    // ------------------------------ Fin de sección 2.10.5 ------------------------------


    // ------------------------------ Sección 2.10.6 - Estilo y etiquetas ------------------------------

    private static Polyline CreateCurve(
        Rect plotRect)
    {
        return new Polyline
        {
            Stroke =
                Brushes.Gray,

            StrokeThickness =
                0.65,

            StrokeDashArray =
                new DoubleCollection
                {
                    3.0,
                    2.0
                },

            StrokeLineJoin =
                PenLineJoin.Round,

            Clip =
                new RectangleGeometry(
                    plotRect)
        };
    }

    private static void AddCompletedSegment(
        Canvas canvas,
        double deviationKJPerKg,
        ref Polyline? segment,
        ref bool labelDrawn)
    {
        if (segment is null
            || segment.Points.Count < 2)
        {
            segment =
                null;

            return;
        }

        canvas.Children.Add(
            segment);

        if (!labelDrawn)
        {
            DrawCurveLabel(
                canvas,
                deviationKJPerKg,
                segment);

            labelDrawn =
                true;
        }

        segment =
            null;
    }

    private static void DrawCurveLabel(
        Canvas canvas,
        double deviationKJPerKg,
        Polyline curve)
    {
        int index =
            Math.Clamp(
                curve.Points.Count / 2,
                0,
                curve.Points.Count - 1);

        Point point =
            curve.Points[index];

        TextBlock label =
            new()
            {
                Text =
                    deviationKJPerKg.ToString(
                        "0.0",
                        CultureInfo.CurrentCulture),

                FontSize =
                    9.0,

                Foreground =
                    Brushes.DimGray,

                Background =
                    Brushes.White
            };

        canvas.Children.Add(
            label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Canvas.SetLeft(
            label,
            point.X + 5.0);

        Canvas.SetTop(
            label,
            point.Y
            - label.DesiredSize.Height
            - 3.0);
    }

    private static void DrawScaleTitle(
        Canvas canvas,
        Rect plotRect)
    {
        TextBlock title =
            new()
            {
                Text =
                    "Desviación de entalpía\n(kJ/kg aire seco)",

                FontSize =
                    9.0,

                FontWeight =
                    FontWeights.SemiBold,

                Foreground =
                    Brushes.DimGray,

                Background =
                    Brushes.White,

                TextAlignment =
                    TextAlignment.Center,

                LayoutTransform =
                    new RotateTransform(
                        -72.0)
            };

        canvas.Children.Add(
            title);

        title.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Canvas.SetLeft(
            title,
            plotRect.Left
            + plotRect.Width * 0.69);

        Canvas.SetTop(
            title,
            plotRect.Top
            + plotRect.Height * 0.43);
    }

    // ------------------------------ Fin de sección 2.10.6 ------------------------------


    // ------------------------------ Sección 2.10.7 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.10.7 ------------------------------
