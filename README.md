# FrioCalc.Psychrometrics

Módulo dedicado a la carta psicrométrica de FrioCalc, desarrollado en C# / WPF sobre .NET 10 para Windows.

## Foco actual

Por ahora el desarrollo activo se limita a la carta psicrométrica. La bibliografía, materiales de clase, referencias técnicas y análisis de Coolselector permanecen en Google Drive y se consultan desde allí cuando haga falta.

## Estado migrado

- Motor psicrométrico básico en `01_Core`.
- Geometría y capas gráficas en `02_Chart`.
- Selector de presión y panel de variables en `05_UI`.
- Zoom, pan y barras de desplazamiento.
- Modo **Carta estándar** con límites visuales de referencia de -10 a 55 °C de TBS y 0 a 33 g/kg de razón de humedad.

## Regla de trabajo

Los cambios realizados desde ChatGPT deben hacerse en una rama de trabajo y quedar registrados mediante commits. Antes de integrar a `main`, compilar en Visual Studio y comprobar 0 errores.
