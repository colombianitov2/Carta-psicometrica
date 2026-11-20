using System;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Processes;

// [4.18] Presentación educativa coherente con el motor (referencia h=0 a 0 °C para aire seco).
public static class EducationalUnits
{
    public static string TemperatureUnit(UnitSystem u)=>u==UnitSystem.IP?"°F":"°C";
    public static string PressureUnit(UnitSystem u)=>u==UnitSystem.IP?"psia":"kPa";
    public static string HumidityUnit(UnitSystem u)=>u==UnitSystem.IP?"gr/lb aire seco":"g/kg aire seco";
    public static string EnthalpyUnit(UnitSystem u)=>u==UnitSystem.IP?"Btu/lb aire seco":"kJ/kg aire seco";
    public static string VolumeUnit(UnitSystem u)=>u==UnitSystem.IP?"ft³/lb aire seco":"m³/kg aire seco";
    public static string EntropyUnit(UnitSystem u)=>u==UnitSystem.IP?"Btu/(lb aire seco·°R)":"kJ/(kg aire seco·K)";
    public static double T(double v,UnitSystem u)=>PsychrometricUnits.TemperatureFromSI(v,u);
    public static double P(double v,UnitSystem u)=>PsychrometricUnits.PressureFromSI(v,u);
    public static double W(double v,UnitSystem u)=>PsychrometricUnits.HumidityFromSI(v,u);
    public static double H(double v,UnitSystem u)=>PsychrometricUnits.EnthalpyFromSI(v,u);
    public static double V(double v,UnitSystem u)=>PsychrometricUnits.VolumeFromSI(v,u);
    public static double DT(double v,UnitSystem u)=>v*(u==UnitSystem.IP?1.8:1);
    public static string Temp(double v,UnitSystem u)=>$"{T(v,u):0.###} {TemperatureUnit(u)}";
    public static string Press(double v,UnitSystem u)=>$"{P(v,u):0.###} {PressureUnit(u)}";
    public static string Hum(double v,UnitSystem u)=>$"{W(v,u):0.###} {HumidityUnit(u)}";
    public static string Enth(double v,UnitSystem u)=>$"{H(v,u):0.###} {EnthalpyUnit(u)}";

    public static AshraeEquation Equation(AshraeEquation e,UnitSystem u)
    {
        if(u==UnitSystem.SI)return e;
        string units=e.Units.Replace("kg vapor/kg aire seco","lb vapor/lb aire seco")
            .Replace("kg/kgda","lb/lbda").Replace("kg/kg aire seco","lb/lb aire seco")
            .Replace("kg/kg mezcla","lb/lb mezcla").Replace("kgda/s","lbda/h")
            .Replace("kg/s","lb/h").Replace("kJ/(kg aire seco·K)","Btu/(lb aire seco·°R)")
            .Replace("kJ/kg aire seco","Btu/lb aire seco").Replace("kJ/kgda","Btu/lbda")
            .Replace("m³/kgda","ft³/lbda").Replace("kg/m³","lb/ft³")
            .Replace("kPa","psia").Replace("kW","Btu/h").Replace("°C","°F");
        if(units=="kg")units="lb";
        var result=e with {Units=units,Origin=UnitText(e.Origin),Derivation=UnitText(e.Derivation)};
        return e.Key switch
        {
            "PwsIce"=>result with {Numerator="−10214,16462 T⁻¹ − 4,893242786 − 0,005376579444 T + 1,920237685·10⁻⁷ T² + 3,557583162·10⁻¹⁰ T³ − 9,034468831·10⁻¹⁴ T⁴ + 4,1635019 ln T",Origin="T en °R (valor numérico); L es logaritmo natural del valor numérico de p en psia; t ≤ 32,018 °F.",Derivation="Correlación del motor transformada a Rankine y psia."},
            "PwsWater"=>result with {Numerator="−10440,39708 T⁻¹ − 11,294649577 − 0,027022355 T + 1,289036049·10⁻⁵ T² − 2,478068073·10⁻⁹ T³ + 6,5459673 ln T",Origin="T en °R (valor numérico); L es logaritmo natural del valor numérico de p en psia; t > 32,018 °F.",Derivation="Correlación del motor transformada a Rankine y psia."},
            "TK"=>result with {Numerator="t + 459,67",Units="°R",Origin="t: temperatura en °F; T: temperatura absoluta en °R.",Derivation="Conversión de Fahrenheit a Rankine."},
            "h"=>result with {Numerator="0,240279·(t − 32) + W·[1075,236458 + 0,444253·(t − 32)]",Origin="t en °F; W en lb/lb de aire seco. Calores específicos en Btu/(lb·°R); 1075,236458 Btu/lb es la referencia a 32 °F.",Derivation="Conversión del modelo SI de FrioCalc, conservando su referencia de entalpía a 32 °F."},
            "v"=>result with {Numerator="R_{da}·T·(1 + 1,607858·W)",Origin="R_{da} = 0,370489 psia·ft³/(lb·°R); T en °R; P en psia."},
            "vInverse"=>result with {Origin="P en psia; T en °R; R_{da} = 0,370489 psia·ft³/(lb·°R)."},
            "MixT"=>result with {Left="t − 32",Numerator="h − 1075,236458 W",Denominator="0,240279 + 0,444253 W",Origin="t en °F; h en Btu/lb de aire seco; W en lb/lb. Sumar 32 °F al cociente para obtener t."},
            "hInverse"=>result with {Numerator="h − 0,240279 (t − 32)",Denominator="1075,236458 + 0,444253 (t − 32)",Origin="h en Btu/lb de aire seco; t en °F; W en lb/lb."},
            "Sensible"=>result with {Numerator="ṁ_{da} (0,240279 + 0,444253 W)(t₂ − t₁)",Origin="ṁ_{da} en lb/h; t₁, t₂ en °F; W en lb/lb. Resultado en Btu/h."},
            "EntropyT"=>result with {Derivation="Temperaturas absolutas expresadas en Rankine.",Origin="T y T_{ref} en °R."},
            "Entropy"=>result with {Origin="θ: razón de temperaturas en °R; π: razón de presiones parciales en psia. c y R en Btu/(lb·°R).",Units="Btu/(lb aire seco·°R)"},
            _=>result
        };
    }

