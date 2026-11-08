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
            plotRect, geometry);
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
                    geometry,
                    ref currentSegment,
                    ref labelDrawn);

                continue;
            }

            Point nextPoint=new(
                geometry.XFromDryBulbC(dryBulbC,plotRect),
                geometry.YFromHumidityRatioKgPerKg(humidityRatio,plotRect));
            if(currentSegment is not null && currentSegment.Points.Count>0 &&
               Math.Abs(currentSegment.Points[^1].Y-nextPoint.Y)>plotRect.Height*.12)
                AddCompletedSegment(canvas,targetDeviationKJPerKg,geometry,
                    ref currentSegment,ref labelDrawn);
            currentSegment ??= CreateCurve(plotRect);
            currentSegment.Points.Add(nextPoint);
        }

        AddCompletedSegment(
            canvas,
            targetDeviationKJPerKg,
            geometry,
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

            bool increasing = maximumDeviation >= minimumDeviation;
            if ((middleDeviation < targetDeviationKJPerKg) == increasing)
                lowerWetBulbC = middleWetBulbC;
            else
                upperWetBulbC = middleWetBulbC;
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
            Stroke = new SolidColorBrush(Color.FromRgb(0,117,120)),
            StrokeThickness = 1.1,

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
        ChartGeometry geometry,
        ref Polyline? segment,
        ref bool labelDrawn)
    {
        // Evitar microfragmentos de 1-3 muestras que no forman una isolínea legible.
        if (segment is null
            || segment.Points.Count < 12)
        {
            segment =
                null;

            return;
        }

        canvas.Children.Add(
            segment);

        if (!labelDrawn && segment.Points.Count >= 20)
        {
            DrawCurveLabel(
                canvas,
                deviationKJPerKg,
                geometry,
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
        ChartGeometry geometry,
        Polyline curve)
    {
        if (curve.Points.Count < 2)
        {
            return;
        }

        double normalizedMagnitude =
            Math.Clamp(
                Math.Abs(deviationKJPerKg) / 1.20,
                0.0,
                1.0);

        double labelFraction =
            0.30
            + normalizedMagnitude * 0.45;

        int index =
            Math.Clamp(
                (int)Math.Round(
                    (curve.Points.Count - 2)
                    * labelFraction),
                0,
                curve.Points.Count - 2);

        Point point =
            curve.Points[index];

        Point nextPoint =
            curve.Points[index + 1];

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
            10.0;

        TextBlock label =
            new()
            {
                Text = "Δh " + PsychrometricUnits.EnthalpyFromSI(deviationKJPerKg,geometry.DisplayUnits).ToString(
                    geometry.DisplayUnits == UnitSystem.SI ? "0.0" : "0.000", CultureInfo.CurrentCulture),

                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0,103,107)),
                Background = Brushes.White,
                ToolTip = "Desviación de entalpía respecto de la entalpía saturada a Tbh. No es entropía."
            };

        canvas.Children.Add(
            label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Rect visible=geometry.GetPlotRect(canvas.ActualWidth,canvas.ActualHeight);
        double left=Math.Clamp(point.X+normalX*offset-label.DesiredSize.Width/2.0,
            visible.Left+3, Math.Max(visible.Left+3,visible.Right-label.DesiredSize.Width-4));
        double top=Math.Clamp(point.Y+normalY*offset-label.DesiredSize.Height-1,
            visible.Top+2, Math.Max(visible.Top+2,visible.Bottom-label.DesiredSize.Height-2));
        Canvas.SetLeft(label,left);
        Canvas.SetTop(label,top);
    }

    private static void DrawScaleTitle(Canvas canvas,Rect plotRect,ChartGeometry geometry)
    {
        // Título legible en el margen superior sin atravesar curvas o puntos.
        string unit=geometry.DisplayUnits==UnitSystem.SI?"kJ/kgda":"Btu/lbda";
        var title=new TextBlock
        {
            Text=$"Δh = h − hsat(Tbh)  [{unit}]",
            FontSize=10,
            FontWeight=FontWeights.SemiBold,
            Foreground=new SolidColorBrush(Color.FromRgb(0,103,107)),
            Background=Brushes.White,
            ToolTip="Desviación de entalpía aproximada del modelo Carrier/VALCON; distinta de la entropía."
        };
        canvas.Children.Add(title);
        Canvas.SetLeft(title,plotRect.Left+8);
        Canvas.SetTop(title,plotRect.Top+4);
    }

    // ------------------------------ Fin de sección 2.10.6 ------------------------------


    // ------------------------------ Sección 2.10.7 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.10.7 ------------------------------
