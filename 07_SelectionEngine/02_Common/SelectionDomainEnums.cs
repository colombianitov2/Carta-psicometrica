namespace FrioCalc.Psychrometrics.SelectionEngine.Common;

// ------------------------------ Sección 7.5.1 - Vocabulario común de selección ------------------------------

public enum CapacityBasis
{
    EvaporatorCapacity = 0,
    CondenserCapacity = 1,
    LineMassFlow = 2
}

public enum SelectionCriterion
{
    Default = 0,
    Size = 1,
    Velocity = 2,
    PressureDrop = 3,
    Capacity = 4,
    SaturationTemperatureDrop = 5
}

public enum SaturationTemperatureReference
{
    Dew = 0,
    Midpoint = 1,
    Bubble = 2
}

public enum TemperatureDifferenceBasis
{
    Bubble = 0,
    Dew = 1,
    TwoPhase = 2
}

public enum FluidPhaseState
{
    Gas = 0,
    Liquid = 1,
    TwoPhase = 2,
    Fluid = 3
}

public enum RefrigerationApplication
{
    MediumTemperature = 0,
    LowTemperature = 1,
    Heating = 2,
    AirConditioning = 3,
    HighTemperature = 4
}

// Procedencia conceptual:
// 07_TIPOS_Y_ESTRUCTURAS_DE_CALCULO.txt.
// Los nombres se normalizan a terminología propia de FrioCalc.
// La existencia de estos tipos no demuestra por sí sola el algoritmo de selección.

// ------------------------------ Fin de sección 7.5.1 ------------------------------
