using System.Globalization;
using System.Windows.Controls;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics;

// [5.1C.22] Conversiones de entradas del editor; los datos del motor continúan en SI.
public partial class MainWindow
{
    private UnitSystem _editorUnits=UnitSystem.SI;
    private const double KgSecondToLbHour=7936.64143865559;
    private const double M3ToFt3=35.31466672149;
    private bool EditorIP=>_editorUnits==UnitSystem.IP;
    private string FlowUnit=>EditorIP?"lb/h":"kg/s";
    private string VolumeFlowUnit=>EditorIP?"ft³/h":"m³/h";
    private string TempUnit=>EditorIP?"°F":"°C";
    private string WaterUnit=>EditorIP?"gr/lb aire seco":"g/kg aire seco";
    private string WaterEnthalpyUnit=>EditorIP?"Btu/lb de vapor":"kJ/kg de vapor";

    private double FlowFromSI(double value)=>value*(EditorIP?KgSecondToLbHour:1);
    private double FlowToSI(double value)=>value/(EditorIP?KgSecondToLbHour:1);
    private double VolumeFlowFromSI(double value)=>value*(EditorIP?M3ToFt3:1);
    private double VolumeFlowToSI(double value)=>value/(EditorIP?M3ToFt3:1);
    private double TemperatureFromSI(double value)=>PsychrometricUnits.TemperatureFromSI(value,_editorUnits);
    private double TemperatureToSI(double value)=>PsychrometricUnits.TemperatureToSI(value,_editorUnits);
    // Los parámetros de agua históricos se almacenan en gramos/kg, no kg/kg.
    private double WaterFromSI(double value)=>PsychrometricUnits.HumidityFromSI(value/1000,_editorUnits);
    private double WaterToSI(double value)=>PsychrometricUnits.HumidityToSI(value,_editorUnits)*1000;
    private double WaterEnthalpyFromSI(double value)=>PsychrometricUnits.EnthalpyFromSI(value,_editorUnits);
    private double WaterEnthalpyToSI(double value)=>PsychrometricUnits.EnthalpyToSI(value,_editorUnits);
    private static string DisplayParameter(double? value,Func<double,double> conversion)=>value is double n?conversion(n).ToString("0.########",CultureInfo.CurrentCulture):"";

    private void SetProcessEditorUnits(UnitSystem units)
    {
        if(_editorUnits==units)return;
        // Convertir las entradas que ya estaban escritas sin reinterpretarlas en el sistema nuevo.
        var previous=_editorUnits;
        string ConvertValue(string raw,Func<double,UnitSystem,double> toSI,Func<double,UnitSystem,double> fromSI)=>
            TryReadFinite(raw,out double value)?fromSI(toSI(value,previous),units).ToString("0.########",CultureInfo.CurrentCulture):raw;
        double FlowIn(double v,UnitSystem u)=>v/(u==UnitSystem.IP?KgSecondToLbHour:1);
        double FlowOut(double v,UnitSystem u)=>v*(u==UnitSystem.IP?KgSecondToLbHour:1);
        double VolIn(double v,UnitSystem u)=>v/(u==UnitSystem.IP?M3ToFt3:1);
        double VolOut(double v,UnitSystem u)=>v*(u==UnitSystem.IP?M3ToFt3:1);
        double WaterIn(double v,UnitSystem u)=>PsychrometricUnits.HumidityToSI(v,u);
        double WaterOut(double v,UnitSystem u)=>PsychrometricUnits.HumidityFromSI(v,u);
        void ConvertBox(TextBox box,Func<double,UnitSystem,double> toSI,Func<double,UnitSystem,double> fromSI)=>box.Text=ConvertValue(box.Text,toSI,fromSI);
        ConvertBox(DryFlowTextBox,FlowIn,FlowOut);
        foreach(var box in new[]{CondensateTemperatureTextBox,ApparatusDewPointTextBox,DesiccantTemperatureTextBox})
            ConvertBox(box,PsychrometricUnits.TemperatureToSI,PsychrometricUnits.TemperatureFromSI);
        foreach(var box in new[]{MixFlowFirstTextBox,MixFlowSecondTextBox})ConvertBox(box,VolIn,VolOut);
        MixFlowsListTextBox.Text=string.Join(";",MixFlowsListTextBox.Text.Split(new[]{';','\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>ConvertValue(x,VolIn,VolOut)));
        foreach(var box in new[]{SteamAddedTextBox,DesiccantRemovedTextBox})ConvertBox(box,WaterIn,WaterOut);
        ConvertBox(SteamEnthalpyTextBox,PsychrometricUnits.EnthalpyToSI,PsychrometricUnits.EnthalpyFromSI);
        foreach(var draft in _segmentDrafts.Values)
        {
            draft.Flow=ConvertValue(draft.Flow,FlowIn,FlowOut);
            draft.Condensate=ConvertValue(draft.Condensate,PsychrometricUnits.TemperatureToSI,PsychrometricUnits.TemperatureFromSI);
        }
        _editorUnits=units;
        DryFlowLabel.Text=$"Caudal másico de aire seco, {FlowUnit} (opcional salvo donde se requiera)";
        CondensateTemperatureLabel.Text=$"Temperatura del condensado para balance del serpentín ({TempUnit}, opcional)";
        MixFlowsLabel.Text=$"Mezcla de N entradas: caudales {VolumeFlowUnit} separados por punto y coma (opcional)";
        MixFlowFirstLabel.Text=$"Solo para mezcla: caudal de entrada 1, {VolumeFlowUnit}";
        MixFlowSecondLabel.Text=$"Solo para mezcla: caudal de entrada 2, {VolumeFlowUnit}";
        ApparatusDewPointLabel.Text=$"ADP o punto de rocío del aparato ({TempUnit})";
        SteamAddedLabel.Text=$"Vapor añadido ({WaterUnit})";
        SteamEnthalpyLabel.Text=$"Entalpía del vapor inyectado ({WaterEnthalpyUnit})";
        DesiccantRemovedLabel.Text=$"Desecante: humedad retirada ({WaterUnit})";
        DesiccantTemperatureLabel.Text=$"Desecante: bulbo seco medido o especificado a la salida ({TempUnit})";
    }
}
