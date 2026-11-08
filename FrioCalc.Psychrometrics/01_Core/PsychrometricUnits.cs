using System;

namespace FrioCalc.Psychrometrics.Core;

// [1.11] SI interno obligatorio; IP solo para presentación e introducción.
public enum UnitSystem { SI, IP }

public static class PsychrometricUnits
{
    public static double TemperatureToSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : (value - 32.0) * 5.0 / 9.0;
    public static double TemperatureFromSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value * 9.0 / 5.0 + 32.0;
    public static double HumidityToSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value / 1000.0 : value / 7000.0;
    public static double HumidityFromSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value * 1000.0 : value * 7000.0;
    // Entropía específica: kJ/(kg_da K) a Btu/(lb_da °F), solo diferencia o referencia explícita.
    public static double EntropyFromSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value*0.23884589662749594;
    public static double EntropyToSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value/0.23884589662749594;

    public static double EnthalpyToSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value / 0.429922614;
    public static double EnthalpyFromSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value * 0.429922614;
    public static double VolumeToSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value / 16.01846337;
    public static double VolumeFromSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value * 16.01846337;
    public static double PressureToSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value / 0.1450377377;
    public static double PressureFromSI(double value, UnitSystem units)
        => units == UnitSystem.SI ? value : value * 0.1450377377;
    public static double FlowToDryAirMassKgS(double volumeFlowM3S, double specificVolumeM3PerKgDryAir)
    {
        if (!double.IsFinite(volumeFlowM3S) || volumeFlowM3S < 0 ||
            !double.IsFinite(specificVolumeM3PerKgDryAir) || specificVolumeM3PerKgDryAir <= 0)
            throw new ArgumentOutOfRangeException(nameof(volumeFlowM3S));
        return volumeFlowM3S / specificVolumeM3PerKgDryAir;
    }
}
