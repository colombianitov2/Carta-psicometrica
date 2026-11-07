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
    string Diagnostic,
    double? CondensateMassFlowKgS = null,
    double? CoilCoolingLoadKW = null);

public static class ProcessCalculator
{
    public static bool TryCalculate(ChartPoint inlet, ChartPoint outlet,
        ProcessType processType, double? dryAirMassFlowKgS,
        out ProcessResult result, out string error)
    {
        return TryCalculate(inlet,outlet,processType,dryAirMassFlowKgS,null,out result,out error);
    }

    // [4.2.1] Balance de serpentín con temperatura explícita del condensado.
    public static bool TryCalculate(ChartPoint inlet, ChartPoint outlet,
        ProcessType processType, double? dryAirMassFlowKgS, double? condensateTempC,
        out ProcessResult result, out string error)
    {
        error = "";
        result = new ProcessResult(0,0,0,null,null,null,null,null,"");
        if (condensateTempC.HasValue && (!double.IsFinite(condensateTempC.Value) ||
            condensateTempC.Value < -50 || condensateTempC.Value > 100))
        {
            error = "La temperatura de condensado debe ser finita y estar entre −50 y 100 °C."; return false;
        }
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
        if (processType == ProcessType.HeatingHumidification && (dT < -.1 || dw < -.00015))
        {
            error = "Calentamiento y humidificación requiere temperatura y humedad no decrecientes."; return false;
        }
        if (processType == ProcessType.Desiccant && dw > .00015)
        {
            error = "La deshumidificación por desecante debe reducir W; revise los estados."; return false;
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
        if (processType == ProcessType.Bypass ||
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
        // FCS (SHR) solo se presenta si las partes tienen signos coherentes.
        if (ratio is double shr && (!double.IsFinite(shr) || shr < -1e-6 || shr > 1.000001)) ratio = null;

        double? condensate = dryAirMassFlowKgS is double flow && dw < -1e-12 ? -flow * dw : null;
        double? coilCooling = null;
        if (condensateTempC is double tCond && condensate is double removed)
        {
            // Entalpía del condensado líquido con referencia a 0 °C. Prohibido inferir tCond.
            const double waterHeatKJPerKgK = 4.186;
            coilCooling = dryAirMassFlowKgS!.Value *
                (inlet.EnthalpyKJPerKgDryAir - outlet.EnthalpyKJPerKgDryAir)
                - removed * waterHeatKJPerKgK * tCond;
        }
        string note = dryAirMassFlowKgS is null
            ? "Se conocen cambios específicos, pero falta el caudal de aire seco para determinar potencias y flujo de agua."
            : "Potencias con referencia de entalpía de aire húmedo; para deshumidificación, verificar adicionalmente entalpía del condensado.";
        if (processType == ProcessType.Desiccant) note += " La regeneración y los balances del desecante requieren datos del equipo.";
        if (processType == ProcessType.SteamHumidification) note += " Verificar entalpía y consumo del vapor inyectado.";
        if (coilCooling.HasValue) note += $" Balance de condensado a {condensateTempC:0.###} °C incluido.";
        result = new ProcessResult(dT, dw, dh, total, dryAirMassFlowKgS * dw,
            sensible, latent, ratio, note,condensate,coilCooling);
        return true;
    }


    // [4.4] Salida de un lavador de aire ideal con efectividad de saturación.
    public static bool TryEvaporativeOutlet(ChartPoint inlet, double efficiency,
        int id, string colorHex, out ChartPoint? output, out string diagnostic)
    {
        output = null;
        diagnostic = "";
        if (!double.IsFinite(efficiency) || efficiency < 0 || efficiency > 1)
        { diagnostic = "La efectividad de saturación debe estar entre 0 y 1."; return false; }
        if (!double.IsFinite(inlet.WetBulbC))
        { diagnostic = "No se pudo determinar el bulbo húmedo de entrada."; return false; }
        double outletTemperature = inlet.DryBulbC
            - efficiency * (inlet.DryBulbC - inlet.WetBulbC);
        // Trayectoria interpolada entre entrada y estado saturado de bulbo húmedo.
        // Así ε=1 llega exactamente a la saturación; no atribuir h constante exacta.
        double pws = SaturationVaporPressure.GetPressureKPa(inlet.WetBulbC);
        if (pws >= inlet.PressureKPa) { diagnostic="Presión de saturación incompatible con bulbo húmedo."; return false; }
        double saturatedWetBulbW = SaturationHumidityRatio.FromPressuresKPa(inlet.PressureKPa,pws);
        double outletHumidity = inlet.HumidityRatioKgPerKg
            + efficiency * (saturatedWetBulbW - inlet.HumidityRatioKgPerKg);
        if (!PointPropertyCalculator.TryCreatePoint(id,$"P{id}",colorHex,inlet.PressureKPa,
            outletTemperature,outletHumidity,out output) || output == null)
        {
            diagnostic = "La salida ideal estimada no define un estado húmedo válido."; return false;
        }
        diagnostic = "Modelo ideal interpolado por efectividad: T2=T1−ε(T1−Tbh1); W2=W1+ε(Ws(Tbh1)−W1). " +
            "A ε=1 la salida está saturada; no sustituye el balance completo de agua de reposición.";
        return true;
    }

    // [4.5] Mezcla ideal de bypass con aire saturado al ADP.
    public static bool TryBypassOutlet(ChartPoint inlet, double apparatusDewPointC,
        double bypassFactor, int id, string colorHex, out ChartPoint? output, out string diagnostic)
    {
        output = null; diagnostic="";
        if (!double.IsFinite(apparatusDewPointC) || apparatusDewPointC > inlet.DryBulbC ||
            !double.IsFinite(bypassFactor) || bypassFactor < 0 || bypassFactor > 1)
        {
            diagnostic="ADP debe ser menor o igual a Tbs de entrada; BF entre 0 y 1."; return false;
        }
        if (bypassFactor < 1.0 && double.IsFinite(inlet.DewPointC) &&
            apparatusDewPointC > inlet.DewPointC + 1e-7)
        {
            diagnostic="Para serpentín deshumidificador, ADP debe ser menor o igual al punto de rocío de entrada."; return false;
        }
        double pws = SaturationVaporPressure.GetPressureKPa(apparatusDewPointC);
        if (pws >= inlet.PressureKPa) { diagnostic="Presión de saturación ADP incompatible."; return false; }
        double ws = SaturationHumidityRatio.FromPressuresKPa(inlet.PressureKPa,pws);
        double hs = MoistAirEnthalpy.FromDryBulbAndHumidityRatio(apparatusDewPointC,ws);
        double w2 = bypassFactor*inlet.HumidityRatioKgPerKg+(1-bypassFactor)*ws;
        double h2 = bypassFactor*inlet.EnthalpyKJPerKgDryAir+(1-bypassFactor)*hs;
        double t2 = (h2-2501.0*w2)/(1.006+1.86*w2);
        if (!PointPropertyCalculator.TryCreatePoint(id,$"P{id}",colorHex,inlet.PressureKPa,
            t2,w2,out output) || output == null)
        {
            diagnostic="Salida sobresaturada: revise BF, ADP y condiciones de entrada."; return false;
        }
        diagnostic="Modelo ideal BF: h2=BF·h1+(1−BF)·hADP y W2=BF·W1+(1−BF)·WADP. No contempla condensado ni calor del ventilador.";
        return true;
    }

    // [4.6] Inyección de vapor con energía aportada por kg de agua.
    public static bool TrySteamOutlet(ChartPoint inlet, double waterAddedKgPerKgDryAir,
        double steamEnthalpyKJPerKg, int id, string colorHex, out ChartPoint? output, out string diagnostic)
    {
        output=null; diagnostic="";
        if (!double.IsFinite(waterAddedKgPerKgDryAir) || waterAddedKgPerKgDryAir < 0 ||
            !double.IsFinite(steamEnthalpyKJPerKg) || steamEnthalpyKJPerKg <= 0)
        {
            diagnostic="Se requiere masa de vapor añadida no negativa y su entalpía específica."; return false;
        }
        double w2=inlet.HumidityRatioKgPerKg+waterAddedKgPerKgDryAir;
        double h2=inlet.EnthalpyKJPerKgDryAir+waterAddedKgPerKgDryAir*steamEnthalpyKJPerKg;
        double t2=(h2-2501.0*w2)/(1.006+1.86*w2);
        if (!PointPropertyCalculator.TryCreatePoint(id,$"P{id}",colorHex,inlet.PressureKPa,
            t2,w2,out output) || output == null)
        {
            diagnostic="La salida de humidificación por vapor no es físicamente válida."; return false;
        }
        diagnostic="Balances: W2=W1+Δm_v/ṁ_da y h2=h1+ΔW·h_vapor, sin pérdidas térmicas.";
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
