using System;
using System.Collections.Generic;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics.Processes;

// [4.7] Casos didácticos A03, diapositivas 6, 7, 14, 15, 22, 23 y 24.
// Se calcula con la presión seleccionada; los caudales volumétricos se convierten a m_da con v.
public static class SlideExamples
{
    private static ChartPoint FromTRh(int id,double t,double rh,double p)
    {
        double w=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(t,rh/100.0,p);
        return FromTW(id,t,w,p);
    }
    private static ChartPoint FromTW(int id,double t,double w,double p)
    {
        if (!PointPropertyCalculator.TryCreatePoint(id,$"P{id}","#336699",p,t,w,out ChartPoint? point)
            || point == null) throw new InvalidOperationException("Estado no válido a la presión seleccionada.");
        return point;
    }
    public static string Evaluate(int slide,double pressureKPa)
    {
        try
        {
            return EvaluateCore(slide,pressureKPa);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return "No se pudo resolver el ejercicio a esta presión: " + ex.Message;
        }
    }
    private static string EvaluateCore(int slide,double p)
    {
        if (slide == 6)
        {
            ChartPoint start=FromTW(1,35,.010,p),end=FromTW(2,25,.010,p);
            double m=5.0/start.SpecificVolumeM3PerKgDryAir;
            double q=m*(end.EnthalpyKJPerKgDryAir-start.EnthalpyKJPerKgDryAir);
            return $"DIAPOSITIVA 6 — Enfriamiento sensible a W constante.\n" +
                $"Supuesto declarado: P={p:0.###} kPa abs. Caudal de entrada 5 m³/s.\n" +
                $"ṁda=V̇/v1=5/{start.SpecificVolumeM3PerKgDryAir:0.#####}={m:0.###} kgda/s.\n" +
                $"W=0,010 kg/kgda, h1={start.EnthalpyKJPerKgDryAir:0.###} y h2={end.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n" +
                $"Q̇=ṁda(h2−h1)={q:0.###} kW (signo negativo: carga de refrigeración).";
        }
        if (slide == 7)
        {
            ChartPoint start=FromTRh(1,2,80,p),end=FromTW(2,21,start.HumidityRatioKgPerKg,p);
            double m=(10000.0/3600.0)/start.SpecificVolumeM3PerKgDryAir;
            double q=m*(end.EnthalpyKJPerKgDryAir-start.EnthalpyKJPerKgDryAir);
            return $"DIAPOSITIVA 7 — Precalentamiento sensible.\n" +
                $"Supuesto declarado: P={p:0.###} kPa abs; W constante.\n" +
                $"ṁda=(10000/3600)/v1={m:0.###} kgda/s.\n" +
                $"h1={start.EnthalpyKJPerKgDryAir:0.###}; h2={end.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n" +
                $"Q̇ calentamiento={q:0.###} kW.";
        }
        if (slide == 14)
        {
            ChartPoint start=FromTRh(1,30,10,p);
            if(!ProcessCalculator.TryEvaporativeOutlet(start,.7,2,"#226688",out var end,out string note)
               || end == null) return note;
            return $"DIAPOSITIVA 14 — Lavador adiabático, ε=70 %.\n" +
                $"Tbs1={start.DryBulbC:0.###} °C; Tbh1={start.WetBulbC:0.###} °C;\n" +
                $"Tbs2=30−0,70(30−Tbh1)={end.DryBulbC:0.###} °C.\n" +
                $"W2={end.HumidityRatioKgPerKg*1000:0.###} g/kgda, HR2={end.RelativeHumidityPercent:0.###} %.\n" +
                $"¿Alcanza 95 % HR con el equipo y ese modelo? {(end.RelativeHumidityPercent>=95 ? "Sí" : "No")}.\n" +
                note;
        }
        if (slide == 15)
        {
            double pws28=SaturationVaporPressure.GetPressureKPa(28);
            if(pws28>=p) return "Presión incompatible con punto de rocío 28 °C.";
            double win=SaturationHumidityRatio.FromPressuresKPa(p,pws28);
            ChartPoint start=FromTW(1,32,win,p);
            ChartPoint end=FromTRh(2,21,100,p);
            double waterRemoved=win-end.HumidityRatioKgPerKg;
            if(waterRemoved<0) return "Los datos no representan una eliminación de humedad.";
            double airEnthalpyDelta=start.EnthalpyKJPerKgDryAir-end.EnthalpyKJPerKgDryAir;
            double condensateH=4.186*21.0;
            double coilApprox=airEnthalpyDelta-waterRemoved*condensateH;
            double latentApprox=waterRemoved*2501.0;
            return $"DIAPOSITIVA 15 — Enfriamiento y deshumidificación.\n" +
                $"W1 (Trocío=28 °C)={win*1000:0.###} g/kgda; W2 (21 °C, HR100 %)={end.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                $"Agua eliminada=W1−W2={waterRemoved:0.######} kg/kgda.\n" +
                $"Δh aire={airEnthalpyDelta:0.###} kJ/kgda.\n" +
                $"Carga aproximada del serpentín={coilApprox:0.###} kJ/kgda considerando condensado líquido a 21 °C.\n" +
                $"Aporte latente de referencia ≈ΔW·2501={latentApprox:0.###} kJ/kgda. Son convenciones de balance, no una separación exacta universal.";
        }
        if (slide == 22)
        {
            ChartPoint room=FromTRh(1,24,55,p),outside=FromTRh(2,32,68,p);
            double ventilation=300.0*8.0/1000.0;
            double m=ventilation/outside.SpecificVolumeM3PerKgDryAir;
            double q=m*(outside.EnthalpyKJPerKgDryAir-room.EnthalpyKJPerKgDryAir);
            return $"DIAPOSITIVA 22 — Auditorio 300 personas.\n" +
                $"Ventilación requerida=300·8 L/s=2400 L/s={ventilation:0.###} m³/s.\n" +
                $"Si la corriente exterior se enfría/humidifica hasta el estado interior: ṁda={m:0.###} kgda/s; " +
                $"diferencia de entalpía Q̇={q:0.###} kW.\n" +
                "Este valor NO es la carga total del auditorio. Faltan ganancias de personas, envolvente, iluminación, equipos, infiltración y régimen.";
        }
        if (slide == 23)
        {
            ChartPoint ret=FromTRh(1,24,55,p),ext=FromTRh(2,32,70,p);
            double m1=(3000.0/3600.0)/ret.SpecificVolumeM3PerKgDryAir;
            double m2=(500.0/3600.0)/ext.SpecificVolumeM3PerKgDryAir;
            if(!ProcessCalculator.TryMix(new List<(ChartPoint,double)> {(ret,m1),(ext,m2)},
               3,"#006688",out var mix,out string note) || mix==null) return note;
            return $"DIAPOSITIVA 23 — Mezcla de aire de retorno y exterior.\n" +
                $"ṁda1=3000/(3600·v1)={m1:0.###} kgda/s;\n" +
                $"ṁda2=500/(3600·v2)={m2:0.###} kgda/s.\n" +
                $"W3=(ṁ1W1+ṁ2W2)/(ṁ1+ṁ2)={mix.HumidityRatioKgPerKg*1000:0.###} g/kgda.\n" +
                $"h3=(ṁ1h1+ṁ2h2)/(ṁ1+ṁ2)={mix.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n" +
                $"Tbs3=(h3−2501·W3)/(1,006+1,86·W3)={mix.DryBulbC:0.###} °C.\n" +
                "No se promedian caudales volumétricos porque los volúmenes específicos son diferentes.";
        }
        if (slide == 24)
        {
            return "DIAPOSITIVA 24 — FCS interior=0,75; factor bypass BF=0,20.\n" +
                "Los dos parámetros no fijan de modo único el estado de impulsión. " +
                "Se requiere, según el método, ADP o características del serpentín, flujo de aire, cargas sensible/latente e hipótesis de mezcla. " +
                "No se calcula un punto de impulsión ficticio.";
        }
        return "Diapositiva fuera del conjunto de ejercicios aprobado.";
    }
}
