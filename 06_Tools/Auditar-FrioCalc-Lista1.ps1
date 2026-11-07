#requires -Version 5.1
<#
FrioCalc — Auditoría integral Lista 1.
Ejecuta compilación y pruebas sin modificar archivos versionados.
Almacena los resultados en Documentos, fuera del repositorio.
Requisito: Windows, SDK .NET 10 y entorno WPF.
#>
[CmdletBinding()]
param([switch]$AbrirEnVisualStudio)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $root 'FrioCalc.Psychrometrics.slnx'
$numeric = Join-Path $root 'tests\FrioCalc.Psychrometrics.Smoke\FrioCalc.Psychrometrics.Smoke.csproj'
$ui = Join-Path $root 'tests\FrioCalc.UI.Smoke\FrioCalc.UI.Smoke.csproj'
$expected = 'chatgpt/friocalc-lista1-2026-11-07'
$actual = ((& git -C $root branch --show-current) | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar Git.' }
if ($actual -ne $expected) {
    throw "La auditoría corresponde a la rama '$expected', pero la copia local está en '$actual'. No se realizará ninguna actualización de rama."
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Se requiere SDK .NET 10 y herramientas de escritorio .NET en Windows.'
}
$version = ((& dotnet --version) | Out-String).Trim()
if (-not $version.StartsWith('10.')) {
    throw "Se requiere SDK .NET 10; se detectó $version."
}
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$documents = [Environment]::GetFolderPath('MyDocuments')
$out = Join-Path $documents ("FrioCalc_Auditorias\Lista1_" + $stamp)
New-Item -ItemType Directory -Force -Path $out | Out-Null
$head = ((& git -C $root rev-parse HEAD) | Out-String).Trim()
$status = ((& git -C $root status --short) | Out-String).Trim()
@"
FRIOCALC — AUDITORÍA LISTA 1
Fecha: $(Get-Date -Format o)
Rama: $actual
Commit: $head
SDK: $version
Directorio de evidencias: $out
Cambios locales: $(if ($status) {$status} else {'Ninguno'})
"@ | Set-Content -Encoding UTF8 (Join-Path $out '00_Entorno.txt')

if ($status) {
    Write-Warning 'Existen cambios locales. Se auditará el estado actual sin descartarlos.'
}
function Invoke-Validation {
    param([string]$Name,[scriptblock]$Action)
    Write-Host "Ejecutando $Name..."
    $log = Join-Path $out ($Name + '.log')
    & $Action 2>&1 | Tee-Object -FilePath $log
    if ($LASTEXITCODE -ne 0) {
        "ERROR: $Name (salida $LASTEXITCODE)" | Add-Content (Join-Path $out 'RESULTADO.txt')
        throw "Fallo en $Name. Consulte el archivo $log."
    }
    "CORRECTO: $Name" | Add-Content (Join-Path $out 'RESULTADO.txt')
}

Push-Location $root
try {
    Invoke-Validation '01_Restaurar_Solucion' { dotnet restore $solution }
    Invoke-Validation '02_Compilar_Sin_Advertencias' { dotnet build $solution -c Debug --no-restore --warnaserror }
    Invoke-Validation '03_Restaurar_Pruebas_Numericas' { dotnet restore $numeric }
    Invoke-Validation '04_Pruebas_Numericas' { dotnet run --project $numeric -c Debug --no-restore }
    Invoke-Validation '05_Restaurar_Pruebas_UI' { dotnet restore $ui }
    Invoke-Validation '06_Pruebas_UI_Inicializacion' { dotnet run --project $ui -c Debug --no-restore }
    Copy-Item (Join-Path $root 'docs\GUIA_AUDITORIA_VISUAL_LISTA1.md') $out -ErrorAction Stop
    "RESULTADO AUTOMÁTICO: TODAS LAS VALIDACIONES APROBADAS. Falta auditoría visual humana." |
        Add-Content (Join-Path $out 'RESULTADO.txt')
    Write-Host "Resultados guardados en: $out"
    if ($AbrirEnVisualStudio) { Invoke-Item $solution }
}
finally { Pop-Location }
