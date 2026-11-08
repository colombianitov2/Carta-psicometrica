# FrioCalc — Entrega candidata Lista 1, 7 de noviembre de 2026

**Repositorio:** https://github.com/colombianitov2/Carta-psicometrica

**Rama candidata aislada:** `chatgpt/friocalc-lista1-2026-11-07`

**No fusionada con `main` ni con `chatgpt/friocalc-psychrometrics`.**

**Validación más reciente:** [GitHub Actions #42447079032](https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42447079032), con 694 comprobaciones numéricas y 106 de WPF, compilación sin errores ni advertencias. El código adicional de integridad de sesiones y edición predictiva forma parte de esta misma rama. Consultar la [auditoría complementaria](AUDITORIA_COMPLEMENTARIA_LISTA1_2026-11-07.md).

**Extensión educativa Zona 2 (versión 1.6):** [manual técnico y pedagógico](ZONA2_TEORIA_INTERACTIVA.md), diez escenarios, explicaciones al modificar controles, ecuaciones con valores sustituidos, diccionario, autoevaluación y ejemplos A03 conectados. La aceptación pedagógica y la revisión visual en el equipo local siguen pendientes.

**Ampliación aprobada del contenido A03:** 31 temas, 12 capítulos de fundamentos de carta y **36/36 diapositivas cubiertas** en el curso navegable. Nuevos apartados 25–36 incluyen climatización, ADP/BF/RSHF/GSHF y auditorio con serpentín y agua fría. [Mapa pedagógico](ZONA2_COBERTURA_CARTA_A03_COMPLETA.md). El objetivo es comprensión interactiva, no una reproducción íntegra del manual ASHRAE.

## Estado de la construcción

La carta y el motor de puntos/procesos están integrados para una auditoría concentrada. La auditoría independiente de la distinción entropía/entalpía y de los límites físicos está disponible en [AUDITORIA_CODIGO_Y_DIAPOS_LISTA1_2026-11-07.md](AUDITORIA_CODIGO_Y_DIAPOS_LISTA1_2026-11-07.md). Hay dos zonas de aplicación: (1) carta, puntos y procesos; (2) teoría, ejemplos y referencias. Los procesos incluyen estados medidos y salidas calculadas cuando se aportan los parámetros físicos requeridos.

**Funciones implementadas para revisión:**
- Presión absoluta con diferentes cartas; control Carta estándar y selector SI/IP.
- Visibilidad de curvas, incluido FCS; entrada de condiciones de estado opcionales y solucionador inverso directo/iterativo.
- Alta, edición, duplicación, selección, eliminación y deshacer puntos.
- Puntos coloreados con identificación única; procesos con color del primer estado, también en estados compartidos.
- Secuencias, mezclas adiabáticas de N entradas, división de dos ramas y cierre de recorridos.
- Calor sensible, vapor inyectado, humidificación evaporativa ideal, deshumidificación conocida con condensado, desecante definido por entrada/salida y bypass/ADP.
- Gestión de varios procesos, cambio de orden, edición por ID, visibilidad, informes y persistencia JSON.
- Zona de teoría con laboratorio gráfico interactivo, ejemplos docentes y registro de 20 propiedades/métodos.
- Exportación de resultados CSV, Markdown, PNG, sesiones JSON y bibliografía CSV.

## Integración verificada al cierre

**GitHub Actions #42447079032** — ejecución Windows con resultado satisfactorio.

- Compilación `net10.0-windows` con advertencias como errores: **0 errores y 0 advertencias**.
- **694 pruebas numéricas**: estados inversos, conservación, modelos, unidades, contraste ASHRAE 2025 y condiciones imposibles.
- **106 pruebas WPF de inicialización, selección, SI/IP, geometría, exportación y presión**, y tres capturas efectivas de la interfaz a 1150×788. No se ha verificado automáticamente 1540×900 en el runner. La captura automática de GitHub está limitada al escritorio virtual de 1150×788. La comprobación a 1540×900 se realiza en Visual Studio local.
- La validación se realizó sobre la rama aislada; no hubo fusión.
- Consultar [evidencias de la compilación y PNG](https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42447079032).

## Verificación técnica

El flujo `.github/workflows/wpf-build.yml` compila WPF en Windows .NET 10 con advertencias como errores, ejecuta pruebas numéricas y pruebas de inicialización de la interfaz, y captura tres imágenes PNG cuando el entorno lo permite.

Compilación **bajo solicitud explícita**: no se ejecuta por commits ordinarios, para reducir notificaciones de GitHub.

La validación numérica incluye cuatro ejemplos y comparación de cartas 1 y 6 del capítulo 1 de ASHRAE 2025 (101,325 y 84,54 kPa). No certifica exactitud en todo el dominio de presión, temperatura y humedad.

## Fuentes verificadas

1. ASHRAE Handbook—Fundamentals 2025, Chapter 1, Psychrometrics: https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch01/F25_Ch01_si.aspx
   - §6, ecuaciones (21)–(32): W, HR, volumen y entalpía de gas ideal
   - §7, ecuación (33): bulbo húmedo termodinámico
   - §9: cartas y transportador FCS
   - §10, ecuaciones (43)–(47): calentamiento, serpentín con condensado, mezcla e inyección de agua/vapor
2. ASHRAE Handbook—Fundamentals 2025, Chapter 32, Sorbents and Desiccants: https://handbook.ashrae.org/Handbooks/F25/SI/F25_Ch32/F25_Ch32_si.aspx
3. PsychroLib: https://psychrometrics.github.io/psychrolib/api_docs.html
4. CoolProp HumidAir: https://coolprop.org/fluid_properties/HumidAir.html
5. A03 Procesos Psicrométricos, diapositivas 1–24, material docente aportado por el usuario.

## Límites de ingeniería y alcance pendiente

- **Desecantes:** se calcula salida únicamente si se proporciona temperatura y agua retirada. La capacidad, isotermas, potencia de regeneración y dimensionamiento deben proceder de datos de producto.
- **Serpentines:** la carga corregida incluye entalpía del condensado solo cuando se introduce temperatura del mismo; el cálculo no sustituye un catálogo/curva real de serpentín.
- **BF/ADP y humidificación:** modelos ideales con hipótesis declaradas, no selección de equipos.
- **Mezclas sobresaturadas:** se rechazan; requieren modelo separado de condensación.
- **Redes de procesos:** la división en ramas permite balances, pero no calcula pérdidas de tuberías, ventiladores ni componentes que no pertenezcan al modelo psicrométrico.
- **Bibliografía:** las referencias HTML verifican números de ecuación; no se inventan páginas de PDF o del libro físico.
- **Terminología:** se conserva «humedad absoluta» como etiqueta de interfaz solicitada, pero W con g/kgda corresponde técnicamente a razón de humedad.
- **Aceptación visual en PC:** debe verificarse en resolución y configuración local, incluidos scroll, zoom, formatos decimales, etiquetas y exportación.

## Preparación inicial y siguientes actualizaciones

La **primera vez**, desde la carpeta original ejecute los comandos de la sección siguiente para crear una copia separada. Después, para actualizar y auditar en un solo paso, haga doble clic en `FrioCalc_Lista1_Auditoria\\06_Tools\\ABRIR-AUDITORIA-LISTA1.cmd`. El script comprueba la rama y si hay modificaciones locales antes de realizar `git pull --ff-only`; nunca descarta archivos del proyecto original.

## Auditoría consolidada en Windows

Ejecutar desde PowerShell en la carpeta local principal (no reemplaza su rama actual):

```powershell
cd 'D:\Proyectos de desarrollo de Software\FrioCalc_Psychrometrics_Git'
git fetch origin
git worktree add --track -b chatgpt/friocalc-lista1-2026-11-07 '..\FrioCalc_Lista1_Auditoria' origin/chatgpt/friocalc-lista1-2026-11-07
cd '..\FrioCalc_Lista1_Auditoria'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.\06_Tools\Auditar-FrioCalc-Lista1.ps1' -AbrirEnVisualStudio
```

Si la rama local ya existe, primero consultar `git worktree list`; no repetir la creación con `-b`.

La auditoría automatizada conserva evidencias en **Documentos\FrioCalc_Auditorias**, sin borrar los archivos de investigación que el usuario tiene en la carpeta original. Ver también [GUIA_AUDITORIA_VISUAL_LISTA1.md](GUIA_AUDITORIA_VISUAL_LISTA1.md), con 30 casos de inspección humana.

**Regla de fusión:** la versión anterior permanece intacta hasta recibir la aprobación final basada en los resultados de esa única auditoría.
