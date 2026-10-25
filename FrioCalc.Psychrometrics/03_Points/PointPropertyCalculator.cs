using System;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Points;

// ------------------------------ Sección 3.2.1 - Cálculo de propiedades del punto ------------------------------

public static class PointPropertyCalculator
{

    // ------------------------------ Fin de sección 3.2.1 ------------------------------


    // ------------------------------ Sección 3.2.2 - Construcción validada del punto ------------------------------

    public static bool TryCreatePoint(
        int id,
        string name,
        string colorHex,
        double pressureKPa,
        double dryBulbC,
        double humidityRatioKgPerKg,
        out ChartPoint? point)
    {
        point =
            null;

        if (!double.IsFinite(pressureKPa)
            || pressureKPa <= 0.0
            || !double.IsFinite(dryBulbC)
            || !double.IsFinite(humidityRatioKgPerKg)
            || humidityRatioKgPerKg < 0.0)
        {
            return false;
        }

        double vaporPressureKPa =
            GetVaporPressureFromHumidityRatio(
                humidityRatioKgPerKg,
                pressureKPa);

        double saturationVaporPressureKPa =
            SaturationVaporPressure.GetPressureKPa(
                dryBulbC);

        if (!double.IsFinite(vaporPressureKPa)
            || vaporPressureKPa < 0.0
            || vaporPressureKPa >= pressureKPa)
        {
            return false;
        }

        double relativeHumidityFraction =
            vaporPressureKPa
            / saturationVaporPressureKPa;

        if (!double.IsFinite(relativeHumidityFraction)
            || relativeHumidityFraction < -1e-9
            || relativeHumidityFraction > 1.0 + 1e-6)
        {
            return false;
        }

        relativeHumidityFraction =
            Math.Clamp(
                relativeHumidityFraction,
                0.0,
                1.0);

        double wetBulbC =
            GetWetBulbFromDryBulbAndHumidityRatio(
                dryBulbC,
                humidityRatioKgPerKg,
                pressureKPa);

        double dewPointC =
            GetDewPointFromVaporPressure(
                vaporPressureKPa,
                dryBulbC);

        double enthalpyKJPerKgDryAir =
            MoistAirEnthalpy.FromDryBulbAndHumidityRatio(
                dryBulbC,
                humidityRatioKgPerKg);

        double specificVolumeM3PerKgDryAir =
            MoistAirSpecificVolume.FromDryBulbAndHumidityRatio(
                dryBulbC,
                humidityRatioKgPerKg,
                pressureKPa);

        point =
            new ChartPoint(
                id,
                name,
                colorHex,
                pressureKPa,
                dryBulbC,
                humidityRatioKgPerKg,
                relativeHumidityFraction * 100.0,
                wetBulbC,
                dewPointC,
                enthalpyKJPerKgDryAir,
                specificVolumeM3PerKgDryAir);

        return true;
    }

    // ------------------------------ Fin de sección 3.2.2 ------------------------------


    // ------------------------------ Sección 3.2.3 - Presión parcial del vapor ------------------------------

    private static double GetVaporPressureFromHumidityRatio(
        double humidityRatioKgPerKg,
        double pressureKPa)
    {
        return
            pressureKPa
            * humidityRatioKgPerKg
            / (
                SaturationHumidityRatio
                    .WaterVaporToDryAirMolecularWeightRatio
                + humidityRatioKgPerKg);
    }

    // ------------------------------ Fin de sección 3.2.3 ------------------------------


    // ------------------------------ Sección 3.2.4 - Temperatura de punto de rocío ------------------------------

    private static double GetDewPointFromVaporPressure(
        double vaporPressureKPa,
        double dryBulbC)
    {
        if (!double.IsFinite(vaporPressureKPa)
            || vaporPressureKPa <= 0.0)
        {
            return double.NaN;
        }

        double minimumC =
            SaturationVaporPressure.MinSupportedTemperatureC;

        double maximumC =
            Math.Min(
                dryBulbC,
                SaturationVaporPressure.MaxSupportedTemperatureC);

        double minimumPressureKPa =
            SaturationVaporPressure.GetPressureKPa(
                minimumC);

        double maximumPressureKPa =
            SaturationVaporPressure.GetPressureKPa(
                maximumC);

        if (vaporPressureKPa < minimumPressureKPa
            || vaporPressureKPa > maximumPressureKPa)
        {
            return double.NaN;
        }

        double lower =
            minimumC;

        double upper =
            maximumC;

        for (int iteration = 0;
             iteration < 70;
             iteration++)
        {
            double middle =
                (lower + upper) / 2.0;

            double middlePressureKPa =
                SaturationVaporPressure.GetPressureKPa(
                    middle);

            if (middlePressureKPa
                < vaporPressureKPa)
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

    // ------------------------------ Fin de sección 3.2.4 ------------------------------


    // ------------------------------ Sección 3.2.5 - Temperatura de bulbo húmedo ------------------------------

    private static double GetWetBulbFromDryBulbAndHumidityRatio(
        double dryBulbC,
        double humidityRatioKgPerKg,
        double pressureKPa)
    {
        double maximumWetBulbC =
            Math.Min(
                dryBulbC,
                GetMaximumWetBulbBelowPressure(
                    pressureKPa));

        double minimumWetBulbC =
            SaturationVaporPressure.MinSupportedTemperatureC;

        double lowerHumidityRatio =
            WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                dryBulbC,
                minimumWetBulbC,
                pressureKPa);

        double upperHumidityRatio =
            WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                dryBulbC,
                maximumWetBulbC,
                pressureKPa);

        if (!double.IsFinite(lowerHumidityRatio)
            || !double.IsFinite(upperHumidityRatio)
            || humidityRatioKgPerKg < lowerHumidityRatio - 1e-9
            || humidityRatioKgPerKg > upperHumidityRatio + 1e-9)
        {
            return double.NaN;
        }

        double lower =
            minimumWetBulbC;

        double upper =
            maximumWetBulbC;

        for (int iteration = 0;
             iteration < 70;
             iteration++)
        {
            double middle =
                (lower + upper) / 2.0;

            double middleHumidityRatio =
                WetBulbHumidityRatio.FromDryBulbAndWetBulb(
                    dryBulbC,
                    middle,
                    pressureKPa);

            if (!double.IsFinite(middleHumidityRatio))
            {
                return double.NaN;
            }

            if (middleHumidityRatio
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

    private static double GetMaximumWetBulbBelowPressure(
        double pressureKPa)
    {
        double maximumSupportedC =
            SaturationVaporPressure.MaxSupportedTemperatureC;

        if (SaturationVaporPressure.GetPressureKPa(
                maximumSupportedC)
            < pressureKPa)
        {
            return maximumSupportedC;
        }

        double lower =
            SaturationVaporPressure.MinSupportedTemperatureC;

        double upper =
            maximumSupportedC;

        double targetPressureKPa =
            pressureKPa * 0.999999;

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

    // ------------------------------ Fin de sección 3.2.5 ------------------------------


    // ------------------------------ Sección 3.2.6 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 3.2.6 ------------------------------
