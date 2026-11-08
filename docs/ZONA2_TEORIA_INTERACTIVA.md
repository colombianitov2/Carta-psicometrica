# Zona 2 — Teoría interactiva de psicrometría (FrioCalc)

**Rama experimental:** `chatgpt/friocalc-lista1-2026-11-07`. No fusionar sin auditoría local.

## Propósito

Convertir las diapositivas 1–24 del material docente **A03 Procesos Psicrométricos(2).pptx** en una experiencia de aprendizaje donde cada modificación de controles calcule un estado físicamente admisible y explique **qué propiedad cambia, por qué, con qué ecuación, qué datos fueron utilizados y qué resultado no puede determinarse**.

Se emplea el mismo motor de estados que la Zona 1; el diagrama de la Zona 2 es **esquemático y didáctico**, no reemplaza una carta oficial ASHRAE para lectura de precisión.

## NÚMERO — CLASE — SECCIÓN — ACCIÓN

| NÚMERO | CLASE | SECCIÓN | ACCIÓN |
|---|---|---|---|
| 4.15 | `TheoryLessonService` | `04_Processes` | Calcular 10 escenarios de estudio con el motor psicrométrico, explicación, fórmulas, hipótesis, ruta y autoevaluación |
| 5.5X.1 | `ProcessTheoryPanel.xaml` | `05_UI` | Controles de Tbs, HR, intensidad; Tbs/HR de segunda corriente; modo conceptual, universitario y técnico |
| 5.5C.2 | `ProcessTheoryPanel.xaml.cs` | `05_UI` | Actualizar texto y diagrama al cambiar cualquier variable; no mostrar estados ficticios |
| 5.5C.3 | `ProcessTheoryPanel.xaml.cs` | `05_UI` | Trazar líneas, flechas, puntos y estados intermedios con una escala esquemática |
| 5.5C.6 | `ProcessTheoryPanel.xaml.cs` | `05_UI` | Cargar escenarios de A03 desde la solución de cada ejercicio |
| 5.1X.5 | `MainWindow.xaml` | `05_UI` | Presentar teorías, bibliografía y resultados de las diapositivas en la Zona 2 |
| T13 | `FrioCalc.Psychrometrics.Smoke` | `tests` | Comprobar estados, textos, advertencias, preguntas y propiedades de 10 escenarios |
| UI.10 | `FrioCalc.UI.Smoke` | `tests` | Mover controles reales, comprobar actualización de explicaciones, responder quiz y cargar casos A03 |

## Laboratorios disponibles

| Tipo | Cómo cambia | Límite que debe explicar |
|---|---|---|
| Calefacción sensible | Tbs sube; W constante; HR suele bajar | Sin flujo másico no se obtiene kW |
| Enfriamiento sensible | Tbs baja; W constante hasta rocío | Se detiene al punto de rocío; luego ocurre condensación |
| Lavador evaporativo | Tbs desciende y W sube según efectividad | Modelo ideal no predice el equipo del fabricante |
| Humidificación por vapor | W aumenta y h incorpora entalpía del vapor | Ejemplo supone 2700 kJ/kg; puede sobresaturarse |
| Enfriamiento + deshumidificación | Dos etapas: entrada→rocío→salida saturada | Para carga real de serpentín hay que considerar condensado |
| Mezcla adiabática | Dos entradas con Tbs/HR editables y fracción de masa seca | No promediar m³/h; convertir cada flujo con v propio |
| Calefacción + humidificación | Dos etapas con punto intermedio visible | El orden de las operaciones importa |
| Desecante | W disminuye; Tbs salida supuesta de modo explícito | No infiere regeneración, capacidad ni calor de adsorción |
| Bypass BF/ADP | Mezcla ideal con ADP supuesto visible | BF solo no define impulsión |
| Factor de calor sensible FCS | Indica relación sensible/total | No inventa punto de salida sin carga y caudal |

## Aprendizaje dentro de la aplicación

1. Seleccionar tipo de proceso y presión de la carta.
2. Mover Tbs inicial, HR inicial y el deslizador de intensidad. En mezcla modificar también la segunda corriente.
3. Observar **qué cambió**: W, HR y h se actualizan desde los estados calculados, junto con los puntos y flechas.
4. Consultar la explicación conceptual o activar nivel universitario/técnico para ecuaciones sustituidas y advertencias.
5. Elegir una propiedad del diccionario interactivo para ver definición, ecuación, forma de medición, valor en P1 y fuente.
6. Responder una pregunta contextual y comprobar la explicación de la respuesta correcta.
7. Cargar los ejercicios A03 (6, 7, 14, 15, 23 y 24) desde la Zona 2, explorarlos y contrastarlos con la solución desarrollada. La diapositiva 22 informa explícitamente que la carga total del auditorio no se deduce de los datos aportados.

## Fuentes separadas por procedencia

- **Material docente original A03**, PhD Hermes Ramírez León, diapositivas 1–24: ejemplos, orden de temas y datos de ejercicios (archivo proporcionado).
- **ASHRAE Handbook—Fundamentals, edición 2025**, capítulo 1, sección 10: procesos típicos de calentamiento/enfriamiento sensible, enfriamiento-deshumidificación, mezcla adiabática e inyección de agua. https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx
- **PsychroLib**, funciones de Tbs/Tbh/HR/W/h/v e inversiones; https://psychrometrics.github.io/psychrolib/api_docs.html

No se reproducen textualmente capítulos enteros ni ilustraciones protegidas por derechos de autor. La terminología «rendimiento de Hendrickson» procede de A03; no se atribuye como denominación normativa de ASHRAE sin confirmación documental.

## Límites y criterios de aceptación

- Las ecuaciones se calculan en SI sobre **kg de aire seco**, a presión absoluta activa. El selector de unidades SI/IP de la carta y el simulador docente no son equivalentes en esta versión: los controles docentes explicitan que operan en SI.
- La Zona 2 debe rechazar estados fuera del dominio, sobresaturados y con datos insuficientes, sin simular potencias o capacidades. Los ejes didácticos se ajustan para no ocultar estados válidos.
- Se requiere auditoría de usabilidad a resolución real y comprobación local de lectura, scroll y visibilidad de controles.
- La validación automatizada de Windows no equivale a ensayos de rendimiento, certificación industrial ni prueba de cada dispositivo de entrada.
- Ampliaciones posteriores: persistencia de progreso didáctico, evaluación calificada y esquemas detallados de equipos si se solicitan; no afirmar que ya están implementadas.

**Validación automatizada confirmada:** [GitHub Actions #42445234209](https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42445234209) — 0 errores, 0 advertencias, **585 pruebas numéricas y 95 comprobaciones WPF aprobadas**. Se probó el cálculo de los diez escenarios, la actualización al mover controles, la autoevaluación, la segunda corriente de la UTA, los enlaces a A03 y la autoescala gráfica a 65 °C.

**Estado:** implementación funcional de aprendizaje interactivo en rama experimental, pendiente de aceptación humana y de validación física externa ampliada. La sección ASHRAE abarca los procesos psicrométricos pertinentes al alcance A03, no la totalidad del Handbook.
