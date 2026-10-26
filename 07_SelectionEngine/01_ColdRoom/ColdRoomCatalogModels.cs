namespace FrioCalc.Psychrometrics.SelectionEngine.ColdRoom;

// ------------------------------ Sección 7.7.1 - Definiciones de mercancías y paneles ------------------------------

public sealed record ColdRoomGoodsDefinition(
    string Name,
    double MinimumStorageTemperatureC,
    double MaximumStorageTemperatureC,
    double DefaultStorageTemperatureC,
    double MinimumRelativeHumidityPercent,
    double MaximumRelativeHumidityPercent,
    double DefaultRelativeHumidityPercent,
    double FreezingTemperatureC,
    double SpecificHeatAboveFreezingKJPerKgK,
    double SpecificHeatFrozenKJPerKgK,
    double HeatOfFreezingKJPerKg,
    RespirationHeatType RespirationHeatType,
    IReadOnlySet<StorageType> SupportedStorageTypes,
    double LoadingDensityKgPerM3,
    double LoadingCoefficient);

public sealed record StandardPanelDefinition(
    string FamilyName,
    double ThicknessM,
    double? UValueWPerM2K,
    double? ThermalConductivityWPerMK,
    string? PanelType);

// Procedencia estructural:
// TStandardGoods, TStandardWallPanel y TStandardWallPanelFamily observados
// en 07_TIPOS_Y_ESTRUCTURAS_DE_CALCULO.txt.
//
// Importante: este archivo define el ESQUEMA de datos propio de FrioCalc.
// No contiene registros ni catálogos comerciales recuperados de Danfoss.

// ------------------------------ Fin de sección 7.7.1 ------------------------------
