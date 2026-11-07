using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.Chart;

// [2.14] Guía de SHR de enfriamiento. No identifica un estado de impulsión único.
public static class SensibleHeatFactorGuideLayer
{
    public static void Draw(Canvas canvas,ChartGeometry geometry,
        ChartPoint origin,double sensibleHeatFactor,double pressureKPa)
    {
        if (!(sensibleHeatFactor>0 && sensibleHeatFactor<=1) ||
            Math.Abs(origin.PressureKPa-pressureKPa)>1e-6) return;
        Rect plot=geometry.GetPlotRect(canvas.ActualWidth,canvas.ActualHeight);
        if(plot.IsEmpty) return;
        double cs=MoistAirEnthalpy.DryAirSpecificHeatKJPerKgK+
            origin.HumidityRatioKgPerKg*MoistAirEnthalpy.WaterVaporSpecificHeatKJPerKgK;
        var stroke=new SolidColorBrush(Color.FromRgb(109,40,217));
        var path=new Polyline
        {
            Stroke=stroke, StrokeThickness=2.5,
            StrokeDashArray=new DoubleCollection{6,3},
            Clip=new RectangleGeometry(plot),
            IsHitTestVisible=false
        };
        for(double t=origin.DryBulbC;t>=Math.Max(-50,origin.DryBulbC-30);t-=.25)
        {
            double dT=t-origin.DryBulbC;
            double denominator=2501+1.86*t;
            if(denominator<=0) break;
            // SHR=Δh_sensible/Δh; Δh=cs·ΔT+(2501+1,86·T2)ΔW.
            double deltaW=cs*dT*(1/sensibleHeatFactor-1)/denominator;
            double w=origin.HumidityRatioKgPerKg+deltaW;
            if (w<0) break;
            double satPressure=SaturationVaporPressure.GetPressureKPa(t);
            if(satPressure>=pressureKPa) break;
            double saturatedW=SaturationHumidityRatio.FromPressuresKPa(pressureKPa,satPressure);
            if(w>saturatedW+1e-8) break;
            path.Points.Add(new Point(geometry.XFromDryBulbC(t,plot),
                geometry.YFromHumidityRatioKgPerKg(w,plot)));
        }
        if(path.Points.Count < 2) return;
        canvas.Children.Add(path);
        var label=new TextBlock
        {
            Text=$"Guía FCS={sensibleHeatFactor:0.##} desde {origin.Name} (no fija impulsión)",
            Foreground=stroke,Background=Brushes.White,FontSize=10,TextWrapping=TextWrapping.NoWrap,
            IsHitTestVisible=false
        };
        Canvas.SetLeft(label,plot.Left+8);
        Canvas.SetTop(label,plot.Top+5);
        canvas.Children.Add(label);
    }
}
