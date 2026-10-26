# 07_SelectionEngine

Motor de selección y dimensionamiento de FrioCalc inspirado en funciones observables y relaciones verificables recuperadas de Coolselector2 5.8.6.898, pero implementado como código nuevo y propio.

## Regla de procedencia

- DFM/SQL recuperado: estructura declarativa observada (formularios, etiquetas, campos, listas, tablas).
- Símbolos/TD32: nombres y tipos observados; no equivalen a cuerpos de código originales.
- Manual/documentación: ecuaciones, reglas o criterios explícitos en los manuales recuperados.
- Pseudocódigo automático: solo orienta investigación; no se trata como fuente original ni como algoritmo validado.
- Pendiente: no se implementa si falta una ecuación, coeficiente, unidad o semántica verificable.

## Primer módulo: cámaras frigoríficas

El flujo recuperado muestra estas etapas funcionales:

1. Load Wizard: elegir asistente o definición manual.
2. Dimensions and surroundings: dimensiones interiores/exteriores, temperatura de entorno, humedad relativa exterior, suelo aislado y temperatura bajo el suelo.
3. Goods: tipo de mercancía, masa total, masa renovada por día, temperatura de entrada y carga de respiración.
4. Review recommended values: temperatura y HR de cámara, horas de operación y paneles.
5. Review Cold Room Load: transferencia de calor, infiltración, mercancía, luces, ventiladores, personas, otras cargas y desescarche.
6. Condensing unit: región, refrigerante y criterios de selección.
7. Expansion valve: electrónica o termostática, material/conexión.
8. Operating conditions: revisión de capacidad y condiciones antes de selección.
9. Misc controls: paneles, entorno y alimentación eléctrica.

No se replica la marca, apariencia, catálogos de producto ni código propietario. El objetivo es conservar el modelo de decisión que sea útil para FrioCalc.

## Lo que todavía NO se implementa como cálculo completo

La propia recuperación indica que no están demostradas:

- ecuación completa de carga térmica de la cámara;
- correlación completa de infiltración Backstrom (1965);
- coeficientes completos para paredes, suelo, techo, personas, mercancía, ventiladores y resistencias;
- motor termofísico completo P/T/h/rho de refrigerantes;
- curvas de capacidad/eficiencia de compresores;
- curvas Kv/Cv y correlaciones completas de pérdida de presión;
- algoritmo exacto de selección de productos.

Esas partes permanecerán pendientes hasta que se respalden con bibliografía pública, normas o evidencia recuperada suficiente.