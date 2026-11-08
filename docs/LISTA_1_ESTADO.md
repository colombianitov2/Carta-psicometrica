# Lista 1 — Estado de construcción de FrioCalc

**Versión de seguimiento:** 1.3 — candidata de auditoría · **Fecha:** 7 de noviembre de 2026 · **Rama:** `chatgpt/friocalc-lista1-2026-11-07`.

**No se ha fusionado con `main` ni con la rama psicrométrica anterior.** Este registro diferencia compilación correcta, implementación funcional parcial y auditoría visual pendiente. No debe interpretarse como aprobación final del software.

## Resumen del avance

- **45/58 actividades:** código presente; falta comprobar su aceptación visual y casos límite.
- **8/58 actividades:** avance parcial, con pendientes técnicos/documentales especificados.
- **5/58 actividades:** validaciones complementarias de entorno, comportamiento o cobertura.

### Evidencia de integración

- Auditoría WPF con tres capturas reales a 1150×788 (carta, procesos y teoría). Los otros tamaños deben inspeccionarse en el computador local; el runner no permite simular válidamente 1540×900. Se corrigieron la distribución y la carta estándar antes de la ejecución final.
- [Artefacto con tres capturas en Actions #42427980132](https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42427980132): descargar desde los artefactos de la ejecución.


- Validación Windows GitHub Actions **#42427980132**: 0 errores, 0 advertencias, **89 pruebas numéricas** y **25 pruebas de inicialización UI** correctas.
- Enlace: https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42427980132
- Flujo controlado: solo se ejecuta mediante solicitud manual o al modificar `.github/validation-request.txt`; los commits ordinarios no desencadenan compilación.
- Carpeta de auditoría local: `06_Tools/Auditar-FrioCalc-Lista1.ps1` con evidencias fuera del repositorio.
- **Limitación:** el éxito de pruebas no sustituye pruebas humanas de interacción, inspección de carta ni verificación con documentación paginada.

## Estado por identificador

### A. Carta y arquitectura

| ID | Actividad | Estado actual |
|---|---|---|
| L1.01 | Sincronización remota/local | Parcial; faltan modelos, casos o referencias |
| L1.02 | Compilación y ejecución | Verificación complementaria pendiente |
| L1.03 | Auditoría de ecuaciones | Parcial; faltan modelos, casos o referencias |
| L1.04 | Modelo unificado de estados y procesos | Código presente; falta auditoría visual de aceptación |
| L1.05 | Selector de unidades SI/IP | Código presente; falta auditoría visual de aceptación |
| L1.06 | Conversiones de unidades | Código presente; falta auditoría visual de aceptación |
| L1.07 | Carta a distintas presiones | Verificación complementaria pendiente |
| L1.08 | Zoom, etiquetas y Carta estándar | Verificación complementaria pendiente |
| L1.09 | Casilla de FCS independiente | Código presente; falta auditoría visual de aceptación |

### B. Puntos y propiedades

| ID | Actividad | Estado actual |
|---|---|---|
| L1.10 | Campos numéricos junto a propiedades | Código presente; falta auditoría visual de aceptación |
| L1.11 | Entradas Tbs, Tbh, Trocío, HR, W, h, v | Código presente; falta auditoría visual de aceptación |
| L1.12 | Solucionador inverso de estados | Código presente; falta auditoría visual de aceptación |
| L1.13 | Método directo/iterativo y contraste | Código presente; falta auditoría visual de aceptación |
| L1.14 | Condiciones insuficientes y redundantes | Código presente; falta auditoría visual de aceptación |
| L1.15 | Imposibilidades físicas y rango | Parcial; faltan modelos, casos o referencias |
| L1.16 | Validación de decimales, signos y unidades | Código presente; falta auditoría visual de aceptación |
| L1.17 | Ubicar punto y limpiar solo tras éxito | Código presente; falta auditoría visual de aceptación |
| L1.18 | Clic derecho con mismo cálculo base | Código presente; falta auditoría visual de aceptación |
| L1.19 | Colores e identificadores de puntos | Código presente; falta auditoría visual de aceptación |
| L1.20 | Selección múltiple de puntos | Código presente; falta auditoría visual de aceptación |
| L1.21 | Eliminar punto seleccionado | Código presente; falta auditoría visual de aceptación |
| L1.22 | Editar, duplicar y deshacer | Código presente; falta auditoría visual de aceptación |
| L1.23 | Propiedades, método y datos originales | Código presente; falta auditoría visual de aceptación |
| L1.24 | Cambios de presión seguros para estados | Código presente; falta auditoría visual de aceptación |

### C. Procesos y ciclos

| ID | Actividad | Estado actual |
|---|---|---|
| L1.25 | Pestaña con segunda carta | Código presente; falta auditoría visual de aceptación |
| L1.26 | Orden de puntos reordenable | Código presente; falta auditoría visual de aceptación |
| L1.27 | Graficar y agrupar proceso | Código presente; falta auditoría visual de aceptación |
| L1.28 | Color del proceso desde primer punto | Código presente; falta auditoría visual de aceptación |
| L1.29 | Administración de varios procesos | Código presente; falta auditoría visual de aceptación |
| L1.30 | Método físico por proceso | Código presente; falta auditoría visual de aceptación |
| L1.31 | Parámetros adicionales por equipo | Código presente; falta auditoría visual de aceptación |
| L1.32 | Calor sensible | Código presente; falta auditoría visual de aceptación |
| L1.33 | Humidificación por vapor | Código presente; falta auditoría visual de aceptación |
| L1.34 | Humidificación evaporativa | Código presente; falta auditoría visual de aceptación |
| L1.35 | Deshumidificación con condensado | Código presente; falta auditoría visual de aceptación |
| L1.36 | Deshumidificación por desecantes | Parcial; faltan modelos, casos o referencias |
| L1.37 | Procesos combinados de varias etapas | Código presente; falta auditoría visual de aceptación |
| L1.38 | Mezcla adiabática de múltiples entradas | Código presente; falta auditoría visual de aceptación |
| L1.39 | Ramas, bypass y ciclos | Código presente; falta auditoría visual de aceptación |
| L1.40 | FCS y guía gráfica de impulsión | Código presente; falta auditoría visual de aceptación |
| L1.41 | Modelo ideal BF/ADP | Código presente; falta auditoría visual de aceptación |
| L1.42 | Balance energético y de agua completo | Parcial; faltan modelos, casos o referencias |
| L1.43 | Trazos, flechas y resultados gráficos | Código presente; falta auditoría visual de aceptación |

### D. Ejercicios del material docente

| ID | Actividad | Estado actual |
|---|---|---|
| L1.44 | Diapositivas 6–7 | Código presente; falta auditoría visual de aceptación |
| L1.45 | Diapositivas 14–15 | Código presente; falta auditoría visual de aceptación |
| L1.46 | Diapositivas 22–24 y límites del modelo | Parcial; faltan modelos, casos o referencias |
| L1.47 | Advertencia ante carga indeterminada | Código presente; falta auditoría visual de aceptación |

### E. Zona 2 — Teoría

| ID | Actividad | Estado actual |
|---|---|---|
| L1.48 | Segunda zona de teoría | Código presente; falta auditoría visual de aceptación |
| L1.49 | Catálogo de 20 variables y métodos | Código presente; falta auditoría visual de aceptación |
| L1.50 | Laboratorio gráfico interactivo | Código presente; falta auditoría visual de aceptación |
| L1.51 | Ejercicios explicados y calculables | Código presente; falta auditoría visual de aceptación |

### F. Bibliografía, pruebas y entrega

| ID | Actividad | Estado actual |
|---|---|---|
| L1.52 | Trazabilidad por fórmula/página | Parcial; faltan modelos, casos o referencias |
| L1.53 | Documentos oficiales y enlaces | Código presente; falta auditoría visual de aceptación |
| L1.54 | Pruebas de conservación y regresión | Verificación complementaria pendiente |
| L1.55 | Auditoría visual y UI | Verificación complementaria pendiente |
| L1.56 | Sesiones JSON con propiedades | Código presente; falta auditoría visual de aceptación |
| L1.57 | Informes CSV, Markdown, PNG | Código presente; falta auditoría visual de aceptación |
| L1.58 | GitHub, VS y auditoría integral | Parcial; faltan modelos, casos o referencias |

## Cambios incorporados después de la validación inicial

- Resolución de pares redundantes y controles de coherencia de estados.
- Desecante con humedad retirada y temperatura de salida **aportadas**, sin inventar propiedades ni regeneración de fabricante.
- Divisor de flujo en dos ramas con balance por rama y fracción ingresada.
- Edición del proceso en el mismo identificador, sin duplicar el proceso al volver a graficarlo.
- Colores de procesos visibles en ambas cartas y anillos diferenciados para estados compartidos.
- Cálculo de condensado, guía FCS, modelos ideales de vapor y bypass, mezclas y ciclos cerrados.
- ASHRAE Fundamentals 2025, capítulo 1 de acceso HTML y capítulo 32 sobre desecantes, con ecuaciones verificadas. Son 20 fichas bibliográficas.
- Capturas de las tres zonas generadas desde Windows en GitHub Actions; mejora responsive: segunda carta a ancho completo y control de capas.
- Carta estándar sincronizada en ambas pestañas desde el inicio.
- Las pruebas numéricas contrastan también ejemplos publicados del capítulo 1 de ASHRAE 2025.

**Criterio de aceptación:** «Código presente» no implica que una función esté certificada para dimensionamiento de equipos. Su aceptación visual y documental es parte de la auditoría única solicitada.

## Pendientes bloqueantes para dar la lista por terminada

1. **Validación física externa:** las pruebas incluyen ejemplos cuantitativos de ASHRAE 2025 a 101,325 y 84,54 kPa. Sigue pendiente la validación sistemática en todo el dominio, la región bajo cero, pares inversos degenerados y comparación con modelos de gas real.
2. **Modelos de equipos:** desecantes y regeneración, geometrías reales de serpentín, cargas completas de auditorio y redes multietapa; solicitar las variables faltantes en lugar de inferirlas.
3. **Aceptación visual:** el flujo de Windows genera tres capturas reales a 1150×788 (tres zonas). La inspección de 1540×900 queda para Visual Studio en el computador del usuario; el runner restringe el tamaño de su ventana. Faltan pruebas humanas de clics, scroll, recorte de textos, FCS, cambio SI/IP, exportación y ciclos.
4. **Edición avanzada de procesos:** se conserva el identificador del proceso al editar; aún debe auditarse el tratamiento de puntos de salida generados, estados compartidos y operaciones de deshacer.
5. **Bibliografía:** el registro incluye fuentes y capítulos, pero no inventa páginas no verificadas del manual ASHRAE. La denominación «Hendrickson» permanece pendiente de corroboración externa.
6. **Integración local:** sincronización con la carpeta de Visual Studio y ejecución de la app en el computador del usuario todavía por confirmar.

## Reglas de cierre

- Cada corrección debe preservar la arquitectura y numeración de clases, indicar ID L1.xx, confirmar compilación y registrar prueba.
- Mantener el código local antiguo intacto hasta que el usuario complete una **única auditoría consolidada** y apruebe la fusión.
- No usar esta rama para diseñar sistemas de seguridad o dimensionar equipos industriales sin validar los modelos y rangos aplicables.
- Fuentes: ASHRAE Handbook—Fundamentals 2025, cap. 1 (https://www.ashrae.org/technical-resources/publications-library/ashrae-handbook/table-of-contents-2025-ashrae-handbook-fundamentals); PsychroLib (https://psychrometrics.github.io/psychrolib/api_docs.html); CoolProp (https://coolprop.org/fluid_properties/HumidAir.html); presentación A03, diapositivas 1–24.
