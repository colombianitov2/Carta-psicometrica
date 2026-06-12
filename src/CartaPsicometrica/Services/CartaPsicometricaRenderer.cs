using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CartaPsicometrica.Models;

namespace CartaPsicometrica.Services;

public sealed class CartaPsicometricaRenderer
{
    public const double CanvasWidth = 1680;
    public const double CanvasHeight = 900;

    private const double X0 = 135;
    private const double Y0 = 785;
    private const double ChartWidth = 1165;
    private const double ChartHeight = 625;
    private const double TMin = -10.0;
    private const double TMax = 55.0;
    private const double WMin = 0.0;
    private const double WMax = 0.032;

    private readonly PsicometriaService psicometria = new();

    public void DibujarBase(Canvas canvas, double presionPa)
    {
        canvas.Children.Clear();
        canvas.Width = CanvasWidth;
        canvas.Height = CanvasHeight;
        canvas.Background = Brushes.White;

        DibujarGrilla(canvas);
        DibujarCurvasHumedadRelativa(canvas, presionPa);
        DibujarLineasEntalpia(canvas, presionPa);
        DibujarLineasBulboHumedo(canvas, presionPa);
        DibujarLineasVolumenEspecifico(canvas, presionPa);
        DibujarMarco(canvas);
        DibujarEscalaFactorCalorSensible(canvas);
        DibujarEtiquetas(canvas, presionPa);
    }

    public void DibujarPunto(Canvas canvas, EstadoPsicometrico estado, Brush color)
    {
        Point punto = ConvertirAPuntoCanvas(estado);

        if (!PuntoDentroDeCarta(punto))
            return;

        Line vertical = new()
        {
            X1 = punto.X,
            Y1 = punto.Y,
            X2 = punto.X,
            Y2 = Y0,
            Stroke = color,
            StrokeThickness = 1.2,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            Opacity = 0.72
        };

        Line horizontal = new()
        {
            X1 = X0,
            Y1 = punto.Y,
            X2 = punto.X,
            Y2 = punto.Y,
            Stroke = color,
            StrokeThickness = 1.2,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            Opacity = 0.72
        };

        Ellipse marker = new()
        {
            Width = 14,
            Height = 14,
            Fill = Brushes.White,
            Stroke = color,
            StrokeThickness = 3
        };

        canvas.Children.Add(vertical);
        canvas.Children.Add(horizontal);

        Canvas.SetLeft(marker, punto.X - 7);
        Canvas.SetTop(marker, punto.Y - 7);
        canvas.Children.Add(marker);

        AgregarTexto(canvas, estado.Nombre, punto.X + 10, punto.Y - 25, 15, color);
    }

    public void DibujarProceso(Canvas canvas, ProcesoPsicometrico proceso)
    {
        Point a = ConvertirAPuntoCanvas(proceso.PuntoA);
        Point b = ConvertirAPuntoCanvas(proceso.PuntoB);

        if (!PuntoDentroDeCarta(a) || !PuntoDentroDeCarta(b))
            return;

        Brush colorProceso = new SolidColorBrush(Color.FromRgb(185, 28, 28));

        Line line = new()
        {
            X1 = a.X,
            Y1 = a.Y,
            X2 = b.X,
            Y2 = b.Y,
            Stroke = colorProceso,
            StrokeThickness = 3.0,
            StrokeDashArray = new DoubleCollection { 7, 4 }
        };

        canvas.Children.Add(line);

        string fcs = double.IsNaN(proceso.FactorCalorSensible)
            ? "FCS N/D"
            : $"FCS = {proceso.FactorCalorSensible:0.###}";

        AgregarTexto(canvas, fcs, (a.X + b.X) / 2.0 + 8, (a.Y + b.Y) / 2.0 - 24, 14, colorProceso);

        if (!double.IsNaN(proceso.FactorCalorSensible))
            DibujarPunteroFcs(canvas, proceso.FactorCalorSensible);
    }

    public bool EstadoDentroDeCarta(EstadoPsicometrico estado)
    {
        return estado.TemperaturaBulboSecoC >= TMin &&
               estado.TemperaturaBulboSecoC <= TMax &&
               estado.RazonHumedadKgKg >= WMin &&
               estado.RazonHumedadKgKg <= WMax;
    }

    private static void DibujarMarco(Canvas canvas)
    {
        Rectangle frame = new()
        {
            Width = ChartWidth,
            Height = ChartHeight,
            Stroke = Brushes.Black,
            StrokeThickness = 1.35,
            Fill = Brushes.Transparent
        };

        Canvas.SetLeft(frame, X0);
        Canvas.SetTop(frame, Y0 - ChartHeight);
        canvas.Children.Add(frame);
    }

