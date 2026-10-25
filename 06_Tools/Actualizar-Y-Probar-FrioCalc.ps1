[CmdletBinding()]
param()

try {
    . (Join-Path $PSScriptRoot 'FrioCalc.Tools.ps1')
    $root = Update-FrioCalcSafely
    Get-Command dotnet -ErrorAction Stop | Out-Null
    $solution = Join-Path $root 'FrioCalc.Psychrometrics.slnx'
    if (!(Test-Path -LiteralPath $solution -PathType Leaf)) { throw "No existe la solucion: $solution" }
    Write-Host 'COMPILANDO DEBUG...'
    & dotnet build $solution --configuration Debug --nologo --verbosity:minimal
    $buildExit = $LASTEXITCODE
    if ($buildExit -ne 0) {
        Write-Host "COMPILACION: FALLIDA (codigo $buildExit). FrioCalc NO se ejecutara." -ForegroundColor Red
        exit $buildExit
    }
    $exe = Join-Path $root 'FrioCalc.Psychrometrics\bin\Debug\net10.0-windows\FrioCalc.Psychrometrics.exe'
    if (!(Test-Path -LiteralPath $exe -PathType Leaf)) { throw "La compilacion no produjo el ejecutable esperado: $exe" }
    Write-Host '========================================'
    Write-Host 'FRIOCALC ACTUALIZADO'
    Write-Host 'COMPILACIÓN: CORRECTA'
    Write-Host 'ERRORES: 0'
    Write-Host 'INICIANDO FRIOCALC...'
    Write-Host '========================================'
    Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) | Out-Null
    exit 0
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
