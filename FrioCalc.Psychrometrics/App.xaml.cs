using System.Configuration;
using System.Data;
using System.Windows;

namespace FrioCalc.Psychrometrics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if(e.Args.Length>0 && e.Args[0]=="--apply-portable-update")
            {
                try { Core.ReleaseUpdateService.ApplyPortableUpdate(e.Args); Shutdown(0); }
                catch(Exception ex) { MessageBox.Show("No se pudo reemplazar el ejecutable: "+ex.Message,"FrioCalc",MessageBoxButton.OK,MessageBoxImage.Error); Shutdown(1); }
                return;
            }
            base.OnStartup(e);
            var window=new MainWindow();
            MainWindow=window;
            window.Show();
        }
    }

}
