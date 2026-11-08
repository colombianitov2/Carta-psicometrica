using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Processes;

// [4.14] Validación de sesiones importadas contra estados RECALCULADOS.
// No confiar en propiedades derivadas serializadas por otro archivo.
public static class SessionFileIntegrity
{
    public static JsonSerializerOptions JsonOptions(bool indented=false) => new()
    {
        WriteIndented=indented,
        NumberHandling=JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static bool TryValidate(double pressureKPa,
        IReadOnlyList<ChartPoint>? points,
        IReadOnlyList<PsychrometricProcess>? processes,
        out string diagnostic)
    {
        diagnostic="";
        string? pressureError=AtmosphericPressure.GetValidationError(pressureKPa);
        if(pressureError!=null)
        {diagnostic="Presión fuera de dominio: "+pressureError;return false;}
        if(points is null || processes is null)
        {diagnostic="Faltan las colecciones de puntos o procesos.";return false;}
        if(points.Count>10000 || processes.Count>2000)
        {diagnostic="Sesión demasiado grande para la carta interactiva.";return false;}
        var uniquePoints=new HashSet<int>();
        foreach(var point in points)
        {
            if(point is null || point.Id<=0 || !uniquePoints.Add(point.Id) ||
               !ValidColor(point.ColorHex) || string.IsNullOrWhiteSpace(point.Name) ||
               Math.Abs(point.PressureKPa-pressureKPa)>1e-6)
            {diagnostic="Identidad, color o presión de punto inválidos.";return false;}
            if(!PointPropertyCalculator.TryCreatePoint(point.Id,point.Name,point.ColorHex,
                point.PressureKPa,point.DryBulbC,point.HumidityRatioKgPerKg,
                out var recalculated) || recalculated is null)
            {diagnostic=$"Estado físico inválido en {point.Name}.";return false;}
            if(!Close(recalculated.RelativeHumidityPercent,point.RelativeHumidityPercent,.1) ||
               !Close(recalculated.WetBulbC,point.WetBulbC,.1) ||
               !Close(recalculated.DewPointC,point.DewPointC,.1) ||
               !Close(recalculated.EnthalpyKJPerKgDryAir,point.EnthalpyKJPerKgDryAir,.03) ||
               !Close(recalculated.SpecificVolumeM3PerKgDryAir,
                   point.SpecificVolumeM3PerKgDryAir,.001))
            {diagnostic=$"Propiedades derivadas alteradas en {point.Name}; vuelva a calcular desde Tbs y W.";return false;}
            if(point.InputConditions is {Count:>0} && point.InputConditions.Any(x =>
                !Enum.IsDefined(x.Key)||!double.IsFinite(x.Value)))
            {diagnostic=$"Datos originales no finitos o desconocidos en {point.Name}.";return false;}
        }
        var processesSeen=new HashSet<int>();
        foreach(var process in processes)
        {
            if(process is null || process.Id<=0 || !processesSeen.Add(process.Id) ||
               string.IsNullOrWhiteSpace(process.Name)||!ValidColor(process.ColorHex) ||
               !Enum.IsDefined(process.Type) ||
               process.PointIds is null || process.PointIds.Count<2 ||
               process.PointIds.Any(id=>!uniquePoints.Contains(id)))
            {diagnostic="Proceso con identidad, color, tipo o puntos incorrectos.";return false;}
            if(process.DryAirMassFlowKgS is double flow && (!double.IsFinite(flow)||flow<=0) ||
               process.CondensateTemperatureC is double temp && (!double.IsFinite(temp)||temp< -50||temp>100) ||
               process.BranchFractionA is double frac && (!double.IsFinite(frac)||frac<=0||frac>=1) ||
               process.GeneratedPointId is int outputId && !process.PointIds.Contains(outputId) ||
               process.SaturationEffectiveness is double eff && (!double.IsFinite(eff)||eff<0||eff>1) ||
               process.BypassFactor is double bf && (!double.IsFinite(bf)||bf<0||bf>1) ||
               process.SteamAddedGramsPerKg is double water && (!double.IsFinite(water)||water<0) ||
               process.SteamEnthalpyKJPerKg is double steam && (!double.IsFinite(steam)||steam<=0) ||
               process.ApparatusDewPointC is double adp && !double.IsFinite(adp) ||
               process.DesiccantRemovedGramsPerKg is double ds && (!double.IsFinite(ds)||ds<0) ||
               process.DesiccantOutletTemperatureC is double dt && !double.IsFinite(dt) ||
               process.MixingInletFlowsM3H is { } mixFlows && mixFlows.Any(v=>!double.IsFinite(v)||v<=0))
            {diagnostic=$"Parámetros del proceso {process.Name} inválidos.";return false;}
        }
        return true;
    }

    private static bool Close(double a,double b,double tolerance) =>
        (double.IsNaN(a)&&double.IsNaN(b)) ||
        (double.IsFinite(a)&&double.IsFinite(b)&&Math.Abs(a-b)<=tolerance);

    private static bool ValidColor(string? color) =>
        color is {Length:7} && color[0]=='#' &&
        color.AsSpan(1).ToString().All(Uri.IsHexDigit);
}
