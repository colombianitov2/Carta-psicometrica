# Análisis de FrioCalc

Proyecto analizado:

```text
D:\Proyectos de desarrollo de Software\Calculadora de refrigeración\FrioCalc
```

## Solución y proyectos

- Solución encontrada: `FrioCalc\FrioCalc.slnx`.
- Proyecto encontrado: `FrioCalc\FrioCalc.csproj`.
- Tipo de aplicación: WPF de escritorio.
- Framework: `net8.0-windows`.
- Propiedades relevantes: `UseWPF=true`, `Nullable=enable`, `ImplicitUsings=enable`.
- Dependencias NuGet externas: no se detectaron en el `.csproj`.

## Archivos revisados

- `FrioCalc\FrioCalc.csproj`.
- `FrioCalc\FrioCalc.slnx`.
- `FrioCalc\App.xaml`.
- `FrioCalc\MainWindow.xaml`.
- `FrioCalc\Views\PsicometriaView.xaml`.
- `FrioCalc\Views\PsicometriaView.xaml.cs`.
- `FrioCalc\Services\PsicometriaService.cs`.
- `FrioCalc\Services\PsicometriaChartRenderer.cs`.
- `FrioCalc\Models\PsicometriaState.cs`.
- `FrioCalc\Models\PsicometriaPoint.cs`.
- `FrioCalc\Models\PsicometriaPropertyRow.cs`.
- `FrioCalc\MainWindowPartes\MainWindow.Psicometria.cs`.
- `FrioCalc\MainWindowPartes\MainWindow.PsicometriaChart.cs`.
- `FrioCalc\MainWindowPartes\MainWindow.Teoria.cs`, por referencias bibliográficas y alcance psicrométrico.
- `FrioCalc\Assets\Tablas\PsychroSI.JPG`.
- `FrioCalc\Assets\Tablas\CartaPsicometricaValcon.jpg`.

## Componentes encontrados

- Vista de carta psicrométrica integrada en `PsicometriaView`.
- Servicio de cálculo psicrométrico en `PsicometriaService`.
- Renderizador WPF por `Canvas` en `PsicometriaChartRenderer`.
- Modelos de estado, punto y filas de propiedades.
- Navegación desde `MainWindow` hacia la vista psicrométrica.
- Recursos gráficos de cartas de referencia ASHRAE/VALCON.
- Sección de teoría con referencias a ASHRAE Handbook Fundamentals, ASHRAE Psychrometric Chart No. 1 y PsychroLib.

## Reutilizado

- Ecuaciones de presión de vapor saturado usadas por FrioCalc.
- Relación de humedad desde presión parcial de vapor.
- Entalpía del aire húmedo.
- Volumen específico.
- Punto de rocío por búsqueda binaria.
- Bulbo húmedo aproximado por entalpía saturada.
- Criterios de validación física básicos.
- Trazado WPF de carta: grilla, HR, saturación, entalpía, bulbo húmedo, volumen específico y escala de factor de calor sensible.

## Reescrito

- La interfaz se reescribió como aplicación dedicada, no como vista secundaria de FrioCalc.
- Se reemplazó la ventana monolítica inicial del nuevo repositorio por `Views/MainWindow.xaml`.
- Se separaron modelos, servicios, control gráfico y ViewModel.
- El cálculo A→B se formalizó como `ProcesoPsicometrico`.
- La exportación CSV y guardado JSON se implementaron como servicios propios.
- Los resultados se organizaron en tablas separadas para Punto A, Punto B y Proceso.

## No reutilizado directamente

- Navegación general de FrioCalc.
- Módulos de teoría, tablas, carga térmica, refrigerantes y CLTD.
- Recursos gráficos `PsychroSI.JPG` y `CartaPsicometricaValcon.jpg`; se mantienen como referencias locales analizadas, pero no se copiaron al nuevo proyecto para evitar depender de imágenes estáticas en la primera versión.
- UI específica de `PsicometriaView`, porque mezclaba interacción de tabla, puntos y trazado dentro de code-behind.

## Limitaciones detectadas

- El bulbo húmedo actual es una aproximación por igualdad de entalpía saturada, útil para primera versión pero debe validarse contra bibliografía y datos patrón.
- No hay unidades IP todavía.
- No hay selección de punto por clic ni edición/movimiento de puntos.
- No hay exportación PNG/SVG de la carta.
- No hay carga de EPW/CSV.
- Los módulos normativos de confort térmico quedan pendientes hasta incluir bibliografía validada.

## Resultado de la extracción

El nuevo proyecto quedó como WPF `.NET 8`, con motor psicrométrico básico compilable, UI dedicada y documentación de ejecución. La base técnica sale de FrioCalc, pero la estructura fue reescrita para que la carta psicométrica crezca como aplicación independiente.
