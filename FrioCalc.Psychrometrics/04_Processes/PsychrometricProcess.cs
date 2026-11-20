using System.Collections.Generic;

namespace FrioCalc.Psychrometrics.Processes;

// [4.1] Secuencia ordenada de puntos; el color se hereda del primer estado.
public enum ProcessType
{
    Automatic, Sensible, HeatingHumidification, CoolingDehumidification,
    SteamHumidification, EvaporativeHumidification, Desiccant,
    AdiabaticMixing, Bypass, Branching
}

public sealed record ProcessSegmentSetting(int FromId,int ToId, ProcessType Type,
    double? DryAirMassFlowKgS = null, double? SensibleHeatFactor = null, double? CondensateTemperatureC = null,
    int Occurrence = 0)
{
    // [4.1.1] Cada aparición de la misma conexión es un tramo independiente.
    // Occurrence cero identifica la primera y conserva compatibilidad con las sesiones antiguas.
    public static (int From,int To,int Occurrence) Key(IReadOnlyList<int> ids,int index)
    {
        int from=ids[index],to=ids[index+1],occurrence=0;
        for(int i=0;i<index;i++)if(ids[i]==from && ids[i+1]==to)occurrence++;
        return (from,to,occurrence);
    }

    public static ProcessSegmentSetting? Find(IReadOnlyList<ProcessSegmentSetting>? settings,IReadOnlyList<int> ids,int index)
    {
        if(settings is null)return null;
        var key=Key(ids,index);
        foreach(var setting in settings)
            if(setting.FromId==key.From && setting.ToId==key.To && setting.Occurrence==key.Occurrence)return setting;
        return null;
    }
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
    double? BranchFractionA = null,
    // Solo para puntos calculados por el proceso; permite editar sin dejar huérfanos.
    int? GeneratedPointId = null,
    double? SteamAddedGramsPerKg = null,
    double? SteamEnthalpyKJPerKg = null,
    double? ApparatusDewPointC = null,
    double? DesiccantRemovedGramsPerKg = null,
    double? DesiccantOutletTemperatureC = null,
    IReadOnlyList<double>? MixingInletFlowsM3H = null,
    IReadOnlyList<ProcessSegmentSetting>? SegmentSettings = null);
