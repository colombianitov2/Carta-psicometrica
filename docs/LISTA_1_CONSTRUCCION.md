# LISTA 1 — Construcción de FrioCalc, revisión de ingeniería

Fecha: 2026-11-07. Rama aislada: chatgpt/friocalc-lista1-2026-11-07.

## Estado verificado
La rama contiene código y archivos de prueba. **No se ha confirmado una compilación WPF ni ejecución en Windows desde esta conversación.** No fusionar a la rama anterior hasta obtener resultados de Visual Studio o GitHub Actions.

## Entregas desarrolladas para auditoría integrada
- Estado inverso basado en dos propiedades independientes, controles de consistencia y rechazo de insuficiencia y sobresaturación.
- SI/IP para entradas, panel de puntos y ejes principales; pendientes escalas auxiliares y presión visible IP.
- Casillas de valor independientes de visibilidad; FCS se puede ocultar y mostrar.
- Alta y eliminación de puntos, selección múltiple en orden y botón Ubicar punto.
- Segunda carta, varios procesos con color común, orden de puntos, mezcla convergente y resultados por etapas.
- Modelos de calor sensible, deshumidificación conocida, humidificación evaporativa ideal, humidificación con vapor y BF/ADP ideal. El diseño de desecantes, la regeneración, procesos bifurcados y cargas de equipo complejas requieren datos adicionales.
- Zona 2 con definiciones y ejemplos interactivos basados en las diapositivas 1–24 de A03.
- Exportación CSV y sesión JSON versionada.
- Pruebas de humo basadas en estados, conservación y unidades.

## Limitaciones físicas a no ocultar
- Potencia requiere flujo másico de aire seco. Flujos volumétricos se convierten con volumen específico de cada corriente.
- La separación sensible/latente se calcula con una convención contable de entalpía y necesita corregirse con la entalpía del condensado para carga rigurosa de serpentín.
- Humidificación evaporativa: modelo aproximado de entalpía constante; aporte entálpico del agua de reposición despreciado.
- Vapor: se requiere masa de agua añadida y entalpía de vapor; no usar valor estándar por defecto.
- BF/ADP: mezcla ideal de bypass y aire en contacto saturado; no modela ventilador ni condensación del equipo.
- Mezcla ideal: si resulta sobresaturada, se bloquea el cálculo y solicita modelo de condensación.
- Los materiales de las diapositivas 25–36 no forman parte del alcance aprobado.
- «Rendimiento de Hendrickson» figura en la diapositiva 13, pero no se ha podido verificar como denominación normativa independiente. Usar «efectividad de saturación» hasta corroborarlo.

## Validación prevista
1. Abrir la rama en copia separada y compilar con SDK .NET 10 y Visual Studio compatible.
2. Ejecutar tests/FrioCalc.Psychrometrics.Smoke en Windows.
3. Comprobar con capturas la visibilidad de FCS, unidades SI/IP, estado con dos entradas, error por datos incompatibles, ubicación, eliminación y selección de puntos.
4. Graficar dos o más procesos con colores distintos; validar flechas y mezcla de dos entradas.
5. Reproducir ejercicios de las diapositivas 6,7,14,15,22,23,24; marcar explícitamente los que requieren datos adicionales.
6. Auditar la lista 1 completa antes de fusionar.

## Fuentes de métodos
- ASHRAE Handbook—Fundamentals, capítulo 1 (Psychrometrics), https://www.ashrae.org/technical-resources/publications-library/ashrae-handbook/table-of-contents-2025-ashrae-handbook-fundamentals
- PsychroLib, código y documentación https://github.com/psychrometrics/psychrolib
- CoolProp HumidAir, https://coolprop.org/fluid_properties/HumidAir.html
- ASHRAE Evaporative Air-Cooling Equipment https://handbook.ashrae.org/handbooks/S20/SI/s20_ch41/s20_ch41_si.aspx
- A03 Procesos Psicrométricos, diapositivas 1–24, Hermes Ramírez León (material docente aportado por el usuario; no redistribuir sin permiso).
