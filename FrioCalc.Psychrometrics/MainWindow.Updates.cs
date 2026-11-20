using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics;

// [5.1C.21] Panel de actualización, validación de paquetes y recuperación de trabajo.
public partial class MainWindow
{
    private ReleaseUpdateService.UpdateRelease? _availableUpdate;
    private static string UpdateRecoveryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FrioCalc","update-recovery.json");

    private void InitializeUpdates()
    {
        UpdateVersionText.Text="Versión actual: "+ReleaseUpdateService.CurrentVersion;
        Loaded+=(_,_)=>RestoreUpdateSession();
    }

    private async void CheckUpdates_Click(object sender,RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled=false;
        InstallUpdateButton.Visibility=Visibility.Collapsed;
        _availableUpdate=null;
        UpdateStatusText.Text="Consultando GitHub…";
        try
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
            _availableUpdate=await ReleaseUpdateService.CheckAsync(ReleaseUpdateService.IsInstalled,timeout.Token);
            UpdateStatusText.Text=_availableUpdate is null?"No hay una versión compatible más reciente.":$"Disponible: {_availableUpdate.Version} ({_availableUpdate.Executable.Size/1_000_000.0:0.0} MB).";
            if(_availableUpdate is not null)
            {
                if(ReleaseUpdateService.IsPublished) InstallUpdateButton.Visibility=Visibility.Visible;
                else UpdateStatusText.Text+=" Abra la versión publicada de la calculadora para instalarla automáticamente.";
            }
        }
        catch(Exception ex) { UpdateStatusText.Text="No se pudo consultar GitHub: "+ex.Message; }
        finally { CheckUpdatesButton.IsEnabled=true; }
    }

    private async void InstallUpdate_Click(object sender,RoutedEventArgs e)
    {
        if(_availableUpdate is not { } release || !ReleaseUpdateService.IsPublished)return;
        if(MessageBox.Show(this,$"Se descargará la calculadora {release.Version} desde GitHub, se verificará su SHA-256 y se cerrará el programa. Los puntos y procesos guardados se podrán recuperar al abrirlo nuevamente. Los grupos todavía sin guardar no se recuperarán. ¿Continuar?","Actualizar calculadora",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        CheckUpdatesButton.IsEnabled=false; InstallUpdateButton.IsEnabled=false;
        UpdateProgress.Value=0; UpdateProgress.Visibility=Visibility.Visible;
        try
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(15));
            UpdateStatusText.Text="Descargando y verificando el paquete…";
            bool installed=ReleaseUpdateService.IsInstalled;
            string package=await ReleaseUpdateService.DownloadAsync(release,new Progress<int>(p=>UpdateProgress.Value=p),timeout.Token);
            if(PsychrometricChart.ActivePoints.Count>0 || _savedProcesses.Count>0)
            {
                var session=new SessionV1(1,PressureSelector.CurrentPressureKPa,ChartMetricsPanel.CurrentUnits,PsychrometricChart.ActivePoints.ToList(),new(_savedProcesses));
                if(!SessionFileIntegrity.TryValidate(session.PressureKPa,session.Points,session.Processes,out string error))throw new InvalidDataException(error);
                Directory.CreateDirectory(Path.GetDirectoryName(UpdateRecoveryPath)!);
                if(File.Exists(UpdateRecoveryPath))throw new IOException("Hay una sesión anterior pendiente de recuperación. Abra nuevamente la calculadora antes de actualizar.");
                using var file=new FileStream(UpdateRecoveryPath,FileMode.CreateNew,FileAccess.Write);
                JsonSerializer.Serialize(file,session,SessionFileIntegrity.JsonOptions(indented:true));
            }
            ReleaseUpdateService.StartUpdate(package,installed);
            Application.Current.Shutdown();
        }
        catch(Exception ex) { UpdateStatusText.Text="No se pudo actualizar: "+ex.Message; }
        finally { UpdateProgress.Visibility=Visibility.Collapsed; CheckUpdatesButton.IsEnabled=true; InstallUpdateButton.IsEnabled=true; }
    }

    private void RestoreUpdateSession()
    {
        if(!File.Exists(UpdateRecoveryPath))return;
        try
        {
            if(MessageBox.Show(this,"Hay puntos y procesos respaldados antes de actualizar. ¿Recuperarlos ahora?","Calculadora de refrigeración – Parte 1 – Carta psicrométrica",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            var session=JsonSerializer.Deserialize<SessionV1>(File.ReadAllText(UpdateRecoveryPath),SessionFileIntegrity.JsonOptions());
            if(session is null || session.Version!=1 || !SessionFileIntegrity.TryValidate(session.PressureKPa,session.Points,session.Processes,out _))throw new InvalidDataException("Sesión de recuperación incompatible.");
            ChartMetricsPanel.ClearStateConditions();
            ChartMetricsPanel.SetUnitSystem(session.Units);
            PressureSelector.ApplyExternalPressure(session.PressureKPa);
            PsychrometricChart.SetSharedPoints(session.Points);
            OnPointsChanged(PsychrometricChart.ActivePoints);
            _savedProcesses.Clear(); _savedProcesses.AddRange(session.Processes);
            _nextProcessId=_savedProcesses.Count==0?1:_savedProcesses.Max(p=>p.Id)+1;
            UpdateProcessesInCharts();
            File.Delete(UpdateRecoveryPath);
        }
        catch(Exception ex) { MessageBox.Show(this,"No se pudo recuperar la sesión. Se conserva el respaldo: "+ex.Message,"Calculadora de refrigeración – Parte 1 – Carta psicrométrica",MessageBoxButton.OK,MessageBoxImage.Warning); }
    }

    private void OpenReleases_Click(object sender,RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(ReleaseUpdateService.ReleasesPage){UseShellExecute=true}); }
        catch(Exception ex) { UpdateStatusText.Text="No se pudo abrir el navegador: "+ex.Message; }
    }
}
