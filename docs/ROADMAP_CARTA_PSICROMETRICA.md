# Roadmap de Carta psicométrica

Este roadmap organiza el alcance funcional completo por fases. Los puntos normativos o que dependan de bibliografía técnica adicional deben quedar pendientes hasta validar fuentes.

## Fase 1: motor básico, dos puntos y proceso A→B

Estado: iniciado en esta entrega.

- 8. Configuración de presión atmosférica.
- 11. Curva de saturación.
- 12. Líneas de temperatura de bulbo seco.
- 13. Líneas de razón de humedad.
- 14. Curvas de humedad relativa.
- 15. Líneas de temperatura de bulbo húmedo.
- 16. Líneas de entalpía.
- 17. Líneas de volumen específico.
- 18. Protractor o escala de factor de calor sensible.
- 39. Calcular propiedades desde presión ambiente.
- 40. Calcular desde temperatura de bulbo seco.
- 42. Calcular desde humedad relativa.
- 46. Calcular entalpía.
- 47. Calcular presión parcial de vapor.
- 48. Calcular presión de vapor saturado.
- 49. Calcular calor específico.
- 50. Calcular volumen específico.
- 51. Calcular densidad.
- 52. Selección inicial de dos puntos A y B.
- 53. Cambio de estado de A hacia B.
- 54. Línea de proceso dibujada.
- 55. Calentamiento sensible.
- 56. Enfriamiento sensible.
- 57. Humidificación.
- 58. Deshumidificación.
- 60. Calentamiento/enfriamiento con humidificación/deshumidificación.
- 62. Flujo de aire seco.
- 63. Relación Δh/ΔW.
- 64. Carga de calefacción.
- 65. Carga de enfriamiento.
- 66. Humidificación en g/s o kg/h según configuración.
- 67. Calor sensible.
- 68. Calor latente.
- 69. Calor total.
- 70. Factor de calor sensible.

## Fase 2: carta gráfica interactiva

- 1. Carta psicrométrica interactiva.
- 2. Visualización moderna equivalente a HTML5/JavaScript/SVG, adaptada a WPF.
- 3. Carta tipo ASHRAE.
- 4. Carta tipo Mollier i-x.
- 5. Carta con unidades SI e IP.
- 6. Cambio de unidades de temperatura, presión, volumen específico, razón de humedad, entalpía y velocidad.
- 7. Configuración de límites visibles de la carta.
- 9. Configuración por presión al nivel del mar.
- 10. Configuración por altitud.
- 19. Regla de entalpía.
- 27. Selección de punto haciendo clic sobre la carta.
- 28. Entrada manual de propiedades del aire.
- 29. Indicador de posición.
- 30. Modo normal y modo preciso.
- 31. Agregar punto.
- 32. Editar punto.
- 33. Nombrar punto.
- 34. Mover punto.
- 35. Eliminar punto.
- 36. Limpiar puntos.
- 41. Calcular desde razón de humedad.
- 43. Calcular desde temperatura de bulbo húmedo.
- 44. Calcular desde temperatura de punto de rocío.
- 45. Calcular desde temperatura de saturación.

## Fase 3: procesos completos y mezcla

- 52. Selección robusta de dos puntos A y B.
- 53. Cambio de estado de A hacia B.
- 54. Línea de proceso dibujada.
- 55. Calentamiento sensible.
- 56. Enfriamiento sensible.
- 57. Humidificación.
- 58. Deshumidificación.
- 59. Humidificación/deshumidificación adiabática.
- 60. Procesos combinados de calor y humedad.
- 61. Todos los procesos superpuestos.
- 62. Flujo de aire seco.
- 63. Relación Δh/ΔW.
- 64. Carga de calefacción.
- 65. Carga de enfriamiento.
- 66. Humidificación en g/s.
- 67. Calor sensible.
- 68. Calor latente.
- 69. Calor total.
- 70. Factor de calor sensible.
- 71. Mezcla de aire entre dos estados.
- 72. Porcentaje de mezcla del punto A.
- 73. Estado resultante de mezcla: TBS, TBH, punto de rocío, HR, W y entalpía.

