using System;
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
    }

    // ------------------------------ Fin de sección 5.3C.3 ------------------------------


    // ------------------------------ Sección 5.3C.4 - Bulbo seco ------------------------------

    private void DryBulbCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        DryBulbVisibilityChanged?.Invoke(
            DryBulbCheckBox.IsChecked == true);
    }

    // ------------------------------ Fin de sección 5.3C.4 ------------------------------


    // ------------------------------ Sección 5.3C.5 - Razón de humedad ------------------------------

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
    }

    // ------------------------------ Fin de sección 5.3C.9 ------------------------------


    // ------------------------------ Sección 5.3C.10 - Cierre ------------------------------

}

// ------------------------------ Fin de sección 5.3C.10 ------------------------------