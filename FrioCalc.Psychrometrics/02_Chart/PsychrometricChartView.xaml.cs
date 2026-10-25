using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.1C.1 - Definición de la vista de carta ------------------------------

public partial class PsychrometricChartView : UserControl
{

    // ------------------------------ Fin de sección 2.1C.1 ------------------------------


    // ------------------------------ Sección 2.1C.2 - Estado gráfico ------------------------------

    private readonly ChartGeometry _geometry =
        new();

    private bool _dryBulbVisible;

    private bool _humidityRatioVisible;

    private bool _saturationCurveVisible;

    private bool _enthalpyVisible;

    private bool _relativeHumidityVisible;

    private bool _wetBulbVisible;

    private bool _specificVolumeVisible;

    private bool _enthalpyDeviationVisible;

    private bool _pointSelectionEnabled;

    private readonly List<ChartPoint> _chartPoints =
        new();

    private int _nextPointId =
        1;

    public event Action<IReadOnlyList<ChartPoint>>? PointsChanged;

    private double _pressureKPa =
        101.325;

    private bool _isPanning;

    private Point _lastPanPoint;

    private bool _updatingScrollBars;

    // ------------------------------ Fin de sección 2.1C.2 ------------------------------


    // ------------------------------ Sección 2.1C.3 - Inicialización ------------------------------

    public PsychrometricChartView()
    {
        InitializeComponent();

        Loaded +=
            PsychrometricChartView_Loaded;

        SizeChanged +=
            PsychrometricChartView_SizeChanged;
    }

    // ------------------------------ Fin de sección 2.1C.3 ------------------------------


    // ------------------------------ Sección 2.1C.4 - Presión de trabajo ------------------------------

