using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics.UI;

// [5.7C] Temario en el orden de fundamentos ASHRAE; fracciones matemáticas reales
// en árbol visual WPF (no fracciones escritas con barras en texto monoespaciado).
public partial class AshraeStudyPanel : UserControl
{
    private bool _ready;
    private bool _updating;
    private double _pressureKPa=AtmosphericPressure.StandardSeaLevelKPa;
    private ChartPoint? _state;

    public AshraeStudyPanel()
    {
        InitializeComponent();
        _updating=true;
        StudySelector.ItemsSource=AshraeStudyCatalog.Units;
        FormulaSelector.ItemsSource=AshraeStudyCatalog.Equations;
        ConstructionStepSelector.ItemsSource=AshraeStudyCatalog.ConstructionSteps;
        StudySelector.SelectedIndex=0;
        FormulaSelector.SelectedIndex=0;
        ConstructionStepSelector.SelectedIndex=0;
        _updating=false;
        _ready=true;
        RefreshStudy();
    }

    public void SetPressure(double pressureKPa)
    {
        if(!double.IsFinite(pressureKPa)||pressureKPa<=0)return;
        _pressureKPa=pressureKPa;
        RefreshStudy();
    }

    public void SetLiveLesson(TheoryLesson? lesson)
    {
        if(lesson is null)return;
        _pressureKPa=lesson.Initial.PressureKPa;
        _updating=true;
        TemperatureSlider.Value=Math.Clamp(lesson.Initial.DryBulbC,
            TemperatureSlider.Minimum,TemperatureSlider.Maximum);
        HumiditySlider.Value=Math.Clamp(lesson.Initial.RelativeHumidityPercent,
            HumiditySlider.Minimum,HumiditySlider.Maximum);
        _updating=false;
        RefreshStudy();
    }

