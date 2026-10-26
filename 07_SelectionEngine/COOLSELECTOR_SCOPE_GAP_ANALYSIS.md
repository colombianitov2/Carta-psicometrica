# Auditoría de alcance Coolselector2 vs FrioCalc

Fecha de comparación: 2026-10-26.

## Fuentes comparadas

- Documentación oficial pública de Coolselector2 y guía de cámaras frigoríficas.
- Material recuperado en Drive mediante REA/Codex.
- Estado actual de la rama chatgpt/coolselector-functional-rebuild.

## A. Carga térmica de cámara frigorífica

### A1. Transmisión por envolvente — PENDIENTE
- paredes;
- techo;
- piso;
- puertas;
- puentes/condiciones especiales si una fuente los respalda;
- panel estándar o U personalizado;
- temperatura ambiente y temperatura bajo piso;
- radiación/sol cuando corresponda.

### A2. Infiltración / renovación de aire — PENDIENTE
- temperatura exterior;
- HR exterior;
- aperturas de puerta: frecuente/regular/rara;
- alternativa por renovaciones de aire por día;
- carga sensible y latente del aire infiltrado;
- método Backstrom solo si se obtiene la correlación verificable.

### A3. Carga de producto — PENDIENTE PRINCIPAL
- masa total;
- masa diaria que entra;
- temperatura de entrada;
- temperatura objetivo;
- calor específico por encima del punto de congelación;
- punto de congelación/descongelación del producto;
- calor latente de congelación;
- calor específico del producto congelado;
- enfriamiento sensible antes de congelar;
- congelación/solidificación;
- enfriamiento sensible después de congelar;
- respiración de frutas/vegetales;
- tiempo de abatimiento/pull-down cuando aplique.

### A4. Cargas eléctricas internas — PENDIENTE
- iluminación;
- motores de evaporador/ventiladores;
- otros motores o maquinaria dentro de cámara;
- potencia eléctrica;
- cantidad de equipos;
- horas de funcionamiento;
- factor de carga/duty cycle cuando la fuente lo permita;
- conversión a carga térmica efectiva dentro del recinto.

### A5. Personas — PENDIENTE
- número de personas;
- tiempo dentro de cámara;
- carga sensible;
- carga latente;
- dependencia con temperatura/actividad cuando la bibliografía lo respalde.

### A6. Desescarche del evaporador — PENDIENTE PRINCIPAL
- activar/desactivar desescarche;
- natural / aire / eléctrico / gas caliente donde corresponda;
- potencia del calentador;
- calentador de bandeja/drenaje;
- número de ciclos al día;
- duración de cada ciclo;
- energía diaria de desescarche;
- fracción de esa energía que llega a la cámara;
- efecto sobre capacidad requerida y horas disponibles de refrigeración;
- caudal de gas caliente: relación documental ya implementada;
- criterios de drenaje y retorno de condensado cuando haya fuente verificable.

### A7. Otras cargas — PENDIENTE
- resistencias anticondensación;
- calentadores de marco/puerta;
- maquinaria;
- equipos eléctricos;
- cualquier carga personalizada.

### A8. Horas de operación / capacidad requerida — PARCIAL
- horas de operación por día ya modeladas;
- referencia documental de <=21 h/día ya implementada;
- falta convertir carga diaria total en capacidad frigorífica requerida considerando tiempo efectivo de operación y desescarche.

## B. Evaporador / air cooler — PENDIENTE

- capacidad frigorífica;
- refrigerante;
- temperatura de evaporación a punto de rocío;
- sobrecalentamiento útil;
- temperatura de condensación;
- temperatura de líquido/subenfriamiento;
- diferencia media de temperatura;
- estimación de potencia de ventiladores;
- estimación de potencia total de desescarche;
- número de evaporadores;
- restricciones físicas: largo/ancho/alto;
- número mínimo de ventiladores;
- FPI/separación de aletas;
- método de desescarche;
- longitud de tubería a unidad condensadora.

## C. Compresores y unidades condensadoras — PENDIENTE

- aplicación MT/LT/HT/AC/calefacción;
- refrigerante;
- capacidad requerida;
- T/P de evaporación;
- T/P de condensación;
- sobrecalentamiento;
- subenfriamiento;
- temperatura de succión;
- temperatura de descarga;
- caudal másico;
- potencia eléctrica;
- COP/EER;
- corriente;
- tensión/frecuencia/fases;
- control de capacidad / velocidad;
- mapa de operación/envelope;
- selección por condiciones ambiente;
- región y alimentación eléctrica.

## D. Válvula de expansión — PENDIENTE

- TXV;
- EEV;
- expansión manual/flotador donde aplique;
- refrigerante;
- capacidad;
- presión/temperatura de evaporación y condensación;
- subenfriamiento;
- caída de presión;
- sobrecalentamiento;
- selección de orificio/valve size;
- conexión/material;
- controlador, sensor de presión, sensor de temperatura y accesorios para EEV.

