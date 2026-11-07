using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics.UI;

// [5.5C.1] Demostraciones psicrométricas físicamente acotadas; sin falsas potencias.
public partial class ProcessTheoryPanel : UserControl
{
    private bool _ready;
    private double _pressureKPa = AtmosphericPressure.StandardSeaLevelKPa;
    public ProcessTheoryPanel()
    {
        InitializeComponent();
        _ready = true;
        Recompute();
    }

    public void SetPressure(double pressureKPa)
    {
        if (!double.IsFinite(pressureKPa) || pressureKPa <= 0) return;
        _pressureKPa = pressureKPa;
        Recompute();
    }

    private void DiagramCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Recompute();
    private void ControlsChanged(object sender, RoutedEventArgs e) => Recompute();

    // [5.5C.2] Los estados se generan con el motor compartido, no con trazos inventados.
    private void Recompute()
    {
        if (!_ready || DiagramCanvas is null || ProcessSelector.SelectedItem is not ComboBoxItem item) return;
        double t = DryBulbSlider.Value, rh = HumiditySlider.Value, fraction = ParameterSlider.Value / 100.0;
        DryBulbValueLabel.Text = $"Tbs inicial: {t:0} °C";
        RelativeHumidityValueLabel.Text = $"HR inicial: {rh:0} %";
        string type = item.Tag?.ToString() ?? "Heating";
        ParameterLabel.Text = type switch
        {
            "Evaporative" => $"Efectividad de saturación: {fraction:0.00}",
            "Steam" => $"Vapor añadido: {(fraction * 3):0.00} g/kg_da (h_vapor=2700 kJ/kg, ejemplo)",
            "Mixing" => $"Fracción de aire seco de la corriente 2: {fraction:P0}",
            _ => $"Intensidad de variación del proceso: {fraction:P0}"
        };
        ChartPoint? start = null, finish = null;
        string description = "";
        try
        {
            double w = RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(t,rh/100.0,_pressureKPa);
            if (!PointPropertyCalculator.TryCreatePoint(1,"P1","#2563EB",_pressureKPa,t,w,out start) || start==null)
            {
                ShowError("El estado inicial no es físicamente admisible a esta presión."); return;
            }
            switch(type)
            {
                case "Heating":
                {
                    double nextT=t+20*fraction;
                    PointPropertyCalculator.TryCreatePoint(2,"P2","#2563EB",_pressureKPa,nextT,w,out finish);
                    description = "Calefacción sensible: Tbs aumenta, W permanece constante; Δh es positivo.";
                    break;
                }
                case "Cooling":
                {
                    double nextT=t-20*fraction;
                    PointPropertyCalculator.TryCreatePoint(2,"P2","#2563EB",_pressureKPa,nextT,w,out finish);
                    description = "Enfriamiento sensible: Tbs disminuye con W constante, siempre que no se alcance condensación.";
                    break;
                }
                case "Evaporative":
                {
                    ProcessCalculator.TryEvaporativeOutlet(start,fraction,2,"#2563EB",out finish,out description);
                    break;
                }
                case "Steam":
                {
                    ProcessCalculator.TrySteamOutlet(start,fraction*.003,2700,2,"#2563EB",out finish,out description);
                    break;
                }
                case "Dehumidification":
                {
                    if (!double.IsFinite(start.DewPointC))
                    {
                        ShowError("No se puede determinar el punto de rocío de entrada."); return;
                    }
                    double nextT=start.DewPointC-12*fraction;
                    double ws=SaturationHumidityRatio.FromPressuresKPa(
                        _pressureKPa,SaturationVaporPressure.GetPressureKPa(nextT));
                    PointPropertyCalculator.TryCreatePoint(2,"P2","#2563EB",_pressureKPa,nextT,ws,out finish);
                    description="Enfriamiento con deshumidificación: la salida ideal se aproxima sobre la curva de saturación. Un balance real requiere condensado y características del equipo.";
                    break;
                }
                case "Mixing":
                {
                    double w2=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(35,.60,_pressureKPa);
                    if (!PointPropertyCalculator.TryCreatePoint(3,"Exterior","#EA580C",_pressureKPa,35,w2,out ChartPoint? other)
                        || other==null) break;
                    if (fraction==0) { finish=start with {Id=2,Name="Mezcla"}; }
                    else if (fraction==1) { finish=other with {Id=2,Name="Mezcla"}; }
                    else ProcessCalculator.TryMix(new List<(ChartPoint,double)>
                    {
                        (start,1-fraction),(other,fraction)
                    },2,"#2563EB",out finish,out description);
                    description="Mezcla adiabática ideal: P1 variable + corriente 2 fija (35 °C, 60 % HR). " +
                        "El control modifica la fracción de flujo de aire seco; ΣṁW y Σṁh se conservan.";
                    break;
                }
            }
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            ShowError(e.Message); return;
        }
        if (start == null || finish == null)
        {
            ShowError("La combinación propuesta no describe un estado admisible. Ajuste los controles.");return;
        }
        TheoryExplanation.Text = description;
        string summary=$"Presión: {_pressureKPa:0.###} kPa abs\n" +
            $"P1: Tbs={start.DryBulbC:0.##} °C, HR={start.RelativeHumidityPercent:0.#} %, " +
            $"W={start.HumidityRatioKgPerKg*1000:0.###} g/kgda, h={start.EnthalpyKJPerKgDryAir:0.###} kJ/kgda\n" +
            $"P2: Tbs={finish.DryBulbC:0.##} °C, HR={finish.RelativeHumidityPercent:0.#} %, " +
            $"W={finish.HumidityRatioKgPerKg*1000:0.###} g/kgda, h={finish.EnthalpyKJPerKgDryAir:0.###} kJ/kgda\n" +
            $"ΔT={finish.DryBulbC-start.DryBulbC:0.###} °C; " +
            $"ΔW={(finish.HumidityRatioKgPerKg-start.HumidityRatioKgPerKg)*1000:0.###} g/kgda; " +
            $"Δh={finish.EnthalpyKJPerKgDryAir-start.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.\n" +
            "Sin caudal másico no puede determinarse potencia.";
        TheoryValues.Text=summary;
        DrawDiagram(start,finish);
    }

