using System.Collections.Generic;
using System.Windows;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics;

// ------------------------------ Sección 5.1C.1 - Definición de ventana principal ------------------------------

public partial class MainWindow : Window
{

    // ------------------------------ Fin de sección 5.1C.1 ------------------------------


    // ------------------------------ Sección 5.1C.2 - Inicialización ------------------------------

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged+=(sender,args)=>ApplyResponsiveWorkspaceLayout();

        PressureSelector.PressureApplied +=
            OnPressureApplied;

        PressureSelector.StandardChartModeChanged +=
            OnStandardChartModeChanged;

        PressureSelector.PointSelectionModeChanged +=
            OnPointSelectionModeChanged;

        PsychrometricChart.PointsChanged +=
            OnPointsChanged;

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
        ChartMetricsPanel.SpecificEntropyVisibilityChanged += visible =>
            PsychrometricChart.SetSpecificEntropyVisible(visible);

        PsychrometricChart.SetPressure(
            AtmosphericPressure.StandardSeaLevelKPa);
        InitializeLista1();
        ApplyResponsiveWorkspaceLayout();
        ProcessTheoryPanel.LessonChanged += AshraeStudyPanel.SetLiveLesson;
        AshraeStudyPanel.SetPressure(PressureSelector.CurrentPressureKPa);
        AshraeStudyPanel.SetLiveLesson(ProcessTheoryPanel.CurrentLesson);
        ProcessTheoryPanel.LessonChanged += TheoryCoursePanel.SetContext;
        TheoryCoursePanel.SetContext(ProcessTheoryPanel.CurrentLesson);
        TheoryCoursePanel.LessonRequested += kind =>
        {
            ProcessTheoryPanel.SelectKind(kind);
            ProcessTheoryPanel.BringIntoView();
        };
        TheoryCoursePanel.ExerciseRequested += slide =>
        {
            TheoryCoursePanel.ShowSlide(slide);
            ShowEducationalSlide(slide);
        };
    }

    // ------------------------------ Fin de sección 5.1C.2 ------------------------------


    // [5.1C.2.1] No desperdiciar un tercio de la pantalla en
    // Procesos y Teoría: el panel lateral solo es necesario en Carta.
    private void WorkspaceTabs_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if(!ReferenceEquals(e.Source,WorkspaceTabs))return;
        ApplyResponsiveWorkspaceLayout();
    }

    private void ApplyResponsiveWorkspaceLayout()
    {
        if(WorkspaceTabs is null||SettingsScrollViewer is null)return;
        bool carta=WorkspaceTabs.SelectedIndex==0;
        SettingsScrollViewer.Visibility=carta?Visibility.Visible:Visibility.Collapsed;
        double windowWidth=ActualWidth>0?ActualWidth:Width;
        SettingsColumn.Width=new GridLength(carta?Math.Clamp(windowWidth*.29,285,355):0);
        SettingsGapColumn.Width=new GridLength(carta?8:0);
    }

    // ------------------------------ Sección 5.1C.3 - Aplicación de presión ------------------------------

    private void OnPressureApplied(
        double pressureKPa)
    {
        PsychrometricChart.SetPressure(
            pressureKPa);
        OnLista1PressureChanged(pressureKPa);
    }

    // ------------------------------ Fin de sección 5.1C.3 ------------------------------


    // ------------------------------ Sección 5.1C.3.1 - Aplicación de Carta estándar ------------------------------

    private void OnStandardChartModeChanged(
        bool isEnabled)
    {
        PsychrometricChart.SetStandardChartMode(
            isEnabled);
        ProcessChart.SetStandardChartMode(isEnabled);
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


    // ------------------------------ Sección 5.1C.7.5 - Selección y propiedades de puntos ------------------------------

    private void OnPointSelectionModeChanged(
        bool isEnabled)
    {
        PsychrometricChart.SetPointSelectionMode(
            isEnabled);
    }

    private void OnPointsChanged(
        IReadOnlyList<ChartPoint> points)
    {
        PointPropertiesPanel.SetPoints(
            points);
        OnLista1PointsChanged(points);
    }

    // ------------------------------ Fin de sección 5.1C.7.5 ------------------------------


    // ------------------------------ Sección 5.1C.8 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 5.1C.8 ------------------------------