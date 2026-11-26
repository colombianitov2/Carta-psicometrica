using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Processes;

// [4.8] Informe reproducible de estados, métodos, balances y bibliografía.
public static class ProcessReportService
{
    public static string BuildMarkdown(double pressureKPa,
        IReadOnlyList<ChartPoint> points,
        IReadOnlyList<PsychrometricProcess> processes, UnitSystem units=UnitSystem.SI)
    {
        bool ip=units==UnitSystem.IP;
        string t=ip?"°F":"°C",w=ip?"gr/lbda":"g/kgda",h=ip?"Btu/lbda":"kJ/kgda",v=ip?"ft³/lbda":"m³/kgda",s=ip?"Btu/(lbda·°F)":"kJ/(kgda·K)",mUnit=ip?"lb/h":"kg/s",q=ip?"Btu/h":"kW",vol=ip?"ft³/h":"m³/h";
        string T(double x)=>F(PsychrometricUnits.TemperatureFromSI(x,units));
        string W(double x)=>F(PsychrometricUnits.HumidityFromSI(x,units));
        string H(double x)=>F(PsychrometricUnits.EnthalpyFromSI(x,units));
        string M(double x)=>F(x*(ip?7936.64143865559:1));
        string Q(double x)=>F(x*(ip?3412.14163312794:1));
        var sb = new StringBuilder();
        sb.AppendLine("# Informe de estados y procesos psicrométricos — Calculadora de refrigeración – Parte 1 – Carta psicrométrica");
        sb.AppendLine();
        sb.AppendLine($"Fecha (UTC): {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Presión absoluta: {F(PsychrometricUnits.PressureFromSI(pressureKPa,units))} {(ip?"psi":"kPa")}. Sistema {units}.");
        sb.AppendLine();
        sb.AppendLine("## Estados");
        sb.AppendLine($"| Punto | Tbs {t} | Tbh {t} | Trocío {t} | HR % | W {w} | h {h} | v {v} | s* {s} | Método |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (ChartPoint p in points)
            sb.AppendLine($"|{p.Name}|{T(p.DryBulbC)}|{T(p.WetBulbC)}|{T(p.DewPointC)}|{F(p.RelativeHumidityPercent)}|{W(p.HumidityRatioKgPerKg)}|{H(p.EnthalpyKJPerKgDryAir)}|{F(PsychrometricUnits.VolumeFromSI(p.SpecificVolumeM3PerKgDryAir,units))}|{F(PsychrometricUnits.EntropyFromSI(MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(p.DryBulbC,p.HumidityRatioKgPerKg,p.PressureKPa),units))}|{p.Method}|");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("### Procedencia de los estados");
        foreach(var p in points)
        {
            sb.AppendLine($"- {p.Name}: origen **{p.Origin}**, método **{p.Method}**.");
            if(p.InputConditions is {Count:>0})
                sb.AppendLine($"  Datos originales (SI): {string.Join(", ",p.InputConditions.Select(kv=>$"{kv.Key}={F(kv.Value)}"))}.");
        }
        sb.AppendLine();
        sb.AppendLine("## Procesos");
        if (processes.Count == 0) sb.AppendLine("No se han registrado procesos.");
        foreach (var process in processes)
        {
            sb.AppendLine($"### {process.Name} — {ProcessResultPresentation.MethodName(process.Type)}");
            sb.AppendLine($"Secuencia: {string.Join(" → ",process.PointIds.Select(id=>"P"+id))}. Color {process.ColorHex}; visible: {(process.Visible?"sí":"no")}.");
            sb.AppendLine($"Flujo másico de aire seco: {(process.DryAirMassFlowKgS is double m?M(m)+" "+mUnit:"no aportado")}.");
            sb.AppendLine();
            if (process.Type==ProcessType.AdiabaticMixing && process.MixingInletFlowsM3H is {Count:>0} flows)
            {
                sb.AppendLine($"|Corriente|Caudal volumétrico ({vol})|Caudal másico seco ({mUnit})|");
                sb.AppendLine("|---|---:|---:|");
                double total=0;
                for(int i=0;i<flows.Count && i<process.PointIds.Count;i++)
                {
                    var point=points.FirstOrDefault(p=>p.Id==process.PointIds[i]);
                    if(point is null)continue;
                    double mass=flows[i]/3600/point.SpecificVolumeM3PerKgDryAir;
                    total+=mass;sb.AppendLine($"|{point.Name}|{F(flows[i]*(ip?35.31466672149:1))}|{M(mass)}|");
                }
                sb.AppendLine($"|Total|—|{M(total)}|");
            }
            if(process.Type==ProcessType.Branching && process.PointIds.Count==3 &&
                process.DryAirMassFlowKgS is double totalFlow && process.BranchFractionA is double fraction)
            {
                var states=process.PointIds.Select(id=>points.FirstOrDefault(p=>p.Id==id)).ToArray();
                if(states.All(p=>p is not null) &&
                    ProcessCalculator.TryBranchBalance(states[0]!,states[1]!,states[2]!,totalFlow,fraction,out var b,out _) && b is not null)
                {
                    sb.AppendLine($"|Rama|Caudal ({mUnit})|Agua ({mUnit})|Potencia ({q})|");
                    sb.AppendLine("|---|---:|---:|---:|");
                    sb.AppendLine($"|{states[1]!.Name}|{M(b.FirstMassFlowKgS)}|{M(b.AddedWaterFirstKgS)}|{Q(b.ThermalFirstKW)}|");
                    sb.AppendLine($"|{states[2]!.Name}|{M(b.SecondMassFlowKgS)}|{M(b.AddedWaterSecondKgS)}|{Q(b.ThermalSecondKW)}|");
                }
            }
            if (process.Type != ProcessType.AdiabaticMixing && process.Type != ProcessType.Branching)
            {
                for (int i = 0; i < process.PointIds.Count - 1; i++)
                {
                    ChartPoint? from=points.FirstOrDefault(p=>p.Id==process.PointIds[i]);
                    ChartPoint? to=points.FirstOrDefault(p=>p.Id==process.PointIds[i+1]);
                    if (from is null || to is null)
                    {
                        sb.AppendLine("Estado faltante: no se puede calcular el tramo.");
                        continue;
                    }
                    var segment=ProcessSegmentSetting.Find(process.SegmentSettings,process.PointIds,i);
                    ProcessType method=segment?.Type??(process.Type==ProcessType.Bypass ? ProcessType.Automatic : process.Type);
                    if (!ProcessCalculator.TryCalculate(from,to,method,segment?.DryAirMassFlowKgS??process.DryAirMassFlowKgS,
                        segment?.CondensateTemperatureC??process.CondensateTemperatureC,out ProcessResult result,out string error))
                    {
                        sb.AppendLine($"{from.Name} → {to.Name}: {error}");
                        continue;
                    }
                    sb.AppendLine($"### Tramo {i+1}: {from.Name} → {to.Name}");
                    sb.AppendLine("|Magnitud|Resultado|");
                    sb.AppendLine("|---|---|");
                    foreach(var row in ProcessResultPresentation.SegmentRows(process,segment,from,to,result,units))
                        sb.AppendLine($"|{row.Label}|{row.Value}|");
                    sb.AppendLine();

                }
            }
            sb.AppendLine();
            sb.AppendLine();
        }
        sb.AppendLine("## Bibliografía");
        sb.AppendLine("ASHRAE Handbook—Fundamentals (2025)");
        sb.AppendLine("Procesos psicrométricos — Hermes Ramírez León (material docente)");
        return sb.ToString();
    }

    private static string F(double value) =>
        double.IsFinite(value)?value.ToString("0.######",CultureInfo.InvariantCulture):"no disponible";
}
