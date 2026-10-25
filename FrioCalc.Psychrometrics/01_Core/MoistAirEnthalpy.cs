using System;

namespace FrioCalc.Psychrometrics.Core;

// ------------------------------ Sección 1.6.1 - Módulo de entalpía del aire húmedo ------------------------------

public static class MoistAirEnthalpy
{

    // ------------------------------ Fin de sección 1.6.1 ------------------------------


    // ------------------------------ Sección 1.6.2 - Constantes SI de la ecuación ASHRAE ------------------------------

    public const double DryAirSpecificHeatKJPerKgK =
        1.006;

    public const double WaterVaporReferenceEnthalpyKJPerKg =
        2501.0;

    public const double WaterVaporSpecificHeatKJPerKgK =
        1.86;

    // ------------------------------ Fin de sección 1.6.2 ------------------------------


    // ------------------------------ Sección 1.6.3 - Cálculo de entalpía ------------------------------

    public static double FromDryBulbAndHumidityRatio(
        double dryBulbC,
        double humidityRatioKgPerKg)
    {
        if (!double.IsFinite(dryBulbC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbC),
                "La temperatura de bulbo seco debe ser un número válido.");
        }

        if (!double.IsFinite(humidityRatioKgPerKg)
            || humidityRatioKgPerKg < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(humidityRatioKgPerKg),
                "La razón de humedad debe ser válida y no negativa.");
        }

        return
            (DryAirSpecificHeatKJPerKgK * dryBulbC)
            +
            humidityRatioKgPerKg
            *
            (
                WaterVaporReferenceEnthalpyKJPerKg
                +
                WaterVaporSpecificHeatKJPerKgK
                * dryBulbC
            );
    }

    // ------------------------------ Fin de sección 1.6.3 ------------------------------


    // ------------------------------ Sección 1.6.4 - Despeje de W para una entalpía constante ------------------------------

    public static double HumidityRatioFromEnthalpyAndDryBulb(
        double enthalpyKJPerKgDryAir,
        double dryBulbC)
    {
        if (!double.IsFinite(enthalpyKJPerKgDryAir))
        {
            throw new ArgumentOutOfRangeException(
                nameof(enthalpyKJPerKgDryAir),
                "La entalpía debe ser un número válido.");
        }

        if (!double.IsFinite(dryBulbC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbC),
                "La temperatura de bulbo seco debe ser un número válido.");
        }

        double denominator =
            WaterVaporReferenceEnthalpyKJPerKg
            +
            WaterVaporSpecificHeatKJPerKgK
            * dryBulbC;

        if (Math.Abs(denominator) < 1e-12)
        {
            throw new InvalidOperationException(
                "No es posible despejar la razón de humedad para esta temperatura.");
        }

        return
            (
                enthalpyKJPerKgDryAir
                -
                DryAirSpecificHeatKJPerKgK
                * dryBulbC
            )
            / denominator;
    }

    // ------------------------------ Fin de sección 1.6.4 ------------------------------


    // ------------------------------ Sección 1.6.5 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 1.6.5 ------------------------------