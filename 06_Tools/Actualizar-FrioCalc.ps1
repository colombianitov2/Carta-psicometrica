[CmdletBinding()]
param([switch]$AbrirVisualStudio)

try {
    . (Join-Path $PSScriptRoot 'FrioCalc.Tools.ps1')
    $root = Update-FrioCalcSafely
    if ($AbrirVisualStudio) { Open-FrioCalcVisualStudio -Root $root }
    exit 0
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
