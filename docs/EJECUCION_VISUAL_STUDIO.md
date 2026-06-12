# Ejecución desde Visual Studio

## Requisitos

- Windows 10/11.
- Visual Studio 2022.
- Carga de trabajo `.NET desktop development`.
- SDK `.NET 8`.
- Runtime `Microsoft.WindowsDesktop.App 8`.

## Abrir la solución

1. Abrir Visual Studio.
2. Seleccionar `Open a project or solution`.
3. Abrir:

```text
D:\Proyectos de desarrollo de Software\Proyecto Refrigeración\Carta psicométrica\CartaPsicometrica.sln
```

## Restaurar paquetes

El proyecto no usa paquetes NuGet externos en esta versión. Visual Studio restaurará automáticamente los proyectos al abrir la solución.

También puede ejecutarse desde PowerShell:

```powershell
dotnet restore
```

## Compilar

En Visual Studio:

1. Seleccionar configuración `Debug`.
2. Seleccionar plataforma `Any CPU`.
3. Ejecutar `Build > Build Solution`.

Desde PowerShell:

```powershell
dotnet build .\CartaPsicometrica.sln
```

## Ejecutar

En Visual Studio:

1. Seleccionar `CartaPsicometrica` como proyecto de inicio.
2. Ejecutar con `F5` para depurar.
3. Ejecutar con `Ctrl+F5` sin depuración.

Desde PowerShell:

```powershell
dotnet run --project .\src\CartaPsicometrica\CartaPsicometrica.csproj
```

## Verificación básica

El proyecto incluye una verificación de consola sin paquetes externos:

```powershell
dotnet run --project .\tests\CartaPsicometrica.Tests\CartaPsicometrica.Tests.csproj
```

Salida esperada:

```text
Verificaciones psicrométricas básicas correctas.
```

## Errores comunes

- Si Visual Studio no reconoce WPF, instalar `.NET desktop development`.
- Si aparece un error de SDK, confirmar que `.NET 8 SDK` esté instalado con `dotnet --info`.
- Si el proyecto de inicio no ejecuta la ventana, seleccionar manualmente `CartaPsicometrica`.
- Si una entrada no calcula, revisar rangos: presión entre `50` y `120` kPa, HR entre `0` y `100%`, y temperaturas entre `-40` y `90 °C`.
