using System;
using System.Collections.Generic;
using System.Globalization;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using System.Windows;
using System.Windows.Controls;

namespace FrioCalc.Psychrometrics.UI;

// ------------------------------ Sección 5.3C.1 - Definición del panel ------------------------------

public partial class ChartMetricsPanel : UserControl
{

    // ------------------------------ Fin de sección 5.3C.1 ------------------------------


    // ------------------------------ Sección 5.3C.2 - Eventos públicos ------------------------------

    public event Action<bool>? DryBulbVisibilityChanged;

    public event Action<bool>? HumidityRatioVisibilityChanged;

    public event Action<bool>? SaturationCurveVisibilityChanged;

    public event Action<bool>? EnthalpyVisibilityChanged;

    public event Action<bool>? RelativeHumidityVisibilityChanged;

    public event Action<bool>? WetBulbVisibilityChanged;

    public event Action<bool>? SpecificVolumeVisibilityChanged;

    public event Action<bool>? EnthalpyDeviationVisibilityChanged;
    public event Action<bool>? SpecificEntropyVisibilityChanged;

    public event Action<bool>? SensibleHeatFactorVisibilityChanged;
    public event Action<UnitSystem>? UnitsChanged;
    public event Action<string,bool>? NumberVisibilityChanged;
    public static readonly (string Key,string Label)[] NumericLabelFamilies =
    {
        ("DryBulb","Temperatura de bulbo seco"),
        ("HumidityRatio","Razón de humedad W"),
        ("Saturation","Curva de saturación"),
        ("Enthalpy","Entalpía"),
        ("RelativeHumidity","Humedad relativa"),
        ("WetBulb","Temperatura de bulbo húmedo"),
        ("SpecificVolume","Volumen específico"),
        ("EnthalpyDeviation","Desviación de entalpía"),
        ("SpecificEntropy","Entropía específica relativa"),
        ("FCS","Factor de calor sensible")
    };
    private readonly Dictionary<string,bool> _visibleNumericLabels = new();

    public bool AreNumbersVisible(string key) =>
        !_visibleNumericLabels.TryGetValue(key,out bool enabled)||enabled;

    public void SetNumberVisibility(string key,bool visible)
    {
        _visibleNumericLabels[key]=visible;
        NumberVisibilityChanged?.Invoke(key,visible);
    }

