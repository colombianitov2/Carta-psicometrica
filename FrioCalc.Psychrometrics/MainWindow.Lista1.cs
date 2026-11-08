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
    private int? _editingProcessId;
    private readonly Stack<SessionV1> _undoHistory = new();
    private bool _restoringHistory;
    private bool _processChartReady;
    private readonly Dictionary<int, ChartPoint> _lastKnownPoints = new();

    private void InitializeLista1()
    {
        PointPropertiesPanel.AddPointRequested += AddStatePoint;
        PsychrometricChart.PointsAboutToChange += RememberHistory;
        PsychrometricChart.PointDeletionRequested += ConfirmPointDeletion;
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
        ProcessTheoryPanel.SetPressure(PressureSelector.CurrentPressureKPa);
        ProcessChart.SetPointSelectionMode(false);
        PsychrometricChart.SetProcessMemberColorMode(true);
        ProcessChart.SetProcessMemberColorMode(true);
        ProcessChart.SetDryBulbVisible(true);
        ProcessChart.SetHumidityRatioVisible(true);
        ProcessChart.SetSaturationCurveVisible(true);
        ProcessChart.SetRelativeHumidityVisible(true);
        ProcessChart.SetEnthalpyVisible(true);
        ProcessChart.SetWetBulbVisible(true);
        ProcessChart.SetSpecificVolumeVisible(false);
        ProcessChart.SetEnthalpyVisible(false);
        ProcessChart.SetWetBulbVisible(false);
        ProcessChart.SetRelativeHumidityVisible(true);
        ProcessChart.SetStandardChartMode(true);
        ProcessChart.SetSensibleHeatFactorVisible(false);
        _processChartReady=true;
        ChartMetricsPanel.SetBasicView();
        PressureSelector.SetStandardChartEnabled(true);
        RefreshSelectionList();
    }

    // [5.1C.9.1] Familias de líneas independientes en la segunda carta.
    private void ProcessFamilyVisibility_Changed(object sender,RoutedEventArgs e)
    {
        if(!_processChartReady) return;
        ProcessChart.SetEnthalpyVisible(ProcessEnthalpyToggle.IsChecked==true);
        ProcessChart.SetWetBulbVisible(ProcessWetBulbToggle.IsChecked==true);
        ProcessChart.SetSpecificVolumeVisible(ProcessSpecificVolumeToggle.IsChecked==true);
        ProcessChart.SetRelativeHumidityVisible(ProcessRelativeHumidityToggle.IsChecked==true);
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

    // [5.1C.10.2] El clic derecho ya no borra silenciosamente
    // procesos asociados; comparte la misma regla de confirmación que el panel.
    private bool ConfirmPointDeletion(int id)
    {
        if(!_savedProcesses.Any(p=>p.PointIds.Contains(id))) return true;
        return MessageBox.Show(
            "El punto seleccionado participa en un proceso guardado. " +
            "Eliminarlo también eliminará los procesos relacionados. ¿Continuar?",
            "Confirmar borrado",MessageBoxButton.YesNo,MessageBoxImage.Warning)
            ==MessageBoxResult.Yes;
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
        PsychrometricChart.SetSensibleHeatFactorGuide(null,null);
        ProcessChart.SetSensibleHeatFactorGuide(null,null);
        ProcessChart.SetSharedPoints(points);
        // [5.1C.10.1] Un estado modificado invalida los balances guardados que lo referencian.
        // Eliminar procesos afectados, en lugar de publicar un informe antiguo como válido.
        var changed = new HashSet<int>(points
            .Where(p => _lastKnownPoints.TryGetValue(p.Id,out var oldPoint) &&
                (Math.Abs(oldPoint.DryBulbC-p.DryBulbC)>1e-8 ||
                 Math.Abs(oldPoint.HumidityRatioKgPerKg-p.HumidityRatioKgPerKg)>1e-10 ||
                 Math.Abs(oldPoint.PressureKPa-p.PressureKPa)>1e-8))
            .Select(p=>p.Id));
        int invalidated = 0;
        if (!_restoringHistory && changed.Count>0)
        {
            invalidated = _savedProcesses.RemoveAll(process =>
                process.PointIds.Any(id=>changed.Contains(id)));
        }
        _savedProcesses.RemoveAll(process => process.PointIds.Any(id => !points.Any(p => p.Id == id)));
        _lastKnownPoints.Clear();
        foreach(var p in points) _lastKnownPoints[p.Id]=p;
        if(invalidated>0)
        {
            ProcessResultText.Text=$"{invalidated} proceso(s) invalidado(s) porque cambió un estado. " +
                "Seleccione de nuevo los puntos y recalcule.";
            SelectedProcessSummaryMainText.Text=ProcessResultText.Text;
        }
        _selectedPointIds.RemoveAll(id => !points.Any(p => p.Id == id));
        RefreshSelectionList();
        UpdateProcessesInCharts();
    }

    private void OnLista1PressureChanged(double pressureKPa)
    {
        PsychrometricChart.SetSensibleHeatFactorGuide(null,null);
        ProcessChart.SetSensibleHeatFactorGuide(null,null);
        ProcessChart.SetPressure(pressureKPa);
        ProcessTheoryPanel.SetPressure(pressureKPa);
        _savedProcesses.Clear();
        _selectedPointIds.Clear();
        _nextProcessId = 1;
        _editingPointId = null;
        _editingProcessId = null;
        _undoHistory.Clear();
        _lastKnownPoints.Clear();
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
    // [5.1C.18] Línea FCS como lugar geométrico, nunca como estado único calculado.
    private void DrawFcsGuide_Click(object sender,RoutedEventArgs e)
    {
        if(_selectedPointIds.Count<1)
        {
            MessageBox.Show("Seleccione el punto de referencia en Carta y puntos.","Guía FCS");return;
        }
        if(!TryReadPositive(FcsGuideTextBox.Text,out double ratio)||ratio>1)
        {
            MessageBox.Show("El FCS debe estar entre 0 (excluido) y 1 (incluido).","Guía FCS");return;
        }
        ChartPoint? point=PsychrometricChart.ActivePoints.FirstOrDefault(p=>p.Id==_selectedPointIds[0]);
        if(point==null) return;
        ProcessChart.SetSensibleHeatFactorGuide(point,ratio);
        PsychrometricChart.SetSensibleHeatFactorGuide(point,ratio);
        ProcessResultText.Text=$"Guía FCS={ratio:0.###} desde {point.Name}. Se necesita una restricción adicional para elegir el punto de suministro.";
        WorkspaceTabs.SelectedIndex=1;
    }

    private void ClearFcsGuide_Click(object sender,RoutedEventArgs e)
    {
        PsychrometricChart.SetSensibleHeatFactorGuide(null,null);
        ProcessChart.SetSensibleHeatFactorGuide(null,null);
        ProcessResultText.Text="Guía de FCS eliminada; los puntos y procesos se conservan.";
    }

    private void PlotProcess_Click(object sender, RoutedEventArgs e)
    {
        string typeValue = (ProcessTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Automatic";
        if (!Enum.TryParse(typeValue, out ProcessType type)) type = ProcessType.Automatic;
        bool predictive = type is ProcessType.EvaporativeHumidification or ProcessType.SteamHumidification or ProcessType.Bypass ||
            (type==ProcessType.Desiccant && _selectedPointIds.Count==1);
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
        // [5.1C.12.0] Validar el cierre ANTES de generar puntos predictivos.
        // Rechazar luego de crear la salida deja puntos huérfanos sin proceso.
        bool closeCycle=CloseCycleCheckBox.IsChecked==true;
        if(closeCycle && (type!=ProcessType.Automatic || selected.Length<3))
        {
            MessageBox.Show("Para cerrar un ciclo seleccione método Automático " +
                "y al menos tres puntos diferentes antes de generar la gráfica.",
                "Ciclo psicrométrico",MessageBoxButton.OK,MessageBoxImage.Warning);
            return;
        }
        // [5.1C.12.1] No confundir una comparación de estados con predicción de equipo.
        if (selected.Length > 1 && predictive)
        {
            MessageBox.Show("Para humidificación evaporativa, inyección de vapor o bypass, " +
                "seleccione solo el punto de entrada y proporcione los datos de equipo requeridos. " +
                "Para comparar dos estados conocidos, utilice el método Automático.",
                "Modelo de equipo no determinado",MessageBoxButton.OK,MessageBoxImage.Warning);
            return;
        }


        if (!TryReadOptionalFlow(DryFlowTextBox.Text, out double? massFlow, out string error))
        {
            MessageBox.Show(error, "Caudal inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }

        double? condensateTemperature = null;
        if (!string.IsNullOrWhiteSpace(CondensateTemperatureTextBox.Text))
        {
            if (!TryReadFinite(CondensateTemperatureTextBox.Text,out double condensateInput) ||
                condensateInput < -50 || condensateInput > 100)
            {
                MessageBox.Show("Temperatura de condensado inválida. Se requiere −50 a 100 °C.",
                    "Balance de serpentín",MessageBoxButton.OK,MessageBoxImage.Warning);return;
            }
            condensateTemperature = condensateInput;
        }
        double? branchFraction = null;
        int? generatedPointId = null;
        double? saturationEffectiveness = null, bypassFactorUsed = null;
        double? steamGramsUsed = null, steamEnthalpyUsed = null, adpUsed = null;
        double? desiccantGramsUsed = null, desiccantTUsed = null;
        List<double>? mixFlowsUsed = null;
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
                saturationEffectiveness=efficiency;
                success = ProcessCalculator.TryEvaporativeOutlet(inlet,efficiency,0,inlet.ColorHex,
                    out predicted,out predictionNote);
            }
            else if (type == ProcessType.Bypass)
            {
                if (!TryReadFinite(ApparatusDewPointTextBox.Text,out double adp) ||
                    !TryReadUnitInterval(BypassFactorTextBox.Text,out double bf))
                { MessageBox.Show("Indique ADP numérico (°C) y BF de 0 a 1.","Bypass",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
                bypassFactorUsed=bf; adpUsed=adp;
                success = ProcessCalculator.TryBypassOutlet(inlet,adp,bf,0,inlet.ColorHex,
                    out predicted,out predictionNote);
            }
            else if (type == ProcessType.Desiccant)
            {
                if (!TryReadNonNegative(DesiccantRemovedTextBox.Text,out double removed) || removed < 0 ||
                    !TryReadFinite(DesiccantTemperatureTextBox.Text,out double outputT))
                {
                    MessageBox.Show("Se necesitan ΔW retirada ≥ 0 g/kgda y la Tbs de salida (°C), " +
                        "derivada de especificación o medición. No se inventa rendimiento del material.",
                        "Desecante",MessageBoxButton.OK,MessageBoxImage.Warning);return;
                }
                desiccantGramsUsed=removed; desiccantTUsed=outputT;
                success=ProcessCalculator.TryDesiccantOutlet(inlet,removed/1000.0,outputT,
                    0,inlet.ColorHex,out predicted,out predictionNote);
            }
            else
            {
                if (!TryReadNonNegative(SteamAddedTextBox.Text,out double waterGrams) ||
                    !TryReadPositive(SteamEnthalpyTextBox.Text,out double steamEnthalpy))
                { MessageBox.Show("Indique vapor añadido ≥ 0 g/kg y entalpía positiva (kJ/kg).",
                    "Humidificación por vapor",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
                steamGramsUsed=waterGrams; steamEnthalpyUsed=steamEnthalpy;
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
            generatedPointId=PsychrometricChart.ActivePoints.Last().Id;
            _selectedPointIds.Add(generatedPointId.Value);
            selected = new ChartPoint?[]{inlet,PsychrometricChart.ActivePoints.Last()};
        }
        if(closeCycle)
            selected=selected.Concat(new[]{selected[0]}).ToArray();
        var ids = new List<int>(_selectedPointIds);
        if (closeCycle) ids.Add(ids[0]);
        string description;
        if(type==ProcessType.Branching)
        {
            if(selected.Length != 3 || massFlow is not double totalFlow ||
                !TryReadUnitInterval(BranchFractionTextBox.Text,out double fraction) ||
                fraction <= 0 || fraction >= 1)
            {
                MessageBox.Show("Seleccione entrada, salida 1 y salida 2 en ese orden. " +
                    "Introduzca caudal másico de entrada (kg/s) y fracción estrictamente entre 0 y 1.",
                    "División en ramas",MessageBoxButton.OK,MessageBoxImage.Warning);return;
            }
            if(!ProcessCalculator.TryBranchBalance(selected[0]!,selected[1]!,selected[2]!,totalFlow,
                fraction,out var balance,out error)||balance is null)
            {
                MessageBox.Show(error,"Balance de ramas inválido",MessageBoxButton.OK,MessageBoxImage.Warning);return;
            }
            branchFraction=fraction;
            description=$"División de {selected[0]!.Name} a {selected[1]!.Name} y {selected[2]!.Name}.\n" +
                $"Flujo entrada={totalFlow:0.#####} kgda/s; rama 1={balance.FirstMassFlowKgS:0.#####} kgda/s; " +
                $"rama 2={balance.SecondMassFlowKgS:0.#####} kgda/s.\n" +
                $"Agua que debe añadirse/retirarse: rama 1={balance.AddedWaterFirstKgS:0.######} kg/s; " +
                $"rama 2={balance.AddedWaterSecondKgS:0.######} kg/s.\n" +
                $"Cambio entálpico exigido: rama 1={balance.ThermalFirstKW:0.###} kW; " +
                $"rama 2={balance.ThermalSecondKW:0.###} kW.\n"+balance.Diagnostic;
        }
        else if (type == ProcessType.AdiabaticMixing)
        {
            var flows = new List<double>();
            if (!string.IsNullOrWhiteSpace(MixFlowsListTextBox.Text))
            {
                foreach (string part in MixFlowsListTextBox.Text.Split(
                    new[] {';','\n'},StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!TryReadPositive(part,out double flowValue))
                    {
                        MessageBox.Show("Cada caudal de mezcla debe ser positivo, separado por punto y coma.",
                            "Mezcla",MessageBoxButton.OK,MessageBoxImage.Warning);return;
                    }
                    flows.Add(flowValue);
                }
            }
            else if (selected.Length == 2 &&
                TryReadPositive(MixFlowFirstTextBox.Text,out double v1) &&
                TryReadPositive(MixFlowSecondTextBox.Text,out double v2))
            {
                flows.Add(v1); flows.Add(v2);
            }
            if (flows.Count != selected.Length)
            {
                MessageBox.Show($"Introduzca {selected.Length} caudales (m³/h), uno por corriente seleccionada.",
                    "Mezcla",MessageBoxButton.OK,MessageBoxImage.Warning);return;
            }
            var inlet = selected[0]!;
            var inputs = selected.Select((state,index) => (
                State: state!,
                DryAirMassFlowKgS: PsychrometricUnits.FlowToDryAirMassKgS(
                    flows[index]/3600.0,state!.SpecificVolumeM3PerKgDryAir))).ToList();
            if (!ProcessCalculator.TryMix(inputs,0,inlet.ColorHex,out ChartPoint? mixed,out error) || mixed == null)
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
            generatedPointId=added.Id;
            mixFlowsUsed=flows.ToList();
            ids.Add(added.Id);
            double m = inputs.Sum(input => input.DryAirMassFlowKgS);
            description = $"Mezcla de {selected.Length} corrientes: ṁda total={m:0.####} kg/s; T={added.DryBulbC:0.##} °C; " +
                $"W={added.HumidityRatioKgPerKg*1000:0.###} g/kg; h={added.EnthalpyKJPerKgDryAir:0.###} kJ/kg. " +
                "Balances de masa de aire seco, vapor y entalpía. Sin condensación.";
        }
        else
        {
            var report = new StringBuilder();
            for (int i=0;i<selected.Length-1;i++)
            {
                if (!ProcessCalculator.TryCalculate(selected[i]!,selected[i+1]!,
                    type == ProcessType.Bypass ? ProcessType.Automatic : type,
                    massFlow,condensateTemperature,out ProcessResult result,out error))
                {
                    MessageBox.Show($"{selected[i]!.Name} → {selected[i+1]!.Name}: {error}",
                        "Proceso no resuelto",MessageBoxButton.OK,MessageBoxImage.Warning); return;
                }
                report.AppendLine($"{selected[i]!.Name} → {selected[i+1]!.Name}");
                report.AppendLine($"ΔT={result.DeltaTemperatureC:0.###} °C; ΔW={result.DeltaHumidityRatioKgPerKg*1000:0.###} g/kg; Δh={result.DeltaEnthalpyKJPerKgDryAir:0.###} kJ/kgda.");
                if (result.ThermalPowerKW.HasValue)
                    report.AppendLine($"Potencia (entalpía del aire): {result.ThermalPowerKW.Value:0.###} kW; agua={result.MoistureFlowKgS:0.#####} kg/s.");
                if (result.CondensateMassFlowKgS is double mc)
                    report.AppendLine($"Condensado eliminado={mc:0.######} kg/s.");
                if (result.CoilCoolingLoadKW is double qc)
                    report.AppendLine($"Carga de serpentín con entalpía del condensado={qc:0.###} kW.");
                report.AppendLine(result.SensibleHeatFactor is double fcs ?
                    $"Factor de calor sensible estimado={fcs:0.###}." :
                    "FCS no definido para estos signos/balances.");
                report.AppendLine(result.Diagnostic);
            }
            description = predictionNote + Environment.NewLine + report.ToString();
            if(ProcessCycleAnalyzer.TryAnalyze(selected.Where(p=>p is not null).Select(p=>p!).ToArray(),
                massFlow,out var cycle,out var cycleError))
            {
                description += Environment.NewLine + 
                    $"Recorrido de {cycle.StageCount} tramos; cerrado={cycle.IsClosed}; " +
                    $"Δh neta={cycle.NetEnthalpyKJPerKgDryAir:0.######} kJ/kgda; " +
                    $"ΔW neta={cycle.NetHumidityRatioKgPerKg*1000:0.######} g/kgda.";
                if(cycle.NetPowerKW is double qcycle)
                    description += $" Potencia neta por entalpía del aire={qcycle:0.###} kW.";
                description += Environment.NewLine + cycle.Diagnostic;
            }
            else description += Environment.NewLine + cycleError;
        }
        ChartPoint initial = selected[0]!;
        int editingIndex=_editingProcessId is int oldId
            ? _savedProcesses.FindIndex(p=>p.Id==oldId) : -1;
        int? oldGeneratedId=editingIndex>=0
            ? _savedProcesses[editingIndex].GeneratedPointId : null;
        // Candidatos anteriores sin metadatos de procedencia (compatibilidad).
        if(editingIndex>=0 && oldGeneratedId is null &&
           _savedProcesses[editingIndex].PointIds.Count>=2 &&
           _savedProcesses[editingIndex].Type is
               (ProcessType.EvaporativeHumidification or ProcessType.SteamHumidification
               or ProcessType.Bypass or ProcessType.AdiabaticMixing))
            oldGeneratedId=_savedProcesses[editingIndex].PointIds[^1];
        int processId=editingIndex>=0 ? _savedProcesses[editingIndex].Id : _nextProcessId++;
        string name=string.IsNullOrWhiteSpace(NewProcessNameTextBox.Text)
            ? $"PR{processId}" : NewProcessNameTextBox.Text.Trim();
        RememberHistory();
        var saved = new PsychrometricProcess(
            processId,name,initial.ColorHex,type,ids,massFlow,Report:description,
            CondensateTemperatureC:condensateTemperature,BranchFractionA:branchFraction,
            GeneratedPointId:generatedPointId,SaturationEffectiveness:saturationEffectiveness,
            BypassFactor:bypassFactorUsed,SteamAddedGramsPerKg:steamGramsUsed,
            SteamEnthalpyKJPerKg:steamEnthalpyUsed,ApparatusDewPointC:adpUsed,
            DesiccantRemovedGramsPerKg:desiccantGramsUsed,
            DesiccantOutletTemperatureC:desiccantTUsed,
            MixingInletFlowsM3H:mixFlowsUsed);
        if(editingIndex>=0) _savedProcesses[editingIndex]=saved;
        else _savedProcesses.Add(saved);
        // [5.1C.12.3] Retirar únicamente la salida generada anterior si
        // ningún otro proceso la utiliza; nunca borrar estados compartidos.
        if(oldGeneratedId is int oldOutput && oldOutput!=generatedPointId &&
           !_savedProcesses.Any(p=>p.PointIds.Contains(oldOutput)))
            PsychrometricChart.DeletePoint(oldOutput);
        _editingProcessId=null;
        UpdateProcessesInCharts();
        ProcessResultText.Text = description;
        SelectedProcessSummaryMainText.Text = $"Proceso: {name}\n" + description;
        PointPropertiesPanel.ClearSelection();
        WorkspaceTabs.SelectedIndex = 1;
    }

    private void RenameProcess_Click(object sender, RoutedEventArgs e)
    {
        int index=SavedProcessesList.SelectedIndex;
        if(index<0||index>=_savedProcesses.Count||string.IsNullOrWhiteSpace(RenameProcessTextBox.Text)) return;
        RememberHistory();
        _savedProcesses[index]=_savedProcesses[index] with {Name=RenameProcessTextBox.Text.Trim()};
        UpdateProcessesInCharts();
        SavedProcessesList.SelectedIndex=index;
    }

    private void ToggleProcessVisibility_Click(object sender, RoutedEventArgs e)
    {
        int index=SavedProcessesList.SelectedIndex;
        if(index<0||index>=_savedProcesses.Count) return;
        RememberHistory();
        _savedProcesses[index]=_savedProcesses[index] with {Visible=!_savedProcesses[index].Visible};
        UpdateProcessesInCharts();
        SavedProcessesList.SelectedIndex=index;
    }

    private void EditProcess_Click(object sender, RoutedEventArgs e)
    {
        int index=SavedProcessesList.SelectedIndex;
        if(index<0||index>=_savedProcesses.Count) return;
        var process=_savedProcesses[index];
        _editingProcessId=process.Id;
        _selectedPointIds.Clear();
        _selectedPointIds.AddRange(process.PointIds);
        // Una salida generada NO es entrada del modelo predictivo.
        bool predictiveProcess=process.Type is ProcessType.EvaporativeHumidification
            or ProcessType.SteamHumidification or ProcessType.Bypass
            or ProcessType.AdiabaticMixing;
        if(_selectedPointIds.Count>=2 &&
           (process.GeneratedPointId==_selectedPointIds[^1] ||
            (process.GeneratedPointId is null && predictiveProcess)))
            _selectedPointIds.RemoveAt(_selectedPointIds.Count-1);
        RefreshSelectionList();
        NewProcessNameTextBox.Text=process.Name;
        CloseCycleCheckBox.IsChecked=process.PointIds.Count>=4 && process.PointIds[0]==process.PointIds[^1];
        if(CloseCycleCheckBox.IsChecked==true && _selectedPointIds.Count>0) _selectedPointIds.RemoveAt(_selectedPointIds.Count-1);
        RefreshSelectionList();
        CondensateTemperatureTextBox.Text=process.CondensateTemperatureC?.ToString("0.###",CultureInfo.CurrentCulture) ?? "";
        BranchFractionTextBox.Text=process.BranchFractionA?.ToString("0.###",CultureInfo.CurrentCulture) ?? "";
        DryFlowTextBox.Text=process.DryAirMassFlowKgS?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        EffectivenessTextBox.Text=process.SaturationEffectiveness?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        BypassFactorTextBox.Text=process.BypassFactor?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        ApparatusDewPointTextBox.Text=process.ApparatusDewPointC?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        SteamAddedTextBox.Text=process.SteamAddedGramsPerKg?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        SteamEnthalpyTextBox.Text=process.SteamEnthalpyKJPerKg?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        DesiccantRemovedTextBox.Text=process.DesiccantRemovedGramsPerKg?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        DesiccantTemperatureTextBox.Text=process.DesiccantOutletTemperatureC?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        MixFlowsListTextBox.Text=process.MixingInletFlowsM3H is {Count:>0}
            ? string.Join(";",process.MixingInletFlowsM3H.Select(flow=>flow.ToString("0.######",CultureInfo.CurrentCulture)))
            : "";
        foreach(ComboBoxItem item in ProcessTypeComboBox.Items)
            if(item.Tag?.ToString()==process.Type.ToString())
                {ProcessTypeComboBox.SelectedItem=item;break;}
        ProcessResultText.Text="Proceso cargado para edición. Al pulsar Graficar proceso reemplazará este proceso conservando su ID; se mantiene opción Deshacer. En predicciones puede generarse un nuevo punto de salida.";
    }

    // [5.1C.16] Propiedades persistentes del proceso seleccionado.
    private void SavedProcessesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = SavedProcessesList.SelectedIndex;
        if(index < 0 || index >= _savedProcesses.Count) return;
        PsychrometricProcess p = _savedProcesses[index];
        string heading=$"{p.Name} · {p.Type} · {string.Join(" → ",p.PointIds.Select(id=>"P"+id))}";
        string detail=string.IsNullOrWhiteSpace(p.Report)
            ? "No hay informe calculado para este proceso importado."
            : p.Report;
        ProcessResultText.Text=heading+Environment.NewLine+detail;
        SelectedProcessSummaryMainText.Text=heading+Environment.NewLine+detail;
    }

    private void DeleteProcess_Click(object sender, RoutedEventArgs e)
    {
        int index = SavedProcessesList.SelectedIndex;
        if (index < 0 || index >= _savedProcesses.Count) return;
        RememberHistory();
        if (_editingProcessId == _savedProcesses[index].Id) _editingProcessId=null;
        _savedProcesses.RemoveAt(index);
        UpdateProcessesInCharts();
        ProcessResultText.Text = "Proceso eliminado. Los puntos originales se conservan.";
    }

    private void UpdateProcessesInCharts()
    {
        PsychrometricChart.SetProcesses(_savedProcesses);
        ProcessChart.SetProcesses(_savedProcesses);
        SavedProcessesList.ItemsSource = _savedProcesses.Select(p =>
            $"{p.Name} {(p.Visible ? "" : "[Oculto]")} · {p.Type} · {string.Join(" → ",p.PointIds.Select(id => "P"+id))}").ToArray();
    }

    private static bool TryReadFinite(string raw, out double value)
    {
        bool valid = double.TryParse(raw.Trim(),NumberStyles.Float,CultureInfo.CurrentCulture,out value)
            || double.TryParse(raw.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value);
        return valid && double.IsFinite(value);
    }

    private static bool TryReadNonNegative(string raw, out double value)
        => TryReadFinite(raw,out value) && value >= 0;

    private static bool TryReadUnitInterval(string raw, out double value)
        => TryReadFinite(raw,out value) && value >= 0 && value <= 1;

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
        try
        {
            if(!SessionFileIntegrity.TryValidate(session.PressureKPa,session.Points,
                session.Processes,out string diagnostic))
                throw new InvalidDataException(diagnostic);
            File.WriteAllText(dialog.FileName,JsonSerializer.Serialize(session,
                SessionFileIntegrity.JsonOptions(indented:true)),new UTF8Encoding(false));
        }
        catch(Exception ex) when (ex is IOException or UnauthorizedAccessException or
            JsonException or InvalidDataException or NotSupportedException)
        {
            MessageBox.Show("No se pudo guardar la sesión: "+ex.Message,
                "FrioCalc",MessageBoxButton.OK,MessageBoxImage.Error);
        }
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
            var session=JsonSerializer.Deserialize<SessionV1>(File.ReadAllText(dialog.FileName),
                SessionFileIntegrity.JsonOptions());
            if (session is null || session.Version != 1 || session.Points is null ||
                session.Processes is null || !double.IsFinite(session.PressureKPa) ||
                session.PressureKPa <= 0)
                throw new InvalidDataException("Archivo de sesión incompatible.");
            if (!SessionFileIntegrity.TryValidate(session.PressureKPa,session.Points,
                session.Processes,out string validationError))
                throw new InvalidDataException(validationError);
            if (MessageBox.Show("Esto reemplazará los puntos y procesos actuales. ¿Continuar?",
                "Abrir sesión",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
            // Restaurar unidades ANTES de escribir la presión de la sesión.
            ChartMetricsPanel.ClearStateConditions();
            ChartMetricsPanel.SetUnitSystem(session.Units);
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            JsonException or InvalidDataException or ArgumentException or NotSupportedException)
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

    private void ComputeSlideExample_Click(object sender, RoutedEventArgs e)
    {
        if(sender is not Button b || !int.TryParse(b.Tag?.ToString(),out int slide)) return;
        TheoryExampleResultText.Text=SlideExamples.Evaluate(slide,PressureSelector.CurrentPressureKPa);
    }

    // [5.1C.17] Informe con ecuaciones, supuestos, estados, procesos y bibliografía.
    private void ExportMarkdownReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog {Title="Exportar informe técnico de FrioCalc",
            Filter="Markdown (*.md)|*.md",FileName="FrioCalc_Informe_Tecnico.md"};
        if(dialog.ShowDialog(this)!=true) return;
        var report=ProcessReportService.BuildMarkdown(PressureSelector.CurrentPressureKPa,
            PsychrometricChart.ActivePoints,_savedProcesses);
        File.WriteAllText(dialog.FileName,report,new UTF8Encoding(true));
        ProcessResultText.Text="Informe Markdown exportado, con supuestos y bibliografía.";
    }

    // [5.1C.19] Imagen rasterizada de la carta completa (incluye puntos y trazados).
    private void ExportChartPng_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog {Title="Exportar carta de procesos",
            Filter="Imagen PNG (*.png)|*.png",FileName="FrioCalc_Carta_Procesos.png"};
        if(dialog.ShowDialog(this)!=true) return;
        try
        {
            ProcessChart.ExportPng(dialog.FileName);
            ProcessResultText.Text="Carta de procesos exportada como PNG a resolución ampliada.";
        }
        catch(Exception ex) when(ex is InvalidOperationException or IOException)
        {
            MessageBox.Show(ex.Message,"Exportación gráfica",MessageBoxButton.OK,MessageBoxImage.Warning);
        }
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