    // Solo prosa dimensional, no sustitución de números ni coeficientes.
    public static string UnitText(string s)=>s
        .Replace("kilogramo","libra").Replace("kilogramos","libras")
        .Replace("kJ/(kg de aire seco·K)","Btu/(lb de aire seco·°R)")
        .Replace("kJ/(kg·K)","Btu/(lb·°R)").Replace("kPa·m³/(kg·K)","psia·ft³/(lb·°R)")
        .Replace("kJ/kgda","Btu/lbda").Replace("kJ/kg","Btu/lb").Replace("m³/kg","ft³/lb")
        .Replace("g/kg","gr/lb").Replace("kg/kg","lb/lb").Replace("kg/m³","lb/ft³")
        .Replace("kg de aire seco","lb de aire seco").Replace("kg de agua","lb de agua")
        .Replace("kg/s","lb/h").Replace("m³/s","ft³/h").Replace("m³/h","ft³/h")
        .Replace("kPa","psia").Replace("kW","Btu/h").Replace("°C","°F");

    public static AshraeStudyUnit Study(AshraeStudyUnit e,UnitSystem u)
    {
        if(u==UnitSystem.SI)return e;
        string origin=e.Id switch
        {
            "A2"=>"P en psia; altitud en pies. La presión local se mide con un barómetro.",
            "A19"=>"t se expresa en °F; T en °R. Una diferencia de 1 °R equivale a una diferencia de 1 °F.",
            "A3"=>"t en °F, W en lb/lb; h en Btu/lb de aire seco. Los calores específicos son 0,240279 y 0,444253 Btu/(lb·°R); la referencia de vapor es 1075,236458 Btu/lb a 32 °F.",
            "A8"=>"R del aire seco ≈ 0,370489 psia·ft³/(lb·°R); P en psia; T en °R; v en ft³/lb. Caudal volumétrico en ft³/h.",
            _=>UnitText(e.Origin)
        };
        string derivation=e.Id switch {
            "A19"=>"La escala absoluta Rankine se obtiene sumando 459,67 a la temperatura Fahrenheit. La ley de gas ideal usa temperatura absoluta.",
            "A3"=>"La suma de contribuciones del aire seco y vapor, referida a 32 °F y con calores específicos constantes, conduce a la expresión de entalpía.",
            _=>UnitText(e.Derivation)
        };
        string example=e.Id switch
        {
            "A1"=>$"P = {Press(101.325,u)}; pᵥ = {Press(1.585,u)}; p de aire seco = {Press(99.740,u)}.",
            "A2"=>$"A nivel del mar: {Press(101.325,u)}. A aproximadamente 9843 ft: {Press(70.1,u)}.",
            "A19"=>"77 °F corresponden a 536,67 °R.",
            "A4"=>$"A 77 °F, la presión de saturación es aproximadamente {Press(3.17,u)}.",
            "A5"=>"70 gr/lb equivalen a W = 0,010 lb/lb de aire seco y γ = 0,00990 lb/lb de mezcla.",
            "A6"=>$"A 77 °F y 50 % HR, pᵥ ≈ {Press(1.585,u)}.",
            "A3"=>$"Para t = 77 °F y W = 0,010 lb/lb, h = {Enth(50.625,u)}.",
            "A8"=>$"Un caudal de 635664 ft³/h con v = {V(.85,u):0.###} ft³/lb corresponde a {5/.85*7936.64144:0.##} lb/h de aire seco.",
            "A7"=>"A 77 °F y 50 % HR, el rocío es aproximadamente 57 °F.",
            "A20"=>"A 77 °F y 50 % HR, el bulbo húmedo está cerca de 64,4 °F.",
            "A10"=>$"Para h = {Enth(50,u)} y 77 °F, W ≈ 68,3 gr/lb.",
            "A11"=>$"Introducir 77 °F, 50 % HR y {Press(101.325,u)} determina un estado.",
            "A12"=>"Enfriar de 95 a 77 °F, con W = 0,010 lb/lb y 635664 ft³/h de entrada. Calcular caudal másico y potencia en Btu/h.",
            "A13"=>"Para 7936,64 lb/h de aire seco, aumentar W de 56 a 70 gr/lb exige 15,87 lb/h de vapor.",
            "A21"=>"Si entra a 86 °F, el bulbo húmedo es 59 °F y ε = 0,70, sale a 67,1 °F.",
            "A14"=>"Aire inicial a 89,6 °F con rocío de 82,4 °F; enfriarlo hasta 69,8 °F saturado.",
            "A22"=>"Con 7936,64 lb/h de aire seco y reducción de 28 gr/lb se retiran 31,75 lb/h de vapor.",
            "A23"=>"Para alcanzar 71,6 °F y 50 % HR, calcular estado intermedio según el humidificador.",
            "A15"=>"Mezclar 105944 ft³/h de retorno a 75,2 °F y 55 % HR con 17657 ft³/h exterior a 89,6 °F y 70 % HR.",
            "A17"=>"Con 511821 Btu/h sensibles y 170607 Btu/h latentes, RSHF = 0,75.",
            "A18"=>"Agua de 42,8 a 53,6 °F, extracción de 409457 Btu/h: caudal ≈ 37974 lb/h con calor específico de 0,9984 Btu/(lb·°R).",
            "A24"=>$"Si h = {H(50,u):0.###} y h saturada = {Enth(50.3,u)}, Δh = {Enth(-.3,u)}.",
            _=>UnitText(e.Example)
        };
        return e with {Definition=UnitText(e.Definition),Derivation=derivation,Origin=origin,Interpretation=UnitText(e.Interpretation),Example=example};
    }

