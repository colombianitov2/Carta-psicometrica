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
        PressureSelector.UnitsSelectionChanged += units => ChartMetricsPanel.SetUnitSystem(units);
        ChartMetricsPanel.NumberVisibilityChanged += (family,visible) =>
            PsychrometricChart.SetNumberVisibility(family,visible);
        ChartMetricsPanel.UnitsChanged += units =>
        {
            PressureSelector.SetUnitSystem(units);
            PointPropertiesPanel.SetUnitSystem(units);
            PsychrometricChart.SetDisplayUnits(units);
            ProcessChart.SetDisplayUnits(units);
            AshraeStudyPanel.SetUnits(units);
            AshraeSimulatorPanel.SetUnits(units);
            ProcessTheoryPanel.SetUnits(units);
            VariableReferencePanel.SetUnits(units);
            BibliographyPanel.SetUnits(units);
            SetProcessEditorUnits(units);
            RefreshProcessGroupLists();
            RefreshGroupCards();
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
        // Iniciar con la carta móvil: rueda=zoom y arrastre=panning.
        // El usuario puede activar «Carta estándar» para fijar el encuadre.
        PressureSelector.SetStandardChartEnabled(false);
        PressureSelector.SetPointSelectionEnabled(true);
        RefreshSelectionList();
        RefreshGroupCards();
        InitializeProcessValidation();
    }

    // [5.1C.8.2] Mostrar únicamente parámetros del método seleccionado.
    // Los controles se conservan en el árbol para mantener sesiones y edición.
    private void ProcessType_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(ProcessTypeComboBox is null || MixingGroup is null)return;
        string type=(ProcessTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()??"Automatic";
        bool predictive=type is "EvaporativeHumidification" or "SteamHumidification"
            or "Bypass" or "Desiccant";
        CondensateGroup.Visibility=type is "Automatic" or "CoolingDehumidification"
            ?Visibility.Visible:Visibility.Collapsed;
        MixingGroup.Visibility=type=="AdiabaticMixing"?Visibility.Visible:Visibility.Collapsed;
        BranchingGroup.Visibility=type=="Branching"?Visibility.Visible:Visibility.Collapsed;
        EffectivenessGroup.Visibility=type=="EvaporativeHumidification"
            ?Visibility.Visible:Visibility.Collapsed;
        BypassGroup.Visibility=type=="Bypass"?Visibility.Visible:Visibility.Collapsed;
        SteamGroup.Visibility=type=="SteamHumidification"?Visibility.Visible:Visibility.Collapsed;
        DesiccantGroup.Visibility=type=="Desiccant"?Visibility.Visible:Visibility.Collapsed;
        PredictiveNotice.Visibility=predictive?Visibility.Visible:Visibility.Collapsed;
        PredictiveExplanation.Visibility=predictive?Visibility.Visible:Visibility.Collapsed;
        CloseCycleCheckBox.Visibility=type=="Automatic"?Visibility.Visible:Visibility.Collapsed;
        if(type!="Automatic" && CloseCycleCheckBox.IsChecked==true)
            CloseCycleCheckBox.IsChecked=false;
        if(SegmentEditorsPanel is not null) SegmentEditorsPanel.Visibility=type=="Automatic"?Visibility.Visible:Visibility.Collapsed;
        UpdateProcessCompatibility();
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
        ProcessPressureIndicator.Text=
            "Presión de trabajo: "+pressureKPa.ToString("0.###",CultureInfo.CurrentCulture)+" kPa absolutos";
        AshraeSimulatorPanel.SetPressure(pressureKPa);
        _savedProcesses.Clear();
        _draftGroups.Clear();_collapsedProcessGroups.Clear();_lastBuilderGroupId=null;
        CloseGroupEditor();
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
    private void ChartPageScroll_SizeChanged(object sender,SizeChangedEventArgs e)
    {
        if(!_isChartFullScreen && PsychrometricChart is not null && e.NewSize.Height>0)
            PsychrometricChart.Height=Math.Max(330,e.NewSize.Height*.76);
    }

    private void RefreshSelectionList()
    {
        RefreshProcessGroupLists();
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
        var assessment=AssessProcessEditor(out var prediction);
        ShowAssessment(ProcessCompatibilityText,assessment);
        if(!assessment.CanCalculate)
        {
            MessageBox.Show(assessment.Message,"Proceso no resuelto",MessageBoxButton.OK,MessageBoxImage.Warning);
            return;
        }
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
        bool closeCycle=CloseCycleCheckBox.IsChecked==true && _selectedPointIds[0]!=_selectedPointIds[^1];
        if(closeCycle && (type!=ProcessType.Automatic || selected.Length<3))
        {
            MessageBox.Show("Para cerrar un ciclo seleccione método Automático " +
                "y al menos tres puntos diferentes antes de generar la gráfica.",
                "Ciclo psicrométrico",MessageBoxButton.OK,MessageBoxImage.Warning);
            return;
        }
        // La validación previa comprueba la salida indicada, o calcula una
        // salida nueva cuando solamente se selecciona el estado de entrada.
        if (!TryReadOptionalFlow(DryFlowTextBox.Text, out double? massFlow, out string error))
        {
            MessageBox.Show(error, "Caudal inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }

        if (!TryReadSegmentSettings(type, out var segmentSettings, out error))
        { MessageBox.Show(error,"Datos del tramo",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
        double? condensateTemperature = null;
        if ((type is ProcessType.Automatic or ProcessType.CoolingDehumidification) &&
            !string.IsNullOrWhiteSpace(CondensateTemperatureTextBox.Text))
        {
            if (!TryReadFinite(CondensateTemperatureTextBox.Text,out double condensateInput) ||
                TemperatureToSI(condensateInput) < -50 || TemperatureToSI(condensateInput) > 100)
            {
                MessageBox.Show($"Temperatura de condensado inválida. Se requiere {TemperatureFromSI(-50):0.##} a {TemperatureFromSI(100):0.##} {TempUnit}.",
                    "Balance de serpentín",MessageBoxButton.OK,MessageBoxImage.Warning);return;
            }
            condensateTemperature = TemperatureToSI(condensateInput);
        }
        double? branchFraction = null;
        int? generatedPointId = null;
        double? saturationEffectiveness = null, bypassFactorUsed = null;
        double? steamGramsUsed = null, steamEnthalpyUsed = null, adpUsed = null;
        double? desiccantGramsUsed = null, desiccantTUsed = null;
        List<double>? mixFlowsUsed = null;
        string predictionNote = "";
        if (predictive && prediction is not null)
        {
            ChartPoint inlet=selected[0]!;
            saturationEffectiveness=prediction.Effectiveness;
            bypassFactorUsed=prediction.Bypass;
            steamGramsUsed=prediction.SteamGrams;
            steamEnthalpyUsed=prediction.SteamEnthalpy;
            adpUsed=prediction.Adp;
            desiccantGramsUsed=prediction.RemovedGrams;
            desiccantTUsed=prediction.OutletT;
            predictionNote=prediction.Note;
            if(selected.Length==1)
            {
                var target=new Dictionary<StateProperty,double>
                {
                    [StateProperty.DryBulb]=prediction.Output.DryBulbC,
                    [StateProperty.HumidityRatio]=prediction.Output.HumidityRatioKgPerKg
                };
                if(!PsychrometricChart.TryAddPointFromConditions(target,out error))
                { MessageBox.Show(error,"Salida fuera de la carta",MessageBoxButton.OK,MessageBoxImage.Warning);return; }
                generatedPointId=PsychrometricChart.ActivePoints.Last().Id;
                _selectedPointIds.Add(generatedPointId.Value);
                selected=new ChartPoint?[]{inlet,PsychrometricChart.ActivePoints.Last()};
            }
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
                    $"Introduzca caudal másico de entrada ({FlowUnit}) y fracción estrictamente entre 0 y 1.",
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
            var flowAssessment=ReadMixFlows(selected.Length,out var flows);
            if(!flowAssessment.CanCalculate)
            {MessageBox.Show(flowAssessment.Message,"Mezcla",MessageBoxButton.OK,MessageBoxImage.Warning);return;}
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
                var segment=ProcessSegmentSetting.Find(segmentSettings,ids,i);
                var segmentType=segment?.Type ?? (type == ProcessType.Bypass ? ProcessType.Automatic : type);
                var segmentFlow=segment?.DryAirMassFlowKgS ?? massFlow;
                var segmentCondensate=segment?.CondensateTemperatureC ?? condensateTemperature;
                if (!ProcessCalculator.TryCalculate(selected[i]!,selected[i+1]!,
                    segmentType,segmentFlow,segmentCondensate,out ProcessResult result,out error))
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
                if(segment?.SensibleHeatFactor is double targetFcs)
                    report.AppendLine($"FCS especificado={targetFcs:0.###}; diferencia respecto al calculado: "+
                        (result.SensibleHeatFactor is double actualFcs ? (actualFcs-targetFcs).ToString("0.###") : "no determinada"));
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
        int editingIndex=_editingProcessId is int oldId
            ? _savedProcesses.FindIndex(p=>p.Id==oldId) : -1;
        int processId=editingIndex>=0 ? _savedProcesses[editingIndex].Id :
            (_editingProcessId is int draftId && _draftGroups.ContainsKey(draftId)?draftId:_nextProcessId++);
        string name=string.IsNullOrWhiteSpace(NewProcessNameTextBox.Text)
            ? $"PR{processId}" : NewProcessNameTextBox.Text.Trim();
        RememberHistory();
        var saved = new PsychrometricProcess(
            processId,name,editingIndex>=0 ? _savedProcesses[editingIndex].ColorHex : PointColorPalette.GetColorHex(processId+31),type,ids,massFlow,Visible:editingIndex<0||_savedProcesses[editingIndex].Visible,Report:description,
            CondensateTemperatureC:condensateTemperature,BranchFractionA:branchFraction,
            GeneratedPointId:generatedPointId,SaturationEffectiveness:saturationEffectiveness,
            BypassFactor:bypassFactorUsed,SteamAddedGramsPerKg:steamGramsUsed,
            SteamEnthalpyKJPerKg:steamEnthalpyUsed,ApparatusDewPointC:adpUsed,
            DesiccantRemovedGramsPerKg:desiccantGramsUsed,
            DesiccantOutletTemperatureC:desiccantTUsed,
            MixingInletFlowsM3H:mixFlowsUsed,SegmentSettings:segmentSettings);
        _draftGroups.Remove(processId);
        _lastBuilderGroupId=processId;
        if(editingIndex>=0) _savedProcesses[editingIndex]=saved;
        else _savedProcesses.Add(saved);
        // Quitar un miembro o sustituir una salida nunca elimina el estado original de la carta.
        _editingProcessId=null;
        UpdateProcessesInCharts();
        ProcessResultText.Text = description;
        SelectedProcessSummaryMainText.Text = $"Proceso: {name}\n" + description;
        PointPropertiesPanel.ClearSelection();
        if(!_returnToChartAfterGroupEditor)WorkspaceTabs.SelectedIndex=1;
        CloseGroupEditor();
    }

    private void RenameProcess_Click(object sender, RoutedEventArgs e)
    {
        // El nombre es metadato: no exigir un par de estados ni recalcular.
        if(_editingProcessId is not int id || string.IsNullOrWhiteSpace(NewProcessNameTextBox.Text)) return;
        string name=NewProcessNameTextBox.Text.Trim();
        int index=_savedProcesses.FindIndex(p=>p.Id==id);
        if(index>=0)
        {
            if(_savedProcesses[index].Name!=name)
            {
                RememberHistory();
                _savedProcesses[index]=_savedProcesses[index] with {Name=name};
            }
        }
        else if(_draftGroups.TryGetValue(id,out var draft))
            _draftGroups[id]=draft with {Name=name};
        else return;
        UpdateProcessesInCharts();
        PointPropertiesPanel.ClearSelection();
        if(!_returnToChartAfterGroupEditor)WorkspaceTabs.SelectedIndex=1;
        CloseGroupEditor();
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
        _lastBuilderGroupId=process.Id;
        LoadSegmentDrafts(process);
        _selectedPointIds.Clear();
        _selectedPointIds.AddRange(process.PointIds);
        // Una salida generada NO es entrada del modelo predictivo.
        if(_selectedPointIds.Count>=2 &&
           (process.GeneratedPointId==_selectedPointIds[^1]))
            _selectedPointIds.RemoveAt(_selectedPointIds.Count-1);
        RefreshSelectionList();
        NewProcessNameTextBox.Text=process.Name;
        // El cierre se representa por una tarjeta final real; no se oculta ni se añade otra al guardar.
        CloseCycleCheckBox.IsChecked=false;
        var editorRoute=_selectedPointIds.ToArray();
        PointPropertiesPanel.SetSelection(editorRoute.Distinct().ToArray());
        // La selección visual de la carta es un conjunto; el recorrido admite repeticiones.
        _selectedPointIds.Clear();
        _selectedPointIds.AddRange(editorRoute);
        CondensateTemperatureTextBox.Text=DisplayParameter(process.CondensateTemperatureC,TemperatureFromSI);
        BranchFractionTextBox.Text=process.BranchFractionA?.ToString("0.###",CultureInfo.CurrentCulture) ?? "";
        DryFlowTextBox.Text=DisplayParameter(process.DryAirMassFlowKgS,FlowFromSI);
        EffectivenessTextBox.Text=process.SaturationEffectiveness?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        BypassFactorTextBox.Text=process.BypassFactor?.ToString("0.######",CultureInfo.CurrentCulture) ?? "";
        ApparatusDewPointTextBox.Text=DisplayParameter(process.ApparatusDewPointC,TemperatureFromSI);
        SteamAddedTextBox.Text=DisplayParameter(process.SteamAddedGramsPerKg,WaterFromSI);
        SteamEnthalpyTextBox.Text=DisplayParameter(process.SteamEnthalpyKJPerKg,WaterEnthalpyFromSI);
        DesiccantRemovedTextBox.Text=DisplayParameter(process.DesiccantRemovedGramsPerKg,WaterFromSI);
        DesiccantTemperatureTextBox.Text=DisplayParameter(process.DesiccantOutletTemperatureC,TemperatureFromSI);
        MixFlowsListTextBox.Text=process.MixingInletFlowsM3H is {Count:>0}
            ? string.Join(";",process.MixingInletFlowsM3H.Select(flow=>DisplayParameter(flow,VolumeFlowFromSI)))
            : "";
        foreach(ComboBoxItem item in ProcessTypeComboBox.Items)
            if(item.Tag?.ToString()==process.Type.ToString())
                {ProcessTypeComboBox.SelectedItem=item;break;}
        OpenGroupEditor();
        RenderGroupResults(process,ProcessResultsPanel);
        ProcessResultText.Text="Proceso cargado para edición. Guardar conserva el identificador y actualiza los resultados.";
    }

    // [5.1C.16] Propiedades persistentes del proceso seleccionado.
    private void SavedProcessesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = SavedProcessesList.SelectedIndex;
        if(index < 0 || index >= _savedProcesses.Count) return;
        PsychrometricProcess p = _savedProcesses[index];
        string heading=$"{p.Name} · {ProcessResultPresentation.MethodName(p.Type)} · {string.Join(" → ",p.PointIds.Select(id=>"P"+id))}";
        string detail=string.IsNullOrWhiteSpace(p.Report)
            ? "No hay informe calculado para este proceso importado."
            : p.Report;
        ProcessResultText.Text=heading+Environment.NewLine+detail;
        SelectedProcessSummaryMainText.Text=heading+Environment.NewLine+detail;
    }

    private void DeleteProcess_Click(object sender, RoutedEventArgs e)
    {
        if(_editingProcessId is int draftId && _draftGroups.Remove(draftId))
        {CloseGroupEditor();return;}
        int index = SavedProcessesList.SelectedIndex;
        if (index < 0 || index >= _savedProcesses.Count) return;
        RememberHistory();
        if (_editingProcessId == _savedProcesses[index].Id) _editingProcessId=null;
        _savedProcesses.RemoveAt(index);
        UpdateProcessesInCharts();
        CloseGroupEditor();
        ProcessResultText.Text = "Proceso eliminado. Los puntos originales se conservan.";
    }

    private void UpdateProcessesInCharts()
    {
        PsychrometricChart.SetProcesses(_savedProcesses);
        ProcessChart.SetProcesses(_savedProcesses);
        SavedProcessesList.ItemsSource = _savedProcesses.Select(p =>
            $"{p.Name} {(p.Visible ? "" : "[Oculto]")} · {ProcessResultPresentation.MethodName(p.Type)} · {string.Join(" → ",p.PointIds.Select(id => "P"+id))}").ToArray();
        RefreshGroupCards();
        if(_editingProcessId is int existing) SavedProcessesList.SelectedIndex=_savedProcesses.FindIndex(p=>p.Id==existing);
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

    private bool TryReadOptionalFlow(string raw, out double? flow, out string error)
    {
        flow = null; error = "";
        if (string.IsNullOrWhiteSpace(raw)) return true;
        if (!TryReadPositive(raw,out double value))
        { error=$"Introduzca un flujo másico de aire seco positivo, en {FlowUnit}."; return false; }
        flow = FlowToSI(value);
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
            _draftGroups.Clear();_collapsedProcessGroups.Clear();_lastBuilderGroupId=null;
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
            Title="Guardar sesión de la calculadora", Filter="Sesión de la calculadora JSON|*.friocalc.json|JSON|*.json",
            FileName="Calculadora_Refrigeracion_Parte1_Sesion.friocalc.json"
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
                "Calculadora de refrigeración – Parte 1 – Carta psicrométrica",MessageBoxButton.OK,MessageBoxImage.Error);
        }
    }

    private void LoadSession_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title="Abrir sesión de la calculadora", Filter="Sesión de la calculadora JSON|*.friocalc.json;*.json|Todos|*.*"
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
            _draftGroups.Clear();_collapsedProcessGroups.Clear();_lastBuilderGroupId=null;
            CloseGroupEditor();
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
        TheoryCoursePanel.ShowSlide(slide);
        ShowEducationalSlide(slide);
    }

    private void ShowEducationalSlide(int slide)
    {
        string solution=SlideExamples.Evaluate(slide,PressureSelector.CurrentPressureKPa);
        string explore=ProcessTheoryPanel.LoadSlideExample(slide);
        UI.AshraeStudyPanel.FormatMath(TheoryExampleResultText,solution+"\n\nLABORATORIO INTERACTIVO: "+explore);
    }

    // [5.1C.17] Informe con ecuaciones, supuestos, estados, procesos y bibliografía.
    private void ExportMarkdownReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog {Title="Exportar informe técnico de la calculadora",
            Filter="Markdown (*.md)|*.md",FileName="Calculadora_Refrigeracion_Parte1_Informe_Tecnico.md"};
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
            Filter="Imagen PNG (*.png)|*.png",FileName="Calculadora_Refrigeracion_Parte1_Carta_Procesos.png"};
        if(dialog.ShowDialog(this)!=true) return;
        try
        {
            PsychrometricChart.ExportPng(dialog.FileName);
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
            FileName="Calculadora_Refrigeracion_Parte1_Psicrometria_Informe.csv"
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
