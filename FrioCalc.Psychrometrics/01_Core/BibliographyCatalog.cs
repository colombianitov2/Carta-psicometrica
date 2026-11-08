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
    private const string ASHRAE="https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx";
    private const string DESICCANTS="https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch32/F25_Ch32_si.aspx";
    private const string PSYCHRO="https://psychrometrics.github.io/psychrolib/api_docs.html";
    private const string COOLPROP="https://coolprop.org/fluid_properties/HumidAir.html";
    private const string EVAP="https://handbook.ashrae.org/handbooks/S20/SI/s20_ch41/s20_ch41_si.aspx";
    private const string SLIDES="Material docente A03 Procesos Psicrométricos, diapositivas 1–24 (suministrado por el usuario).";

    public static IReadOnlyList<BibliographyEntry> Entries {get;} = new []
    {
        new BibliographyEntry("B01","Presión atmosférica absoluta","P","kPa abs","psi abs",
            "Presión termodinámica absoluta de la mezcla.","Medición barométrica o presión de estación a la altitud real.","AtmosphericPressure",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §2: Standard Atmosphere","HTML sin número de página",ASHRAE,"Capítulo íntegro consultado en ASHRAE Handbook Online"),
        new BibliographyEntry("B02","Bulbo seco","Tbs","°C","°F",
            "Temperatura del aire medida sin influencia de evaporación en el sensor.",
            "Termómetro de bulbo seco, sonda de temperatura calibrada.","ChartPoint.DryBulbC",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §3: Moist Air Definitions","HTML sin número de página",ASHRAE,"Definición verificada en capítulo oficial"),
        new BibliographyEntry("B03","Bulbo húmedo termodinámico","Tbh","°C","°F",
            "Temperatura de equilibrio asociada al proceso de saturación adiabática.",
            "Resolver W(Tbs,Tbh,P) o invertir desde Tbs,W,P.","WetBulbHumidityRatio.FromDryBulbAndWetBulb",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §7; ecuación (33)","HTML sin número de página",ASHRAE,"Modelo termodinámico de bulbo húmedo confirmado"),
        new BibliographyEntry("B04","Punto de rocío","Trocío","°C","°F",
            "Temperatura a la que el vapor alcanza la saturación a presión parcial dada.",
            "Invertir presión de vapor de saturación; sensor óptico de punto de rocío.","PointPropertyCalculator.GetDewPointFromVaporPressure",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §7; ecuación (38)","HTML sin número de página",ASHRAE,"Definición por saturación confirmada"),
        new BibliographyEntry("B05","Humedad relativa","φ / HR","%","%",
            "Relación entre presión parcial de vapor y presión de saturación a Tbs.",
            "Higrómetro o cálculo de Pv/Pws(Tbs)·100.","RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §6; ecuación (23)","HTML sin número de página",ASHRAE,"Relación de humedad relativa confirmada"),
        new BibliographyEntry("B06","Razón de humedad W (rotulada «humedad absoluta» en la interfaz)","W","kg/kgda o g/kgda","lb/lbda o gr/lbda",
            "Masa de vapor de agua por masa de aire seco.",
            "W=0,621945·Pv/(P−Pv), con presiones absolutas coherentes.","SaturationHumidityRatio.FromPressuresKPa",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §6; ecuación (21)","HTML sin número de página",ASHRAE,"W es razón de humedad por masa de aire seco, distinta de humedad absoluta en kg/m³"),
        new BibliographyEntry("B07","Presión de vapor de saturación","Pws","kPa","psi",
            "Presión de equilibrio sobre agua o hielo, según temperatura.",
            "Ecuaciones de correlación de saturación de ASHRAE.","SaturationVaporPressure.GetPressureKPa",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §4; ecuaciones (5) y (6)","HTML sin número de página",ASHRAE,"Saturación sobre agua y hielo verificada"),
        new BibliographyEntry("B08","Entalpía de aire húmedo","h","kJ/kgda","Btu/lbda",
            "Entalpía por unidad de masa de aire seco con referencia convencional.",
            "h=1,006·Tbs+W·(2501+1,86·Tbs), SI aproximado.","MoistAirEnthalpy.FromDryBulbAndHumidityRatio",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §6; ecuaciones (29)–(32)","HTML sin número de página",ASHRAE,"Aproximación de gas ideal confirmada en edición 2025"),
        new BibliographyEntry("B09","Volumen específico del aire húmedo","v","m³/kgda","ft³/lbda",
            "Volumen de aire húmedo por masa de aire seco.",
            "v=R_da·T_K·(1+1,607858W)/P; P en Pa.","MoistAirSpecificVolume.FromDryBulbAndHumidityRatio",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §6; ecuaciones (25)–(28)","HTML sin número de página",ASHRAE,"Volumen por masa de aire seco confirmado"),
        new BibliographyEntry("B10","Desviación de entalpía","Δhdesv","kJ/kgda","Btu/lbda",
            "Corrección respecto de líneas auxiliares de entalpía de la carta.",
            "Revisar correlación y definición del código para verificar equivalencia con la carta de referencia.","EnthalpyDeviation",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §9; ver transportador de la carta","HTML sin número de página",ASHRAE,"El transportador figura en §9; la correlación exacta requiere auditoría del código"),
        new BibliographyEntry("B11","Factor de calor sensible","FCS / SHR","adimensional","adimensional",
            "Razón de carga sensible a carga total cuando ambas son físicamente compatibles.",
            "Q sensible/Q total; requiere definir convención, flujo y componentes de carga.","ProcessCalculator.TryCalculate",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §9; relación sensible/total","HTML sin número de página",ASHRAE,"La definición del transportador está confirmada; convención de cada equipo pendiente"),
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
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §10; ecuaciones (43)–(45)","HTML sin número de página",ASHRAE,"Balances de calentamiento y serpentín con condensado verificados"),
        new BibliographyEntry("B17","Masa de condensado","ṁcond","kg/s","lb/h",
            "Masa de agua retirada del flujo de aire por unidad de tiempo.",
            "ṁcond=ṁda·(W1−W2) si W disminuye.","ProcessCalculator.TryCalculate",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §10; ecuación (44)","HTML sin número de página",ASHRAE,"Balance de masa de agua para serpentín verificado"),
        new BibliographyEntry("B18","Mezcla adiabática de corrientes","h_mix / W_mix","kJ/kgda y kg/kgda","Btu/lbda y lb/lbda",
            "Estado de salida por conservación de masa de aire seco, vapor y entalpía.",
            "Wmix=Σ(ṁda W)/Σṁda; hmix=Σ(ṁda h)/Σṁda.","ProcessCalculator.TryMix",
            "ASHRAE Handbook—Fundamentals 2025","Cap. 1, §10; ecuación (46)","HTML sin número de página",ASHRAE,"Mezcla adiabática de corrientes verificada"),
        new BibliographyEntry("B19","Deshumidificación por desecante","ΔW / Tbs2","kg/kgda y °C","lb/lbda y °F",
            "Retirada de vapor mediante sorción; exige propiedades y regeneración del material.",
            "Introducir humedad retirada y Tbs de salida medidos/garantizados; dimensionamiento mediante isotermas de sorción y datos del fabricante.",
            "ProcessCalculator.TryDesiccantOutlet","ASHRAE Handbook—Fundamentals 2025",
            "Cap. 32: Sorbents and Desiccants, §§1–3","HTML sin número de página",DESICCANTS,
            "Modelo de estado verificado; capacidad/regeneración no calculadas"),
        new BibliographyEntry("B20","División de aire en dos ramas","m1 / m2","kgda/s","lbda/h",
            "La división conserva la masa total de aire seco; cambios de estado en cada rama requieren aportes de masa/energía.",
            "ṁda,1=f·ṁda,total; ṁda,2=(1−f)·ṁda,total; comparar entradas y salidas por rama.",
            "ProcessCalculator.TryBranchBalance","Balance de masa y energía en flujo estacionario",
            "Aplicación de balances por componente","No aplica",ASHRAE,
            "Balance algebraico implementado; pérdidas de ducto y equipos no calculadas")
        new BibliographyEntry("B21","Entropía específica relativa del aire húmedo ideal","s*",
            "kJ/(kgda·K)","Btu/(lbda·°F)",
            "Suma de las contribuciones de entropía de gases ideales por masa de aire seco; referencia convencional definida en código.",
            "c_p,da ln(T/T*)−R_da ln(p_da/p_da*)+W[c_p,v ln(T/T*)−R_v ln(p_v/p_v*)].",
            "MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio",
            "CoolProp Humid Air Properties y modelo clásico de mezcla de gases ideales",
            "Entropy Sda y referencias; comparar con modelos no ideales de RP-1485",
            "HTML, sin página impresa",
            "https://coolprop.org/fluid_properties/HumidAir.html",
            "Modelo ideal relativo propio; NO atribuir sus referencias arbitrarias a ASHRAE"),
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
