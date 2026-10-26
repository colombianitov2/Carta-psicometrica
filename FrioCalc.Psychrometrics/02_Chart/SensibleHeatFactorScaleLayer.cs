using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.11.1 - Escala exterior de Factor de Calor Sensible ------------------------------

public static class SensibleHeatFactorScaleLayer
{

    // ------------------------------ Fin de sección 2.11.1 ------------------------------


    // ------------------------------ Sección 2.11.2 - Valores de la escala FCS ------------------------------

    private static readonly double[] ScaleValues =
    {
        0.40,
        0.45,
        0.50,
        0.55,
        0.60,
        0.65,
        0.70,
        0.75,
        0.80,
        0.85,
        0.90,
        0.95,
        1.00
    };

    // ------------------------------ Fin de sección 2.11.2 ------------------------------


    // ------------------------------ Sección 2.11.3 - Dibujado principal ------------------------------

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

        Point topPoint =
            new(
                plotRect.Right + 58.0,
                plotRect.Top + 6.0);

        Point bottomPoint =
            new(
                plotRect.Right + 38.0,
                plotRect.Bottom - 6.0);

        Line scaleLine =
            new()
            {
                X1 = topPoint.X,
                Y1 = topPoint.Y,
                X2 = bottomPoint.X,
                Y2 = bottomPoint.Y,
                Stroke = Brushes.DimGray,
                StrokeThickness = 1.0
            };

        canvas.Children.Add(
            scaleLine);

        foreach (double value
                 in ScaleValues)
        {
            DrawTickAndLabel(
                canvas,
                topPoint,
                bottomPoint,
                value);
        }

        DrawTitle(
            canvas,
            plotRect);
    }

    // ------------------------------ Fin de sección 2.11.3 ------------------------------


    // ------------------------------ Sección 2.11.4 - Marcas y valores ------------------------------

    private static void DrawTickAndLabel(
        Canvas canvas,
        Point topPoint,
        Point bottomPoint,
        double sensibleHeatFactor)
    {
        double fraction =
            (sensibleHeatFactor - 0.40)
            / (1.00 - 0.40);

        double x =
            topPoint.X
            + (bottomPoint.X - topPoint.X)
            * fraction;

        double y =
            topPoint.Y
            + (bottomPoint.Y - topPoint.Y)
            * fraction;

        Line tick =
            new()
            {
                X1 = x - 4.0,
                Y1 = y,
                X2 = x + 5.0,
                Y2 = y,
                Stroke = Brushes.DimGray,
                StrokeThickness = 0.8
            };

        canvas.Children.Add(
            tick);

        TextBlock label =
            new()
            {
                Text =
                    sensibleHeatFactor.ToString(
                        "0.00",
                        CultureInfo.CurrentCulture),

                FontSize =
                    8.8,

                Foreground =
                    Brushes.DimGray,

                Background =
                    Brushes.White
            };

        canvas.Children.Add(
            label);

        label.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Canvas.SetLeft(
            label,
            x + 7.0);

        Canvas.SetTop(
            label,
            y
            - label.DesiredSize.Height / 2.0);
    }

    // ------------------------------ Fin de sección 2.11.4 ------------------------------


    // ------------------------------ Sección 2.11.5 - Título exterior ------------------------------

    private static void DrawTitle(
        Canvas canvas,
        Rect plotRect)
    {
        TextBlock title =
            new()
            {
                Text =
                    "Factor de Calor Sensible (FCS)",

                FontSize =
                    9.0,

                FontWeight =
                    FontWeights.SemiBold,

                Foreground =
                    Brushes.DimGray,

                LayoutTransform =
                    new RotateTransform(
                        -90.0)
            };

        canvas.Children.Add(
            title);

        title.Measure(
            new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

        Canvas.SetLeft(
            title,
            plotRect.Right + 78.0);

        Canvas.SetTop(
            title,
            plotRect.Top
            + (
                plotRect.Height
                - title.DesiredSize.Height)
            / 2.0);
    }

    // ------------------------------ Fin de sección 2.11.5 ------------------------------


    // ------------------------------ Sección 2.11.6 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 2.11.6 ------------------------------
