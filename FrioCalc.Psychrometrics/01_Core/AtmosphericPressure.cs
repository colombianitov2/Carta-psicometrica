using System;

namespace FrioCalc.Psychrometrics.Core;

// ============================================================
// [1.1.1] INICIO - Definición del módulo de presión atmosférica
// ============================================================

public static class AtmosphericPressure
{

    // ============================================================
    // [1.1.1] FIN - Definición del módulo de presión atmosférica
    // ============================================================


    // ============================================================
    // [1.1.2] INICIO - Constantes y límites iniciales
    // ============================================================

    public const double StandardSeaLevelKPa = 101.325;

    // Rango operativo inicial de FrioCalc Psychrometrics.
    // No representa los límites físicos absolutos de la atmósfera.
    public const double MinSupportedKPa = 10.0;
    public const double MaxSupportedKPa = 120.0;

    // ============================================================
    // [1.1.2] FIN - Constantes y límites iniciales
    // ============================================================


    // ============================================================
    // [1.1.3] INICIO - Validación de presión
    // ============================================================

    public static bool IsSupported(double pressureKPa)
    {
        return double.IsFinite(pressureKPa)
            && pressureKPa >= MinSupportedKPa
            && pressureKPa <= MaxSupportedKPa;
    }

    public static string? GetValidationError(double pressureKPa)
    {
        if (!double.IsFinite(pressureKPa))
        {
            return "La presión debe ser un número válido.";
        }

        if (pressureKPa < MinSupportedKPa)
        {
            return $"La presión debe ser igual o mayor que {MinSupportedKPa:0.###} kPa abs.";
        }

        if (pressureKPa > MaxSupportedKPa)
        {
            return $"La presión debe ser igual o menor que {MaxSupportedKPa:0.###} kPa abs.";
        }

        return null;
    }

    // ============================================================
    // [1.1.3] FIN - Validación de presión
    // ============================================================


    // ============================================================
    // [1.1.4] INICIO - Presión de atmósfera estándar por altitud
    // ============================================================

    public static double FromStandardAltitudeMeters(double altitudeMeters)
    {
        if (!double.IsFinite(altitudeMeters))
        {
            throw new ArgumentOutOfRangeException(
                nameof(altitudeMeters),
                "La altitud debe ser un número válido.");
        }

        // Limitamos esta función inicial a altitudes en las que
        // utilizaremos los presets de la carta.
        if (altitudeMeters < -500.0 || altitudeMeters > 5000.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(altitudeMeters),
                "La función estándar inicial admite altitudes entre -500 y 5000 m.");
        }

        return StandardSeaLevelKPa
            * Math.Pow(
                1.0 - (2.25577e-5 * altitudeMeters),
                5.2559);
    }

    // ============================================================
    // [1.1.4] FIN - Presión de atmósfera estándar por altitud
    // ============================================================


    // ============================================================
    // [1.1.5] INICIO - Cierre del módulo
    // ============================================================

}

// ============================================================
// [1.1.5] FIN - Cierre del módulo
// ============================================================