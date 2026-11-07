using System;
using System.Collections.Generic;
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
