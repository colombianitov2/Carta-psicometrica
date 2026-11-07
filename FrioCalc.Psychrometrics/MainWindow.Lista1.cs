using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics;

// [5.1C.9] Coordinación de la Lista 1; estado de puntos y procesos separados del render.
public partial class MainWindow
{
    private readonly List<int> _selectedPointIds = new();
    private readonly List<PsychrometricProcess> _savedProcesses = new();
    private int _nextProcessId = 1;
    private int? _editingPointId;
    private readonly Stack<SessionV1> _undoHistory = new();
    private bool _restoringHistory;

    private void InitializeLista1()
    {
        PointPropertiesPanel.AddPointRequested += AddStatePoint;
        PsychrometricChart.PointsAboutToChange += RememberHistory;
        PointPropertiesPanel.OpenProcessesRequested += () => WorkspaceTabs.SelectedIndex = 1;
        PointPropertiesPanel.DeletePointRequested += DeleteStatePoint;
        PointPropertiesPanel.EditPointRequested += BeginPointEdit;
        PointPropertiesPanel.DuplicatePointRequested += DuplicatePoint;
        PointPropertiesPanel.SelectionChanged += ids =>
        {
            _selectedPointIds.Clear();
            _selectedPointIds.AddRange(ids);
            RefreshSelectionList();
        };
        ChartMetricsPanel.SensibleHeatFactorVisibilityChanged += visible =>
        {
            PsychrometricChart.SetSensibleHeatFactorVisible(visible);
            ProcessChart.SetSensibleHeatFactorVisible(visible);
        };
        ChartMetricsPanel.UnitsChanged += units =>
        {
            PressureSelector.SetUnitSystem(units);
            PointPropertiesPanel.SetUnitSystem(units);
            PsychrometricChart.SetDisplayUnits(units);
            ProcessChart.SetDisplayUnits(units);
            ProcessResultText.Text =
                "Unidades cambiadas en ejes principales, entradas y propiedades. Se están auditando las etiquetas auxiliares.";
        };
        PsychrometricChart.SetSensibleHeatFactorVisible(false);
        ProcessChart.SetPressure(PressureSelector.CurrentPressureKPa);
        ProcessChart.SetPointSelectionMode(false);
        ProcessChart.SetProcessMemberColorMode(true);
        ProcessChart.SetDryBulbVisible(true);
        ProcessChart.SetHumidityRatioVisible(true);
        ProcessChart.SetSaturationCurveVisible(true);
        ProcessChart.SetRelativeHumidityVisible(true);
        ProcessChart.SetEnthalpyVisible(true);
        ProcessChart.SetWetBulbVisible(true);
        ProcessChart.SetSpecificVolumeVisible(true);
        ProcessChart.SetSensibleHeatFactorVisible(false);
        RefreshSelectionList();
    }

