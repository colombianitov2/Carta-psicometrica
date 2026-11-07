using System.Collections.Generic;

namespace FrioCalc.Psychrometrics.Processes;

// [4.1] Secuencia ordenada de puntos; el color se hereda del primer estado.
public enum ProcessType
{
    Automatic, Sensible, HeatingHumidification, CoolingDehumidification,
    SteamHumidification, EvaporativeHumidification, Desiccant,
    AdiabaticMixing, Bypass, Branching
}

public sealed record PsychrometricProcess(
    int Id,
    string Name,
    string ColorHex,
    ProcessType Type,
    IReadOnlyList<int> PointIds,
    double? DryAirMassFlowKgS,
    double? SaturationEffectiveness = null,
    double? BypassFactor = null,
    string Method = "Balances de masa y energía",
    bool Visible = true,
    string Report = "",
    double? CondensateTemperatureC = null,
    double? BranchFractionA = null);
