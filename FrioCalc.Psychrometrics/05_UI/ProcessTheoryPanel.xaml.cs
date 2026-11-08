using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.Primitives;
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
    private bool _updatingControls;
    private double _pressureKPa = AtmosphericPressure.StandardSeaLevelKPa;
    private TheoryLesson? _currentLesson;
    private TheoryLesson? _previousLesson;
    private string _previousKind="";
    private double _previousTemperature=double.NaN;
    private double _previousRH=double.NaN;
    private double _previousFraction=double.NaN;
    private double _previousPressure=double.NaN;

    public ProcessTheoryPanel()
    {
        InitializeComponent();
        _updatingControls=true;
        VariableSelector.ItemsSource=TheoryLessonService.Variables;
        VariableSelector.SelectedIndex=0;
        _updatingControls=false;
        _ready=true;
        Recompute();
    }

    public void SetPressure(double pressureKPa)
    {
        if (!double.IsFinite(pressureKPa) || pressureKPa <= 0) return;
        _pressureKPa = pressureKPa;
        Recompute();
    }

    private void DiagramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if(_ready && _currentLesson is not null)
            DrawDiagram(_currentLesson);
    }

    private void ControlsChanged(object sender, RoutedEventArgs e)
    {
        if(_ready && !_updatingControls)Recompute();
    }

    // [5.5C.2] El texto, las ecuaciones y la carta reciben el mismo estado
    // calculado del motor central; las explicaciones se regeneran al arrastrar.
    private void Recompute()
    {
        if (!_ready || _updatingControls || DiagramCanvas is null ||
            ProcessSelector.SelectedItem is not ComboBoxItem item)return;

        double t=DryBulbSlider.Value,rh=HumiditySlider.Value,intensity=ParameterSlider.Value/100.0;
        string kind=item.Tag?.ToString()??"Heating";
        DryBulbValueLabel.Text=$"Tbs inicial: {t:0} °C";
        RelativeHumidityValueLabel.Text=$"HR inicial: {rh:0} %";
        ParameterLabel.Text=kind switch
        {
            "Evaporative"=>$"Efectividad de saturación ε: {intensity:0.00}",
            "Steam"=>$"Vapor supuesto añadido: {intensity*3:0.00} g/kgda",
            "Mixing"=>$"Fracción de masa de aire seco exterior: {intensity:P0}",
            "Bypass"=>$"Fracción de contacto ideal: {intensity:P0} (BF={1-intensity:0.00})",
            "SHR"=>$"FCS indicado: {intensity:0.00}",
            "Desiccant"=>$"Porción supuesta del agua extraída: {intensity:P0}",
            _=>$"Intensidad de transformación: {intensity:P0}"
        };

        if(!TheoryLessonService.TryEvaluate(kind,t,rh,intensity,_pressureKPa,
            out TheoryLesson? lesson,out string error)||lesson is null)
        {
            _currentLesson=null;
            ShowError(error);
            return;
        }
        _currentLesson=lesson;
        string change="";
        if(_previousLesson is not null && _previousKind==kind)
        {
            if(Math.Abs(t-_previousTemperature)>1e-8)
                change=$"Tbs inicial: {_previousTemperature:0.##} → {t:0.##} °C. ";
            else if(Math.Abs(rh-_previousRH)>1e-8)
                change=$"HR inicial: {_previousRH:0.##} → {rh:0.##} %. ";
            else if(Math.Abs(intensity-_previousFraction)>1e-8)
                change=$"Intensidad: {_previousFraction:P0} → {intensity:P0}. ";
            else if(Math.Abs(_pressureKPa-_previousPressure)>1e-7)
                change=$"Presión absoluta: {_previousPressure:0.###} → {_pressureKPa:0.###} kPa. ";
        }
        if(string.IsNullOrEmpty(change))
            change="Modifique cualquier control para explorar el efecto del proceso. ";
        if(lesson.Final is ChartPoint final)
        {
            string direction=(final.HumidityRatioKgPerKg-lesson.Initial.HumidityRatioKgPerKg) switch
            {
                > 0.000001=>"W aumenta",
                < -0.000001=>"W disminuye",
                _=>"W permanece constante"
            };
            change+=direction+$"; la HR pasa de {lesson.Initial.RelativeHumidityPercent:0.#} % a " +
                $"{final.RelativeHumidityPercent:0.#} %. " +
                $"La entalpía cambia {final.EnthalpyKJPerKgDryAir-lesson.Initial.EnthalpyKJPerKgDryAir:+0.###;-0.###;0} kJ/kgda.";
        }
        else change+="No puede determinarse un estado de impulsión sin información adicional.";
        TheoryChangeText.Text=change;
        TheoryExplanation.Text=lesson.Title+" — "+lesson.Explanation;
        TheoryFormulaText.Text=lesson.Equations;
        TheoryStepsText.Text=lesson.Steps;
        TheoryWarningText.Text="LÍMITES DEL MODELO: "+lesson.Warning;
        TheorySourceText.Text=lesson.References;
        TheoryQuestionText.Text=lesson.Question;
        TheoryValues.Text=FormatStateSummary(lesson);
        int oldSelected=TheoryAnswerCombo.SelectedIndex;
        TheoryAnswerCombo.ItemsSource=lesson.Answers.ToArray();
        TheoryAnswerCombo.SelectedIndex=-1;
        TheoryFeedbackText.Text="Seleccione la respuesta y compruébela.";
        int level=ExplanationLevelComboBox.SelectedIndex;
        TheoryStepsText.Visibility=level==0?Visibility.Collapsed:Visibility.Visible;
        TheoryFormulaText.Visibility=level==0?Visibility.Collapsed:Visibility.Visible;
        TheorySourceText.Visibility=level==2?Visibility.Visible:Visibility.Collapsed;
        VariableSelector_Changed(VariableSelector,new SelectionChangedEventArgs(
            Selector.SelectionChangedEvent,Array.Empty<object>(),Array.Empty<object>()));
        DrawDiagram(lesson);
        _previousLesson=lesson;
        _previousKind=kind;
        _previousTemperature=t;
        _previousRH=rh;
        _previousFraction=intensity;
        _previousPressure=_pressureKPa;
    }

    private static string FormatStateSummary(TheoryLesson lesson)
    {
        static string Describe(string name,ChartPoint point) =>
            $"{name}: Tbs={point.DryBulbC:0.###} °C; HR={point.RelativeHumidityPercent:0.###} %; " +
            $"W={point.HumidityRatioKgPerKg*1000:0.###} g/kgda; h={point.EnthalpyKJPerKgDryAir:0.###} kJ/kgda; " +
            $"Tbh={point.WetBulbC:0.###} °C; v={point.SpecificVolumeM3PerKgDryAir:0.######} m³/kgda.";
        string result=$"P={lesson.Initial.PressureKPa:0.###} kPa absolutos\n"+
            Describe("Entrada",lesson.Initial);
        if(lesson.Final is ChartPoint output)
            result+="\n"+Describe("Salida",output)+
                $"\nΔT={output.DryBulbC-lesson.Initial.DryBulbC:+0.###;-0.###;0} °C; " +
                $"ΔW={(output.HumidityRatioKgPerKg-lesson.Initial.HumidityRatioKgPerKg)*1000:+0.###;-0.###;0} g/kgda; " +
                $"Δh={output.EnthalpyKJPerKgDryAir-lesson.Initial.EnthalpyKJPerKgDryAir:+0.###;-0.###;0} kJ/kgda.";
        else result+="\nEstado de salida: INDETERMINADO (faltan condiciones de diseño).";
        result+="\nSin flujo másico no puede calcularse potencia kW.";
        return result;
    }

    private void ShowError(string message)
    {
        TheoryChangeText.Text="Condición de estudio no resoluble con el modelo seleccionado.";
        TheoryExplanation.Text=message;
        TheoryFormulaText.Text="No se muestran ecuaciones con estados inválidos.";
        TheoryStepsText.Text="";
        TheoryWarningText.Text="Revise la presión, el punto de rocío y el dominio físico.";
        TheoryValues.Text="No se dibuja un punto fuera de saturación o de las hipótesis.";
        TheoryFeedbackText.Text="";
        DiagramCanvas.Children.Clear();
    }

    private void VariableSelector_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!_ready||VariableExplanationText is null)return;
        VariableExplanationText.Text=TheoryLessonService.DescribeVariable(
            (VariableSelector.SelectedItem as TheoryVariable)?.Key??"",
            _currentLesson?.Initial);
    }

    private void CheckTheoryAnswer_Click(object sender,RoutedEventArgs e)
    {
        if(_currentLesson is null)return;
        int choice=TheoryAnswerCombo.SelectedIndex;
        TheoryFeedbackText.Text=choice<0
            ?"Seleccione primero una respuesta."
            :choice==_currentLesson.CorrectAnswer
                ?"CORRECTO. "+_currentLesson.AnswerExplanation
                :"REVISE EL RAZONAMIENTO. "+_currentLesson.AnswerExplanation;
    }

    private void ResetTheoryExample_Click(object sender,RoutedEventArgs e)
    {
        _updatingControls=true;
        DryBulbSlider.Value=25;
        HumiditySlider.Value=50;
        ParameterSlider.Value=50;
        ProcessSelector.SelectedIndex=0;
        ExplanationLevelComboBox.SelectedIndex=1;
        _updatingControls=false;
        _previousLesson=null;
        Recompute();
    }

    // [5.5C.3] Diagrama esquemático calculado: Tbs horizontal, W vertical; saturación visible.
    private void DrawDiagram(TheoryLesson lesson)
    {
        DiagramCanvas.Children.Clear();
        double width=DiagramCanvas.Width,height=DiagramCanvas.Height;
        if(width<250||height<200)return;
        const double left=52,right=20,top=14,bottom=39;
        double plotW=width-left-right,plotH=height-top-bottom;
        const double minT=-5,maxT=60,minW=0,maxW=.045;
        double X(double t)=>left+(t-minT)/(maxT-minT)*plotW;
        double Y(double w)=>top+(maxW-w)/(maxW-minW)*plotH;
        for(double tc=0;tc<=60;tc+=10)
        {
            AddLine(X(tc),top,X(tc),top+plotH,Brushes.Gainsboro,1);
            AddText(tc.ToString("0",CultureInfo.InvariantCulture),
                X(tc)-8,top+plotH+4,Brushes.DimGray,10);
        }
        for(int g=0;g<=40;g+=10)
        {
            AddLine(left,Y(g/1000.0),left+plotW,Y(g/1000.0),Brushes.Gainsboro,1);
            AddText(g.ToString(CultureInfo.InvariantCulture),
                14,Y(g/1000.0)-8,Brushes.DimGray,10);
        }
        AddText("Tbs (°C)",left+plotW/2-20,height-17,Brushes.Black,11);
        AddText("W (g/kgda)",4,1,Brushes.Black,11);
        var sat=new Polyline{Stroke=Brushes.SlateGray,StrokeThickness=1.5};
        for(double tc=-5;tc<=60;tc+=.25)
        {
            double pws=SaturationVaporPressure.GetPressureKPa(tc);
            if(pws>=_pressureKPa)break;
            double ws=SaturationHumidityRatio.FromPressuresKPa(_pressureKPa,pws);
            if(ws>maxW)break;
            sat.Points.Add(new Point(X(tc),Y(ws)));
        }
        DiagramCanvas.Children.Add(sat);
        // Se marca cualquier corriente auxiliar como guía, nunca como un
        // tramo adicional de energía del equipo.
        if(lesson.Auxiliary is ChartPoint auxiliary && (lesson.Kind is "Mixing" or "Bypass"))
        {
            var guide=new Line
            {
                X1=X(lesson.Initial.DryBulbC),Y1=Y(lesson.Initial.HumidityRatioKgPerKg),
                X2=X(auxiliary.DryBulbC),Y2=Y(auxiliary.HumidityRatioKgPerKg),
                Stroke=Brushes.DarkOrange,StrokeThickness=1.3,
                StrokeDashArray=new DoubleCollection{5,4}
            };
            DiagramCanvas.Children.Add(guide);
        }
        for(int i=1;i<lesson.Route.Count;i++)
        {
            ChartPoint previous=lesson.Route[i-1],next=lesson.Route[i];
            var from=new Point(X(previous.DryBulbC),Y(previous.HumidityRatioKgPerKg));
            var to=new Point(X(next.DryBulbC),Y(next.HumidityRatioKgPerKg));
            AddLine(from.X,from.Y,to.X,to.Y,Brushes.RoyalBlue,3);
            double dx=to.X-from.X,dy=to.Y-from.Y,dist=Math.Sqrt(dx*dx+dy*dy);
            if(dist>15)
            {
                double ux=dx/dist,uy=dy/dist;
                var arrow=new Polygon
                {
                    Fill=Brushes.RoyalBlue,
                    Points=new PointCollection {
                        new Point((from.X+to.X)/2+ux*8,(from.Y+to.Y)/2+uy*8),
                        new Point((from.X+to.X)/2-ux*6-uy*5,(from.Y+to.Y)/2-uy*6+ux*5),
                        new Point((from.X+to.X)/2-ux*6+uy*5,(from.Y+to.Y)/2-uy*6-ux*5)
                    }
                };
                DiagramCanvas.Children.Add(arrow);
            }
        }
        DrawPoint(lesson.Initial,"P1",Brushes.RoyalBlue);
        if(lesson.Auxiliary is ChartPoint mid)
            DrawPoint(mid,mid.Name,Brushes.DarkOrange);
        if(lesson.Final is ChartPoint final)
            DrawPoint(final,"P2",Brushes.MediumBlue);
        else AddText("Estado de impulsión sin determinar",left+plotW*.25,top+plotH*.45,
            Brushes.DarkRed,13);

        void DrawPoint(ChartPoint state,string label,Brush color)
        {
            double px=X(state.DryBulbC),py=Y(state.HumidityRatioKgPerKg);
            var dot=new Ellipse{Width=11,Height=11,Fill=color,
                Stroke=Brushes.White,StrokeThickness=1};
            Canvas.SetLeft(dot,px-5.5);
            Canvas.SetTop(dot,py-5.5);
            DiagramCanvas.Children.Add(dot);
            AddText(label,Math.Clamp(px+7,left+1,width-right-45),
                Math.Clamp(py-18,top,height-bottom-15),color,11);
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
