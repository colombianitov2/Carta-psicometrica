# Auditoría psicrométrica

## Fuentes de control

- ASHRAE, *Fundamentals of Psychrometrics (SI), Second Edition*, capítulos 2 y 3.
- ASHRAE Handbook—Fundamentals, psicrometría.
- PsychroLib como referencia de implementación de ecuaciones psicrométricas SI.
- Carta VALCON/Carrier aportada como referencia visual para escalas auxiliares.

## Siete propiedades fundamentales de la carta

La auditoría conserva las siete propiedades indicadas por ASHRAE:

1. Temperatura de bulbo seco.
2. Temperatura de bulbo húmedo.
3. Temperatura de punto de rocío.
4. Humedad relativa.
5. Entalpía.
6. Humedad absoluta o razón de humedad.
7. Volumen específico.

Las líneas de punto de rocío coinciden con las líneas horizontales de humedad absoluta; por eso no se duplica una segunda familia gráfica. El punto de rocío sí se calcula explícitamente en `03_Points/PointPropertyCalculator.cs`.

## Control numérico de referencia

Estado de prueba: 21 °C de bulbo seco, 60 % HR, 101.325 kPa.

Valores del material ASHRAE por lectura de carta:
- Punto de rocío: aproximadamente 13 °C.
- Entalpía: aproximadamente 45 kJ/kg de aire seco.
- Bulbo húmedo: aproximadamente 16 °C.
- Volumen específico: aproximadamente 0.846 m³/kg de aire seco.

Valores obtenidos con las ecuaciones usadas por FrioCalc:
- Punto de rocío: 12.947 °C.
- Entalpía: 44.745 kJ/kg de aire seco.
- Bulbo húmedo: 16.008 °C.
- Volumen específico: 0.845752 m³/kg de aire seco.

Resultado: los valores coinciden con la resolución gráfica esperada.

Control adicional: 21 °C y 50 % HR produce un bulbo húmedo aproximado de 14.60 °C, coherente con la lectura gráfica ASHRAE.

## Auditoría visual aplicada

- Entalpía: líneas sólidas oscuras; líneas principales con mayor grosor.
- Bulbo húmedo: líneas inclinadas punteadas.
- Humedad relativa: etiquetas escalonadas para reducir solapamiento.
- Volumen específico: líneas intermedias de 0.01 m³/kg en Carta estándar y rótulos principales cada 0.05 m³/kg.
- Volumen específico: valores situados en la intersección con el borde inferior.
- Desviación de entalpía: etiquetas separadas de las de humedad relativa.
- Factor de Calor Sensible: escala auxiliar exterior.
- Márgenes ampliados para evitar recortes de escalas auxiliares.

## Presión

Todas las capas se recalculan con la presión absoluta activa. Las correcciones no están fijadas a 101.325 kPa y se aplican también a los presets de altitud y a la presión personalizada.

## Puntos

Los puntos se calculan con la presión activa y muestran:
- bulbo seco;
- humedad absoluta;
- humedad relativa;
- bulbo húmedo;
- punto de rocío;
- entalpía;
- volumen específico;
- presión absoluta.

Al cambiar la presión se eliminan los puntos existentes para evitar presentar propiedades pertenecientes a otra presión.
