using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.6.1 - Capa gráfica de entalpía ------------------------------

public static class EnthalpyLayer
{

    // ------------------------------ Fin de sección 2.6.1 ------------------------------


    // ------------------------------ Sección 2.6.2 - Configuración gráfica ------------------------------

    private const double MinorEnthalpyStepKJPerKg =
        5.0;

    private const double MajorEnthalpyStepKJPerKg =
        10.0;

    private const double TemperatureCalculationStepC =
        0.20;

    // ------------------------------ Fin de sección 2.6.2 ------------------------------


    // ------------------------------ Sección 2.6.3 - Dibujado principal ------------------------------

    public static void Draw(
        Canvas canvas,
        ChartGeometry geometry,
        double totalPressureKPa)
    {
        if (canvas.ActualWidth <= 0.0
            || canvas.ActualHeight <= 0.0)
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

        double minimumEnthalpy =
            GetMinimumVisibleEnthalpy(
                geometry);

        double maximumEnthalpy =
            GetMaximumVisibleEnthalpy(
                geometry);

        double firstEnthalpy =
            Math.Floor(
                minimumEnthalpy
                / MinorEnthalpyStepKJPerKg)
            * MinorEnthalpyStepKJPerKg;

        double lastEnthalpy =
            Math.Ceiling(
                maximumEnthalpy
                / MinorEnthalpyStepKJPerKg)
            * MinorEnthalpyStepKJPerKg;

        for (
            double enthalpy = firstEnthalpy;
            enthalpy <= lastEnthalpy + 1e-9;
            enthalpy += MinorEnthalpyStepKJPerKg)
        {
            bool isMajor =
                IsMajorEnthalpy(
                    enthalpy);

            DrawConstantEnthalpyLine(
                canvas,
                geometry,
                plotRect,
                enthalpy,
                isMajor);
        }
    }

    // ------------------------------ Fin de sección 2.6.3 ------------------------------


    // ------------------------------ Sección 2.6.4 - Líneas de entalpía constante ------------------------------

    private static void DrawConstantEnthalpyLine(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect,
        double enthalpyKJPerKg,
        bool isMajor)
    {
        Polyline? currentSegment =
            null;

        Point? firstVisiblePoint =
            null;

        for (
            double dryBulbC = geometry.MinDryBulbC;
            dryBulbC <= geometry.MaxDryBulbC + 1e-9;
            dryBulbC += TemperatureCalculationStepC)
        {
            double humidityRatio =
                MoistAirEnthalpy
                    .HumidityRatioFromEnthalpyAndDryBulb(
                        enthalpyKJPerKg,
                        dryBulbC);

            bool valid =
                IsPointInsideVisibleChartWindow(
                    humidityRatio,
                    geometry);

            if (!valid)
            {
                if (currentSegment is not null
                    && currentSegment.Points.Count >= 2)
                {
                    canvas.Children.Add(
                        currentSegment);
                }

                currentSegment =
                    null;

                continue;
            }

            double x =
                geometry.XFromDryBulbC(
                    dryBulbC,
                    plotRect);

            double y =
                geometry.YFromHumidityRatioKgPerKg(
                    humidityRatio,
                    plotRect);

            Point point =
                new(
                    x,
                    y);

            if (firstVisiblePoint is null)
            {
                firstVisiblePoint =
                    point;
            }

            if (currentSegment is null)
            {
                currentSegment =
                    CreatePolyline(
                        plotRect,
                        isMajor);
            }

            currentSegment.Points.Add(
                point);
        }

        if (currentSegment is not null
            && currentSegment.Points.Count >= 2)
        {
            canvas.Children.Add(
                currentSegment);
        }

        if (isMajor
            && firstVisiblePoint.HasValue)
        {
            DrawLabel(
                canvas,
                enthalpyKJPerKg,
                firstVisiblePoint.Value,
                plotRect);
        }
    }

    // ------------------------------ Fin de sección 2.6.4 ------------------------------


    // ------------------------------ Sección 2.6.5 - Validación solo contra la ventana visible ------------------------------

