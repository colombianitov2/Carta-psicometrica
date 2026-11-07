using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FrioCalc.Psychrometrics.Points;

namespace FrioCalc.Psychrometrics.UI;

// ------------------------------ Sección 5.4C.1 - Definición del panel de propiedades ------------------------------

public partial class PointPropertiesPanel : UserControl
{

    private readonly List<int> _selection = new();
    private IReadOnlyList<ChartPoint> _latestPoints = Array.Empty<ChartPoint>();
    public event Action? AddPointRequested;
    public event Action? OpenProcessesRequested;
    public event Action<int>? DeletePointRequested;
    public event Action<IReadOnlyList<int>>? SelectionChanged;
    public IReadOnlyList<int> SelectedPointIds => _selection.AsReadOnly();

    // ------------------------------ Fin de sección 5.4C.1 ------------------------------


    // ------------------------------ Sección 5.4C.2 - Inicialización ------------------------------

    public PointPropertiesPanel()
    {
        InitializeComponent();

        SetPoints(
            Array.Empty<ChartPoint>());
    }

    // ------------------------------ Fin de sección 5.4C.2 ------------------------------


    // ------------------------------ Sección 5.4C.3 - Actualización de puntos ------------------------------

    public void SetPoints(
        IReadOnlyList<ChartPoint> points)
    {
        _latestPoints = points;
        _selection.RemoveAll(id => !System.Linq.Enumerable.Any(points, p => p.Id == id));
        PointsWrapPanel.Children.Clear();

        EmptyStateTextBlock.Visibility =
            points.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        foreach (ChartPoint point
                 in points)
        {
            PointsWrapPanel.Children.Add(
                CreatePointCard(
                    point));
        }
    }

    // ------------------------------ Fin de sección 5.4C.3 ------------------------------


    // ------------------------------ Sección 5.4C.4 - Tarjeta de propiedades ------------------------------

