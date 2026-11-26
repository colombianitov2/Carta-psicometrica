using System;
using System.Collections.Generic;
using System.Globalization;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Processes;

// [4.8.2] Presentación compartida de resultados en la carta, Procesos y Word.
public static class ProcessResultPresentation
{
    public static string MethodName(ProcessType type)=>type switch
    {
        ProcessType.Automatic=>"Automático · diferencias entre estados",
        ProcessType.Sensible=>"Calefacción o enfriamiento sensible",
        ProcessType.HeatingHumidification=>"Calentamiento y humidificación",
        ProcessType.CoolingDehumidification=>"Enfriamiento y deshumidificación",
        ProcessType.SteamHumidification=>"Humidificación por vapor",
        ProcessType.EvaporativeHumidification=>"Humidificación evaporativa",
        ProcessType.Desiccant=>"Deshumidificación con desecante",
        ProcessType.AdiabaticMixing=>"Mezcla adiabática",
        ProcessType.Bypass=>"Factor de bypass / ADP",
        ProcessType.Branching=>"Divisor con dos ramas y estados conocidos",
        _=>type.ToString()
    };

    public static IReadOnlyList<(string Label,string Value)> SegmentRows(
        PsychrometricProcess process,ProcessSegmentSetting? setting,ChartPoint from,ChartPoint to,
        ProcessResult r,UnitSystem units)
    {
        bool ip=units==UnitSystem.IP;
        string h=EducationalUnits.EnthalpyUnit(units),w=EducationalUnits.HumidityUnit(units),
            t=EducationalUnits.TemperatureUnit(units),m=ip?"lb/h":"kg/s",q=ip?"Btu/h":"kW";
        string F(double value)=>double.IsFinite(value)?value.ToString("0.###",CultureInfo.CurrentCulture):"—";
        string H(double value)=>F(EducationalUnits.H(value,units));
        string W(double value)=>F(EducationalUnits.W(value,units));
        string M(double? value)=>value is double n?(n*(ip?7936.64143865559:1)).ToString("0.######",CultureInfo.CurrentCulture):"Falta caudal";
        string Q(double? value)=>value is double n?F(n*(ip?3412.14163312794:1)):"Falta caudal";
        var method=setting?.Type??process.Type;
        double dh=r.DeltaEnthalpyKJPerKgDryAir,dw=r.DeltaHumidityRatioKgPerKg;
        double? flow=setting?.DryAirMassFlowKgS??process.DryAirMassFlowKgS;
        var rows=new List<(string Label,string Value)>
        {
            ("Método seleccionado",MethodName(method)),
            ("Transformación entre estados",ProcessCompatibility.Direction(from,to)),
            ($"Variación de temperatura ΔT ({t})",F(EducationalUnits.DT(r.DeltaTemperatureC,units))),
            ($"Variación de humedad absoluta ΔW ({w})",W(dw)),
            ($"Variación de entalpía del aire Δh ({h})",H(dh)),
            (dh<0?$"Disminución de entalpía específica del aire ({h})":$"Aumento de entalpía específica del aire ({h})",H(Math.Abs(dh))),
            ($"Componente sensible específica ({h})",H(r.SensibleSpecificKJPerKgDryAir)),
            ($"Componente latente específica ({h})",H(r.LatentSpecificKJPerKgDryAir)),
            (dw<0?$"Agua retirada por masa de aire seco ({w})":$"Agua añadida por masa de aire seco ({w})",W(Math.Abs(dw))),
            ($"Caudal másico de aire seco ({m})",flow.HasValue?M(flow):"No indicado"),
            ($"Potencia por cambio de entalpía del aire ({q})",Q(r.ThermalPowerKW)),
            ($"Potencia sensible ({q})",Q(r.SensiblePowerKW)),
            ($"Potencia latente ({q})",Q(r.LatentPowerKW)),
            ($"Flujo de agua añadido/retirado ({m})",M(r.MoistureFlowKgS)),
            ("FCS calculado",r.SensibleHeatFactor is double f?F(f):"No definido"),
            ("FCS indicado",setting?.SensibleHeatFactor is double target?F(target):"No indicado")
        };
        if(dh<0)rows.Insert(11,($"Potencia extraída del aire por descenso de h ({q})",Q(-r.ThermalPowerKW)));
        if(r.CondensateSpecificKgPerKgDryAir is double condensate)
        {
            rows.Add(($"Condensado específico ({w})",W(condensate)));
            rows.Add(($"Caudal de condensado ({m})",M(r.CondensateMassFlowKgS)));
            rows.Add(($"Temperatura del condensado ({t})",(setting?.CondensateTemperatureC??process.CondensateTemperatureC) is double tc?F(EducationalUnits.T(tc,units)):"No indicada"));
            rows.Add(($"Carga específica de enfriamiento del serpentín ({h})",r.CoilCoolingSpecificKJPerKgDryAir is double cs?H(cs):"Falta temperatura del condensado"));
            rows.Add(($"Carga de enfriamiento del serpentín ({q})",r.CoilCoolingLoadKW.HasValue?Q(r.CoilCoolingLoadKW):
                r.CoilCoolingSpecificKJPerKgDryAir.HasValue?"Falta caudal":flow.HasValue?"Falta temperatura del condensado":"Faltan caudal y temperatura del condensado"));
            if(method==ProcessType.Automatic)rows.Add(("Hipótesis de serpentín","Si el agua retirada se condensa como líquido; los estados no identifican el equipo."));
        }
        rows.Add(("Convención del balance","Signo positivo: aumento de entalpía o humedad del aire. Sensible a W inicial constante; latente por diferencia. El cambio de entalpía no equivale necesariamente al calor del equipo."));
        return rows;
    }
}
