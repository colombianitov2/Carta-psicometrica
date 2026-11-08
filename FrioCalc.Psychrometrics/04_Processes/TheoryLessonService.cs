using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Processes;

// [4.15] Motor docente independiente de WPF. Todos los valores se obtienen
// del mismo calculador usado en Carta y Procesos; no se inventan estados.
// ASHRAE 2025 Fundamentals, cap. 1, §10; A03 docente diapositivas 1–24.
// Las hipótesis docentes NO constituyen dimensionamiento de equipos.
public sealed record TheoryLesson(
    string Kind, string Title, ChartPoint Initial, ChartPoint? Final,
    IReadOnlyList<ChartPoint> Route, ChartPoint? Auxiliary,
    string Explanation, string Steps, string Equations,
    string Warning, string References, string Question,
    IReadOnlyList<string> Answers, int CorrectAnswer, string AnswerExplanation);

public sealed record TheoryVariable(
    string Key, string Title, string Unit, string Definition,
    string Equation, string HowObtained, string Caution, string Reference);

public static class TheoryLessonService
{
    public const string AshraeSource =
        "ASHRAE Handbook—Fundamentals (2025), capítulo 1, §10, Typical Air-Conditioning Processes; "
        + "https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx";
    public const string PsychroLibSource =
        "PsychroLib, documentación técnica: https://psychrometrics.github.io/psychrolib/api_docs.html";
    public const string A03Source = "Material docente A03 Procesos Psicrométricos, diapositivas 1–24 (archivo aportado por el usuario).";

