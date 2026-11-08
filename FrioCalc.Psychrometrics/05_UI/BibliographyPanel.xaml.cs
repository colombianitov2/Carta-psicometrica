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
    public BibliographyPanel()
    {
        InitializeComponent();
        EntriesGrid.ItemsSource=BibliographyCatalog.Entries;
    }

    private void EntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntryDetailsText == null || EntriesGrid.SelectedItem is not BibliographyEntry item) return;
        string? formulaKey=item.Id switch
        {
            "B04"=>"Tdp","B05"=>"RH","B06"=>"W","B07"=>"Ws",
            "B08"=>"h","B09"=>"v","B11"=>"SHR","B12"=>"BF",
            "B15"=>"Mdot","B18"=>"MixW",_=>null
        };
        var formula=formulaKey is null?null:AshraeStudyCatalog.FindEquation(formulaKey);
        EntryEquationVisual.Content=formula is null?null:
            AshraeStudyPanel.RenderMathematicalFraction(formula);
        EntryEquationProvenance.Text=formula is null?
            "No se dispone de una fracción equivalente para este registro; consulte su definición y método.": 
            "Fundamento: "+formula.Origin+"\nDeducción: "+formula.Derivation;
        EntryDetailsText.Text=$"ID {item.Id} — {item.Variable} ({item.Symbol})\n" +
            $"Definición: {item.Definition}\nObtención: {item.ObtainingMethod}\n" +
            $"Método en código: {item.MethodId}\nReferencia: {item.Source} / {item.Chapter}\n" +
            $"Página: {item.Page}. Estado documental: {item.Verification}\nURL: {item.Url}";
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
        var dialog=new SaveFileDialog {Title="Guardar registro bibliográfico de FrioCalc",
            Filter="CSV UTF-8|*.csv", FileName="FrioCalc_Bibliografia_Catalogo.csv"};
        if(dialog.ShowDialog()!=true) return;
        File.WriteAllText(dialog.FileName,BibliographyCatalog.ExportCsv(),new UTF8Encoding(true));
    }
}
