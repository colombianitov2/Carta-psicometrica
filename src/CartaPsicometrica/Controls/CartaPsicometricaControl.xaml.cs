using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CartaPsicometrica.Models;
using CartaPsicometrica.Services;

namespace CartaPsicometrica.Controls;

public partial class CartaPsicometricaControl : UserControl
{
    public static readonly DependencyProperty EstadoAProperty =
        DependencyProperty.Register(
            nameof(EstadoA),
            typeof(EstadoPsicometrico),
            typeof(CartaPsicometricaControl),
            new PropertyMetadata(null, OnChartPropertyChanged));

    public static readonly DependencyProperty EstadoBProperty =
        DependencyProperty.Register(
            nameof(EstadoB),
            typeof(EstadoPsicometrico),
            typeof(CartaPsicometricaControl),
            new PropertyMetadata(null, OnChartPropertyChanged));

    public static readonly DependencyProperty ProcesoProperty =
        DependencyProperty.Register(
            nameof(Proceso),
            typeof(ProcesoPsicometrico),
            typeof(CartaPsicometricaControl),
            new PropertyMetadata(null, OnChartPropertyChanged));

    public static readonly DependencyProperty PresionPaProperty =
        DependencyProperty.Register(
            nameof(PresionPa),
            typeof(double),
            typeof(CartaPsicometricaControl),
            new PropertyMetadata(101325.0, OnChartPropertyChanged));

    private readonly CartaPsicometricaRenderer renderer = new();

    public CartaPsicometricaControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Redibujar();
    }

    public EstadoPsicometrico? EstadoA
    {
        get => (EstadoPsicometrico?)GetValue(EstadoAProperty);
        set => SetValue(EstadoAProperty, value);
    }

    public EstadoPsicometrico? EstadoB
    {
        get => (EstadoPsicometrico?)GetValue(EstadoBProperty);
        set => SetValue(EstadoBProperty, value);
    }

    public ProcesoPsicometrico? Proceso
    {
        get => (ProcesoPsicometrico?)GetValue(ProcesoProperty);
        set => SetValue(ProcesoProperty, value);
    }

    public double PresionPa
    {
        get => (double)GetValue(PresionPaProperty);
        set => SetValue(PresionPaProperty, value);
    }

    private static void OnChartPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is CartaPsicometricaControl control && control.IsLoaded)
            control.Redibujar();
    }

    private void Redibujar()
    {
        renderer.DibujarBase(ChartCanvas, PresionPa);

        if (Proceso != null)
            renderer.DibujarProceso(ChartCanvas, Proceso);

        if (EstadoA != null)
            renderer.DibujarPunto(ChartCanvas, EstadoA, new SolidColorBrush(Color.FromRgb(15, 118, 110)));

        if (EstadoB != null)
            renderer.DibujarPunto(ChartCanvas, EstadoB, new SolidColorBrush(Color.FromRgb(217, 119, 6)));
    }
}
