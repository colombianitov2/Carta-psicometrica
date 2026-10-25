using System;

namespace FrioCalc.Psychrometrics.Core;

// ============================================================
// [1.5.1] INICIO - Definición del módulo de razón de humedad
//                  de saturación
// ============================================================

public static class SaturationHumidityRatio
{

    // ============================================================
    // [1.5.1] FIN - Definición del módulo de razón de humedad
    //               de saturación
    // ============================================================


    // ============================================================
    // [1.5.2] INICIO - Constante psicrométrica
    // ============================================================

    public const double WaterVaporToDryAirMolecularWeightRatio
        = 0.621945;

    // ============================================================
    // [1.5.2] FIN - Constante psicrométrica
    // ============================================================


    // ============================================================
    // [1.5.3] INICIO - Cálculo de Ws a partir de presiones
    // ============================================================

    public static double FromPressuresKPa(
        double totalPressureKPa,
        double saturationVaporPressureKPa)
    {
        if (!double.IsFinite(totalPressureKPa))
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalPressureKPa),
                "La presión total debe ser un número válido.");
        }

        if (!double.IsFinite(saturationVaporPressureKPa))
        {
            throw new ArgumentOutOfRangeException(
                nameof(saturationVaporPressureKPa),
                "La presión de vapor de saturación debe ser un número válido.");
        }

        if (totalPressureKPa <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalPressureKPa),
                "La presión total debe ser mayor que cero.");
        }

        if (saturationVaporPressureKPa < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(saturationVaporPressureKPa),
                "La presión de vapor de saturación no puede ser negativa.");
        }

        if (saturationVaporPressureKPa >= totalPressureKPa)
        {
            throw new ArgumentOutOfRangeException(
                nameof(saturationVaporPressureKPa),
                "Para aire húmedo, la presión de vapor de saturación debe ser menor que la presión total.");
        }

        return WaterVaporToDryAirMolecularWeightRatio
            * saturationVaporPressureKPa
            / (totalPressureKPa - saturationVaporPressureKPa);
    }

    // ============================================================
    // [1.5.3] FIN - Cálculo de Ws a partir de presiones
    // ============================================================


    // ============================================================
    // [1.5.4] INICIO - Conversión a gramos por kilogramo
    // ============================================================

    public static double ToGramsPerKilogram(
        double humidityRatioKgPerKg)
    {
        if (!double.IsFinite(humidityRatioKgPerKg)
            || humidityRatioKgPerKg < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(humidityRatioKgPerKg),
                "La razón de humedad debe ser un valor válido y no negativo.");
        }

        return humidityRatioKgPerKg * 1000.0;
    }

    // ============================================================
    // [1.5.4] FIN - Conversión a gramos por kilogramo
    // ============================================================


    // ============================================================
    // [1.5.5] INICIO - Cierre del módulo
    // ============================================================

}

// ============================================================
// [1.5.5] FIN - Cierre del módulo
// ============================================================