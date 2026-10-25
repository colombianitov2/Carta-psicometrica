using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.UI;

// ============================================================
// [5.2C.1] INICIO - Definición del selector de presión
// ============================================================

public partial class PressureSelector : UserControl
{

    // ============================================================
    // [5.2C.1] FIN - Definición del selector de presión
    // ============================================================


    // ============================================================
    // [5.2C.2] INICIO - Estado y evento público
    // ============================================================

    private bool _updatingUi;

    public double CurrentPressureKPa { get; private set; }
        = AtmosphericPressure.StandardSeaLevelKPa;

    public event Action<double>? PressureApplied;

    public event Action<bool>? StandardChartModeChanged;

    // ============================================================
    // [5.2C.2] FIN - Estado y evento público
    // ============================================================


    // ============================================================
    // [5.2C.3] INICIO - Inicialización
    // ============================================================

    public PressureSelector()
    {
        InitializeComponent();

        PresetComboBox.ItemsSource = PressurePresetCatalog.All;
        PresetComboBox.SelectedIndex = 0;

        SetPressureText(
            AtmosphericPressure.StandardSeaLevelKPa);

        ValidateCurrentInput();
    }

    // ============================================================
    // [5.2C.3] FIN - Inicialización
    // ============================================================


    // ============================================================
    // [5.2C.4] INICIO - Selección de preset
    // ============================================================

    private void PresetComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_updatingUi)
        {
            return;
        }

        if (PresetComboBox.SelectedItem is not PressurePreset preset)
        {
            return;
        }

        if (preset.IsCustom)
        {
            PressureTextBox.Focus();
            PressureTextBox.SelectAll();
            ValidateCurrentInput();
            return;
        }

        SetPressureText(preset.PressureKPa);
        ValidateCurrentInput();
    }

    // ============================================================
    // [5.2C.4] FIN - Selección de preset
    // ============================================================


    // ============================================================
    // [5.2C.5] INICIO - Edición manual
    // ============================================================

    private void PressureTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_updatingUi)
        {
            return;
        }

        if (PresetComboBox.SelectedItem is PressurePreset preset
            && !preset.IsCustom)
        {
            _updatingUi = true;
            PresetComboBox.SelectedItem = PressurePresetCatalog.Custom;
            _updatingUi = false;
        }

        ValidateCurrentInput();
    }

    // ============================================================
    // [5.2C.5] FIN - Edición manual
    // ============================================================

    // ============================================================
    // [5.2C.5.1] INICIO - Cambio de Carta estándar
    // ============================================================

    private void StandardChartToggle_Checked(
        object sender,
        RoutedEventArgs e)
    {
        StandardChartModeChanged?.Invoke(true);
    }

    private void StandardChartToggle_Unchecked(
        object sender,
        RoutedEventArgs e)
    {
        StandardChartModeChanged?.Invoke(false);
    }

    // ============================================================
    // [5.2C.5.1] FIN - Cambio de Carta estándar
    // ============================================================


    // ============================================================
    // [5.2C.6] INICIO - Aplicar presión
    // ============================================================

    private void ApplyButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!TryReadPressure(out double pressureKPa))
        {
            ValidateCurrentInput();
            return;
        }

        string? error =
            AtmosphericPressure.GetValidationError(pressureKPa);

        if (error is not null)
        {
            ValidateCurrentInput();
            return;
        }

        CurrentPressureKPa = pressureKPa;

        PressureApplied?.Invoke(pressureKPa);
    }

    // ============================================================
    // [5.2C.6] FIN - Aplicar presión
    // ============================================================


    // ============================================================
    // [5.2C.7] INICIO - Lectura robusta del número
    // ============================================================

    private bool TryReadPressure(out double pressureKPa)
    {
        string input =
            PressureTextBox.Text.Trim();

        if (double.TryParse(
                input,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out pressureKPa))
        {
            return true;
        }

        string normalized =
            input.Replace(',', '.');

        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out pressureKPa);
    }

    // ============================================================
    // [5.2C.7] FIN - Lectura robusta del número
    // ============================================================


    // ============================================================
    // [5.2C.8] INICIO - Validación visual
    // ============================================================

    private void ValidateCurrentInput()
    {
        if (!TryReadPressure(out double pressureKPa))
        {
            ValidationTextBlock.Text =
                "Introduzca un valor numérico de presión.";

            ApplyButton.IsEnabled = false;
            return;
        }

        string? error =
            AtmosphericPressure.GetValidationError(pressureKPa);

        ValidationTextBlock.Text =
            error ?? string.Empty;

        ApplyButton.IsEnabled =
            error is null;
    }

    // ============================================================
    // [5.2C.8] FIN - Validación visual
    // ============================================================


    // ============================================================
    // [5.2C.9] INICIO - Escritura controlada en interfaz
    // ============================================================

    private void SetPressureText(double pressureKPa)
    {
        _updatingUi = true;

        PressureTextBox.Text =
            pressureKPa.ToString(
                "0.###",
                CultureInfo.CurrentCulture);

        _updatingUi = false;
    }

    // ============================================================
    // [5.2C.9] FIN - Escritura controlada en interfaz
    // ============================================================


    // ============================================================
    // [5.2C.10] INICIO - Cierre del selector
    // ============================================================

}

// ============================================================
// [5.2C.10] FIN - Cierre del selector
// ============================================================ 