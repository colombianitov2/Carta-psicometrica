using System;
using System.Windows;
using System.Windows.Controls;
using FrioCalc.Psychrometrics;
using FrioCalc.Psychrometrics.UI;
using FrioCalc.Psychrometrics.Chart;
using FrioCalc.Psychrometrics.Points;
using System.Collections.Generic;
using System.Linq;
using FrioCalc.Psychrometrics.Core;
using System.Linq;
using System.Windows.Shapes;

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

            Check(chartMetrics.FindName("SpecificEntropyCheckBox") is CheckBox,
                "Casilla independiente entropía");
            Check(chartMetrics.FindName("EnthalpyDeviationCheckBox") is CheckBox,
                "Casilla independiente desviación de entalpía");
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
            // [UI.5] Selección real de punto desde la tarjeta de propiedades.
            var properties=(PointPropertiesPanel)window.FindName("PointPropertiesPanel");
            var cards=properties.FindName("PointsWrapPanel") as WrapPanel;
            Check(cards!=null && cards.Children.Count==1,"Tarjeta de punto creada");
            var pointCard=(Border)cards!.Children[0];
            var pointContents=(StackPanel)pointCard.Child;
            var include=pointContents.Children.OfType<CheckBox>()
                .FirstOrDefault(x=>x.Content?.ToString()=="Incluir en proceso");
            Check(include!=null,"Casilla de selección de procesos");
            include!.IsChecked=true;
            Check(properties.SelectedPointIds.Count==1,
                "Selección desde tarjeta compartida");
            Check(((ListBox)window.FindName("ProcessSelectionList")).Items.Count==1,
                "Selección sincronizada con pestaña de procesos");
            include.IsChecked=false;
            Check(properties.SelectedPointIds.Count==0,
                "Desmarcado retira selección sin borrar punto");

            // [UI.6] Conversión de unidades en controles, presión y puntos.
            chartMetrics.SetUnitSystem(UnitSystem.IP);
            var pressureUnit=pressurePanel.FindName("PressureUnitLabel") as TextBlock;
            Check(pressureUnit?.Text=="psi abs","Selector presión muestra psi absoluta");
            chartMetrics.SetUnitSystem(UnitSystem.SI);
            Check(pressureUnit?.Text=="kPa abs","Selector presión restaura kPa absoluta");



            // [UI.6.1] Flujo real: crear proceso con dos puntos seleccionados,
            // eliminarlo, deshacer y verificar recuperación coherente.
            Check(mainChart.TryAddPointFromConditions(new Dictionary<StateProperty,double>
                { [StateProperty.DryBulb]=30,[StateProperty.RelativeHumidity]=55 },
                out var secondError),"Segundo punto de proceso: "+secondError);
            Check(properties.SelectedPointIds.Count==0,
                "Sin selección residual tras añadir segundo estado");
            var updatedCards=(WrapPanel)properties.FindName("PointsWrapPanel");
            Check(updatedCards.Children.Count==2,"Dos tarjetas para proceso");
            foreach(Border card in updatedCards.Children.OfType<Border>())
            {
                var panel=(StackPanel)card.Child;
                var selector=panel.Children.OfType<CheckBox>()
                    .FirstOrDefault(x=>x.Content?.ToString()=="Incluir en proceso");
                Check(selector!=null,"Selector de proceso disponible en cada punto");
                selector!.IsChecked=true;
            }
            Check(properties.SelectedPointIds.Count==2,"Dos puntos elegidos en orden");
            var typeSelector=(ComboBox)window.FindName("ProcessTypeComboBox");
            typeSelector.SelectedIndex=0;
            ((TextBox)window.FindName("NewProcessNameTextBox")).Text="Auditoria P1-P2";
            var plotMethod=typeof(MainWindow).GetMethod("PlotProcess_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(plotMethod!=null,"Método de graficación existente");
            plotMethod!.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            var processesList=(ListBox)window.FindName("SavedProcessesList");
            Check(processesList.Items.Count==1,"Proceso calculado y guardado en la interfaz");
            Check(processChart.ActivePoints.Count==2,"Dos estados coherentes en la segunda carta");
            Check(((TextBlock)window.FindName("ProcessResultText")).Text.Contains("ΔT="),
                "Resultado numérico de proceso disponible");
            processesList.SelectedIndex=0;
            var remove=typeof(MainWindow).GetMethod("DeleteProcess_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(remove!=null,"Acción de eliminar proceso existente");
            remove!.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            Check(processesList.Items.Count==0 &&
                mainChart.ActivePoints.Count==2,"Eliminar proceso no borra sus puntos");
            var undo=typeof(MainWindow).GetMethod("UndoLastAction_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(undo!=null,"Deshacer implementado");
            undo!.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            Check(processesList.Items.Count==1,"Deshacer recupera proceso eliminado");
            Check(mainChart.ActivePoints.Count==2&&processChart.ActivePoints.Count==2,
                "Deshacer mantiene ambas cartas sincronizadas");

            // [UI.6.2] Regresión de edición predictiva con salida calculada.
            var twoPointCards=(WrapPanel)properties.FindName("PointsWrapPanel");
            var inletCard=(StackPanel)((Border)twoPointCards.Children[0]).Child;
            var inletCheck=inletCard.Children.OfType<CheckBox>()
                .First(x=>x.Content?.ToString()=="Incluir en proceso");
            inletCheck.IsChecked=true;
            Check(properties.SelectedPointIds.Count==1,"Entrada de proceso predictivo única");
            foreach(ComboBoxItem item in typeSelector.Items)
            {
                if(item.Tag?.ToString()=="EvaporativeHumidification")
                {typeSelector.SelectedItem=item;break;}
            }
            ((TextBox)window.FindName("EffectivenessTextBox")).Text="0.5";
            ((TextBox)window.FindName("NewProcessNameTextBox")).Text="Lavador editable";
            plotMethod.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            Check(processesList.Items.Count==2 && mainChart.ActivePoints.Count==3,
                "Humidificación genera una salida y un proceso");
            int oldCalculatedOutput=mainChart.ActivePoints.Last().Id;
            processesList.SelectedIndex=1;
            var edit=typeof(MainWindow).GetMethod("EditProcess_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(edit!=null,"Edición de proceso visible");
            edit!.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            Check(properties.SelectedPointIds.Count==1,
                "Edición de proceso recupera entradas y excluye salida calculada");
            Check(((TextBox)window.FindName("EffectivenessTextBox")).Text.StartsWith("0.5"),
                "Edición recupera efectividad de saturación");
            ((TextBox)window.FindName("EffectivenessTextBox")).Text="0.65";
            plotMethod.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            Check(processesList.Items.Count==2 && mainChart.ActivePoints.Count==3,
                "Recalcular proceso reemplaza salida antigua sin crear huérfanos");
            Check(!mainChart.ActivePoints.Any(p=>p.Id==oldCalculatedOutput),
                "Salida generada anterior realmente retirada");
            Check(processChart.ActivePoints.Count==mainChart.ActivePoints.Count,
                "Segunda carta sincronizada al editar predicción");

            // [UI.7] Auditoría geométrica de entalpías: nunca mostrar estados supersaturados.
            foreach(double pressure in new[]{84.54,101.325})
            {
                var geometry=new ChartGeometry();
                geometry.SetStandardChartMode(true);
                var validationCanvas=new Canvas {Width=850,Height=620};
                validationCanvas.Measure(new Size(850,620));
                validationCanvas.Arrange(new Rect(0,0,850,620));
                EnthalpyLayer.Draw(validationCanvas,geometry,pressure);
                var lines=validationCanvas.Children.OfType<Polyline>().ToList();
                Check(lines.Count>4,$"Familia de entalpía representada a P={pressure}");
                var rectangle=geometry.GetPlotRect(850,620);
                bool invalid=false;
                int sampled=0;
                foreach(var line in lines)
                foreach(var vertex in line.Points)
                {
                    double t=geometry.DryBulbCFromX(vertex.X,rectangle);
                    double w=geometry.HumidityRatioKgPerKgFromY(vertex.Y,rectangle);
                    double ps=SaturationVaporPressure.GetPressureKPa(t);
                    if(ps<pressure)
                    {
                        double ws=SaturationHumidityRatio.FromPressuresKPa(pressure,ps);
                        if(w>ws+1e-8) invalid=true;
                    }
                    sampled++;
                }
                Check(sampled>100,$"Puntos geométricos comprobados a P={pressure}");
                Check(!invalid,$"No dibujar entalpía sobre curva de saturación P={pressure}");
            }

            Check(window.FindName("StateMethodComboBox") == null, "Namescope aislado de control de métodos");
            // [UI.2] Capturas automáticas de referencia (no equivalen a aceptación visual).
            try
            {
                window.Show();
                tabs.SelectedIndex=0; // La integración de procesos puede dejar activa la segunda carta.
                window.UpdateLayout();
                var entropyToggle=(CheckBox)chartMetrics.FindName("SpecificEntropyCheckBox");
                entropyToggle.IsChecked=true;
                window.UpdateLayout();
                var entropyCanvas=(Canvas)mainChart.FindName("ChartCanvas");
                Check(entropyCanvas.Children.OfType<Path>().Any(),
                    "Curvas de entropía dibujadas con geometría de trayectoria");
                Check(entropyCanvas.Children.OfType<TextBlock>().Any(t=>t.Text.StartsWith("s*=")),
                    "Etiquetas de entropía específica s* visibles");
                entropyToggle.IsChecked=false;
                window.UpdateLayout();
                string folder=System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"ui-snapshots");
                System.IO.Directory.CreateDirectory(folder);
                // [UI.3] Capturas del viewport real del runner.
                // El escritorio virtual limita la ventana a 1150×788; no afirmar
                // que se probó 1540×900 solo cambiando Window.Width.
                window.Width=1150;
                window.Height=788;
                window.UpdateLayout();
                for(int tab=0;tab<3;tab++)
                {
                    tabs.SelectedIndex=tab;
                    window.UpdateLayout();
                    window.Dispatcher.Invoke(() => { },
                        System.Windows.Threading.DispatcherPriority.Render);
                    int width=Math.Max(1,(int)Math.Ceiling(window.ActualWidth));
                    int height=Math.Max(1,(int)Math.Ceiling(window.ActualHeight));
                    if(width<1000 || height<700)
                        throw new InvalidOperationException($"Viewport CI demasiado pequeño: {width}×{height}.");
                    var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(
                        width,height,96,96,System.Windows.Media.PixelFormats.Pbgra32);
                    bitmap.Render(window);
                    var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                    string file=System.IO.Path.Combine(folder,$"Zona_{tab+1}_CI_{width}x{height}.png");
                    using(var stream=System.IO.File.Create(file)) encoder.Save(stream);
                    writer.WriteLine($"CAPTURA_WPF: {file} ({width}×{height})");
                }

                // [UI.4] Capturas aisladas de las dos magnitudes que el usuario confundía.
                tabs.SelectedIndex=0;
                window.UpdateLayout();
                foreach(var overlay in new[] {
                    (Toggle:"Entropia_s_relativa", CheckBox:entropyToggle),
                    (Toggle:"Desviacion_entalpia", CheckBox:(CheckBox)chartMetrics.FindName("EnthalpyDeviationCheckBox"))
                })
                {
                    overlay.CheckBox.IsChecked=true;
                    mainChart.RefreshChart();
                    window.UpdateLayout();
                    var chartOnly=(Canvas)mainChart.FindName("ChartCanvas");
                    Check(chartOnly.ActualWidth>220 && chartOnly.ActualHeight>170,
                        $"Overlay {overlay.Toggle} con superficie gráfica real");
                    var snapshot=new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)Math.Ceiling(chartOnly.ActualWidth),(int)Math.Ceiling(chartOnly.ActualHeight),
                        96,96,System.Windows.Media.PixelFormats.Pbgra32);
                    snapshot.Render(chartOnly);
                    var enc=new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(snapshot));
                    string file=System.IO.Path.Combine(folder,$"{overlay.Toggle}.png");
                    using(var stream=System.IO.File.Create(file))enc.Save(stream);
                    writer.WriteLine("CAPTURA_WPF: "+file);
                    overlay.CheckBox.IsChecked=false;
                }
            }
            catch(Exception visualError)
            {
                throw new InvalidOperationException("No se pudieron capturar las tres zonas WPF.",visualError);
            }
            // [UI.8] Prueba real de archivo PNG exportado por la segunda carta.
            tabs.SelectedIndex=1;
            window.UpdateLayout();
            string exportedChart=System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),
                "ui-snapshots","ProcessChart_Export.png");
            processChart.ExportPng(exportedChart);
            Check(System.IO.File.Exists(exportedChart) &&
                new System.IO.FileInfo(exportedChart).Length>1500,
                "Exportación PNG real de carta de procesos");

            // [UI.9] Cambiar presión invalida puntos y mantiene sincronía.
            pressurePanel.ApplyExternalPressure(84.54);
            Check(mainChart.ActivePoints.Count==0 && processChart.ActivePoints.Count==0,
                "Cambio de presión elimina puntos inconsistentes en ambas cartas");
            Check(Math.Abs(pressurePanel.CurrentPressureKPa-84.54)<.0001,
                "Presión personalizada realmente aplicada");
            pressurePanel.ApplyExternalPressure(101.325);
            Check(Math.Abs(pressurePanel.CurrentPressureKPa-101.325)<.0001,
                "Presión estándar restaurada");

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