    public static IReadOnlyList<TheoryVariable> Variables {get;} = new[]
    {
        new TheoryVariable("Tbs","Temperatura de bulbo seco","°C",
            "Temperatura termodinámica del aire, medida por un sensor protegido de radiación.",
            "Tbs = lectura de temperatura; T_K = Tbs + 273,15.",
            "Termómetro o sensor calibrado; en carta es el eje horizontal.",
            "No determina por sí sola humedad absoluta ni entalpía.",AshraeSource),
        new TheoryVariable("Tbh","Temperatura de bulbo húmedo termodinámico","°C",
            "Temperatura asociada a saturación adiabática del aire húmedo.",
            "W = f(Tbs, Tbh, P); el cálculo inverso busca Tbh con la W conocida.",
            "Lectura psicrométrica corregida o solución de ecuaciones de humedad.",
            "No equivale exactamente a una trayectoria de entalpía constante.",PsychroLibSource),
        new TheoryVariable("Tdp","Temperatura de punto de rocío","°C",
            "Temperatura de saturación a la presión parcial de vapor actual.",
            "p_v = P·W/(0,621945+W); p_ws(Tdp) = p_v.",
            "Medición de rocío o inversión de la curva de presión de saturación.",
            "Con W=0, el punto de rocío no está definido.",AshraeSource),
        new TheoryVariable("RH","Humedad relativa","%",
            "Relación entre la presión parcial del vapor y la presión de saturación a la misma Tbs.",
            "HR = 100·p_v/p_ws(Tbs).",
            "Higrómetro calibrado o cálculo a partir de Tbs, W y P.",
            "Puede caer al calentar aun cuando no se retire una sola gota de agua.",PsychroLibSource),
        new TheoryVariable("W","Humedad absoluta (razón de humedad)","g/kg de aire seco",
            "Masa de vapor de agua dividida por masa de aire seco.",
            "W = 0,621945·p_v/(P−p_v). Mostrar 1000·W en g/kgda.",
            "Se determina con dos propiedades independientes y presión.",
            "No confundir con HR; W puede permanecer constante aunque HR varíe.",AshraeSource),
        new TheoryVariable("h","Entalpía del aire húmedo","kJ/kg de aire seco",
            "Energía específica convencional de aire húmedo referida al aire seco.",
            "h = 1,006·Tbs + W·(2501+1,86·Tbs).",
            "Balance de aire seco y vapor; los coeficientes SI son aproximados.",
            "Una potencia requiere flujo másico de aire seco; en condensación interviene la entalpía del agua drenada.",PsychroLibSource),
        new TheoryVariable("v","Volumen específico","m³/kg de aire seco",
            "Volumen ocupado por aire húmedo por kg de aire seco.",
            "v = R_da·T_K·(1+1,607858·W)/P (P en unidades coherentes).",
            "Cálculo con presión absoluta, Tbs y W.",
            "No promediar directamente caudales volumétricos al mezclar estados distintos.",PsychroLibSource),
        new TheoryVariable("P","Presión atmosférica absoluta","kPa absolutos",
            "Presión total del aire húmedo, que condiciona saturación y propiedades.",
            "P = presión barométrica local; nunca sustituir por presión manométrica.",
            "Barómetro de estación o dato de presión absoluta.",
            "La carta y los puntos se deben recalcular cuando cambia la presión.",AshraeSource),
        new TheoryVariable("SHR","Factor de calor sensible FCS/SHR","adimensional",
            "Relación de transferencia sensible y transferencia térmica total para un balance coherente.",
            "FCS = Q_sensible/Q_total; requiere definición del sistema y del balance.",
            "Cargas sensible y total, medidas o calculadas por diseño.",
            "FCS no fija un estado único de impulsión sin datos adicionales.",AshraeSource),
        new TheoryVariable("BF","Factor de bypass y ADP","adimensional / °C",
            "Modelo ideal del serpentín: mezcla de aire que no contactó la superficie con aire al ADP saturado.",
            "h2=BF·h1+(1−BF)·hADP; W2=BF·W1+(1−BF)·WADP.",
            "BF/ADP del equipo o valores dados en el ejercicio; requiere los dos.",
            "Conocer solo BF=0,20 no identifica el estado de salida.",AshraeSource),
        new TheoryVariable("s","Entropía específica relativa s*","kJ/(kgda·K)",
            "Entropía relativa de la mezcla ideal con referencias explícitas.",
            "s*=cp_da·ln(T/T*)−R_da·ln(p_da/p_da*)+W[cp_v·ln(T/T*)−R_v·ln(p_v/p_v*)].",
            "Modelo ideal de FrioCalc, misma presión de referencia para comparar estados.",
            "No equivale a la desviación de entalpía ni a la entropía absoluta del modelo real.",
            "Modelo relativo de FrioCalc; https://coolprop.org/fluid_properties/HumidAir.html"),
        new TheoryVariable("dh","Desviación de entalpía Δh","kJ/kgda",
            "Corrección auxiliar respecto del estado saturado a la temperatura de bulbo húmedo.",
            "Δh = h(Tbs,W) − h_sat(Tbh), según la convención usada por FrioCalc.",
            "Cálculo de estados y referencia saturada.",
            "No es entropía. Confirmar convención al comparar con una carta específica.",
            AshraeSource)
    };

