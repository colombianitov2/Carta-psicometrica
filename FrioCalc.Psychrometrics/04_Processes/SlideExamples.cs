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
        if(slide==32)
        {
            return "DIAPOSITIVA 32 — Reiteración del problema de auditorio (FCSi y BF).\n" +
                EvaluateCore(24,p);
        }
        if(slide==36)
        {
            // [4.7.36] Balance de sistema completo con hipótesis declaradas.
            // La diapositiva aporta cargas local, estados y temperaturas de agua.
            // El 20% exterior se interpreta SUPUESTAMENTE por masa de aire seco;
            // no hay un caudal de diseño ni rendimiento del serpentín garantizado.
            ChartPoint room=FromTRh(1,24,50,p);
            ChartPoint outdoor=FromTRh(2,34,70,p);
            ChartPoint coilOut=FromTRh(3,11,95,p);
            ChartPoint supply=FromTW(4,12.5,coilOut.HumidityRatioKgPerKg,p);
            if(!ProcessCalculator.TryMix(new List<(ChartPoint State,double DryAirMassFlowKgS)>
            { (room,.8),(outdoor,.2) },5,"#147D74",out var mixed,out var mixError)
                ||mixed is null)
                return "DIAPOSITIVA 36 — No se puede formar la mezcla a la presión indicada: "+mixError;
            const double rshKW=150,rlhKW=50,cpWater=4.186;
            const double waterIn=6,waterOut=12;
            double deltaT=room.DryBulbC-supply.DryBulbC;
            double cpMix=1.006+1.86*supply.HumidityRatioKgPerKg;
            if(deltaT<=0 || cpMix<=0)return "DIAPOSITIVA 36 — La impulsión no permite extraer calor sensible del recinto.";
            double massDryAir=rshKW/(cpMix*deltaT);
            double latentEstimated=massDryAir*(room.HumidityRatioKgPerKg-
                supply.HumidityRatioKgPerKg)*2501.0;
            double totalFromRoomStates=massDryAir*(room.EnthalpyKJPerKgDryAir-
                supply.EnthalpyKJPerKgDryAir);
            double latentDeviation=latentEstimated-rlhKW;
            double waterRemoved=Math.Max(0,massDryAir*(mixed.HumidityRatioKgPerKg-
                coilOut.HumidityRatioKgPerKg));
            // La salida de condensado se idealiza como líquido a T del serpentín.
            double condensateSpecificEnthalpy=cpWater*coilOut.DryBulbC;
            double coilLoad=massDryAir*(mixed.EnthalpyKJPerKgDryAir-
                coilOut.EnthalpyKJPerKgDryAir)-
                waterRemoved*condensateSpecificEnthalpy;
            double chilledWaterFlow=coilLoad/(cpWater*(waterOut-waterIn));
            double fanLoad=massDryAir*(supply.EnthalpyKJPerKgDryAir-
                coilOut.EnthalpyKJPerKgDryAir);
            if(!double.IsFinite(coilLoad)||coilLoad<=0||!double.IsFinite(chilledWaterFlow))
                return "DIAPOSITIVA 36 — El conjunto de estados y cargas no permite un balance de enfriamiento positivo.";
            string consistency=Math.Abs(latentDeviation)<=.1*rlhKW
                ?"Las cargas sensibles y latentes son aproximadamente compatibles bajo las hipótesis."
                :"ADVERTENCIA: la carga latente suministrada por los estados NO coincide dentro de ±10% con RLH=50 kW; revise caudal, suministro o condiciones.";
            return $"DIAPOSITIVA 36 — Auditorio con serpentín, ventilador draw-through y agua fría.\n"+
                $"P={p:0.###} kPa abs. RSH=150 kW, RLH=50 kW; aire exterior=20 % de m_da (SUPUESTO; la diapositiva no define base de 20 %).\n"+
                $"Recinto R: 24 °C / 50% HR; exterior E: 34 °C / 70% HR.\n"+
                $"Serpentín C: 11 °C / 95% HR; ventilador después del serpentín ΔT=+1,5 °C → impulsión S: {supply.DryBulbC:0.###} °C y W_S=W_C.\n"+
                $"1. R: W={room.HumidityRatioKgPerKg*1000:0.###} g/kgda, h={room.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n"+
                $"2. E: W={outdoor.HumidityRatioKgPerKg*1000:0.###} g/kgda, h={outdoor.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n"+
                $"3. M: W=0,8·W_R+0,2·W_E={mixed.HumidityRatioKgPerKg*1000:0.###} g/kgda; h={mixed.EnthalpyKJPerKgDryAir:0.###} kJ/kgda; Tbs={mixed.DryBulbC:0.###} °C.\n"+
                $"4. C: W={coilOut.HumidityRatioKgPerKg*1000:0.###} g/kgda; h_C={coilOut.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n"+
                $"5. S: Tbs=11+1,5={supply.DryBulbC:0.###} °C; h_S={supply.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n"+
                $"6. cp_s≈1,006+1,86·W_S={cpMix:0.######} kJ/(kgda·K); ΔT_local={deltaT:0.###} °C.\n"+
                $"7. ṁ_da=RSH/[cp_s·(T_R−T_S)]={rshKW:0.###}/({cpMix:0.######}·{deltaT:0.###})={massDryAir:0.###} kgda/s.\n"+
                $"8. RLH estimada≈ṁ_da·(W_R−W_S)·2501={latentEstimated:0.###} kW (dato del enunciado: 50 kW; diferencia={latentDeviation:+0.###;-0.###;0} kW).\n"+
                $"   Transferencia total de recinto a partir de h≈{totalFromRoomStates:0.###} kW (RSH+RLH del enunciado=200 kW).\n"+
                $"9. Agua condensada=ṁ_da·(W_M−W_C)={waterRemoved:0.######} kg/s; condensado líquido a 11 °C SUPUESTO.\n"+
                $"10. Q̇_serpentín=ṁ_da·(h_M−h_C)−ṁ_cond·[4,186·11]={coilLoad:0.###} kW.\n"+
                $"11. Q̇_ventilador≈ṁ_da·(h_S−h_C)={fanLoad:0.###} kW; no sumar esta potencia al lado incorrecto del balance.\n"+
                $"12. ṁ_agua fría=Q̇_serpentín/[4,186·(12−6)]={chilledWaterFlow:0.###} kg/s.\n"+
                consistency+"\n"+
                "Conclusión condicionada: un sistema UTA con mezcla, serpentín de enfriamiento/deshumidificación y ventilador después del serpentín es conceptualmente compatible. "+
                "NO es posible seleccionar fabricante/modelo ni verificar capacidad real sin curvas, pérdidas de presión, ventilador, condensado, controles y datos de proyecto. "+
                "Si el 20% es volumétrico en vez de másico, recalcular la mezcla con los volúmenes específicos individuales.";
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
