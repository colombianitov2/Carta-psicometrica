using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.3.1 - Definición de capa de bulbo seco ------------------------------

public static class DryBulbLayer
{

    // ------------------------------ Fin de sección 2.3.1 ------------------------------


    // ------------------------------ Sección 2.3.2 - Dibujado principal ------------------------------

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

        DrawVerticalLines(
            canvas,
            geometry,
            plotRect);

        DrawAxisTitle(
            canvas,
            plotRect);
    }

    // ------------------------------ Fin de sección 2.3.2 ------------------------------


    // ------------------------------ Sección 2.3.3 - Generación de líneas verticales ------------------------------

    private static void DrawVerticalLines(
        Canvas canvas,
        ChartGeometry geometry,
        Rect plotRect)
    {
        double minimum =
            geometry.MinDryBulbC;

        double maximum =
            geometry.MaxDryBulbC;

        double minorStep =
            geometry.MinorDryBulbStepC;

        double majorStep =
            geometry.MajorDryBulbStepC;

        double start =
            RoundUpToStep(
                minimum,
                minorStep);

        const double tolerance = 1e-9;

        for (
            double dryBulbC = start;
            dryBulbC <= maximum + tolerance;
            dryBulbC += minorStep)
        {
            double x =
                geometry.XFromDryBulbC(
                    dryBulbC,
                    plotRect);

            bool isMajor =
                IsMajorValue(
                    dryBulbC,
                    majorStep);

            Line line =
                new()
                {
                    X1 = x,
                    Y1 = plotRect.Top,
                    X2 = x,
                    Y2 = plotRect.Bottom,
                    Stroke = isMajor ? Brushes.Gray : Brushes.LightGray,
                    StrokeThickness = isMajor ? 1.0 : 0.5
                };

            canvas.Children.Add(line);

            if (isMajor)
            {
                DrawTemperatureLabel(
                    canvas,
                    dryBulbC,
                    x,
                    plotRect);
            }
        }
    }

    // ------------------------------ Fin de sección 2.3.3 ------------------------------


    // ------------------------------ Sección 2.3.4 - Etiquetas numéricas ------------------------------

    private static void DrawTemperatureLabel(
        Canvas canvas,
        double dryBulbC,
        double x,
        Rect plotRect)
    {
        TextBlock label =
            new()
            {
                Text = dryBulbC.ToString(
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
            x - (label.DesiredSize.Width / 2.0);

        left =
            Math.Max(
                0.0,
                Math.Min(
                    left,
                    canvas.ActualWidth - label.DesiredSize.Width));

        double top =
            plotRect.Bottom + 5.0;

        Canvas.SetLeft(
            label,
            left);

        Canvas.SetTop(
            label,
            top);
    }

    // ------------------------------ Fin de sección 2.3.4 ------------------------------


    // ------------------------------ Sección 2.3.5 - Título del eje horizontal ------------------------------

    private static void DrawAxisTitle(
        Canvas canvas,
        Rect plotRect)
    {
        TextBlock title =
            new()
            {
                Text = "Temperatura de bulbo seco (°C)",
                FontSize = 12.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            };

        canvas.Children.Add(title);

        title.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        double left =
            plotRect.Left
            + ((plotRect.Width - title.DesiredSize.Width) / 2.0);

        double top =
            plotRect.Bottom + 25.0;

        Canvas.SetLeft(
            title,
            left);

        Canvas.SetTop(
            title,
            top);
    }

    // ------------------------------ Fin de sección 2.3.5 ------------------------------


    // ------------------------------ Sección 2.3.6 - Utilidades ------------------------------

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

    // ------------------------------ Fin de sección 2.3.6 ------------------------------


    // ------------------------------ Sección 2.3.7 - Cierre del módulo ------------------------------

}

// ------------------------------ Fin de sección 2.3.7 ------------------------------