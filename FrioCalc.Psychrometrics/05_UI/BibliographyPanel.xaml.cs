using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FrioCalc.Psychrometrics.Core;
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