    public void SetPressure(
        double pressureKPa)
    {
        bool pressureChanged =
            Math.Abs(
                pressureKPa
                - _pressureKPa)
            > 1e-9;

        _pressureKPa =
            pressureKPa;

        PressureValueText.Text =
            $"Presión: {pressureKPa.ToString("0.###", CultureInfo.CurrentCulture)} kPa abs";

        if (pressureChanged)
        {
            ClearPoints();
        }

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.4 ------------------------------


    // ------------------------------ Sección 2.1C.4.1 - Carta estándar ------------------------------

    public void SetStandardChartMode(
        bool isEnabled)
    {
        _geometry.SetStandardChartMode(
            isEnabled);

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.4.1 ------------------------------


    // ------------------------------ Sección 2.1C.5 - Visibilidad de bulbo seco ------------------------------

    public void SetDryBulbVisible(
        bool isVisible)
    {
        _dryBulbVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.5 ------------------------------


    // ------------------------------ Sección 2.1C.6 - Visibilidad de humedad absoluta ------------------------------

    public void SetHumidityRatioVisible(
        bool isVisible)
    {
        _humidityRatioVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.6 ------------------------------


    // ------------------------------ Sección 2.1C.7 - Visibilidad de curva de saturación ------------------------------

    public void SetSaturationCurveVisible(
        bool isVisible)
    {
        _saturationCurveVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.7 ------------------------------


    // ------------------------------ Sección 2.1C.8 - Visibilidad de entalpía ------------------------------

    public void SetEnthalpyVisible(
        bool isVisible)
    {
        _enthalpyVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.8 ------------------------------


    // ------------------------------ Sección 2.1C.8.1 - Visibilidad de humedad relativa ------------------------------

    public void SetRelativeHumidityVisible(
        bool isVisible)
    {
        _relativeHumidityVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.8.1 ------------------------------


    // ------------------------------ Sección 2.1C.8.2 - Visibilidad de temperatura de bulbo húmedo ------------------------------

    public void SetWetBulbVisible(
        bool isVisible)
    {
        _wetBulbVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.8.2 ------------------------------


    // ------------------------------ Sección 2.1C.8.3 - Visibilidad de volumen específico ------------------------------

    public void SetSpecificVolumeVisible(
        bool isVisible)
    {
        _specificVolumeVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.8.3 ------------------------------


    // ------------------------------ Sección 2.1C.8.4 - Visibilidad de desviación de entalpía ------------------------------

    public void SetEnthalpyDeviationVisible(
        bool isVisible)
    {
        _enthalpyDeviationVisible =
            isVisible;

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.8.4 ------------------------------


    // ------------------------------ Sección 2.1C.8.5 - Modo de selección de puntos ------------------------------

    public void SetPointSelectionMode(
        bool isEnabled)
    {
        _pointSelectionEnabled =
            isEnabled;

        ChartCanvas.Cursor =
            isEnabled
                ? Cursors.Cross
                : Cursors.Hand;
    }

    // ------------------------------ Fin de sección 2.1C.8.5 ------------------------------


    // ------------------------------ Sección 2.1C.9 - Regeneración de la carta ------------------------------

    public void RefreshChart()
    {
        ChartCanvas.Children.Clear();

        if (_humidityRatioVisible)
        {
            HumidityRatioLayer.Draw(
                ChartCanvas,
                _geometry);
        }

        if (_dryBulbVisible)
        {
            DryBulbLayer.Draw(
                ChartCanvas,
                _geometry);
        }

        if (_relativeHumidityVisible)
        {
            RelativeHumidityLayer.Draw(
                ChartCanvas,
                _geometry,
                _pressureKPa);
        }

        if (_specificVolumeVisible)
        {
            SpecificVolumeLayer.Draw(
                ChartCanvas,
                _geometry,
                _pressureKPa);
        }

        if (_enthalpyVisible)
        {
            EnthalpyLayer.Draw(
                ChartCanvas,
                _geometry,
                _pressureKPa);
        }

        if (_wetBulbVisible)
        {
            WetBulbLayer.Draw(
                ChartCanvas,
                _geometry,
                _pressureKPa);
        }

        if (_saturationCurveVisible)
        {
            SaturationCurveLayer.Draw(
                ChartCanvas,
                _geometry,
                _pressureKPa);
        }

        if (_enthalpyDeviationVisible)
        {
            EnthalpyDeviationLayer.Draw(
                ChartCanvas,
                _geometry,
                _pressureKPa);
        }

        SensibleHeatFactorScaleLayer.Draw(
            ChartCanvas,
            _geometry);

        PointLayer.Draw(
            ChartCanvas,
            _geometry,
            _chartPoints);

        UpdateScrollBars();
    }

    // ------------------------------ Fin de sección 2.1C.9 ------------------------------


    // ------------------------------ Sección 2.1C.10 - Actualización de barras de desplazamiento ------------------------------

    private void UpdateScrollBars()
    {
        _updatingScrollBars =
            true;

        try
        {
            double dryBulbSpan =
                _geometry.MaxDryBulbC
                - _geometry.MinDryBulbC;

            double totalDryBulbSpan =
                _geometry.ActiveMaxDryBulbC
                - _geometry.ActiveMinDryBulbC;

            double maximumHorizontalValue =
                _geometry.ActiveMaxDryBulbC
                - dryBulbSpan;

            HorizontalChartScrollBar.Minimum =
                _geometry.ActiveMinDryBulbC;

            HorizontalChartScrollBar.Maximum =
                Math.Max(
                    _geometry.ActiveMinDryBulbC,
                    maximumHorizontalValue);

            HorizontalChartScrollBar.ViewportSize =
                dryBulbSpan;

            HorizontalChartScrollBar.SmallChange =
                Math.Max(
                    0.5,
                    dryBulbSpan / 20.0);

            HorizontalChartScrollBar.LargeChange =
                Math.Min(
                    dryBulbSpan * 0.8,
                    totalDryBulbSpan);

            HorizontalChartScrollBar.Value =
                Math.Clamp(
                    _geometry.MinDryBulbC,
                    HorizontalChartScrollBar.Minimum,
                    HorizontalChartScrollBar.Maximum);


            double humiditySpan =
                _geometry.MaxHumidityRatioKgPerKg
                - _geometry.MinHumidityRatioKgPerKg;

            double totalHumiditySpan =
                _geometry.ActiveMaxHumidityRatioKgPerKg
                - _geometry.ActiveMinHumidityRatioKgPerKg;

            double maximumVerticalScroll =
                Math.Max(
                    0.0,
                    totalHumiditySpan - humiditySpan);

            VerticalChartScrollBar.Minimum =
                0.0;

            VerticalChartScrollBar.Maximum =
                maximumVerticalScroll;

            VerticalChartScrollBar.ViewportSize =
                humiditySpan;

            VerticalChartScrollBar.SmallChange =
                Math.Max(
                    0.0005,
                    humiditySpan / 20.0);

            VerticalChartScrollBar.LargeChange =
                Math.Min(
                    humiditySpan * 0.8,
                    totalHumiditySpan);

            double verticalValue =
                _geometry.ActiveMaxHumidityRatioKgPerKg
                - _geometry.MaxHumidityRatioKgPerKg;

            VerticalChartScrollBar.Value =
                Math.Clamp(
                    verticalValue,
                    0.0,
                    maximumVerticalScroll);
        }
        finally
        {
            _updatingScrollBars =
                false;
        }
    }

    // ------------------------------ Fin de sección 2.1C.10 ------------------------------


    // ------------------------------ Sección 2.1C.11 - Zoom con rueda del mouse ------------------------------

    private void ChartCanvas_MouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        Rect plotRect =
            _geometry.GetPlotRect(
                ChartCanvas.ActualWidth,
                ChartCanvas.ActualHeight);

        if (plotRect.IsEmpty)
        {
            return;
        }

        Point mousePoint =
            e.GetPosition(
                ChartCanvas);

        if (!plotRect.Contains(mousePoint))
        {
            return;
        }

        double zoomFactor =
            e.Delta > 0
                ? 0.85
                : 1.15;

        _geometry.ZoomAt(
            plotRect,
            mousePoint,
            zoomFactor);

        RefreshChart();

        e.Handled =
            true;
    }

    // ------------------------------ Fin de sección 2.1C.11 ------------------------------


    // ------------------------------ Sección 2.1C.12 - Inicio de desplazamiento con la mano ------------------------------

    private void ChartCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        Rect plotRect =
            _geometry.GetPlotRect(
                ChartCanvas.ActualWidth,
                ChartCanvas.ActualHeight);

        Point mousePoint =
            e.GetPosition(
                ChartCanvas);

        if (!plotRect.Contains(mousePoint))
        {
            return;
        }

        _isPanning =
            true;

        _lastPanPoint =
            mousePoint;

        ChartCanvas.CaptureMouse();

        ChartCanvas.Cursor =
            Cursors.Hand;

        e.Handled =
            true;
    }

    // ------------------------------ Fin de sección 2.1C.12 ------------------------------


    // ------------------------------ Sección 2.1C.13 - Movimiento de la carta con la mano ------------------------------

    private void ChartCanvas_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        Point currentPoint =
            e.GetPosition(
                ChartCanvas);

        Rect plotRect =
            _geometry.GetPlotRect(
                ChartCanvas.ActualWidth,
                ChartCanvas.ActualHeight);

        if (plotRect.IsEmpty)
        {
            return;
        }

        double deltaX =
            currentPoint.X
            - _lastPanPoint.X;

        double deltaY =
            currentPoint.Y
            - _lastPanPoint.Y;

        double dryBulbSpan =
            _geometry.MaxDryBulbC
            - _geometry.MinDryBulbC;

        double humiditySpan =
            _geometry.MaxHumidityRatioKgPerKg
            - _geometry.MinHumidityRatioKgPerKg;

        double dryBulbDelta =
            -(deltaX / plotRect.Width)
            * dryBulbSpan;

        double humidityDelta =
            (deltaY / plotRect.Height)
            * humiditySpan;

        _geometry.PanBy(
            dryBulbDelta,
            humidityDelta);

        _lastPanPoint =
            currentPoint;

        RefreshChart();

        e.Handled =
            true;
    }

    // ------------------------------ Fin de sección 2.1C.13 ------------------------------


    // ------------------------------ Sección 2.1C.14 - Fin de desplazamiento con la mano ------------------------------

    private void ChartCanvas_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        EndPanning();

        e.Handled =
            true;
    }

    private void ChartCanvas_LostMouseCapture(
        object sender,
        MouseEventArgs e)
    {
        _isPanning =
            false;

        ChartCanvas.Cursor =
            _pointSelectionEnabled
                ? Cursors.Cross
                : Cursors.Hand;
    }

    private void EndPanning()
    {
        if (!_isPanning)
        {
            return;
        }

        _isPanning =
            false;

        ChartCanvas.ReleaseMouseCapture();

        ChartCanvas.Cursor =
            _pointSelectionEnabled
                ? Cursors.Cross
                : Cursors.Hand;
    }

    // ------------------------------ Fin de sección 2.1C.14 ------------------------------


    // ------------------------------ Sección 2.1C.14.1 - Selección de puntos con clic derecho ------------------------------

    private void ChartCanvas_MouseRightButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (!_pointSelectionEnabled)
        {
            return;
        }

        Rect plotRect =
            _geometry.GetPlotRect(
                ChartCanvas.ActualWidth,
                ChartCanvas.ActualHeight);

        if (plotRect.IsEmpty)
        {
            return;
        }

        Point mousePoint =
            e.GetPosition(
                ChartCanvas);

        if (!plotRect.Contains(
                mousePoint))
        {
            return;
        }

        int existingPointIndex =
            FindPointAt(
                mousePoint,
                plotRect);

        if (existingPointIndex >= 0)
        {
            _chartPoints.RemoveAt(
                existingPointIndex);

            NotifyPointsChanged();

            RefreshChart();

            e.Handled =
                true;

            return;
        }

        double dryBulbC =
            _geometry.DryBulbCFromX(
                mousePoint.X,
                plotRect);

        double humidityRatioKgPerKg =
            _geometry.HumidityRatioKgPerKgFromY(
                mousePoint.Y,
                plotRect);

        int pointId =
            _nextPointId;

        string pointName =
            $"P{pointId}";

        string colorHex =
            PointColorPalette.GetColorHex(
                pointId);

        if (PointPropertyCalculator.TryCreatePoint(
                pointId,
                pointName,
                colorHex,
                _pressureKPa,
                dryBulbC,
                humidityRatioKgPerKg,
                out ChartPoint? point)
            && point is not null)
        {
            _chartPoints.Add(
                point);

            _nextPointId++;

            NotifyPointsChanged();

            RefreshChart();
        }

        e.Handled =
            true;
    }

    // ------------------------------ Fin de sección 2.1C.14.1 ------------------------------


    // ------------------------------ Sección 2.1C.14.2 - Colección de puntos activos ------------------------------

    private int FindPointAt(
        Point mousePoint,
        Rect plotRect)
    {
        const double hitRadius =
            10.0;

        double hitRadiusSquared =
            hitRadius * hitRadius;

        for (int index = _chartPoints.Count - 1;
             index >= 0;
             index--)
        {
            ChartPoint point =
                _chartPoints[index];

            double x =
                _geometry.XFromDryBulbC(
                    point.DryBulbC,
                    plotRect);

            double y =
                _geometry.YFromHumidityRatioKgPerKg(
                    point.HumidityRatioKgPerKg,
                    plotRect);

            double deltaX =
                mousePoint.X - x;

            double deltaY =
                mousePoint.Y - y;

            double distanceSquared =
                deltaX * deltaX
                + deltaY * deltaY;

            if (distanceSquared
                <= hitRadiusSquared)
            {
                return index;
            }
        }

        return -1;
    }

    private void NotifyPointsChanged()
    {
        PointsChanged?.Invoke(
            _chartPoints);
    }

    private void ClearPoints()
    {
        if (_chartPoints.Count == 0)
        {
            return;
        }

        _chartPoints.Clear();

        NotifyPointsChanged();
    }

    // ------------------------------ Fin de sección 2.1C.14.2 ------------------------------


    // ------------------------------ Sección 2.1C.15 - Barra horizontal ------------------------------

    private void HorizontalChartScrollBar_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingScrollBars)
        {
            return;
        }

        _geometry.SetHorizontalScrollPosition(
            e.NewValue);

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.15 ------------------------------


    // ------------------------------ Sección 2.1C.16 - Barra vertical ------------------------------

    private void VerticalChartScrollBar_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingScrollBars)
        {
            return;
        }

        _geometry.SetVerticalScrollPosition(
            e.NewValue);

        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.16 ------------------------------


    // ------------------------------ Sección 2.1C.17 - Carga y cambio de tamaño ------------------------------

    private void PsychrometricChartView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        PressureValueText.Text =
            $"Presión: {_pressureKPa.ToString("0.###", CultureInfo.CurrentCulture)} kPa abs";

        RefreshChart();
    }

    private void PsychrometricChartView_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        RefreshChart();
    }

    // ------------------------------ Fin de sección 2.1C.17 ------------------------------


    // ------------------------------ Sección 2.1C.18 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.1C.18 ------------------------------