    public static string ConstructionExample(ChartConstructionStep step,UnitSystem u)
    {
        if(u==UnitSystem.SI)return step.NumericalExample;
        return step.Number switch
        {
            1=>$"P = {Press(101.325,u)}.",
            3=>$"A 77 °F, presión de saturación ≈ {Press(3.17,u)}.",
            4=>$"A 77 °F y {Press(101.325,u)}, Wₛ ≈ 140 gr/lb.",
            6=>"A 77 °F y 50 % HR, W ≈ 69,3 gr/lb.",
            7=>$"A 77 °F y 50 % HR, h ≈ {Enth(50.3,u)}.",
            _=>UnitText(step.NumericalExample)
        };
    }

    public static string EngineeringConditions(string kind,UnitSystem units)=>kind switch
    {
        "Heating" or "Cooling"=>"Balance estacionario a presión constante, sin intercambio de agua; la potencia requiere caudal de aire seco.",
        "Dehumidification"=>"Salida saturada ideal; para la carga del serpentín se necesita caudal seco y entalpía del condensado a su temperatura de salida.",
        "Steam" or "HeatingHumidification"=>"Balance de aire y vapor con entalpía de suministro "+H(2700,units).ToString("0.###")+" "+(units==UnitSystem.IP?"Btu/lb":"kJ/kg")+" de vapor.",
        "Evaporative"=>"Aproximación al estado saturado mediante efectividad de saturación. El descenso térmico máximo corresponde al bulbo húmedo inicial.",
        "Mixing"=>"Balance adiabático de masa seca, vapor y entalpía; las fracciones corresponden a masa seca, sin condensación.",
        "Bypass"=>"Mezcla ideal de estado inicial y aire saturado al ADP; BF y ADP determinan la salida.",
        "Desiccant"=>"La extracción de vapor y la temperatura de salida se especifican; son independientes hasta definir un modelo de sorción y energía.",
        "SHR"=>"FCS es una razón de cargas; para una salida única se requiere otra condición independiente.",
        _=>"Balance entre estados a presión constante."
    };

