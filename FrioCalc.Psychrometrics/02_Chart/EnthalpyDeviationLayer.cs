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
                if (currentSegment is not null
                    && currentSegment.Points.Count >= 2)
                {
                    canvas.Children.Add(
                        currentSegment);

                    if (!labelDrawn)
                    {
                        DrawCurveLabel(
                            canvas,
                            targetDeviationKJPerKg,
                            currentSegment);

                        labelDrawn =
                            true;
                    }
                }

                currentSegment =
                    null;

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

        if (currentSegment is not null
            && currentSegment.Points.Count >= 2)
        {
            canvas.Children.Add(
                currentSegment);

            if (!labelDrawn)
            {
                DrawCurveLabel(
                    canvas,
                    targetDeviationKJPerKg,
                    currentSegment);
            }
        }
    }

    // ------------------------------ Fin de sección 2.10.4 ------------------------------


    // ------------------------------ Sección 2.10.5 - Resolución de humedad para desviación constante ------------------------------

    private static bool TryGetHumidityRatioForDeviation(
        double dryBulbC,
        double targetDeviationKJPerKg,
        ChartGeometry geometry,
        double totalPressureKPa,
        out double humidityRatioKgPerKg)
    {
        humidityRatioKgPerKg =
            double.NaN;

        double saturationPressureKPa =
            SaturationVaporPressure.GetPressureKPa(
                dryBulbC);

        if (saturationPressureKPa
            >= totalPressureKPa)
        {
            return false;
        }

        double saturationHumidityRatio =
            SaturationHumidityRatio.FromPressuresKPa(
                totalPressureKPa,
                saturationPressureKPa);

        double lowerHumidityRatio =
            Math.Max(
                0.0,
                geometry.MinHumidityRatioKgPerKg);

        double upperHumidityRatio =
            Math.Min(
                saturationHumidityRatio,
                geometry.MaxHumidityRatioKgPerKg);

        if (upperHumidityRatio
            <= lowerHumidityRatio)
        {
            return false;
        }

        double lowerDeviation =
            EnthalpyDeviation.FromDryBulbAndHumidityRatio(
                dryBulbC,
                lowerHumidityRatio,
                totalPressureKPa);

        double upperDeviation =
            EnthalpyDeviation.FromDryBulbAndHumidityRatio(
                dryBulbC,
                upperHumidityRatio,
                totalPressureKPa);

        if (!double.IsFinite(lowerDeviation)
            || !double.IsFinite(upperDeviation))
        {
            return false;
        }

        double minimumDeviation =
            Math.Min(
                lowerDeviation,
                upperDeviation);

        double maximumDeviation =
            Math.Max(
                lowerDeviation,
                upperDeviation);

        if (targetDeviationKJPerKg
                < minimumDeviation - 1e-6
            || targetDeviationKJPerKg
                > maximumDeviation + 1e-6)
        {
            return false;
        }

        double lower =
            lowerHumidityRatio;

        double upper =
            upperHumidityRatio;

        for (int iteration = 0;
             iteration < 34;
             iteration++)
        {
            double middle =
                (lower + upper) / 2.0;

            double middleDeviation =
                EnthalpyDeviation.FromDryBulbAndHumidityRatio(
                    dryBulbC,
                    middle,
                    totalPressureKPa);

            if (!double.IsFinite(middleDeviation))
            {
                return false;
            }

            if (middleDeviation
                < targetDeviationKJPerKg)
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

        humidityRatioKgPerKg =
            (lower + upper) / 2.0;

        return
            humidityRatioKgPerKg
                >= geometry.MinHumidityRatioKgPerKg
            && humidityRatioKgPerKg
                <= geometry.MaxHumidityRatioKgPerKg;
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

    private static void DrawCurveLabel(
        Canvas canvas,
        double deviationKJPerKg,
        Polyline curve)
    {
        if (curve.Points.Count == 0)
        {
            return;
        }

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