    private void ShowError(string message)
    {
        TheoryExplanation.Text=message;
        TheoryValues.Text="No se representa un estado que viole las condiciones físicas.";
        DiagramCanvas.Children.Clear();
    }

    // [5.5C.3] Diagrama esquemático calculado: Tbs horizontal, W vertical; saturación visible.
    private void DrawDiagram(ChartPoint start, ChartPoint finish)
    {
        DiagramCanvas.Children.Clear();
        double width=DiagramCanvas.Width, height=DiagramCanvas.Height;
        if (width < 250 || height < 200) return;
        const double left=52,right=20,top=13,bottom=39;
        double plotW=width-left-right,plotH=height-top-bottom;
        const double minT=-5,maxT=60,minW=0,maxW=.045;
        double X(double t) => left+(t-minT)/(maxT-minT)*plotW;
        double Y(double w) => top+(maxW-w)/(maxW-minW)*plotH;
        for(double tc=0;tc<=60;tc+=10)
        {
            AddLine(X(tc),top,X(tc),top+plotH,Brushes.Gainsboro,1);
            AddText(tc.ToString("0",CultureInfo.InvariantCulture),X(tc)-8,top+plotH+4,Brushes.DimGray,10);
        }
        for(int g=0;g<=40;g+=10)
        {
            AddLine(left,Y(g/1000.0),left+plotW,Y(g/1000.0),Brushes.Gainsboro,1);
            AddText(g.ToString(CultureInfo.InvariantCulture),14,Y(g/1000.0)-8,Brushes.DimGray,10);
        }
        AddText("Tbs (°C)",left+plotW/2-20,height-17,Brushes.Black,11);
        AddText("W (g/kgda)",4,1,Brushes.Black,11);
        var saturation=new Polyline {Stroke=Brushes.SlateGray,StrokeThickness=1.5};
        for(double tc=-5;tc<=60;tc+=.5)
        {
            double pws=SaturationVaporPressure.GetPressureKPa(tc);
            if(pws>=_pressureKPa) break;
            double w=SaturationHumidityRatio.FromPressuresKPa(_pressureKPa,pws);
            if(w>maxW) break;
            saturation.Points.Add(new Point(X(tc),Y(w)));
        }
        DiagramCanvas.Children.Add(saturation);
        AddLine(X(start.DryBulbC),Y(start.HumidityRatioKgPerKg),
                X(finish.DryBulbC),Y(finish.HumidityRatioKgPerKg),
                new SolidColorBrush(Color.FromRgb(37,99,235)),3);
        DrawPoint(start,"P1",Brushes.RoyalBlue);
        DrawPoint(finish,"P2",Brushes.MediumBlue);
        void DrawPoint(ChartPoint state,string name,Brush color)
        {
            double x=X(state.DryBulbC),y=Y(state.HumidityRatioKgPerKg);
            var dot=new Ellipse {Width=12,Height=12,Fill=color,Stroke=Brushes.White,StrokeThickness=1};
            Canvas.SetLeft(dot,x-6);Canvas.SetTop(dot,y-6);DiagramCanvas.Children.Add(dot);
            AddText(name,x+7,y-16,color,11);
        }
    }

    private void AddLine(double x1,double y1,double x2,double y2,Brush brush,double thick)
    {
        DiagramCanvas.Children.Add(new Line {X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=brush,StrokeThickness=thick});
    }
    private void AddText(string text,double x,double y,Brush brush,double size)
    {
        var label=new TextBlock {Text=text,FontSize=size,Foreground=brush};
        Canvas.SetLeft(label,x);Canvas.SetTop(label,y);
        DiagramCanvas.Children.Add(label);
    }
}
