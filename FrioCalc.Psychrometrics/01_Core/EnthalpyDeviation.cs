using System;

namespace FrioCalc.Psychrometrics.Core;

// ------------------------------ Sección 1.10.1 - Definición del módulo de desviación de entalpía ------------------------------

public static class EnthalpyDeviation
{

    // ------------------------------ Fin de sección 1.10.1 ------------------------------


    // ------------------------------ Sección 1.10.2 - Desviación de entalpía desde el estado del aire ------------------------------

    public static double FromDryBulbAndHumidityRatio(
        double dryBulbC,
        double humidityRatioKgPerKg,
        double totalPressureKPa)
    {
        ValidateInputs(
            dryBulbC,
            humidityRatioKgPerKg,
            totalPressureKPa);

        double saturationPressureAtDryBulbKPa =
            SaturationVaporPressure.GetPressureKPa(
                dryBulbC);

        if (saturationPressureAtDryBulbKPa
            < totalPressureKPa)
        {
            double saturationHumidityRatioAtDryBulb =
                SaturationHumidityRatio.FromPressuresKPa(
                    totalPressureKPa,
                    saturationPressureAtDryBulbKPa);

            if (humidityRatioKgPerKg
                > saturationHumidityRatioAtDryBulb + 1e-9)
            {
                return double.NaN;
            }
        }

        double wetBulbC =
            GetWetBulbFromDryBulbAndHumidityRatio(
                dryBulbC,
                humidityRatioKgPerKg,
                totalPressureKPa);

        if (!double.IsFinite(wetBulbC))
        {
            return double.NaN;
        }

        double saturationPressureAtWetBulbKPa =
            SaturationVaporPressure.GetPressureKPa(
                wetBulbC);

        if (saturationPressureAtWetBulbKPa
            >= totalPressureKPa)
        {
            return double.NaN;
        }

        double saturatedHumidityRatioAtWetBulb =
            SaturationHumidityRatio.FromPressuresKPa(
                totalPressureKPa,
                saturationPressureAtWetBulbKPa);

        double actualEnthalpy =
            MoistAirEnthalpy.FromDryBulbAndHumidityRatio(
                dryBulbC,
                humidityRatioKgPerKg);

        double saturatedEnthalpyAtWetBulb =
            MoistAirEnthalpy.FromDryBulbAndHumidityRatio(
                wetBulbC,
                saturatedHumidityRatioAtWetBulb);

        return
            actualEnthalpy
            - saturatedEnthalpyAtWetBulb;
    }

    // Fuente conceptual:
    // Carta psicrométrica Carrier/VALCON: desviación de entalpía.
    // La desviación corrige la lectura obtenida al prolongar una línea de
    // bulbo húmedo hasta la escala de entalpía. Es cero en saturación.

    // ------------------------------ Fin de sección 1.10.2 ------------------------------


    // ------------------------------ Sección 1.10.3 - Inversión numérica de bulbo húmedo ------------------------------

    private static double GetWetBulbFromDryBulbAndHumidityRatio(
        double dryBulbC,
        double humidityRatioKgPerKg,
        double totalPressureKPa)
    {
        double maximumWetBulbC =
            Math.Min(
                dryBulbC,
                GetMaximumWetBulbBelowPressure(
                    totalPressureKPa));

        double minimumWetBulbC =
            Math.Min(
                maximumWetBulbC,
                Math.Max(
                    SaturationVaporPressure.MinSupportedTemperatureC,
                    -100.0));

        double humidityAtMaximum =
            WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                dryBulbC,
                maximumWetBulbC,
                totalPressureKPa);

        if (!double.IsFinite(humidityAtMaximum))
        {
            return double.NaN;
        }

        if (humidityRatioKgPerKg
            >= humidityAtMaximum - 1e-12)
        {
            return maximumWetBulbC;
        }

        double lower =
            minimumWetBulbC;

        double upper =
            maximumWetBulbC;

        for (int iteration = 0;
             iteration < 60;
             iteration++)
        {
            double middle =
                (lower + upper) / 2.0;

            double calculatedHumidityRatio =
                WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                    dryBulbC,
                    middle,
                    totalPressureKPa);

            if (!double.IsFinite(calculatedHumidityRatio))
            {
                upper =
                    middle;

                continue;
            }

            if (calculatedHumidityRatio
                < humidityRatioKgPerKg)
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

        return
            (lower + upper) / 2.0;
    }

    // ------------------------------ Fin de sección 1.10.3 ------------------------------


    // ------------------------------ Sección 1.10.4 - Máximo bulbo húmedo permitido por presión ------------------------------

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
             iteration < 70;
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

    // ------------------------------ Fin de sección 1.10.4 ------------------------------


    // ------------------------------ Sección 1.10.5 - Validación ------------------------------

    private static void ValidateInputs(
        double dryBulbC,
        double humidityRatioKgPerKg,
        double totalPressureKPa)
    {
        if (!double.IsFinite(dryBulbC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dryBulbC),
                "La temperatura de bulbo seco debe ser válida.");
        }

        if (!double.IsFinite(humidityRatioKgPerKg)
            || humidityRatioKgPerKg < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(humidityRatioKgPerKg),
                "La humedad absoluta debe ser válida y no negativa.");
        }

        if (!double.IsFinite(totalPressureKPa)
            || totalPressureKPa <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalPressureKPa),
                "La presión absoluta debe ser mayor que cero.");
        }
    }

    // ------------------------------ Fin de sección 1.10.5 ------------------------------


    // ------------------------------ Sección 1.10.6 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 1.10.6 ------------------------------
