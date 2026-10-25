# Herramientas locales. No modifica el codigo de FrioCalc.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-FrioCalcGit {
    param([string[]]$GitArguments)
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& git @GitArguments 2>&1)
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousPreference
    }
    if ($code -ne 0) {
        throw "Git fallo ($code): git $($GitArguments -join ' ')`n$($output -join [Environment]::NewLine)"
    }
    return $output
}

function Update-FrioCalcSafely {
    # El proceso conserva el mutex hasta terminar (incluida compilacion/arranque).
    $script:FrioCalcMutex = New-Object System.Threading.Mutex($false, 'Local\FrioCalcPsychrometricsUpdate')
    try { $acquired = $script:FrioCalcMutex.WaitOne(0) }
    catch [System.Threading.AbandonedMutexException] { $acquired = $true }
    if (!$acquired) { throw 'Ya hay otra actualizacion de FrioCalc en curso.' }
    $expectedRoot = 'D:\Proyectos de desarrollo de Software\FrioCalc_Psychrometrics_Git'
    $branch = 'chatgpt/friocalc-psychrometrics'
    $root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
    if ($root -ne $expectedRoot) { throw "Ruta incorrecta: $root" }
    Set-Location -LiteralPath $root
    Get-Command git -ErrorAction Stop | Out-Null
    $gitRoot = (Invoke-FrioCalcGit @('rev-parse', '--show-toplevel') | Out-String).Trim().Replace('/', '\')
    if ($gitRoot -ne $expectedRoot) { throw "El repositorio Git no corresponde a $expectedRoot" }
    $origin = (Invoke-FrioCalcGit @('remote', 'get-url', 'origin') | Out-String).Trim()
    if ($origin -notin @('https://github.com/colombianitov2/Carta-psicometrica.git', 'https://github.com/colombianitov2/Carta-psicometrica', 'git@github.com:colombianitov2/Carta-psicometrica.git')) {
        throw "El remoto origin no es el autorizado: $origin"
    }
    $actualBranch = (Invoke-FrioCalcGit @('branch', '--show-current') | Out-String).Trim()
    if ($actualBranch -ne $branch) { throw "Rama incorrecta: '$actualBranch'. Abre la rama $branch antes de actualizar." }
    foreach ($state in @('MERGE_HEAD', 'CHERRY_PICK_HEAD', 'REVERT_HEAD', 'rebase-merge', 'rebase-apply')) {
        $statePath = (Invoke-FrioCalcGit @('rev-parse', '--git-path', $state) | Out-String).Trim()
        if (Test-Path -LiteralPath $statePath) { throw "Hay una operacion Git pendiente: $state" }
    }
    $dirty = @(Invoke-FrioCalcGit @('status', '--porcelain', '--untracked-files=all'))
    if ($dirty.Count -gt 0) {
        Write-Host ($dirty -join [Environment]::NewLine)
        throw 'Hay cambios locales sin guardar. No se actualizará FrioCalc para evitar pérdida de trabajo.'
    }
    Write-Host "Repositorio: $root"
    Write-Host "Rama: $branch"
    $previousPrompt = $env:GIT_TERMINAL_PROMPT
    try {
        $env:GIT_TERMINAL_PROMPT = '0'
        Write-Host 'Descargando referencias...'
        Invoke-FrioCalcGit @('fetch', 'origin') | ForEach-Object { Write-Host $_ }
        # Vuelve a verificar por si el editor guardo un archivo durante fetch.
        if (@(Invoke-FrioCalcGit @('status', '--porcelain', '--untracked-files=all')).Count -gt 0) {
            throw 'Hay cambios locales sin guardar. No se actualizará FrioCalc para evitar pérdida de trabajo.'
        }
        Invoke-FrioCalcGit @('pull', '--ff-only', 'origin', $branch) | ForEach-Object { Write-Host $_ }
    } finally {
        $env:GIT_TERMINAL_PROMPT = $previousPrompt
    }
    Write-Host 'FRIOCALC ACTUALIZADO'
    return $root
}

function Open-FrioCalcVisualStudio {
    param([string]$Root)
    $solution = Join-Path $Root 'FrioCalc.Psychrometrics.slnx'
    if (!(Test-Path -LiteralPath $solution -PathType Leaf)) { throw "No existe la solucion: $solution" }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (!(Test-Path -LiteralPath $vswhere)) { throw 'No se encontro vswhere de Visual Studio.' }
    $ide = @(& $vswhere -latest -products * -property productPath) | Select-Object -First 1
    if (!$ide -or !(Test-Path -LiteralPath $ide)) { throw 'No se encontro Visual Studio instalado.' }
    Start-Process -FilePath $ide -ArgumentList ('"{0}"' -f $solution) -WorkingDirectory $Root | Out-Null
    Write-Host "Visual Studio: $solution"
}
