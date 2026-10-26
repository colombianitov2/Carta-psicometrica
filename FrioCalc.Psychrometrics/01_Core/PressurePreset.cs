namespace FrioCalc.Psychrometrics.Core;

// ============================================================
// [1.2.1] INICIO - Modelo de preset de presión
// ============================================================

public sealed record PressurePreset(
    string Name,
    double PressureKPa,
    bool IsCustom = false)
{
    public override string ToString()
    {
        return Name;
    }
}

// ============================================================
// [1.2.1] FIN - Modelo de preset de presión
// ============================================================