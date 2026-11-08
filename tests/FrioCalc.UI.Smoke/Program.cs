using System;
using System.Windows;
using System.Windows.Controls;
using FrioCalc.Psychrometrics;
using FrioCalc.Psychrometrics.UI;
using FrioCalc.Psychrometrics.Chart;
using FrioCalc.Psychrometrics.Points;
using System.Collections.Generic;

namespace FrioCalc.UISmoke;

// UI integration smoke: create the WPF control tree in STA without opening a window.
// This is NOT a screenshot test and does not replace manual interactive audit.
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        int checks = 0;
        try
        {
            using var output = Console.OpenStandardOutput();
            var writer = new System.IO.StreamWriter(output) { AutoFlush = true };
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var window = new MainWindow();
            void Check(bool valid, string description)
            {
                checks++;
                if (!valid) throw new InvalidOperationException("UI FAIL: " + description);
            }
            Check(window.Title.Contains("Psicrometría"), "Ventana psicrométrica");
            Check(window.FindName("WorkspaceTabs") is TabControl, "Tres zonas de trabajo");
            var tabs = (TabControl)window.FindName("WorkspaceTabs");
            Check(tabs.Items.Count == 3, "Carta, procesos, teoría");
            Check(window.FindName("PsychrometricChart") != null, "Carta principal");
            Check(window.FindName("ProcessChart") != null, "Carta de procesos");
            Check(window.FindName("PointPropertiesPanel") is PointPropertiesPanel, "Gestor puntos");
            Check(window.FindName("ChartMetricsPanel") is ChartMetricsPanel, "Entradas propiedades");
            Check(window.FindName("PressureSelector") is PressureSelector, "Presión");
            var pressurePanel=(PressureSelector)window.FindName("PressureSelector");
            Check(pressurePanel.FindName("StandardChartToggle") is System.Windows.Controls.Primitives.ToggleButton,
                "Toggle Carta estándar");
            var chartMetrics=(ChartMetricsPanel)window.FindName("ChartMetricsPanel");
            Check(chartMetrics.FindName("DryBulbCheckBox") is CheckBox dryBulb &&
                dryBulb.IsChecked == true, "Bulbo seco inicial activo");

            Check(window.FindName("ProcessSelectionList") is ListBox, "Selección ordenada");
            Check(window.FindName("SavedProcessesList") is ListBox, "Procesos guardados");
            Check(window.FindName("TheoryExampleResultText") is TextBlock, "Ejercicios A03");
            Check(window.FindName("ProcessTheoryPanel") is ProcessTheoryPanel, "Laboratorio teórico");
            Check(window.FindName("BibliographyPanel") is BibliographyPanel, "Catálogo bibliográfico");
            Check(window.FindName("FcsGuideTextBox") is TextBox, "Guía FCS");
            Check(window.FindName("CloseCycleCheckBox") is CheckBox, "Cierre de ciclo");
            Check(window.FindName("CondensateTemperatureTextBox") is TextBox, "Temperatura de condensado");
            Check(window.FindName("MixFlowsListTextBox") is TextBox, "Mezclas múltiples");
            Check(window.FindName("BranchFractionTextBox") is TextBox, "Fracción de ramificación");
            Check(window.FindName("DesiccantRemovedTextBox") is TextBox, "Datos de desecante");
            var mainChart=(PsychrometricChartView)window.FindName("PsychrometricChart");
            var processChart=(PsychrometricChartView)window.FindName("ProcessChart");
            Check(mainChart.TryAddPointFromConditions(new Dictionary<StateProperty,double>
            {
                [StateProperty.DryBulb]=25,[StateProperty.RelativeHumidity]=60
            },out string note), "Entrada de punto interactivo: "+note);
            Check(mainChart.ActivePoints.Count==1,"Punto creado en carta principal");
            Check(processChart.ActivePoints.Count==1,
                "Sincronización automática con segunda carta");

            Check(window.FindName("StateMethodComboBox") == null, "Namescope aislado de control de métodos");
            // [UI.2] Capturas automáticas de referencia (no equivalen a aceptación visual).
            try
            {
                window.Show();
                window.UpdateLayout();
                string folder=System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"ui-snapshots");
                System.IO.Directory.CreateDirectory(folder);
                // [UI.3] Dos áreas de contenido medidas y dispuestas en tamaños
                // independientes de la pantalla física de GitHub Actions.
                // El HWND puede quedar limitado a 1150 px; por eso NO se renderiza
                // window.ActualWidth, sino el contenido con ancho explícito.
                if (window.Content is not FrameworkElement root)
                    throw new InvalidOperationException("No hay raíz visual WPF para las capturas.");
                // Desacoplar del HWND limitado por el tamaño de pantalla del runner.
                // El árbol WPF se renderiza fuera de pantalla y se restituye al final.
                window.Content = null;
                foreach (var resolution in new[] { (W:1150.0,H:788.0), (W:1540.0,H:900.0) })
                {
                    double desiredWidth=resolution.W-28;
                    double desiredHeight=resolution.H-60;
                    root.Width=desiredWidth;
                    root.Height=desiredHeight;
                    for(int tab=0;tab<3;tab++)
                    {
                        tabs.SelectedIndex=tab;
                        root.Measure(new Size(desiredWidth,desiredHeight));
                        root.Arrange(new Rect(0,0,desiredWidth,desiredHeight));
                        root.UpdateLayout();
                        int width=(int)Math.Ceiling(root.ActualWidth);
                        int height=(int)Math.Ceiling(root.ActualHeight);
                        Check(Math.Abs(width-desiredWidth)<=1 && Math.Abs(height-desiredHeight)<=1,
                            $"Viewport independiente {resolution.W}x{resolution.H}, zona {tab+1}");
                        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(
                            width,height,96,96,System.Windows.Media.PixelFormats.Pbgra32);
                        bitmap.Render(root);
                        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                        string file=System.IO.Path.Combine(folder,
                            $"Zona_{tab+1}_{(int)resolution.W}x{(int)resolution.H}.png");
                        using(var stream=System.IO.File.Create(file)) encoder.Save(stream);
                        writer.WriteLine($"CAPTURA_WPF: {file} ({width}x{height} contenido)");
                    }
                }
                window.Content = root;
            }
            catch(Exception visualError)
            {
                throw new InvalidOperationException("La auditoría visual automatizada no pudo generar las seis capturas válidas.",visualError);
            }
            writer.WriteLine($"PRUEBAS UI SUPERADAS: {checks}");
            window.Close();
            app.Shutdown();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
