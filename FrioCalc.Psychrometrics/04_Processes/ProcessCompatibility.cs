using System;
using System.Collections.Generic;
using System.Linq;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Processes;

// [4.13] Compatibilidad física de estados y selección de modelo.
// La razón de humedad W, no el porcentaje HR, determina adición o retirada de agua.
public enum ProcessCompatibilityStatus { Compatible, Incomplete, Incompatible }
public readonly record struct ProcessAssessment(ProcessCompatibilityStatus Status,string Message)
{
    public bool CanCalculate => Status==ProcessCompatibilityStatus.Compatible;
    public static ProcessAssessment Ok(string message)=>new(ProcessCompatibilityStatus.Compatible,message);
    public static ProcessAssessment Need(string message)=>new(ProcessCompatibilityStatus.Incomplete,message);
    public static ProcessAssessment Invalid(string message)=>new(ProcessCompatibilityStatus.Incompatible,message);
}

public static class ProcessCompatibility
{
    // Tolerancia de igualdad numérica y tolerancia de concordancia de estados
    // introducidos de manera independiente. No se redondean los balances.
    private const double DirectionT=1e-5, DirectionW=1e-9;
    public const double MatchT=0.02, MatchW=0.00002; // °C, kg/kgda
    public static bool IsValidState(ChartPoint p)
    {
        if(!double.IsFinite(p.PressureKPa)||p.PressureKPa<=0||
           !double.IsFinite(p.DryBulbC)||p.DryBulbC<SaturationVaporPressure.MinSupportedTemperatureC||
           p.DryBulbC>SaturationVaporPressure.MaxSupportedTemperatureC||
           !double.IsFinite(p.HumidityRatioKgPerKg)||p.HumidityRatioKgPerKg<0||
           !double.IsFinite(p.EnthalpyKJPerKgDryAir)||
           !double.IsFinite(p.SpecificVolumeM3PerKgDryAir)||p.SpecificVolumeM3PerKgDryAir<=0||
           !double.IsFinite(p.RelativeHumidityPercent)||p.RelativeHumidityPercent<0||p.RelativeHumidityPercent>100.0001)
            return false;
        double pv=p.PressureKPa*p.HumidityRatioKgPerKg/(0.621945+p.HumidityRatioKgPerKg);
        double ps=SaturationVaporPressure.GetPressureKPa(p.DryBulbC);
        return double.IsFinite(pv)&&pv>=0&&pv<p.PressureKPa&&pv<=ps*(1+1e-6);
    }
    public static bool SameState(ChartPoint a,ChartPoint b) =>
        Math.Abs(a.PressureKPa-b.PressureKPa)<=1e-6 &&
        Math.Abs(a.DryBulbC-b.DryBulbC)<=MatchT &&
        Math.Abs(a.HumidityRatioKgPerKg-b.HumidityRatioKgPerKg)<=MatchW;

    public static string Direction(ChartPoint from,ChartPoint to)
    {
        double dt=to.DryBulbC-from.DryBulbC,dw=to.HumidityRatioKgPerKg-from.HumidityRatioKgPerKg;
        string thermal=dt>DirectionT?"calentamiento":dt< -DirectionT?"enfriamiento":"temperatura constante";
        string moisture=dw>DirectionW?"humidificación":dw< -DirectionW?"deshumidificación":"W constante";
        return thermal+" · "+moisture;
    }