    public static string ImperialSteps(TheoryLesson lesson,double intensity)
    {
        const UnitSystem u=UnitSystem.IP;
        var a=lesson.Initial;
        if(lesson.Final is not ChartPoint b)
            return $"1. Estado de referencia: {Temp(a.DryBulbC,u)}, HR={a.RelativeHumidityPercent:0.##} %, P={Press(a.PressureKPa,u)}.\n2. FCS={intensity:0.###}; falta otra condición independiente para localizar la impulsión.";
        string difference=$"Δh = {Enth(b.EnthalpyKJPerKgDryAir-a.EnthalpyKJPerKgDryAir,u)}; HR final = {b.RelativeHumidityPercent:0.###} %.";
        string exit=$"T₂ = {Temp(b.DryBulbC,u)}; W₂ = {Hum(b.HumidityRatioKgPerKg,u)}; h₂ = {Enth(b.EnthalpyKJPerKgDryAir,u)}.";
        string initial=$"1. T₁ = {Temp(a.DryBulbC,u)}; W₁ = {Hum(a.HumidityRatioKgPerKg,u)}; h₁ = {Enth(a.EnthalpyKJPerKgDryAir,u)}.";
        string step=lesson.Kind switch
        {
            "Heating"=>$"2. T₂ = {T(a.DryBulbC,u):0.###} + 36·{intensity:0.###} = {Temp(b.DryBulbC,u)}. W permanece constante.",
            "Cooling"=>$"2. Descenso solicitado: 36·{intensity:0.###} °F. Rocío inicial = {Temp(a.DewPointC,u)}; se limita la salida al rocío sin condensación.",
            "Evaporative"=>$"2. Tbh₁ = {Temp(a.WetBulbC,u)}, ε={intensity:0.###}. T₂ = {T(a.DryBulbC,u):0.###} − {intensity:0.###}·({T(a.DryBulbC,u):0.###} − {T(a.WetBulbC,u):0.###}) °F.",
            "Steam"=>$"2. ΔW = {Hum(b.HumidityRatioKgPerKg-a.HumidityRatioKgPerKg,u)}; h del vapor supuesto = {H(2700,u):0.###} Btu/lb de vapor. h₂ = h₁ + ΔW·h del vapor (ΔW en lb/lb).",
            "HeatingHumidification"=>$"2. Calentar a {Temp(lesson.Auxiliary?.DryBulbC??a.DryBulbC,u)} a W constante; después añadir {Hum(b.HumidityRatioKgPerKg-a.HumidityRatioKgPerKg,u)} de vapor con h = {H(2700,u):0.###} Btu/lb.",
            "Dehumidification"=>$"2. Enfriar hasta rocío {Temp(a.DewPointC,u)}, después hasta salida saturada. Agua retirada = {Hum(a.HumidityRatioKgPerKg-b.HumidityRatioKgPerKg,u)}.",
            "Desiccant"=>$"2. Extracción supuesta = {Hum(a.HumidityRatioKgPerKg-b.HumidityRatioKgPerKg,u)}. Temperatura de salida supuesta, no obtenida de isoterma.",
            "Mixing" when lesson.Auxiliary is ChartPoint ext=>$"2. Exterior: {Temp(ext.DryBulbC,u)}, W = {Hum(ext.HumidityRatioKgPerKg,u)}. Fracciones secas: {1-intensity:0.###} y {intensity:0.###}. Ponderar W y h por masa seca.",
            "Bypass" when lesson.Auxiliary is ChartPoint adp=>$"2. ADP supuesto = {Temp(adp.DryBulbC,u)}; BF = {1-intensity:0.###}. Ponderar h y W de entrada y ADP.",
            _=>"2. Aplicar las ecuaciones del proceso."
        };
        return initial+"\n"+step+"\n3. "+exit+"\n4. "+difference;
    }
}
