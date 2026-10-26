# Análisis histórico de FrioCalc

Este documento se conserva como contexto de la extracción original de la carta psicrométrica desde el proyecto FrioCalc monolítico.

Proyecto analizado:

`D:\Proyectos de desarrollo de Software\Calculadora de refrigeración\FrioCalc`

- Aplicación WPF `.NET 8` para Windows.
- Se identificaron `PsicometriaService`, `PsicometriaChartRenderer`, modelos psicrométricos y recursos gráficos ASHRAE/VALCON.
- La carta dedicada se reestructuró posteriormente como `FrioCalc.Psychrometrics`, separando núcleo, geometría/capas y controles UI.
- La bibliografía y recursos estáticos se mantienen en Drive como fuentes de consulta.

Limitaciones históricas registradas: unidades IP pendientes, validación del bulbo húmedo contra bibliografía, procesos psicrométricos y funciones adicionales aún por ampliar.
