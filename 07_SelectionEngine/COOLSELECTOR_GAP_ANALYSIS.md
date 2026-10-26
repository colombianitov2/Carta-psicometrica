# Análisis de brechas FrioCalc vs alcance de Coolselector2

Fecha de revisión: 2026-10-26.

Este documento compara: (1) alcance oficial público de Coolselector2; (2) material recuperado en Drive; (3) módulos ya iniciados en FrioCalc. No implica copiar productos, catálogos o código propietario.

## A. Cámara frigorífica y carga térmica

### Ya representado como entradas/modelos
- dimensiones internas/externas;
- ambiente exterior y humedad relativa;
- temperatura bajo piso y piso aislado;
- producto, masa, rotación y temperatura de entrada;
- temperatura y HR de cámara;
- horas de operación;
- paneles;
- infiltración por puertas o renovaciones/día;
- luces;
- ventiladores;
- personas;
- otras cargas;
- desescarche básico;
- refrigerante, región y tipo de válvula de expansión.

### Falta programar como cálculo
- transmisión por paredes, techo y piso;
- puentes térmicos cuando aplique;
- infiltración sensible + latente;
- carga de producto antes de congelación;
- carga de congelación;
- carga posterior al congelamiento;
- respiración de frutas/vegetales;
- carga de personas;
- iluminación con horario de uso;
- ventiladores/motores del evaporador;
- otros motores o equipos eléctricos dentro de la cámara;
- calor de equipos/procesos internos;
- carga de desescarche;
- distribución de cargas por horas de operación;
- pull-down / tiempo de abatimiento;
- margen/seguridad configurable;
- capacidad frigorífica requerida final.

## B. Desescarche

### Ya tenemos
- natural y eléctrico en formularios recuperados;
- potencia de desescarche;
- cantidad de ciclos/día;
- duración por ciclo;
- relación documental de caudal de gas caliente.

### Falta
- método completo: natural/aire, eléctrico, gas caliente y salmuera caliente donde aplique;
- inicio por horario;
- inicio por intervalo;
- inicio por temperatura del evaporador;
- inicio por tiempo acumulado de compresor;
- inicio manual/entrada digital/red;
- terminación por tiempo;
- terminación por temperatura;
- sensor de terminación de desescarche;
- temperatura objetivo de terminación;
- tiempo mínimo y máximo de desescarche;
- pump-down previo;
- retardo de apertura de gas caliente;
- drip-off / goteo;
- drenaje y retardo de drenaje;
- calefactor de bandeja y retardo;
- operación de ventiladores durante desescarche;
- retardo de ventiladores después de desescarche;
- temperatura de arranque del ventilador;
- coordinación de varios evaporadores;
- desescarche adaptativo;
- cálculo de energía térmica/electrica de desescarche;
- efecto de humedad/infiltración sobre frecuencia de desescarche.

## C. Ventiladores, motores y cargas internas

Falta un modelo genérico de cargas por motores/equipos:
- cantidad;
- potencia nominal;
- eficiencia;
- factor de carga;
- horas de operación;
- ubicación del motor respecto al recinto;
- fracción de potencia que termina como calor dentro de la cámara;
- ventiladores del evaporador;
- bombas dentro del espacio;
- transportadores y maquinaria;
- resistencias/calefactores;
- iluminación con horario;
- personas con actividad y permanencia.

## D. Evaporador / air cooler

Falta:
- capacidad requerida;
- temperatura de evaporación/dew;
- superheat útil y adicional;
- temperatura de entrada de aire;
- diferencia media de temperatura;
- potencia de ventiladores;
- potencia de desescarche;
- número de evaporadores;
- dimensiones máximas;
- número mínimo de ventiladores;
- FPI / separación de aletas;
- método de desescarche;
- caudal de aire y selección térmica;
- caída de presión del aire si se incorpora;
- selección independiente de fabricante.

## E. Compresores y unidades condensadoras

