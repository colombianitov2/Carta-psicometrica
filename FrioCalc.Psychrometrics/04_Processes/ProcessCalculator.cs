using System;
using System.Collections.Generic;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Processes;

// [4.2] Balances por kg de aire seco; no atribuir potencia sin caudal.
public sealed record ProcessResult(
    double DeltaTemperatureC, double DeltaHumidityRatioKgPerKg,
    double DeltaEnthalpyKJPerKgDryAir, double? ThermalPowerKW,
    double? MoistureFlowKgS, double? SensiblePowerKW,
    double? LatentPowerKW, double? SensibleHeatFactor,
    string Diagnostic);

public static class ProcessCalculator
{
    public static bool TryCalculate(ChartPoint inlet, ChartPoint outlet,
        ProcessType processType, double? dryAirMassFlowKgS,
        out ProcessResult result, out string error)
    {
        error = "";
        result = new ProcessResult(0, 0, 0, null, null, null, null, null, "");
        if (Math.Abs(inlet.PressureKPa - outlet.PressureKPa) > 1e-6)
        {
            error = "Estados a presiones diferentes: se requiere un modelo de compresión o expansión."; return false;
        }
        if (dryAirMassFlowKgS is double m && (!double.IsFinite(m) || m <= 0))
        {
            error = "El caudal másico de aire seco debe ser positivo."; return false;
        }
        double dT = outlet.DryBulbC - inlet.DryBulbC;
        double dw = outlet.HumidityRatioKgPerKg - inlet.HumidityRatioKgPerKg;
        double dh = outlet.EnthalpyKJPerKgDryAir - inlet.EnthalpyKJPerKgDryAir;

        if (processType == ProcessType.Sensible && Math.Abs(dw) > 0.00015)
        {
            error = "Un proceso exclusivamente sensible debe mantener la humedad absoluta."; return false;
        }
        if (processType == ProcessType.CoolingDehumidification && (dT > .1 || dw > .00015))
        {
            error = "En enfriamiento y deshumidificación, T y W no deben aumentar."; return false;
        }
        if (processType == ProcessType.EvaporativeHumidification && (dT > .1 || dw < -.00015))
        {
            error = "La humidificación evaporativa ideal enfría el aire mientras aumenta su humedad."; return false;
        }
        if (processType == ProcessType.SteamHumidification && dw < -.00015)
        {
            error = "La inyección de vapor no puede retirar humedad."; return false;
        }
        if (processType == ProcessType.Desiccant || processType == ProcessType.Bypass ||
            processType == ProcessType.AdiabaticMixing)
        {
            error = "Este modelo requiere información de equipo o de varias corrientes. No se calcula como proceso simple de dos estados.";
            return false;
        }

        // Convención: calor sensible a humedad inicial constante; diferencia de entalpía residual.
        // Para condensación se necesita un balance separado de la entalpía del condensado
        // si se pretende calcular con exactitud la carga del serpentín.
        double sensibleSpecific = (MoistAirEnthalpy.DryAirSpecificHeatKJPerKgK
            + inlet.HumidityRatioKgPerKg * MoistAirEnthalpy.WaterVaporSpecificHeatKJPerKgK) * dT;
        double latentSpecific = dh - sensibleSpecific;
        double? total = dryAirMassFlowKgS * dh;
        double? sensible = dryAirMassFlowKgS * sensibleSpecific;
        double? latent = dryAirMassFlowKgS * latentSpecific;
        double? ratio = Math.Abs(dh) > 1e-9 ? sensibleSpecific / dh : null;
        string note = dryAirMassFlowKgS is null
            ? "Se conocen cambios específicos, pero falta el caudal de aire seco para determinar potencias y flujo de agua."
            : "Potencias con referencia de entalpía de aire húmedo; para deshumidificación, verificar adicionalmente entalpía del condensado.";
        result = new ProcessResult(dT, dw, dh, total, dryAirMassFlowKgS * dw,
            sensible, latent, ratio, note);
        return true;
    }

    // [4.3] Mezcla adiabática con conservación explícita de masa de aire seco y vapor.
    public static bool TryMix(IReadOnlyList<(ChartPoint State, double DryAirMassFlowKgS)> streams,
        int id, string colorHex, out ChartPoint? mixed, out string diagnostic)
    {
        mixed = null; diagnostic = "";
        if (streams.Count < 2) { diagnostic = "Se requieren al menos dos corrientes de entrada."; return false; }
        double mass = 0, moisture = 0, enthalpy = 0;
        double p = streams[0].State.PressureKPa;
        foreach (var (state, flow) in streams)
        {
            if (!double.IsFinite(flow) || flow <= 0)
            { diagnostic = "Cada flujo de aire seco debe ser positivo."; return false; }
            if (Math.Abs(state.PressureKPa - p) > 1e-6)
            { diagnostic = "Para mezcla adiabática, las corrientes deben compartir presión."; return false; }
            mass += flow;
            moisture += flow * state.HumidityRatioKgPerKg;
            enthalpy += flow * state.EnthalpyKJPerKgDryAir;
        }
        double w = moisture / mass, h = enthalpy / mass;
        double dryBulb = (h - MoistAirEnthalpy.WaterVaporReferenceEnthalpyKJPerKg * w)
            / (MoistAirEnthalpy.DryAirSpecificHeatKJPerKgK
                + MoistAirEnthalpy.WaterVaporSpecificHeatKJPerKgK * w);
        if (!PointPropertyCalculator.TryCreatePoint(id, $"M{id}", colorHex,
            p, dryBulb, w, out mixed) || mixed == null)
        {
            diagnostic = "La mezcla resultante es inválida o sobresaturada; se requiere balance de condensación.";
            return false;
        }
        diagnostic = "Mezcla determinada con m_da, m_da·W y m_da·h; sin condensación.";
        return true;
    }
}
