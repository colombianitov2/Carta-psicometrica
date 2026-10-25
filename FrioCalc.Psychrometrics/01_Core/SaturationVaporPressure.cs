using System;

namespace FrioCalc.Psychrometrics.Core;

// ============================================================
// [1.4.1] INICIO - Definición del módulo de presión de saturación
// ============================================================

public static class SaturationVaporPressure
{

    // ============================================================
    // [1.4.1] FIN - Definición del módulo de presión de saturación
    // ============================================================


    // ============================================================
    // [1.4.2] INICIO - Constantes y rango de validez
    // ============================================================

    public const double TriplePointWaterC = 0.01;

    public const double KelvinOffset = 273.15;

    public const double MinSupportedTemperatureC = -100.0;

    public const double MaxSupportedTemperatureC = 200.0;

    // ============================================================
    // [1.4.2] FIN - Constantes y rango de validez
    // ============================================================


    // ============================================================
    // [1.4.3] INICIO - Cálculo público de Pws en pascales
    // ============================================================

    public static double GetPressurePa(
        double dryBulbTemperatureC)
    {
        ValidateTemperature(
            dryBulbTemperatureC);

        double temperatureK =
            dryBulbTemperatureC + KelvinOffset;

        double lnPws;

        if (dryBulbTemperatureC <= TriplePointWaterC)
        {
            lnPws =
                CalculateLogPressureBelowTriplePoint(
                    temperatureK);
        }
        else
        {
            lnPws =
                CalculateLogPressureAboveTriplePoint(
                    temperatureK);
        }

        return Math.Exp(lnPws);
    }

    // ============================================================
    // [1.4.3] FIN - Cálculo público de Pws en pascales
    // ============================================================


    // ============================================================
    // [1.4.4] INICIO - Ecuación ASHRAE bajo el punto triple
    // ============================================================

    private static double CalculateLogPressureBelowTriplePoint(
        double temperatureK)
    {
        double t =
            temperatureK;

        return
            (-5.6745359E+03 / t)
            + 6.3925247
            - (9.677843E-03 * t)
            + (6.2215701E-07 * Math.Pow(t, 2))
            + (2.0747825E-09 * Math.Pow(t, 3))
            - (9.484024E-13 * Math.Pow(t, 4))
            + (4.1635019 * Math.Log(t));
    }

    // ============================================================
    // [1.4.4] FIN - Ecuación ASHRAE bajo el punto triple
    // ============================================================


    // ============================================================
    // [1.4.5] INICIO - Ecuación ASHRAE sobre el punto triple
    // ============================================================

    private static double CalculateLogPressureAboveTriplePoint(
        double temperatureK)
    {
        double t =
            temperatureK;

        return
            (-5.8002206E+03 / t)
            + 1.3914993
            - (4.8640239E-02 * t)
            + (4.1764768E-05 * Math.Pow(t, 2))
            - (1.4452093E-08 * Math.Pow(t, 3))
            + (6.5459673 * Math.Log(t));
    }

    // ============================================================
    // [1.4.5] FIN - Ecuación ASHRAE sobre el punto triple
    // ============================================================


    // ============================================================
    // [1.4.6] INICIO - Conversión directa a kilopascales
    // ============================================================

    public static double GetPressureKPa(
        double dryBulbTemperatureC)
    {
        return
            GetPressurePa(dryBulbTemperatureC)
            / 1000.0;
    }

    // ============================================================
    // [1.4.6] FIN - Conversión directa a kilopascales
    // ============================================================


    // ============================================================
    // [1.4.7] INICIO - Validación de temperatura
    // ============================================================

    private static void ValidateTemperature(
        double dryBulbTemperatureC)
    {
        if (!double.IsFinite(dryBulbTemperatureC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbTemperatureC),
                "La temperatura debe ser un número válido.");
        }

        if (
            dryBulbTemperatureC < MinSupportedTemperatureC
            || dryBulbTemperatureC > MaxSupportedTemperatureC)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbTemperatureC),
                $"La temperatura debe estar entre " +
                $"{MinSupportedTemperatureC:0.##} °C y " +
                $"{MaxSupportedTemperatureC:0.##} °C.");
        }
    }

    // ============================================================
    // [1.4.7] FIN - Validación de temperatura
    // ============================================================


    // ============================================================
    // [1.4.8] INICIO - Cierre del módulo
    // ============================================================

}

// ============================================================
// [1.4.8] FIN - Cierre del módulo
// ============================================================