    public static ProcessAssessment AssessPair(ChartPoint from,ChartPoint to,ProcessType type)
    {
        string prefix=from.Name+" → "+to.Name+": ";
        if(!IsValidState(from)||!IsValidState(to))
            return ProcessAssessment.Invalid(prefix+"Hay un estado no admisible del aire húmedo.");
        if(Math.Abs(from.PressureKPa-to.PressureKPa)>1e-6)
            return ProcessAssessment.Invalid(prefix+"Las presiones son distintas. Se requiere un modelo de cambio de presión.");
        double dt=to.DryBulbC-from.DryBulbC,dw=to.HumidityRatioKgPerKg-from.HumidityRatioKgPerKg,
            dh=to.EnthalpyKJPerKgDryAir-from.EnthalpyKJPerKgDryAir;
        bool unchanged=Math.Abs(dt)<=DirectionT&&Math.Abs(dw)<=DirectionW;
        string direction=Direction(from,to);
        ProcessAssessment Fail(string reason)=>ProcessAssessment.Invalid(prefix+reason+" Los estados indican "+direction+".");
        ProcessAssessment Ok(string detail)=>ProcessAssessment.Ok(prefix+detail);
        switch(type)
        {
            case ProcessType.Automatic:
                return Ok(unchanged?"Sin cambio de estado.":"Diferencias de estado: "+direction+".");
            case ProcessType.Sensible:
                if(Math.Abs(dw)>0.00015) return Fail("El proceso exclusivamente sensible requiere W constante.");
                return Ok(unchanged?"Sin transformación térmica.":(dt>=0?"Calefacción":"Enfriamiento")+" sensible (W constante dentro de la tolerancia del modelo).");
            case ProcessType.HeatingHumidification:
                if(dt<=DirectionT||dw<=DirectionW) return Fail("Calentamiento y humidificación exige que aumenten tanto T como W.");
                return Ok("Aumentan T y W: calentamiento y humidificación.");
            case ProcessType.CoolingDehumidification:
                if(dt>=-DirectionT||dw>=-DirectionW) return Fail("Enfriamiento y deshumidificación exige que disminuyan tanto T como W.");
                return Ok("Disminuyen T y W: enfriamiento y deshumidificación.");
            case ProcessType.SteamHumidification:
                if(unchanged) return Ok("Sin transformación; corresponde a aporte de vapor nulo.");
                if(dw<=DirectionW) return Fail("Inyectar vapor exige aumentar W, no disminuirla ni conservarla con cambio térmico.");
                if(dh<=0) return Fail("El balance del modelo requiere aporte positivo de entalpía del vapor.");
                return Ok("Aumentan W y h; comprobar la entalpía y masa del vapor para verificar el equipo.");
            case ProcessType.EvaporativeHumidification:
                if(unchanged) return Ok("Sin transformación; efectividad nula o entrada ya saturada.");
                if(dt>=-DirectionT||dw<=DirectionW) return Fail("El modelo evaporativo directo exige enfriar y aumentar W.");
                if(!double.IsFinite(from.WetBulbC)||from.DryBulbC-from.WetBulbC<=DirectionT)
                    return Fail("No existe potencial de enfriamiento evaporativo en el estado inicial.");
                if(to.DryBulbC<from.WetBulbC-MatchT)
                    return Fail("La salida no puede estar por debajo del bulbo húmedo inicial en este modelo.");
                double epsilon=Math.Clamp(-dt/(from.DryBulbC-from.WetBulbC),0,1);
                if(!ProcessCalculator.TryEvaporativeOutlet(from,epsilon,0,from.ColorHex,out var estimated,out _)||
                    estimated is null||!SameState(estimated,to))
                    return Fail("T y W de salida no corresponden a una misma efectividad de saturación del modelo ideal.");
                return Ok("Trayectoria compatible con la efectividad del modelo evaporativo ideal.");
            case ProcessType.Desiccant:
                if(unchanged)return Ok("Sin retirada de humedad; no hay deshumidificación efectiva.");
                if(dw>=-DirectionW)return Fail("Un desecante debe retirar vapor; W debe disminuir.");
                return Ok("W disminuye. El estado térmico se especifica; no se predice la sorción o regeneración.");
            case ProcessType.Bypass:
                if(unchanged)return Ok("Sin transformación; compatible con BF=1.");
                if(dt>=-DirectionT||dw>DirectionW)return Fail("El modelo de serpentín con bypass no puede calentar ni añadir humedad.");
                return Ok("Dirección compatible con un serpentín; se debe verificar ADP y BF.");
            default:
                return Fail("La mezcla o división requiere varias corrientes y balances, no un solo tramo de dos estados.");
        }
    }

    public static ProcessAssessment AssessSelection(ProcessType type,IReadOnlyList<ChartPoint> states)
    {
        if(states.Any(s=>!IsValidState(s)))
            return ProcessAssessment.Invalid("Hay estados no admisibles del aire húmedo.");
        if(states.Count>1&&states.Any(s=>Math.Abs(s.PressureKPa-states[0].PressureKPa)>1e-6))
            return ProcessAssessment.Invalid("Todos los estados deben compartir presión absoluta para este modelo.");
        if(type==ProcessType.AdiabaticMixing)
            return states.Count<2?ProcessAssessment.Need("Seleccione al menos dos corrientes de entrada.") :
                ProcessAssessment.Ok("Corrientes de entrada compatibles; compruebe sus caudales y la salida mezclada.");
        if(type==ProcessType.Branching)
        {
            if(states.Count<3)return ProcessAssessment.Need("Seleccione entrada, salida 1 y salida 2, en ese orden.");
            if(states.Count>3)return ProcessAssessment.Invalid("El divisor admite exactamente una entrada y dos salidas.");
            return ProcessAssessment.Ok("Tres estados a la misma presión; compruebe caudal y fracción de las ramas.");
        }
        bool equipment=type is ProcessType.SteamHumidification or ProcessType.EvaporativeHumidification or ProcessType.Bypass;
        if(equipment&&states.Count>2)
            return ProcessAssessment.Invalid("Este modelo de equipo admite una entrada o una entrada y una salida de comprobación. Use tramos de un grupo automático para un recorrido mayor.");
        if(states.Count==0)return ProcessAssessment.Need("Seleccione estados del grupo.");
        if(states.Count==1)
            return equipment||type==ProcessType.Desiccant
                ?ProcessAssessment.Ok("Entrada válida; se requieren parámetros del equipo para determinar la salida.")
                :ProcessAssessment.Need("Seleccione al menos dos estados en el orden del proceso.");
        ProcessAssessment result=ProcessAssessment.Ok("");
        for(int i=0;i<states.Count-1;i++)
        {
            result=AssessPair(states[i],states[i+1],type);
            if(!result.CanCalculate)return result;
        }
        return states.Count==2?result:ProcessAssessment.Ok("Los "+(states.Count-1)+" tramos son compatibles con el método seleccionado.");
    }
}
