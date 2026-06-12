# [ En desarrollo] Carta psicométrica

Carta psicométrica para cálculo avanzado de propiedades del aire húmedo, procesos HVAC, ciclos de aire, visualización gráfica, datos climáticos, exportación, confort térmico y análisis de estados. Este repositorio será la base de desarrollo de un módulo independiente, completo y ampliable.

## Descripción completa del alcance

Este proyecto desarrollará una carta psicométrica avanzada capaz de calcular puntos, procesos, ciclos de aire, datos climáticos, confort térmico, exportaciones y cambios entre estados dentro de la misma carta. El alcance inicial se basa en una lista fusionada de funciones tomadas como referencia funcional de herramientas psicrométricas interactivas existentes, eliminando duplicados y agregando el cálculo de ciclos multiestado.

## Alcance funcional completo

### A. Carta psicrométrica e interfaz gráfica

1. Carta psicrométrica interactiva.
2. Visualización en navegador con soporte HTML5, JavaScript y SVG.
3. Carta tipo ASHRAE.
4. Carta tipo Mollier i-x.
5. Carta con unidades SI e IP.
6. Cambio de unidades de temperatura, presión, volumen específico, razón de humedad, entalpía y velocidad.
7. Configuración de límites visibles de la carta.
8. Configuración de presión atmosférica.
9. Configuración por presión al nivel del mar.
10. Configuración por altitud.
11. Curva de saturación.
12. Líneas de temperatura de bulbo seco.
13. Líneas de razón de humedad.
14. Curvas de humedad relativa.
15. Líneas de temperatura de bulbo húmedo.
16. Líneas de entalpía.
17. Líneas de volumen específico.
18. Protractor o escala de factor de calor sensible.
19. Regla de entalpía.
20. Edición de colores, estilo y espesor de líneas.
21. Configuración de fondo, marco, líneas de proceso y elementos gráficos.
22. Exportación de carta como SVG.
23. Exportación de carta como PNG.
24. Guardado y carga de configuraciones.
25. Edición de configuración en JSON.
26. Configuración predeterminada del usuario.

### B. Entrada y cálculo de puntos psicrométricos

27. Selección de punto haciendo clic sobre la carta.
28. Entrada manual de propiedades del aire.
29. Indicador de posición.
30. Modo normal y modo preciso.
31. Agregar punto.
32. Editar punto.
33. Nombrar punto.
34. Mover punto.
35. Eliminar punto.
36. Limpiar puntos.
37. Importar datos de puntos.
38. Exportar datos de puntos.
39. Calcular propiedades desde presión ambiente.
40. Calcular desde temperatura de bulbo seco.
41. Calcular desde razón de humedad.
42. Calcular desde humedad relativa.
43. Calcular desde temperatura de bulbo húmedo.
44. Calcular desde temperatura de punto de rocío.
45. Calcular desde temperatura de saturación.
46. Calcular entalpía.
47. Calcular presión parcial de vapor.
48. Calcular presión de vapor saturado.
49. Calcular calor específico.
50. Calcular volumen específico.
51. Calcular densidad.

### C. Procesos psicrométricos entre puntos

52. Selección de dos puntos A y B.
53. Cambio de estado de A hacia B.
54. Línea de proceso dibujada.
55. Calentamiento sensible.
56. Enfriamiento sensible.
57. Humidificación.
58. Deshumidificación.
59. Humidificación/deshumidificación adiabática.
60. Calentamiento/enfriamiento con humidificación/deshumidificación.
61. Todos los procesos superpuestos.
62. Flujo de aire seco.
63. Relación Δh/ΔW.
64. Carga de calefacción.
65. Carga de enfriamiento.
66. Humidificación en g/s.
67. Calor sensible.
68. Calor latente.
69. Calor total.
70. Factor de calor sensible.
71. Mezcla de aire entre dos estados.
72. Porcentaje de mezcla del punto A.
73. Estado resultante de mezcla: TBS, TBH, punto de rocío, HR, W y entalpía.

### D. Ciclos HVAC de aire

74. Ciclo de aire primario.
75. Ciclo de aire secundario.
76. Condiciones interiores: TBS y HR.
77. Condiciones exteriores: TBS y HR.
78. Diferencia de temperatura de ventilación.
79. Relación de aire fresco.
80. Carga térmica.
81. Carga húmeda.
82. Flujo de ventilación.
83. Selección verano/invierno.
84. Máxima diferencia de temperatura de ventilación.
85. Temperatura de ventilación.
86. Humedad relativa de ventilación.
87. Punto de rocío del aparato.
88. Carga de enfriamiento del ciclo.
89. Carga de calefacción del ciclo.
90. Flujo de aire fresco.
91. Flujo de retorno.
92. Flujo primario.
93. Flujo secundario.
94. Dibujo del ciclo primario.
95. Dibujo del ciclo secundario.

### E. Datos climáticos, archivos y mapeo

96. Cargar estación meteorológica.
97. Buscar estación meteorológica.
98. Cargar archivo climático.
99. Cargar archivos EPW.
100. Cargar archivos CSV.
101. Usar datos de ejemplo.
102. Mostrar datos horarios.
103. Mostrar rangos mensuales.
104. Mostrar contorno diario.
105. Mostrar grilla distribuida.
106. Mapeo de resultados de EnergyPlus.
107. Selección de métrica a visualizar.
108. Filtro por año, mes, día, temporada, hora y día de semana.
109. Filtro por valores superiores, inferiores o entre umbrales.
110. Promedio, mínimo, máximo, tiempo sobre umbral, tiempo bajo umbral y tiempo entre valores.
111. Eje X configurable.
112. Eje Y configurable.
113. Escala de datos bloqueable.
114. Exportar datos psicrométricos como JSON.
115. Exportar datos psicrométricos como CSV.

