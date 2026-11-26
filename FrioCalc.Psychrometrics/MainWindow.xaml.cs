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
        InitializeUpdates();
        PsychrometricChart.EnableViewControls();
        PsychrometricChart.FullScreenRequested += ToggleChartFullScreen;
        PsychrometricChart.FullScreenPointSelectionRequested += PressureSelector.SetPointSelectionEnabled;
        PreviewKeyDown += (_, e) =>
        {
            if (_isChartFullScreen && e.Key == System.Windows.Input.Key.Escape)
            {
                ToggleChartFullScreen();
                e.Handled = true;
            }
        };
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
        AshraeSimulatorPanel.SetSimulatorMode(true);
        BibliographyTopics.ItemsSource=FrioCalc.Psychrometrics.Processes.AshraeStudyCatalog.Units;
        ProcessTheoryPanel.LessonChanged += AshraeSimulatorPanel.SetLiveLesson;
        AshraeSimulatorPanel.SetPressure(PressureSelector.CurrentPressureKPa);
        AshraeSimulatorPanel.SetLiveLesson(ProcessTheoryPanel.CurrentLesson);
        ProcessTheoryPanel.LessonChanged += TheoryCoursePanel.SetContext;
        TheoryCoursePanel.SetContext(ProcessTheoryPanel.CurrentLesson);
        TheoryCoursePanel.LessonRequested += kind =>
        {
            ProcessTheoryPanel.SelectKind(kind);
            WorkspaceTabs.SelectedIndex=3;
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
        if(_isChartFullScreen) return;
        bool carta=WorkspaceTabs.SelectedIndex==0;
        SettingsScrollViewer.Visibility=carta?Visibility.Visible:Visibility.Collapsed;
        double windowWidth=ActualWidth>0?ActualWidth:Width;
        SettingsColumn.Width=new GridLength(carta?Math.Clamp(windowWidth*.29,285,355):0);
        SettingsGapColumn.Width=new GridLength(carta?8:0);
    }

    // [5.1C.12.1] La vista se traslada al host de pantalla completa,
    // conservando el objeto, eventos, geometría, puntos, procesos y selección.
    private bool _isChartFullScreen;
    private System.Windows.Controls.Panel? _chartOriginalParent;
    private int _chartOriginalIndex;
    private double _chartOriginalHeight;
    private Thickness _chartOriginalMargin;
    private double _chartOriginalScrollOffset;
    private WindowState _chartOriginalWindowState;
    private WindowStyle _chartOriginalWindowStyle;
    private ResizeMode _chartOriginalResizeMode;
    private Rect _chartOriginalRestoreBounds;

    private void ToggleChartFullScreen()
    {
        if (!_isChartFullScreen)
        {
            if (PsychrometricChart.Parent is not System.Windows.Controls.Panel parent) return;
            _chartOriginalParent = parent;
            _chartOriginalIndex = parent.Children.IndexOf(PsychrometricChart);
            _chartOriginalHeight = PsychrometricChart.Height;
            _chartOriginalMargin = PsychrometricChart.Margin;
            _chartOriginalScrollOffset = ChartPageScroll.VerticalOffset;
            _chartOriginalWindowState = WindowState;
            _chartOriginalWindowStyle = WindowStyle;
            _chartOriginalResizeMode = ResizeMode;
            _chartOriginalRestoreBounds = RestoreBounds;
            _isChartFullScreen = true;
            PsychrometricChart.SetFullScreenPresentation(true);
            parent.Children.Remove(PsychrometricChart);
            PsychrometricChart.Height = double.NaN;
            PsychrometricChart.Margin = new Thickness(8);
            ChartFullScreenHost.Children.Add(PsychrometricChart);
            WorkspaceTabs.Visibility = Visibility.Collapsed;
            SettingsScrollViewer.Visibility = Visibility.Collapsed;
            ChartFullScreenHost.Visibility = Visibility.Visible;
            WindowState = System.Windows.WindowState.Normal;
            WindowStyle = System.Windows.WindowStyle.None;
            ResizeMode = System.Windows.ResizeMode.NoResize;
            WindowState = System.Windows.WindowState.Maximized;
        }
        else
        {
            if (_chartOriginalParent is null) return;
            PsychrometricChart.SetFullScreenPresentation(false);
            ChartFullScreenHost.Children.Remove(PsychrometricChart);
            ChartFullScreenHost.Visibility = Visibility.Collapsed;
            PsychrometricChart.Height = _chartOriginalHeight;
            PsychrometricChart.Margin = _chartOriginalMargin;
            _chartOriginalParent.Children.Insert(_chartOriginalIndex, PsychrometricChart);
            _chartOriginalParent = null;
            WindowState = System.Windows.WindowState.Normal;
            WindowStyle = _chartOriginalWindowStyle;
            ResizeMode = _chartOriginalResizeMode;
            if (!_chartOriginalRestoreBounds.IsEmpty &&
                _chartOriginalRestoreBounds.Width > 0 && _chartOriginalRestoreBounds.Height > 0)
            {
                Left = _chartOriginalRestoreBounds.Left;
                Top = _chartOriginalRestoreBounds.Top;
                Width = _chartOriginalRestoreBounds.Width;
                Height = _chartOriginalRestoreBounds.Height;
            }
            WindowState = _chartOriginalWindowState;
            WorkspaceTabs.Visibility = Visibility.Visible;
            _isChartFullScreen = false;
            ApplyResponsiveWorkspaceLayout();
            UpdateLayout();
            ChartPageScroll.ScrollToVerticalOffset(_chartOriginalScrollOffset);
        }
        UpdateLayout();
        PsychrometricChart.RefreshChart();
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
