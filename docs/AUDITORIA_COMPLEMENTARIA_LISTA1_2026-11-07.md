# FrioCalc — Auditoría complementaria de robustez, interacción y seguridad de sesiones

**Fecha:** 7 de noviembre de 2026 (America/Bogota). **Rama:** `chatgpt/friocalc-lista1-2026-11-07`. **Verificación más reciente:** [GitHub Actions #42436851686](https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42436851686), ejecutada en Windows con .NET 10 y WPF.

## Dictamen comprobable

**Aprobación automatizada:** compilación con 0 errores y 0 advertencias, 519 pruebas numéricas y 76 comprobaciones WPF. La evidencia gráfica contiene cinco vistas, incluidas ambas familias de curvas y las tres zonas, más una exportación PNG de la carta de procesos. Las capturas automáticas se tomaron en el escritorio virtual a 1150×788.

**No equivale** a certificación termodinámica ni de usabilidad del 100%. Los métodos avanzados sin datos de equipo conservan una salida explícitamente indeterminada o condicionada.

## Defectos confirmados y resueltos en esta auditoría

| ID | Gravedad | Defecto | Corrección y comprobación |
|---|---|---|---|
| AU-09 | Media | Tres propiedades coherentes pero redondeadas podían generar dos raíces casi iguales y un falso error de ambigüedad. | Agrupación por resolución de entrada y mínimo residuo normalizado. Prueba: Tbs=25°C, HR=60%, W redondeada a 0,1 g/kgda; caso contradictorio continúa rechazado. |
| AU-10 | Alta | El constructor de puntos podía intentar calcular presión de saturación fuera del intervalo admitido, violando el contrato `TryCreatePoint`. | Rechazo sin lanzar excepción de Tbs fuera de [−100,200] °C; extensión del solucionador hasta 120°C, coherente con límite de carta extendida. |
| AU-11 | Media | El cálculo de bypass deshumidificador podía representar ganancia de humedad para aire muy seco con rocío indefinido. | Rechazo de `W_ADP > W_entrada` cuando BF<1, adicional al chequeo de Trocío. |
| AU-12 | Alta | El clic derecho eliminaba un punto utilizado por procesos sin solicitar confirmación. | Evento `PointDeletionRequested` y confirmación previa si existen procesos asociados. |
| AU-13 | Media | Modificar solo metadatos del punto invalidaba sus procesos aun cuando T, W y P permanecían iguales. | Comparación de estados físicos, no igualdad completa de registros. |
| AU-14 | Alta | El guardado de aire seco (`W=0`) fallaba porque el punto de rocío es indefinido (`NaN`) y JSON no admite NaN numérico. | Serialización explícita de valores flotantes nombrados y validación de retorno; prueba real de ida/vuelta con W=0. |
| AU-15 | Alta | Un archivo JSON podía incluir propiedades derivadas alteradas o color de proceso inválido; la importación validaba principalmente entalpía. | `SessionFileIntegrity`: recálculo de HR, bulbo húmedo, rocío, h y v; IDs, referencias, colores y parámetros; rechazo de sesiones inconsistentes. |
| AU-16 | Media | Guardar en archivo sin permisos o con falla de disco no disponía de captura controlada de errores. | Bloque de guardado con mensajes de fallo y sin finalizar inesperadamente la aplicación. |
| AU-17 | Media | La pestaña de procesos podía quedar desincronizada de las casillas al cargar un proceso para editar. | `PointPropertiesPanel.SetSelection` restituye selección y orden; comprobado mediante test WPF de interacción. |
| AU-18 | Alta | Editar un proceso predictivo incluía la salida calculada entre los puntos de entrada e impedía recalcular; podía crear salidas huérfanas. | `GeneratedPointId` y parámetros persistentes por tipo; al editar se seleccionan solo entradas y la salida anterior se retira únicamente si no la usa otro proceso. |
| AU-19 | Media | Una opción inválida de ciclo se comprobaba después de crear una salida predictiva y podía dejar puntos no utilizados. | Validación de cierre antes de cualquier creación predictiva. |
| AU-20 | Media | Etiquetas de puntos cercanos se superponían sobre otras y sobre los marcadores. | Ubicación por candidatos alrededor de puntos, minimización de solapes y recorte dentro de carta. Prueba de tres estados próximos. |

## Alcance de las pruebas funcionales en Windows

1. **519 comprobaciones numéricas** incluyen matrices en cuatro presiones, humedad y temperatura, saturación, referencias de ASHRAE, balances de vapor/condensado/mezcla, inversores, entropía relativa, unidades, casos A03 y sesiones manipuladas.
2. **76 comprobaciones WPF** incluyen inicialización, segunda carta, clic en selección, SI/IP, gráficos de entropía y entalpía, exportación PNG, cambio de presión, creación/eliminación/deshacer procesos, edición predictiva, persistencia de parámetros, eliminación de salida antigua y etiquetas próximas.
3. **Capturas a 1150×788** en tres zonas, más isolíneas de s* y Δh, generadas por la aplicación en el runner de Windows. No constituyen prueba de todos los clics o resoluciones.
4. **Pruebas de regresión del PowerPoint A03, diapositivas 1–24.** La resolución de cargas totales de equipo sigue restringida a parámetros realmente proporcionados; ninguna regla inferirá potencia, ADP o regeneración sin datos suficientes.

## Restricciones de alcance aún vigentes

- No se ha comprobado manualmente toda la interacción en el PC del usuario, ni a otras resoluciones de pantalla, ni en sesiones de miles de procesos reales.
- La entropía s* es un **modelo relativo ideal**, distinto de la entropía absoluta/tabulada de aire húmedo real y de la desviación de entalpía Δh.
- Serpentines, desecantes y dimensionamiento industrial requieren las curvas y propiedades reales del fabricante. Un cálculo de estado o un balance de aire no constituye selección de equipo.
- El comportamiento bifásico con niebla o agua arrastrada no está modelado en esta carta.
- La bibliografía electrónica incluye capítulos y ecuaciones donde están confirmados; no asignar números de páginas impresas sin documento paginado.
- No se realizó ninguna fusión con `main`; la versión anterior continúa sin cambios.

## Fuentes primarias de cálculo

- [ASHRAE Handbook—Fundamentals 2025, capítulo 1, Psychrometrics](https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx).
- [PsychroLib: API y fórmulas de psicrometría](https://psychrometrics.github.io/psychrolib/api_docs.html).
- [CoolProp: Humid Air Properties](https://coolprop.org/fluid_properties/HumidAir.html).
- Material docente aportado: `A03 Procesos Psicrométricos(2).pptx`, diapositivas 1–24.

## Inspección final guiada

Para una revisión local sin alterar la versión antigua, ejecutar `06_Tools/ABRIR-AUDITORIA-LISTA1.cmd` desde el worktree `FrioCalc_Lista1_Auditoria` y seguir `docs/GUIA_AUDITORIA_VISUAL_LISTA1.md`. Registrar cualquier diferencia con los valores de referencia y el commit exacto probado.

**Dictamen:** candidata de ingeniería apta para auditoría manual concentrada, con regresiones automatizadas aprobadas. No certificada como «perfecta» ni como herramienta de selección industrial sin calibración de modelos y aprobación del usuario.
