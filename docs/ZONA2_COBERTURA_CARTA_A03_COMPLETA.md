# FrioCalc — Zona 2: carta psicrométrica y presentación A03 completa

**Alcance ampliado por solicitud del usuario:** 36 diapositivas de `A03 Procesos Psicrométricos(2).pptx`, más una introducción técnica a la lectura de la carta. La antigua restricción a las diapositivas 1–24 queda reemplazada **solo para la Zona 2**, no para otras ramas de la aplicación.

**Separación de procedencias:** los ejemplos, cifras y secuencia de diapositivas son del material docente A03. Las ecuaciones/interpretaciones añadidas se basan en el cálculo del programa, ASHRAE Handbook—Fundamentals 2025 capítulo 1 y PsychroLib. **No** se presenta el texto complementario como transcripción literal del manual ASHRAE.

## 1. Curso de lectura de la carta (12 unidades complementarias)

| ID | Materia | Interacción prevista |
|---|---|---|
| BASE-01 | Qué representa la carta, aire húmedo a presión absoluta fija | Estado P1 y lectura de propiedades |
| BASE-02 | Ejes Tbs y W, razón de humedad y terminología | Guías vertical/horizontal al punto |
| BASE-03 | Saturación de vapor, HR=100 %, región monofásica | Resaltar envolvente y mostrar Ws |
| BASE-04 | Curvas de humedad relativa y variación al calentar | Curvas de HR calculadas |
| BASE-05 | Temperatura de bulbo húmedo termodinámico | Interacción con lavado evaporativo |
| BASE-06 | Temperatura y horizontal hasta punto de rocío | Leer Trocío desde W constante |
| BASE-07 | Isolíneas de entalpía y balance de energía | Mostrar aproximación de h constante |
| BASE-08 | Volumen específico y conversión de caudales | Mezclas ponderadas con ṁda=V̇/v |
| BASE-09 | Factor de calor sensible FCS/SHR | Advertencia de salida indeterminada |
| BASE-10 | Desviación de entalpía Δh frente a entropía relativa s* | Familias distintas, no equivalentes |
| BASE-11 | Presión atmosférica y sistemas de unidades | Dependencia con P absoluta |
| BASE-12 | Resolución de estado, selección de puntos, mezclas y ciclos | Orden, balances, límites físicos |

Las figuras del curso son **esquemas originales calculados**, no copias de la carta licenciada de ASHRAE. La carta principal (Zona 1) sigue siendo el instrumento de trazado de mayor precisión del programa.

## 2. Correspondencia completa de A03 (36 diapositivas)

| Diapositivas | Teoría de la presentación | Desarrollo incorporado |
|---|---|---|
| 1–2 | Psicrometría e introducción | Propiedades de estado, alcance y objetivos |
| 3–5 | Calefacción y enfriamiento sensible | W/Trocío invariantes bajo hipótesis de proceso sensible |
| 6 | Enfriar 5 m³/s: 35→25°C, W=10 g/kgda | Cálculo de v, ṁda, h1, h2 y potencia |
| 7 | Calentar 10000 m³/h: 2°C/80% HR → 21°C | Balance sensible de calor |
| 8–9 | Humidificación/deshumidificación: motivación | Adición o retirada de masa de vapor |
| 10–13 | Vapor, evaporación, enfriamiento y desecantes | Procesos y ecuaciones explicados sin inventar datos de equipo |
| 14 | Lavador: 30°C/10% HR, efectividad 70% | Tbh, T2, W2, HR2 y prueba de HR95% |
| 15 | Tbs32°C, Trocío28°C, salida21°C saturada | Condensado, Δh, calor latente y límites del balance |
| 16–17 | Procesos combinados | Calentamiento + humedad; enfriamiento + condensación; mezcla |
| 18–21 | Diagramas gráficos de procesos combinados | Interpretación complementaria y tramos de estado |
| 22 | Auditorio 300 personas, 8 L/s por persona | Ventilación 2,4 m³/s, sin atribuir carga total no definida |
| 23 | Mezclar 3000 m³/h retorno y 500 m³/h exterior | Conversión por volumen específico propio y mezcla másica |
| 24 | Auditorio: FCSi=0,75, BF=0,20 | Interpretación sin crear un punto ficticio |
| 25–27 | Nueva sección: procesos de climatización, esquemas | Equipos, puntos de estado y trayectorias condicionadas |
| 28 | Cuatro climas colombianos de referencia docente | Tipo de carga sensible/latente y sistemas posibles |
| 29–30 | Climatización para climas cálidos y fríos (visual) | Trayectorias y decisiones teóricas, con limitaciones |
| 31 | ADP, SHR y bypass | Definición y relaciones del modelo de serpentín |
| 32 | Repetición del problema de FCS/BF en auditorio | Equivalencia didáctica con ejercicio 24, sin falsa respuesta |
| 33–35 | RSHF/GSHF, impulsión y compatibilidad de serpentín (gráficas) | Dirección de procesos y fronteras de balance |
| 36 | Auditorio: RSH150 kW, RLH50 kW, ventilador +1,5°C, 20% exterior, serpentín11°C/95% HR, agua6→12°C | Algoritmo por estados R/E/M/C/S; cálculo estimado de caudal de aire, serpentín y agua fría; comprobar cierre sensible/latente |

