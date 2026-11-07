using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FrioCalc.Psychrometrics.Points;

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
        sb.AppendLine("| Punto | Tbs °C | Tbh °C | Trocío °C | HR % | W g/kgda | h kJ/kgda | v m³/kgda | Método |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (ChartPoint p in points)
            sb.AppendLine($"|{p.Name}|{F(p.DryBulbC)}|{F(p.WetBulbC)}|{F(p.DewPointC)}|{F(p.RelativeHumidityPercent)}|{F(p.HumidityRatioKgPerKg*1000)}|{F(p.EnthalpyKJPerKgDryAir)}|{F(p.SpecificVolumeM3PerKgDryAir)}|{p.Method}|");
        sb.AppendLine();
        sb.AppendLine("## Procesos");
        if (processes.Count == 0) sb.AppendLine("No se han registrado procesos.");
        foreach (var process in processes)
        {
            sb.AppendLine($"### {process.Name} — {process.Type}");
            sb.AppendLine($"Secuencia: {string.Join(" → ",process.PointIds.Select(id=>"P"+id))}. Color {process.ColorHex}; visible: {(process.Visible?"sí":"no")}.");
            sb.AppendLine($"Flujo másico de aire seco: {(process.DryAirMassFlowKgS is double m?F(m)+" kgda/s":"no aportado")}.");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(process.Report))
            {
                sb.AppendLine("Resultado registrado al crear el proceso:");
                sb.AppendLine();
                foreach (string line in process.Report.Split('\n'))
                    sb.AppendLine("> " + line.TrimEnd('\r'));
                sb.AppendLine();
            }
            if (process.Type != ProcessType.AdiabaticMixing)
            {
                sb.AppendLine("|Tramo|ΔT °C|ΔW g/kgda|Δh kJ/kgda|Q̇ basada en h (kW)|FCS estimado|");
                sb.AppendLine("|---|---:|---:|---:|---:|---:|");
                for (int i = 0; i < process.PointIds.Count - 1; i++)
                {
                    ChartPoint? from=points.FirstOrDefault(p=>p.Id==process.PointIds[i]);
                    ChartPoint? to=points.FirstOrDefault(p=>p.Id==process.PointIds[i+1]);
                    if (from is null || to is null)
                    {
                        sb.AppendLine("|Estado faltante|—|—|—|—|—|");
                        continue;
                    }
                    ProcessType method=process.Type==ProcessType.Bypass ? ProcessType.Automatic : process.Type;
                    if (!ProcessCalculator.TryCalculate(from,to,method,process.DryAirMassFlowKgS,
                        out ProcessResult result,out string error))
                    {
                        sb.AppendLine($"|{from.Name}→{to.Name}: {error}|—|—|—|—|—|");
                        continue;
                    }
                    sb.AppendLine($"|{from.Name}→{to.Name}|{F(result.DeltaTemperatureC)}|{F(result.DeltaHumidityRatioKgPerKg*1000)}|{F(result.DeltaEnthalpyKJPerKgDryAir)}|{(result.ThermalPowerKW.HasValue?F(result.ThermalPowerKW.Value):"sin caudal")}|{(result.SensibleHeatFactor.HasValue?F(result.SensibleHeatFactor.Value):"indefinido")}|");
                }
            }
            sb.AppendLine();
            sb.AppendLine("**Advertencia de ingeniería:** una trayectoria entre estados conocidos no demuestra que el equipo sea realizable. Deshumidificación rigurosa requiere considerar entalpía del condensado. Vapor: comprobar caudal y entalpía del agua inyectada. Desecantes: datos y regeneración del equipo.");
            sb.AppendLine();
        }
        sb.AppendLine("## Ecuaciones y métodos");
        sb.AppendLine("- W = 0,621945·p_v/(p−p_v).");
        sb.AppendLine("- h = 1,006·Tbs + W·(2501 + 1,86·Tbs), en kJ/kg de aire seco.");
        sb.AppendLine("- v = 287,042·T_K·(1+1,607858·W)/p_Pa, en m³/kg de aire seco.");
        sb.AppendLine("- Q̇_aire = ṁ_da·(h₂−h₁); signo positivo indica aporte de calor al aire.");
        sb.AppendLine("- Mezcla adiabática: Wmix=Σ(ṁ_da,i·Wi)/Σṁ_da,i; hmix=Σ(ṁ_da,i·hi)/Σṁ_da,i.");
        sb.AppendLine("- BF ideal: W₂=BF·W₁+(1−BF)·W_ADP; h₂=BF·h₁+(1−BF)·h_ADP.");
        sb.AppendLine();
        sb.AppendLine("## Fuentes de referencia");
        sb.AppendLine("1. ASHRAE Handbook—Fundamentals 2025, cap. 1 Psychrometrics: https://www.ashrae.org/technical-resources/publications-library/ashrae-handbook/table-of-contents-2025-ashrae-handbook-fundamentals");
        sb.AppendLine("2. PsychroLib: https://github.com/psychrometrics/psychrolib");
        sb.AppendLine("3. CoolProp HumidAir: https://coolprop.org/fluid_properties/HumidAir.html");
        sb.AppendLine("4. Hermes Ramírez León, A03 Procesos Psicrométricos, diapositivas 1–24 (material del usuario).");
        sb.AppendLine();
        sb.AppendLine("**Estado documental:** falta revisar edición, capítulo y número de ecuación o página exacta de cada método. No interpretar este informe como certificación del diseño.");
        return sb.ToString();
    }

    private static string F(double value) =>
        double.IsFinite(value)?value.ToString("0.######",CultureInfo.InvariantCulture):"no disponible";
}
