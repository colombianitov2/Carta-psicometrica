using System;
using System.Windows;

namespace FrioCalc.Psychrometrics.Chart;

// ------------------------------ Sección 2.2.1 - Definición de geometría de la carta ------------------------------

public sealed class ChartGeometry
{

    // ------------------------------ Fin de sección 2.2.1 ------------------------------


    // ------------------------------ Sección 2.2.2 - Límites globales y Carta estándar ------------------------------

    public const double GlobalMinDryBulbC = -50.0;
    public const double GlobalMaxDryBulbC = 120.0;

    public const double GlobalMinHumidityRatioKgPerKg = 0.0;
    public const double GlobalMaxHumidityRatioKgPerKg = 0.100;

    // Límites visuales de la carta psicrométrica convencional de referencia.
    // No representan límites físicos generales del aire húmedo.
    public const double StandardMinDryBulbC = -10.0;
    public const double StandardMaxDryBulbC = 55.0;

    public const double StandardMinHumidityRatioKgPerKg = 0.0;
    public const double StandardMaxHumidityRatioKgPerKg = 0.033;

    public bool IsStandardChartMode { get; private set; }

    public double ActiveMinDryBulbC =>
        IsStandardChartMode
            ? StandardMinDryBulbC
            : GlobalMinDryBulbC;

    public double ActiveMaxDryBulbC =>
        IsStandardChartMode
            ? StandardMaxDryBulbC
            : GlobalMaxDryBulbC;

    public double ActiveMinHumidityRatioKgPerKg =>
        IsStandardChartMode
            ? StandardMinHumidityRatioKgPerKg
            : GlobalMinHumidityRatioKgPerKg;

    public double ActiveMaxHumidityRatioKgPerKg =>
        IsStandardChartMode
            ? StandardMaxHumidityRatioKgPerKg
            : GlobalMaxHumidityRatioKgPerKg;

    // ------------------------------ Fin de sección 2.2.2 ------------------------------


    // ------------------------------ Sección 2.2.3 - Rango visible inicial ------------------------------

    public double MinDryBulbC { get; private set; } = -10.0;

    public double MaxDryBulbC { get; private set; } = 120.0;

    public double MinHumidityRatioKgPerKg { get; private set; } = 0.0;

    public double MaxHumidityRatioKgPerKg { get; private set; } = 0.100;

    // ------------------------------ Fin de sección 2.2.3 ------------------------------


    // ------------------------------ Sección 2.2.4 - Paso de líneas ------------------------------

    public double MinorDryBulbStepC { get; set; } = 1.0;

    public double MajorDryBulbStepC { get; set; } = 5.0;

    public double MinorHumidityRatioStepKgPerKg { get; set; } = 0.001;

    public double MajorHumidityRatioStepKgPerKg { get; set; } = 0.005;

    // ------------------------------ Fin de sección 2.2.4 ------------------------------


    // ------------------------------ Sección 2.2.5 - Márgenes del área gráfica ------------------------------

    public double LeftMargin { get; set; } = 50.0;

    public double TopMargin { get; set; } = 30.0;

    public double RightMargin { get; set; } = 125.0;

    public double BottomMargin { get; set; } = 90.0;

    // ------------------------------ Fin de sección 2.2.5 ------------------------------


    // ------------------------------ Sección 2.2.6 - Obtención del rectángulo de dibujo ------------------------------

    public Rect GetPlotRect(
        double availableWidth,
        double availableHeight)
    {
        double width =
            availableWidth - LeftMargin - RightMargin;

        double height =
            availableHeight - TopMargin - BottomMargin;

        if (width <= 0.0 || height <= 0.0)
        {
            return Rect.Empty;
        }

        return new Rect(
            LeftMargin,
            TopMargin,
            width,
            height);
    }

    // ------------------------------ Fin de sección 2.2.6 ------------------------------


    // ------------------------------ Sección 2.2.7 - Conversión temperatura a coordenada X ------------------------------

    public double XFromDryBulbC(
        double temperatureC,
        Rect plotRect)
    {
        double normalized =
            (temperatureC - MinDryBulbC)
            / (MaxDryBulbC - MinDryBulbC);

        return plotRect.Left
            + (normalized * plotRect.Width);
    }

    // ------------------------------ Fin de sección 2.2.7 ------------------------------


    // ------------------------------ Sección 2.2.8 - Conversión coordenada X a temperatura ------------------------------

    public double DryBulbCFromX(
        double x,
        Rect plotRect)
    {
        double normalized =
            (x - plotRect.Left)
            / plotRect.Width;

        return MinDryBulbC
            + normalized
            * (MaxDryBulbC - MinDryBulbC);
    }

    // ------------------------------ Fin de sección 2.2.8 ------------------------------


