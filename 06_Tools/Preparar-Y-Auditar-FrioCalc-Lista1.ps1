#requires -Version 5.1
<#
 [6.7] Preparación segura de la copia de auditoría Lista 1.
 Crea un worktree hermano o sincroniza uno existente sin modificar
 la rama original ni los documentos locales de investigación.
#>
[CmdletBinding()]
param([switch]$AbrirEnVisualStudio)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$branch = 'chatgpt/friocalc-lista1-2026-11-07'
$dest = Join-Path (Split-Path $root -Parent) 'FrioCalc_Lista1_Auditoria'
$git = Get-Command git -ErrorAction Stop

function Assert-GitExit([string]$Action) {
    if ($LASTEXITCODE -ne 0) { throw "$Action falló (código $LASTEXITCODE). Ningún archivo local se ha eliminado." }
}

Write-Host "FrioCalc Lista 1: copia aislada para auditoría." -ForegroundColor Cyan
Write-Host "Proyecto original (no se modifica): $root"
Write-Host "Copia a preparar: $dest"

& git -C $root rev-parse --show-toplevel | Out-Null
Assert-GitExit 'Identificación del repositorio'
& git -C $root fetch origin $branch
Assert-GitExit 'Descarga de rama candidata'

$remoteRef = "refs/remotes/origin/$branch"
& git -C $root show-ref --verify --quiet $remoteRef
Assert-GitExit "Verificación de la rama remota $remoteRef"

if (Test-Path -LiteralPath $dest) {
    if (-not (Test-Path -LiteralPath (Join-Path $dest '.git'))) {
        throw "La carpeta destino ya existe, pero no es el worktree esperado: $dest. No se sobrescribe."
    }
    $actualBranch = ((& git -C $dest branch --show-current) | Out-String).Trim()
    Assert-GitExit 'Consulta de rama de la copia de auditoría'
    if ($actualBranch -ne $branch) {
        throw "La copia existente está en la rama '$actualBranch' y no '$branch'. No se cambia automáticamente."
    }
    $dirty = ((& git -C $dest status --porcelain) | Out-String).Trim()
    Assert-GitExit 'Estado del worktree de auditoría'
    if ($dirty) {
        throw "Hay cambios sin registrar en la copia de auditoría. Revise con 'git status' antes de actualizarla; no se descartará nada."
    }
    & git -C $dest pull --ff-only origin $branch
    Assert-GitExit 'Actualización segura de la copia de auditoría'
}
else {
    & git -C $root show-ref --verify --quiet "refs/heads/$branch"
    $hasLocalBranch = $LASTEXITCODE -eq 0
    if ($hasLocalBranch) {
        & git -C $root worktree add $dest $branch
    }
    else {
        & git -C $root worktree add --track -b $branch $dest "origin/$branch"
    }
    Assert-GitExit 'Creación de worktree aislado'
}

$testScript = Join-Path $dest '06_Tools\Auditar-FrioCalc-Lista1.ps1'
if (-not (Test-Path -LiteralPath $testScript)) {
    throw "No se encontró el script de auditoría: $testScript"
}
Write-Host "Copia segura lista en: $dest" -ForegroundColor Green
$argsToAudit = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$testScript)
if ($AbrirEnVisualStudio) { $argsToAudit += '-AbrirEnVisualStudio' }
& powershell.exe @argsToAudit
Assert-GitExit 'Auditoría integral de FrioCalc'
