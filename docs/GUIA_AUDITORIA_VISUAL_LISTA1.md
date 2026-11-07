# FrioCalc — Auditoría consolidada de la Lista 1
Fecha de preparación: 2026-11-07.
Apertura: copia **separada** en rama chatgpt/friocalc-lista1-2026-11-07. No fusionar con main ni modificar la copia antigua hasta aprobación.

## 0. Preparación de una copia independiente

En PowerShell, desde el repositorio base:

```powershell
cd 'D:\Proyectos de desarrollo de Software\FrioCalc_Psychrometrics_Git'
git fetch origin
git worktree add --track -b chatgpt/friocalc-lista1-2026-11-07 '..\FrioCalc_Lista1_Auditoria' origin/chatgpt/friocalc-lista1-2026-11-07
cd '..\FrioCalc_Lista1_Auditoria'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File '.\06_Tools\Auditar-FrioCalc-Lista1.ps1' -AbrirEnVisualStudio
```

Si la rama ya está creada como rama local, no ejecute el comando worktree con -b de nuevo. Verifique primero con `git branch --list` y `git worktree list`.

Los informes se guardan en **Documentos\FrioCalc_Auditorias\Lista1_aaaaMMdd_HHmmss**, fuera del repositorio; las pruebas no modifican los archivos originales.

## 1. Lista de comprobación en una sesión

| Prueba | Acción | Esperado / evidencia |
|---|---|---|
| A01 | Compilar .NET 10 con warnings-as-errors | 0 errores y 0 advertencias |
| A02 | Ejecutar pruebas de consola | Estados, presión, mezcla, unidades, condensado, guardado sin excepciones |
| A03 | Ejecutar prueba de inicialización UI | Tres zonas y controles WPF disponibles |
| A04 | Presión 101,325 kPa → valor personalizado | Curvas, puntos y títulos responden; no usa presión manométrica |
| A05 | Carta estándar, desplazamiento, zoom | Límites visuales estables |
| A06 | Casilla FCS on/off y Default/None | Escala FCS se oculta sin afectar puntos ni otras familias |
| A07 | Cambiar SI ↔ IP | Las magnitudes se convierten, no cambia el estado físico; comprobar presión psi |
| A08 | Introducir Tbs=25 °C, HR=60 % | Se obtiene un estado coherente y se limpian entradas |
| A09 | Tbs=25 °C sin otro dato | Rechazo por información insuficiente; conserva campos |
| A10 | Tbs=25 °C, HR=60 % y h imposible | Rechazo por inconsistencia; no agrega punto |
| A11 | Método directo frente al iterativo | Valores compatibles dentro de tolerancia declarada |
| A12 | Clic derecho para crear/eliminar punto | Mismo motor de propiedades físicas, color/ID únicos |
| A13 | Editar, duplicar, borrar y deshacer | Persistencia de identidad y resultado sin estados obsoletos |
| A14 | Seleccionar P1→P2→P3, variar orden | Flechas y cálculo corresponden a orden real |
| A15 | Crear dos procesos de colores diferentes | Conservar ambos y sus leyendas; borrar solo el seleccionado |
| A16 | Dibujar FCS 0,75 desde estado de habitación | Guía geométrica sin punto de impulsión inventado |
| A17 | Mezclar 3000 y 500 m³/h; dividir un flujo en dos ramas | Convertir por v; verificar masa y energía de mezcla; introducir fracción de rama y verificar balance de cada salida |
| A18 | Humidificar con vapor y usar desecante | Registrar parámetros, HR, h y agua aportada; desecante requiere humedad retirada y Tbs de salida proporcionadas, sin inferir regeneración |
| A19 | Humidificar evaporativamente con ε=0,70 y ε=1 | A ε=1 alcanzar saturación, sin valores fuera de 100% |
| A20 | Deshumidificar con y sin temperatura de condensado | Q de aire y Q de serpentín diferenciadas |
| A21 | BF=0,2 y ADP definido | Obtener salida ideal solo con suficientes datos |
| A22 | Cerrar recorrido de 3+ estados | Trazado cerrado; advertir que Δh neta 0 no implica potencia nula de equipos |
| A23 | Guardar, cerrar y abrir sesión JSON | P, SI/IP, métodos, entradas y procesos se recuperan |
| A24 | Exportar CSV, Markdown y PNG | Archivos legibles, unidades, fórmulas, advertencias y nombres |
| A25 | Zona 2: cada tipo de proceso con deslizador | No representar estados imposibles; comprobar cambio de P |
| A26 | Zona 2: visor bibliográfico 18 entradas | Referencias y enlaces oficiales; exportar CSV |
| A27 | Diapositivas A03 6,7,14,15,22,23,24 | Cifras, unidades e hipótesis reproducibles |
| A28 | Auditorio sin carga de envolvente | Advertencia de carga total indeterminada |
| A29 | Capturas y navegación a 1150×788 y 1540×900 | Sin controles cortados; scroll, divisores ajustables, modo estándar, leyendas y controles de capas legibles |
| A30 | Abrir nuevamente versión anterior | Sin alteraciones por la rama de auditoría |

## 2. Límites que deben indicarse en el informe final

- Modelo de vapor y BF ideal sin dimensionamiento detallado de equipos.
- Desecantes: no calcular regeneración ni eficacia de material sin parámetros/características.
- Mezcla sobresaturada: no inventar condensación; pedir modelo o entrada.
- Sin flujo másico no hay potencia. Cuando se condensa, entalpía del condensado solo si el dato se proporciona.
- Contenido docente A03: solo diapositivas 1–24; no copiar imágenes protegidas a informes públicos.
- La guía FCS no es un estado de impulsión único.
- Validar rangos y configuración de comparación con PsychroLib/CoolProp/ASHRAE.

## 3. Registro de incidencias
Anote ID Axx, screenshot, inputs, presión, sistema SI/IP, resultado observado, resultado esperado, severidad y commit auditado. No modificar fuentes originales de referencia durante la auditoría.