    // ------------------------------ Sección 2.2.9 - Conversión razón de humedad a coordenada Y ------------------------------

    public double YFromHumidityRatioKgPerKg(
        double humidityRatioKgPerKg,
        Rect plotRect)
    {
        double normalized =
            (humidityRatioKgPerKg - MinHumidityRatioKgPerKg)
            / (MaxHumidityRatioKgPerKg - MinHumidityRatioKgPerKg);

        return plotRect.Bottom
            - (normalized * plotRect.Height);
    }

    // ------------------------------ Fin de sección 2.2.9 ------------------------------


    // ------------------------------ Sección 2.2.10 - Conversión coordenada Y a razón de humedad ------------------------------

    public double HumidityRatioKgPerKgFromY(
        double y,
        Rect plotRect)
    {
        double normalized =
            (plotRect.Bottom - y)
            / plotRect.Height;

        return MinHumidityRatioKgPerKg
            + normalized
            * (MaxHumidityRatioKgPerKg - MinHumidityRatioKgPerKg);
    }

    // ------------------------------ Fin de sección 2.2.10 ------------------------------


    // ------------------------------ Sección 2.2.11 - Zoom alrededor del cursor ------------------------------

    public void ZoomAt(
        Rect plotRect,
        Point anchorPoint,
        double zoomFactor)
    {
        double dryBulbSpan =
            MaxDryBulbC - MinDryBulbC;

        double humiditySpan =
            MaxHumidityRatioKgPerKg
            - MinHumidityRatioKgPerKg;

        double anchorDryBulb =
            DryBulbCFromX(
                anchorPoint.X,
                plotRect);

        double anchorHumidity =
            HumidityRatioKgPerKgFromY(
                anchorPoint.Y,
                plotRect);

        double horizontalFraction =
            (anchorDryBulb - MinDryBulbC)
            / dryBulbSpan;

        double verticalFraction =
            (anchorHumidity - MinHumidityRatioKgPerKg)
            / humiditySpan;

        double newDryBulbSpan =
            Math.Clamp(
                dryBulbSpan * zoomFactor,
                2.0,
                GlobalMaxDryBulbC - GlobalMinDryBulbC);

        double newHumiditySpan =
            Math.Clamp(
                humiditySpan * zoomFactor,
                0.005,
                GlobalMaxHumidityRatioKgPerKg
                    - GlobalMinHumidityRatioKgPerKg);

        double newMinDryBulb =
            anchorDryBulb
            - horizontalFraction * newDryBulbSpan;

        double newMaxDryBulb =
            newMinDryBulb + newDryBulbSpan;

        double newMinHumidity =
            anchorHumidity
            - verticalFraction * newHumiditySpan;

        double newMaxHumidity =
            newMinHumidity + newHumiditySpan;

        ClampDryBulbRange(
            ref newMinDryBulb,
            ref newMaxDryBulb);

        ClampHumidityRange(
            ref newMinHumidity,
            ref newMaxHumidity);

        MinDryBulbC = newMinDryBulb;
        MaxDryBulbC = newMaxDryBulb;

        MinHumidityRatioKgPerKg = newMinHumidity;
        MaxHumidityRatioKgPerKg = newMaxHumidity;
    }

    // ------------------------------ Fin de sección 2.2.11 ------------------------------


    // ------------------------------ Sección 2.2.12 - Desplazamiento manual ------------------------------

    public void PanBy(
        double dryBulbDeltaC,
        double humidityRatioDeltaKgPerKg)
    {
        double newMinDryBulb =
            MinDryBulbC + dryBulbDeltaC;

        double newMaxDryBulb =
            MaxDryBulbC + dryBulbDeltaC;

        double newMinHumidity =
            MinHumidityRatioKgPerKg
            + humidityRatioDeltaKgPerKg;

        double newMaxHumidity =
            MaxHumidityRatioKgPerKg
            + humidityRatioDeltaKgPerKg;

        ClampDryBulbRange(
            ref newMinDryBulb,
            ref newMaxDryBulb);

        ClampHumidityRange(
            ref newMinHumidity,
            ref newMaxHumidity);

        MinDryBulbC = newMinDryBulb;
        MaxDryBulbC = newMaxDryBulb;

        MinHumidityRatioKgPerKg = newMinHumidity;
        MaxHumidityRatioKgPerKg = newMaxHumidity;
    }

    // ------------------------------ Fin de sección 2.2.12 ------------------------------


    // ------------------------------ Sección 2.2.13 - Posición mediante barra horizontal ------------------------------