    private static bool IsPointInsideVisibleChartWindow(
        double humidityRatioKgPerKg,
        ChartGeometry geometry)
    {
        if (!double.IsFinite(humidityRatioKgPerKg))
        {
            return false;
        }

        if (humidityRatioKgPerKg
            < geometry.MinHumidityRatioKgPerKg)
        {
            return false;
        }

        if (humidityRatioKgPerKg
            > geometry.MaxHumidityRatioKgPerKg)
        {
            return false;
        }

        return true;
    }

    // ------------------------------ Fin de sección 2.6.5 ------------------------------


    // ------------------------------ Sección 2.6.6 - Creación de polilínea ------------------------------

    private static Polyline CreatePolyline(
        Rect plotRect,
        bool isMajor)
    {
        return new Polyline
        {
            Stroke =
                isMajor
                    ? Brushes.Black
                    : Brushes.DimGray,

            StrokeThickness =
                isMajor
                    ? 1.45
                    : 0.75,

            StrokeLineJoin =
                PenLineJoin.Round,

            Clip =
                new RectangleGeometry(
                    plotRect)
        };
    }

    // ------------------------------ Fin de sección 2.6.6 ------------------------------


    // ------------------------------ Sección 2.6.7 - Etiqueta de entalpía en la zona izquierda superior ------------------------------

    private static void DrawLabel(
        Canvas canvas,
        double enthalpyKJPerKg,
        Point point,
        Rect plotRect)
    {
        TextBlock label =
            new()
            {
                Text =
                    enthalpyKJPerKg.ToString(
                        "0",
                        CultureInfo.CurrentCulture),

                FontSize = 10.0,
                Foreground = Brushes.DimGray,
                Background = Brushes.White
            };

        canvas.Children.Add(
            label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        double left =
            Math.Max(
                plotRect.Left + 2.0,
                point.X - label.DesiredSize.Width - 6.0);

        double top =
            Math.Max(
                plotRect.Top,
                point.Y - label.DesiredSize.Height - 2.0);

        Canvas.SetLeft(
            label,
            left);

        Canvas.SetTop(
            label,
            top);
    }

    // ------------------------------ Fin de sección 2.6.7 ------------------------------


    // ------------------------------ Sección 2.6.8 - Rango automático de entalpía ------------------------------

    private static double GetMinimumVisibleEnthalpy(
        ChartGeometry geometry)
    {
        double h1 =
            MoistAirEnthalpy.FromDryBulbAndHumidityRatio(
                geometry.MinDryBulbC,
                geometry.MinHumidityRatioKgPerKg);

        double h2 =
            MoistAirEnthalpy.FromDryBulbAndHumidityRatio(
                geometry.MaxDryBulbC,
                geometry.MinHumidityRatioKgPerKg);

        return Math.Min(
            h1,
            h2);
    }

    private static double GetMaximumVisibleEnthalpy(
        ChartGeometry geometry)
    {
        double h1 =
            MoistAirEnthalpy.FromDryBulbAndHumidityRatio(
                geometry.MinDryBulbC,
                geometry.MaxHumidityRatioKgPerKg);

        double h2 =
            MoistAirEnthalpy.FromDryBulbAndHumidityRatio(
                geometry.MaxDryBulbC,
                geometry.MaxHumidityRatioKgPerKg);

        return Math.Max(
            h1,
            h2);
    }

    // ------------------------------ Fin de sección 2.6.8 ------------------------------


    // ------------------------------ Sección 2.6.9 - Clasificación de líneas ------------------------------

    private static bool IsMajorEnthalpy(
        double enthalpyKJPerKg)
    {
        double multiple =
            enthalpyKJPerKg
            / MajorEnthalpyStepKJPerKg;

        return Math.Abs(
            multiple - Math.Round(multiple))
            < 1e-8;
    }

    // ------------------------------ Fin de sección 2.6.9 ------------------------------


    // ------------------------------ Sección 2.6.10 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.6.10 ------------------------------