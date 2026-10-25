namespace FrioCalc.Psychrometrics.Points;

// ------------------------------ Sección 3.1.1 - Modelo de punto psicrométrico ------------------------------

public sealed record ChartPoint(
    int Id,
    string Name,
    string ColorHex,
    double PressureKPa,
    double DryBulbC,
    double HumidityRatioKgPerKg,
    double RelativeHumidityPercent,
    double WetBulbC,
    double DewPointC,
    double EnthalpyKJPerKgDryAir,
    double SpecificVolumeM3PerKgDryAir);

// ------------------------------ Fin de sección 3.1.1 ------------------------------