    private static void DibujarGrilla(Canvas canvas)
    {
        for (double t = TMin; t <= TMax + 0.001; t += 5)
        {
            double x = XDesdeTemperatura(t);

            Line line = new()
            {
                X1 = x,
                Y1 = Y0,
                X2 = x,
                Y2 = Y0 - ChartHeight,
                Stroke = Brushes.LightGray,
                StrokeThickness = 0.8
            };

            canvas.Children.Add(line);
            AgregarTexto(canvas, t.ToString("0", CultureInfo.InvariantCulture), x - 10, Y0 + 18, 13, Brushes.Black);
        }

        for (double wg = 0; wg <= 32.001; wg += 2)
        {
            double w = wg / 1000.0;
            double y = YDesdeRazonHumedad(w);

            Line line = new()
            {
                X1 = X0,
                Y1 = y,
                X2 = X0 + ChartWidth,
                Y2 = y,
                Stroke = Brushes.LightGray,
                StrokeThickness = 0.8
            };

            canvas.Children.Add(line);
            AgregarTexto(canvas, wg.ToString("0", CultureInfo.InvariantCulture), X0 + ChartWidth + 12, y - 8, 13, Brushes.Black);
        }
    }

    private void DibujarCurvasHumedadRelativa(Canvas canvas, double presionPa)
    {
        for (double hr = 0.1; hr <= 1.001; hr += 0.1)
        {
            Polyline curve = new()
            {
                Stroke = hr >= 0.999 ? Brushes.Black : Brushes.DimGray,
                StrokeThickness = hr >= 0.999 ? 2.4 : 0.95,
                Opacity = hr >= 0.999 ? 1.0 : 0.82
            };

            for (double t = TMin; t <= TMax; t += 0.25)
            {
                double w = psicometria.RazonHumedadDesdeHumedadRelativa(t, hr, presionPa);

                if (w < WMin || w > WMax)
                    continue;

                curve.Points.Add(new Point(XDesdeTemperatura(t), YDesdeRazonHumedad(w)));
            }

            if (curve.Points.Count > 1)
                canvas.Children.Add(curve);

            if (hr < 0.999 && curve.Points.Count > 15)
            {
                Point labelPoint = curve.Points[Math.Min(curve.Points.Count - 1, curve.Points.Count * 3 / 4)];
                AgregarTexto(canvas, $"{hr * 100:0}%", labelPoint.X + 4, labelPoint.Y - 15, 11, Brushes.DimGray);
            }
        }
    }

    private void DibujarLineasEntalpia(Canvas canvas, double presionPa)
    {
        for (double h = 0; h <= 115; h += 5)
        {
            Polyline line = new()
            {
                Stroke = Brushes.SlateGray,
                StrokeThickness = h % 10 == 0 ? 0.85 : 0.55,
                StrokeDashArray = new DoubleCollection { 8, 5 },
                Opacity = h % 10 == 0 ? 0.90 : 0.55
            };

            for (double t = TMin; t <= TMax; t += 0.25)
            {
                double w = (h - 1.006 * t) / (2501.0 + 1.86 * t);

                if (w < WMin || w > WMax)
                    continue;

                double ws = psicometria.RazonHumedadSaturada(t, presionPa);

                if (w > ws)
                    continue;

                line.Points.Add(new Point(XDesdeTemperatura(t), YDesdeRazonHumedad(w)));
            }

            if (line.Points.Count < 2)
                continue;

            canvas.Children.Add(line);

            if (h % 10 == 0)
            {
                Point labelPoint = line.Points[Math.Min(12, line.Points.Count - 1)];
                AgregarTexto(canvas, $"h={h:0}", labelPoint.X + 4, labelPoint.Y - 12, 10, Brushes.SlateGray, -28);
            }
        }
    }

    private void DibujarLineasBulboHumedo(Canvas canvas, double presionPa)
    {
        for (double tbh = -10; tbh <= 35; tbh += 5)
        {
            double ws = psicometria.RazonHumedadSaturada(tbh, presionPa);
            double h = psicometria.Entalpia(tbh, ws);

            Polyline line = new()
            {
                Stroke = Brushes.DarkSlateGray,
                StrokeThickness = 0.85,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                Opacity = 0.85
            };

            for (double t = Math.Max(tbh, TMin); t <= TMax; t += 0.25)
            {
                double w = (h - 1.006 * t) / (2501.0 + 1.86 * t);

                if (w < WMin || w > WMax)
                    continue;

                double wSat = psicometria.RazonHumedadSaturada(t, presionPa);

                if (w > wSat)
                    continue;

                line.Points.Add(new Point(XDesdeTemperatura(t), YDesdeRazonHumedad(w)));
            }

            if (line.Points.Count < 2)
                continue;

            canvas.Children.Add(line);

            Point labelPoint = line.Points[Math.Min(20, line.Points.Count - 1)];
            AgregarTexto(canvas, $"{tbh:0} °C Tbh", labelPoint.X + 4, labelPoint.Y - 14, 10, Brushes.DarkSlateGray, -25);
        }
    }

