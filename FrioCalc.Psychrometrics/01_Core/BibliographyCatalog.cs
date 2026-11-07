using System;
using System.Collections.Generic;
using System.Text;

namespace FrioCalc.Psychrometrics.Core;

// [1.12.1] Registro trazable: no inventar páginas ni atribuir a ASHRAE 2025
// números de ecuación de otras ediciones.
public sealed record BibliographyEntry(
    string Id,string Variable,string Symbol,string UnitsSI,string UnitsIP,
    string Definition,string ObtainingMethod,string MethodId,string Source,
    string Chapter,string Page,string Url,string Verification);

public static class BibliographyCatalog
{
    private const string ASHRAE="https://www.ashrae.org/technical-resources/publications-library/ashrae-handbook/table-of-contents-2025-ashrae-handbook-fundamentals";
    private const string PSYCHRO="https://psychrometrics.github.io/psychrolib/api_docs.html";
    private const string COOLPROP="https://coolprop.org/fluid_properties/HumidAir.html";
    private const string EVAP="https://handbook.ashrae.org/handbooks/S20/SI/s20_ch41/s20_ch41_si.aspx";
    private const string SLIDES="Material docente A03 Procesos Psicrométricos, diapositivas 1–24 (suministrado por el usuario).";

    public static IReadOnlyList<BibliographyEntry> Entries {get;} = new []
    {
        new BibliographyEntry("B01","Presión atmosférica absoluta","P","kPa abs","psi abs",
            "Presión termodinámica absoluta de la mezcla.","Medición barométrica o presión de estación a la altitud real.","AtmosphericPressure",
            "ASHRAE Handbook—Fundamentals 2025","Capítulo 1: Psychrometrics","No verificada",ASHRAE,"Índice oficial; capítulo completo sujeto a licencia"),
        new BibliographyEntry("B02","Bulbo seco","Tbs","°C","°F",
            "Temperatura del aire medida sin influencia de evaporación en el sensor.",
            "Termómetro de bulbo seco, sonda de temperatura calibrada.","ChartPoint.DryBulbC",
            "ASHRAE Handbook—Fundamentals 2025","Capítulo 1: Psychrometrics","No verificada",ASHRAE,"Índice oficial"),
        new BibliographyEntry("B03","Bulbo húmedo termodinámico","Tbh","°C","°F",
            "Temperatura de equilibrio asociada al proceso de saturación adiabática.",
            "Resolver W(Tbs,Tbh,P) o invertir desde Tbs,W,P.","WetBulbHumidityRatio.FromDryBulbAndWetBulb",
            "PsychroLib / ASHRAE 2017 (referencia documentada por PsychroLib)","PsychroLib GetHumRatioFromTWetBulb","Consultar código",PSYCHRO,"Documentación API abierta"),
        new BibliographyEntry("B04","Punto de rocío","Trocío","°C","°F",
            "Temperatura a la que el vapor alcanza la saturación a presión parcial dada.",
            "Invertir presión de vapor de saturación; sensor óptico de punto de rocío.","PointPropertyCalculator.GetDewPointFromVaporPressure",
            "PsychroLib","GetTDewPointFromHumRatio","Consultar código",PSYCHRO,"Documentación API abierta"),
        new BibliographyEntry("B05","Humedad relativa","φ / HR","%","%",
            "Relación entre presión parcial de vapor y presión de saturación a Tbs.",
            "Higrómetro o cálculo de Pv/Pws(Tbs)·100.","RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity",
            "PsychroLib","GetHumRatioFromRelHum","Consultar código",PSYCHRO,"Documentación API abierta"),
        new BibliographyEntry("B06","Razón de humedad / humedad absoluta","W","kg/kgda o g/kgda","lb/lbda o gr/lbda",
            "Masa de vapor de agua por masa de aire seco.",
            "W=0,621945·Pv/(P−Pv), con presiones absolutas coherentes.","SaturationHumidityRatio.FromPressuresKPa",
            "PsychroLib / ASHRAE 2025","GetHumRatioFromVapPres; capítulo 1","No verificada",PSYCHRO,"Ecuación e implementación contrastables"),
        new BibliographyEntry("B07","Presión de vapor de saturación","Pws","kPa","psi",
            "Presión de equilibrio sobre agua o hielo, según temperatura.",
            "Ecuaciones de correlación de saturación de ASHRAE.","SaturationVaporPressure.GetPressureKPa",
            "PsychroLib","GetSatVapPres","Consultar código",PSYCHRO,"Fases y rango de aplicación"),
        new BibliographyEntry("B08","Entalpía de aire húmedo","h","kJ/kgda","Btu/lbda",
            "Entalpía por unidad de masa de aire seco con referencia convencional.",
            "h=1,006·Tbs+W·(2501+1,86·Tbs), SI aproximado.","MoistAirEnthalpy.FromDryBulbAndHumidityRatio",
            "PsychroLib, referencia ASHRAE 2017","GetMoistAirEnthalpy / eq. 30 (edición 2017)","Consultar código",PSYCHRO,"No trasladar numeración a la edición 2025"),
        new BibliographyEntry("B09","Volumen específico del aire húmedo","v","m³/kgda","ft³/lbda",
            "Volumen de aire húmedo por masa de aire seco.",
            "v=R_da·T_K·(1+1,607858W)/P; P en Pa.","MoistAirSpecificVolume.FromDryBulbAndHumidityRatio",
            "PsychroLib, referencia ASHRAE 2017","GetMoistAirVolume / eq. 26 (edición 2017)","Consultar código",PSYCHRO,"No trasladar numeración a la edición 2025"),
        new BibliographyEntry("B10","Desviación de entalpía","Δhdesv","kJ/kgda","Btu/lbda",
            "Corrección respecto de líneas auxiliares de entalpía de la carta.",
            "Revisar correlación y definición del código para verificar equivalencia con la carta de referencia.","EnthalpyDeviation",
            "ASHRAE Handbook—Fundamentals 2025","Capítulo 1: Psychrometrics","Pendiente",ASHRAE,"Validación bibliográfica y geométrica pendiente"),
        new BibliographyEntry("B11","Factor de calor sensible","FCS / SHR","adimensional","adimensional",
            "Razón de carga sensible a carga total cuando ambas son físicamente compatibles.",
            "Q sensible/Q total; requiere definir convención, flujo y componentes de carga.","ProcessCalculator.TryCalculate",
            SLIDES,"Diapositiva 24","No aplica",ASHRAE,"Definición y convención requieren contraste según método de equipo"),
        new BibliographyEntry("B12","Factor de bypass","BF","adimensional","adimensional",
            "Fracción ideal de bypass respecto del aire que contacta el serpentín.",
            "h2=BF·h1+(1−BF)·hADP y W2=BF·W1+(1−BF)·WADP.","ProcessCalculator.TryBypassOutlet",
            SLIDES,"Diapositiva 24","No aplica",ASHRAE,"Modelo ideal; comprobar ADP y datos de serpentín"),
        new BibliographyEntry("B13","Punto de rocío del aparato","ADP","°C","°F",
            "Estado saturado de contacto ideal de un serpentín.",
            "Se requiere temperatura/saturación de referencia del serpentín; no inferir de BF solamente.","ProcessCalculator.TryBypassOutlet",
            SLIDES,"Diapositiva 24","No aplica",ASHRAE,"Modelo ideal, sujeto a datos"),
        new BibliographyEntry("B14","Efectividad de saturación evaporativa","ε","adimensional","adimensional",
            "Fracción de acercamiento de la temperatura de salida al bulbo húmedo de entrada.",
            "ε=(Tbs1−Tbs2)/(Tbs1−Tbh1).","ProcessCalculator.TryEvaporativeOutlet",
            "ASHRAE Handbook, Evaporative Air-Cooling Equipment","Direct Evaporative Air Coolers","Consultar original",EVAP,"Término 'Hendrickson' sin validación normativa independiente"),
        new BibliographyEntry("B15","Caudal másico de aire seco","ṁda","kgda/s","lbda/h",
            "Masa de aire seco transportada por unidad de tiempo.",
            "ṁda=V̇/v, empleando el v de la corriente correspondiente.","PsychrometricUnits.FlowToDryAirMassKgS",
            SLIDES,"Diapositiva 23","No aplica",PSYCHRO,"Conversiones verificables con volumen específico"),
        new BibliographyEntry("B16","Carga por cambio de entalpía del aire","Q̇aire","kW","Btu/h",
            "Tasa de cambio de entalpía del flujo de aire húmedo.",
            "Q̇aire=ṁda·(h2−h1). Para condensación, contabilizar la entalpía del condensado.","ProcessCalculator.TryCalculate",
            SLIDES,"Diapositivas 6, 7 y 15","No aplica",ASHRAE,"Distinta de carga final de equipo si hay flujos de agua"),
        new BibliographyEntry("B17","Masa de condensado","ṁcond","kg/s","lb/h",
            "Masa de agua retirada del flujo de aire por unidad de tiempo.",
            "ṁcond=ṁda·(W1−W2) si W disminuye.","ProcessCalculator.TryCalculate",
            SLIDES,"Diapositiva 15","No aplica",ASHRAE,"Requiere caudal másico de aire seco"),
        new BibliographyEntry("B18","Mezcla adiabática de corrientes","h_mix / W_mix","kJ/kgda y kg/kgda","Btu/lbda y lb/lbda",
            "Estado de salida por conservación de masa de aire seco, vapor y entalpía.",
            "Wmix=Σ(ṁda W)/Σṁda; hmix=Σ(ṁda h)/Σṁda.","ProcessCalculator.TryMix",
            SLIDES,"Diapositiva 23","No aplica",PSYCHRO,"Hipótesis: misma presión, sin pérdidas ni condensación")
    };

    // [1.12.2] CSV auditable, sin información sujeta a copyright reproducida en extenso.
    public static string ExportCsv()
    {
        var sb=new StringBuilder();
        sb.AppendLine("Id;Variable;Simbolo;Unidad_SI;Unidad_IP;Definicion;Obtencion;Metodo_codigo;Fuente;Capitulo;Pagina;URL;Verificacion");
        foreach(var e in Entries)
        {
            var values = new[]{e.Id,e.Variable,e.Symbol,e.UnitsSI,e.UnitsIP,e.Definition,
                e.ObtainingMethod,e.MethodId,e.Source,e.Chapter,e.Page,e.Url,e.Verification};
            sb.AppendLine(string.Join(";",Array.ConvertAll(values, Escape)));
        }
        return sb.ToString();
    }
    private static string Escape(string value) => "\"" + value.Replace("\"","\"\"") + "\"";
}
