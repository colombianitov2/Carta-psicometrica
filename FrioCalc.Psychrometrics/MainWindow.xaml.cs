using System.Windows;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics;

// ------------------------------ Sección 5.1C.1 - Definición de ventana principal ------------------------------

public partial class MainWindow : Window
{

    // ------------------------------ Fin de sección 5.1C.1 ------------------------------


    // ------------------------------ Sección 5.1C.2 - Inicialización ------------------------------

    public MainWindow()
    {
        InitializeComponent();

        PressureSelector.PressureApplied +=
            OnPressureApplied;

        PressureSelector.StandardChartModeChanged +=
            OnStandardChartModeChanged;

        ChartMetricsPanel.DryBulbVisibilityChanged +=
            OnDryBulbVisibilityChanged;

        ChartMetricsPanel.HumidityRatioVisibilityChanged +=
            OnHumidityRatioVisibilityChanged;

        ChartMetricsPanel.SaturationCurveVisibilityChanged +=
            OnSaturationCurveVisibilityChanged;

        ChartMetricsPanel.EnthalpyVisibilityChanged +=
            OnEnthalpyVisibilityChanged;

        ChartMetricsPanel.RelativeHumidityVisibilityChanged +=
            OnRelativeHumidityVisibilityChanged;

        ChartMetricsPanel.WetBulbVisibilityChanged +=
            OnWetBulbVisibilityChanged;

        ChartMetricsPanel.SpecificVolumeVisibilityChanged +=
            OnSpecificVolumeVisibilityChanged;

        ChartMetricsPanel.EnthalpyDeviationVisibilityChanged +=
            OnEnthalpyDeviationVisibilityChanged;

        PsychrometricChart.SetPressure(
            AtmosphericPressure.StandardSeaLevelKPa);
    }

    // ------------------------------ Fin de sección 5.1C.2 ------------------------------


    // ------------------------------ Sección 5.1C.3 - Aplicación de presión ------------------------------

    private void OnPressureApplied(
        double pressureKPa)
    {
        PsychrometricChart.SetPressure(
            pressureKPa);
    }

    // ------------------------------ Fin de sección 5.1C.3 ------------------------------


    // ------------------------------ Sección 5.1C.3.1 - Aplicación de Carta estándar ------------------------------

    private void OnStandardChartModeChanged(
        bool isEnabled)
    {
        PsychrometricChart.SetStandardChartMode(
            isEnabled);
    }

    // ------------------------------ Fin de sección 5.1C.3.1 ------------------------------


    // ------------------------------ Sección 5.1C.4 - Visibilidad de bulbo seco ------------------------------

    private void OnDryBulbVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetDryBulbVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.4 ------------------------------


    // ------------------------------ Sección 5.1C.5 - Visibilidad de humedad absoluta ------------------------------

    private void OnHumidityRatioVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetHumidityRatioVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.5 ------------------------------


    // ------------------------------ Sección 5.1C.6 - Visibilidad de curva de saturación ------------------------------

    private void OnSaturationCurveVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetSaturationCurveVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.6 ------------------------------


    // ------------------------------ Sección 5.1C.7 - Visibilidad de entalpía ------------------------------

    private void OnEnthalpyVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetEnthalpyVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.7 ------------------------------


    // ------------------------------ Sección 5.1C.7.1 - Visibilidad de humedad relativa ------------------------------

    private void OnRelativeHumidityVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetRelativeHumidityVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.7.1 ------------------------------


    // ------------------------------ Sección 5.1C.7.2 - Visibilidad de temperatura de bulbo húmedo ------------------------------

    private void OnWetBulbVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetWetBulbVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.7.2 ------------------------------


    // ------------------------------ Sección 5.1C.7.3 - Visibilidad de volumen específico ------------------------------

    private void OnSpecificVolumeVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetSpecificVolumeVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.7.3 ------------------------------


    // ------------------------------ Sección 5.1C.7.4 - Visibilidad de desviación de entalpía ------------------------------

    private void OnEnthalpyDeviationVisibilityChanged(
        bool isVisible)
    {
        PsychrometricChart.SetEnthalpyDeviationVisible(
            isVisible);
    }

    // ------------------------------ Fin de sección 5.1C.7.4 ------------------------------


    // ------------------------------ Sección 5.1C.8 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 5.1C.8 ------------------------------