    private void DibujarLineasVolumenEspecifico(Canvas canvas, double presionPa)
    {
        for (double v = 0.70; v <= 1.05; v += 0.05)
        {
            Polyline line = new()
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 0.7,
                StrokeDashArray = new DoubleCollection { 2, 5 },
                Opacity = 0.55
            };

            for (double t = TMin; t <= TMax; t += 0.25)
            {
                double tK = t + 273.15;
                double w = ((v * presionPa) / (287.055 * tK) - 1.0) / 1.607858;

                if (w < WMin || w > WMax)
                    continue;

                double ws = psicometria.RazonHumedadSaturada(t, presionPa);

                if (w > ws)
                    continue;

                line.Points.Add(new Point(XDesdeTemperatura(t), YDesdeRazonHumedad(w)));
            }

            if (line.Points.Count > 1)
                canvas.Children.Add(line);
        }
    }

    private static void DibujarEscalaFactorCalorSensible(Canvas canvas)
    {
        double x = 1540;
        double yTop = 95;
        double yBottom = 705;

        Line axis = new()
        {
            X1 = x,
            Y1 = yTop,
            X2 = x,
            Y2 = yBottom,
            Stroke = Brushes.Black,
            StrokeThickness = 1.2
        };

        canvas.Children.Add(axis);

        AgregarTexto(canvas, "Factor de calor sensible", x - 145, yTop - 36, 13, Brushes.Black);
        AgregarTexto(canvas, "FCS", x + 66, yTop + 245, 14, Brushes.Black, 90);

        double[] values =
        {
            0.35, 0.40, 0.45, 0.50, 0.55, 0.60, 0.65,
            0.70, 0.75, 0.80, 0.85, 0.90, 0.95, 1.00
        };

        foreach (double value in values)
        {
            double y = MapFcsToY(value, yTop, yBottom);

            Line tick = new()
            {
                X1 = x,
                Y1 = y,
                X2 = x + 34,
                Y2 = y - 12,
                Stroke = Brushes.Black,
                StrokeThickness = value is 0.35 or 1.00 ? 1.1 : 0.8
            };

            canvas.Children.Add(tick);
            AgregarTexto(canvas, value.ToString("0.00", CultureInfo.InvariantCulture), x + 42, y - 20, 10, Brushes.Black);
        }
    }

    private static void DibujarPunteroFcs(Canvas canvas, double fcs)
    {
        double value = Math.Clamp(fcs, 0.35, 1.00);
        double x = 1540;
        double yTop = 95;
        double yBottom = 705;
        double y = MapFcsToY(value, yTop, yBottom);
        Brush color = new SolidColorBrush(Color.FromRgb(185, 28, 28));

        Ellipse marker = new()
        {
            Width = 12,
            Height = 12,
            Fill = color,
            Stroke = Brushes.White,
            StrokeThickness = 2
        };

        Canvas.SetLeft(marker, x - 6);
        Canvas.SetTop(marker, y - 6);
        canvas.Children.Add(marker);

        AgregarTexto(canvas, $"FCS {fcs:0.###}", x - 90, y - 10, 12, color);
    }

    private static void DibujarEtiquetas(Canvas canvas, double presionPa)
    {
        AgregarTexto(canvas, $"Carta psicrométrica SI - Presión {presionPa / 1000.0:0.###} kPa", X0, 48, 16, Brushes.Black);
        AgregarTexto(canvas, "Temperatura de bulbo seco (Tbs) [°C]", X0 + 430, Y0 + 52, 16, Brushes.Black);
        AgregarTexto(canvas, "Razón de humedad (W) [g/kg aire seco]", X0 + ChartWidth + 54, Y0 - 390, 15, Brushes.Black, 90);
        AgregarTexto(canvas, "Curvas de humedad relativa (HR) [%]", X0 + 740, Y0 - 595, 14, Brushes.DimGray);
    }

    private static double MapFcsToY(double fcs, double yTop, double yBottom)
    {
        double normalized = (fcs - 0.35) / (1.00 - 0.35);
        return yTop + normalized * (yBottom - yTop);
    }

    private static Point ConvertirAPuntoCanvas(EstadoPsicometrico estado)
    {
        return new Point(XDesdeTemperatura(estado.TemperaturaBulboSecoC), YDesdeRazonHumedad(estado.RazonHumedadKgKg));
    }

    private static bool PuntoDentroDeCarta(Point point)
    {
        return point.X >= X0 &&
               point.X <= X0 + ChartWidth &&
               point.Y >= Y0 - ChartHeight &&
               point.Y <= Y0;
    }

    private static double XDesdeTemperatura(double temperaturaC)
    {
        return X0 + (temperaturaC - TMin) / (TMax - TMin) * ChartWidth;
    }

    private static double YDesdeRazonHumedad(double razonHumedad)
    {
        return Y0 - (razonHumedad - WMin) / (WMax - WMin) * ChartHeight;
    }

    private static void AgregarTexto(Canvas canvas, string text, double x, double y, double size, Brush brush, double angle = 0)
    {
        TextBlock tb = new()
        {
            Text = text,
            FontSize = size,
            Foreground = brush,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Padding = new Thickness(2, 0, 2, 0)
        };

        if (Math.Abs(angle) > 0.01)
        {
            tb.RenderTransform = new RotateTransform(angle);
            tb.RenderTransformOrigin = new Point(0, 0);
        }

        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        canvas.Children.Add(tb);
    }
}
