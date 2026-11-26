using System;
using System.Windows;
using System.Windows.Controls;
using FrioCalc.Psychrometrics;
using FrioCalc.Psychrometrics.UI;
using FrioCalc.Psychrometrics.Processes;
using FrioCalc.Psychrometrics.Chart;
using FrioCalc.Psychrometrics.Points;
using System.Collections.Generic;
using System.Linq;
using FrioCalc.Psychrometrics.Core;
using System.Globalization;
using System.Windows.Shapes;
using System.Windows.Documents;

namespace FrioCalc.UISmoke;

// UI integration smoke: create the WPF control tree in STA without opening a window.
// This is NOT a screenshot test and does not replace manual interactive audit.
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        // La máquina de CI utiliza inglés; simular también la configuración
        // regional colombiana permite detectar pruebas dependientes del separador decimal.
        string? requestedCulture=Environment.GetEnvironmentVariable("FRIOCALC_UI_CULTURE");
        if(!string.IsNullOrWhiteSpace(requestedCulture))
        {
            var culture=CultureInfo.GetCultureInfo(requestedCulture);
            CultureInfo.CurrentCulture=culture;
            CultureInfo.CurrentUICulture=culture;
        }
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
            string VisibleText(TextBlock block)=>new TextRange(block.ContentStart,block.ContentEnd).Text;
            Check(window.Title == ReleaseUpdateService.OfficialProductName, "Ventana psicrométrica");
            Check(window.FindName("WorkspaceTabs") is TabControl, "Tres zonas de trabajo");
            var tabs = (TabControl)window.FindName("WorkspaceTabs");
            Check(tabs.Items.Count == 7 && tabs.Items.OfType<TabItem>().Any(t=>Equals(t.Header,"Variables")) &&
                tabs.Items.OfType<TabItem>().Any(t=>Equals(t.Header,"Instrucciones")),
                "Carta, procesos, fundamentos, simulador, variables, instrucciones y bibliografía");
            Check(window.FindName("VariableReferencePanel") is VariableReferencePanel,"Tabla de variables independiente");
            Check(window.FindName("CheckUpdatesButton") is Button updateButton && Equals(updateButton.Content,"Buscar actualizaciones"),"Panel de actualizaciones en Bibliografía");
            Check(window.FindName("UpdateVersionText") is TextBlock updateVersion && updateVersion.Text.Contains(ReleaseUpdateService.CurrentVersion.ToString()),"Versión en panel de actualización");
            Check(window.FindName("InstallUpdateButton") is Button installUpdate && installUpdate.Visibility==Visibility.Collapsed,"Instalación solo tras encontrar una actualización");
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
            var teaching=(ProcessTheoryPanel)window.FindName("ProcessTheoryPanel");
            var processTopics=(ComboBox)teaching.FindName("ProcessSelector");
            var drySlider=(Slider)teaching.FindName("DryBulbSlider");
            var intensitySlider=(Slider)teaching.FindName("ParameterSlider");
            var explanation=(TextBlock)teaching.FindName("TheoryExplanation");
            var steps=(TextBlock)teaching.FindName("TheoryStepsText");
            var formulas=(TextBlock)teaching.FindName("TheoryFormulaText");
            var change=(TextBlock)teaching.FindName("TheoryChangeText");
            var warning=(TextBlock)teaching.FindName("TheoryWarningText");
            var variableSelector=(ComboBox)teaching.FindName("VariableSelector");
            var variableInfo=(TextBlock)teaching.FindName("VariableExplanationText");
            var question=(TextBlock)teaching.FindName("TheoryQuestionText");
            var options=(ComboBox)teaching.FindName("TheoryAnswerCombo");
            var feedback=(TextBlock)teaching.FindName("TheoryFeedbackText");
            Check(processTopics.Items.Count>=10,"Zona 2 incluye diez escenarios docentes");
            Check(explanation.Text.Contains("Calefacción")&&formulas.Inlines.OfType<Run>().Any(r=>r.Text=="2"&&r.BaselineAlignment==BaselineAlignment.Subscript),
                "Zona 2 explica el proceso inicial con fórmula");
            double former=drySlider.Value;
            drySlider.Value=former+3;
            Check(teaching.CurrentLesson is { } warmer && Math.Abs(warmer.Initial.DryBulbC-(former+3))<1e-9 &&
                change.Text.Contains("Tbs inicial") && VisibleText(steps).Contains($"{former+3:0.##}"),
                $"Mover temperatura actualiza estado, explicación y sustitución numérica: {change.Text} | {VisibleText(steps)}");
            intensitySlider.Value=75;
            Check(change.Text.Contains("Intensidad") &&
                (VisibleText(steps).Contains("0,75") || VisibleText(steps).Contains("0.75")),
                "Mover intensidad modifica pasos en tiempo real");
            processTopics.SelectedIndex=6; // Calentamiento + humidificación.
            var combined=teaching.CurrentLesson;
            var stateTable=(StackPanel)teaching.FindName("TheoryStateTable");
            Check(combined is {Kind:"HeatingHumidification",Auxiliary:not null,Final:not null} &&
                combined.Route.Count==3 && combined.Route[1].Id==combined.Auxiliary.Id &&
                Math.Abs(combined.Initial.HumidityRatioKgPerKg-combined.Auxiliary.HumidityRatioKgPerKg)<1e-10 &&
                combined.Final.HumidityRatioKgPerKg>combined.Auxiliary.HumidityRatioKgPerKg &&
                explanation.Text.Contains("Calentamiento seguido") &&
                VisibleText(steps).Contains("HR intermedia") &&
                VisibleText(steps).Contains($"{combined.Auxiliary.RelativeHumidityPercent:0.###}") &&
                stateTable.Children.OfType<Grid>().FirstOrDefault()?.ColumnDefinitions.Count==4,
                $"Lección combinada muestra estados intermedios: {explanation.Text} | {VisibleText(steps)}");
            processTopics.SelectedIndex=9; // FCS con salida indeterminada.
            var values=(TextBlock)teaching.FindName("TheoryValues");
            Check(values.Text.Contains("INDETERMINADO") &&
                warning.Text.Contains("no se debe dibujar"),
                "FCS no crea salida inexistente");
            variableSelector.SelectedItem=TheoryLessonService.Variables.Single(v=>v.Key=="SHR");
            Check(options.Items.Count==3 && question.Text.Contains("FCS"),
                "Cuestionario depende de la variable seleccionada");
            options.SelectedIndex=0;
            var quiz=typeof(ProcessTheoryPanel).GetMethod("CheckTheoryAnswer_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(quiz!=null,"Verificación de quiz enlazada");
            quiz!.Invoke(teaching,new object[]{teaching,new RoutedEventArgs(Button.ClickEvent)});
            Check(feedback.Text.Contains("CORRECTO"),"Cuestionario responde a elección del estudiante");
            var fcsQuestion=question.Text;
            variableSelector.SelectedIndex=4; // W.
            var firstWQuestion=question.Text;
            Check(firstWQuestion!=fcsQuestion && firstWQuestion.Contains("humedad absoluta") &&
                options.SelectedIndex==-1,"Cambiar variable actualiza pregunta y limpia respuesta");
            var nextVariableQuestion=(Button)teaching.FindName("NextVariableQuestionButton");
            nextVariableQuestion.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(question.Text!=firstWQuestion && options.Items.Count==3,
                "Siguiente pregunta recorre el banco de la variable");
            var secondWQuestion=question.Text;
            options.SelectedIndex=2;
            quiz.Invoke(teaching,new object[]{teaching,new RoutedEventArgs(Button.ClickEvent)});
            Check(feedback.Text.Contains("CORRECTO"),"La respuesta correcta pertenece a la pregunta nueva");
            foreach(var variable in TheoryLessonService.Variables)
            {
                variableSelector.SelectedItem=variable;
                Check(options.Items.Count==3&&question.Text==VariableQuizCatalog.For(variable.Key)[variable.Key=="W"?1:0].Prompt,
                    "Pregunta vinculada a "+variable.Key);
            }
            variableSelector.SelectedIndex=4;
            Check(question.Text==secondWQuestion,"Cada variable conserva posición al volver");
            Check(VisibleText(variableInfo).Contains("Humedad")&&((StackPanel)teaching.FindName("VariableEquationsPanel")).Children.Count>0 && !VisibleText(variableInfo).Contains("Referencia:"),
                "Inspector de propiedades representa la ecuación sin referencias intercaladas");
            // [UI.10] Ejercicio real A03/23 enlaza ambas corrientes y
            // muestra las condiciones de la UTA en el laboratorio.
            string linked=teaching.LoadSlideExample(23);
            Check(linked.Contains("3000")&&linked.Contains("500"),
                "Enlace docente A03/23 conserva caudales de ambas corrientes");
            Check(processTopics.SelectedIndex==5,
                "A03/23 selecciona mezcla adiabática");
            Check(question.Text==secondWQuestion,"Cambiar proceso no reemplaza pregunta de variable");
            var secondStream=(StackPanel)teaching.FindName("SecondStreamControls");
            var secondDry=(Slider)teaching.FindName("SecondDryBulbSlider");
            var secondHum=(Slider)teaching.FindName("SecondHumiditySlider");
            Check(secondStream.Visibility==Visibility.Visible &&
                Math.Abs(secondDry.Value-32)<.01&&Math.Abs(secondHum.Value-70)<.01,
                "A03/23 configura 32°C y HR70% en segunda corriente");
            secondDry.Value=34;
            Check(change.Text.Contains("Tbs exterior")&&VisibleText(steps).Contains("34"),
                "Cambiar Tbs exterior actualiza la explicación dinámica");
            Check(teaching.LoadSlideExample(24).Contains("FCSi=0,75") &&
                ((TextBlock)teaching.FindName("TheoryValues")).Text.Contains("INDETERMINADO"),
                "A03/24 no inventa impulsión a partir de FCS/BF");
            Check(teaching.LoadSlideExample(22).Contains("no incluyen envolvente"),
                "A03/22 advierte sobre carga total no resoluble");
            // [UI.11] Extender ejes si el alumno supera el máximo de la
            // carta didáctica inicial, manteniendo todos los puntos visibles.
            processTopics.SelectedIndex=0; // Calefacción.
            drySlider.Value=45;
            intensitySlider.Value=100; // 45 + 20 = 65 °C.
            var educationalCanvas=(Canvas)teaching.FindName("DiagramCanvas");
            var dots=educationalCanvas.Children.OfType<Ellipse>().ToArray();
            Check(dots.Length>=2,"Dos estados del ejemplo de calefacción a 65°C");
            Check(dots.All(dot=>
                Canvas.GetLeft(dot)>=0 && Canvas.GetLeft(dot)+dot.Width<=educationalCanvas.Width &&
                Canvas.GetTop(dot)>=0 && Canvas.GetTop(dot)+dot.Height<=educationalCanvas.Height),
                "Autoescala del esquema conserva los puntos fuera de 60°C");

            // Restore the tutorial to its reference state for the visual captures.
            var resetLesson=typeof(ProcessTheoryPanel).GetMethod("ResetTheoryExample_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            resetLesson!.Invoke(teaching,new object[]{teaching,new RoutedEventArgs(Button.ClickEvent)});
            Check(processTopics.SelectedIndex==0&&drySlider.Value==25,
                "Restablecer escenario docente");

            // [UI.12] Navegación real: 31 temas y 36 láminas, curso reactivo.
            var curriculum=window.FindName("TheoryCoursePanel") as TheoryCoursePanel;
            if(curriculum is null)
                throw new InvalidOperationException("UI FAIL: Zona 2 no integra curso completo");
            var chapterSelector=(ComboBox)curriculum.FindName("TopicSelector");
            var slideSelector=(ComboBox)curriculum.FindName("SlideSelector");
            var chapterSummary=(TextBlock)curriculum.FindName("TopicSummary");
            var chapterSources=(TextBlock)curriculum.FindName("TopicReference");
            var chapterEquation=(TextBlock)curriculum.FindName("TopicEquation");
            var liveExplanation=(TextBlock)curriculum.FindName("LiveStateExplanation");
            var courseChart=(Canvas)curriculum.FindName("ReadingCanvas");
            Check(chapterSelector.Items.Count>=31 && slideSelector.Items.Count==36,
                "Índice de 31 temas y 36 diapositivas");
            curriculum.ShowSlide(36);
            Check(curriculum.CurrentTopic?.LastSlide==36 &&
                chapterSummary.Text.Contains("150 kW") &&
                chapterSources.Text.Contains("A03"),
                "A03/36: enunciado y procedencia completos");
            slideSelector.SelectedItem=15;
            Check(curriculum.CurrentTopic?.FirstSlide==15 &&
                chapterEquation.Inlines.OfType<InlineUIContainer>().Any(),
                "Ir a diapositiva 15 actualiza fórmula de condensado");
            chapterSelector.SelectedItem=TheoryCourseCatalog.Find("BASE-03");
            Check(chapterEquation.Inlines.OfType<Run>().Any(r=>r.Text=="sat"&&r.BaselineAlignment==BaselineAlignment.Subscript) &&
                courseChart.Children.OfType<Polyline>().Any(),
                "Carta guiada resalta envolvente de saturación");
            var topicRequest=typeof(TheoryCoursePanel).GetMethod("ExploreTopic_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(topicRequest!=null,"Enlace de teoría a laboratorio disponible");
            chapterSelector.SelectedItem=TheoryCourseCatalog.Find("A03-12");
            topicRequest!.Invoke(curriculum,new object[]{curriculum,new RoutedEventArgs(Button.ClickEvent)});
            Check(processTopics.SelectedIndex==5,
                "Curso lanza proceso mezcla en laboratorio");
            Check(liveExplanation.Text.Contains("Tbs=") &&
                liveExplanation.Text.Contains("W="),
                "Curso explica estado actual después de cambiar simulación");
            var exerciseRequest=typeof(TheoryCoursePanel).GetMethod("SolveTopicExercise_Click",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(exerciseRequest!=null,"Enlace a ejercicio resuelto disponible");
            chapterSelector.SelectedItem=TheoryCourseCatalog.Find("A03-19");
            exerciseRequest!.Invoke(curriculum,new object[]{curriculum,new RoutedEventArgs(Button.ClickEvent)});
            Check(((TextBlock)window.FindName("TheoryExampleResultText")).Text.Contains("DIAPOSITIVA 36"),
                "Curso A03/36 produce cálculo auditable");
            curriculum.ShowSlide(1);
            Check(curriculum.CurrentTopic?.FirstSlide==1,"Volver a inicio de presentación");

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
            Check(!pointContents.Children.OfType<CheckBox>().Any(x=>x.Content?.ToString()=="Incluir en proceso"),
                "La tarjeta ya no contiene casilla de procesos");
            var pointTexts=pointContents.Children.OfType<Grid>().SelectMany(row=>row.Children.OfType<TextBlock>()).ToArray();
            Check(pointTexts.Any(t=>t.Text=="Automático") && pointTexts.All(t=>t.Text!="Automatic"),
                "Método de resolución traducido, sin cambiar identificador interno");
            Check(pointTexts.Any(t=>t.Text=="Entropía relativa\u00A0(s*):") &&
                pointTexts.Any(t=>t.Text=="Bulbo seco:") &&
                pointTexts.All(t=>!t.Text.StartsWith("DryBulb:")),
                "Etiqueta de entropía compacta y propiedades de entrada en español");
            var available=(ListBox)window.FindName("AvailablePointsList");
            Check(available.Items.Count==1,"Punto disponible en la zona de procesos");
            properties.SetSelection(new[]{mainChart.ActivePoints[0].Id});
            Check(((ListBox)window.FindName("ProcessSelectionList")).Items.Count==1,
                "Lista ordenada sincronizada con pestaña de procesos");
            properties.ClearSelection();
            Check(properties.SelectedPointIds.Count==0 && mainChart.ActivePoints.Count==1,
                "Retirar selección conserva punto");

            // [UI.6] Conversión de unidades en controles, presión y puntos.
            var flowEditor=(TextBox)window.FindName("DryFlowTextBox");
            var condensateEditor=(TextBox)window.FindName("CondensateTemperatureTextBox");
            flowEditor.Text="1"; condensateEditor.Text="10";
            chartMetrics.SetUnitSystem(UnitSystem.IP);
            Check(((TextBlock)window.FindName("DryFlowLabel")).Text.Contains("lb/h") &&
                ((TextBlock)window.FindName("CondensateTemperatureLabel")).Text.Contains("°F") &&
                Math.Abs(double.Parse(flowEditor.Text,CultureInfo.CurrentCulture)-7936.64143865559)<1e-5 &&
                Math.Abs(double.Parse(condensateEditor.Text,CultureInfo.CurrentCulture)-50)<1e-7,"Editor convierte caudal y temperatura a IP sin reinterpretación");
            var ipStudy=(AshraeStudyPanel)window.FindName("AshraeStudyPanel");
            ipStudy.SelectUnit("A19");
            var ipEquation=(AshraeEquation)((ComboBox)ipStudy.FindName("FormulaSelector")).SelectedItem;
            Check(ipEquation.Units=="°R" && ipEquation.Numerator.Contains("459,67") &&
                ((TextBlock)ipStudy.FindName("UnitExample")).Text.Contains("77 °F"),
                "Fundamentos cambia ecuación, temperatura absoluta y ejemplo a IP");
            string expectedIpInitial = EducationalUnits.Temp(drySlider.Value,UnitSystem.IP);
            Check(((TextBlock)teaching.FindName("DryBulbValueLabel")).Text.Contains(expectedIpInitial) &&
                VisibleText(steps).Contains("°F") && VisibleText(steps).Contains("gr/lb") &&
                !VisibleText(steps).Contains("°C") && steps.Visibility==Visibility.Visible &&
                teaching.FindName("ExplanationLevelComboBox")==null,
                $"Simulador IP con procedimiento de ingeniería fijo: {expectedIpInitial} | {((TextBlock)teaching.FindName("DryBulbValueLabel")).Text} | {VisibleText(steps)}");
            var ipEnthalpy=EducationalUnits.Equation(AshraeStudyCatalog.FindEquation("h")!,UnitSystem.IP);
            Check(ipEnthalpy.Numerator.Contains("0,240279") && ipEnthalpy.Numerator.Contains("t − 32") &&
                Math.Abs((.240279*45+.010*(1075.236458+.444253*45))-EducationalUnits.H(50.625,UnitSystem.IP))<1e-4,
                "Entalpía IP conserva referencia y valor del modelo SI");
            var pressureUnit=pressurePanel.FindName("PressureUnitLabel") as TextBlock;
            Check(pressureUnit?.Text=="psi abs","Selector presión muestra psi absoluta");
            chartMetrics.SetUnitSystem(UnitSystem.SI);
            Check(Math.Abs(double.Parse(flowEditor.Text,CultureInfo.CurrentCulture)-1)<1e-7 &&
                Math.Abs(double.Parse(condensateEditor.Text,CultureInfo.CurrentCulture)-10)<1e-7,"Editor restaura entradas originales SI");
            flowEditor.Clear(); condensateEditor.Clear();
            Check(pressureUnit?.Text=="kPa abs","Selector presión restaura kPa absoluta");
            Check(((AshraeEquation)((ComboBox)ipStudy.FindName("FormulaSelector")).SelectedItem).Units=="K",
                "Fundamentos vuelve al SI");
            ipStudy.SelectUnit("A1");



            // [UI.6.1] Flujo real: crear proceso con dos puntos seleccionados,
            // eliminarlo, deshacer y verificar recuperación coherente.
            Check(mainChart.TryAddPointFromConditions(new Dictionary<StateProperty,double>
                { [StateProperty.DryBulb]=30,[StateProperty.RelativeHumidity]=55 },
                out var secondError),"Segundo punto de proceso: "+secondError);
            Check(properties.SelectedPointIds.Count==0,
                "Sin selección residual tras añadir segundo estado");
            var updatedCards=(WrapPanel)properties.FindName("PointsWrapPanel");
            Check(updatedCards.Children.Count==2,"Dos tarjetas para proceso");
            properties.SetSelection(mainChart.ActivePoints.Select(p=>p.Id).ToArray());
            Check(properties.SelectedPointIds.Count==2,"Dos puntos elegidos en orden");
            var typeSelector=(ComboBox)window.FindName("ProcessTypeComboBox");
            var compatibilityText=(TextBlock)window.FindName("ProcessCompatibilityText");
            var validatedSave=(Button)window.FindName("SaveProcessGroupButton");
            typeSelector.SelectedIndex=3;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.Contains("Proceso no posible")&&
                compatibilityText.Text.Contains("P1 → P2"),
                "Seleccionar enfriamiento con estados de calentamiento informa incompatibilidad sin guardar");
            typeSelector.SelectedIndex=1;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.Contains("W constante"),
                "Calor sensible incompatible con cambio de humedad");
            typeSelector.SelectedIndex=2;
            Check(validatedSave.IsEnabled&&compatibilityText.Text.StartsWith("Compatible:"),
                "Método de calentamiento y humidificación compatible");
            typeSelector.SelectedIndex=4;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.StartsWith("Datos insuficientes:"),
                "Dos estados de humidificación por vapor requieren parámetros de equipo");
            typeSelector.SelectedIndex=5;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.StartsWith("Proceso no posible:"),
                "Evaporación incompatible con calentamiento");
            typeSelector.SelectedIndex=6;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.Contains("desecante"),
                "Desecante no permite aumento de W");
            typeSelector.SelectedIndex=7;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.StartsWith("Datos insuficientes:"),
                "Mezcla sin caudales no se guarda");
            typeSelector.SelectedIndex=8;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.Contains("serpentín"),
                "Bypass incompatible con calentamiento y adición de humedad");
            typeSelector.SelectedIndex=9;
            Check(!validatedSave.IsEnabled&&compatibilityText.Text.StartsWith("Datos insuficientes:"),
                "Divisor sin tres estados no se guarda");
            typeSelector.SelectedIndex=0;
            Check(validatedSave.IsEnabled,"Automático vuelve a habilitar el cálculo");
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
            properties.SetSelection(new[]{mainChart.ActivePoints[0].Id});
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
            string recoveredEffectiveness=((TextBox)window.FindName("EffectivenessTextBox")).Text;
            Check(double.TryParse(recoveredEffectiveness,NumberStyles.Float,
                CultureInfo.CurrentCulture,out double parsedEffectiveness) &&
                Math.Abs(parsedEffectiveness-.5)<1e-9,
                "Edición recupera efectividad 0,5 en configuración regional "+
                CultureInfo.CurrentCulture.Name+"; leído: '"+recoveredEffectiveness+"'");
            ((TextBox)window.FindName("EffectivenessTextBox")).Text="0.65";
            plotMethod.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            Check(processesList.Items.Count==2 && mainChart.ActivePoints.Count==4,
                "Recalcular proceso conserva la salida antigua como punto independiente");
            Check(mainChart.ActivePoints.Any(p=>p.Id==oldCalculatedOutput),
                "Salida anterior conservada para otros grupos");
            Check(processChart.ActivePoints.Count==mainChart.ActivePoints.Count,
                "Segunda carta sincronizada al editar predicción");

            // [UI.6.2.1] Constructor de tarjetas: agregar, ordenar, calcular y editar sin otra ventana.
            var builder=(StackPanel)window.FindName("ProcessBuilderView");
            var groupEditor=(StackPanel)window.FindName("ProcessEditor");
            var groupCards=(StackPanel)window.FindName("ProcessCardsPanel");
            var plus=(Button)window.FindName("AddProcessGroupButton");
            Check(builder.Visibility==Visibility.Visible && groupEditor.Visibility==Visibility.Collapsed &&
                available.ItemTemplate is DataTemplate,"Constructor visible con tarjetas de propiedades");
            plus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(builder.Visibility==Visibility.Visible && groupEditor.Visibility==Visibility.Collapsed,
                "Crear grupo no despliega sus parámetros");
            ListBox GroupList(Border card)=>((StackPanel)card.Child).Children.OfType<ListBox>().Single();
            var draftCard=groupCards.Children.OfType<Border>().Last();
            int groupId=(int)GroupList(draftCard).Tag;
            var groupScroll=(ScrollViewer)window.FindName("ProcessGroupsScroll");
            Check(groupScroll.VerticalScrollBarVisibility==ScrollBarVisibility.Auto &&
                groupScroll.Content==groupCards && double.IsFinite(groupScroll.MaxHeight) &&
                ScrollViewer.GetVerticalScrollBarVisibility(GroupList(draftCard))==ScrollBarVisibility.Auto &&
                double.IsFinite(GroupList(draftCard).MaxHeight),
                "Lista de grupos y tarjetas tienen desplazamiento vertical acotado");
            System.Windows.Controls.Primitives.ToggleButton GroupToggle(Border card)=>
                ((StackPanel)card.Child).Children.OfType<DockPanel>().Single().Children
                    .OfType<System.Windows.Controls.Primitives.ToggleButton>().Single();
            Button GroupPencil(Border card)=>((StackPanel)card.Child).Children.OfType<DockPanel>()
                .Single().Children.OfType<Button>().Single();
            GroupToggle(draftCard).IsChecked=false;
            Check(GroupList(draftCard).Visibility==Visibility.Collapsed && GroupPencil(draftCard).Visibility==Visibility.Visible,
                "Grupo contraído conserva el lápiz y oculta sus tarjetas");
            GroupPencil(draftCard).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((TextBox)window.FindName("NewProcessNameTextBox")).Text="Borrador renombrado";
            ((Button)window.FindName("RenameProcessButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            draftCard=groupCards.Children.OfType<Border>().Last();
            Check(builder.Visibility==Visibility.Visible && GroupToggle(draftCard).IsChecked==false &&
                GroupList(draftCard).Items.Count==0 &&
                GroupPencil(draftCard).ToolTip?.ToString()=="Editar Borrador renombrado",
                "Renombrar grupo vacío contraído conserva nombre, ID y estado visual");
            GroupToggle(draftCard).IsChecked=true;
            Check(GroupList(draftCard).Visibility==Visibility.Visible && (int)GroupList(draftCard).Tag==groupId,
                "Expandir restaura el mismo grupo");
            var membership=typeof(MainWindow).GetMethod("UpdateBuilderMembership",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            int firstState=mainChart.ActivePoints[1].Id,secondState=mainChart.ActivePoints[0].Id;
            membership.Invoke(window,new object[]{groupId,firstState,0,false,-1});
            Check(processesList.Items.Count==2 && GroupList(groupCards.Children.OfType<Border>().Last()).Items.Count==1,
                "Un punto conserva borrador sin inventar proceso");
            membership.Invoke(window,new object[]{groupId,secondState,1,false,-1});
            Check(processesList.Items.Count==3 && GroupList(groupCards.Children.OfType<Border>().Last()).Items.Count==2,
                "Segundo punto calcula y guarda automáticamente el tramo");
            Check(processesList.Items.Cast<string>().Last().Contains($"P{firstState} → P{secondState}"),
                "El proceso respeta orden de ingreso, no numeración original");
            membership.Invoke(window,new object[]{groupId,secondState,0,false,1});
            Check(processesList.Items.Cast<string>().Last().Contains($"P{secondState} → P{firstState}"),
                "Reordenar recalcula dirección sin duplicar el punto");
            int thirdState=mainChart.ActivePoints[2].Id,fourthState=mainChart.ActivePoints[3].Id;
            membership.Invoke(window,new object[]{groupId,thirdState,2,false,-1});
            membership.Invoke(window,new object[]{groupId,fourthState,3,false,-1});
            membership.Invoke(window,new object[]{groupId,secondState,4,false,-1});
            string closedRoute=$"P{secondState} → P{firstState} → P{thirdState} → P{fourthState} → P{secondState}";
            Check(processesList.Items.Cast<string>().Last().Contains(closedRoute) &&
                GroupList(groupCards.Children.OfType<Border>().Last()).Items.Count==5 &&
                mainChart.ActivePoints.Count==4,
                "Arrastrar desde disponibles repite el estado inicial y cierra cuatro tramos sin mover la primera tarjeta");
            var groupCard=groupCards.Children.OfType<Border>().Last();
            var chartGroups=(StackPanel)window.FindName("MainProcessResultsPanel");
            var chartGroup=chartGroups.Children.OfType<Border>().Single(card=>Equals(card.Tag,groupId));
            StackPanel GroupResults(Border card)=>((StackPanel)card.Child).Children.OfType<StackPanel>().Single();
            var resultLabels=GroupResults(groupCard).Children.OfType<Grid>().SelectMany(grid=>grid.Children.OfType<Border>())
                .Select(cell=>cell.Child).OfType<TextBlock>().Select(t=>t.Text).ToArray();
            Check(resultLabels.Any(s=>s=="Método seleccionado") &&
                resultLabels.Any(s=>s.StartsWith("Componente sensible específica")) &&
                resultLabels.Any(s=>s.StartsWith("Componente latente específica")) &&
                resultLabels.Any(s=>s.StartsWith("Potencia sensible")),
                "Tablas de tramos incluyen método, energía sensible y latente");
            GroupToggle(chartGroup).IsChecked=false;
            Check(GroupResults(chartGroup).Visibility==Visibility.Collapsed &&
                GroupResults(groupCard).Visibility==Visibility.Collapsed &&
                GroupList(groupCard).Visibility==Visibility.Collapsed &&
                GroupToggle(groupCard).IsChecked==false,
                "Contraer desde carta oculta tablas y tarjetas del grupo en ambas vistas");
            GroupToggle(groupCard).IsChecked=true;
            Check(GroupToggle(chartGroup).IsChecked==true&&GroupResults(chartGroup).Visibility==Visibility.Visible,
                "Expandir desde procesos restaura también la carta");
            GroupToggle(chartGroup).IsChecked=false;
            GroupPencil(chartGroup).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(tabs.SelectedIndex==1&&groupEditor.Visibility==Visibility.Visible,
                "Lápiz de carta abre el editor del mismo grupo");
            ((TextBox)window.FindName("NewProcessNameTextBox")).Text="Ciclo renombrado desde carta";
            ((Button)window.FindName("RenameProcessButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            groupCard=groupCards.Children.OfType<Border>().Last();
            chartGroup=chartGroups.Children.OfType<Border>().Single(card=>Equals(card.Tag,groupId));
            Check(tabs.SelectedIndex==0&&GroupPencil(chartGroup).ToolTip?.ToString()=="Editar Ciclo renombrado desde carta" &&
                GroupToggle(groupCard).IsChecked==false&&GroupToggle(chartGroup).IsChecked==false &&
                processesList.Items.Cast<string>().Last().Contains(closedRoute),
                "Renombrar desde carta regresa a carta y conserva grupo contraído y recorrido");
            GroupToggle(groupCard).IsChecked=false;
            Check(GroupList(groupCard).Visibility==Visibility.Collapsed && mainChart.ActivePoints.Count==4 &&
                processesList.Items.Cast<string>().Last().Contains(closedRoute),
                "Contraer un ciclo no altera sus puntos ni conexiones");
            GroupPencil(groupCard).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((TextBox)window.FindName("NewProcessNameTextBox")).Text="Ciclo cerrado renombrado";
            ((Button)window.FindName("RenameProcessButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            groupCard=groupCards.Children.OfType<Border>().Last();
            Check(GroupToggle(groupCard).IsChecked==false && processesList.Items.Cast<string>().Last().Contains("Ciclo cerrado renombrado") &&
                processesList.Items.Cast<string>().Last().Contains(closedRoute) && mainChart.ActivePoints.Count==4,
                "Renombrar ciclo contraído no recalcula ni cambia el recorrido");
            GroupToggle(groupCard).IsChecked=true;
            var pencil=GroupPencil(groupCard);
            pencil.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(builder.Visibility==Visibility.Collapsed && groupEditor.Visibility==Visibility.Visible,
                "Lápiz cambia a editor dentro de la misma pestaña");
            Check(((ListBox)window.FindName("ProcessSelectionList")).Items.Count==5,
                "Editor conserva las dos apariciones del punto inicial");
            ((TextBox)window.FindName("NewProcessNameTextBox")).Text="Grupo por tarjetas";
            ((Button)window.FindName("SaveProcessGroupButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(builder.Visibility==Visibility.Visible && groupEditor.Visibility==Visibility.Collapsed &&
                processesList.Items.Cast<string>().Last().Contains("Grupo por tarjetas") &&
                processesList.Items.Cast<string>().Last().Contains(closedRoute),
                "Guardar vuelve al constructor conservando grupo");
            membership.Invoke(window,new object[]{groupId,secondState,0,true,4});
            Check(processesList.Items.Cast<string>().Last().Contains($"P{secondState} → P{firstState} → P{thirdState} → P{fourthState}") &&
                GroupList(groupCards.Children.OfType<Border>().Last()).Items.Count==4 && mainChart.ActivePoints.Count==4,
                "Retirar la tarjeta de cierre abre el recorrido sin borrar el punto inicial");
            membership.Invoke(window,new object[]{groupId,firstState,0,true,1});
            membership.Invoke(window,new object[]{groupId,fourthState,0,true,2});
            membership.Invoke(window,new object[]{groupId,thirdState,0,true,1});
            Check(processesList.Items.Count==2 && mainChart.ActivePoints.Count==4,
                "Retirar miembros no los borra de la carta");
            var finalDraft=groupCards.Children.OfType<Border>().Last();
            ((StackPanel)finalDraft.Child).Children.OfType<DockPanel>().Single().Children.OfType<Button>().Single()
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            remove!.Invoke(window,new object[]{window,new RoutedEventArgs(Button.ClickEvent)});
            Check(groupCards.Children.OfType<Border>().Count()==2 && mainChart.ActivePoints.Count==4,
                "Eliminar borrador conserva los estados existentes");
            Check(window.FindName("ProcessPageScroll") is ScrollViewer processPage && processPage.AllowDrop,
                "El fondo de la página admite retirar tarjetas del grupo");
            Check(window.FindName("ChartPageScroll") is ScrollViewer chartPage &&
                chartPage.Content is StackPanel chartContent && chartContent.Children.Contains(mainChart) &&
                chartContent.Children.Contains(properties),
                "Una barra de la carta envuelve gráfico y propiedades");

            // [UI.6.3] Etiquetas de puntos cercanos no deben superponerse.
            var proximityCanvas=new Canvas{Width=800,Height=520};
            proximityCanvas.Measure(new Size(800,520));
            proximityCanvas.Arrange(new Rect(0,0,800,520));
            var proximityGeometry=new ChartGeometry();
            proximityGeometry.SetStandardChartMode(true);
            var nearby=new List<ChartPoint>();
            foreach(var condition in new[]{
                (T:25.0,W:.0100),(T:25.2,W:.0102),(T:24.9,W:.0099)})
            {
                int id=nearby.Count+1;
                Check(PointPropertyCalculator.TryCreatePoint(id,$"C{id}","#336699",
                    101.325,condition.T,condition.W,out var point)&&point!=null,
                    "Estado de prueba de etiquetas");
                nearby.Add(point!);
            }
            PointLayer.Draw(proximityCanvas,proximityGeometry,nearby);
            var pointLabels=proximityCanvas.Children.OfType<TextBlock>()
                .Where(t=>t.Text.StartsWith("C")).ToArray();
            Check(pointLabels.Length==3,"Todas las etiquetas de tres puntos próximos");
            var labelRects=pointLabels.Select(t=>new Rect(
                Canvas.GetLeft(t),Canvas.GetTop(t),t.DesiredSize.Width,t.DesiredSize.Height)).ToArray();
            bool labelOverlap=false;
            for(int i=0;i<labelRects.Length;i++)
            for(int j=i+1;j<labelRects.Length;j++)
                if(labelRects[i].IntersectsWith(labelRects[j])) labelOverlap=true;
            Check(!labelOverlap,"Etiquetas cercanas distribuidas sin cruces");

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

            // [UI.13] Rango, continuidad y dominio físico de contornos Δh.
            foreach(double testPressure in new[]{84.54,101.325})
            {
                var geom=new ChartGeometry();
                geom.SetStandardChartMode(true);
                var cv=new Canvas{Width=860,Height=560};
                cv.Measure(new Size(860,560));
                cv.Arrange(new Rect(0,0,860,560));
                EnthalpyDeviationLayer.Draw(cv,geom,testPressure);
                var deviations=cv.Children.OfType<Polyline>().ToArray();
                Check(deviations.Length>=2,"Se ven contornos Δh físicos a P="+testPressure);
                Check(deviations.Sum(x=>x.Points.Count)>25,
                    "Δh no está oculta ni son solo fragmentos aislados P="+testPressure);
                var clipRect=geom.GetPlotRect(860,560);
                bool aboveSat=false;
                foreach(var curve in deviations)
                foreach(var vertex in curve.Points)
                {
                    double t=geom.DryBulbCFromX(vertex.X,clipRect);
                    double w=geom.HumidityRatioKgPerKgFromY(vertex.Y,clipRect);
                    double pSat=SaturationVaporPressure.GetPressureKPa(t);
                    if(pSat<testPressure)
                    {
                        double ws=SaturationHumidityRatio.FromPressuresKPa(testPressure,pSat);
                        if(w>ws+2e-5) aboveSat=true;
                    }
                }
                Check(!aboveSat,"Δh respeta saturación a P="+testPressure);
            }

            // [UI.14] Enseñanza basada en ASHRAE y fracciones WPF.
            var ashrae=(AshraeStudyPanel)window.FindName("AshraeStudyPanel");
            var studySelector=(ComboBox)ashrae.FindName("StudySelector");
            var eqSelector=(ComboBox)ashrae.FindName("FormulaSelector");
            var simulator=(AshraeStudyPanel)window.FindName("AshraeSimulatorPanel");
            var stepSelector=(ComboBox)simulator.FindName("ConstructionStepSelector");
            var formulaVisual=(ContentControl)ashrae.FindName("FormulaVisual");
            Check(studySelector.Items.Count==24 &&
                eqSelector.Items.Count==2 && stepSelector.Items.Count==12,
                "Fundamentos expone 24 temas y ecuaciones fijas por tema; simulador expone 12 pasos");
            ashrae.SelectUnit("A4");
            Check(formulaVisual.Content is WrapPanel,
                "Fracción matemática representada por elementos, no texto plano");
            var fraction=(WrapPanel)formulaVisual.Content;
            Check(fraction.Children.Count==2 &&
                fraction.Children[1] is StackPanel fractionStack &&
                fractionStack.Children.Count==3 && fractionStack.Children[1] is Border,
                "Numerador, línea horizontal y denominador visibles");
            simulator.SelectConstructionStep(12);
            ((Slider)simulator.FindName("TemperatureSlider")).Value=45;
            ((Slider)simulator.FindName("HumiditySlider")).Value=100;
            var constructionCanvas=(Canvas)simulator.FindName("ConstructionCanvas");
            Check(constructionCanvas.Children.OfType<Ellipse>().Any(e=>Canvas.GetTop(e)>=0 && Canvas.GetTop(e)+e.Height<=constructionCanvas.Height), "Carta de construcción incluye el punto a 45 °C y 100 % HR");
            Check(stepSelector.SelectedIndex==11 &&
                ((TextBlock)simulator.FindName("ConstructionStepTitle")).Text.Contains("12"),
                "Construcción de carta describe cada etapa hasta la doce");
            ashrae.SelectUnit("A4");
            Check(((TextBlock)ashrae.FindName("UnitHeading")).Text.Contains("presión de saturación",StringComparison.OrdinalIgnoreCase),
                "Temario navega en orden físico independiente del PowerPoint");
            var workUnits=(ComboBox)pressurePanel.FindName("WorkUnitsComboBox");
            Check(workUnits!=null&&workUnits.SelectedIndex==0,
                "Unidad SI está en panel de presión al inicio");
            Check(pressurePanel.FindName("SelectPointsToggle") is
                System.Windows.Controls.Primitives.ToggleButton selectPoint &&
                selectPoint.IsChecked==true,
                "Selección de puntos habilitada al iniciar");

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

                // [UI.3.1] Espacio completo para explicación y tablas en
                // pantallas estrechas: ajustes solo visibles en Carta.
                var sideSettings=(ScrollViewer)window.FindName("SettingsScrollViewer");
                Check(sideSettings.Visibility==Visibility.Collapsed,
                    "La teoría usa el ancho completo sin barra lateral de carta");
                tabs.SelectedIndex=1;
                window.UpdateLayout();
                Check(sideSettings.Visibility==Visibility.Collapsed,
                    "Procesos usa ancho completo para sus tablas");
                tabs.SelectedIndex=0;
                window.UpdateLayout();
                Check(sideSettings.Visibility==Visibility.Visible,
                    "Los ajustes reaparecen en Carta psicrométrica");

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
                "ui-snapshots","MainChart_With_Processes.png");
            mainChart.ExportPng(exportedChart);
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

            // [UI.14.1] La bibliografía debe mostrar fórmulas en fracción visual.
            var bibliography=(BibliographyPanel)window.FindName("BibliographyPanel");
            var bibliographyGrid=(DataGrid)bibliography.FindName("EntriesGrid");
            var chosenEntry=bibliographyGrid.Items.Cast<BibliographyEntry>()
                .First(entry=>entry.Id=="B06");
            bibliographyGrid.SelectedItem=chosenEntry;
            var bibliographyFraction=(ContentControl)bibliography.FindName("EntryEquationVisual");
            Check(bibliographyFraction.Content is WrapPanel,
                "Bibliografía W renderiza numerador sobre denominador");
            Check(((TextBlock)bibliography.FindName("EntryEquationProvenance")).Text.Contains("Deducción"),
                "Catálogo técnico conserva la deducción");
            var visibleBibliography=(ItemsControl)window.FindName("BibliographyTopics");
            Check(visibleBibliography.Items.Count==24 && bibliography.Visibility==Visibility.Collapsed &&
                visibleBibliography.Items.Cast<AshraeStudyUnit>().All(unit=>unit.Source==AshraeStudyCatalog.Book),
                "Bibliografía visible contiene únicamente temas y libros");

            // [UI.15] Sin barras gráficas intrusivas y clic agrega/quita puntos.
            tabs.SelectedIndex=0;
            window.UpdateLayout();
            var horizontalScroll=(System.Windows.Controls.Primitives.ScrollBar)
                mainChart.FindName("HorizontalChartScrollBar");
            var verticalScroll=(System.Windows.Controls.Primitives.ScrollBar)
                mainChart.FindName("VerticalChartScrollBar");
            Check(horizontalScroll.Visibility==Visibility.Collapsed &&
                verticalScroll.Visibility==Visibility.Collapsed,
                "Desplazamiento de carta sin barras laterales");
            var chartCanvas=(Canvas)mainChart.FindName("ChartCanvas");
            var clickMethod=typeof(PsychrometricChartView).GetMethod("AddOrRemovePointAt",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Check(clickMethod!=null,"El clic del ratón dispone de un controlador propio");
            // El punto debe quedar dentro del dominio a 101,325 kPa.
            var clickPlot=new ChartGeometry().GetPlotRect(
                chartCanvas.ActualWidth,chartCanvas.ActualHeight);
            var loc=new Point(clickPlot.Left+clickPlot.Width*.55,
                clickPlot.Bottom-clickPlot.Height*.08);
            clickMethod!.Invoke(mainChart,new object[]{loc});
            Check(mainChart.ActivePoints.Count==1,"Clic crea punto en carta");
            clickMethod.Invoke(mainChart,new object[]{loc});
            Check(mainChart.ActivePoints.Count==0,"Segundo clic elimina el punto creado");
            // [UI.16] Ocultar números de HR sin borrar la curva visible.
            chartMetrics.SetNumberVisibility("RelativeHumidity",false);
            var currentCanvas=(Canvas)mainChart.FindName("ChartCanvas");
            Check(currentCanvas.Children.OfType<Polyline>().Any(),
                "Desactivar etiquetas de HR conserva las curvas");
            Check(!currentCanvas.Children.OfType<TextBlock>()
                    .Any(t=>t.Text.Contains("%") && t.Visibility==Visibility.Visible),
                "La opción quita números de HR sin ocultar curvas");
            chartMetrics.SetNumberVisibility("RelativeHumidity",true);
            Check(currentCanvas.Children.OfType<TextBlock>()
                .Any(t=>t.Text.Contains("%") && t.Visibility==Visibility.Visible),
                "Se pueden restaurar números de humedad relativa");

            // [UI.17] Una curva de HR con solo dos muestras visibles debe
            // dibujarse sin intervalo inválido en la selección de etiqueta.
            var narrowCanvas=new Canvas{Width=900,Height=600};
            narrowCanvas.Measure(new Size(900,600));
            narrowCanvas.Arrange(new Rect(0,0,900,600));
            narrowCanvas.UpdateLayout();
            var narrowGeometry=new ChartGeometry();
            Rect narrowPlot=narrowGeometry.GetPlotRect(900,600);
            narrowGeometry.ZoomAt(narrowPlot,new Point(narrowPlot.Left+narrowPlot.Width/2,
                narrowPlot.Top+narrowPlot.Height/2),.001);
            narrowGeometry.PanBy(25-narrowGeometry.MinDryBulbC,0);
            // La malla adaptativa usa 80 intervalos en un rango de 2 °C.
            double tEnd=narrowGeometry.MaxDryBulbC;
            double wA=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(tEnd-.05,.90,101.325);
            double wB=RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(tEnd-.025,.90,101.325);
            narrowGeometry.PanBy(0,(wA+wB)/2-narrowGeometry.MinHumidityRatioKgPerKg);
            RelativeHumidityLayer.Draw(narrowCanvas,narrowGeometry,101.325);
            Check(narrowCanvas.Children.OfType<Polyline>().Any(p=>p.Points.Count==2),
                "HR: dos vértices visibles sin excepción al etiquetar");

            // [UI.18] Pantalla completa: mover la misma instancia y crear
            // estados allí, conservarlos al regresar, sin cambiar su identidad.
            var fullButton=(Button)mainChart.FindName("FullScreenButton");
            var resetButton=(Button)mainChart.FindName("ResetChartViewButton");
            var fullHost=(Grid)window.FindName("ChartFullScreenHost");
            var originalChartParent=(Panel)mainChart.Parent;
            Check(fullButton.Visibility==Visibility.Visible && fullHost.Visibility==Visibility.Collapsed,
                "Botón de pantalla completa en la carta principal");
            fullButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Check(fullHost.Visibility==Visibility.Visible && ReferenceEquals(mainChart.Parent,fullHost)
                && tabs.Visibility==Visibility.Collapsed && mainChart.PointSelectionEnabled,
                "Pantalla completa reutiliza la carta y oculta la interfaz");
            mainChart.ResetVisibleView();
            var fullCanvas=(Canvas)mainChart.FindName("ChartCanvas");
            Rect fullPlot=new ChartGeometry().GetPlotRect(fullCanvas.ActualWidth,fullCanvas.ActualHeight);
            Check(!fullPlot.IsEmpty,"Carta ampliada dispone de superficie interactiva");
            var fullPoint=new Point(fullPlot.Left+fullPlot.Width*.55,fullPlot.Bottom-fullPlot.Height*.08);
            clickMethod!.Invoke(mainChart,new object[]{fullPoint});
            Check(mainChart.ActivePoints.Count==1 && processChart.ActivePoints.Count==1,
                "Punto creado en pantalla completa llega al modelo compartido");
            int preservedId=mainChart.ActivePoints[0].Id;
            Check(mainChart.TryAddPointFromConditions(new Dictionary<StateProperty,double>{
                {StateProperty.DryBulb,30},{StateProperty.RelativeHumidity,50}},out _),
                "Segundo estado válido en la vista ampliada");
            var fullRoute=new PsychrometricProcess(999,"Prueba pantalla completa","#008080",
                ProcessType.Automatic,mainChart.ActivePoints.Select(p=>p.Id).ToArray(),null);
            mainChart.SetProcesses(new[]{fullRoute});
            Check(fullCanvas.Children.OfType<Line>().Any(l=>l.ToolTip is string tip && tip.Contains(fullRoute.Name)),
                "Tramo visible en pantalla completa");
            fullButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Check(ReferenceEquals(mainChart.Parent,originalChartParent)
                && tabs.Visibility==Visibility.Visible && fullHost.Visibility==Visibility.Collapsed
                && mainChart.ActivePoints.Any(p=>p.Id==preservedId)
                && mainChart.ActivePoints.Count==2 &&
                fullCanvas.Children.OfType<Line>().Any(l=>l.ToolTip is string tip && tip.Contains(fullRoute.Name)),
                "Regreso conserva puntos, proceso y carta original");
            resetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(mainChart.ActivePoints.Count==2 &&
                fullCanvas.Children.OfType<Line>().Any(l=>l.ToolTip is string tip && tip.Contains(fullRoute.Name)),
                "Restablecer encuadre no borra datos");

            // [UI.19] Encabezado fijo y ecuaciones exponenciales.
            tabs.SelectedIndex=4;
            window.UpdateLayout();
            var variableView=(VariableReferencePanel)window.FindName("VariableReferencePanel");
            var variableRoot=(Grid)variableView.Content;
            var variableHeader=variableRoot.Children.OfType<Grid>().Single(g=>Grid.GetRow(g)==1);
            var variableScroll=variableRoot.Children.OfType<ScrollViewer>().Single();
            var variableRows=(StackPanel)variableScroll.Content;
            Check(variableHeader.Children.Count==5 && variableRows.Children.Count>10 &&
                Grid.GetRow(variableScroll)==2 && Math.Abs(variableHeader.ActualWidth-variableScroll.ViewportWidth)<2,
                "Encabezado fijo de Variables alineado con las filas");
            var fixedY=variableHeader.TranslatePoint(new Point(),variableRoot).Y;
            variableScroll.ScrollToVerticalOffset(250);
            window.UpdateLayout();
            Check(variableScroll.VerticalOffset>0 && Math.Abs(variableHeader.TranslatePoint(new Point(),variableRoot).Y-fixedY)<.1,
                "Desplazar Variables conserva encabezado");
            var mathBlock=new TextBlock();
            AshraeStudyPanel.FormatMath(mathBlock,"e^{5,2559 ln(x)} + p_{ws}");
            Check(mathBlock.Inlines.OfType<Run>().Any(x=>x.BaselineAlignment==BaselineAlignment.Superscript&&x.Text=="5,2559 ln(x)") &&
                mathBlock.Inlines.OfType<Run>().Any(x=>x.BaselineAlignment==BaselineAlignment.Subscript&&x.Text=="ws"),
                "Exponentes y subíndices matemáticos reales");
            Check(AshraeStudyCatalog.FindEquation("PwsExp")!.Numerator=="e^{L}" &&
                EducationalUnits.Equation(AshraeStudyCatalog.FindEquation("Atmospheric")!,UnitSystem.IP).Numerator.Contains("e^{"),
                "Ecuaciones exponenciales SI/IP sin texto exp");

            // [UI.20] Formato real del desarrollo numérico y de las expresiones históricas.
            var workedMath=new TextBlock();
            AshraeStudyPanel.FormatMath(workedMath,"ṁda=(10000/3600)/v1; HR=100·p_v/p_ws(Tbs).");
            var fractionVisuals=workedMath.Inlines.OfType<InlineUIContainer>().ToArray();
            Check(fractionVisuals.Length==2 && fractionVisuals[0].Child is StackPanel nested &&
                nested.Children[1] is Border && ((TextBlock)nested.Children[0]).Inlines.OfType<InlineUIContainer>().Any(),
                "Divisiones de ejemplos, incluidas anidadas, con barra matemática real");
            var dimensionText=new TextBlock();
            AshraeStudyPanel.FormatMath(dimensionText,"Energía en kJ/kg de aire seco; caudal en kg/s.");
            Check(dimensionText.Text.Contains("kJ/kg")&&dimensionText.Text.Contains("kg/s")&&
                !dimensionText.Inlines.OfType<InlineUIContainer>().Any(),"No confundir unidades con operaciones");
            var equationText=new TextBlock();
            AshraeStudyPanel.FormatMath(equationText,"W2=W1; c_p,da; 10⁻⁵");
            Check(equationText.Inlines.OfType<Run>().Count(r=>r.BaselineAlignment==BaselineAlignment.Subscript)>=3 &&
                equationText.Inlines.OfType<Run>().Any(r=>r.BaselineAlignment==BaselineAlignment.Superscript&&r.Text=="−5"),
                "Índices de estados, subíndices compuestos y potencias tipográficas");
            foreach(var unit in new[]{UnitSystem.SI,UnitSystem.IP})
            {
                ashrae.SetUnits(unit);
                foreach(var topic in AshraeStudyCatalog.Units)
                {
                    ashrae.SelectUnit(topic.Id);
                    Check(formulaVisual.Content is WrapPanel,"Ecuación visible en fundamento "+topic.Id+" "+unit);
                }
                simulator.SetUnits(unit);
                for(int step=1;step<=AshraeStudyCatalog.ConstructionSteps.Count;step++)
                    simulator.SelectConstructionStep(step);
                teaching.SetUnits(unit);
                foreach(var variable in TheoryLessonService.Variables)
                {
                    variableSelector.SelectedItem=variable;
                    Check(((StackPanel)teaching.FindName("VariableEquationsPanel")).Children.Count>0,
                        "Diccionario renderiza ecuaciones de "+variable.Key+" "+unit);
                }
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
