using System;

namespace FrioCalc.Psychrometrics.Core;

// ------------------------------ Sección 1.7.1 - Definición del módulo de humedad relativa ------------------------------

public static class RelativeHumidity
{

    // ------------------------------ Fin de sección 1.7.1 ------------------------------


    // ------------------------------ Sección 1.7.2 - Humedad absoluta desde humedad relativa ------------------------------

    public static double HumidityRatioFromDryBulbAndRelativeHumidity(
        double dryBulbC,
        double relativeHumidityFraction,
        double totalPressureKPa)
    {
        if (!double.IsFinite(dryBulbC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbC),
                "La temperatura de bulbo seco debe ser un número válido.");
        }

        if (!double.IsFinite(relativeHumidityFraction)
            || relativeHumidityFraction < 0.0
            || relativeHumidityFraction > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relativeHumidityFraction),
                "La humedad relativa debe estar entre 0 y 1.");
        }

        if (!double.IsFinite(totalPressureKPa)
            || totalPressureKPa <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalPressureKPa),
                "La presión absoluta debe ser mayor que cero.");
        }

        double saturationVaporPressureKPa =
            SaturationVaporPressure.GetPressureKPa(
                dryBulbC);

        double vaporPressureKPa =
            relativeHumidityFraction
            * saturationVaporPressureKPa;

        if (vaporPressureKPa >= totalPressureKPa)
        {
            return double.NaN;
        }

        return
            SaturationHumidityRatio
                .WaterVaporToDryAirMolecularWeightRatio
            * vaporPressureKPa
            / (totalPressureKPa - vaporPressureKPa);
    }

    // Fuente de la ecuación:
    // ASHRAE Handbook—Fundamentals, capítulo de psicrometría.
    // PsychroLib: GetHumRatioFromRelHum.

    // ------------------------------ Fin de sección 1.7.2 ------------------------------


    // ------------------------------ Sección 1.7.3 - Cierre del módulo ------------------------------

}

// ------------------------------ Fin de sección 1.7.3 ------------------------------