    // [5.1C.10] Puntos entrados por teclado.
    private void AddStatePoint()
    {
        if (!ChartMetricsPanel.TryReadStateConditions(out var conditions, out string error))
        {
            MessageBox.Show(error, "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }
        bool ok = _editingPointId.HasValue
            ? PsychrometricChart.TryUpdatePoint(_editingPointId.Value,conditions,
                ChartMetricsPanel.SelectedSolveMethod,out error)
            : PsychrometricChart.TryAddPointFromConditions(conditions,
                ChartMetricsPanel.SelectedSolveMethod,out error);
        if (!ok)
        {
            MessageBox.Show(error,"No se pudo calcular el punto",MessageBoxButton.OK,MessageBoxImage.Warning); return;
        }
        _editingPointId = null;
        ChartMetricsPanel.ClearStateConditions();
    }

    private void BeginPointEdit(int id)
    {
        ChartPoint? point = PsychrometricChart.ActivePoints.FirstOrDefault(p=>p.Id==id);
        if (point is null) return;
        ChartMetricsPanel.ClearStateConditions();
        ChartMetricsPanel.SetStateInputSI(StateProperty.DryBulb,point.DryBulbC);
        ChartMetricsPanel.SetStateInputSI(StateProperty.HumidityRatio,point.HumidityRatioKgPerKg);
        _editingPointId = id;
        WorkspaceTabs.SelectedIndex = 0;
        MessageBox.Show($"Editando {point.Name}. Modifique las condiciones y pulse Ubicar punto para guardar sin cambiar su identificador.",
            "Editar punto",MessageBoxButton.OK,MessageBoxImage.Information);
    }

    private void DuplicatePoint(int id)
    {
        ChartPoint? point = PsychrometricChart.ActivePoints.FirstOrDefault(p=>p.Id==id);
        if (point is null) return;
        var conditions = new Dictionary<StateProperty,double>
        {
            [StateProperty.DryBulb]=point.DryBulbC,
            [StateProperty.HumidityRatio]=point.HumidityRatioKgPerKg
        };
        if (!PsychrometricChart.TryAddPointFromConditions(conditions,out string error))
            MessageBox.Show(error,"Duplicar punto",MessageBoxButton.OK,MessageBoxImage.Warning);
    }

    private void DeleteStatePoint(int id)
    {
        bool used = _savedProcesses.Any(p => p.PointIds.Contains(id));
        if (used && MessageBox.Show(
                "Este punto forma parte de procesos guardados. Al eliminarlo también se eliminarán los procesos relacionados. ¿Continuar?",
                "Eliminar punto", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        if (PsychrometricChart.DeletePoint(id))
        {
            _savedProcesses.RemoveAll(p => p.PointIds.Contains(id));
            UpdateProcessesInCharts();
        }
    }

    private void OnLista1PointsChanged(IReadOnlyList<ChartPoint> points)
    {
        ProcessChart.SetSharedPoints(points);
        _savedProcesses.RemoveAll(process => process.PointIds.Any(id => !points.Any(p => p.Id == id)));
        _selectedPointIds.RemoveAll(id => !points.Any(p => p.Id == id));
        RefreshSelectionList();
        UpdateProcessesInCharts();
    }

    private void OnLista1PressureChanged(double pressureKPa)
    {
        ProcessChart.SetPressure(pressureKPa);
        _savedProcesses.Clear();
        _selectedPointIds.Clear();
        _nextProcessId = 1;
        _editingPointId = null;
        _undoHistory.Clear();
        PointPropertiesPanel.ClearSelection();
        ProcessResultText.Text = "La presión cambió; puntos y procesos anteriores se descartan para evitar estados inconsistentes.";
        UpdateProcessesInCharts();
    }

    // [5.1C.11] Selección ordenada para la segunda carta.
    private void RefreshSelectionList()
    {
        ProcessSelectionList.ItemsSource = _selectedPointIds
            .Select((id, index) => $"{index + 1}. P{id}")
            .ToArray();
    }

    private void MovePointUp_Click(object sender, RoutedEventArgs e)
        => MoveSelection(-1);

    private void MovePointDown_Click(object sender, RoutedEventArgs e)
        => MoveSelection(1);

    private void MoveSelection(int direction)
    {
        int current = ProcessSelectionList.SelectedIndex;
        int next = current + direction;
        if (current < 0 || next < 0 || next >= _selectedPointIds.Count) return;
        (_selectedPointIds[current], _selectedPointIds[next]) =
            (_selectedPointIds[next], _selectedPointIds[current]);
        RefreshSelectionList();
        ProcessSelectionList.SelectedIndex = next;
    }

    // [5.1C.12] Graficación, cálculos y agrupación de procesos independientes.
    private void PlotProcess_Click(object sender, RoutedEventArgs e)
    {
        string typeValue = (ProcessTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Automatic";
        if (!Enum.TryParse(typeValue, out ProcessType type)) type = ProcessType.Automatic;
        bool predictive = type is ProcessType.EvaporativeHumidification or ProcessType.SteamHumidification or ProcessType.Bypass;
        if (_selectedPointIds.Count < (predictive ? 1 : 2))
        {
            MessageBox.Show(predictive ? "Seleccione al menos un punto de entrada." :
                "Seleccione al menos dos puntos en Carta y puntos.",
                "Proceso", MessageBoxButton.OK, MessageBoxImage.Information); return;
        }
        var points = PsychrometricChart.ActivePoints;
        var selected = _selectedPointIds
            .Select(id => points.FirstOrDefault(p => p.Id == id)).ToArray();
        if (selected.Any(p => p == null)) return;

        if (!TryReadOptionalFlow(DryFlowTextBox.Text, out double? massFlow, out string error))
        {
            MessageBox.Show(error, "Caudal inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }

        string predictionNote = "";
        if (selected.Length == 1 && predictive)
        {
            ChartPoint inlet = selected[0]!;
            ChartPoint? predicted = null;
            bool success;
            if (type == ProcessType.EvaporativeHumidification)
            {
                if (!TryReadUnitInterval(EffectivenessTextBox.Text,out double efficiency))
                { MessageBox.Show("Indique efectividad ε entre 0 y 1.","Humidificación",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
                success = ProcessCalculator.TryEvaporativeOutlet(inlet,efficiency,0,inlet.ColorHex,
                    out predicted,out predictionNote);
            }
            else if (type == ProcessType.Bypass)
            {
                if (!TryReadPositiveOrZero(ApparatusDewPointTextBox.Text,out double adp) ||
                    !TryReadUnitInterval(BypassFactorTextBox.Text,out double bf))
                { MessageBox.Show("Indique ADP numérico (°C) y BF de 0 a 1.","Bypass",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
                success = ProcessCalculator.TryBypassOutlet(inlet,adp,bf,0,inlet.ColorHex,
                    out predicted,out predictionNote);
            }
            else
            {
                if (!TryReadPositiveOrZero(SteamAddedTextBox.Text,out double waterGrams) ||
                    !TryReadPositive(SteamEnthalpyTextBox.Text,out double steamEnthalpy))
                { MessageBox.Show("Indique vapor añadido ≥ 0 g/kg y entalpía positiva (kJ/kg).",
                    "Humidificación por vapor",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
                success = ProcessCalculator.TrySteamOutlet(inlet,waterGrams/1000.0,steamEnthalpy,
                    0,inlet.ColorHex,out predicted,out predictionNote);
            }
            if (!success || predicted == null)
            { MessageBox.Show(predictionNote,"Predicción no válida",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
            var target = new Dictionary<StateProperty,double>
            {
                [StateProperty.DryBulb] = predicted.DryBulbC,
                [StateProperty.HumidityRatio] = predicted.HumidityRatioKgPerKg
            };
            if (!PsychrometricChart.TryAddPointFromConditions(target,out error))
            { MessageBox.Show(error,"Salida fuera de la carta",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
            _selectedPointIds.Add(PsychrometricChart.ActivePoints.Last().Id);
            selected = new ChartPoint?[]{inlet,PsychrometricChart.ActivePoints.Last()};
        }
        var ids = new List<int>(_selectedPointIds);
        string description;
        if (type == ProcessType.AdiabaticMixing)
        {
            if (selected.Length != 2)
            {
                MessageBox.Show("La mezcla en esta versión requiere dos corrientes de entrada.",
                    "Mezcla",MessageBoxButton.OK,MessageBoxImage.Warning); return;
            }
            if (!TryReadPositive(MixFlowFirstTextBox.Text,out double v1) ||
                !TryReadPositive(MixFlowSecondTextBox.Text,out double v2))
            {
                MessageBox.Show("Indique ambos caudales volumétricos positivos en m³/h.",
                    "Mezcla",MessageBoxButton.OK,MessageBoxImage.Warning); return;
            }
            var first = selected[0]!;
            var second = selected[1]!;
            var inputs = new List<(ChartPoint State,double DryAirMassFlowKgS)>
            {
                (first, PsychrometricUnits.FlowToDryAirMassKgS(v1/3600.0,first.SpecificVolumeM3PerKgDryAir)),
                (second,PsychrometricUnits.FlowToDryAirMassKgS(v2/3600.0,second.SpecificVolumeM3PerKgDryAir))
            };
            if (!ProcessCalculator.TryMix(inputs,0,first.ColorHex,out ChartPoint? mixed,out error) || mixed == null)
            {
                MessageBox.Show(error,"Mezcla no resuelta",MessageBoxButton.OK,MessageBoxImage.Warning); return;
            }
            var constraints = new Dictionary<StateProperty,double>
            {
                [StateProperty.DryBulb]=mixed.DryBulbC,
                [StateProperty.HumidityRatio]=mixed.HumidityRatioKgPerKg
            };
            if (!PsychrometricChart.TryAddPointFromConditions(constraints,out error))
            {
                MessageBox.Show(error,"Mezcla fuera de la carta",MessageBoxButton.OK,MessageBoxImage.Warning); return;
            }
            ChartPoint added = PsychrometricChart.ActivePoints.Last();
            ids.Add(added.Id);
            double m = inputs.Sum(s => s.DryAirMassFlowKgS);
            description = $"Mezcla: ṁda total={m:0.####} kg/s; T={added.DryBulbC:0.##} °C; " +
                $"W={added.HumidityRatioKgPerKg*1000:0.###} g/kg; h={added.EnthalpyKJPerKgDryAir:0.###} kJ/kg. " +
                "Balances de masa de aire seco, agua y entalpía. Sin condensación.";
        }
        else
        {
            var report = new StringBuilder();
            for (int i=0;i<selected.Length-1;i++)
            {
                if (!ProcessCalculator.TryCalculate(selected[i]!,selected[i+1]!,
                    type == ProcessType.Bypass ? ProcessType.Automatic : type,
                    massFlow,out ProcessResult result,out error))
                {
                    MessageBox.Show($"{selected[i]!.Name} → {selected[i+1]!.Name}: {error}",
                        "Proceso no resuelto",MessageBoxButton.OK,MessageBoxImage.Warning); return;
                }
                report.AppendLine($"{selected[i]!.Name} → {selected[i+1]!.Name}");
                report.AppendLine($"ΔT={result.DeltaTemperatureC:0.###} °C; ΔW={result.DeltaHumidityRatioKgPerKg*1000:0.###} g/kg; Δh={result.DeltaEnthalpyKJPerKgDryAir:0.###} kJ/kgda.");
                if (result.ThermalPowerKW.HasValue)
                    report.AppendLine($"Potencia (entalpía del aire): {result.ThermalPowerKW.Value:0.###} kW; agua={result.MoistureFlowKgS:0.#####} kg/s.");
                report.AppendLine(result.Diagnostic);
            }
            description = predictionNote + Environment.NewLine + report.ToString();
        }
        ChartPoint initial = selected[0]!;
        int processId = _nextProcessId++;
        RememberHistory();
        _savedProcesses.Add(new PsychrometricProcess(
            processId,$"PR{processId}",initial.ColorHex,type,ids,massFlow));
        UpdateProcessesInCharts();
        ProcessResultText.Text = description;
        PointPropertiesPanel.ClearSelection();
        WorkspaceTabs.SelectedIndex = 1;
    }

    private void DeleteProcess_Click(object sender, RoutedEventArgs e)
    {
        int index = SavedProcessesList.SelectedIndex;
        if (index < 0 || index >= _savedProcesses.Count) return;
        RememberHistory();
        _savedProcesses.RemoveAt(index);
        UpdateProcessesInCharts();
        ProcessResultText.Text = "Proceso eliminado. Los puntos originales se conservan.";
    }

    private void UpdateProcessesInCharts()
    {
        PsychrometricChart.SetProcesses(_savedProcesses);
        ProcessChart.SetProcesses(_savedProcesses);
        SavedProcessesList.ItemsSource = _savedProcesses.Select(p =>
            $"{p.Name} · {p.Type} · {string.Join(" → ",p.PointIds.Select(id => "P"+id))}").ToArray();
    }

    private static bool TryReadPositiveOrZero(string raw, out double value)
    {
        bool valid = double.TryParse(raw.Trim(),NumberStyles.Float,CultureInfo.CurrentCulture,out value)
            || double.TryParse(raw.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value);
        return valid && double.IsFinite(value);
    }

    private static bool TryReadUnitInterval(string raw, out double value)
        => TryReadPositiveOrZero(raw,out value) && value >= 0 && value <= 1;

    private static bool TryReadPositive(string raw, out double value)
    {
        bool valid = double.TryParse(raw.Trim(),NumberStyles.Float,CultureInfo.CurrentCulture,out value)
            || double.TryParse(raw.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value);
        return valid && double.IsFinite(value) && value > 0;
    }

    private static bool TryReadOptionalFlow(string raw, out double? flow, out string error)
    {
        flow = null; error = "";
        if (string.IsNullOrWhiteSpace(raw)) return true;
        if (!TryReadPositive(raw,out double value))
        { error="Introduzca un flujo másico de aire seco positivo, en kg/s."; return false; }
        flow = value;
        return true;
    }



    // [5.1C.15] Deshacer cambios de puntos o procesos; historial de sesión.
    private void RememberHistory()
    {
        if (_restoringHistory) return;
        _undoHistory.Push(new SessionV1(1,PressureSelector.CurrentPressureKPa,
            ChartMetricsPanel.CurrentUnits,PsychrometricChart.ActivePoints.ToList(),
            new List<PsychrometricProcess>(_savedProcesses)));
    }

    private void UndoLastAction_Click(object sender, RoutedEventArgs e)
    {
        if (_undoHistory.Count==0)
        {
            ProcessResultText.Text="No existen acciones para deshacer."; return;
        }
        SessionV1 previous=_undoHistory.Pop();
        _restoringHistory=true;
        try
        {
            PsychrometricChart.SetSharedPoints(previous.Points);
            _savedProcesses.Clear();
            _savedProcesses.AddRange(previous.Processes);
            _nextProcessId=Math.Max(1,_savedProcesses.Count==0?1:_savedProcesses.Max(x=>x.Id)+1);
            OnPointsChanged(PsychrometricChart.ActivePoints);
            PointPropertiesPanel.ClearSelection();
            ProcessResultText.Text="Se restauró el estado de puntos y procesos previo a la última acción.";
        }
        finally { _restoringHistory=false; }
    }

    // [5.1C.14] Archivos de sesión versionados: estado de puntos y procesos.
    private sealed record SessionV1(int Version, double PressureKPa, UnitSystem Units,
        List<ChartPoint> Points, List<PsychrometricProcess> Processes);

    private void SaveSession_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title="Guardar proyecto FrioCalc", Filter="FrioCalc JSON|*.friocalc.json|JSON|*.json",
            FileName="FrioCalc_Sesion.friocalc.json"
        };
        if (dialog.ShowDialog(this) != true) return;
        var session = new SessionV1(1, PressureSelector.CurrentPressureKPa,
            ChartMetricsPanel.CurrentUnits, PsychrometricChart.ActivePoints.ToList(),
            new List<PsychrometricProcess>(_savedProcesses));
        File.WriteAllText(dialog.FileName,
            JsonSerializer.Serialize(session,new JsonSerializerOptions { WriteIndented=true }),
            new UTF8Encoding(false));
    }

    private void LoadSession_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title="Abrir sesión FrioCalc", Filter="FrioCalc JSON|*.friocalc.json;*.json|Todos|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var session=JsonSerializer.Deserialize<SessionV1>(File.ReadAllText(dialog.FileName));
            if (session is null || session.Version != 1 || session.Points is null ||
                session.Processes is null || !double.IsFinite(session.PressureKPa) ||
                session.PressureKPa <= 0)
                throw new InvalidDataException("Archivo de sesión incompatible.");
            if (session.Points.Any(p=>Math.Abs(p.PressureKPa-session.PressureKPa)>1e-6) ||
                session.Points.GroupBy(p=>p.Id).Any(g=>g.Count()>1) ||
                session.Processes.Any(p=>p.PointIds.Any(id=>!session.Points.Any(x=>x.Id==id))))
                throw new InvalidDataException("Puntos o procesos contradictorios en el archivo.");
            if (MessageBox.Show("Esto reemplazará los puntos y procesos actuales. ¿Continuar?",
                "Abrir sesión",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
            PressureSelector.ApplyExternalPressure(session.PressureKPa);
            PsychrometricChart.SetSharedPoints(session.Points);
            OnPointsChanged(PsychrometricChart.ActivePoints);
            _savedProcesses.Clear();
            _savedProcesses.AddRange(session.Processes);
            _nextProcessId=Math.Max(1,_savedProcesses.Count==0 ? 1 : _savedProcesses.Max(p=>p.Id)+1);
            _selectedPointIds.Clear();
            PointPropertiesPanel.ClearSelection();
            UpdateProcessesInCharts();
            WorkspaceTabs.SelectedIndex=0;
            ProcessResultText.Text="Sesión cargada. Confirme la coherencia de cálculos y métodos.";
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or ArgumentException)
        {
            MessageBox.Show(ex.Message,"Error al abrir sesión",MessageBoxButton.OK,MessageBoxImage.Error);
        }
    }

    // [5.1C.13] Casos didácticos y exportación auditable.
    private void ExampleCooling_Click(object sender, RoutedEventArgs e)
    {
        ChartMetricsPanel.ClearStateConditions();
        ChartMetricsPanel.SetStateInputSI(StateProperty.DryBulb,35);
        ChartMetricsPanel.SetStateInputSI(StateProperty.HumidityRatio,.010);
        WorkspaceTabs.SelectedIndex=0;
    }
    private void ExampleHumidifier_Click(object sender, RoutedEventArgs e)
    {
        ChartMetricsPanel.ClearStateConditions();
        ChartMetricsPanel.SetStateInputSI(StateProperty.DryBulb,30);
        ChartMetricsPanel.SetStateInputSI(StateProperty.RelativeHumidity,10);
        WorkspaceTabs.SelectedIndex=0;
    }

    private void ExportReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title="Exportar resultados psicrométricos",
            Filter="CSV (UTF-8)|*.csv",
            FileName="FrioCalc_Psicrometria_Informe.csv"
        };
        if (dialog.ShowDialog(this)!=true) return;
        var sb=new StringBuilder();
        sb.AppendLine("Tipo;ID;Nombre;Presion_kPa_abs;Tbs_C;Tbh_C;Trocio_C;HR_pct;W_kg_kgda;h_kJ_kgda;v_m3_kgda");
        foreach(var p in PsychrometricChart.ActivePoints)
            sb.AppendLine(FormattableString.Invariant($"Punto;{p.Id};{p.Name};{p.PressureKPa};{p.DryBulbC};{p.WetBulbC};{p.DewPointC};{p.RelativeHumidityPercent};{p.HumidityRatioKgPerKg};{p.EnthalpyKJPerKgDryAir};{p.SpecificVolumeM3PerKgDryAir}"));
        sb.AppendLine("Tipo;ID;Nombre;Metodo;Puntos;Flujo_mda_kg_s;Nota");
        foreach(var p in _savedProcesses)
            sb.AppendLine($"Proceso;{p.Id};{p.Name};{p.Type};{string.Join(">",p.PointIds)};{p.DryAirMassFlowKgS};Verificar balance y supuestos en la interfaz");
        sb.AppendLine("Fuente;ASHRAE Handbook-Fundamentals capitulo psicrometria;PsychroLib;CoolProp HumidAir");
        sb.AppendLine("Advertencia;CSV no sustituye auditoria de balances de condensado ni modelos de equipos.");
        File.WriteAllText(dialog.FileName,sb.ToString(),new UTF8Encoding(true));
        ProcessResultText.Text="Informe CSV exportado. Revise las advertencias y unidades de cada columna.";
    }
}
