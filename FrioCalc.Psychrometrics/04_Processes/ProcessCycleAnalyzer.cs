using System;
using System.Collections.Generic;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Processes;

// [4.9] Cierre de un recorrido por estados. No equivale al balance completo de equipos.
public sealed record ProcessCycleResult(
    int StageCount,bool IsClosed,double NetTemperatureC,
    double NetHumidityRatioKgPerKg,double NetEnthalpyKJPerKgDryAir,
    double? NetPowerKW,string Diagnostic);

public static class ProcessCycleAnalyzer
{
    public static bool TryAnalyze(IReadOnlyList<ChartPoint> orderedStates,
        double? dryAirMassFlowKgS,out ProcessCycleResult result,out string error)
    {
        result=new ProcessCycleResult(0,false,0,0,0,null,"");
        error="";
        if(orderedStates.Count<2)
        {error="Se necesitan al menos dos estados.";return false;}
        double pressure=orderedStates[0].PressureKPa;
        foreach(var p in orderedStates)
        {
            if(Math.Abs(p.PressureKPa-pressure)>1e-6)
            {error="Los tramos no comparten presión; requiere modelo de compresión/expansión.";return false;}
        }
        if(dryAirMassFlowKgS is double flow && (!double.IsFinite(flow)||flow<=0))
        {error="El flujo másico indicado no es positivo.";return false;}
        ChartPoint first=orderedStates[0],last=orderedStates[^1];
        double dt=last.DryBulbC-first.DryBulbC,
            dw=last.HumidityRatioKgPerKg-first.HumidityRatioKgPerKg,
            dh=last.EnthalpyKJPerKgDryAir-first.EnthalpyKJPerKgDryAir;
        bool closed=Math.Abs(dt)<.02&&Math.Abs(dw)<.00002;
        string note=closed
            ? "Recorrido geométricamente cerrado. Δh del aire tiende a cero; esto NO significa potencia nula en cada equipo."
            : "Recorrido abierto: el balance indicado corresponde solo al cambio de estado del aire entre extremos.";
        result=new ProcessCycleResult(orderedStates.Count-1,closed,dt,dw,dh,
            dryAirMassFlowKgS*dh,note);
        return true;
    }
}