Falta:
- aplicación MT/LT/HT/AC/calefacción;
- capacidad requerida;
- refrigerante;
- Tevap / dew;
- Tcond / dew;
- superheat útil/adicional;
- temperatura de retorno de gas;
- subcooling útil/adicional;
- frecuencia eléctrica y tensión;
- velocidad fija/variable;
- tipo de compresor;
- envolvente de operación;
- capacidad, potencia, COP/EER;
- descarga y temperatura de descarga;
- corriente eléctrica cuando haya datos;
- selección de uno o varios compresores;
- staging / paralelización;
- unidad condensadora y corrección por ambiente;
- funcionamiento a alta temperatura ambiente.

## F. Tuberías

Falta motor general para:
- línea de succión;
- línea de líquido;
- descarga;
- hot gas;
- retorno de aceite;
- elevadores/riser;
- material de tubería;
- diámetro interior/comercial;
- caudal másico;
- velocidad;
- caída de presión;
- caída equivalente de temperatura de saturación;
- longitud recta;
- accesorios y longitud equivalente;
- desnivel vertical;
- estados líquido/gas/bifásico;
- riesgo de flash gas;
- límites de retorno de aceite;
- uniones/ramificaciones;
- pérdida total del circuito.

## G. Válvulas y componentes de línea

Falta selección genérica de:
- TXV;
- EEV;
- válvulas solenoides;
- válvulas de retención;
- válvulas manuales/cierre;
- válvulas de regulación de presión;
- válvulas de bypass de gas caliente;
- válvulas de inyección;
- válvulas de agua;
- estaciones de válvulas;
- filtros/strainers;
- filter driers;
- burnout filters;
- mirillas;
- receptores;
- accesorios y conexiones.

## H. Controles, sensores y lógica de operación

Falta:
- control de temperatura y diferencial;
- sensores de aire entrada/salida;
- sensor de desescarche;
- transductores de presión;
- control de superheat;
- control de ventiladores;
- control de compresor;
- tiempos mínimos ON/OFF;
- pump-down;
- alarmas;
- monitoreo;
- comunicación/BMS;
- controladores electrónicos;
- alimentación eléctrica y compatibilidad de señales.

## I. Refrigerantes y motor termodinámico

Falta un motor verificable para:
- P-T;
- h, s, rho, v;
- calidad x;
- dew/bubble/mid;
- glide;
- superheat/subcooling;
- mezclas;
- estados bifásicos;
- límites críticos;
- diagramas log(p)-h;
- líneas de calidad;
- isentropas;
- procesos de compresión/expansión.

## J. Seguridad y válvulas de alivio

Falta:
- cálculo según norma seleccionada;
- EN 13136;
- ISO 5149;
- ASHRAE 15;
- escenarios de alivio;
- fuego/calor externo;
- compresor;
- fuentes internas;
- overflow;
- área/caudal de alivio;
- selección de válvula;
- contrapresión y descarga cuando aplique.

## K. CO2 transcrítico y sistemas avanzados

Falta:
- booster;
- válvula de alta presión;
- gas bypass;
- compresor paralelo;
- receptor/flash tank;
- eyectores HP;
- eyectores LP;
- eyectores de líquido;
- optimización de COP;
- control de alta presión;
- selección por condiciones transcríticas.

## L. Intercambiadores de calor

Falta:
- intercambiador interno líquido-succión;
- evaporadores;
- condensadores;
- gas coolers;
- economizadores;
- recuperadores;
- UA/LMTD/NTU según módulo;
- caída de presión;
- fase simple y bifásica.

## M. Diagramas y presentación de resultados

Falta:
- diagrama log(p)-h interactivo;
- estados numerados del ciclo;
- calidad, isentropas y líneas de referencia;
- tablas de resultados;
- comparación de alternativas;
- advertencias de operación;
- reporte técnico;
- lista de materiales propia;
- exportación PDF/CSV/Excel cuando corresponda.

## N. Orden recomendado de implementación

1. Carga térmica completa de cámara frigorífica.
2. Motores/ventiladores/personas/luces/equipos y horarios.
3. Desescarche completo y control asociado.
4. Motor termodinámico de refrigerantes.
5. Diagrama log(p)-h y ciclo frigorífico.
6. Evaporadores y condensadores.
7. Compresores y unidades condensadoras.
8. Tuberías y pérdidas.
9. Válvulas de expansión y componentes de línea.
10. Controladores, sensores y alarmas.
11. Seguridad/alivio.
12. CO2 transcrítico y eyectores.
13. Reportes, BOM y comparación de alternativas.