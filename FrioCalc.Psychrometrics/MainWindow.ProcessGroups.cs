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
    private ListBox? _groupDragSource;
    private sealed class SegmentDraft
    {
        public ProcessType Type = ProcessType.Automatic;
        public string Flow = "", Fcs = "", Condensate = "";
    }
    private readonly Dictionary<(int,int),SegmentDraft> _segmentDrafts = new();

    private void RefreshProcessGroupLists()
    {
        if (AvailablePointsList is null || ProcessSelectionList is null) return;
        AvailablePointsList.ItemsSource = PsychrometricChart.ActivePoints.ToArray();
        ProcessSelectionList.ItemsSource = _selectedPointIds.Select(id =>
            PsychrometricChart.ActivePoints.FirstOrDefault(p=>p.Id==id)).OfType<ChartPoint>().ToArray();
        RefreshSegmentEditors();
    }

    private void ProcessList_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _groupDragSource = sender as ListBox;
        _groupDragStart = e.GetPosition(this);
        _groupDragPoint = _groupDragSource is ListBox box &&
            e.OriginalSource is DependencyObject child &&
            ItemsControl.ContainerFromElement(box,child) is ListBoxItem item
            ? item.Content as ChartPoint : null;
    }

    private void ProcessList_MouseMove(object sender, MouseEventArgs e)
    {
        if(e.LeftButton!=MouseButtonState.Pressed || _groupDragPoint is null || _groupDragSource is null) return;
        Vector delta=e.GetPosition(this)-_groupDragStart;
        if(Math.Abs(delta.X)<SystemParameters.MinimumHorizontalDragDistance &&
           Math.Abs(delta.Y)<SystemParameters.MinimumVerticalDragDistance) return;
        var data=new DataObject();
        data.SetData("FrioCalc.PointId",_groupDragPoint.Id);
        data.SetData("FrioCalc.FromSequence",ReferenceEquals(_groupDragSource,ProcessSelectionList));
        var source=_groupDragSource;
        _groupDragSource=null; _groupDragPoint=null;
        DragDrop.DoDragDrop(source,data,DragDropEffects.Move);
    }

    private void ProcessSequence_Drop(object sender, DragEventArgs e)
    {
        if(!e.Data.GetDataPresent("FrioCalc.PointId") || e.Data.GetData("FrioCalc.PointId") is not int id) return;
        if(!PsychrometricChart.ActivePoints.Any(p=>p.Id==id))return;
        int target=_selectedPointIds.Count;
        if(e.OriginalSource is DependencyObject child &&
           ItemsControl.ContainerFromElement(ProcessSelectionList,child) is ListBoxItem container &&
           container.Content is ChartPoint other)
        {
            target=_selectedPointIds.IndexOf(other.Id);
            if(e.GetPosition(container).Y>container.ActualHeight/2)target++;
        }
        int original=_selectedPointIds.IndexOf(id);
        if(original>=0)
        {
            _selectedPointIds.RemoveAt(original);
            if(original<target)target--;
        }
        _selectedPointIds.Insert(Math.Clamp(target,0,_selectedPointIds.Count),id);
        RefreshProcessGroupLists();
        e.Handled=true;
    }

    private void AvailablePoints_Drop(object sender, DragEventArgs e)
    {
        if(e.Data.GetDataPresent("FrioCalc.FromSequence") && e.Data.GetData("FrioCalc.FromSequence") is true &&
           e.Data.GetData("FrioCalc.PointId") is int id)
        {
            _selectedPointIds.Remove(id);
            RefreshProcessGroupLists();
            e.Handled=true;
        }
    }

    private void AvailablePoints_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if(AvailablePointsList.SelectedItem is not ChartPoint point)return;
        if(ProcessEditor.Visibility!=Visibility.Visible) NewProcessGroup_Click(sender,e);
        if(!_selectedPointIds.Contains(point.Id))_selectedPointIds.Add(point.Id);
        RefreshProcessGroupLists();
    }

    private void ProcessSequence_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if(ProcessSelectionList.SelectedItem is not ChartPoint point)return;
        _selectedPointIds.Remove(point.Id);
        RefreshProcessGroupLists();
    }

    private void OpenGroupEditor()
    {
        ProcessEditor.Visibility=Visibility.Visible; SequenceEditor.Visibility=Visibility.Visible;
        RefreshProcessGroupLists();
    }

    private void CloseGroupEditor()
    {
        ProcessEditor.Visibility=Visibility.Collapsed; SequenceEditor.Visibility=Visibility.Collapsed;
        _editingProcessId=null; _selectedPointIds.Clear(); _segmentDrafts.Clear();
        RefreshProcessGroupLists();
    }

    private void CloseProcessEditor_Click(object sender,RoutedEventArgs e)=>CloseGroupEditor();

    private void NewProcessGroup_Click(object sender,RoutedEventArgs e)
    {
        _editingProcessId=null; _selectedPointIds.Clear(); _segmentDrafts.Clear();
        SavedProcessesList.SelectedIndex=-1;
        ProcessTypeComboBox.SelectedIndex=0;
        CloseCycleCheckBox.IsChecked=false;
        foreach(var input in new[]{NewProcessNameTextBox,RenameProcessTextBox,DryFlowTextBox,
            FcsGuideTextBox,CondensateTemperatureTextBox,MixFlowsListTextBox,MixFlowFirstTextBox,
            MixFlowSecondTextBox,BranchFractionTextBox,EffectivenessTextBox,ApparatusDewPointTextBox,
            BypassFactorTextBox,SteamAddedTextBox,SteamEnthalpyTextBox,DesiccantRemovedTextBox,DesiccantTemperatureTextBox})
            input.Clear();
        ProcessResultsPanel.Children.Clear();
        ProcessResultText.Text="Arrastre los estados a la secuencia del grupo.";
        OpenGroupEditor();
    }

    private void LoadSegmentDrafts(PsychrometricProcess process)
    {
        _segmentDrafts.Clear();
        if(process.SegmentSettings is null)return;
        foreach(var s in process.SegmentSettings)
            _segmentDrafts[(s.FromId,s.ToId)]=new SegmentDraft {
                Type=s.Type, Flow=s.DryAirMassFlowKgS?.ToString(CultureInfo.CurrentCulture)??"",
                Fcs=s.SensibleHeatFactor?.ToString(CultureInfo.CurrentCulture)??"",
                Condensate=s.CondensateTemperatureC?.ToString(CultureInfo.CurrentCulture)??""};
    }

    private void RefreshSegmentEditors()
    {
        if(SegmentEditorsPanel is null)return;
        SegmentEditorsPanel.Children.Clear();
        for(int i=0;i<_selectedPointIds.Count-1;i++)
        {
            var key=(_selectedPointIds[i],_selectedPointIds[i+1]);
            if(!_segmentDrafts.TryGetValue(key,out var draft))
                _segmentDrafts[key]=draft=new SegmentDraft();
            var fields=new StackPanel { Margin=new Thickness(0,5,0,5) };
            fields.Children.Add(new TextBlock { Text=$"P{key.Item1} → P{key.Item2}",FontWeight=FontWeights.SemiBold });
            var methods=new ComboBox { ItemsSource=new[]{ProcessType.Automatic,ProcessType.Sensible,
                ProcessType.HeatingHumidification,ProcessType.CoolingDehumidification,ProcessType.SteamHumidification,
                ProcessType.EvaporativeHumidification,ProcessType.Desiccant}, SelectedItem=draft.Type,
                Margin=new Thickness(0,4,0,6), MinWidth=120, MaxWidth=340,HorizontalAlignment=HorizontalAlignment.Left };
            methods.SelectionChanged+=(_,_)=>{if(methods.SelectedItem is ProcessType kind)draft.Type=kind;};
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
            Field("Caudal seco (kg/s), opcional",draft.Flow,v=>draft.Flow=v);
            Field("FCS especificado (0 a 1), opcional",draft.Fcs,v=>draft.Fcs=v);
            Field("Temperatura del condensado (°C), opcional",draft.Condensate,v=>draft.Condensate=v);
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
            var key=(_selectedPointIds[i],_selectedPointIds[i+1]);
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
                if(!TryReadFinite(draft.Condensate,out double t)||t< -50||t>100)
                {error="Temperatura del condensado fuera del intervalo −50 a 100 °C.";return false;}
                cond=t;
            }
            settings.Add(new ProcessSegmentSetting(key.Item1,key.Item2,draft.Type,flow,fcs,cond));
        }
        return true;
    }

    private void RefreshGroupCards()
    {
        if(ProcessCardsPanel is null)return;
        ProcessCardsPanel.Children.Clear();
        MainProcessResultsPanel.Children.Clear();
        foreach(var group in _savedProcesses)
        {
            var color=(Brush)new BrushConverter().ConvertFromString(group.ColorHex)!;
            var body=new StackPanel();
            var header=new DockPanel();
            var pencil=new Button {Content="✎",ToolTip="Editar grupo",Width=30,Margin=new Thickness(8,0,0,0)};
            DockPanel.SetDock(pencil,Dock.Right);
            pencil.Click+=(_,_)=>{
                SavedProcessesList.SelectedIndex=_savedProcesses.FindIndex(p=>p.Id==group.Id);
                EditProcess_Click(pencil,new RoutedEventArgs());
            };
            header.Children.Add(pencil);
            header.Children.Add(new TextBlock {Text=group.Name+(group.Visible?"":" (oculto)"),Foreground=color,
                FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center});
            body.Children.Add(header);
            body.Children.Add(new TextBlock {Text=string.Join(" → ",group.PointIds.Select(id=>"P"+id)),
                TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,7,0,3),Foreground=color});
            ProcessCardsPanel.Children.Add(new Border {Child=body,Width=200,Padding=new Thickness(10),
                Margin=new Thickness(0,0,8,8),Background=Brushes.White,BorderBrush=color,
                BorderThickness=new Thickness(1.5),CornerRadius=new CornerRadius(5)});
            if(group.Visible)RenderGroupResults(group,MainProcessResultsPanel,false);
        }
        if(_savedProcesses.Count==0)MainProcessResultsPanel.Children.Add(new TextBlock{Text="Todavía no hay un proceso calculado.",Foreground=Brushes.DimGray});
    }

    private void RenderGroupResults(PsychrometricProcess process,StackPanel host,bool clear=true)
    {
        if(clear)host.Children.Clear();
        host.Children.Add(new TextBlock{Text=process.Name,FontWeight=FontWeights.Bold,Margin=new Thickness(0,8,0,5)});
        var points=process.PointIds.Distinct().Select(id=>PsychrometricChart.ActivePoints.FirstOrDefault(p=>p.Id==id))
            .OfType<ChartPoint>().ToArray();
        var table=new Grid { Margin=new Thickness(0,0,0,8) };
        foreach(double weight in new[]{1.4,1,1,1,1,1}) table.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(weight,GridUnitType.Star)});
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
        Row("Punto","Tbs (°C)","HR (%)","W (g/kg)","h (kJ/kg)","v (m³/kg)");
        foreach(var p in points)Row(p.Name,p.DryBulbC.ToString("0.##"),p.RelativeHumidityPercent.ToString("0.##"),
            (p.HumidityRatioKgPerKg*1000).ToString("0.##"),p.EnthalpyKJPerKgDryAir.ToString("0.##"),p.SpecificVolumeM3PerKgDryAir.ToString("0.###"));
        host.Children.Add(table);
        var segmentTable=new Grid{Margin=new Thickness(0,0,0,8)};
        foreach(double weight in new[]{1.2,1,1,1,1,1})segmentTable.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(weight,GridUnitType.Star)});
        table=segmentTable;
        Row("Tramo","ΔT (°C)","ΔW (g/kg)","Δh (kJ/kg)","Potencia (kW)","FCS");
        if(process.Type!=ProcessType.AdiabaticMixing && process.Type!=ProcessType.Branching)
        {
            for(int i=0;i<process.PointIds.Count-1;i++)
            {
                var from=points.FirstOrDefault(p=>p.Id==process.PointIds[i]);var to=points.FirstOrDefault(p=>p.Id==process.PointIds[i+1]);
                if(from is null||to is null)continue;
                var seg=process.SegmentSettings?.FirstOrDefault(x=>x.FromId==from.Id&&x.ToId==to.Id);
                var type=seg?.Type??(process.Type==ProcessType.Bypass?ProcessType.Automatic:process.Type);
                if(ProcessCalculator.TryCalculate(from,to,type,seg?.DryAirMassFlowKgS??process.DryAirMassFlowKgS,
                    seg?.CondensateTemperatureC??process.CondensateTemperatureC,out var r,out _))
                    Row(from.Name+" → "+to.Name,r.DeltaTemperatureC.ToString("0.###"),(r.DeltaHumidityRatioKgPerKg*1000).ToString("0.###"),
                        r.DeltaEnthalpyKJPerKgDryAir.ToString("0.###"),r.ThermalPowerKW?.ToString("0.###")??"Sin caudal",r.SensibleHeatFactor?.ToString("0.###")??"—");
            }
            host.Children.Add(segmentTable);
        }
        else if(!string.IsNullOrWhiteSpace(process.Report))
            host.Children.Add(new TextBlock{Text=process.Report,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,7)});
    }

    private void ExportWordReport_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog{Title="Exportar informe de FrioCalc",Filter="Documento Word (*.docx)|*.docx",FileName="FrioCalc_Informe.docx"};
        if(dialog.ShowDialog(this)!=true)return;
        try { WordProcessReport.Write(dialog.FileName,PressureSelector.CurrentPressureKPa,PsychrometricChart.ActivePoints,_savedProcesses); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException)
        { MessageBox.Show(ex.Message,"Exportar Word",MessageBoxButton.OK,MessageBoxImage.Warning); }
    }
}
