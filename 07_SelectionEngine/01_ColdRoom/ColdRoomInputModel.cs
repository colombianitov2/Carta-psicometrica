namespace FrioCalc.Psychrometrics.SelectionEngine.ColdRoom;

// ------------------------------ Sección 7.2.1 - Modelo de entradas de cámara frigorífica ------------------------------

public sealed record RoomDimensionsInput(
    double LengthM,
    double WidthM,
    double HeightM,
    DimensionReference Reference);

public sealed record SurroundingsInput(
    double SurroundingTemperatureC,
    double SurroundingRelativeHumidityPercent,
    bool FloorInsulated,
    double FloorTemperatureC);

public sealed record GoodsInput(
    string? GoodsName,
    StorageType StorageType,
    RespirationHeatType RespirationHeatType,
    double TotalMassKg,
    double DailyChangedMassKg,
    double InletTemperatureC);

public sealed record InfiltrationInput(
    AirExchangeInputMode Mode,
    double OutdoorTemperatureC,
    double OutdoorRelativeHumidityPercent,
    DoorOpeningFrequency? DoorOpeningFrequency,
    double? AirChangesPerDay);

public sealed record AdditionalLoadsInput(
    double LightsW,
    double FansW,
    double OtherW,
    double PeopleHoursPerDay,
    bool DefrostEnabled,
    DefrostMethod DefrostMethod,
    double DefrostPowerW,
    double DefrostsPerDay,
    double DefrostMinutesPerCycle);

public sealed record RoomConditionsInput(
    double RoomTemperatureC,
    double RoomRelativeHumidityPercent,
    double OperatingHoursPerDay);

public sealed record PanelInput(
    string? PanelFamily,
    double? ThicknessM,
    double? UValueWPerM2K,
    bool UValueDefined);

public sealed record ColdRoomInputModel(
    RoomDimensionsInput Dimensions,
    SurroundingsInput Surroundings,
    GoodsInput Goods,
    InfiltrationInput Infiltration,
    AdditionalLoadsInput AdditionalLoads,
    RoomConditionsInput RoomConditions,
    PanelInput Panels);

// Procedencia: campos observados en TCOLDROOMVALUEWIZARDDLG y
// TCOMMERCIALCOLDROOMDLG, normalizados a unidades explícitas de FrioCalc.
// Este modelo NO implica que el algoritmo completo de carga haya sido recuperado.

// ------------------------------ Fin de sección 7.2.1 ------------------------------
