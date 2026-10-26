using System.Collections.Generic;

namespace FrioCalc.Psychrometrics.Core;

// ============================================================
// [1.3.1] INICIO - Catálogo de presiones predefinidas
// ============================================================

public static class PressurePresetCatalog
{
    public static PressurePreset Custom { get; } =
        new(
            "Personalizada...",
            double.NaN,
            true);

    public static IReadOnlyList<PressurePreset> All { get; } =
        new List<PressurePreset>
        {
            CreateAltitudePreset("Nivel del mar", 0),
            CreateAltitudePreset("Altitud estándar", 500),
            CreateAltitudePreset("Altitud estándar", 1000),
            CreateAltitudePreset("Altitud estándar", 1500),
            CreateAltitudePreset("Altitud estándar", 2000),
            CreateAltitudePreset("Altitud estándar", 2500),
            CreateAltitudePreset("Altitud estándar", 3000),
            Custom
        };

    // ============================================================
    // ============================================================
    // [1.3.2] INICIO - Creación de presets por altitud
    // ============================================================

    private static PressurePreset CreateAltitudePreset(
        string description,
        int altitudeMeters)
    {
        double pressureKPa =
            AtmosphericPressure.FromStandardAltitudeMeters(altitudeMeters);

        string name;

        if (altitudeMeters == 0)
        {
            name =
                $"Nivel del mar — {pressureKPa:0.###} kPa";
        }
        else
        {
            name =
                $"{altitudeMeters} m — {pressureKPa:0.###} kPa";
        }

        return new PressurePreset(
            name,
            pressureKPa);
    }

    // ============================================================
    // [1.3.2] FIN - Creación de presets por altitud
    // ============================================================       // ============================================================
}