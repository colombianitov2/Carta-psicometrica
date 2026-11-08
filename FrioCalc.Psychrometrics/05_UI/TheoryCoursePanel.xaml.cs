using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics.UI;

// [5.6C] Navegación de todo el curso de A03 y fundamentos de carta.
public partial class TheoryCoursePanel : UserControl
{
    private bool _ready;
    private ChartPoint? _state;
    private double _pressure=101.325;
    public event Action<string>? LessonRequested;
    public event Action<int>? ExerciseRequested;

    public TheoryCoursePanel()
    {
        InitializeComponent();
        TopicSelector.ItemsSource=TheoryCourseCatalog.Topics;
        SlideSelector.ItemsSource=Enumerable.Range(1,36).ToArray();
        TopicSelector.SelectedIndex=0;
        _ready=true;
        DisplayCurrentTopic();
    }

    public TheoryTopic? CurrentTopic => TopicSelector.SelectedItem as TheoryTopic;

    public void SetContext(TheoryLesson? lesson)
    {
        _state=lesson?.Initial;
        if(lesson is not null)_pressure=lesson.Initial.PressureKPa;
        if(_ready)DisplayCurrentTopic();
    }

    public void ShowSlide(int slide)
    {
        TheoryTopic? topic=TheoryCourseCatalog.ForSlide(slide);
        if(topic is null)return;
        TopicSelector.SelectedItem=topic;
        // A03 1–36 se puede abrir directamente desde la presentación.
        if(!Equals(SlideSelector.SelectedItem,slide))
        {
            _syncing=true;
            SlideSelector.SelectedItem=slide;
            _syncing=false;
        }
        DisplayCurrentTopic();
    }