    private void StudySelector_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!_ready||_updating)return;
        RefreshStudy();
    }
    private void FormulaSelector_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!_ready||_updating)return;
        RefreshFormula();
    }
    private void ConstructionStepSelector_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!_ready||_updating)return;
        RefreshConstruction();
    }
    private void ExampleChanged(object sender,RoutedEventArgs e)
    {
        if(!_ready||_updating)return;
        RefreshState();
    }
    private void PreviousUnit_Click(object sender,RoutedEventArgs e)
        => StudySelector.SelectedIndex=Math.Max(0,StudySelector.SelectedIndex-1);
    private void NextUnit_Click(object sender,RoutedEventArgs e)
        => StudySelector.SelectedIndex=Math.Min(StudySelector.Items.Count-1,StudySelector.SelectedIndex+1);
    private void PreviousStep_Click(object sender,RoutedEventArgs e)
        => ConstructionStepSelector.SelectedIndex=Math.Max(0,ConstructionStepSelector.SelectedIndex-1);
    private void NextStep_Click(object sender,RoutedEventArgs e)
        => ConstructionStepSelector.SelectedIndex=Math.Min(ConstructionStepSelector.Items.Count-1,
            ConstructionStepSelector.SelectedIndex+1);

    public void SelectUnit(string id)
    {
        var unit=AshraeStudyCatalog.Find(id);
        if(unit is not null)StudySelector.SelectedItem=unit;
    }

    public void SelectConstructionStep(int step)
    {
        if(step>=1 && step<=ConstructionStepSelector.Items.Count)
        {
            ConstructionExpander.IsExpanded=true;
            ConstructionStepSelector.SelectedIndex=step-1;
        }
    }

    private void RefreshStudy()
    {
        if(!_ready)return;
        if(StudySelector.SelectedItem is AshraeStudyUnit unit)
        {
            UnitHeading.Text=unit.Title;
            UnitDefinition.Text=unit.Definition;
            UnitDerivation.Text=unit.Derivation;
            UnitOrigin.Text=unit.Origin;
            UnitInterpretation.Text=unit.Interpretation;
            UnitLimitation.Text="LÍMITE DE VALIDEZ: "+unit.Limitation;
            UnitExample.Text=unit.Example;
            BibliographyReference.Text="Fuente principal: "+unit.Source+
                "\nInterpretaciones y ejemplos originales de FrioCalc. El índice sigue ASHRAE, no la secuencia de la presentación docente.";
            var formula=AshraeStudyCatalog.FindEquation(unit.EquationKey);
            if(formula is not null)
            {
                _updating=true;
                FormulaSelector.SelectedItem=formula;
                _updating=false;
            }
        }
        RefreshState();
        RefreshConstruction();
    }

    private void RefreshState()
    {
        if(!_ready)return;
        double temperature=TemperatureSlider.Value,relativeHumidity=HumiditySlider.Value;
        TemperatureValue.Text=$"Tbs: {temperature:0} °C";
        HumidityValue.Text=$"HR: {relativeHumidity:0} %";
        PressureValue.Text=$"Presión absoluta aplicada: {_pressureKPa:0.###} kPa";
        _state=null;
        try
        {
            double w=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(
                temperature,relativeHumidity/100.0,_pressureKPa);
            if(PointPropertyCalculator.TryCreatePoint(9901,"Ejemplo","#1D4ED8",
                _pressureKPa,temperature,w,out var point)&&point is not null)
                _state=point;
        }
        catch(ArgumentException){/* Estado incompatible con P; texto de aviso. */}
        if(_state is null)
            LiveValues.Text="No existe un estado monofásico válido para esta combinación de T, HR y presión.";
        else
            LiveValues.Text=$"Resultado: W={_state.HumidityRatioKgPerKg*1000:0.###} g/kgda; "+
                $"h={_state.EnthalpyKJPerKgDryAir:0.###} kJ/kgda; "+
                $"v={_state.SpecificVolumeM3PerKgDryAir:0.######} m³/kgda; "+
                $"Tbh={_state.WetBulbC:0.###} °C; "+
                $"Trocío={_state.DewPointC:0.###} °C. "+
                "Cambie Tbs o HR para observar las diferencias.";
        RefreshFormula();
        RefreshConstruction();
    }

    public static UIElement RenderMathematicalFraction(AshraeEquation formula)
    {
        var horizontal=new StackPanel{Orientation=Orientation.Horizontal,
            VerticalAlignment=VerticalAlignment.Center};
        horizontal.Children.Add(new TextBlock{Text=formula.Left+"  =  ",
            FontSize=23,FontFamily=new FontFamily("Cambria Math"),
            VerticalAlignment=VerticalAlignment.Center});
        var fraction=new StackPanel{Orientation=Orientation.Vertical,
            HorizontalAlignment=HorizontalAlignment.Center};
        fraction.Children.Add(new TextBlock{Text=formula.Numerator,
            FontSize=20,TextAlignment=TextAlignment.Center,
            FontFamily=new FontFamily("Cambria Math"),Margin=new Thickness(5,0,5,3)});
        fraction.Children.Add(new Border{Height=1.8,Background=Brushes.Black,
            Margin=new Thickness(1,0,1,2)});
        fraction.Children.Add(new TextBlock{Text=formula.Denominator,
            FontSize=20,TextAlignment=TextAlignment.Center,
            FontFamily=new FontFamily("Cambria Math"),Margin=new Thickness(5,0,5,0)});
        horizontal.Children.Add(fraction);
        return horizontal;
    }

    private void RefreshFormula()
    {
        if(!_ready || FormulaSelector.SelectedItem is not AshraeEquation equation)return;
        FormulaVisual.Content=RenderMathematicalFraction(equation);
        FormulaUnits.Text="Resultado: "+equation.Units;
        FormulaOrigin.Text="Origen: "+equation.Origin;
        FormulaDerivation.Text="Deducción: "+equation.Derivation;
        FormulaLimitations.Text="Condiciones: "+equation.Limitations;
        FormulaSubstitution.Text=ExampleSubstitution(equation,_state,_pressureKPa);
    }

    private static string ExampleSubstitution(AshraeEquation e,ChartPoint? point,double p)
    {
        if(point is null)return "Elija condiciones termodinámicamente válidas para sustituir valores.";
        double t=point.DryBulbC,w=point.HumidityRatioKgPerKg,
            ps=SaturationVaporPressure.GetPressureKPa(t),
            pv=p*w/(.621945+w);
        return e.Key switch
        {
            "W"=>$"pᵥ={pv:0.######} kPa; P={p:0.###} kPa. W=(0,621945×{pv:0.######})/({p:0.###}−{pv:0.######})={w:0.######} kg/kgda = {w*1000:0.###} g/kgda.",
            "pv"=>$"pᵥ=({p:0.###}×{w:0.######})/(0,621945+{w:0.######})={pv:0.######} kPa.",
            "RH"=>$"φ=100×{pv:0.######}/{ps:0.######}={point.RelativeHumidityPercent:0.###}%. La presión de saturación corresponde a {t:0.###}°C.",
            "Ws"=>ps>=p?"No hay Ws monofásico a esta P y T.":
                 $"Wₛ=(0,621945×{ps:0.######})/({p:0.###}−{ps:0.######})={.621945*ps/(p-ps)*1000:0.###} g/kgda.",
            "h"=>$"h≈1,006×{t:0.###}+{w:0.######}×(2501+1,86×{t:0.###})={point.EnthalpyKJPerKgDryAir:0.###} kJ/kgda (modelo aproximado).",
            "v"=>$"v≈R_da×{t+273.15:0.###}×(1+1,607858×{w:0.######})/{p:0.###}={point.SpecificVolumeM3PerKgDryAir:0.######} m³/kgda, unidades coherentes.",
            "Tdp"=>double.IsFinite(point.DewPointC)?
                 $"pᵥ={pv:0.######} kPa; resolver p_ws(Trocío)={pv:0.######} ⇒ Trocío={point.DewPointC:0.###} °C.":
                 "Con W=0 el punto de rocío no está definido.",
            "Mdot"=>$"Si V̇=3000 m³/h, ṁ_da=(3000/3600)/{point.SpecificVolumeM3PerKgDryAir:0.######}={(3000.0/3600)/point.SpecificVolumeM3PerKgDryAir:0.###} kgda/s.",
            "MixW"=>"Para dos entradas reales calcule ṁ_i=V̇_i/v_i y después sustituya cada W_i. No promedie m³/h.",
            "SHR"=>"Ejemplo de recinto: Q_sensible=150 kW y Q_total=200 kW, FCS=150/200=0,75. No fija estado de impulsión.",
            "BF"=>"Ejemplo: BF=0,20, h₂=0,20·h₁+0,80·h_ADP. Sin ADP no hay estado de salida.",
            "Tbh"=>$"Estado actual Tbh={point.WetBulbC:0.###}°C. Se calcula resolviendo el balance de saturación adiabática a P={p:0.###} kPa.",
            _=>"Seleccione una ecuación."
        };
    }

    private void RefreshConstruction()
    {
        if(!_ready || ConstructionStepSelector.SelectedItem is not ChartConstructionStep step)return;
        ConstructionStepTitle.Text=$"Paso {step.Number} de {AshraeStudyCatalog.ConstructionSteps.Count}: {step.Title}";
        ConstructionWhat.Text=step.WhatToDo;
        ConstructionWhy.Text=step.WhyThisMatters;
        ConstructionExample.Text=step.NumericalExample;
        ConstructionBasis.Text=step.Basis;
        DrawChart(step.Number);
    }

    private void DrawChart(int untilStep)
    {
        ConstructionCanvas.Children.Clear();
        const double left=45,top=17,width=735,height=265,bottom=37,right=18;
        const double minT=-10,maxT=55,maxW=.060;
        double X(double t)=>left+(t-minT)/(maxT-minT)*(width-left-right);
        double Y(double w)=>height-bottom-w/maxW*(height-top-bottom);
        void DrawLine(double x1,double y1,double x2,double y2,Brush brush,double stroke=1)
            =>ConstructionCanvas.Children.Add(new Line
            {X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=brush,StrokeThickness=stroke});
        void AddText(string txt,double x,double y,Brush brush)
        {
            var label=new TextBlock{Text=txt,FontSize=10.5,Foreground=brush};
            Canvas.SetLeft(label,x);Canvas.SetTop(label,y);
            ConstructionCanvas.Children.Add(label);
        }
        for(int i=-10;i<=55;i+=5)
        {
            double x=X(i);
            DrawLine(x,top,x,height-bottom,i%10==0?Brushes.LightGray:Brushes.Gainsboro);
            if(i%10==0)AddText(i.ToString(CultureInfo.CurrentCulture),x-7,height-bottom+4,Brushes.DimGray);
        }
        for(int g=0;g<=60;g+=10)
        {
            DrawLine(left,Y(g/1000.0),width-right,Y(g/1000.0),Brushes.LightGray);
            AddText(g.ToString(CultureInfo.CurrentCulture),10,Y(g/1000.0)-6,Brushes.DimGray);
        }
        AddText("Tbs [°C]",width*.46,height-15,Brushes.Black);
        AddText("W [g/kgda]",1,0,Brushes.Black);
        if(untilStep<4)return;
        for(int humidityPercent=100;humidityPercent>=10;humidityPercent-=10)
        {
            if(humidityPercent!=100 && untilStep<6)continue;
            var poly=new Polyline
            {
                Stroke=humidityPercent==100?Brushes.Black:Brushes.SlateGray,
                StrokeThickness=humidityPercent==100?2.4:1.1
            };
            for(double t=minT;t<=maxT;t+=.4)
            {
                double ps=SaturationVaporPressure.GetPressureKPa(t);
                double pv=ps*humidityPercent/100;
                if(pv>=_pressureKPa)break;
                double w=.621945*pv/(_pressureKPa-pv);
                if(w<=maxW)poly.Points.Add(new Point(X(t),Y(w)));
            }
            if(poly.Points.Count>=2)ConstructionCanvas.Children.Add(poly);
        }
        if(untilStep>=7)
        {
            double h=50;
            var poly=new Polyline{Stroke=Brushes.Teal,StrokeThickness=2};
            for(double t=minT;t<=maxT;t+=.5)
            {
                double w=(h-1.006*t)/(2501+1.86*t);
                double ps=SaturationVaporPressure.GetPressureKPa(t);
                if(ps>=_pressureKPa)break;
                double ws=.621945*ps/(_pressureKPa-ps);
                if(w>=0&&w<=maxW&&w<=ws)poly.Points.Add(new Point(X(t),Y(w)));
            }
            if(poly.Points.Count>=2)ConstructionCanvas.Children.Add(poly);
        }
        if(untilStep>=12&&_state is ChartPoint state &&
           state.DryBulbC>=minT&&state.DryBulbC<=maxT &&
           state.HumidityRatioKgPerKg>=0&&state.HumidityRatioKgPerKg<=maxW)
        {
            var mark=new Ellipse{Width=10,Height=10,Fill=Brushes.RoyalBlue};
            Canvas.SetLeft(mark,X(state.DryBulbC)-5);
            Canvas.SetTop(mark,Y(state.HumidityRatioKgPerKg)-5);
            ConstructionCanvas.Children.Add(mark);
            AddText("Estado del ejemplo",X(state.DryBulbC)+8,
                Math.Max(top,Y(state.HumidityRatioKgPerKg)-15),Brushes.RoyalBlue);
        }
    }
}