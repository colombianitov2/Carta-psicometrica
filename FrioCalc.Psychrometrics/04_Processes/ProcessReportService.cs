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
        IReadOnlyList<PsychrometricProcess> processes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Informe de estados y procesos psicrométricos — FrioCalc");
        sb.AppendLine();
        sb.AppendLine($"Fecha (UTC): {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Presión absoluta: {F(pressureKPa)} kPa. Valores internos en SI.");
        sb.AppendLine();
        sb.AppendLine("## Estados");
        sb.AppendLine("| Punto | Tbs °C | Tbh °C | Trocío °C | HR % | W g/kgda | h kJ/kgda | v m³/kgda | s* kJ/(kgda·K) | Método |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (ChartPoint p in points)
            sb.AppendLine($"|{p.Name}|{F(p.DryBulbC)}|{F(p.WetBulbC)}|{F(p.DewPointC)}|{F(p.RelativeHumidityPercent)}|{F(p.HumidityRatioKgPerKg*1000)}|{F(p.EnthalpyKJPerKgDryAir)}|{F(p.SpecificVolumeM3PerKgDryAir)}|{F(MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(p.DryBulbC,p.HumidityRatioKgPerKg,p.PressureKPa))}|{p.Method}|");
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
            sb.AppendLine($"### {process.Name} — {process.Type}");
            sb.AppendLine($"Secuencia: {string.Join(" → ",process.PointIds.Select(id=>"P"+id))}. Color {process.ColorHex}; visible: {(process.Visible?"sí":"no")}.");
            sb.AppendLine($"Flujo másico de aire seco: {(process.DryAirMassFlowKgS is double m?F(m)+" kgda/s":"no aportado")}.");
            sb.AppendLine();
            if (process.Type==ProcessType.AdiabaticMixing && process.MixingInletFlowsM3H is {Count:>0} flows)
            {
                sb.AppendLine("|Corriente|Caudal volumétrico (m³/h)|Caudal másico seco (kg/s)|");
                sb.AppendLine("|---|---:|---:|");
                double total=0;
                for(int i=0;i<flows.Count && i<process.PointIds.Count;i++)
                {
                    var point=points.FirstOrDefault(p=>p.Id==process.PointIds[i]);
                    if(point is null)continue;
                    double mass=flows[i]/3600/point.SpecificVolumeM3PerKgDryAir;
                    total+=mass;sb.AppendLine($"|{point.Name}|{F(flows[i])}|{F(mass)}|");
                }
                sb.AppendLine($"|Total|—|{F(total)}|");
            }
            if(process.Type==ProcessType.Branching && process.PointIds.Count==3 &&
                process.DryAirMassFlowKgS is double totalFlow && process.BranchFractionA is double fraction)
            {
                var states=process.PointIds.Select(id=>points.FirstOrDefault(p=>p.Id==id)).ToArray();
                if(states.All(p=>p is not null) &&
                    ProcessCalculator.TryBranchBalance(states[0]!,states[1]!,states[2]!,totalFlow,fraction,out var b,out _) && b is not null)
                {
                    sb.AppendLine("|Rama|Caudal (kg/s)|Agua (kg/s)|Potencia (kW)|");
                    sb.AppendLine("|---|---:|---:|---:|");
                    sb.AppendLine($"|{states[1]!.Name}|{F(b.FirstMassFlowKgS)}|{F(b.AddedWaterFirstKgS)}|{F(b.ThermalFirstKW)}|");
                    sb.AppendLine($"|{states[2]!.Name}|{F(b.SecondMassFlowKgS)}|{F(b.AddedWaterSecondKgS)}|{F(b.ThermalSecondKW)}|");
                }
            }
            if (process.Type != ProcessType.AdiabaticMixing && process.Type != ProcessType.Branching)
            {
                sb.AppendLine("|Tramo|ΔT °C|ΔW g/kgda|Δh kJ/kgda|Potencia (kW)|FCS calculado|FCS indicado|Condensado (kg/s)|Carga serpentín (kW)|");
                sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");
                for (int i = 0; i < process.PointIds.Count - 1; i++)
                {
                    ChartPoint? from=points.FirstOrDefault(p=>p.Id==process.PointIds[i]);
                    ChartPoint? to=points.FirstOrDefault(p=>p.Id==process.PointIds[i+1]);
                    if (from is null || to is null)
                    {
                        sb.AppendLine("|Estado faltante|—|—|—|—|—|");
                        continue;
                    }
                    var segment=process.SegmentSettings?.FirstOrDefault(x=>x.FromId==from.Id&&x.ToId==to.Id);
                    ProcessType method=segment?.Type??(process.Type==ProcessType.Bypass ? ProcessType.Automatic : process.Type);
                    if (!ProcessCalculator.TryCalculate(from,to,method,segment?.DryAirMassFlowKgS??process.DryAirMassFlowKgS,
                        segment?.CondensateTemperatureC??process.CondensateTemperatureC,out ProcessResult result,out string error))
                    {
                        sb.AppendLine($"|{from.Name}→{to.Name}: {error}|—|—|—|—|—|");
                        continue;
                    }
                    sb.AppendLine($"|{from.Name}→{to.Name}|{F(result.DeltaTemperatureC)}|{F(result.DeltaHumidityRatioKgPerKg*1000)}|{F(result.DeltaEnthalpyKJPerKgDryAir)}|{(result.ThermalPowerKW.HasValue?F(result.ThermalPowerKW.Value):"sin caudal")}|{(result.SensibleHeatFactor.HasValue?F(result.SensibleHeatFactor.Value):"indefinido")}|{(segment?.SensibleHeatFactor is double f?F(f):"—")}|{(result.CondensateMassFlowKgS is double mc?F(mc):"—")}|{(result.CoilCoolingLoadKW is double qc?F(qc):"—")}|");

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