    private bool _syncing;
    private void TopicSelector_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!_ready || _syncing)return;
        TheoryTopic? topic=CurrentTopic;
        if(topic is {Group:"A03"})
        {
            _syncing=true;
            SlideSelector.SelectedItem=topic.FirstSlide;
            _syncing=false;
        }
        DisplayCurrentTopic();
    }

    private void SlideSelector_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!_ready || _syncing)return;
        if(SlideSelector.SelectedItem is int slide)ShowSlide(slide);
    }

    private void PreviousTopic_Click(object sender,RoutedEventArgs e)
        => TopicSelector.SelectedIndex=Math.Max(0,TopicSelector.SelectedIndex-1);

    private void NextTopic_Click(object sender,RoutedEventArgs e)
        => TopicSelector.SelectedIndex=Math.Min(TopicSelector.Items.Count-1,
            TopicSelector.SelectedIndex+1);

    private void ExploreTopic_Click(object sender,RoutedEventArgs e)
    {
        if(CurrentTopic is {LessonKind:{Length:>0}} lesson)
            LessonRequested?.Invoke(lesson.LessonKind);
    }

    private void SolveTopicExercise_Click(object sender,RoutedEventArgs e)
    {
        if(CurrentTopic is {Group:"A03"} topic &&
           Enumerable.Range(topic.FirstSlide,topic.LastSlide-topic.FirstSlide+1)
               .FirstOrDefault(s=>new[]{6,7,14,15,22,23,24,32,36}.Contains(s)) is int slide && slide>0)
            ExerciseRequested?.Invoke(slide);
    }

    private void DisplayCurrentTopic()
    {
        if(!_ready || CurrentTopic is not TheoryTopic t)return;
        TopicHeading.Text=t.Title;
        TopicSlideLabel.Text=t.SlideLabel;
        TopicSummary.Text=t.Summary;
        TopicPhysics.Text=t.PhysicalExplanation;
        TopicChartExplanation.Text=t.ChartReading;
        TopicEquation.Text=t.Equation;
        TopicProcedure.Text=t.Procedure;
        TopicLimits.Text="LÍMITES / PRECAUCIONES: "+t.Limits;
        TopicReference.Text=t.Source;
        ExploreTopicButton.IsEnabled=!string.IsNullOrEmpty(t.LessonKind);
        SolveTopicExerciseButton.IsEnabled=t.Group=="A03" &&
            Enumerable.Range(t.FirstSlide,t.LastSlide-t.FirstSlide+1)
                .Any(s=>new[]{6,7,14,15,22,23,24,32,36}.Contains(s));
        LiveStateExplanation.Text=ExplainLiveState(t);
        DrawMiniChart(t);
    }

    private string ExplainLiveState(TheoryTopic t)
    {
        if(_state is null)
            return "Inicie o modifique un ejemplo del laboratorio para aplicar estas ideas a un estado calculado.";
        ChartPoint s=_state;
        string text=$"P={s.PressureKPa:0.###} kPa abs; Tbs={s.DryBulbC:0.###} °C; " +
            $"HR={s.RelativeHumidityPercent:0.###} %; W={s.HumidityRatioKgPerKg*1000:0.###} g/kgda; " +
            $"h={s.EnthalpyKJPerKgDryAir:0.###} kJ/kgda.";
        if(t.Id=="BASE-03"||t.Id=="BASE-04"||t.Id=="BASE-06")
        {
            double pws=SaturationVaporPressure.GetPressureKPa(s.DryBulbC);
            double ws=pws<s.PressureKPa?
                SaturationHumidityRatio.FromPressuresKPa(s.PressureKPa,pws):double.NaN;
            text+=$" A esta Tbs y P: W_sat={ws*1000:0.###} g/kgda; " +
                $"Trocío={(double.IsFinite(s.DewPointC)?s.DewPointC.ToString("0.###"):"indefinido")} °C.";
        }
        if(t.Id=="BASE-05")text+=$" Tbh={s.WetBulbC:0.###} °C.";
        if(t.Id=="BASE-08")text+=$" v={s.SpecificVolumeM3PerKgDryAir:0.######} m³/kgda.";
        if(t.Id=="BASE-07")text+=" Potencia kW: indeterminada sin ṁda.";
        return text;
    }

    // [5.6C.3] Carta esquemática calculada y sensible a la familia elegida.
    // No copia gráficas de ASHRAE ni muestra estados sobrenaturados.
    private void DrawMiniChart(TheoryTopic topic)
    {
        ReadingCanvas.Children.Clear();
        const double xmin=0,xmax=55,wmin=0,wmax=.035;
        const double l=51,top=12,right=16,bottom=37,width=700,height=265;
        double X(double t)=>l+(t-xmin)/(xmax-xmin)*(width-l-right);
        double Y(double w)=>height-bottom-(w-wmin)/(wmax-wmin)*(height-top-bottom);
        void Line(double x1,double y1,double x2,double y2,Brush color,double thick,bool dashed=false)
        {
            var shape=new Line{X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=color,StrokeThickness=thick};
            if(dashed)shape.StrokeDashArray=new DoubleCollection{4,4};
            ReadingCanvas.Children.Add(shape);
        }
        void Text(string str,double x,double y,Brush color)
        {
            var block=new TextBlock{Text=str,FontSize=11,Foreground=color,
                Background=Brushes.White,IsHitTestVisible=false};
            Canvas.SetLeft(block,x);Canvas.SetTop(block,y);
            ReadingCanvas.Children.Add(block);
        }
        for(double t=0;t<=50;t+=10)
        {
            Line(X(t),top,X(t),height-bottom,Brushes.Gainsboro,1);
            Text(t.ToString("0"),X(t)-7,height-bottom+4,Brushes.DimGray);
        }
        for(int g=0;g<=30;g+=10)
        {
            Line(l,Y(g/1000.0),width-right,Y(g/1000.0),Brushes.Gainsboro,1);
            Text(g.ToString(),17,Y(g/1000.0)-8,Brushes.DimGray);
        }
        Text("Tbs (°C)",width/2-28,height-15,Brushes.Black);
        Text("W (g/kgda)",3,0,Brushes.Black);
        bool highlightSat=topic.Id=="BASE-03";
        bool highlightRh=topic.Id=="BASE-04";
        double pressure=double.IsFinite(_pressure)&&_pressure>0?_pressure:101.325;
        for(int family=100;family>=25;family-=25)
        {
            var shape=new Polyline
            {
                Stroke=family==100?(highlightSat?Brushes.DarkViolet:Brushes.SlateGray):
                    (highlightRh?Brushes.OrangeRed:Brushes.LightSlateGray),
                StrokeThickness=(family==100&&highlightSat)||(family!=100&&highlightRh)?3:1.2
            };
            for(double t=xmin;t<=xmax;t+=.5)
            {
                double ps=SaturationVaporPressure.GetPressureKPa(t);
                if(ps>=pressure)break;
                double vapor=ps*family/100;
                if(vapor>=pressure)break;
                double w=0.621945*vapor/(pressure-vapor);
                if(w<=wmax)shape.Points.Add(new Point(X(t),Y(w)));
            }
            ReadingCanvas.Children.Add(shape);
        }
        if(_state is not ChartPoint state)return;
        if(state.DryBulbC<xmin||state.DryBulbC>xmax||
           state.HumidityRatioKgPerKg<wmin||state.HumidityRatioKgPerKg>wmax)
        {
            Text("El estado actual queda fuera de la escala de este esquema; consulte la carta principal.",
                85,top+12,Brushes.DarkRed);
            return;
        }
        double px=X(state.DryBulbC),py=Y(state.HumidityRatioKgPerKg);
        if(topic.Id is "BASE-01" or "BASE-02" or "BASE-11")
        {
            Line(px,py,px,height-bottom,Brushes.Firebrick,2,true);
            Line(l,py,px,py,Brushes.Teal,2,true);
        }
        if(topic.Id=="BASE-06"||topic.Id=="BASE-03")
        {
            if(double.IsFinite(state.DewPointC))
                Line(X(state.DewPointC),py,px,py,Brushes.DarkCyan,2.4,true);
        }
        if(topic.Id=="BASE-07"||topic.Id=="BASE-05")
        {
            var curve=new Polyline{Stroke=Brushes.OrangeRed,StrokeThickness=2.4};
            double h=state.EnthalpyKJPerKgDryAir;
            for(double t=xmin;t<=xmax;t+=.25)
            {
                double w=(h-1.006*t)/(2501+1.86*t);
                double ps=SaturationVaporPressure.GetPressureKPa(t);
                if(ps>=pressure)continue;
                double ws=SaturationHumidityRatio.FromPressuresKPa(pressure,ps);
                if(w>=wmin&&w<=wmax&&w<=ws)curve.Points.Add(new Point(X(t),Y(w)));
            }
            ReadingCanvas.Children.Add(curve);
            Text("h constante (referencia)",l+205,top+13,Brushes.OrangeRed);
        }
        var dot=new Ellipse{Width=12,Height=12,Fill=Brushes.RoyalBlue,
            Stroke=Brushes.White,StrokeThickness=1.5};
        Canvas.SetLeft(dot,px-6);Canvas.SetTop(dot,py-6);
        ReadingCanvas.Children.Add(dot);
        Text("P1",Math.Min(px+10,width-40),Math.Max(py-20,top+1),Brushes.Navy);
    }
}
