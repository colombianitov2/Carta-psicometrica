namespace FrioCalc.Psychrometrics.SelectionEngine.ColdRoom;

// ------------------------------ Sección 7.1.1 - Enumeraciones del módulo de cámara frigorífica ------------------------------

public enum DimensionReference
{
    Inner = 0,
    Outer = 1
}

public enum StorageType
{
    Fresh = 0,
    Packed = 1,
    Frozen = 2
}

public enum RespirationHeatType
{
    None = 0,
    Vegetables = 1,
    Fruits = 2
}

public enum DoorOpeningFrequency
{
    Often = 0,
    Regular = 1,
    Rare = 2
}

public enum AirExchangeInputMode
{
    DoorOpenings = 0,
    AirChangesPerDay = 1
}

public enum DefrostMethod
{
    Natural = 0,
    Electric = 1
}

public enum ExpansionValveType
{
    Electronic = 0,
    Thermostatic = 1
}

// Procedencia:
// - StorageType y RespirationHeatType: símbolos TD32 recuperados.
// - DoorOpeningFrequency: símbolo TD32 recuperado y formulario DFM.
// - DimensionReference, AirExchangeInputMode, DefrostMethod y ExpansionValveType:
//   alternativas observadas directamente en formularios DFM.
// No representan código fuente original ni algoritmos internos.

// ------------------------------ Fin de sección 7.1.1 ------------------------------
