using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics;

// [5.1C.20] Editor de grupos independiente de los estados de la carta.
public partial class MainWindow
{
    private Point _groupDragStart;
    private ChartPoint? _groupDragPoint;
    private int _groupDragIndex=-1;
    private ListBox? _groupDragSource;
    private readonly Dictionary<int,PsychrometricProcess> _draftGroups=new();
    // Estado exclusivamente visual, separado de los datos del proceso.
    private readonly HashSet<int> _collapsedProcessGroups=new();
    private int? _lastBuilderGroupId;

    private void ProcessPageScroll_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ProcessGroupsScroll is null || AvailablePointsList is null || e.NewSize.Height <= 0) return;
        double height = Math.Max(150, e.NewSize.Height - 170);
        ProcessGroupsScroll.MaxHeight = height;
        AvailablePointsList.MaxHeight = height;
    }
    private sealed class SegmentDraft
    {
        public ProcessType Type = ProcessType.Automatic;
        public string Flow = "", Fcs = "", Condensate = "";
    }
    private readonly Dictionary<(int,int,int),SegmentDraft> _segmentDrafts = new();

    // [5.1C.20.1] Tarjetas de estado: mismas propiedades, sin acciones que borren puntos.
    public sealed class ProcessPointCard
    {
        public ChartPoint Point {get;}
        public string Title {get;}
        public string Color {get;}
        public string DryBulb {get;}
        public string Humidity {get;}
        public string RelativeHumidity {get;}
        public string WetBulb {get;}
        public string DewPoint {get;}
        public string Enthalpy {get;}
        public string Volume {get;}
        public string Entropy {get;}
        public ProcessPointCard(ChartPoint point,UnitSystem units,int order=0,string? groupColor=null)
        {
            Point=point;Title=order>0?$"{order}. {point.Name}":point.Name;Color=groupColor??point.ColorHex;
            bool si=units==UnitSystem.SI;
            string F(double v,string f,string unit)=>double.IsFinite(v)?v.ToString(f,CultureInfo.CurrentCulture)+" "+unit:"—";
            DryBulb=F(PsychrometricUnits.TemperatureFromSI(point.DryBulbC,units),"0.00",si?"°C":"°F");
            Humidity=F(PsychrometricUnits.HumidityFromSI(point.HumidityRatioKgPerKg,units),"0.00",si?"g/kg aire seco":"gr/lb aire seco");
            RelativeHumidity=F(point.RelativeHumidityPercent,"0.0","%");
            WetBulb=F(PsychrometricUnits.TemperatureFromSI(point.WetBulbC,units),"0.00",si?"°C":"°F");
            DewPoint=F(PsychrometricUnits.TemperatureFromSI(point.DewPointC,units),"0.00",si?"°C":"°F");
            Enthalpy=F(PsychrometricUnits.EnthalpyFromSI(point.EnthalpyKJPerKgDryAir,units),"0.00",si?"kJ/kg aire seco":"Btu/lb aire seco");
            Volume=F(PsychrometricUnits.VolumeFromSI(point.SpecificVolumeM3PerKgDryAir,units),"0.000",si?"m³/kg aire seco":"ft³/lb aire seco");
            double entropy=MoistAirSpecificEntropy.FromDryBulbAndHumidityRatio(point.DryBulbC,point.HumidityRatioKgPerKg,point.PressureKPa);
            Entropy=F(PsychrometricUnits.EntropyFromSI(entropy,units),"0.000",si?"kJ/(kgda·K)":"Btu/(lbda·°F)");
        }
    }

    private ProcessPointCard[] CardsFor(IEnumerable<int> ids,bool numbered,string? color=null)
    {
        var points=PsychrometricChart.ActivePoints;
        return ids.Select(id=>points.FirstOrDefault(p=>p.Id==id)).OfType<ChartPoint>()
            .Select((p,i)=>new ProcessPointCard(p,ChartMetricsPanel.CurrentUnits,numbered?i+1:0,color)).ToArray();
    }

    private void RefreshProcessGroupLists()
    {
        if(AvailablePointsList is null || ProcessSelectionList is null || EditorAvailablePointsList is null)return;
        var available=CardsFor(PsychrometricChart.ActivePoints.Select(p=>p.Id),false);
        AvailablePointsList.ItemsSource=available;
        EditorAvailablePointsList.ItemsSource=available;
        ProcessSelectionList.ItemsSource=CardsFor(_selectedPointIds,true);
        RefreshSegmentEditors();
    }

    private void ProcessList_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _groupDragSource=sender as ListBox;
        _groupDragStart=e.GetPosition(this);
        _groupDragPoint=null;_groupDragIndex=-1;
        if(_groupDragSource is ListBox box && e.OriginalSource is DependencyObject child &&
           ItemsControl.ContainerFromElement(box,child) is ListBoxItem item &&
           item.Content is ProcessPointCard card)
        {
            _groupDragPoint=card.Point;
            _groupDragIndex=box.ItemContainerGenerator.IndexFromContainer(item);
        }
    }

    private void ProcessList_MouseMove(object sender, MouseEventArgs e)
    {
        if(e.LeftButton!=MouseButtonState.Pressed || _groupDragPoint is null || _groupDragSource is null)return;
        Vector delta=e.GetPosition(this)-_groupDragStart;
        if(Math.Abs(delta.X)<SystemParameters.MinimumHorizontalDragDistance &&
           Math.Abs(delta.Y)<SystemParameters.MinimumVerticalDragDistance)return;
        var data=new DataObject();
        data.SetData("FrioCalc.PointId",_groupDragPoint.Id);
        data.SetData("FrioCalc.FromSequence",ReferenceEquals(_groupDragSource,ProcessSelectionList));
        data.SetData("FrioCalc.SourceIndex",_groupDragIndex);
        if(_groupDragSource.Tag is int groupId)data.SetData("FrioCalc.FromGroup",groupId);
        var source=_groupDragSource;
        _groupDragSource=null;_groupDragPoint=null;_groupDragIndex=-1;
        DragDrop.DoDragDrop(source,data,DragDropEffects.Copy|DragDropEffects.Move);
    }

    private static int DropPosition(ListBox box,DragEventArgs e,IReadOnlyList<int> ids)
    {
        if(e.OriginalSource is DependencyObject child &&
           ItemsControl.ContainerFromElement(box,child) is ListBoxItem item)
        {
            int target=box.ItemContainerGenerator.IndexFromContainer(item);
            if(target>=0)return target+(e.GetPosition(item).Y>item.ActualHeight/2?1:0);
        }
        return ids.Count;
    }

    // sourceIndex identifica una aparición concreta, no el ID del estado.
    // -1 significa copiar desde el catálogo, aun si ese estado figura en el recorrido.
    private static bool EditOrderedSequence(List<int> ids,int id,int target,bool remove,int sourceIndex)
    {
        if(sourceIndex>=0)
        {
            if(sourceIndex>=ids.Count || ids[sourceIndex]!=id)return false;
            ids.RemoveAt(sourceIndex);
            if(remove)return true;
            if(sourceIndex<target)target--;
        }
        else if(remove)return false;
        ids.Insert(Math.Clamp(target,0,ids.Count),id);
        return true;
    }

    private static int DragSourceIndex(IDataObject data)=>
        data.GetData("FrioCalc.SourceIndex") is int index?index:-1;

    private void ProcessSequence_Drop(object sender,DragEventArgs e)
    {
        if(e.Data.GetData("FrioCalc.PointId") is not int id ||
           !PsychrometricChart.ActivePoints.Any(p=>p.Id==id))return;
        int source=e.Data.GetData("FrioCalc.FromSequence") is true?DragSourceIndex(e.Data):-1;
        if(EditOrderedSequence(_selectedPointIds,id,DropPosition(ProcessSelectionList,e,_selectedPointIds),false,source))
        {
            CloseCycleCheckBox.IsChecked=false;
            RefreshProcessGroupLists();
        }
        e.Effects=source>=0?DragDropEffects.Move:DragDropEffects.Copy;e.Handled=true;
    }

    // El fondo de la página y el catálogo aceptan retirar una tarjeta sin borrar el estado.
    private void ProcessPage_Drop(object sender,DragEventArgs e)
    {
        if(e.Data.GetData("FrioCalc.PointId") is not int id)return;
        int source=DragSourceIndex(e.Data);
        if(source<0)return;
        if(e.Data.GetData("FrioCalc.FromSequence") is true)
        {
            if(EditOrderedSequence(_selectedPointIds,id,0,true,source))
            {
                CloseCycleCheckBox.IsChecked=false;
                RefreshProcessGroupLists();
            }
        }
        else if(e.Data.GetData("FrioCalc.FromGroup") is int groupId)
            UpdateBuilderMembership(groupId,id,0,true,source);
        else return;
        e.Effects=DragDropEffects.Move;e.Handled=true;
    }

    private void AvailablePoints_Drop(object sender,DragEventArgs e)=>ProcessPage_Drop(sender,e);

    private void AvailablePoints_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        if(sender is not ListBox box || box.SelectedItem is not ProcessPointCard card)return;
        if(ReferenceEquals(box,EditorAvailablePointsList))
        {
            _selectedPointIds.Add(card.Point.Id);
            CloseCycleCheckBox.IsChecked=false;
            RefreshProcessGroupLists();return;
        }
        int groupId=_lastBuilderGroupId??-1;
        if(FindBuilderGroup(groupId) is null)groupId=CreateDraftGroup();
        var group=FindBuilderGroup(groupId)!;
        UpdateBuilderMembership(groupId,card.Point.Id,group.PointIds.Count,false,-1);
    }

    private void ProcessSequence_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        if(ProcessSelectionList.SelectedItem is not ProcessPointCard card)return;
        if(EditOrderedSequence(_selectedPointIds,card.Point.Id,0,true,ProcessSelectionList.SelectedIndex))
        {
            CloseCycleCheckBox.IsChecked=false;
            RefreshProcessGroupLists();
        }
    }

    // [5.1C.20.2] Cada grupo tiene su propio recuadro; los cambios válidos se calculan sin abrir el editor.
    private PsychrometricProcess? FindBuilderGroup(int id)=>
        _savedProcesses.FirstOrDefault(p=>p.Id==id) ?? (_draftGroups.TryGetValue(id,out var draft)?draft:null);

    private int CreateDraftGroup()
    {
        int id=_nextProcessId++;
        _draftGroups[id]=new PsychrometricProcess(id,$"PR{id}",PointColorPalette.GetColorHex(id+31),
            ProcessType.Automatic,Array.Empty<int>(),null);
        _collapsedProcessGroups.Remove(id);
        _lastBuilderGroupId=id;
        ProcessBuilderStatus.Text="Arrastre los puntos al nuevo grupo.";
        RefreshGroupCards();return id;
    }

    private void NewProcessGroup_Click(object sender,RoutedEventArgs e)=>CreateDraftGroup();

    private void BuilderGroup_Drop(object sender,DragEventArgs e)
    {
        if(sender is not FrameworkElement element || element.Tag is not int groupId ||
           e.Data.GetData("FrioCalc.PointId") is not int id)return;
        var group=FindBuilderGroup(groupId);if(group is null)return;
        int source=e.Data.GetData("FrioCalc.FromGroup") is int origin && origin==groupId
            ?DragSourceIndex(e.Data):-1;
        int target=sender is ListBox box?DropPosition(box,e,group.PointIds):group.PointIds.Count;
        UpdateBuilderMembership(groupId,id,target,false,source);
        e.Effects=source>=0?DragDropEffects.Move:DragDropEffects.Copy;e.Handled=true;
    }

    private void BuilderGroup_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        if(sender is ListBox box && box.Tag is int groupId && box.SelectedItem is ProcessPointCard card)
            UpdateBuilderMembership(groupId,card.Point.Id,0,true,box.SelectedIndex);
    }

    private void UpdateBuilderMembership(int groupId,int pointId,int target,bool remove,int sourceIndex)
    {
        var group=FindBuilderGroup(groupId);
        if(group is null || !PsychrometricChart.ActivePoints.Any(p=>p.Id==pointId))return;
        _lastBuilderGroupId=groupId;
        var ids=group.PointIds.ToList();
        if(!EditOrderedSequence(ids,pointId,target,remove,sourceIndex) || ids.SequenceEqual(group.PointIds))return;
        if(group.Type!=ProcessType.Automatic || group.GeneratedPointId.HasValue)
        {
            // Los métodos de equipo requieren sus parámetros; editar sin sustituir el resultado guardado.
            OpenBuilderGroupEditor(groupId);
            _selectedPointIds.Clear();
            _selectedPointIds.AddRange(ids.Where(id=>id!=group.GeneratedPointId));
            RefreshProcessGroupLists();
            ProcessBuilderStatus.Text="Revise los parámetros del equipo para guardar la nueva secuencia.";
            return;
        }
        var segments=new List<ProcessSegmentSetting>();
        var report=new System.Text.StringBuilder();
        for(int i=0;i<ids.Count-1;i++)
        {
            int fromId=ids[i],toId=ids[i+1];
            var from=PsychrometricChart.ActivePoints.First(p=>p.Id==fromId);
            var to=PsychrometricChart.ActivePoints.First(p=>p.Id==toId);
            var setting=ProcessSegmentSetting.Find(group.SegmentSettings,ids,i);
            if(!ProcessCalculator.TryCalculate(from,to,setting?.Type??ProcessType.Automatic,
                setting?.DryAirMassFlowKgS??group.DryAirMassFlowKgS,
                setting?.CondensateTemperatureC??group.CondensateTemperatureC,out var result,out var error))
            {
                ProcessBuilderStatus.Text=$"{from.Name} → {to.Name}: {error}";
                return;
            }
            if(setting is not null)segments.Add(setting);
            report.AppendLine($"{from.Name} → {to.Name}: ΔT={result.DeltaTemperatureC:0.###} °C; ΔW={result.DeltaHumidityRatioKgPerKg*1000:0.###} g/kg; Δh={result.DeltaEnthalpyKJPerKgDryAir:0.###} kJ/kgda.");
        }
        if(ProcessCycleAnalyzer.TryAnalyze(ids.Select(id=>PsychrometricChart.ActivePoints.First(p=>p.Id==id)).ToArray(),
            group.DryAirMassFlowKgS,out var cycle,out _))
            report.AppendLine($"Recorrido: {cycle.StageCount} tramos; cerrado={(cycle.IsClosed?"sí":"no")}; Δh neta={cycle.NetEnthalpyKJPerKgDryAir:0.######} kJ/kgda.");
        var changed=group with {PointIds=ids.ToArray(),SegmentSettings=segments,Report=report.ToString()};
        int index=_savedProcesses.FindIndex(p=>p.Id==groupId);
        if(index>=0 || ids.Count>=2)RememberHistory();
        if(ids.Count<2)
        {
            if(index>=0)_savedProcesses.RemoveAt(index);
            _draftGroups[groupId]=changed;
            ProcessBuilderStatus.Text="Añada otro punto para calcular el primer tramo.";
        }
        else
        {
            _draftGroups.Remove(groupId);
            if(index>=0)_savedProcesses[index]=changed;else _savedProcesses.Add(changed);
            ProcessBuilderStatus.Text=$"{changed.Name}: {ids.Count-1} tramo(s) calculados."+
                (ids.Count>=3 && ids[0]==ids[^1]?" Recorrido cerrado.":"");
        }
        UpdateProcessesInCharts();
    }

    private void OpenBuilderGroupEditor(int id)
    {
        int index=_savedProcesses.FindIndex(p=>p.Id==id);
        if(index>=0)
        {
            SavedProcessesList.SelectedIndex=index;EditProcess_Click(this,new RoutedEventArgs());return;
        }
        if(!_draftGroups.TryGetValue(id,out var draft))return;
        _editingProcessId=id;_selectedPointIds.Clear();_selectedPointIds.AddRange(draft.PointIds);
        LoadSegmentDrafts(draft);
        SavedProcessesList.SelectedIndex=-1;
        ProcessTypeComboBox.SelectedIndex=0;
        CloseCycleCheckBox.IsChecked=false;
        foreach(var input in new[]{NewProcessNameTextBox,DryFlowTextBox,
            FcsGuideTextBox,CondensateTemperatureTextBox,MixFlowsListTextBox,MixFlowFirstTextBox,
            MixFlowSecondTextBox,BranchFractionTextBox,EffectivenessTextBox,ApparatusDewPointTextBox,
            BypassFactorTextBox,SteamAddedTextBox,SteamEnthalpyTextBox,DesiccantRemovedTextBox,DesiccantTemperatureTextBox})
            input.Clear();
        NewProcessNameTextBox.Text=draft.Name;
        DryFlowTextBox.Text=DisplayParameter(draft.DryAirMassFlowKgS,FlowFromSI);
        CondensateTemperatureTextBox.Text=DisplayParameter(draft.CondensateTemperatureC,TemperatureFromSI);
        ProcessResultsPanel.Children.Clear();
        OpenGroupEditor();
    }

    private void OpenGroupEditor()
    {
        ProcessBuilderView.Visibility=Visibility.Collapsed;
        ProcessEditor.Visibility=Visibility.Visible;
        ProcessEditorHeading.Text="Editar "+(FindBuilderGroup(_editingProcessId??-1)?.Name??"grupo de procesos");
        RefreshProcessGroupLists();ProcessPageScroll.ScrollToTop();
    }

    private void CloseGroupEditor()
    {
        ProcessEditor.Visibility=Visibility.Collapsed;ProcessBuilderView.Visibility=Visibility.Visible;
        _editingProcessId=null;_selectedPointIds.Clear();_segmentDrafts.Clear();
        RefreshProcessGroupLists();RefreshGroupCards();ProcessPageScroll.ScrollToTop();
    }

    private void CloseProcessEditor_Click(object sender,RoutedEventArgs e)
    {
        PointPropertiesPanel.ClearSelection();
        CloseGroupEditor();
    }

    private void LoadSegmentDrafts(PsychrometricProcess process)
    {
        _segmentDrafts.Clear();
        if(process.SegmentSettings is null)return;
        foreach(var s in process.SegmentSettings)
            _segmentDrafts[(s.FromId,s.ToId,s.Occurrence)]=new SegmentDraft {
                Type=s.Type, Flow=DisplayParameter(s.DryAirMassFlowKgS,FlowFromSI),
                Fcs=s.SensibleHeatFactor?.ToString(CultureInfo.CurrentCulture)??"",
                Condensate=DisplayParameter(s.CondensateTemperatureC,TemperatureFromSI)};
    }

    private void RefreshSegmentEditors()
    {
        if(SegmentEditorsPanel is null)return;
        SegmentEditorsPanel.Children.Clear();
        for(int i=0;i<_selectedPointIds.Count-1;i++)
        {
            var key=ProcessSegmentSetting.Key(_selectedPointIds,i);
            if(!_segmentDrafts.TryGetValue(key,out var draft))
                _segmentDrafts[key]=draft=new SegmentDraft();
            var fields=new StackPanel { Margin=new Thickness(0,5,0,5) };
            fields.Children.Add(new TextBlock { Text=$"Tramo {i+1}: P{key.Item1} → P{key.Item2}",FontWeight=FontWeights.SemiBold });
            var choices=new Dictionary<ProcessType,string>{
                [ProcessType.Automatic]="Automático", [ProcessType.Sensible]="Calor sensible",
                [ProcessType.HeatingHumidification]="Calentamiento y humidificación",
                [ProcessType.CoolingDehumidification]="Enfriamiento y deshumidificación",
                [ProcessType.SteamHumidification]="Humidificación con vapor",
                [ProcessType.EvaporativeHumidification]="Humidificación evaporativa",
                [ProcessType.Desiccant]="Deshumidificación con desecante"};
            var methods=new ComboBox { ItemsSource=choices,DisplayMemberPath="Value",SelectedValuePath="Key",
                SelectedValue=draft.Type,Margin=new Thickness(0,4,0,6),MinWidth=120,
                MaxWidth=340,HorizontalAlignment=HorizontalAlignment.Left };
            methods.SelectionChanged+=(_,_)=>{if(methods.SelectedValue is ProcessType kind)draft.Type=kind;};
            fields.Children.Add(new TextBlock {Text="Método del tramo (Automático si no se especifica)"});
            fields.Children.Add(methods);
            var row=new WrapPanel();
            void Field(string label,string value,Action<string> setter)
            {
                var panel=new StackPanel{Margin=new Thickness(0,0,12,4),Width=184};
                panel.Children.Add(new TextBlock{Text=label,TextWrapping=TextWrapping.Wrap,FontSize=12});
                var box=new TextBox{Text=value,Margin=new Thickness(0,3,0,0),MinHeight=26};
                box.TextChanged+=(_,_)=>setter(box.Text);
                panel.Children.Add(box);row.Children.Add(panel);
            }
            Field($"Caudal seco ({FlowUnit}), opcional",draft.Flow,v=>draft.Flow=v);
            Field("FCS especificado (0 a 1), opcional",draft.Fcs,v=>draft.Fcs=v);
            Field($"Temperatura del condensado ({TempUnit}), opcional",draft.Condensate,v=>draft.Condensate=v);
            fields.Children.Add(row);
            SegmentEditorsPanel.Children.Add(new Border{Child=fields,BorderBrush=Brushes.LightGray,BorderThickness=new Thickness(1),
                Padding=new Thickness(9),CornerRadius=new CornerRadius(5),Margin=new Thickness(0,0,0,6)});
        }
    }

    private bool TryReadSegmentSettings(ProcessType type,out List<ProcessSegmentSetting> settings,out string error)
    {
        settings=new();error="";
        if(type!=ProcessType.Automatic)return true;
        for(int i=0;i<_selectedPointIds.Count-1;i++)
        {
            var key=ProcessSegmentSetting.Key(_selectedPointIds,i);
            if(!_segmentDrafts.TryGetValue(key,out var draft))continue;
            if(!TryReadOptionalFlow(draft.Flow,out double? flow,out error))return false;
            double? fcs=null,cond=null;
            if(!string.IsNullOrWhiteSpace(draft.Fcs))
            {
                if(!TryReadUnitInterval(draft.Fcs,out double f)) {error="El FCS debe estar entre 0 y 1.";return false;}
                fcs=f;
            }
            if(!string.IsNullOrWhiteSpace(draft.Condensate))
            {
                if(!TryReadFinite(draft.Condensate,out double t)||TemperatureToSI(t)< -50||TemperatureToSI(t)>100)
                {error=$"Temperatura del condensado fuera del intervalo {TemperatureFromSI(-50):0.##} a {TemperatureFromSI(100):0.##} {TempUnit}.";return false;}
                cond=TemperatureToSI(t);
            }
            settings.Add(new ProcessSegmentSetting(key.Item1,key.Item2,draft.Type,flow,fcs,cond,key.Occurrence));
        }
        return true;
    }

    private void RefreshGroupCards()
    {
        if(ProcessCardsPanel is null || MainProcessResultsPanel is null)return;
        var activeIds=PsychrometricChart.ActivePoints.Select(p=>p.Id).ToHashSet();
        foreach(int id in _draftGroups.Keys.ToArray())
        {
            if(_savedProcesses.Any(p=>p.Id==id)){_draftGroups.Remove(id);continue;}
            var draft=_draftGroups[id];_draftGroups[id]=draft with {PointIds=draft.PointIds.Where(activeIds.Contains).ToArray()};
        }
        _collapsedProcessGroups.IntersectWith(_savedProcesses.Select(p=>p.Id).Concat(_draftGroups.Keys));
        double groupScrollOffset=ProcessGroupsScroll.VerticalOffset;
        ProcessCardsPanel.Children.Clear();MainProcessResultsPanel.Children.Clear();
        foreach(var group in _savedProcesses.Concat(_draftGroups.Values).OrderBy(p=>p.Id))
        {
            bool pending=_draftGroups.ContainsKey(group.Id);
            var color=(Brush)new BrushConverter().ConvertFromString(group.ColorHex)!;
            var body=new StackPanel();
            var header=new DockPanel{Margin=new Thickness(0,0,0,7),LastChildFill=true};
            bool expanded=!_collapsedProcessGroups.Contains(group.Id);
            var expandToggle=new System.Windows.Controls.Primitives.ToggleButton
            {
                Content=expanded?"▾":"▸", IsChecked=expanded, Width=30, Height=30,
                Margin=new Thickness(0,0,7,0), ToolTip=expanded?"Contraer grupo":"Expandir grupo",
                VerticalAlignment=VerticalAlignment.Center
            };
            DockPanel.SetDock(expandToggle,Dock.Left);
            var pencil=new Button{Content="✎",ToolTip="Editar "+group.Name,Width=32,Height=30,Margin=new Thickness(8,0,0,0)};
            System.Windows.Automation.AutomationProperties.SetName(pencil,"Editar "+group.Name);
            DockPanel.SetDock(pencil,Dock.Right);
            pencil.Click+=(_,_)=>OpenBuilderGroupEditor(group.Id);
            header.Children.Add(pencil);
            header.Children.Add(expandToggle);
            header.Children.Add(new TextBlock{Text=group.Name+(group.Visible?"":" (oculto)"),Foreground=color,
                FontWeight=FontWeights.SemiBold,FontSize=15,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center});
            body.Children.Add(header);
            var ids=group.PointIds.ToArray();
            string summary=ids.Length==0?"Sin puntos · arrastre aquí los estados.":
                ids.Length==1?$"1 punto · P{ids[0]} · sin tramo":
                $"{ids.Length-1} tramo(s) · {string.Join(" → ",ids.Select(id=>"P"+id))}";
            var summaryText=new TextBlock{Text=summary,
                Foreground=Brushes.DimGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,6)};
            body.Children.Add(summaryText);
            var list=new ListBox{Tag=group.Id,ItemsSource=CardsFor(ids,true,group.ColorHex),
                ItemTemplate=(DataTemplate)FindResource("ProcessPointCardTemplate"),
                ItemContainerStyle=(Style)FindResource("ProcessPointItemStyle"),
                MinHeight=100,MaxHeight=340,AllowDrop=true,Background=Brushes.WhiteSmoke,
                HorizontalContentAlignment=HorizontalAlignment.Stretch};
            ScrollViewer.SetVerticalScrollBarVisibility(list,ScrollBarVisibility.Auto);
            ScrollViewer.SetHorizontalScrollBarVisibility(list,ScrollBarVisibility.Disabled);
            ScrollViewer.SetCanContentScroll(list,false);
            list.PreviewMouseLeftButtonDown+=ProcessList_MouseDown;
            list.PreviewMouseMove+=ProcessList_MouseMove;
            list.Drop+=BuilderGroup_Drop;
            list.MouseDoubleClick+=BuilderGroup_DoubleClick;
            body.Children.Add(list);
            Expander? results=null;
            if(!pending)
            {
                results=new Expander{Header="Resultados del grupo",Margin=new Thickness(0,8,0,0)};
                var tables=new StackPanel();
                RenderGroupResults(group,tables);
                results.Content=tables;body.Children.Add(results);
                if(group.Visible)RenderGroupResults(group,MainProcessResultsPanel,false);
            }
            // [5.1C.20.3] El encabezado, resumen y lápiz permanecen siempre;
            // contraer no cambia los estados, tramos, visibilidad en carta ni color.
            void SetExpanded(bool show)
            {
                if(show)_collapsedProcessGroups.Remove(group.Id);
                else _collapsedProcessGroups.Add(group.Id);
                list.Visibility=show?Visibility.Visible:Visibility.Collapsed;
                if(results is not null)results.Visibility=show?Visibility.Visible:Visibility.Collapsed;
                expandToggle.Content=show?"▾":"▸";
                expandToggle.ToolTip=show?"Contraer "+group.Name:"Expandir "+group.Name;
                System.Windows.Automation.AutomationProperties.SetName(expandToggle,
                    show?"Contraer "+group.Name:"Expandir "+group.Name);
            }
            expandToggle.Checked+=(_,_)=>SetExpanded(true);
            expandToggle.Unchecked+=(_,_)=>SetExpanded(false);
            SetExpanded(expanded);
            var border=new Border{Child=body,Tag=group.Id,AllowDrop=true,Padding=new Thickness(10),Margin=new Thickness(0,0,0,10),
                Background=Brushes.White,BorderBrush=color,BorderThickness=new Thickness(1.5),CornerRadius=new CornerRadius(6)};
            border.Drop+=BuilderGroup_Drop;
            ProcessCardsPanel.Children.Add(border);
        }
        ProcessGroupsScroll.ScrollToVerticalOffset(groupScrollOffset);
        if(_savedProcesses.Count==0)MainProcessResultsPanel.Children.Add(new TextBlock{Text="Todavía no hay un proceso calculado.",Foreground=Brushes.DimGray});
        if(ProcessCardsPanel.Children.Count==0)ProcessCardsPanel.Children.Add(new TextBlock{
            Text="Pulse + para crear un grupo.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DimGray,Margin=new Thickness(0,6,0,0)});
    }

    private void RenderGroupResults(PsychrometricProcess process,StackPanel host,bool clear=true)
    {
        var units=ChartMetricsPanel.CurrentUnits;
        bool ip=units==UnitSystem.IP;
        string flowUnit=ip?"lb/h":"kg/s",powerUnit=ip?"Btu/h":"kW";
        string Flow(double value)=> (value*(ip?7936.64143865559:1)).ToString("0.######");
        string Power(double value)=> (value*(ip?3412.14163312794:1)).ToString("0.###");
        if(clear)host.Children.Clear();
        host.Children.Add(new TextBlock{Text=process.Name,FontWeight=FontWeights.Bold,Margin=new Thickness(0,8,0,5)});
        var points=process.PointIds.Distinct().Select(id=>PsychrometricChart.ActivePoints.FirstOrDefault(p=>p.Id==id))
            .OfType<ChartPoint>().ToArray();
        if(points.Length>0)
            host.Children.Add(UI.AshraeStudyPanel.BuildStateTable(points,points.Select(p=>p.Name).ToArray(),units));
        var table=new Grid { Margin=new Thickness(0,0,0,8) };
        void Row(params string[] cells)
        {
            int index=table.RowDefinitions.Count;table.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
            for(int j=0;j<cells.Length;j++)
            {
                var label=new TextBlock{Text=cells[j],TextWrapping=TextWrapping.Wrap,Margin=new Thickness(5),FontSize=12,
                    FontWeight=index==0?FontWeights.SemiBold:FontWeights.Normal};
                var cell=new Border{Child=label,BorderBrush=Brushes.LightGray,BorderThickness=new Thickness(.5),
                    Background=index==0?Brushes.WhiteSmoke:Brushes.White};
                Grid.SetRow(cell,index);Grid.SetColumn(cell,j);table.Children.Add(cell);
            }
        }
        var segmentTable=new Grid{Margin=new Thickness(0,0,0,8)};
        foreach(double weight in new[]{1.2,1,1,1,1,1,1,1})segmentTable.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(weight,GridUnitType.Star)});
        table=segmentTable;
        Row("Tramo",$"ΔT ({EducationalUnits.TemperatureUnit(units)})",$"ΔW ({EducationalUnits.HumidityUnit(units)})",$"Δh ({EducationalUnits.EnthalpyUnit(units)})",$"Potencia ({powerUnit})","FCS calculado","FCS indicado",$"Condensado ({flowUnit})");
        if(process.Type!=ProcessType.AdiabaticMixing && process.Type!=ProcessType.Branching)
        {
            for(int i=0;i<process.PointIds.Count-1;i++)
            {
                var from=points.FirstOrDefault(p=>p.Id==process.PointIds[i]);var to=points.FirstOrDefault(p=>p.Id==process.PointIds[i+1]);
                if(from is null||to is null)continue;
                var seg=ProcessSegmentSetting.Find(process.SegmentSettings,process.PointIds,i);
                var type=seg?.Type??(process.Type==ProcessType.Bypass?ProcessType.Automatic:process.Type);
                if(ProcessCalculator.TryCalculate(from,to,type,seg?.DryAirMassFlowKgS??process.DryAirMassFlowKgS,
                    seg?.CondensateTemperatureC??process.CondensateTemperatureC,out var r,out _))
                    Row(from.Name+" → "+to.Name,EducationalUnits.DT(r.DeltaTemperatureC,units).ToString("0.###"),EducationalUnits.W(r.DeltaHumidityRatioKgPerKg,units).ToString("0.###"),
                        EducationalUnits.H(r.DeltaEnthalpyKJPerKgDryAir,units).ToString("0.###"),r.ThermalPowerKW is double power?Power(power):"Sin caudal",r.SensibleHeatFactor?.ToString("0.###")??"—",seg?.SensibleHeatFactor?.ToString("0.###")??"—",r.CondensateMassFlowKgS is double condensate?Flow(condensate):"—");
            }
            host.Children.Add(segmentTable);
        }
        else
        {
            table=new Grid{Margin=new Thickness(0,0,0,8)};
            table.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(2,GridUnitType.Star)});
            table.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            Row("Magnitud","Resultado");
            if(process.Type==ProcessType.AdiabaticMixing && process.MixingInletFlowsM3H is {Count:>0} flows && points.Length>=flows.Count)
            {
                double totalMixed=0;
                for(int i=0;i<flows.Count;i++)
                {
                    double flow=flows[i]/3600/points[i].SpecificVolumeM3PerKgDryAir;
                    totalMixed+=flow;Row($"Caudal másico de {points[i].Name} ({flowUnit})",Flow(flow));
                }
                Row($"Caudal másico total ({flowUnit})",Flow(totalMixed));
            }
            else if(process.Type==ProcessType.Branching && points.Length==3 && process.DryAirMassFlowKgS is double total &&
                process.BranchFractionA is double fraction &&
                ProcessCalculator.TryBranchBalance(points[0],points[1],points[2],total,fraction,out var b,out _) && b is not null)
            {
                Row($"Caudal de entrada ({flowUnit})",Flow(total));
                Row($"Caudal rama 1 ({flowUnit})",Flow(b.FirstMassFlowKgS));
                Row($"Caudal rama 2 ({flowUnit})",Flow(b.SecondMassFlowKgS));
                Row($"Agua rama 1 ({flowUnit})",Flow(b.AddedWaterFirstKgS));
                Row($"Agua rama 2 ({flowUnit})",Flow(b.AddedWaterSecondKgS));
                Row($"Potencia rama 1 ({powerUnit})",Power(b.ThermalFirstKW));
                Row($"Potencia rama 2 ({powerUnit})",Power(b.ThermalSecondKW));
            }
            host.Children.Add(table);
        }
    }

    private void ExportWordReport_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog{Title="Exportar informe de la calculadora",Filter="Documento Word (*.docx)|*.docx",FileName="Calculadora_Refrigeracion_Parte1_Informe.docx"};
        if(dialog.ShowDialog(this)!=true)return;
        try { WordProcessReport.Write(dialog.FileName,PressureSelector.CurrentPressureKPa,PsychrometricChart.ActivePoints,_savedProcesses,ChartMetricsPanel.CurrentUnits); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException)
        { MessageBox.Show(ex.Message,"Exportar Word",MessageBoxButton.OK,MessageBoxImage.Warning); }
    }
}