## Fase 4: ciclos de aire

- 74. Ciclo de aire primario.
- 75. Ciclo de aire secundario.
- 76. Condiciones interiores: TBS y HR.
- 77. Condiciones exteriores: TBS y HR.
- 78. Diferencia de temperatura de ventilación.
- 79. Relación de aire fresco.
- 80. Carga térmica.
- 81. Carga húmeda.
- 82. Flujo de ventilación.
- 83. Selección verano/invierno.
- 84. Máxima diferencia de temperatura de ventilación.
- 85. Temperatura de ventilación.
- 86. Humedad relativa de ventilación.
- 87. Punto de rocío del aparato.
- 88. Carga de enfriamiento del ciclo.
- 89. Carga de calefacción del ciclo.
- 90. Flujo de aire fresco.
- 91. Flujo de retorno.
- 92. Flujo primario.
- 93. Flujo secundario.
- 94. Dibujo del ciclo primario.
- 95. Dibujo del ciclo secundario.

## Fase 5: datos climáticos EPW/CSV

- 96. Cargar estación meteorológica.
- 97. Buscar estación meteorológica.
- 98. Cargar archivo climático.
- 99. Cargar archivos EPW.
- 100. Cargar archivos CSV.
- 101. Usar datos de ejemplo.
- 102. Mostrar datos horarios.
- 103. Mostrar rangos mensuales.
- 104. Mostrar contorno diario.
- 105. Mostrar grilla distribuida.
- 106. Mapeo de resultados de EnergyPlus.
- 107. Selección de métrica a visualizar.
- 108. Filtro por año, mes, día, temporada, hora y día de semana.
- 109. Filtro por valores superiores, inferiores o entre umbrales.
- 110. Promedio, mínimo, máximo, tiempo sobre umbral, tiempo bajo umbral y tiempo entre valores.
- 111. Eje X configurable.
- 112. Eje Y configurable.
- 113. Escala de datos bloqueable.

## Fase 6: confort térmico y overlays normativos

Pendiente de bibliografía normativa antes de implementar.

- 116. Sin overlay de confort.
- 117. Carta bioclimática de Givoni.
- 118. Outdoor Work Heat Index.
- 119. ISO 7730 / PMV.
- 120. ASHRAE 55.
- 121. EN 15251.
- 122. Mostrar PMV.
- 123. Temperatura media exterior.
- 124. Día del año.
- 125. Potencial de exposición solar.
- 126. Velocidad del aire.
- 127. Nivel de ropa, clo.
- 128. Tasa metabólica, met.
- 129. Temperatura radiante media.
- 130. Recalcular temperatura media exterior móvil de 30 días con datos horarios.
- 131. Predecir aislamiento de ropa a partir de temperatura exterior.
- 132. Resumen de puntos dentro/fuera de zonas de confort.

## Fase 7: ciclos multiestado y conexión con ciclo de refrigeración

- 133. Cálculo y trazado de cambios encadenados A→B, B→C, C→A y ciclos cerrados, incluyendo relación conceptual con evaporador, compresor, condensador, válvula de expansión y retorno al evaporador cuando corresponda.

## Fase 8: exportación, configuraciones y validación

- 20. Edición de colores, estilo y espesor de líneas.
- 21. Configuración de fondo, marco, líneas de proceso y elementos gráficos.
- 22. Exportación de carta como SVG.
- 23. Exportación de carta como PNG.
- 24. Guardado y carga de configuraciones.
- 25. Edición de configuración en JSON.
- 26. Configuración predeterminada del usuario.
- 37. Importar datos de puntos.
- 38. Exportar datos de puntos.
- 114. Exportar datos psicrométricos como JSON.
- 115. Exportar datos psicrométricos como CSV.
- Validación cruzada con ASHRAE Handbook Fundamentals, PsychroLib y casos patrón.
- Pruebas automatizadas ampliadas para presión, altitud, límites de carta y procesos.
