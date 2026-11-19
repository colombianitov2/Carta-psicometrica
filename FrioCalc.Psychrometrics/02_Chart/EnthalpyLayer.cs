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
                totalPressureKPa,
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
        double totalPressureKPa,
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
                    humidityRatio, dryBulbC, totalPressureKPa, geometry);

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
                plotRect,
                geometry);
        }
    }

    // ------------------------------ Fin de sección 2.6.4 ------------------------------


    // ------------------------------ Sección 2.6.5 - Validación solo contra la ventana visible ------------------------------

    private static bool IsPointInsideVisibleChartWindow(
        double humidityRatioKgPerKg,
        double dryBulbC,
        double pressureKPa,
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

        // La carta de aire húmedo monofásico no admite HR > 100%.
        double pws=SaturationVaporPressure.GetPressureKPa(dryBulbC);
        if (pws < pressureKPa)
        {
            double saturatedW=SaturationHumidityRatio.FromPressuresKPa(pressureKPa,pws);
            if (humidityRatioKgPerKg>saturatedW+1e-10) return false;
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
        Rect plotRect,
        ChartGeometry geometry)
    {
        TextBlock label =
            new()
            {
                Text =
                    PsychrometricUnits.EnthalpyFromSI(enthalpyKJPerKg,geometry.DisplayUnits).ToString(
                        "0",CultureInfo.CurrentCulture),

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

        // Prolongación tangente a la isentálpica hacia la región no saturada dibujable.
        // La prolongación es una guía gráfica, no un estado físico válido.
        double t = geometry.DryBulbCFromX(point.X, plotRect);
        double w = MoistAirEnthalpy.HumidityRatioFromEnthalpyAndDryBulb(enthalpyKJPerKg,t-0.25);
        var previous = new Point(geometry.XFromDryBulbC(t-0.25,plotRect),
            geometry.YFromHumidityRatioKgPerKg(w,plotRect));
        Vector direction = previous - point;
        if (direction.Length < 1e-8 || !double.IsFinite(direction.Length)) direction = new Vector(-1,-1);
        direction.Normalize();
        double distance = 32.0;
        if (direction.X < 0) distance = Math.Min(distance, (point.X-2)/-direction.X);
        if (direction.Y < 0) distance = Math.Min(distance, (point.Y-3)/-direction.Y);
        Point anchor = point + direction * Math.Max(0,distance);
        double left = Math.Clamp(anchor.X-label.DesiredSize.Width-2, 1,
            Math.Max(1,canvas.ActualWidth-label.DesiredSize.Width-1));
        double top = Math.Clamp(anchor.Y-label.DesiredSize.Height-2, 1,
            Math.Max(1,canvas.ActualHeight-label.DesiredSize.Height-1));
        if(distance>4)
            canvas.Children.Add(new Line { X1=point.X, Y1=point.Y,
                X2=anchor.X, Y2=anchor.Y, Stroke=Brushes.DimGray,
                StrokeThickness=0.7, StrokeDashArray=new DoubleCollection{3,2}, IsHitTestVisible=false });

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