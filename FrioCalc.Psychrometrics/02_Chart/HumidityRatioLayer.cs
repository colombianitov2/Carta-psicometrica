using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.4.1 - Definición de capa de humedad absoluta ------------------------------

public static class HumidityRatioLayer
{

    // ------------------------------ Fin de sección 2.4.1 ------------------------------


    // ------------------------------ Sección 2.4.2 - Dibujado principal ------------------------------

    public static void Draw(
        Canvas canvas,
        ChartGeometry geometry)
    {
        if (canvas.ActualWidth <= 0.0
            || canvas.ActualHeight <= 0.0)
        {
            return;
        }

        Rect plotRect =
            geometry.GetPlotRect(
                canvas.ActualWidth,
                canvas.ActualHeight);

        if (plotRect.IsEmpty)
        {
            return;
        }

        DrawHorizontalLines(
            canvas,
            geometry,
            plotRect);

        DrawAxisTitle(
            canvas,
            plotRect, geometry);
    }

    // ------------------------------ Fin de sección 2.4.2 ------------------------------


    // ------------------------------ Sección 2.4.3 - Generación de líneas horizontales ------------------------------

    private static void DrawHorizontalLines(Canvas canvas, ChartGeometry geometry, Rect plotRect)
    {
        bool ip = geometry.DisplayUnits == UnitSystem.IP;
        double min = PsychrometricUnits.HumidityFromSI(geometry.MinHumidityRatioKgPerKg,geometry.DisplayUnits);
        double max = PsychrometricUnits.HumidityFromSI(geometry.MaxHumidityRatioKgPerKg,geometry.DisplayUnits);
        double minorStep = ip ? 10.0 : geometry.MinorHumidityRatioStepKgPerKg * 1000.0;
        double majorStep = ip ? 50.0 : geometry.MajorHumidityRatioStepKgPerKg * 1000.0;
        for (double display = RoundUpToStep(min,minorStep); display <= max + 1e-9; display += minorStep)
        {
            double w = PsychrometricUnits.HumidityToSI(display,geometry.DisplayUnits);
            double y = geometry.YFromHumidityRatioKgPerKg(w,plotRect);
            bool major = IsMajorValue(display,majorStep);
            canvas.Children.Add(new Line { X1=plotRect.Left,Y1=y,X2=plotRect.Right,Y2=y,
                Stroke=major ? Brushes.Gray : Brushes.LightGray, StrokeThickness=major ? 1 : .5 });
            if (major) DrawHumidityRatioLabel(canvas,display,y,plotRect);
        }
    }

    // ------------------------------ Fin de sección 2.4.3 ------------------------------


    // ------------------------------ Sección 2.4.4 - Etiquetas numéricas ------------------------------

    private static void DrawHumidityRatioLabel(
        Canvas canvas,
        double humidityRatioKgPerKg,
        double y,
        Rect plotRect)
    {
        // El valor recibido ya está convertido a la unidad que se dibuja (g/kgda o gr/lbda).
        double gramsPerKilogram = humidityRatioKgPerKg;

        TextBlock label =
            new()
            {
                Text = gramsPerKilogram.ToString(
                    "0",
                    CultureInfo.CurrentCulture),
                FontSize = 11.0,
                Foreground = Brushes.DimGray
            };

        canvas.Children.Add(label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        double left =
            plotRect.Right + 7.0;

        double top =
            y - (label.DesiredSize.Height / 2.0);

        Canvas.SetLeft(
            label,
            left);

        Canvas.SetTop(
            label,
            top);
    }

    // ------------------------------ Fin de sección 2.4.4 ------------------------------


    // ------------------------------ Sección 2.4.5 - Título del eje vertical mostrado a la derecha ------------------------------

    private static void DrawAxisTitle(
        Canvas canvas,
        Rect plotRect,
        ChartGeometry geometry)
    {
        TextBlock title =
            new()
            {
                Text = geometry.DisplayUnits == UnitSystem.SI
                    ? "Humedad absoluta (g/kg aire seco)" : "Humedad absoluta (gr/lb aire seco)",
                FontSize = 12.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.DimGray
            };

        canvas.Children.Add(title);

        title.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        double left =
            plotRect.Right
            - title.DesiredSize.Width;

        double top =
            plotRect.Top - 24.0;

        Canvas.SetLeft(
            title,
            left);

        Canvas.SetTop(
            title,
            top);
    }

    // ------------------------------ Fin de sección 2.4.5 ------------------------------


    // ------------------------------ Sección 2.4.6 - Utilidades ------------------------------

    private static bool IsMajorValue(
        double value,
        double majorStep)
    {
        double multiple =
            value / majorStep;

        return Math.Abs(
            multiple - Math.Round(multiple))
            < 1e-8;
    }

    private static double RoundUpToStep(
        double value,
        double step)
    {
        return Math.Ceiling(value / step) * step;
    }

    // ------------------------------ Fin de sección 2.4.6 ------------------------------


    // ------------------------------ Sección 2.4.7 - Cierre del módulo ------------------------------

}

// ------------------------------ Fin de sección 2.4.7 ------------------------------