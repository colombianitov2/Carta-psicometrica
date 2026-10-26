# Matriz de fuentes — reconstrucción funcional

## 04_FORMULARIOS_REFRIGERACION.txt

Evidencia declarativa DFM recuperada. Confirma el flujo de entrada de cámara frigorífica:
- dimensiones y entorno;
- mercancía;
- revisión de valores recomendados;
- revisión de carga de cámara;
- unidad condensadora;
- válvula de expansión;
- condiciones de operación;
- controles varios.

También confirma campos de dimensiones, entorno, infiltración, mercancía, cargas adicionales, desescarche, región, refrigerante y tipo de válvula.

Uso en FrioCalc: reconstruir flujo de preguntas y modelo de datos.

## 07_TIPOS_Y_ESTRUCTURAS_DE_CALCULO.txt

Símbolos y tipos observados. Confirma conceptos útiles:
- almacenamiento: fresco, envasado, congelado;
- respiración: ninguna, vegetales, frutas;
- frecuencia de apertura de puertas: frecuente, regular, rara;
- criterios de selección: tamaño, velocidad, pérdida de presión, capacidad y caída de temperatura de saturación;
- referencia de saturación: rocío, media y burbuja;
- estado de fluido: gas, líquido y bifásico;
- estructura de mercancías con temperatura, HR, congelación, calores específicos, calor de congelación, respiración y parámetros de carga;
- estructura de paneles con espesor y valor U.

Límite: los símbolos/tipos no demuestran por sí solos las ecuaciones internas.

## 01_FORMULAS_DOCUMENTADAS_Y_LIMITES.txt

Relaciones documentales implementables:
- T_evap_media = T_cámara - ΔT_media.
- Sobrecalentamiento nominal = 0.65 × DT1.
- Límites documentados de DT1.
- T_entrada sugerida = T_almacenamiento + 5 K.
- masa total estimada = volumen × ocupación × densidad.
- masa diaria = masa total × renovación diaria.
- iluminación histórica = 8 W/m² bajo la condición documentada.
- máximo recomendado de operación: 21 h/día.

No se presenta este conjunto como motor completo de carga frigorífica.

## Pseudocódigo y matemática tipada

Se usan solo para localizar estructuras o hipótesis. La propia recuperación indica que no se demostró equivalencia numérica con el motor original. No se copiarán como algoritmos sin validación independiente.