    private void NumberLabelsButton_Click(object sender,RoutedEventArgs e)
    {
        var content=new StackPanel{Margin=new Thickness(15)};
        content.Children.Add(new TextBlock
        {
            Text="Desmarque las propiedades cuyos números desea ocultar. Las curvas permanecerán visibles.",
            TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)
        });
        foreach(var (key,label) in NumericLabelFamilies)
        {
            string targetKey=key;
            var check=new CheckBox{Content=label,IsChecked=AreNumbersVisible(key),
                Margin=new Thickness(0,0,0,11)};
            check.Checked+=(s,args)=>SetNumberVisibility(targetKey,true);
            check.Unchecked+=(s,args)=>SetNumberVisibility(targetKey,false);
            content.Children.Add(check);
        }
        var close=new Button{Content="Cerrar",Height=30,Width=110,HorizontalAlignment=HorizontalAlignment.Right};
        content.Children.Add(close);
        var dialog=new Window
        {
            Title="Números de las propiedades de la carta",Width=390,Height=480,
            MinWidth=340,MinHeight=360,WindowStartupLocation=WindowStartupLocation.CenterOwner,
            Owner=Window.GetWindow(this),
            Content=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=content}
        };
        close.Click+=(s,args)=>dialog.Close();
        dialog.ShowDialog();
    }

    public UnitSystem CurrentUnits { get; private set; } = UnitSystem.SI;
    public StateSolveMethod SelectedSolveMethod =>
        Enum.TryParse((StateMethodComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
            out StateSolveMethod method) ? method : StateSolveMethod.Automatic;
    private bool _initialized;

    // ------------------------------ Fin de sección 5.3C.2 ------------------------------


    // ------------------------------ Sección 5.3C.3 - Inicialización ------------------------------

    public ChartMetricsPanel()
    {
        InitializeComponent();

        DryBulbCheckBox.IsChecked =
            false;

        HumidityRatioCheckBox.IsChecked =
            false;

        SaturationCurveCheckBox.IsChecked =
            false;

        EnthalpyCheckBox.IsChecked =
            false;

        RelativeHumidityCheckBox.IsChecked =
            false;

        WetBulbCheckBox.IsChecked =
            false;

        SpecificVolumeCheckBox.IsChecked =
            false;

        EnthalpyDeviationCheckBox.IsChecked =
            false;
        SpecificEntropyCheckBox.IsChecked = false;
        SensibleHeatFactorCheckBox.IsChecked = false;
        _initialized = true;
    }

    // ------------------------------ Fin de sección 5.3C.3 ------------------------------


    // [5.3C.3.1] Estado inicial legible; el usuario puede usar Default/None.
    public void SetBasicView()
    {
        DryBulbCheckBox.IsChecked=true;
        HumidityRatioCheckBox.IsChecked=true;
        SaturationCurveCheckBox.IsChecked=true;
        RelativeHumidityCheckBox.IsChecked=true;
    }

    // ------------------------------ Sección 5.3C.4 - Bulbo seco ------------------------------

    private void DryBulbCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        DryBulbVisibilityChanged?.Invoke(
            DryBulbCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.4 ------------------------------


    // ------------------------------ Sección 5.3C.5 - Humedad absoluta ------------------------------

    private void HumidityRatioCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        HumidityRatioVisibilityChanged?.Invoke(
            HumidityRatioCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.5 ------------------------------


    // ------------------------------ Sección 5.3C.6 - Curva de saturación ------------------------------

    private void SaturationCurveCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        SaturationCurveVisibilityChanged?.Invoke(
            SaturationCurveCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.6 ------------------------------


    // ------------------------------ Sección 5.3C.7 - Entalpía ------------------------------

    private void EnthalpyCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        EnthalpyVisibilityChanged?.Invoke(
            EnthalpyCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.7 ------------------------------


    // ------------------------------ Sección 5.3C.7.1 - Humedad relativa ------------------------------

    private void RelativeHumidityCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        RelativeHumidityVisibilityChanged?.Invoke(
            RelativeHumidityCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.7.1 ------------------------------


    // ------------------------------ Sección 5.3C.7.2 - Temperatura de bulbo húmedo ------------------------------

    private void WetBulbCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        WetBulbVisibilityChanged?.Invoke(
            WetBulbCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.7.2 ------------------------------


    // ------------------------------ Sección 5.3C.7.3 - Volumen específico ------------------------------

    private void SpecificVolumeCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        SpecificVolumeVisibilityChanged?.Invoke(
            SpecificVolumeCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.7.3 ------------------------------


    // ------------------------------ Sección 5.3C.7.4 - Desviación de entalpía ------------------------------

    private void SpecificEntropyCheckBox_Changed(object sender,RoutedEventArgs e)
        => SpecificEntropyVisibilityChanged?.Invoke(SpecificEntropyCheckBox.IsChecked==true);

    private void EnthalpyDeviationCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        EnthalpyDeviationVisibilityChanged?.Invoke(
            EnthalpyDeviationCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.7.4 ------------------------------


    // ------------------------------ Sección 5.3C.8 - Configuración Default ------------------------------

    private void DefaultButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DryBulbCheckBox.IsChecked =
            true;

        HumidityRatioCheckBox.IsChecked =
            true;

        SaturationCurveCheckBox.IsChecked =
            true;

        EnthalpyCheckBox.IsChecked =
            true;

        RelativeHumidityCheckBox.IsChecked =
            true;

        WetBulbCheckBox.IsChecked =
            true;

        SpecificVolumeCheckBox.IsChecked =
            true;

        EnthalpyDeviationCheckBox.IsChecked =
            true;
        // Las isolíneas de entropía son una extensión opcional, no ASHRAE por defecto.
        SpecificEntropyCheckBox.IsChecked = false;
        SensibleHeatFactorCheckBox.IsChecked = true;
    }

    // ------------------------------ Fin de sección 5.3C.8 ------------------------------


    // ------------------------------ Sección 5.3C.9 - Configuración None ------------------------------

    private void NoneButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DryBulbCheckBox.IsChecked =
            false;

        HumidityRatioCheckBox.IsChecked =
            false;

        SaturationCurveCheckBox.IsChecked =
            false;

        EnthalpyCheckBox.IsChecked =
            false;

        RelativeHumidityCheckBox.IsChecked =
            false;

        WetBulbCheckBox.IsChecked =
            false;

        SpecificVolumeCheckBox.IsChecked =
            false;

        EnthalpyDeviationCheckBox.IsChecked =
            false;
        SensibleHeatFactorCheckBox.IsChecked = false;
    }

    // ------------------------------ Fin de sección 5.3C.9 ------------------------------



    // [5.3C.11] Visibilidad del factor de calor sensible
    private void SensibleHeatFactorCheckBox_Changed(object sender, RoutedEventArgs e)
        => SensibleHeatFactorVisibilityChanged?.Invoke(SensibleHeatFactorCheckBox.IsChecked == true);

    // [5.3C.12] Entrada de estados (interna siempre en SI).
    public bool TryReadStateConditions(out Dictionary<StateProperty,double> conditions, out string message)
    {
        conditions = new Dictionary<StateProperty,double>();
        message = "";
        var inputs = new (StateProperty Key, TextBox Box)[]
        {
            (StateProperty.DryBulb,DryBulbInput),
            (StateProperty.WetBulb,WetBulbInput),
            (StateProperty.DewPoint,DewPointInput),
            (StateProperty.RelativeHumidity,RelativeHumidityInput),
            (StateProperty.HumidityRatio,HumidityRatioInput),
            (StateProperty.Enthalpy,EnthalpyInput),
            (StateProperty.SpecificVolume,SpecificVolumeInput)
        };
        foreach (var (key, box) in inputs)
        {
            string raw = box.Text.Trim();
            if (raw.Length == 0) continue;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out double value)
                && !double.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                message = $"Valor inválido en {key}: {raw}"; return false;
            }
            if (!double.IsFinite(value)) { message = $"Valor no finito en {key}."; return false; }
            value = key switch
            {
                StateProperty.DryBulb or StateProperty.WetBulb or StateProperty.DewPoint =>
                    PsychrometricUnits.TemperatureToSI(value, CurrentUnits),
                StateProperty.HumidityRatio => PsychrometricUnits.HumidityToSI(value, CurrentUnits),
                StateProperty.Enthalpy => PsychrometricUnits.EnthalpyToSI(value, CurrentUnits),
                StateProperty.SpecificVolume => PsychrometricUnits.VolumeToSI(value, CurrentUnits),
                _ => value
            };
            conditions[key] = value;
        }
        return true;
    }

    public void SetUnitSystem(UnitSystem unitSystem)
    {
        int requestedIndex = unitSystem == UnitSystem.IP ? 1 : 0;
        if (UnitsComboBox.SelectedIndex != requestedIndex)
            UnitsComboBox.SelectedIndex = requestedIndex;
    }

    public void ClearStateConditions()
    {
        foreach (TextBox tb in new [] {DryBulbInput,WetBulbInput,DewPointInput,RelativeHumidityInput,
            HumidityRatioInput,EnthalpyInput,SpecificVolumeInput}) tb.Clear();
    }

    // [5.3C.13] Conversión de entradas existentes antes de cambiar unidades.
    private void UnitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        UnitSystem next = UnitsComboBox.SelectedIndex == 1 ? UnitSystem.IP : UnitSystem.SI;
        if (next == CurrentUnits) return;
        if (!TryReadStateConditions(out var conditions, out string diagnostic))
        {
            MessageBox.Show(diagnostic,"No se pueden convertir las entradas",MessageBoxButton.OK,MessageBoxImage.Warning);
            UnitsComboBox.SelectedIndex = CurrentUnits == UnitSystem.SI ? 0 : 1;
            return;
        }
        CurrentUnits = next;
        foreach (var (key, val) in conditions)
        {
            double shown = key switch
            {
                StateProperty.DryBulb or StateProperty.WetBulb or StateProperty.DewPoint =>
                    PsychrometricUnits.TemperatureFromSI(val, next),
                StateProperty.HumidityRatio => PsychrometricUnits.HumidityFromSI(val, next),
                StateProperty.Enthalpy => PsychrometricUnits.EnthalpyFromSI(val, next),
                StateProperty.SpecificVolume => PsychrometricUnits.VolumeFromSI(val, next),
                _ => val
            };
            TextBox tb = key switch
            {
                StateProperty.DryBulb => DryBulbInput,
                StateProperty.WetBulb => WetBulbInput,
                StateProperty.DewPoint => DewPointInput,
                StateProperty.RelativeHumidity => RelativeHumidityInput,
                StateProperty.HumidityRatio => HumidityRatioInput,
                StateProperty.Enthalpy => EnthalpyInput,
                _ => SpecificVolumeInput
            };
            tb.Text = shown.ToString("0.######",CultureInfo.CurrentCulture);
        }
        bool ip = next == UnitSystem.IP;
        DryBulbLabel.Text = "Temperatura de bulbo seco (" + (ip ? "°F" : "°C") + ")";
        WetBulbLabel.Text = "Temperatura de bulbo húmedo (" + (ip ? "°F" : "°C") + ")";
        DewPointLabel.Text = "Punto de rocío (" + (ip ? "°F" : "°C") + ")";
        HumidityRatioLabel.Text = "Humedad absoluta (" + (ip ? "gr/lb" : "g/kg") + " aire seco)";
        EnthalpyLabel.Text = "Entalpía (" + (ip ? "Btu/lb" : "kJ/kg") + " aire seco)";
        SpecificVolumeLabel.Text = "Volumen específico (" + (ip ? "ft³/lb" : "m³/kg") + " aire seco)";
        SpecificEntropyLabel.Text = "Entropía relativa s* ("+(ip?"Btu/(lbda·°F)":"kJ/(kgda·K)")+")";
        UnitsChanged?.Invoke(next);
    }


    // [5.3C.14] Insertar casos didácticos en SI preservando la unidad seleccionada.
    public void SetStateInputSI(StateProperty property, double canonicalValue)
    {
        double shown = property switch
        {
            StateProperty.DryBulb or StateProperty.WetBulb or StateProperty.DewPoint =>
                PsychrometricUnits.TemperatureFromSI(canonicalValue, CurrentUnits),
            StateProperty.HumidityRatio =>
                PsychrometricUnits.HumidityFromSI(canonicalValue, CurrentUnits),
            StateProperty.Enthalpy =>
                PsychrometricUnits.EnthalpyFromSI(canonicalValue, CurrentUnits),
            StateProperty.SpecificVolume =>
                PsychrometricUnits.VolumeFromSI(canonicalValue, CurrentUnits),
            _ => canonicalValue
        };
        TextBox box = property switch
        {
            StateProperty.DryBulb => DryBulbInput,
            StateProperty.WetBulb => WetBulbInput,
            StateProperty.DewPoint => DewPointInput,
            StateProperty.RelativeHumidity => RelativeHumidityInput,
            StateProperty.HumidityRatio => HumidityRatioInput,
            StateProperty.Enthalpy => EnthalpyInput,
            StateProperty.SpecificVolume => SpecificVolumeInput,
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };
        box.Text = shown.ToString("0.######", CultureInfo.CurrentCulture);
    }

    // ------------------------------ Sección 5.3C.10 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 5.3C.10 ------------------------------