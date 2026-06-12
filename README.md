# Carta psicométrica

Aplicación WPF independiente para cálculo psicrométrico en SI, trazado inicial de carta y análisis de procesos HVAC entre dos estados de aire húmedo.

La primera versión funcional rescata la base técnica del módulo psicrométrico de FrioCalc y la reestructura como proyecto dedicado, compilable desde Visual Studio y desde `dotnet`.

## Estado actual

- Solución `CartaPsicometrica.sln`.
- Aplicación WPF en `.NET 8` para Windows.
- Motor psicrométrico básico en SI separado de la interfaz.
- Entrada de Punto A y Punto B por `TBS + HR`.
- Presión atmosférica editable en kPa.
- Flujo de aire seco editable en kg/s.
- Cálculo de propiedades por punto: TBS, HR, W, TBH, punto de rocío, entalpía, presión parcial de vapor, presión de vapor saturado, calor específico aproximado, volumen específico, densidad y grado de saturación.
- Cálculo de proceso A→B: ΔT, ΔW, Δh, Δh/ΔW, carga sensible, carga latente, carga total, SHR/FCS, condensado y humidificación.
- Carta inicial con grilla, curva de saturación, curvas de HR, líneas de entalpía, bulbo húmedo, volumen específico, puntos A/B, línea de proceso y escala FCS.
- Exportación de resultados a CSV.
- Guardado de configuración en JSON.
- Verificación básica sin paquetes externos en `tests/CartaPsicometrica.Tests`.

## Estructura

```text
CartaPsicometrica.sln
src/CartaPsicometrica/
  App.xaml
  App.xaml.cs
  Controls/
  Models/
  Services/
  ViewModels/
  Views/
docs/
tests/CartaPsicometrica.Tests/
```

## Compilar y ejecutar

Desde PowerShell en la carpeta del repositorio:

```powershell
dotnet restore
dotnet build
dotnet run --project .\src\CartaPsicometrica\CartaPsicometrica.csproj
```

Verificación básica:

```powershell
dotnet run --project .\tests\CartaPsicometrica.Tests\CartaPsicometrica.Tests.csproj
```

## Visual Studio

1. Abrir `CartaPsicometrica.sln`.
2. Verificar que esté instalada la carga de trabajo `.NET desktop development`.
3. Verificar que el SDK `.NET 8` esté instalado.
4. Seleccionar `CartaPsicometrica` como proyecto de inicio.
5. Compilar con `Build > Build Solution`.
6. Ejecutar con `F5` o `Ctrl+F5`.

## Reutilización desde FrioCalc

Se reutilizó como base técnica la formulación psicrométrica y el renderizador WPF del módulo `Psicometria` de FrioCalc, adaptándolos a nombres, modelos y servicios propios del nuevo proyecto.

La UI original de FrioCalc no se copió: se reescribió como ventana dedicada con MVVM simple, paneles de entrada, carta central y resultados por pestañas.

Más detalle en:

- `docs/ANALISIS_FRIOCALC.md`
- `docs/EJECUCION_VISUAL_STUDIO.md`
- `docs/ROADMAP_CARTA_PSICROMETRICA.md`

## Nota técnica

Las ecuaciones psicrométricas implementadas son la base ya presente en FrioCalc, documentada allí como alineada con ASHRAE Handbook Fundamentals y PsychroLib. Los módulos normativos de confort térmico, PMV, ASHRAE 55, ISO 7730, EN 15251/EN 16798, Givoni, EPW y ciclos avanzados quedan preparados en roadmap y deben implementarse solo con bibliografía técnica validada.
