# Mapa del proyecto FrioCalc.Psychrometrics

## 1. Core

- `1.x` — `01_Core`
  - `AtmosphericPressure.cs`
  - `MoistAirEnthalpy.cs`
  - `PressurePreset.cs`
  - `PressurePresetCatalog.cs`
  - `SaturationHumidityRatio.cs`
  - `SaturationVaporPressure.cs`
  - `1.7` — `RelativeHumidity.cs`
  - `1.8` — `WetBulbHumidityRatio.cs`
  - `1.9` — `MoistAirSpecificVolume.cs`

## 2. Carta psicrométrica

- `2.1` — `PsychrometricChartView.xaml/.cs`
- `2.2` — `ChartGeometry.cs`
- `2.3` — `DryBulbLayer.cs`
- `2.4` — `HumidityRatioLayer.cs` (nombre visible: Humedad absoluta)
- `2.5` — `SaturationCurveLayer.cs`
- `2.6` — `EnthalpyLayer.cs`
- `2.7` — `RelativeHumidityLayer.cs`
- `2.8` — `WetBulbLayer.cs`
- `2.9` — `SpecificVolumeLayer.cs`

## 5. Interfaz

- `5.1` — `MainWindow.xaml/.cs`
- `5.2` — `PressureSelector.xaml/.cs`
- Panel de variables — `ChartMetricsPanel.xaml/.cs`

## Convención de identificadores

- `X` identifica bloques XAML.
- `C` identifica bloques code-behind C#.
- Ejemplo: `5.2X.4.1` es un bloque XAML dentro de `PressureSelector`; `5.2C.5.1` es su lógica asociada.
- No renumerar secciones existentes por insertar nuevas responsabilidades.
