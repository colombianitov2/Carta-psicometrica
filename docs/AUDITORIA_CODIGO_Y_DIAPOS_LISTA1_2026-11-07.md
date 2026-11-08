# Auditoría técnica independiente de FrioCalc — Carta psicrométrica y procesos

**Revisión:** 2026-11-07 (Colombia). **Rama:** `chatgpt/friocalc-lista1-2026-11-07`. **Estado:** candidato verificable; sin fusión a la versión anterior.

## 1. Alcance y material fuente

- Presentación original del usuario: **A03 Procesos Psicrométricos(2).pptx**, PhD. Hermes Ramírez León, **diapositivas 1–24**. La diapositiva 25 inicia un tema posterior, no incluido en esta Lista 1.
- Manual de referencia: **ASHRAE Handbook—Fundamentals 2025, Chapter 1, Psychrometrics** [fuente oficial](https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx).
- Validaciones complementarias: [CoolProp Humid Air](https://coolprop.org/fluid_properties/HumidAir.html), [PsychroLib](https://psychrometrics.github.io/psychrolib/api_docs.html) y [MathWorks, Modeling Moist Air Systems](https://www.mathworks.com/help/simscape/ug/modeling-moist-air-systems.html).
- Se realizaron lectura de código fuente C#/WPF, contraste de ecuaciones, compilaciones remotas reales en Windows, tests numéricos y capturas de UI. **No** se ha probado la aplicación interactuando en el PC físico del usuario.

## 2. Hallazgos y correcciones en código

| ID | Severidad | Problema comprobado | Corrección / evidencia |
|---|---|---|---|
| AU-01 | Alta | Se confundían «entropía» y «desviación de entalpía». Antes solo existía `EnthalpyDeviationLayer`. | Se conservó Δh separado y se añadió `MoistAirSpecificEntropy` + `SpecificEntropyLayer`, con casilla independiente y leyenda s*. |
| AU-02 | Alta | `EnthalpyLayer` verificaba los límites rectangulares pero no evitaba prolongar líneas por encima de HR=100%. | Se pasa presión absoluta a la rutina de cada entalpía y se rechaza W>Ws(T,P). Test WPF inspecciona todos los vértices de polilíneas a 101,325 y 84,54 kPa. |
| AU-03 | Media | La opción Carta estándar permitía zoom/desplazamiento pese a exigir límites fijos. | `ChartGeometry.ZoomAt` y `PanBy` no alteran rangos cuando `IsStandardChartMode` está activo; pruebas en ambos modos. |
| AU-04 | Media | El lector llamado «positivo o cero» aceptaba números negativos, confundiendo ADP/T de condensado con masa de agua añadida. | Separación entre entrada finita con signo y valores no negativos; agua de vapor y retirada por desecante no pueden ser negativas. |
| AU-05 | Media | Δh se dibujaba gris tenue con etiquetas inclinadas superpuestas y un título atravesando la carta. | Curvas turquesas segmentadas, etiquetas legibles Δh y título pequeño arriba del área gráfica; captura WPF independiente. |
| AU-06 | Media | La representación de s* no tenía unidades explícitas IP/SI ni procedencia del modelo. | `EntropyFromSI/ToSI`, unidades SI/IP en casilla, panel del punto e informes; nueva fuente B21 de bibliografía. |
| AU-07 | Media | Pruebas limitadas a pocos estados y controles de presencia. | Matriz de 48 estados con 4 presiones × 4 temperaturas × 3 HR, inversiones, sobresaturación, valores de s*, casos A03 y controles WPF de selección y unidades. |
| AU-08 | Baja | Había cobertura limitada de exportación y cambios de presión en pruebas UI. | Casos añadidos para exportación PNG y reinicio de puntos al aplicar una presión distinta. |

### AU-01 — Definición termodinámica de entropía relativa

La familia adicional s* tiene la expresión de mezcla ideal por kilogramo de aire seco:

```text
s* = cp,da ln(T/T*) − Rda ln(pda/pda*)
     + W [ cp,v ln(T/T*) − Rv ln(pv/pv*) ]
```

Referencias **convencionales**: `T*=273,15 K`, `pda*=101,325 kPa`, `pv*=0,611657 kPa`. Unidades internas: `kJ/(kg de aire seco·K)`. No es una entropía absoluta del modelo real RP-1485, ni tiene por qué coincidir numéricamente con los valores tabulados de CoolProp; se evalúan diferencias de estado con una misma referencia. Las isolíneas se construyen por contorno 2D dentro del dominio monofásico, sin extrapolar por encima de saturación.

**Advertencia importante:** ASHRAE Chart No. 1 no incluye las curvas de s* como una familia convencional; son una ampliación opcional de FrioCalc. La propiedad que sí existía, «desviación de entalpía», se conserva como corrección `Δh = h(Tbs,W) − hsat(Tbh)` del modelo empleado en la aplicación. La familia se identifica expresamente como **no equivalente a entropía**.

### AU-02 — Región de saturación

Las líneas de entalpía y entropía no deben interpretarse como propiedades de gas monofásico en la región sobresaturada (HR>100%). La nueva rutina descarta puntos fuera del dominio calculado a la presión absoluta activa. El trabajo de modelar niebla, líquido arrastrado y dos fases sigue fuera del alcance de la carta convencional programada.

## 3. Correspondencia comprobable con el PowerPoint A03

| Diapositivas | Contenido de la fuente | Implementación verificable |
|---|---|---|
| 1–3 | Introducción y familias de transformaciones | Zona 2 y selector de procesos |
| 4–5 | Calefacción y enfriamiento con W constante | Calor sensible en `ProcessCalculator` + laboratorio teórico |
| 6 | 5 m³/s, 35→25 °C, W=10 g/kgda | `SlideExamples.Evaluate(6)` y caso automático |
| 7 | 10.000 m³/h, 2 °C/80% HR→21 °C | `SlideExamples.Evaluate(7)` y caso automático |
| 8–13 | Humidificación, deshumidificación, vapor, lavador, desecantes, rendimiento del lavador | Modelos de vapor, evaporativo y desecante condicionado a datos; notas sobre efectividad |
| 14 | Lavador a 30 °C/10% HR, rendimiento 70% | `SlideExamples.Evaluate(14)`; se comprueba salida y posibilidad de HR=95% |
| 15 | Deshumidificación de 32 °C/Trocío 28 °C a 21 °C saturado | `SlideExamples.Evaluate(15)`, masa de condensado y distinción de carga del aire/serpentín |
| 16–21 | Enfriamiento + deshumidificación, calentamiento + humidificación, mezcla adiabática | Pestaña de procesos, segunda carta, mezclas en masa seca, rutas y ramas |
| 22 | Auditorio 300 personas × 8 L/s/pers, condiciones exterior/interior | `SlideExamples.Evaluate(22)`; calcula ventilación y diferencia de entalpía, **no inventa carga total** |
| 23 | Mezcla 3000 y 500 m³/h con v distinto | `SlideExamples.Evaluate(23)`; masa seca a partir de V̇/v |
| 24 | Auditorio con FCSi=0,75 y BF=0,20 | Guía geométrica FCS, modelo BF/ADP; **no inventa ADP, caudal, carga completa ni impulsión** |

La denominación docente «Rendimiento de Hendrickson» figura en la diapositiva 13, pero no se identificó una fuente técnica independiente que autorice elevar ese nombre a estándar. La implementación utiliza «efectividad de saturación» y anota la terminología del material docente.

## 4. Evidencia de pruebas

**Última validación completa comprobada: GitHub Actions [#42431421445](https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42431421445)**. Resultado: **compilación correcta con 0 errores y 0 advertencias; 504 pruebas numéricas y 48 pruebas WPF aprobadas**. La ejecución cargó artefacto con las tres zonas, capas de s* y Δh, y una exportación PNG de la segunda carta.

El test WPF inspecciona directamente los vértices dibujados por la capa de entalpía a **84,54 y 101,325 kPa** para verificar que ninguno está por encima de la humedad de saturación. La prueba confirma además que un cambio real de presión elimina puntos antiguos en ambas cartas y que se exporta un PNG no vacío.



- CI y registros oficiales: [GitHub Actions](https://github.com/colombianitov2/Carta-psicometrica/actions).
- Las pruebas se ejecutan en **Windows, .NET 10, WPF**, con advertencias tratadas como errores.
- Se evalúan propiedades, inversores, unidades, presión, diferencias de entalpía, entropía relativa, mezcla, vapor, evaporación, serpentina con condensado, estados inválidos, referencias ASHRAE y ejercicios del material A03.
- UI: creación de puntos, sincronización entre cartas, selección desde tarjeta, SI/IP, elementos de la Zona 2, etiquetas y trazados; se generan capturas en el escritorio virtual del runner.
- **La resolución gráfica automáticamente capturada es 1150×788.** No afirmar que el runner Windows ha validado el comportamiento real a 1540×900 en el PC local.
- Las capturas de UI no demuestran que todos los controles hayan recibido clics ni que se hayan completado rutas de usuario con grandes proyectos. La auditoría presencial es una etapa diferente.

## 5. Límites pendientes y por qué no puede declararse «perfecto»

1. **Certificación de ingeniería:** faltan contrastes independientes exhaustivos sobre todo el dominio de presiones/temperaturas, modelos reales de gas húmedo y comportamiento bifásico.
2. **Serpentines:** selección y dimensionamiento reales requieren curvas de fabricante, ADP, aire/agua, correcciones, pérdidas y condiciones de funcionamiento.
3. **Desecantes:** capacidad, isotermas, cinética y energía de regeneración dependen de datos reales de material y equipo. La aplicación calcula estados solo con temperatura y agua retirada proporcionadas.
4. **Carga completa de un auditorio:** diapositivas 22 y 24 omiten carga por envolvente, ocupación, iluminación, infiltración y otras condiciones de operación. Un número único sin hipótesis sería ficticio.
5. **Interacción humana final:** controles de pantalla completa, navegación a otras resoluciones, series extensas de procesos, impresión/exportaciones grandes y comparación gráfica detallada con la carta oficial.
6. **Bibliografía:** la fuente electrónica ASHRAE tiene capítulos/ecuaciones, pero no se deben inventar páginas de un PDF o manual impreso no comprobado.

## 6. Procedimiento para auditoría local

1. Mantener intacta la rama anterior y no fusionar este candidato.
2. Usar `06_Tools/ABRIR-AUDITORIA-LISTA1.cmd` dentro de la copia separada `FrioCalc_Lista1_Auditoria`.
3. Ejecutar el conjunto de pruebas automatizadas y abrir Visual Studio.
4. Seguir los 30 casos de `docs/GUIA_AUDITORIA_VISUAL_LISTA1.md` en una única sesión y registrar incidencias.
5. No adoptar el sistema para seleccionar equipos industriales hasta completar la validación técnica pertinente.

**Conclusión:** hay una candidata WPF considerablemente más controlada y medible. El objetivo «perfecto» no es demostrable solo con estas pruebas y fuentes, por lo que las limitaciones quedan expresas y son criterios de cierre, no características ocultas.
