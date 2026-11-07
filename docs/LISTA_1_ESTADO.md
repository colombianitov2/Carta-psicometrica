# Lista 1 — Estado de construcción de FrioCalc

**Versión de seguimiento:** 1.1 · **Fecha:** 7 de noviembre de 2026 · **Rama:** `chatgpt/friocalc-lista1-2026-11-07`.

**No se ha fusionado con `main` ni con la rama psicrométrica anterior.** Este registro diferencia compilación correcta, implementación funcional parcial y auditoría visual pendiente. No debe interpretarse como aprobación final del software.

## Resumen del avance

- **34/58 actividades:** código presente; falta comprobar su aceptación visual y casos límite.
- **19/58 actividades:** avance parcial, con pendientes técnicos/documentales especificados.
- **5/58 actividades:** validaciones complementarias de entorno, comportamiento o cobertura.

### Evidencia de integración

- Validación Windows GitHub Actions **#42415621386**: 0 errores, 0 advertencias, **49 pruebas numéricas** y **18 pruebas de inicialización UI** correctas.
- Enlace: https://github.com/colombianitov2/Carta-psicometrica/actions/runs/42415621386
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
| L1.05 | Selector de unidades SI/IP | Parcial; faltan modelos, casos o referencias |
| L1.06 | Conversiones de unidades | Parcial; faltan modelos, casos o referencias |
| L1.07 | Carta a distintas presiones | Verificación complementaria pendiente |
| L1.08 | Zoom, etiquetas y Carta estándar | Verificación complementaria pendiente |
| L1.09 | Casilla de FCS independiente | Código presente; falta auditoría visual de aceptación |

### B. Puntos y propiedades

| ID | Actividad | Estado actual |
|---|---|---|
| L1.10 | Campos numéricos junto a propiedades | Código presente; falta auditoría visual de aceptación |
| L1.11 | Entradas Tbs, Tbh, Trocío, HR, W, h, v | Código presente; falta auditoría visual de aceptación |
| L1.12 | Solucionador inverso de estados | Código presente; falta auditoría visual de aceptación |
| L1.13 | Método directo/iterativo y contraste | Parcial; faltan modelos, casos o referencias |
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
| L1.29 | Administración de varios procesos | Parcial; faltan modelos, casos o referencias |
| L1.30 | Método físico por proceso | Parcial; faltan modelos, casos o referencias |
| L1.31 | Parámetros adicionales por equipo | Parcial; faltan modelos, casos o referencias |
| L1.32 | Calor sensible | Código presente; falta auditoría visual de aceptación |
| L1.33 | Humidificación por vapor | Código presente; falta auditoría visual de aceptación |
| L1.34 | Humidificación evaporativa | Código presente; falta auditoría visual de aceptación |
| L1.35 | Deshumidificación con condensado | Parcial; faltan modelos, casos o referencias |
| L1.36 | Deshumidificación por desecantes | Parcial; faltan modelos, casos o referencias |
| L1.37 | Procesos combinados de varias etapas | Parcial; faltan modelos, casos o referencias |
| L1.38 | Mezcla adiabática de múltiples entradas | Código presente; falta auditoría visual de aceptación |
| L1.39 | Ramas, bypass y ciclos | Parcial; faltan modelos, casos o referencias |
| L1.40 | FCS y guía gráfica de impulsión | Código presente; falta auditoría visual de aceptación |
| L1.41 | Modelo ideal BF/ADP | Código presente; falta auditoría visual de aceptación |
| L1.42 | Balance energético y de agua completo | Parcial; faltan modelos, casos o referencias |
| L1.43 | Trazos, flechas y resultados gráficos | Parcial; faltan modelos, casos o referencias |

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
| L1.49 | Catálogo de 18 variables y métodos | Código presente; falta auditoría visual de aceptación |
| L1.50 | Laboratorio gráfico interactivo | Código presente; falta auditoría visual de aceptación |
| L1.51 | Ejercicios explicados y calculables | Código presente; falta auditoría visual de aceptación |

### F. Bibliografía, pruebas y entrega

| ID | Actividad | Estado actual |
|---|---|---|
| L1.52 | Trazabilidad por fórmula/página | Parcial; faltan modelos, casos o referencias |
| L1.53 | Documentos oficiales y enlaces | Parcial; faltan modelos, casos o referencias |
| L1.54 | Pruebas de conservación y regresión | Verificación complementaria pendiente |
| L1.55 | Auditoría visual y UI | Verificación complementaria pendiente |
| L1.56 | Sesiones JSON con propiedades | Código presente; falta auditoría visual de aceptación |
| L1.57 | Informes CSV, Markdown, PNG | Código presente; falta auditoría visual de aceptación |
| L1.58 | GitHub, VS y auditoría integral | Parcial; faltan modelos, casos o referencias |

## Pendientes bloqueantes para dar la lista por terminada

1. **Validación física externa:** contrastar datos de diferentes presiones y temperaturas con referencias ASHRAE, PsychroLib y CoolProp, con tolerancias cuantificadas; comprobar especialmente nieve/hielo, saturación, soluciones inversas y mezclas.
2. **Modelos de equipos:** desecantes y regeneración, geometrías reales de serpentín, cargas completas de auditorio y redes multietapa; solicitar las variables faltantes en lugar de inferirlas.
3. **Aceptación visual:** escala FCS, entradas, colores, zonas, fuentes, tamaño de letra, solapamientos, cambio SI/IP, cartas a distintas presiones, exportación y ciclos.
4. **Edición avanzada de procesos:** el botón cargar para editar crea una nueva versión del proceso; no sustituye todavía el proceso original en sitio.
5. **Bibliografía:** el registro incluye fuentes y capítulos, pero no inventa páginas no verificadas del manual ASHRAE. La denominación «Hendrickson» permanece pendiente de corroboración externa.
6. **Integración local:** sincronización con la carpeta de Visual Studio y ejecución de la app en el computador del usuario todavía por confirmar.

## Reglas de cierre

- Cada corrección debe preservar la arquitectura y numeración de clases, indicar ID L1.xx, confirmar compilación y registrar prueba.
- Mantener el código local antiguo intacto hasta que el usuario complete una **única auditoría consolidada** y apruebe la fusión.
- No usar esta rama para diseñar sistemas de seguridad o dimensionar equipos industriales sin validar los modelos y rangos aplicables.
- Fuentes: ASHRAE Handbook—Fundamentals 2025, cap. 1 (https://www.ashrae.org/technical-resources/publications-library/ashrae-handbook/table-of-contents-2025-ashrae-handbook-fundamentals); PsychroLib (https://psychrometrics.github.io/psychrolib/api_docs.html); CoolProp (https://coolprop.org/fluid_properties/HumidAir.html); presentación A03, diapositivas 1–24.