    public static bool TryEvaluate(string kind,double dryBulbC,double relativeHumidityPercent,
        double intensity,double pressureKPa,out TheoryLesson? lesson,out string error,
        double secondDryBulbC=35,double secondRelativeHumidityPercent=60)
    {
        lesson=null;error="";
        if(!double.IsFinite(dryBulbC)||!double.IsFinite(relativeHumidityPercent)||
           !double.IsFinite(intensity)||!double.IsFinite(pressureKPa)||
           relativeHumidityPercent<=0||relativeHumidityPercent>100||
           intensity<0||intensity>1||pressureKPa<=0)
        {error="Compruebe Tbs, HR entre 0 y 100 %, intensidad entre 0 y 1 y presión absoluta positiva.";return false;}

        if(kind=="Mixing" && (!double.IsFinite(secondDryBulbC) ||
            !double.IsFinite(secondRelativeHumidityPercent) ||
            secondRelativeHumidityPercent<=0 || secondRelativeHumidityPercent>100))
        {error="La segunda corriente requiere temperatura y humedad relativa válidas.";return false;}

        try
        {
            double w=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(
                dryBulbC,relativeHumidityPercent/100.0,pressureKPa);
            if(!Make(1,"P1",pressureKPa,dryBulbC,w,out var start))
            {error="El estado inicial no es físicamente válido a esta presión.";return false;}
            ChartPoint? finish=null,aux=null;
            var route=new List<ChartPoint>{start!};
            string title="",description="",steps="",formula="",warning="";
            string question="",reason="";
            string[] options=Array.Empty<string>();
            int correct=0;
            double nominalT,humidityWater;
            switch(kind)
            {
                case "Heating":
                    title="Calefacción sensible";
                    nominalT=start!.DryBulbC+20*intensity;
                    if(!Make(2,"P2",pressureKPa,nominalT,w,out finish))
                    {error="La salida calentada está fuera del dominio del modelo.";return false;}
                    description="Se añade energía sin agregar agua. La humedad absoluta W y el punto de rocío permanecen constantes; la HR disminuye al elevar Tbs.";
                    formula="W2 = W1; h = 1,006·T + W·(2501+1,86·T); q_específica = h2−h1.";
                    steps=$"1. W1 = {w*1000:0.###} g/kgda.\n2. T2 = {dryBulbC:0.##} + 20·{intensity:0.##} = {finish!.DryBulbC:0.##} °C.\n" +
                        $"3. h2−h1 = {finish.EnthalpyKJPerKgDryAir:0.###} − {start.EnthalpyKJPerKgDryAir:0.###} = " +
                        $"{finish.EnthalpyKJPerKgDryAir-start.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n" +
                        $"4. HR: {start.RelativeHumidityPercent:0.##} % → {finish.RelativeHumidityPercent:0.##} %; no cambió W.";
                    warning="Sin caudal másico no se puede expresar la potencia en kW. En esta demostración se desprecia el intercambio de humedad.";
                    (question,options,correct,reason)=
                        ("Al calentar sin añadir agua, ¿qué pasa con W?",
                         new[]{"Permanece constante","Aumenta","Disminuye"},0,
                         "Se conserva la masa de vapor por masa de aire seco; solo cambia la temperatura y, con ella, la HR.");
                    break;

                case "Cooling":
                    title="Enfriamiento sensible";
                    nominalT=dryBulbC-20*intensity;
                    bool dewReached=double.IsFinite(start!.DewPointC) && nominalT<start.DewPointC;
                    double appliedT=dewReached?start.DewPointC:nominalT;
                    if(!Make(2,"P2",pressureKPa,appliedT,w,out finish))
                    {error="La salida enfriada está fuera del dominio del modelo.";return false;}
                    description="Mientras W es constante, el aire pierde energía y aumenta la HR. Al alcanzar el punto de rocío, no puede continuar enfriándose a W constante sin condensación.";
                    formula="W2 = W1 (sin condensación); q_específica = h2−h1; HR2=100·p_v/p_ws(T2).";
                    steps=$"1. T2 solicitado = {dryBulbC:0.##} − 20·{intensity:0.##} = {nominalT:0.##} °C.\n" +
                        $"2. Punto de rocío de entrada = {start.DewPointC:0.##} °C.\n" +
                        $"3. Temperatura de salida válida = {appliedT:0.##} °C; W2 = {finish!.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                        $"4. h2−h1 = {finish.EnthalpyKJPerKgDryAir-start.EnthalpyKJPerKgDryAir:0.###} kJ/kgda; HR2 = {finish.RelativeHumidityPercent:0.##} %.";
                    warning=dewReached
                        ?"Se alcanzó el punto de rocío. La temperatura se LIMITÓ a ese valor. Para enfriar más, seleccione «Enfriamiento y deshumidificación»."
                        :"Modelo válido solo hasta la primera condensación. Sin flujo másico no hay potencia en kW.";
                    (question,options,correct,reason)=
                        ("¿Qué ocurre si el aire sigue enfriándose por debajo de su punto de rocío?",
                         new[]{"La HR queda invariable","Comienza la condensación","Aumenta W sin aporte de agua"},1,
                         "La presión parcial de vapor alcanza la saturación y parte del vapor se elimina como condensado.");
                    break;

                case "Evaporative":
                    title="Humidificación evaporativa ideal";
                    if(!ProcessCalculator.TryEvaporativeOutlet(start!,intensity,2,"#2563EB",
                        out finish,out error)||finish is null)return false;
                    description="La evaporación introduce vapor y usa energía del aire para el cambio de fase. En el modelo ideal el aire se enfría acercándose al bulbo húmedo.";
                    formula="T2=T1−ε·(T1−Tbh1); W2=W1+ε·[W_sat(Tbh1,P)−W1].";
                    steps=$"1. Tbh1 = {start!.WetBulbC:0.###} °C, ε = {intensity:0.##}.\n" +
                        $"2. T2 = {start.DryBulbC:0.##} − {intensity:0.##}·({start.DryBulbC:0.##}−{start.WetBulbC:0.##}) = {finish.DryBulbC:0.###} °C.\n" +
                        $"3. W1={start.HumidityRatioKgPerKg*1000:0.###}; W2={finish.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                        $"4. HR2={finish.RelativeHumidityPercent:0.###} %, h2−h1={finish.EnthalpyKJPerKgDryAir-start.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.";
                    warning="Efectividad de saturación en un modelo de aproximación ideal, no eficiencia universal de cualquier lavador. Requiere balance de agua y datos del fabricante.";
                    (question,options,correct,reason)=
                        ("¿Qué ocurre cuando ε llega a 1 en este modelo?",
                         new[]{"Se alcanza el estado saturado ideal","La temperatura asciende 20 °C","La humedad absoluta se vuelve cero"},0,
                         "La temperatura se aproxima a Tbh1 y W al valor saturado correspondiente.");
                    break;

                case "Steam":
                    title="Humidificación mediante inyección de vapor";
                    humidityWater=.003*intensity;
                    if(!ProcessCalculator.TrySteamOutlet(start!,humidityWater,2700.0,
                        2,"#2563EB",out finish,out error)||finish is null)return false;
                    description="Se incorpora masa de vapor y su entalpía. La temperatura NO tiene por qué permanecer constante: depende de la entalpía específica del vapor inyectado.";
                    formula="W2=W1+ΔW; h2=h1+ΔW·h_vapor; h_vapor=2700 kJ/kg (supuesto docente).";
                    steps=$"1. Vapor añadido ΔW={humidityWater*1000:0.###} g/kgda = {humidityWater:0.######} kg/kgda.\n" +
                        $"2. h2={start!.EnthalpyKJPerKgDryAir:0.###}+{humidityWater:0.######}·2700 = {finish.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n" +
                        $"3. W2={finish.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                        $"4. T2={finish.DryBulbC:0.###} °C y HR2={finish.RelativeHumidityPercent:0.###} %.";
                    warning="Se supuso vapor con h=2700 kJ/kg; su estado real, disponibilidad y pérdidas no se determinaron. Puede haber estados sobresaturados que deben rechazarse.";
                    (question,options,correct,reason)=
                        ("¿Qué dato energético se necesita para el balance del vapor añadido?",
                         new[]{"Solo HR exterior","Entalpía específica del vapor","Solo temperatura del edificio"},1,
                         "La corriente de vapor transporta energía, de modo que h2=h1+ΔW·h_vapor.");
                    break;

                case "Dehumidification":
                    title="Enfriamiento y deshumidificación";
                    if(!double.IsFinite(start!.DewPointC)){error="No se puede determinar el punto de rocío de entrada.";return false;}
                    if(!Saturated(3,"Rocío",pressureKPa,start.DewPointC,out aux))
                    {error="El punto de rocío está fuera del dominio.";return false;}
                    nominalT=start.DewPointC-12*intensity;
                    if(!Saturated(2,"P2",pressureKPa,nominalT,out finish))
                    {error="El punto de salida está fuera del dominio de saturación.";return false;}
                    description="Primero se enfría hasta el punto de rocío (sin cambiar W). Después, al continuar enfriando, el exceso de vapor condensa y W disminuye.";
                    formula="W2=W_sat(T2,P); m_agua/m_da=W1−W2; q_aire=h2−h1.";
                    steps=$"1. Trocío1={start.DewPointC:0.###} °C, W1={start.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                        $"2. Enfriar hasta rocío: T={aux!.DryBulbC:0.###} °C sin retirar agua.\n" +
                        $"3. Enfriamiento adicional = 12·{intensity:0.##} = {12*intensity:0.###} °C → T2={finish!.DryBulbC:0.###} °C saturado.\n" +
                        $"4. Agua condensada={(start.HumidityRatioKgPerKg-finish.HumidityRatioKgPerKg)*1000:0.###} g/kgda.\n" +
                        $"5. Diferencia de entalpía del aire={finish.EnthalpyKJPerKgDryAir-start.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.";
                    warning="Esta demostración toma una salida saturada ideal. La carga real del serpentín requiere flujo másico, temperatura/entalpía del condensado y datos del equipo.";
                    (question,options,correct,reason)=
                        ("¿En qué momento comienza la deshumidificación por enfriamiento?",
                         new[]{"Desde cualquier temperatura superior al rocío","Solo después de superar el punto de rocío hacia abajo","Al aumentar Tbs"},1,
                         "Al bajar por debajo del punto de rocío se retira vapor como líquido para mantener saturación.");
                    route.Add(aux!);
                    break;

                case "Mixing":
                    title="Mezcla adiabática de dos corrientes";
                    double otherW=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(
                        secondDryBulbC,secondRelativeHumidityPercent/100,pressureKPa);
                    if(!Make(3,"Exterior",pressureKPa,secondDryBulbC,otherW,out aux))
                    {error="La corriente exterior del ejemplo no es válida a esta presión.";return false;}
                    if(intensity==0) finish=start! with {Id=2,Name="Mezcla"};
                    else if(intensity==1) finish=aux! with {Id=2,Name="Mezcla"};
                    else if(!ProcessCalculator.TryMix(new List<(ChartPoint State,double DryAirMassFlowKgS)>
                    {(start!,1-intensity),(aux!,intensity)},2,"#2563EB",out finish,out error)||finish is null) return false;
                    description="Las corrientes se mezclan sin intercambio de calor externo ni condensación. El peso de cada propiedad es el flujo MÁSICO de aire seco, nunca el caudal volumétrico bruto.";
                    formula="ṁda,total=Σṁda; W3=Σ(ṁda·W)/Σṁda; h3=Σ(ṁda·h)/Σṁda.";
                    steps=$"1. Estado 1: T={start!.DryBulbC:0.###} °C, W={w*1000:0.###} g/kgda.\n" +
                        $"2. Estado exterior: T={secondDryBulbC:0.##} °C, HR={secondRelativeHumidityPercent:0.##} %, W={aux!.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                        $"3. Fracciones de m_da: m1={1-intensity:0.###}; m2={intensity:0.###} (escala normalizada).\n" +
                        $"4. W3=({1-intensity:0.###}·{w*1000:0.###}+{intensity:0.###}·{aux.HumidityRatioKgPerKg*1000:0.###})={finish!.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                        $"5. h3={finish.EnthalpyKJPerKgDryAir:0.###} kJ/kgda; T3={finish.DryBulbC:0.###} °C.";
                    warning="Fracciones normalizadas de masa seca, no caudales reales. Para potencias en kW se necesita un caudal y se debe verificar ausencia de condensación.";
                    (question,options,correct,reason)=
                        ("¿Por qué no se promedian únicamente los m³/h de ambas corrientes?",
                         new[]{"Porque sus volúmenes específicos pueden diferir","Porque toda mezcla es isotérmica","Porque la presión no influye"},0,
                         "El balance es por masa de aire seco: ṁda=V̇/v para cada corriente.");
                    break;

                case "HeatingHumidification":
                    title="Calentamiento seguido de humidificación";
                    nominalT=dryBulbC+15*intensity;
                    if(!Make(3,"Calentado",pressureKPa,nominalT,w,out aux))
                    {error="Estado intermedio de calefacción inválido.";return false;}
                    humidityWater=.0015*intensity;
                    if(!ProcessCalculator.TrySteamOutlet(aux!,humidityWater,2700,2,"#2563EB",
                        out finish,out error)||finish is null)return false;
                    route.Add(aux!);
                    description="Transformación de invierno en dos etapas: calefacción sensible con W constante y después aportación de vapor y energía.";
                    formula="Etapa A: W_A=W1, h_A=h(T_A,W1). Etapa B: W2=W_A+ΔW; h2=h_A+ΔW·h_vapor.";
                    steps=$"1. Calentar de {dryBulbC:0.##} a {aux!.DryBulbC:0.##} °C; W={w*1000:0.###} g/kgda constante.\n" +
                        $"2. HR intermedia={aux!.RelativeHumidityPercent:0.###} %.\n" +
                        $"3. Añadir {humidityWater*1000:0.###} g de vapor/kgda a h_vapor=2700 kJ/kg.\n" +
                        $"4. T2={finish.DryBulbC:0.###} °C, W2={finish.HumidityRatioKgPerKg*1000:0.###} g/kgda, HR2={finish.RelativeHumidityPercent:0.###} %.";
                    warning="El orden importa. No se han calculado pérdidas, potencia de generador de vapor ni cargas de ocupación.";
                    (question,options,correct,reason)=
                        ("¿Qué permanece constante durante la primera etapa de calentamiento?",
                         new[]{"W","HR","La entalpía"},0,
                         "El serpentín aporta calor sin incorporar agua durante la etapa sensible.");
                    break;

                case "Desiccant":
                    title="Deshumidificación por desecante: datos necesarios";
                    humidityWater=w*.6*intensity;
                    nominalT=dryBulbC+8*intensity;
                    if(!ProcessCalculator.TryDesiccantOutlet(start!,humidityWater,nominalT,2,
                        "#2563EB",out finish,out error)||finish is null)return false;
                    description="Un desecante adsorbe vapor y reduce W. Aquí se muestran estados con una cantidad retirada y Tbs de salida SUPUESTAS, no una predicción del funcionamiento de un material.";
                    formula="W2=W1−(masa de agua retirada/masa de aire seco); T2 se debe proporcionar.";
                    steps=$"1. W1={w*1000:0.###} g/kgda.\n" +
                        $"2. Se SUPONE extracción de {humidityWater*1000:0.###} g/kgda.\n" +
                        $"3. Se SUPONE T2={nominalT:0.###} °C (no calculada por equilibrio de adsorción).\n" +
                        $"4. W2={finish.HumidityRatioKgPerKg*1000:0.###} g/kgda, HR2={finish.RelativeHumidityPercent:0.###} %.";
                    warning="Sin isoterma de sorción, caudal, calor de adsorción, características del lecho y regeneración NO se puede predecir temperatura, capacidad o potencia real.";
                    (question,options,correct,reason)=
                        ("¿Qué parámetro es imprescindible para predecir un desecante real?",
                         new[]{"Una temperatura exterior cualquiera","Datos del material y regeneración","Solo el color del rotor"},1,
                         "La capacidad y energía del desecante dependen de sus isotermas y condiciones reales.");
                    break;

                case "Bypass":
                    title="Bypass ideal del serpentín: BF y ADP";
                    if(!double.IsFinite(start!.DewPointC))
                    {error="No se puede construir ADP sin un estado de entrada con vapor.";return false;}
                    double adp=Math.Max(-60,start.DewPointC-4);
                    if(!Saturated(3,"ADP",pressureKPa,adp,out aux))
                    {error="ADP supuesto fuera del dominio de saturación.";return false;}
                    double bf=1-intensity;
                    if(!ProcessCalculator.TryBypassOutlet(start,adp,bf,2,"#2563EB",
                        out finish,out error)||finish is null)return false;
                    description="Modelo ideal de una fracción de aire que pasa sin contacto y otra que se aproxima al ADP saturado. BF no es el factor de calor sensible.";
                    formula="W2=BF·W1+(1−BF)·W_ADP; h2=BF·h1+(1−BF)·h_ADP.";
                    steps=$"1. Supuesto docente: ADP=Trocío1−4={adp:0.###} °C, saturado.\n" +
                        $"2. BF=1−intensidad=1−{intensity:0.##}={bf:0.###}.\n" +
                        $"3. W2={bf:0.###}·{w*1000:0.###}+{1-bf:0.###}·{aux!.HumidityRatioKgPerKg*1000:0.###}={finish.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                        $"4. h2={finish.EnthalpyKJPerKgDryAir:0.###} kJ/kgda; T2={finish.DryBulbC:0.###} °C.";
                    warning="ADP es SUPUESTO para visualizar. Un BF conocido sin ADP y datos del equipo no determina un estado de impulsión.";
                    (question,options,correct,reason)=
                        ("Si solo conocemos BF=0,20, ¿podemos fijar la impulsión?",
                         new[]{"Sí, con BF basta","No, faltan ADP y otras condiciones","Solo con la hora del día"},1,
                         "BF es una fracción de mezcla ideal, no un punto termodinámico.");
                    break;

                case "SHR":
                    title="Factor de calor sensible (FCS) — estados no determinados";
                    description="FCS describe la razón sensible/total. Por sí solo puede indicar una dirección en la carta desde el estado del local, pero no una posición única de suministro.";
                    formula="FCS=Q_sensible/Q_total. No existe un T2 único deducible de FCS únicamente.";
                    steps=$"1. Estado conocido: {dryBulbC:0.###} °C, {relativeHumidityPercent:0.###}% HR y P={pressureKPa:0.###} kPa.\n" +
                        $"2. FCS indicado={intensity:0.###}. Una familia de posibles salidas cumple ese factor.\n" +
                        "3. Se necesitan caudal, carga sensible/latente y datos del equipo para localizar un estado real.";
                    warning="Resultado indeterminado deliberadamente: no se debe dibujar un punto de impulsión ficticio.";
                    (question,options,correct,reason)=
                        ("¿Basta un FCS=0,75 para calcular un punto de impulsión?",
                         new[]{"Sí","No, faltan datos de carga y caudal","Solo si HR es 50 %"},1,
                         "El FCS fija una relación de calor, no determina por sí solo el estado de suministro.");
                    break;

                default:
                    error="Proceso docente no reconocido.";return false;
            }

            if(finish is not null)route.Add(finish);
            if(finish is null && kind!="SHR"){error="No se pudo determinar una salida físicamente válida.";return false;}
            lesson=new TheoryLesson(kind,title,start!,finish,route,aux,description,steps,
                formula,warning,AshraeSource+"; "+A03Source,
                question,options,correct,reason);
            return true;
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or OverflowException)
        {error="La combinación de datos no puede resolverse en el dominio del modelo: "+ex.Message;return false;}
    }

    public static string DescribeVariable(string key,ChartPoint? state)
    {
        var variable=Variables.FirstOrDefault(v=>v.Key==key);
        if(variable is null) return "Seleccione una variable de la carta.";
        string value="No calculado";
        if(state is not null)
        {
            value=key switch
            {
                "Tbs"=>$"{state.DryBulbC:0.###} °C",
                "Tbh"=>$"{state.WetBulbC:0.###} °C",
                "Tdp"=>double.IsFinite(state.DewPointC)?$"{state.DewPointC:0.###} °C":"No definido",
                "RH"=>$"{state.RelativeHumidityPercent:0.###} %",
                "W"=>$"{state.HumidityRatioKgPerKg*1000:0.###} g/kgda",
                "h"=>$"{state.EnthalpyKJPerKgDryAir:0.###} kJ/kgda",
                "v"=>$"{state.SpecificVolumeM3PerKgDryAir:0.######} m³/kgda",
                "P"=>$"{state.PressureKPa:0.###} kPa abs",
                _=>"Depende de datos del proceso o de referencias explícitas"
            };
        }
        return $"{variable.Title} [{variable.Unit}]\n{variable.Definition}\n\n" +
            $"Ecuación: {variable.Equation}\nObtención: {variable.HowObtained}\n" +
            $"En el estado inicial: {(state is null?"No calculado":value)}\n" +
            $"Precaución: {variable.Caution}\nReferencia: {variable.Reference}";
    }

    private static bool Make(int id,string name,double p,double t,double w,out ChartPoint? point)
        => PointPropertyCalculator.TryCreatePoint(id,name,"#2563EB",p,t,w,out point)
           && point is not null;

    private static bool Saturated(int id,string name,double p,double t,out ChartPoint? point)
    {
        point=null;
        if(t < SaturationVaporPressure.MinSupportedTemperatureC ||
           t > SaturationVaporPressure.MaxSupportedTemperatureC)return false;
        double pws=SaturationVaporPressure.GetPressureKPa(t);
        if(pws>=p)return false;
        double w=SaturationHumidityRatio.FromPressuresKPa(p,pws);
        return Make(id,name,p,t,w,out point);
    }
}