### F. Confort térmico y overlays

116. Sin overlay de confort.
117. Carta bioclimática de Givoni.
118. Outdoor Work Heat Index.
119. ISO 7730 / PMV.
120. ASHRAE 55.
121. EN 15251.
122. Mostrar PMV.
123. Temperatura media exterior.
124. Día del año.
125. Potencial de exposición solar.
126. Velocidad del aire.
127. Nivel de ropa, clo.
128. Tasa metabólica, met.
129. Temperatura radiante media.
130. Recalcular temperatura media exterior móvil de 30 días con datos horarios.
131. Predecir aislamiento de ropa a partir de temperatura exterior.
132. Resumen de puntos dentro/fuera de zonas de confort.

### G. Ciclos multiestado dentro de la misma carta

133. Cálculo y trazado de cambios entre estados encadenados dentro de la misma carta: A→B, B→C, C→A y ciclos cerrados. El sistema debe permitir representar procesos asociados a evaporador, compresor, condensador, válvula de expansión y retorno al evaporador, vinculando los estados del aire en la carta psicrométrica con el análisis general del ciclo de refrigeración cuando corresponda.

## Bibliografía y fuentes técnicas por descargar o actualizar

### Prioridad 1 — Motor psicrométrico y validación

1. 2025 ASHRAE Handbook—Fundamentals, SI Edition.
2. PsychroLib — documentación oficial y código C#.
3. ASHRAE Fundamentals of Psychrometrics, SI, Second Edition.

### Prioridad 2 — Confort térmico y overlays normativos

4. ANSI/ASHRAE Standard 55-2023 — Thermal Environmental Conditions for Human Occupancy.
5. ISO 7730:2025 — Ergonomics of the thermal environment.
6. EN 16798-1:2019 / versión nacional vigente — Indoor environmental input parameters.
7. CEN/TR 16798-2 — Technical Report de interpretación de EN 16798-1.
8. CBE Thermal Comfort Tool — documentación/código como referencia de validación.

### Prioridad 3 — Ventilación, IAQ, aire exterior y ciclos

9. ANSI/ASHRAE Standard 62.1-2022 — Ventilation and Acceptable Indoor Air Quality.
10. CIBSE KS17 — Indoor Air Quality & Ventilation.
11. VDI 6022 Blatt 1 — Hygiene requirements for ventilation and air-conditioning systems.

### Prioridad 4 — Datos climáticos y archivos meteorológicos

12. ANSI/ASHRAE Standard 169 — Climatic Data for Building Design Standards.
13. ASHRAE Weather Data Center / datos climáticos 2025.
14. EnergyPlus Weather / EPW Data Dictionary.
15. Climate.OneBuilding EPW weather files.
16. CIBSE Weather Data / Guide J / TM49, según disponibilidad.

### Prioridad 5 — CIBSE y VDI como bloques normativos alternativos

17. CIBSE Guide A — Environmental Design.
18. CIBSE TM52 — The limits of thermal comfort: avoiding overheating.
19. CIBSE TM59 — Design methodology for the assessment of overheating risk in homes.
20. VDI 2078 — Calculation of thermal loads and room temperatures.
21. VDI 3803 Blatt 1 — Central air-conditioning systems.
22. VDI 3803 Blatt 2/4/5/6.
23. VDI 2081 Blatt 1 y Blatt 2 — Noise generation and noise reduction.
24. VDI 2082 — Sales outlets.

### Prioridad 6 — Overlays especiales

25. Givoni — Building Bioclimatic Chart / Comfort, climate analysis and building design guidelines.
26. OSHA/NIOSH o fuente oficial equivalente para Heat Index de trabajo exterior.

## Temas que requieren bibliografía adicional o actualización

1. Overlays de confort: ASHRAE 55, ISO 7730, EN 15251/EN 16798.
2. PMV, PPD y disconfort local con ISO vigente.
3. Confort adaptativo y sobrecalentamiento CIBSE.
4. CIBSE TM52 y TM59.
5. VDI 2078: cargas térmicas y temperaturas interiores.
6. VDI 6022: higiene en sistemas de ventilación y aire acondicionado.
7. VDI 3803: sistemas centrales/descentralizados, filtros, recuperación y ductos.
8. VDI 2081: ruido en sistemas HVAC.
9. Datos climáticos normalizados ASHRAE 169 / Weather Data Center 2025.
10. Importación EPW/CSV y mapeo EnergyPlus.
11. Carta bioclimática de Givoni.
12. Outdoor Work Heat Index.
13. Ciclos primario/secundario con ventilación normativa.
14. Actualización del ASHRAE Handbook—Fundamentals a edición 2025 SI.

## Estado del repositorio

Proyecto en desarrollo. La primera etapa debe enfocarse en construir el motor psicrométrico verificable, los modelos de puntos, las propiedades completas por estado y el trazado básico de procesos entre puntos. Las funciones normativas, climáticas y de confort deben agregarse por etapas, verificando cada módulo con bibliografía técnica y pruebas de cálculo.