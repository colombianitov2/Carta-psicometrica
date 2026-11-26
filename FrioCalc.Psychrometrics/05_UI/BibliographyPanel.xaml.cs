using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Processes;
using Microsoft.Win32;

namespace FrioCalc.Psychrometrics.UI;

// [5.6C.1] Acceso a registros documentales oficiales y exportación sin modificaciones.
public partial class BibliographyPanel : UserControl
{
    private UnitSystem _units=UnitSystem.SI;

    public void SetUnits(UnitSystem units)
    {
        _units=units;
        if(EntriesGrid.SelectedItem is BibliographyEntry)
            RenderSelectedEntry();
    }

    public BibliographyPanel()
    {
        InitializeComponent();
        EntriesGrid.ItemsSource=BibliographyCatalog.Entries;
    }

    private void EntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RenderSelectedEntry();
    }

    private void RenderSelectedEntry()
    {
        if (EntryDetailsText == null || EntriesGrid.SelectedItem is not BibliographyEntry item) return;
        string? formulaKey=item.Id switch
        {
            "B01"=>"Atmospheric","B02"=>"TK","B03"=>"Tbh",
            "B04"=>"Tdp","B05"=>"RH","B06"=>"W","B07"=>"PwsExp",
            "B08"=>"h","B09"=>"v","B10"=>"DeltaH","B11"=>"SHR","B12"=>"BF",
            "B13"=>"BFW","B14"=>"Evap","B15"=>"Mdot","B16"=>"Heat","B17"=>"Condensate",
            "B18"=>"MixW","B19"=>"Condensate","B21"=>"Entropy",_=>null
        };
        var formula=formulaKey is null?null:AshraeStudyCatalog.FindEquation(formulaKey);
        EntryEquationVisual.Content=formula is null?null:
            AshraeStudyPanel.RenderMathematicalFraction(EducationalUnits.Equation(formula,_units));
        var display=formula is null?null:EducationalUnits.Equation(formula,_units);
        AshraeStudyPanel.FormatMath(EntryEquationProvenance,display is null?
            "Consulte la definición y el método de obtención.":"Fundamento: "+display.Origin+"\nDeducción: "+display.Derivation);
        AshraeStudyPanel.FormatMath(EntryDetailsText,$"ID {item.Id} — {item.Variable} ({item.Symbol})\n" +
            $"Definición: {item.Definition}\nObtención: {item.ObtainingMethod}\n" +
            $"Método en código: {item.MethodId}\nReferencia: {item.Source} / {item.Chapter}\n" +
            $"Página: {item.Page}. Estado documental: {item.Verification}\nURL: {item.Url}");
    }

    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if(EntriesGrid.SelectedItem is not BibliographyEntry item)
        { MessageBox.Show("Seleccione primero una fuente.", "Bibliografía");return; }
        if(!Uri.TryCreate(item.Url,UriKind.Absolute,out var uri) ||
            uri.Scheme!="https")
        { MessageBox.Show("URL no disponible.", "Bibliografía");return; }
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});
    }

    private void ExportBibliography_Click(object sender, RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog {Title="Guardar registro bibliográfico de la calculadora",
            Filter="CSV UTF-8|*.csv", FileName="Calculadora_Refrigeracion_Parte1_Bibliografia.csv"};
        if(dialog.ShowDialog()!=true) return;
        File.WriteAllText(dialog.FileName,BibliographyCatalog.ExportCsv(),new UTF8Encoding(true));
    }
}
