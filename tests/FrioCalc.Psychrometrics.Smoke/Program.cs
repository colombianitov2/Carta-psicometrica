using System;
using System.Collections.Generic;
using System.Linq;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

// [T1] Pruebas sin paquetes externos. Ejecutar en Windows con .NET 10.
static class Program
{
    private static int _checks;
    private static void Check(bool value, string name)
    {
        _checks++;
        if (!value) throw new Exception("PRUEBA FALLIDA: " + name);
    }

    private static ChartPoint Create(int id, double dry, double rh, double pressure=101.325)
    {
        var w=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(dry,rh/100.0,pressure);
        Check(PointPropertyCalculator.TryCreatePoint(id,$"P{id}","#006688",pressure,dry,w,out var point)
            && point is not null,"Punto base");
        return point!;
    }

    static int Main()
    {
        try
        {
            var p=Create(1,25,60);
            Check(Math.Abs(p.RelativeHumidityPercent-60)<.001,"HR");
            Check(p.DewPointC < p.DryBulbC,"Punto de rocío");
            Check(p.HumidityRatioKgPerKg>0 && p.HumidityRatioKgPerKg<.03,"W razonable");
            Check(p.WetBulbC<=p.DryBulbC,"Bulbo húmedo");

            // [T2] Inversión de estado usando pares realmente independientes
            Check(StateInputSolver.TrySolve(new Dictionary<StateProperty,double> {
                [StateProperty.DryBulb]=25,[StateProperty.RelativeHumidity]=60
            },101.325,2,"#005599",out var fromTRh,out var m1) && fromTRh is not null,m1);
            Check(StateInputSolver.TrySolve(new Dictionary<StateProperty,double> {
                [StateProperty.HumidityRatio]=p.HumidityRatioKgPerKg,
                [StateProperty.Enthalpy]=p.EnthalpyKJPerKgDryAir
            },101.325,3,"#005599",out var fromWH,out var m2) && fromWH is not null,m2);
            Check(Math.Abs(fromWH!.DryBulbC-25)<.13,"Inversión W/h");
            Check(StateInputSolver.TrySolve(new Dictionary<StateProperty,double> {
                [StateProperty.DryBulb]=25,
                [StateProperty.RelativeHumidity]=60,
                [StateProperty.Enthalpy]=p.EnthalpyKJPerKgDryAir
            },101.325,4,"#005599",out _,out _),"Redundancia coherente");
            Check(!StateInputSolver.TrySolve(new Dictionary<StateProperty,double> {
                [StateProperty.DryBulb]=25,
                [StateProperty.RelativeHumidity]=60,
                [StateProperty.Enthalpy]=400
            },101.325,5,"#005599",out _,out _),"Contradicción detectada");
            Check(!StateInputSolver.TrySolve(new Dictionary<StateProperty,double> {
                [StateProperty.DryBulb]=25
            },101.325,6,"#005599",out _,out _),"Dato insuficiente");

            // [T3] Conservación de mezcla: flujo másico por v, no promedio volumétrico
            var a=Create(7,24,55);
            var b=Create(8,32,70);
            double ma=PsychrometricUnits.FlowToDryAirMassKgS(3000/3600.0,a.SpecificVolumeM3PerKgDryAir);
            double mb=PsychrometricUnits.FlowToDryAirMassKgS(500/3600.0,b.SpecificVolumeM3PerKgDryAir);
            Check(ProcessCalculator.TryMix(new List<(ChartPoint,double)> {(a,ma),(b,mb)},
                9,"#006688",out var mixed,out var mixError) && mixed is not null,mixError);
            Check(Math.Abs((ma*a.HumidityRatioKgPerKg+mb*b.HumidityRatioKgPerKg)/(ma+mb)
                -mixed!.HumidityRatioKgPerKg)<1e-8,"Conservación vapor mezcla");
            Check(Math.Abs((ma*a.EnthalpyKJPerKgDryAir+mb*b.EnthalpyKJPerKgDryAir)/(ma+mb)
                -mixed.EnthalpyKJPerKgDryAir)<1e-8,"Conservación energía mezcla");

            // [T4] Sensible, humidificación y bypass
            var cooled=Create(10,20,60);
            var stateW= new Dictionary<StateProperty,double> {
                [StateProperty.DryBulb]=20, [StateProperty.HumidityRatio]=p.HumidityRatioKgPerKg
            };
            Check(StateInputSolver.TrySolve(stateW,101.325,11,"#667788",out var sensibleOut,out var sError)
                && sensibleOut is not null,sError);
            Check(ProcessCalculator.TryCalculate(p,sensibleOut!,ProcessType.Sensible,1.0,
                out var sensibleResult,out var err),err);
            Check(sensibleResult.ThermalPowerKW is double power && power<0,"Potencia sensible");
            Check(Math.Abs(sensibleResult.DeltaHumidityRatioKgPerKg)<1e-9,"W constante");

            var dryAir=Create(12,30,10);
            Check(ProcessCalculator.TryEvaporativeOutlet(dryAir,.7,13,"#446688",
                out var evaporated,out var evapError) && evaporated is not null,evapError);
            Check(evaporated!.DryBulbC<dryAir.DryBulbC &&
                evaporated.HumidityRatioKgPerKg>dryAir.HumidityRatioKgPerKg,
                "Evaporación ideal enfría y humidifica");
            Check(ProcessCalculator.TrySteamOutlet(dryAir,.002,2700,14,"#446688",
                out var steamed,out var steamError) && steamed is not null,steamError);
            Check(steamed!.HumidityRatioKgPerKg > dryAir.HumidityRatioKgPerKg,"Inyección vapor");
            Check(ProcessCalculator.TryBypassOutlet(Create(15,30,60),10,.2,16,"#447788",
                out var bypass,out var bypassError) && bypass is not null,bypassError);

            // [T4.1] Efectividad al 100 % debe alcanzar saturación y BF no debe humidificar por error.
            Check(ProcessCalculator.TryEvaporativeOutlet(dryAir,1.0,17,"#446688",
                out var saturated,out var satError) && saturated is not null,satError);
            Check(Math.Abs(saturated!.RelativeHumidityPercent-100) < .05,"Evaporativa ε=100% saturación");
            Check(!ProcessCalculator.TryBypassOutlet(Create(18,30,40),25,.2,19,"#447788",
                out _,out _),"ADP superior a rocío incompatible para deshumidificación");
            var humidIn=Create(20,30,60);
            var humidOut=Create(21,20,95);
            Check(ProcessCalculator.TryCalculate(humidIn,humidOut,ProcessType.CoolingDehumidification,
                1.0,20.0,out var cooling,out var coolError),coolError);
            Check(cooling.CondensateMassFlowKgS is double waterMass && waterMass>0,
                "Condensado mass balance");
            Check(cooling.CoilCoolingLoadKW is double coil && coil>0,
                "Carga de serpentín condensado");
            Check(ProcessCalculator.TryCalculate(humidIn,humidOut,ProcessType.CoolingDehumidification,
                1.0,out var uncorrected,out _)
                && uncorrected.CoilCoolingLoadKW is null,"Sin temperatura del condensado, carga no inferida");

            Check(fromTRh!.InputConditions is {Count:2}, "Trazabilidad de variables de entrada");
            string saved=System.Text.Json.JsonSerializer.Serialize(fromTRh);
            var restored=System.Text.Json.JsonSerializer.Deserialize<ChartPoint>(saved);
            Check(restored is not null && restored.InputConditions is {Count:2},
                "Serialización de condiciones de entrada");

            // [T4.2] Recorridos cerrados por estados, no confundir con potencia cero.
            var closedPath=new ChartPoint[]{p,cooled,p};
            Check(ProcessCycleAnalyzer.TryAnalyze(closedPath,1.0,out var cycle,out var cycleError),
                cycleError);
            Check(cycle.IsClosed && Math.Abs(cycle.NetEnthalpyKJPerKgDryAir)<1e-12,
                "Ciclo cerrado sin error de cierre");
            Check(!ProcessCycleAnalyzer.TryAnalyze(new ChartPoint[]{p,Create(31,22,75,90.0)},
                1.0,out _,out _),"Rechazo de ciclo a presión contradictoria");
            Check(BibliographyCatalog.Entries.Count>=18,"Catálogo bibliográfico");
            Check(BibliographyCatalog.ExportCsv().Contains("MoistAirEnthalpy.FromDryBulbAndHumidityRatio"),
                "Exportación bibliográfica con método");

            // [T4.3] Ramas y desecante con datos suficientes.
            Check(ProcessCalculator.TryDesiccantOutlet(p,.002,32,40,"#669900",
                out var desiccated,out var desError)&&desiccated is not null,desError);
            Check(Math.Abs(desiccated!.HumidityRatioKgPerKg-(p.HumidityRatioKgPerKg-.002))<1e-9,
                "Desecante con remoción explícita");
            Check(!ProcessCalculator.TryDesiccantOutlet(p,.05,32,41,"#669900",out _,out _),
                "Desecante no puede retirar agua inexistente");
            Check(ProcessCalculator.TryBranchBalance(p,cooled,desiccated,
                2,.35,out var split,out var splitError)&&split!=null,splitError);
            Check(Math.Abs(split!.FirstMassFlowKgS+split.SecondMassFlowKgS-2)<1e-10,
                "Conservación del flujo másico de ramas");
            Check(!ProcessCalculator.TryBranchBalance(p,cooled,desiccated,
                2,1.2,out _,out _),"Fracción de rama fuera de rango");

            // [T4.4] Ejemplos públicos ASHRAE 2025, Fundamentals cap. 1 §9–10.
            // Fuente: https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx
            // Los valores leídos de la carta son aproximados (redondeos gráficos).
            double wetW=WetBulbHumidityRatio.FromDryBulbAndWetBulb(40,20,101.325);
            Check(PointPropertyCalculator.TryCreatePoint(50,"ASHRAE Ej.1","#336699",
                101.325,40,wetW,out var ashraeOne)&&ashraeOne!=null,"ASHRAE Ej.1 estado");
            Check(Math.Abs(ashraeOne!.HumidityRatioKgPerKg*1000-6.5)<1.5,
                "ASHRAE 2025 Ej.1 W aproximado");
            Check(Math.Abs(ashraeOne.EnthalpyKJPerKgDryAir-56.7)<2.0,
                "ASHRAE 2025 Ej.1 entalpía");
            Check(Math.Abs(ashraeOne.SpecificVolumeM3PerKgDryAir-.896)<.025,
                "ASHRAE 2025 Ej.1 volumen específico");
            var ashraeThree=Create(51,30,50);
            Check(Math.Abs(ashraeThree.HumidityRatioKgPerKg*1000-13.3)<.8,
                "ASHRAE 2025 Ej.3 W 30°C/50%HR");
            Check(Math.Abs(ashraeThree.EnthalpyKJPerKgDryAir-64.3)<1.5,
                "ASHRAE 2025 Ej.3 entalpía");
            Check(Math.Abs(ashraeThree.SpecificVolumeM3PerKgDryAir-.877)<.025,
                "ASHRAE 2025 Ej.3 volumen específico");
            Check(BibliographyCatalog.Entries.Count>=20,
                "Catálogo bibliográfico ampliado");
            Check(BibliographyCatalog.Entries.Any(e=>e.Url.Contains("/F25_Ch01_si.aspx")),
                "Referencia ASHRAE 2025 primaria");

            // [T4.5] Validación externa independiente: ASHRAE 2025, Fundamentals, cap. 1,
            // §9 comparación cartas 1/6 y §10 ejemplos 2–4 (lectura gráfica aproximada).
            // https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx
            double wSea = WetBulbHumidityRatio.FromDryBulbAndWetBulb(40,30,101.325);
            Check(PointPropertyCalculator.TryCreatePoint(60,"Carta 1","#145388",
                101.325,40,wSea,out var sea) && sea!=null,"ASHRAE Carta 1 estado");
            Check(Math.Abs(sea!.HumidityRatioKgPerKg*1000-23.0)<1.2,
                "ASHRAE Carta 1 W ≈23.0 g/kgda");
            Check(Math.Abs(sea.EnthalpyKJPerKgDryAir-99.5)<2.5,
                "ASHRAE Carta 1 h ≈99.5 kJ/kgda");
            Check(Math.Abs(sea.SpecificVolumeM3PerKgDryAir-0.920)<0.025,
                "ASHRAE Carta 1 v ≈0.920 m³/kgda");

            double pAltitude = 84.54; // Presión carta 6, 1500 m, ASHRAE 2025.
            double wAltitude = WetBulbHumidityRatio.FromDryBulbAndWetBulb(40,30,pAltitude);
            Check(PointPropertyCalculator.TryCreatePoint(61,"Carta 6","#145388",
                pAltitude,40,wAltitude,out var mountain) && mountain!=null,
                "ASHRAE Carta 6 estado");
            Check(Math.Abs(mountain!.HumidityRatioKgPerKg*1000-28.6)<1.5,
                "ASHRAE Carta 6 W ≈28.6 g/kgda");
            Check(Math.Abs(mountain.EnthalpyKJPerKgDryAir-114.1)<3.0,
                "ASHRAE Carta 6 h ≈114.1 kJ/kgda");
            Check(Math.Abs(mountain.SpecificVolumeM3PerKgDryAir-1.111)<0.03,
                "ASHRAE Carta 6 v ≈1.111 m³/kgda");
            Check(mountain.HumidityRatioKgPerKg > sea.HumidityRatioKgPerKg,
                "ASHRAE humedad mayor con altitud a Tbs y Tbh fijas");

            // ASHRAE ejemplo 2: 2 °C saturado, 10 m³/s de entrada y salida a 40 °C.
            var sat2 = Create(62,2,100);
            Check(Math.Abs(sat2.HumidityRatioKgPerKg*1000-4.38)<.40,
                "ASHRAE ejemplo 2 W1 ≈4.38 g/kgda");
            Check(Math.Abs(sat2.SpecificVolumeM3PerKgDryAir-.785)<.02,
                "ASHRAE ejemplo 2 v1 ≈0.785 m³/kgda");
            Check(PointPropertyCalculator.TryCreatePoint(63,"Salida 40°C","#145388",
                101.325,40,sat2.HumidityRatioKgPerKg,out var hot40) && hot40!=null,
                "ASHRAE ejemplo 2 salida");
            Check(ProcessCalculator.TryCalculate(sat2,hot40!,ProcessType.Sensible,
                10/sat2.SpecificVolumeM3PerKgDryAir,out var qHeating,out var qHeatError),qHeatError);
            Check(qHeating.ThermalPowerKW is double heatKw && Math.Abs(heatKw-490)<20,
                "ASHRAE ejemplo 2 carga sensible ≈490 kW");

            // ASHRAE ejemplo 3: 30 °C/50 % HR a 10 °C saturados, 5 m³/s y
            // condensado drenado a 10 °C (h_agua = 42.02 kJ/kg).
            var coolingIn = Create(64,30,50);
            var coolingOut = Create(65,10,100);
            double mCooling = 5/coolingIn.SpecificVolumeM3PerKgDryAir;
            Check(ProcessCalculator.TryCalculate(coolingIn,coolingOut,
                ProcessType.CoolingDehumidification,mCooling,10,
                out var fullCoil,out var coilError),coilError);
            Check(fullCoil.CoilCoolingLoadKW is double fullQ && Math.Abs(fullQ-197)<15,
                "ASHRAE ejemplo 3 carga de serpentín ≈197 kW");

            // ASHRAE ejemplo 4: mezcla de 2 m³/s a 4°C/2°C Tbh y 6.25 m³/s
            // a 25°C/50% HR. Resultado gráfico ~19.5°C Tbs y 14.6°C Tbh.
            double wCold = WetBulbHumidityRatio.FromDryBulbAndWetBulb(4,2,101.325);
            Check(PointPropertyCalculator.TryCreatePoint(66,"Aire exterior","#145388",
                101.325,4,wCold,out var cold)&&cold!=null,"ASHRAE ejemplo 4 exterior");
            var return25 = Create(67,25,50);
            double mCold = 2/cold!.SpecificVolumeM3PerKgDryAir;
            double mWarm = 6.25/return25.SpecificVolumeM3PerKgDryAir;
            Check(ProcessCalculator.TryMix(new List<(ChartPoint,double)> {
                (cold,mCold),(return25,mWarm)},68,"#145388",
                out var ashraeMix,out var mixingError)&&ashraeMix!=null,mixingError);
            Check(Math.Abs(ashraeMix!.DryBulbC-19.5)<1.2,
                "ASHRAE ejemplo 4 Tbs mezcla ≈19.5°C");
            Check(Math.Abs(ashraeMix.WetBulbC-14.6)<1.2,
                "ASHRAE ejemplo 4 Tbh mezcla ≈14.6°C");

            // [T5.1] Carta estándar: rangos y zoom deben permanecer bloqueados.
            var standardGeometry=new FrioCalc.Psychrometrics.Chart.ChartGeometry();
            standardGeometry.SetStandardChartMode(true);
            var rect=new System.Windows.Rect(0,0,900,600);
            double standardMinT=standardGeometry.MinDryBulbC;
            double standardMaxT=standardGeometry.MaxDryBulbC;
            double standardMaxW=standardGeometry.MaxHumidityRatioKgPerKg;
            standardGeometry.ZoomAt(rect,new System.Windows.Point(450,300),.50);
            standardGeometry.PanBy(12,.01);
            Check(Math.Abs(standardGeometry.MinDryBulbC-standardMinT)<1e-10 &&
                Math.Abs(standardGeometry.MaxDryBulbC-standardMaxT)<1e-10 &&
                Math.Abs(standardGeometry.MaxHumidityRatioKgPerKg-standardMaxW)<1e-10,
                "Carta estándar inmoviliza zoom y desplazamiento");
            standardGeometry.SetStandardChartMode(false);
            double expanded=standardGeometry.MaxDryBulbC-standardGeometry.MinDryBulbC;
            standardGeometry.ZoomAt(rect,new System.Windows.Point(450,300),.8);
            Check(standardGeometry.MaxDryBulbC-standardGeometry.MinDryBulbC<expanded,
                "Carta personalizada permite zoom");

            // [T6] Entropía específica: referencia termodinámica explícita.
            double sRef=MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(0,0,101.325);
            Check(Math.Abs(sRef)<1e-12,"Entropía referencia aire seco");
            double s25=MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(25,0,101.325);
            Check(s25>0.085 && s25<0.09,"Entropía ideal T25 aire seco");
            double s25Wet=MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(
                p.DryBulbC,p.HumidityRatioKgPerKg,p.PressureKPa);
            Check(double.IsFinite(s25Wet),"Entropía relativa de estado húmedo válida");
            Check(double.IsFinite(MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(-10,0.001,101.325)),
                "Entropía bajo cero físicamente admisible");
            Check(double.IsNaN(MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(
                25,0.08,101.325)),"Entropía no admite estado sobresaturado");
            Check(Math.Abs(PsychrometricUnits.EntropyToSI(
                PsychrometricUnits.EntropyFromSI(s25Wet,UnitSystem.IP),UnitSystem.IP)-s25Wet)<1e-10,
                "Conversión entropía SI/IP");
            Check(BibliographyCatalog.Entries.Any(e=>e.MethodId.Contains("MoistAirSpecificEntropy")),
                "Bibliografía de modelo entropía relativa");

            // [T6.1] Desviación de entalpía: referencia saturada debe ser cero.
            double satW25=SaturationHumidityRatio.FromPressuresKPa(
                101.325,SaturationVaporPressure.GetPressureKPa(25));
            double devSat=EnthalpyDeviation.FromDryBulbAndHumidityRatio(25,satW25,101.325);
            Check(double.IsFinite(devSat)&&Math.Abs(devSat)<.015,
                "Desviación de entalpía cero en saturación");
            double devDry=EnthalpyDeviation.FromDryBulbAndHumidityRatio(25,satW25*.5,101.325);
            Check(double.IsFinite(devDry)&&Math.Abs(devDry)>1e-5,
                "Desviación de entalpía no nula fuera de saturación");

            // [T7] Matriz de estados desde ASHRAE: cuatro presiones y temperaturas
            // por encima/debajo de cero, HR secas/húmedas, consistencia inversa.
            // Son verificaciones de dominio, NO sustituyen datos independientes RP-1485.
            int sampleId=100;
            foreach(double pressure in new[]{77.058,84.54,92.634,101.325})
            foreach(double tbs in new[]{-10.0,5.0,25.0,45.0})
            foreach(double relative in new[]{.20,.50,.80})
            {
                double ratio=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(
                    tbs,relative,pressure);
                Check(double.IsFinite(ratio)&&ratio>=0,
                    $"Matriz W({tbs},{relative},{pressure})");
                Check(PointPropertyCalculator.TryCreatePoint(sampleId,
                    $"M{sampleId}","#4673AB",pressure,tbs,ratio,out var state)
                    && state!=null,$"Matriz estado {sampleId}");
                Check(Math.Abs(state!.RelativeHumidityPercent-relative*100)<.015,
                    $"Matriz HR {sampleId}");
                Check(double.IsFinite(state.WetBulbC)&&state.WetBulbC<=tbs+.02,
                    $"Matriz Tbh física {sampleId}");
                Check(double.IsFinite(state.DewPointC)&&state.DewPointC<=tbs+.02,
                    $"Matriz Trocío físico {sampleId}");
                Check(Math.Abs(MoistAirEnthalpy.HumidityRatioFromEnthalpyAndDryBulb(
                    state.EnthalpyKJPerKgDryAir,tbs)-ratio)<1e-9,
                    $"Matriz inversión h/W {sampleId}");
                Check(double.IsFinite(MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(
                    tbs,ratio,pressure)),$"Matriz entropía relativa {sampleId}");
                double pws=SaturationVaporPressure.GetPressureKPa(tbs);
                double satW=SaturationHumidityRatio.FromPressuresKPa(pressure,pws);
                Check(!PointPropertyCalculator.TryCreatePoint(sampleId+1000,
                    "Sobresaturado","#4673AB",pressure,tbs,satW*1.02,out _),
                    $"Matriz rechazo de saturación {sampleId}");
                sampleId++;
            }

            // [T8] Cobertura de los ejercicios aprobados A03, diapositivas 1–24.
            foreach(int slide in new[]{6,7,14,15,22,23,24})
            {
                string output=SlideExamples.Evaluate(slide,101.325);
                Check(output.StartsWith($"DIAPOSITIVA {slide}",StringComparison.Ordinal),
                    $"A03 diapositiva {slide}, salida informativa reproducible");
                Check(!output.Contains("No se pudo resolver",StringComparison.OrdinalIgnoreCase),
                    $"A03 diapositiva {slide}, sin estado imposible oculto");
            }
            Check(SlideExamples.Evaluate(22,101.325).Contains("NO es la carga total",
                StringComparison.OrdinalIgnoreCase),
                "A03 diapositiva 22, advertencia de carga total indeterminada");
            Check(SlideExamples.Evaluate(24,101.325).Contains("no fijan de modo único",
                StringComparison.OrdinalIgnoreCase),
                "A03 diapositiva 24, BF y FCS no inventan impulsión");

            // [T12] Persistencia completa de aire seco y defensa contra sesión adulterada.
            var drySessionPoint=Create(8800,20,0);
            string dryJson=System.Text.Json.JsonSerializer.Serialize(drySessionPoint,
                SessionFileIntegrity.JsonOptions(indented:true));
            var dryReloaded=System.Text.Json.JsonSerializer.Deserialize<ChartPoint>(
                dryJson,SessionFileIntegrity.JsonOptions());
            Check(dryReloaded is not null && double.IsNaN(dryReloaded.DewPointC),
                "Sesión serializa y restaura punto de rocío indefinido del aire seco");
            Check(SessionFileIntegrity.TryValidate(101.325,
                new[]{dryReloaded!},Array.Empty<PsychrometricProcess>(),out var sessionMsg),
                "Sesión válida, propiedades recalculadas: "+sessionMsg);
            Check(!SessionFileIntegrity.TryValidate(101.325,
                new[]{dryReloaded! with {RelativeHumidityPercent=45.0}},
                Array.Empty<PsychrometricProcess>(),out _),
                "Rechazo de propiedades derivadas manipuladas");
            Check(!SessionFileIntegrity.TryValidate(101.325,
                new[]{dryReloaded!},
                new[]{new PsychrometricProcess(1,"M","#ZZZZZZ",
                    ProcessType.Automatic,new[]{8800,8800},1.0)},out _),
                "Rechazo de color inválido antes de dibujar procesos");
            Check(!SessionFileIntegrity.TryValidate(101.325,
                new[]{dryReloaded!},
                new[]{new PsychrometricProcess(1,"M","#123ABC",
                    ProcessType.Automatic,new[]{8800,999999},1.0)},out _),
                "Rechazo de procesos con referencias inexistentes");

            // [T10] Redundancia numérica: tres propiedades compatibles,
            // redondeadas al 0,1 g/kgda, deben representar un estado único.
            var redundantPoint=Create(7000,25,60);
            double roundedW=Math.Round(redundantPoint.HumidityRatioKgPerKg*1000,1)/1000;
            var redundantInputs=new Dictionary<StateProperty,double>
            {
                [StateProperty.DryBulb]=25,
                [StateProperty.RelativeHumidity]=60,
                [StateProperty.HumidityRatio]=roundedW
            };
            Check(StateInputSolver.TrySolve(redundantInputs,101.325,7001,"#446688",
                StateSolveMethod.Automatic,out var redundancyResolved,out var redundantError)
                && redundancyResolved is not null,"Tres propiedades redundantes redondeadas: "+redundantError);
            Check(Math.Abs(redundancyResolved!.DryBulbC-25)<.01 &&
                  Math.Abs(redundancyResolved.RelativeHumidityPercent-60)<.35,
                "Estado redundante conserva Tbs y HR");
            var impossibleInputs=new Dictionary<StateProperty,double>
            {
                [StateProperty.DryBulb]=25,
                [StateProperty.RelativeHumidity]=60,
                [StateProperty.HumidityRatio]=roundedW*1.4
            };
            Check(!StateInputSolver.TrySolve(impossibleInputs,101.325,7002,"#446688",
                StateSolveMethod.Automatic,out _,out _),"Inconsistencia de entradas detectada");

            // [T11] Límite del dominio y convenciones Try sin excepciones.
            Check(!PointPropertyCalculator.TryCreatePoint(7003,"Fuera","#446688",
                101.325,-101.0,0.001,out _),"Fuera del rango ASHRAE bajo: retorno false");
            Check(!PointPropertyCalculator.TryCreatePoint(7004,"Fuera","#446688",
                101.325,201.0,0.001,out _),"Fuera del rango ASHRAE alto: retorno false");
            Check(StateInputSolver.TrySolve(new Dictionary<StateProperty,double>{
                [StateProperty.DryBulb]=115.0,[StateProperty.RelativeHumidity]=5.0},
                101.325,7005,"#446688",StateSolveMethod.Direct,
                out var hotState,out _ ) && hotState is not null,
                "Estado directo admitido a 115 C (dentro del alcance gráfico extendido)");
            var airDry=Create(7006,25,0);
            Check(!ProcessCalculator.TryBypassOutlet(airDry,15,.5,7007,"#446688",
                out _,out _),"ADP que humidifica aire seco no es deshumidificación");

            // [T9] Informes: propiedades y fuentes exportables en formato verificable.
            string markdown=ProcessReportService.BuildMarkdown(101.325,
                new List<ChartPoint>{p,humidIn},Array.Empty<PsychrometricProcess>());
            Check(markdown.Contains("s* kJ/(kgda·K)",StringComparison.Ordinal),
                "Informe incluye entropía relativa y unidades");
            Check(markdown.Contains("Presión absoluta",StringComparison.Ordinal),
                "Informe conserva presión absoluta");
            Check(markdown.Contains("Procedencia de los estados",StringComparison.Ordinal),
                "Informe conserva trazabilidad del método");
            Check(BibliographyCatalog.ExportCsv().Contains("MoistAirSpecificEntropy",
                StringComparison.Ordinal),
                "Bibliografía exporta referencia del modelo de entropía");

            // [T5] Unidades sin cambio de estado
            double original=101.325;
            Check(Math.Abs(PsychrometricUnits.PressureToSI(
                PsychrometricUnits.PressureFromSI(original,UnitSystem.IP),UnitSystem.IP)-original)<1e-8,
                "Presión SI/IP");
            Check(Math.Abs(PsychrometricUnits.TemperatureToSI(
                PsychrometricUnits.TemperatureFromSI(25,UnitSystem.IP),UnitSystem.IP)-25)<1e-8,
                "Temperatura SI/IP");
            Check(Math.Abs(PsychrometricUnits.HumidityToSI(
                PsychrometricUnits.HumidityFromSI(p.HumidityRatioKgPerKg,UnitSystem.IP),UnitSystem.IP)
                -p.HumidityRatioKgPerKg)<1e-9,"Humedad SI/IP");

            // [T13] Zona 2: los textos didácticos usan estados reales del
            // motor, ecuaciones resueltas, advertencias y fuente explícita.
            string[] lessonKinds={"Heating","Cooling","Evaporative","Steam",
                "Dehumidification","Mixing","HeatingHumidification",
                "Desiccant","Bypass","SHR"};
            foreach(string kind in lessonKinds)
            {
                Check(TheoryLessonService.TryEvaluate(kind,25,50,.5,101.325,
                    out var lesson,out string why)&&lesson is not null,
                    "Lección "+kind+": "+why);
                Check(lesson!.Initial.PressureKPa==101.325 &&
                    lesson.Initial.DryBulbC==25,"Estado inicial del motor "+kind);
                Check(!string.IsNullOrWhiteSpace(lesson.Equations)&&
                    !string.IsNullOrWhiteSpace(lesson.Steps)&&
                    !string.IsNullOrWhiteSpace(lesson.Warning),
                    "Explicación, ecuación, pasos y advertencias "+kind);
                Check(lesson.Answers.Count==3 && lesson.CorrectAnswer>=0 &&
                    lesson.CorrectAnswer<3 && lesson.AnswerExplanation.Length>10,
                    "Autoevaluación docente válida "+kind);
                Check(lesson.References.Contains("ASHRAE") &&
                    lesson.References.Contains("A03"),
                    "Referencias de clase y libro "+kind);
                if(kind=="SHR")
                    Check(lesson.Final is null &&
                        lesson.Warning.Contains("no se debe dibujar"),
                        "FCS indeterminado sin punto fabricado");
                else
                    Check(lesson.Final is not null &&
                        lesson.Route.Count>=2 &&
                        lesson.Final.RelativeHumidityPercent<=100.001,
                        "Ruta física en lección "+kind);
            }
            Check(TheoryLessonService.TryEvaluate("Cooling",25,80,1,101.325,
                out var limited,out _) && limited!.Final is not null &&
                Math.Abs(limited.Final.DryBulbC-limited.Initial.DewPointC)<.02 &&
                limited.Warning.Contains("LIMITÓ"),
                "Teoría detiene enfriamiento sensible al llegar a condensación");
            Check(TheoryLessonService.TryEvaluate("Mixing",25,50,.25,101.325,
                out var mixLesson,out _) &&
                mixLesson!.Final is not null && mixLesson.Auxiliary is not null &&
                mixLesson.Final.HumidityRatioKgPerKg>mixLesson.Initial.HumidityRatioKgPerKg &&
                mixLesson.Final.HumidityRatioKgPerKg<mixLesson.Auxiliary.HumidityRatioKgPerKg,
                "Interacción de mezcla mantiene W entre las entradas");
            Check(!TheoryLessonService.TryEvaluate("Steam",25,50,2,101.325,out _,out _),
                "Rechazo de intensidad docente fuera del dominio");
            Check(TheoryLessonService.TryEvaluate("Mixing",24,55,.13,101.325,
                out var mixFromA03,out _,32,70) && mixFromA03?.Auxiliary is not null &&
                Math.Abs(mixFromA03.Auxiliary.DryBulbC-32)<1e-9 &&
                Math.Abs(mixFromA03.Auxiliary.RelativeHumidityPercent-70)<.005,
                "A03/23 entrada exterior 32°C/70% HR parametrizable");
            Check(TheoryLessonService.Variables.Count>=12,
                "Diccionario de propiedades ASHRAE ampliado");
            Check(TheoryLessonService.DescribeVariable("W",p).Contains("g/kgda") &&
                TheoryLessonService.DescribeVariable("RH",p).Contains("HR"),
                "Inspector de variables usa estado actual y ecuación");

            // [T14] Curso completo: teoría de carta y TODAS las 36 diapositivas.
            Check(TheoryCourseCatalog.Topics.Count>=31,"Curso incluye carta y A03");
            Check(TheoryCourseCatalog.Topics.Count(t=>t.Group=="Carta")>=12,
                "Doce capítulos originales de lectura de la carta");
            Check(TheoryCourseCatalog.CoveredSlides.SequenceEqual(
                Enumerable.Range(1,36)),"A03: cobertura exacta de diapositivas 1..36");
            foreach(var topic in TheoryCourseCatalog.Topics)
            {
                Check(!string.IsNullOrWhiteSpace(topic.Summary) &&
                    !string.IsNullOrWhiteSpace(topic.PhysicalExplanation) &&
                    !string.IsNullOrWhiteSpace(topic.ChartReading) &&
                    !string.IsNullOrWhiteSpace(topic.Equation) &&
                    !string.IsNullOrWhiteSpace(topic.Procedure) &&
                    !string.IsNullOrWhiteSpace(topic.Limits) &&
                    !string.IsNullOrWhiteSpace(topic.Source),
                    "Tema académico íntegro: "+topic.Id);
                Check(TheoryCourseCatalog.DetailedReading(topic).Contains("Procedencia:"),
                    "Trazabilidad explícita: "+topic.Id);
            }
            for(int slide=1;slide<=36;slide++)
            {
                var topic=TheoryCourseCatalog.ForSlide(slide);
                Check(topic is not null && topic.FirstSlide<=slide &&
                    topic.LastSlide>=slide,"Navegación de diapositiva "+slide);
            }
            Check(TheoryCourseCatalog.Find("BASE-03")!.Equation.Contains("W_sat"),
                "Envolvente de saturación está documentada");
            Check(TheoryCourseCatalog.Find("BASE-10")!.Limits.Contains("referencias"),
                "Entropía relativa distinta de Δh");
            Check(TheoryCourseCatalog.ForSlide(28)!.Limits.Contains("NO datos de diseño"),
                "Clima docente no implica dato meteorológico certificado");
            Check(TheoryCourseCatalog.ForSlide(36)!.Summary.Contains("150 kW"),
                "Enunciado auditorio completo conservado");
            string slide36=SlideExamples.Evaluate(36,101.325);
            Check(slide36.StartsWith("DIAPOSITIVA 36") &&
                slide36.Contains("150 kW")&&slide36.Contains("50 kW") &&
                slide36.Contains("serpentín")&&slide36.Contains("ṁ_agua fría"),
                "A03/36 balance completo y caudal de agua");
            Check(slide36.Contains("SUPUESTO") && slide36.Contains("NO es posible seleccionar"),
                "A03/36 hipótesis explícitas y ausencia de equipo ficticio");
            Check(SlideExamples.Evaluate(32,101.325).Contains("BF"),
                "A03/32 incluye RSHF y bypass");
            Check(!SlideExamples.Evaluate(36,101.325).Contains("No se pudo resolver"),
                "A03/36 estados válidos a presión normal");

            // [T15] Orden pedagógico ASHRAE y geometría solicitada 0–60 g/kgda.
            Check(AshraeStudyCatalog.Units.Count==18,"Temario ASHRAE tiene 18 capítulos originales");
            Check(AshraeStudyCatalog.Equations.Count==12,"Doce ecuaciones matemáticas documentadas");
            Check(AshraeStudyCatalog.ConstructionSteps.Count==12,"Doce pasos de construcción");
            Check(AshraeStudyCatalog.Units[0].Title.Contains("Aire seco") &&
                AshraeStudyCatalog.Units[8].Title.Contains("Convención de ejes"),
                "Temario empieza por composición antes de las cartas");
            foreach(var eq in AshraeStudyCatalog.Equations)
                Check(eq.Numerator.Length>0&&eq.Denominator.Length>0 &&
                    eq.Origin.Length>20 && eq.Derivation.Length>20,
                    "Ecuación derivada y fracción legible "+eq.Key);
            foreach(var step in AshraeStudyCatalog.ConstructionSteps)
                Check(step.WhyThisMatters.Length>20 && step.Basis.Length>20,
                    "Paso con origen y explicación "+step.Number);
            Check(Math.Abs(ChartGeometry.StandardMaxHumidityRatioKgPerKg-.060)<1e-12,
                "Carta estándar con rango 0–60 g/kgda");
            var graphicalRange=new ChartGeometry();
            graphicalRange.SetStandardChartMode(true);
            Check(Math.Abs(graphicalRange.MaxHumidityRatioKgPerKg-.06)<1e-12,
                "Activar Carta estándar mantiene escala de humedad solicitada");
            Check(graphicalRange.BottomMargin<=65,
                "Margen inferior compacto sin barras de desplazamiento");

            Console.WriteLine($"PRUEBAS SUPERADAS: {_checks}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