## E. Tuberías — PENDIENTE PRINCIPAL

- línea de aspiración;
- línea de líquido;
- descarga/hot gas;
- retorno de aceite;
- gas caliente de desescarche;
- caudal másico;
- velocidad;
- pérdida de presión;
- caída equivalente de temperatura de saturación;
- longitud equivalente;
- accesorios/codos/reductores;
- tramos verticales/riser;
- estado gas/líquido/bifásico;
- flash gas;
- criterios de diámetro;
- selección por tamaño, velocidad o pérdida de presión;
- regla de uniones ya implementada.

## F. Válvulas y componentes de línea — PENDIENTE

- solenoides;
- válvulas de control/regulación;
- check valves;
- stop/shut-off;
- filtros secadores;
- burnout filters;
- strainer;
- mirillas;
- reguladores de presión;
- válvulas de bypass de gas caliente;
- válvulas de 4 vías;
- pilotos;
- estaciones ICF / equivalentes funcionales;
- conexiones y fittings;
- criterios Kv/Cv, caída de presión, capacidad y estado del fluido.

## G. Intercambiadores de calor — PENDIENTE

- evaporadores;
- condensadores;
- intercambiadores internos;
- intercambiadores líquido-aspiración;
- balance de energía;
- UA/LMTD/epsilon-NTU cuando el módulo correspondiente se desarrolle;
- pérdidas de presión;
- propiedades de los dos fluidos.

## H. Refrigerantes / motor termodinámico — PENDIENTE CRÍTICO

- propiedades P-T-h-s-rho;
- bubble/dew/midpoint;
- glide;
- calidad;
- líquido subenfriado;
- vapor sobrecalentado;
- bifásico;
- mezclas;
- diagramas log(p)-h;
- ciclo frigorífico;
- cálculo de estados;
- base verificable independiente del motor propietario.

## I. Seguridad y alivio — PENDIENTE

- EN 13136;
- ISO 5149;
- ASHRAE 15;
- recipiente expuesto a calor externo;
- descarga/overflow de compresor;
- líquido atrapado;
- capacidad requerida de válvula;
- capacidad de válvula seleccionada;
- línea de entrada;
- línea de descarga;
- pérdida de presión permitida según norma;
- cambio de válvula/changeover cuando corresponda.

## J. Controles, sensores y electrónica — PENDIENTE

- control de temperatura;
- control de desescarche;
- control de ventiladores;
- control de sobrecalentamiento;
- sensores de temperatura;
- transmisores de presión;
- presostatos;
- alarmas;
- detección de refrigerante;
- alimentación eléctrica;
- controladores programables cuando aplique;
- lógica de compatibilidad entre sensor/controlador/actuador.

## K. Sistemas avanzados — PENDIENTE

- CO2 transcrítico;
- booster;
- ejectores HP/LP/LE;
- gas bypass;
- control de alta presión;
- sistemas industriales;
- componentes en serie;
- cálculo de ramificaciones/uniones;
- sistemas con recuperación de calor cuando haya fuente.

## L. Herramientas de ingeniería / UX — PENDIENTE

- conversión completa de unidades;
- condiciones de operación en SI/International/US;
- diagramas log(p)-h;
- gráficos de rendimiento;
- comparación de alternativas;
- preferencias de selección;
- columnas configurables;
- reportes técnicos;
- lista/BOM de componentes;
- exportación de resultados;
- guardado/recuperación de proyectos;
- trazabilidad bibliográfica de cada resultado.

## Estado actual de FrioCalc

YA EXISTE / PARCIAL:
- carta psicrométrica SI;
- presión atmosférica variable;
- propiedades psicrométricas principales;
- puntos sobre carta;
- estructura inicial de cámara frigorífica;
- flujo de preguntas de cámara;
- modelo de mercancías/paneles;
- algunas relaciones documentales de cámara;
- presión absoluta/manométrica;
- caudal de gas caliente de desescarche;
- regla de caudales en uniones;
- vocabulario común de selección.

FALTA EL GRUESO:
- motor completo de carga térmica;
- motor de refrigerantes;
- ciclo frigorífico;
- tuberías;
- selección genérica de componentes;
- compresores;
- evaporadores/condensadores;
- válvulas;
- controles;
- seguridad/alivio;
- sistemas avanzados;
- reportes/BOM/log(p)-h.

## Prioridad recomendada

1. Motor completo de carga térmica de cámara.
2. Motor de propiedades de refrigerantes y ciclo frigorífico.
3. Evaporador + desescarche + ventiladores.
4. Compresor / unidad condensadora.
5. Válvula de expansión.
6. Tuberías y pérdidas de presión.
7. Componentes de línea.
8. Controles y sensores.
9. Seguridad y alivio.
10. Sistemas avanzados y herramientas de reporte.