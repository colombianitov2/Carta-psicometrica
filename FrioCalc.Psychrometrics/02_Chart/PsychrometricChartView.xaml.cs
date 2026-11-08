using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;
using FrioCalc.Psychrometrics.Core;

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
    private bool _specificEntropyVisible;

    private bool _sensibleHeatFactorVisible;
    private ChartPoint? _sensibleHeatFactorGuideOrigin;
    private double? _sensibleHeatFactorGuideRatio;
    private bool _processMemberColorMode;
    private readonly List<PsychrometricProcess> _processes = new();

    private bool _pointSelectionEnabled;

    private readonly List<ChartPoint> _chartPoints =
        new();

    private int _nextPointId =
        1;

    public event Action<IReadOnlyList<ChartPoint>>? PointsChanged;
    public event Action? PointsAboutToChange;
    private readonly Dictionary<string,bool> _numberVisibility = new();

    // Etiquetas y trazos se controlan por separado: apagar números NO borra las curvas.
    public void SetNumberVisibility(string family,bool visible)
    {
        _numberVisibility[family]=visible;
        RefreshChart();
    }

    private void DrawWithLabelPreference(string family,Action render)
    {
        int first=ChartCanvas.Children.Count;
        render();
        if(_numberVisibility.TryGetValue(family,out bool show) && !show)
            foreach(var item in ChartCanvas.Children.Cast<UIElement>().Skip(first))
                if(item is TextBlock label && label.Text.Any(char.IsDigit))
                    label.Visibility=Visibility.Collapsed;
    }

    // Confirmar antes de quitar con clic derecho un punto usado por procesos.
    public event Func<int,bool>? PointDeletionRequested;

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

        UpdatePressureLabel();

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

    // [2.1C.8.6] Isolíneas opcionales de entropía relativa (no curva ASHRAE estándar).
    public void SetSpecificEntropyVisible(bool isVisible)
    {
        _specificEntropyVisible=isVisible;
        RefreshChart();
    }

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
            DrawWithLabelPreference("HumidityRatio",()=>HumidityRatioLayer.Draw(ChartCanvas,_geometry));
        }

        if (_dryBulbVisible)
        {
            DrawWithLabelPreference("DryBulb",()=>DryBulbLayer.Draw(ChartCanvas,_geometry));
        }

        if (_relativeHumidityVisible)
        {
            DrawWithLabelPreference("RelativeHumidity",()=>RelativeHumidityLayer.Draw(ChartCanvas,_geometry,_pressureKPa));
        }

        if (_specificVolumeVisible)
        {
            DrawWithLabelPreference("SpecificVolume",()=>SpecificVolumeLayer.Draw(ChartCanvas,_geometry,_pressureKPa));
        }

        if (_enthalpyVisible)
        {
            DrawWithLabelPreference("Enthalpy",()=>EnthalpyLayer.Draw(ChartCanvas,_geometry,_pressureKPa));
        }

        if (_wetBulbVisible)
        {
            DrawWithLabelPreference("WetBulb",()=>WetBulbLayer.Draw(ChartCanvas,_geometry,_pressureKPa));
        }

        if (_saturationCurveVisible)
        {
            DrawWithLabelPreference("Saturation",()=>SaturationCurveLayer.Draw(ChartCanvas,_geometry,_pressureKPa));
        }

        if (_specificEntropyVisible)
            DrawWithLabelPreference("SpecificEntropy",()=>SpecificEntropyLayer.Draw(ChartCanvas,_geometry,_pressureKPa));

        if (_enthalpyDeviationVisible)
        {
            DrawWithLabelPreference("EnthalpyDeviation",()=>EnthalpyDeviationLayer.Draw(ChartCanvas,_geometry,_pressureKPa));
        }

        if (_sensibleHeatFactorVisible)
        {
            DrawWithLabelPreference("FCS",()=>SensibleHeatFactorScaleLayer.Draw(ChartCanvas,_geometry));
        }

        ProcessLayer.Draw(ChartCanvas, _geometry, _chartPoints, _processes);
        if (_sensibleHeatFactorGuideOrigin is ChartPoint origin && _sensibleHeatFactorGuideRatio is double fcs)
            SensibleHeatFactorGuideLayer.Draw(ChartCanvas,_geometry,origin,fcs,_pressureKPa);

        PointLayer.Draw(
            ChartCanvas,
            _geometry,
            _chartPoints);

        if (_processMemberColorMode)
            ProcessLayer.DrawMemberColors(ChartCanvas, _geometry, _chartPoints, _processes);

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
            int idToDelete=_chartPoints[existingPointIndex].Id;
            if(PointDeletionRequested is not null && !PointDeletionRequested.Invoke(idToDelete))
            {
                e.Handled=true;
                return;
            }
            PointsAboutToChange?.Invoke();
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
            PointsAboutToChange?.Invoke();
            _chartPoints.Add(point with { Origin = "Clic derecho", Method = "Tbs + W (ratón)" });

            _nextPointId++;

            NotifyPointsChanged();

            RefreshChart();
        }
        else
        {
            MessageBox.Show("No es un estado válido del aire húmedo sin condensación a esta presión. "+
                "Haga clic debajo de la curva de saturación y dentro de los límites visibles.",
                "Punto no válido",MessageBoxButton.OK,MessageBoxImage.Information);
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



    public void SetProcessMemberColorMode(bool enabled)
    {
        _processMemberColorMode = enabled;
        RefreshChart();
    }

    public void SetDisplayUnits(UnitSystem units)
    {
        _geometry.DisplayUnits = units;
        UpdatePressureLabel();
        RefreshChart();
    }

    // [2.1C.25] La etiqueta de presión debe corresponder al sistema de unidades elegido.
    private void UpdatePressureLabel()
    {
        var units=_geometry.DisplayUnits;
        double displayed=PsychrometricUnits.PressureFromSI(_pressureKPa,units);
        PressureValueText.Text=$"Presión: {displayed.ToString("0.###",CultureInfo.CurrentCulture)} "+
            (units==UnitSystem.SI?"kPa abs":"psi abs");
    }

    // [2.1C.24] Exportación PNG directa del Canvas visible, en resolución 2×.
    public void ExportPng(string fileName)
    {
        if (ChartCanvas.ActualWidth < 100 || ChartCanvas.ActualHeight < 100)
            throw new InvalidOperationException("La carta debe estar visible antes de exportarse.");
        int width=(int)Math.Ceiling(ChartCanvas.ActualWidth*2.0);
        int height=(int)Math.Ceiling(ChartCanvas.ActualHeight*2.0);
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(
            width,height,192,192,System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(ChartCanvas);
        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream=System.IO.File.Create(fileName);
        encoder.Save(stream);
    }

    // [2.1C.19] FCS independiente del resto de las familias gráficas.
    // [2.1C.23] Proyección de familia de estados compatibles con un FCS conocido.
    public void SetSensibleHeatFactorGuide(ChartPoint? origin,double? factor)
    {
        _sensibleHeatFactorGuideOrigin=origin;
        _sensibleHeatFactorGuideRatio=factor;
        RefreshChart();
    }

    public void SetSensibleHeatFactorVisible(bool isVisible)
    {
        _sensibleHeatFactorVisible = isVisible;
        RefreshChart();
    }

    // [2.1C.20] Creación de punto con restricciones, sin alterar otros puntos.
    public bool TryAddPointFromConditions(
        IReadOnlyDictionary<StateProperty, double> conditions, out string diagnostic)
        => TryAddPointFromConditions(conditions,StateSolveMethod.Automatic,out diagnostic);

    public bool TryAddPointFromConditions(
        IReadOnlyDictionary<StateProperty, double> conditions, StateSolveMethod method,
        out string diagnostic)
    {
        string color = PointColorPalette.GetColorHex(_nextPointId);
        if (!StateInputSolver.TrySolve(conditions, _pressureKPa, _nextPointId, color, method,
            out ChartPoint? point, out diagnostic) || point == null) return false;
        if (point.DryBulbC < _geometry.ActiveMinDryBulbC ||
            point.DryBulbC > _geometry.ActiveMaxDryBulbC ||
            point.HumidityRatioKgPerKg < _geometry.ActiveMinHumidityRatioKgPerKg ||
            point.HumidityRatioKgPerKg > _geometry.ActiveMaxHumidityRatioKgPerKg)
        {
            diagnostic = "Punto físico válido, pero fuera de los límites activos de la carta. Amplíe los límites o desactive Carta estándar.";
            return false;
        }
        PointsAboutToChange?.Invoke();
        _chartPoints.Add(point);
        _nextPointId++;
        NotifyPointsChanged();
        RefreshChart();
        return true;
    }

    // [2.1C.21] Compartir el estado de puntos con la carta de procesos.
    public void SetSharedPoints(IReadOnlyList<ChartPoint> points)
    {
        _chartPoints.Clear();
        _chartPoints.AddRange(points);
        _nextPointId = 1;
        foreach (var point in points) _nextPointId = Math.Max(_nextPointId, point.Id + 1);
        RefreshChart();
    }

    public void SetProcesses(IReadOnlyList<PsychrometricProcess> processes)
    {
        _processes.Clear();
        _processes.AddRange(processes);
        RefreshChart();
    }

    // [2.1C.22] Edición validada preservando identidad y color del punto.
    public bool TryUpdatePoint(int id, IReadOnlyDictionary<StateProperty,double> conditions,
        StateSolveMethod method, out string diagnostic)
    {
        int index = _chartPoints.FindIndex(p => p.Id == id);
        if (index < 0) { diagnostic = "El punto ya no existe."; return false; }
        ChartPoint previous = _chartPoints[index];
        if (!StateInputSolver.TrySolve(conditions,_pressureKPa,id,previous.ColorHex,method,
            out ChartPoint? newPoint,out diagnostic) || newPoint == null) return false;
        if (newPoint.DryBulbC < _geometry.ActiveMinDryBulbC ||
            newPoint.DryBulbC > _geometry.ActiveMaxDryBulbC ||
            newPoint.HumidityRatioKgPerKg < _geometry.ActiveMinHumidityRatioKgPerKg ||
            newPoint.HumidityRatioKgPerKg > _geometry.ActiveMaxHumidityRatioKgPerKg)
        {
            diagnostic = "El punto editado está fuera de los límites activos de la carta."; return false;
        }
        PointsAboutToChange?.Invoke();
        _chartPoints[index] = newPoint with { Name=previous.Name };
        NotifyPointsChanged();
        RefreshChart();
        return true;
    }

    public bool DeletePoint(int id)
    {
        int index = _chartPoints.FindIndex(p => p.Id == id);
        if (index < 0) return false;
        PointsAboutToChange?.Invoke();
        _chartPoints.RemoveAt(index);
        NotifyPointsChanged();
        RefreshChart();
        return true;
    }

    public IReadOnlyList<ChartPoint> ActivePoints => _chartPoints.AsReadOnly();

    // ------------------------------ Sección 2.1C.18 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.1C.18 ------------------------------