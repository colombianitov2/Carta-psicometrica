using System;

namespace FrioCalc.Psychrometrics.Core;

// ------------------------------ Sección 1.9.1 - Definición del módulo de volumen específico ------------------------------

public static class MoistAirSpecificVolume
{

    // ------------------------------ Fin de sección 1.9.1 ------------------------------


    // ------------------------------ Sección 1.9.2 - Constantes SI ------------------------------

    public const double DryAirGasConstantJPerKgK =
        287.042;

    public const double WaterVaporVolumeFactor =
        1.607858;

    public const double KelvinOffset =
        273.15;

    // ------------------------------ Fin de sección 1.9.2 ------------------------------


    // ------------------------------ Sección 1.9.3 - Cálculo de volumen específico ------------------------------

    public static double FromDryBulbAndHumidityRatio(
        double dryBulbC,
        double humidityRatioKgPerKg,
        double totalPressureKPa)
    {
        ValidateInputs(
            dryBulbC,
            humidityRatioKgPerKg,
            totalPressureKPa);

        double temperatureK =
            dryBulbC + KelvinOffset;

        double pressurePa =
            totalPressureKPa * 1000.0;

        return
            DryAirGasConstantJPerKgK
            * temperatureK
            * (1.0
               + WaterVaporVolumeFactor
               * humidityRatioKgPerKg)
            / pressurePa;
    }

    // ------------------------------ Fin de sección 1.9.3 ------------------------------


    // ------------------------------ Sección 1.9.4 - Despeje de humedad absoluta para volumen específico constante ------------------------------

    public static double HumidityRatioFromDryBulbAndSpecificVolume(
        double dryBulbC,
        double specificVolumeM3PerKgDryAir,
        double totalPressureKPa)
    {
        if (!double.IsFinite(specificVolumeM3PerKgDryAir)
            || specificVolumeM3PerKgDryAir <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specificVolumeM3PerKgDryAir),
                "El volumen específico debe ser mayor que cero.");
        }

        ValidateInputs(
            dryBulbC,
            0.0,
            totalPressureKPa);

        double temperatureK =
            dryBulbC + KelvinOffset;

        double pressurePa =
            totalPressureKPa * 1000.0;

        return
            (
                specificVolumeM3PerKgDryAir
                * pressurePa
                / (
                    DryAirGasConstantJPerKgK
                    * temperatureK)
                - 1.0
            )
            / WaterVaporVolumeFactor;
    }

    // Fuente de la ecuación:
    // ASHRAE Handbook—Fundamentals, ecuación de volumen específico del aire húmedo.
    // PsychroLib: GetMoistAirVolume.

    // ------------------------------ Fin de sección 1.9.4 ------------------------------


    // ------------------------------ Sección 1.9.5 - Validación ------------------------------

    private static void ValidateInputs(
        double dryBulbC,
        double humidityRatioKgPerKg,
        double totalPressureKPa)
    {
        if (!double.IsFinite(dryBulbC)
            || dryBulbC + KelvinOffset <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbC),
                "La temperatura debe ser válida y superior al cero absoluto.");
        }

        if (!double.IsFinite(humidityRatioKgPerKg)
            || humidityRatioKgPerKg < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(humidityRatioKgPerKg),
                "La humedad absoluta no puede ser negativa.");
        }

        if (!double.IsFinite(totalPressureKPa)
            || totalPressureKPa <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalPressureKPa),
                "La presión absoluta debe ser mayor que cero.");
        }
    }

    // ------------------------------ Fin de sección 1.9.5 ------------------------------


    // ------------------------------ Sección 1.9.6 - Cierre del módulo ------------------------------

}

// ------------------------------ Fin de sección 1.9.6 ------------------------------