    private Border CreatePointCard(
        ChartPoint point)
    {
        Brush pointBrush =
            CreateBrush(
                point.ColorHex);

        StackPanel content =
            new();

        StackPanel header =
            new()
            {
                Orientation =
                    Orientation.Horizontal,

                Margin =
                    new Thickness(
                        0,
                        0,
                        0,
                        7)
            };

        Ellipse colorMarker =
            new()
            {
                Width = 11.0,
                Height = 11.0,
                Fill = pointBrush,
                Margin = new Thickness(
                    0,
                    0,
                    7,
                    0),
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        TextBlock name =
            new()
            {
                Text = point.Name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13.5,
                Foreground = pointBrush,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        header.Children.Add(
            colorMarker);

        header.Children.Add(
            name);

        content.Children.Add(
            header);

        var choose = new CheckBox
        {
            Content = "Incluir en proceso",
            IsChecked = _selection.Contains(point.Id),
            Margin = new Thickness(0,0,0,5),
            ToolTip = "Se utilizará el orden en que seleccione cada punto."
        };
        choose.Checked += (_, _) =>
        {
            if (!_selection.Contains(point.Id)) _selection.Add(point.Id);
            SelectionChanged?.Invoke(SelectedPointIds);
        };
        choose.Unchecked += (_, _) =>
        {
            _selection.Remove(point.Id);
            SelectionChanged?.Invoke(SelectedPointIds);
        };
        content.Children.Add(choose);
        var erase = new Button
        {
            Content = "Eliminar este punto",
            Padding = new Thickness(5,2,5,2),
            Margin = new Thickness(0,0,0,6),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        erase.Click += (_, _) => DeletePointRequested?.Invoke(point.Id);
        content.Children.Add(erase);

        AddProperty(
            content,
            "Bulbo seco",
            FormatValue(
                point.DryBulbC,
                "0.00",
                "°C"));

        AddProperty(
            content,
            "Humedad absoluta",
            FormatValue(
                point.HumidityRatioKgPerKg * 1000.0,
                "0.00",
                "g/kg aire seco"));

        AddProperty(
            content,
            "Humedad relativa",
            FormatValue(
                point.RelativeHumidityPercent,
                "0.0",
                "%"));

        AddProperty(
            content,
            "Bulbo húmedo",
            FormatValue(
                point.WetBulbC,
                "0.00",
                "°C"));

        AddProperty(
            content,
            "Punto de rocío",
            FormatValue(
                point.DewPointC,
                "0.00",
                "°C"));

        AddProperty(
            content,
            "Entalpía",
            FormatValue(
                point.EnthalpyKJPerKgDryAir,
                "0.00",
                "kJ/kg aire seco"));

        AddProperty(
            content,
            "Volumen específico",
            FormatValue(
                point.SpecificVolumeM3PerKgDryAir,
                "0.000",
                "m³/kg aire seco"));

        AddProperty(
            content,
            "Presión",
            FormatValue(
                point.PressureKPa,
                "0.###",
                "kPa abs"));

        return
            new Border
            {
                Width = 285.0,
                Margin = new Thickness(
                    0,
                    0,
                    10,
                    10),
                Padding = new Thickness(
                    10),
                BorderBrush = pointBrush,
                BorderThickness =
                    new Thickness(
                        2.0,
                        1.0,
                        1.0,
                        1.0),
                CornerRadius =
                    new CornerRadius(
                        5.0),
                Background =
                    Brushes.White,
                Child =
                    content
            };
    }

    // ------------------------------ Fin de sección 5.4C.4 ------------------------------


    // ------------------------------ Sección 5.4C.5 - Filas y formato ------------------------------

    private static void AddProperty(
        Panel panel,
        string propertyName,
        string value)
    {
        Grid row =
            new()
            {
                Margin =
                    new Thickness(
                        0,
                        1,
                        0,
                        1)
            };

        row.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        132.0)
            });

        row.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        1.0,
                        GridUnitType.Star)
            });

        TextBlock name =
            new()
            {
                Text =
                    propertyName + ":",
                Foreground =
                    Brushes.DimGray,
                FontSize =
                    11.5
            };

        TextBlock valueText =
            new()
            {
                Text =
                    value,
                FontSize =
                    11.5,
                FontWeight =
                    FontWeights.SemiBold,
                TextWrapping =
                    TextWrapping.Wrap
            };

        Grid.SetColumn(
            name,
            0);

        Grid.SetColumn(
            valueText,
            1);

        row.Children.Add(
            name);

        row.Children.Add(
            valueText);

        panel.Children.Add(
            row);
    }

    private static string FormatValue(
        double value,
        string format,
        string unit)
    {
        if (!double.IsFinite(value))
        {
            return "—";
        }

        return
            value.ToString(
                format,
                CultureInfo.CurrentCulture)
            + " "
            + unit;
    }

    private static Brush CreateBrush(
        string colorHex)
    {
        try
        {
            return
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        colorHex));
        }
        catch
        {
            return Brushes.Red;
        }
    }

    // ------------------------------ Fin de sección 5.4C.5 ------------------------------



    // [5.4C.7] Acciones externas y selección ordenada
    private void AddPoint_Click(object sender, RoutedEventArgs e)
        => AddPointRequested?.Invoke();

    private void OpenProcesses_Click(object sender, RoutedEventArgs e)
    {
        if (_selection.Count < 2)
        {
            MessageBox.Show("Seleccione al menos dos puntos para crear un proceso.",
                "Procesos psicrométricos",MessageBoxButton.OK,MessageBoxImage.Information);
            return;
        }
        OpenProcessesRequested?.Invoke();
    }

    public void ClearSelection()
    {
        _selection.Clear();
        SetPoints(_latestPoints);
        SelectionChanged?.Invoke(SelectedPointIds);
    }

    // ------------------------------ Sección 5.4C.6 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 5.4C.6 ------------------------------
