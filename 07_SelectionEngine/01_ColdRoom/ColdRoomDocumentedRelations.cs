using System;

namespace FrioCalc.Psychrometrics.SelectionEngine.ColdRoom;

// ------------------------------ Sección 7.3.1 - Relaciones documentadas de cámara frigorífica ------------------------------

public static class ColdRoomDocumentedRelations
{
    public const double DocumentedMaximumOperatingHoursPerDay = 21.0;

    public const double HistoricalLightingPowerDensityWPerM2 = 8.0;

    public static double GetMeanEvaporatingTemperatureC(
        double roomTemperatureC,
        double meanTemperatureDifferenceK)
    {
        ValidateFinite(
            roomTemperatureC,
            nameof(roomTemperatureC));

        ValidateNonNegative(
            meanTemperatureDifferenceK,
            nameof(meanTemperatureDifferenceK));

        return
            roomTemperatureC
            - meanTemperatureDifferenceK;
    }

    public static double GetNominalSuperheatK(
        double dt1DewPointK)
    {
        ValidateNonNegative(
            dt1DewPointK,
            nameof(dt1DewPointK));

        return
            0.65
            * dt1DewPointK;
    }

    public static (double MinimumK, double MaximumK) GetDocumentedDt1Limits(
        bool electronicExpansion,
        StorageType storageType)
    {
        const double maximumK = 15.0;

        if (!electronicExpansion)
        {
            return
                (8.0, maximumK);
        }

        double minimumK =
            storageType switch
            {
                StorageType.Fresh => 4.0,
                StorageType.Packed => 7.5,
                StorageType.Frozen => 8.0,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(storageType))
            };

        return
            (minimumK, maximumK);
    }

    public static double GetSuggestedGoodsInletTemperatureC(
        double storageTemperatureC)
    {
        ValidateFinite(
            storageTemperatureC,
            nameof(storageTemperatureC));

        return
            storageTemperatureC
            + 5.0;
    }

    public static double GetEstimatedGoodsMassKg(
        double roomVolumeM3,
        double roomOccupancyPercent,
        double loadingDensityKgPerM3)
    {
        ValidateNonNegative(
            roomVolumeM3,
            nameof(roomVolumeM3));

        ValidatePercentage(
            roomOccupancyPercent,
            nameof(roomOccupancyPercent));

        ValidateNonNegative(
            loadingDensityKgPerM3,
            nameof(loadingDensityKgPerM3));

        return
            roomVolumeM3
            * (roomOccupancyPercent / 100.0)
            * loadingDensityKgPerM3;
    }

    public static double GetDailyChangedGoodsMassKg(
        double totalGoodsMassKg,
        double dailyChangePercent)
    {
        ValidateNonNegative(
            totalGoodsMassKg,
            nameof(totalGoodsMassKg));

        ValidatePercentage(
            dailyChangePercent,
            nameof(dailyChangePercent));

        return
            totalGoodsMassKg
            * (dailyChangePercent / 100.0);
    }

    public static double GetHistoricalLightingPowerW(
        double floorAreaM2)
    {
        ValidateNonNegative(
            floorAreaM2,
            nameof(floorAreaM2));

        return
            HistoricalLightingPowerDensityWPerM2
            * floorAreaM2;
    }

    private static void ValidateFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El valor debe ser finito.");
        }
    }

    private static void ValidateNonNegative(
        double value,
        string parameterName)
    {
        ValidateFinite(
            value,
            parameterName);

        if (value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El valor no puede ser negativo.");
        }
    }

    private static void ValidatePercentage(
        double value,
        string parameterName)
    {
        ValidateFinite(
            value,
            parameterName);

        if (value < 0.0
            || value > 100.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El porcentaje debe estar entre 0 y 100.");
        }
    }
}

// Fuente documental:
// Cold Room Calculation and Component Selection.pdf
// - T_evap_media = T_cámara - ΔT_media: p. 12.
// - Sobrecalentamiento nominal = 0.65 × DT1: p. 13.
// - Límites DT1: p. 12.
// - T_entrada sugerida = T_almacenamiento + 5 K: p. 8.
// - Masa desde volumen/ocupación/densidad y renovación diaria: método descrito p. 7.
// - 8 W/m²: ejemplo histórico documentado, no equivalencia universal con LED.
// - 21 h/día: recomendación documental, no límite físico.
//
// No se implementan aquí infiltración, transmisión total, personas,
// mercancía completa ni capacidad frigorífica total porque la recuperación
// no contiene todavía ecuaciones completas verificadas para esas partidas.

// ------------------------------ Fin de sección 7.3.1 ------------------------------
