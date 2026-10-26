# Hoja de ruta de reconstrucción funcional

Esta rama no busca clonar Coolselector. El objetivo es identificar y reconstruir, con código propio, las funciones generales que sirvan a FrioCalc.

## Módulos observados y potencialmente reutilizables

### 1. Cámaras frigoríficas
- asistente de definición de carga;
- dimensiones y entorno;
- mercancías;
- infiltración;
- cargas adicionales;
- desescarche;
- paneles;
- condiciones de cámara;
- condiciones de selección de componentes.

Estado: iniciado en 07_SelectionEngine/01_ColdRoom.

### 2. Motor común de selección
- criterio por tamaño;
- criterio por velocidad;
- criterio por pérdida de presión;
- criterio por capacidad;
- criterio por caída de temperatura de saturación;
- base de capacidad: evaporador, condensador o caudal másico;
- referencias de saturación: rocío, media y burbuja;
- estado de fluido.

Estado: vocabulario inicial creado en 07_SelectionEngine/02_Common.

### 3. Propiedades de refrigerantes
Se observaron módulos y tipos relacionados con motores termodinámicos y referencias ASEREP/REFPROP.

Estado: NO implementar todavía a partir del pseudocódigo. FrioCalc debe usar ecuaciones/bibliotecas verificables y fuentes autorizadas para las propiedades.

### 4. Tuberías y uniones
- caudal másico;
- velocidad;
- pérdida de presión;
- caída equivalente de temperatura de saturación;
- uniones con adición o retirada de caudal;
- estados líquido, gas y bifásico.

Estado: solo la regla documental de uniones está implementada.

### 5. Válvulas y componentes
Se observaron categorías funcionales para válvulas solenoides, control, expansión, retención, cierre, filtros, mirillas, pilotos, conectores, tuberías y accesorios.

Estado: pendiente de reconstruir criterios genéricos; no importar catálogos Danfoss.

### 6. Intercambiadores / evaporadores / condensadores
Se observaron módulos Fhx y formularios de selección de evaporadores/coolers.

Estado: pendiente. Requiere contrastar ecuaciones y datos con bibliografía independiente.

### 7. Compresores y unidades condensadoras
Se observaron aplicaciones MT, LT, HT, aire acondicionado y calefacción, además de flujos de selección por región/refrigerante.

Estado: pendiente. No reutilizar curvas comerciales sin una fuente permitida y verificable.

### 8. Desescarche por gas caliente
La relación documental de caudal másico está implementada en 7.6.

### 9. Válvulas de alivio / seguridad
Se observó soporte para estándares EN 13136, ISO 5149, ASHRAE 15 y otros.

Estado: pendiente. Debe construirse desde las normas vigentes y no desde una inferencia del binario.

## Orden de desarrollo recomendado

1. Terminar cámara frigorífica: entradas, resultados y carga térmica respaldada.
2. Crear motor de unidades y conversiones común.
3. Crear motor de refrigerantes verificable.
4. Crear tuberías y pérdidas de presión.
5. Crear selección genérica de válvulas/componentes.
6. Crear evaporadores/condensadores.
7. Crear compresores y unidades condensadoras.
8. Integrar normas de seguridad y alivio.

Cada módulo debe conservar trazabilidad de la ecuación y de la fuente.