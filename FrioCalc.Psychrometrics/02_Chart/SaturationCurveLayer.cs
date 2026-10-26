using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.5.1 - Definición de capa de saturación ------------------------------

public static class SaturationCurveLayer
{

    // ------------------------------ Fin de sección 2.5.1 ------------------------------


    // ------------------------------ Sección 2.5.2 - Dibujado principal de la curva ------------------------------

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

        if (!double.IsFinite(totalPressureKPa)
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

        Polyline saturationCurve =
            new()
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2.2,
                StrokeLineJoin = PenLineJoin.Round,

                Clip =
                    new RectangleGeometry(
                        plotRect)
            };

        BuildCurvePoints(
            saturationCurve,
            geometry,
            plotRect,
            totalPressureKPa);

        if (saturationCurve.Points.Count >= 2)
        {
            canvas.Children.Add(
                saturationCurve);
        }
    }

    // ------------------------------ Fin de sección 2.5.2 ------------------------------


    // ------------------------------ Sección 2.5.3 - Generación de puntos termodinámicos ------------------------------

    private static void BuildCurvePoints(
        Polyline curve,
        ChartGeometry geometry,
        Rect plotRect,
        double totalPressureKPa)
    {
        const double temperatureStepC = 0.1;

        double minimumTemperatureC =
            Math.Max(
                geometry.MinDryBulbC,
                SaturationVaporPressure.MinSupportedTemperatureC);

        double maximumTemperatureC =
            Math.Min(
                geometry.MaxDryBulbC,
                SaturationVaporPressure.MaxSupportedTemperatureC);

        for (
            double dryBulbC = minimumTemperatureC;
            dryBulbC <= maximumTemperatureC;
            dryBulbC += temperatureStepC)
        {
            double saturationVaporPressureKPa =
                SaturationVaporPressure.GetPressureKPa(
                    dryBulbC);

            if (saturationVaporPressureKPa
                >= totalPressureKPa)
            {
                break;
            }

            double saturationHumidityRatio =
                SaturationHumidityRatio.FromPressuresKPa(
                    totalPressureKPa,
                    saturationVaporPressureKPa);

            double x =
                geometry.XFromDryBulbC(
                    dryBulbC,
                    plotRect);

            double y =
                geometry.YFromHumidityRatioKgPerKg(
                    saturationHumidityRatio,
                    plotRect);

            curve.Points.Add(
                new Point(
                    x,
                    y));
        }
    }

    // ------------------------------ Fin de sección 2.5.3 ------------------------------


    // ------------------------------ Sección 2.5.4 - Cierre del módulo ------------------------------

}

// ------------------------------ Fin de sección 2.5.4 ------------------------------