**A03 25–36 está expresamente incluida**, conforme a la ampliación pedida. Las diapositivas principalmente gráficas no tienen un desarrollo textual equivalente en el archivo: el curso presenta una interpretación original con advertencia de procedencia, sin inventar detalles de esas imágenes.

## 3. Interacciones de aprendizaje

- Seleccionar tema por índice o ir a cualquier diapositiva 1–36 con el selector.
- Leer resumen, explicación física, relación ecuación–carta, desarrollo y límites antes de iniciar un cálculo.
- Observar un **esquema psicrométrico calculado** con ejes, saturación y familias HR, con familias resaltadas según tema.
- Al mover Tbs, HR o presión en el laboratorio, actualizar el estado mostrado en el curso mediante enlace de eventos.
- Lanzar la simulación correspondiente a un tema, o resolver un ejercicio A03 sin reescribir sus condiciones.
- En ejercicios 32/36, mostrar explícitamente las hipótesis y la ausencia de datos reales de equipo.
- Conservar la evaluación interactiva y el diccionario de las doce propiedades existentes.

## 4. Principios de seguridad técnica y honestidad

1. Un sistema de acondicionamiento requiere balances coherentes de **aire seco, vapor y energía**.
2. No permitir estados con HR>100% en la carta monofásica ni inventar propiedades indefinidas de aire completamente seco.
3. El ejercicio 36 interpreta el **20% de aire exterior como fracción másica seca para ilustración**, porque el texto no precisa si es volumen o masa. Si se especifica una fracción volumétrica, deben recalcularse ṁda1 y ṁda2 con sus volúmenes específicos.
4. La carga de serpentín se estima con entalpía de condensado líquido a temperatura de salida de serpentín (**hipótesis**, no dato del enunciado). La compatibilidad RSH/RLH debe verificarse y comunicar discrepancias.
5. El caudal de agua fría depende de ΔT=12−6=6°C, cp del agua supuesto y potencia estimada. No equivale a una selección hidráulica o de catálogo.
6. La tabla climática de A03/28 se considera **ilustrativa de clase**; no son datos meteorológicos verificados para dimensionamiento de edificaciones.
7. Los diagramas y familias de líneas del curso son educativos. Para lectura fina, usar Zona 1 y validar frente a carta autorizada.
8. La documentación pública de ASHRAE comprueba la existencia del capítulo 1 de psicrometría, pero no autoriza transcribir el libro entero ni atribuirle literalmente cada explicación creada aquí.

## 5. Mapa de código

- **4.16** `TheoryCourseCatalog.cs`: 31 temas, cobertura 1–36 comprobable, fundamentos, ecuaciones, fuentes.
- **5.6X** `TheoryCoursePanel.xaml`: panel con índice, navegación por diapositivas y lectura interactiva.
- **5.6C** `TheoryCoursePanel.xaml.cs`: estado en vivo, familias de curvas esquemáticas y enlaces a simuladores.
- **4.7.36** `SlideExamples.cs`: balance de auditorio con ventilador y agua fría, supuestos y comprobaciones.
- **5.5C** `ProcessTheoryPanel.xaml.cs`: enlace desde teoría a simuladores y estados actuales.
- **5.1C** `MainWindow.xaml[.cs/.Lista1.cs]`: incorporación a Zona 2 y enlazado de ejercicios 32/36.
- **T14 y UI.12**: pruebas de cobertura, fuentes, ecuaciones, navegación y cálculos.

## 6. Fuentes y validación

1. Documento aportado: **A03 Procesos Psicrométricos(2).pptx**, PhD Hermes Ramírez León.
2. ASHRAE, *2025 Handbook—Fundamentals*, capítulo 1, Psychrometrics: https://www.ashrae.org/technical-resources/publications-library/ashrae-handbook/table-of-contents-2025-ashrae-handbook-fundamentals
3. PsychroLib, documentación de funciones de aire húmedo: https://psychrometrics.github.io/psychrolib/api_docs.html
4. CoolProp, descripción de modelos de aire húmedo: https://coolprop.org/fluid_properties/HumidAir.html

**Cierre pendiente:** aceptación visual/pedagógica en el equipo local, cotejo detallado con las figuras originales del PowerPoint y documentación paginada de normas disponibles bajo licencia. La compilación automatizada y las pruebas funcionales se documentan en GitHub Actions cuando concluyen, sin confundirlas con una certificación de ingeniería industrial.