    public void SetHorizontalScrollPosition(
        double minimumDryBulbC)
    {
        double span =
            MaxDryBulbC - MinDryBulbC;

        double availableSpan =
            ActiveMaxDryBulbC
            - ActiveMinDryBulbC;

        if (span >= availableSpan)
        {
            MinDryBulbC =
                ActiveMinDryBulbC;

            MaxDryBulbC =
                ActiveMaxDryBulbC;

            return;
        }

        double maximumAllowedMinimum =
            ActiveMaxDryBulbC - span;

        double newMinimum =
            Math.Clamp(
                minimumDryBulbC,
                ActiveMinDryBulbC,
                maximumAllowedMinimum);

        MinDryBulbC =
            newMinimum;

        MaxDryBulbC =
            newMinimum + span;
    }

    // ------------------------------ Fin de sección 2.2.13 ------------------------------


    // ------------------------------ Sección 2.2.14 - Posición mediante barra vertical ------------------------------

    public void SetVerticalScrollPosition(
        double scrollPosition)
    {
        double span =
            MaxHumidityRatioKgPerKg
            - MinHumidityRatioKgPerKg;

        double totalSpan =
            ActiveMaxHumidityRatioKgPerKg
            - ActiveMinHumidityRatioKgPerKg;

        if (span >= totalSpan)
        {
            MinHumidityRatioKgPerKg =
                ActiveMinHumidityRatioKgPerKg;

            MaxHumidityRatioKgPerKg =
                ActiveMaxHumidityRatioKgPerKg;

            return;
        }

        double maximumScroll =
            Math.Max(
                0.0,
                totalSpan - span);

        double value =
            Math.Clamp(
                scrollPosition,
                0.0,
                maximumScroll);

        double newMaximum =
            ActiveMaxHumidityRatioKgPerKg
            - value;

        double newMinimum =
            newMaximum - span;

        MinHumidityRatioKgPerKg =
            newMinimum;

        MaxHumidityRatioKgPerKg =
            newMaximum;
    }

    // ------------------------------ Fin de sección 2.2.14 ------------------------------


    // ------------------------------ Sección 2.2.15 - Modo Carta estándar y reinicio del zoom ------------------------------

    public void SetStandardChartMode(
        bool isEnabled)
    {
        IsStandardChartMode =
            isEnabled;

        ResetVisibleRanges();
    }

    public void ResetVisibleRanges()
    {
        if (IsStandardChartMode)
        {
            MinDryBulbC =
                StandardMinDryBulbC;

            MaxDryBulbC =
                StandardMaxDryBulbC;

            MinHumidityRatioKgPerKg =
                StandardMinHumidityRatioKgPerKg;

            MaxHumidityRatioKgPerKg =
                StandardMaxHumidityRatioKgPerKg;

            return;
        }

        MinDryBulbC = -10.0;
        MaxDryBulbC = 120.0;

        MinHumidityRatioKgPerKg = 0.0;
        MaxHumidityRatioKgPerKg = 0.100;
    }

    // ------------------------------ Fin de sección 2.2.15 ------------------------------


    // ------------------------------ Sección 2.2.16 - Ajuste de límites activos ------------------------------

    private void ClampDryBulbRange(
        ref double minimum,
        ref double maximum)
    {
        double allowedMinimum =
            ActiveMinDryBulbC;

        double allowedMaximum =
            ActiveMaxDryBulbC;

        double allowedSpan =
            allowedMaximum
            - allowedMinimum;

        double span =
            maximum - minimum;

        if (span >= allowedSpan)
        {
            minimum =
                allowedMinimum;

            maximum =
                allowedMaximum;

            return;
        }

        if (minimum < allowedMinimum)
        {
            minimum =
                allowedMinimum;

            maximum =
                minimum + span;
        }

        if (maximum > allowedMaximum)
        {
            maximum =
                allowedMaximum;

            minimum =
                maximum - span;
        }
    }

    private void ClampHumidityRange(
        ref double minimum,
        ref double maximum)
    {
        double allowedMinimum =
            ActiveMinHumidityRatioKgPerKg;

        double allowedMaximum =
            ActiveMaxHumidityRatioKgPerKg;

        double allowedSpan =
            allowedMaximum
            - allowedMinimum;

        double span =
            maximum - minimum;

        if (span >= allowedSpan)
        {
            minimum =
                allowedMinimum;

            maximum =
                allowedMaximum;

            return;
        }

        if (minimum < allowedMinimum)
        {
            minimum =
                allowedMinimum;

            maximum =
                minimum + span;
        }

        if (maximum > allowedMaximum)
        {
            maximum =
                allowedMaximum;

            minimum =
                maximum - span;
        }
    }

    // ------------------------------ Fin de sección 2.2.16 ------------------------------


    // ------------------------------ Sección 2.2.17 - Cierre del módulo ------------------------------

}

// ------------------------------ Fin de sección 2.2.17 ------------------------------