using System;

namespace FrioCalc.Psychrometrics.Core;

// ------------------------------ Sección 1.8.1 - Definición del módulo de bulbo húmedo ------------------------------

public static class WetBulbHumidityRatio
{

    // ------------------------------ Fin de sección 1.8.1 ------------------------------


    // ------------------------------ Sección 1.8.2 - Humedad absoluta desde bulbo seco y bulbo húmedo ------------------------------

    public static double FromDryBulbAndWetBulb(
        double dryBulbC,
        double wetBulbC,
        double totalPressureKPa)
    {
        if (!double.IsFinite(dryBulbC)
            || !double.IsFinite(wetBulbC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbC),
                "Las temperaturas deben ser números válidos.");
        }

        if (wetBulbC > dryBulbC)
        {
            throw new ArgumentOutOfRangeException(
                nameof(wetBulbC),
                "La temperatura de bulbo húmedo no puede superar la de bulbo seco.");
        }

        if (!double.IsFinite(totalPressureKPa)
            || totalPressureKPa <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalPressureKPa),
                "La presión absoluta debe ser mayor que cero.");
        }

        double saturationVaporPressureAtWetBulbKPa =
            SaturationVaporPressure.GetPressureKPa(
                wetBulbC);

        if (saturationVaporPressureAtWetBulbKPa
            >= totalPressureKPa)
        {
            return double.NaN;
        }

        double saturatedHumidityRatioAtWetBulb =
            SaturationHumidityRatio.FromPressuresKPa(
                totalPressureKPa,
                saturationVaporPressureAtWetBulbKPa);

        double numerator;
        double denominator;

        if (wetBulbC >= 0.0)
        {
            numerator =
                (2501.0 - 2.326 * wetBulbC)
                * saturatedHumidityRatioAtWetBulb
                - 1.006 * (dryBulbC - wetBulbC);

            denominator =
                2501.0
                + 1.86 * dryBulbC
                - 4.186 * wetBulbC;
        }
        else
        {
            numerator =
                (2830.0 - 0.24 * wetBulbC)
                * saturatedHumidityRatioAtWetBulb
                - 1.006 * (dryBulbC - wetBulbC);

            denominator =
                2830.0
                + 1.86 * dryBulbC
                - 2.1 * wetBulbC;
        }

        if (Math.Abs(denominator) < 1e-12)
        {
            return double.NaN;
        }

        return numerator / denominator;
    }

    // Fuente de las ecuaciones:
    // ASHRAE Handbook—Fundamentals, capítulo de psicrometría.
    // PsychroLib: GetHumRatioFromTWetBulb.

    // ------------------------------ Fin de sección 1.8.2 ------------------------------


    // ------------------------------ Sección 1.8.3 - Cierre del módulo ------------------------------

}

// ------------------------------ Fin de sección 1.8.3 ------------------------------