param(
    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory,
    [string] $InnoCompiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    [ValidateRange(1,65535)] [int] $BetaNumber = 3
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($env:OS -ne 'Windows_NT') { throw 'La publicación WPF requiere Windows.' }
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'FrioCalc.Psychrometrics\FrioCalc.Psychrometrics.csproj'
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path $output) {
    if (@(Get-ChildItem -LiteralPath $output -Force).Count -gt 0) {
        throw 'El directorio de salida debe estar vacío. No se reemplazan archivos existentes.'
    }
} else {
    New-Item -ItemType Directory -Path $output | Out-Null
}
if (!(Test-Path -LiteralPath $InnoCompiler -PathType Leaf)) {
    throw 'Instale Inno Setup 6 y vuelva a ejecutar o indique -InnoCompiler.'
}

function Invoke-Checked([string] $Program, [string[]] $Arguments) {
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Program terminó con código $LASTEXITCODE."
    }
}

Push-Location $root
try {
    Invoke-Checked 'dotnet' @('build','FrioCalc.Psychrometrics.slnx','-c','Release','--warnaserror')
    Invoke-Checked 'dotnet' @('run','--project','tests/FrioCalc.Psychrometrics.Smoke/FrioCalc.Psychrometrics.Smoke.csproj','-c','Release')
    Invoke-Checked 'dotnet' @('run','--project','tests/FrioCalc.UI.Smoke/FrioCalc.UI.Smoke.csproj','-c','Release')
    $previousCulture = $env:FRIOCALC_UI_CULTURE
    try {
        $env:FRIOCALC_UI_CULTURE = 'es-CO'
        Invoke-Checked 'dotnet' @('run','--project','tests/FrioCalc.UI.Smoke/FrioCalc.UI.Smoke.csproj','-c','Release')
    } finally {
        $env:FRIOCALC_UI_CULTURE = $previousCulture
    }
    $stage = Join-Path $output 'stage'
    Invoke-Checked 'dotnet' @('publish',$project,'-c','Release','-r','win-x64','--self-contained','true',
        '-p:PublishSingleFile=true','-p:IncludeNativeLibrariesForSelfExtract=true','-p:EnableCompressionInSingleFile=true',
        '-p:PublishTrimmed=false','-p:DebugType=None','-p:DebugSymbols=false',
        "-p:Version=1.0.0-beta.$BetaNumber",'-p:AssemblyVersion=1.0.0.0',"-p:FileVersion=1.0.0.$BetaNumber",'-o',$stage)
    $application = Join-Path $stage 'FrioCalc.Psychrometrics.exe'
    if (!(Test-Path $application -PathType Leaf)) { throw 'No se produjo el ejecutable.' }
    $remaining = @(Get-ChildItem -LiteralPath $stage -Recurse -File | Where-Object { $_.FullName -ne $application })
    if ($remaining.Count -ne 0) { throw 'La publicación no resultó en un ejecutable autónomo único.' }
    $process = Start-Process -FilePath $application -WorkingDirectory $stage -PassThru
    try {
        Start-Sleep -Seconds 8
        $process.Refresh()
        if ($process.HasExited) { throw "El ejecutable publicado salió prematuramente: código $($process.ExitCode)." }
    } finally {
        if (!$process.HasExited) { Stop-Process -Id $process.Id }
    }
    $portable = Join-Path $output "FrioCalc-Beta$BetaNumber-win-x64-Portable.exe"
    Copy-Item -LiteralPath $application -Destination $portable
    # Probar el reemplazo real del EXE portable en un directorio aislado de CI.
    $updateTest = Join-Path $output 'update-test'
    New-Item -ItemType Directory -Path $updateTest | Out-Null
    $target = Join-Path $updateTest 'FrioCalc-test.exe'
    Copy-Item -LiteralPath $application -Destination $target
    $old = Start-Process -FilePath $target -PassThru
    try {
        $helper = Start-Process -FilePath $application -ArgumentList @('--apply-portable-update',"$($old.Id)","`"$target`"") -PassThru
        if (!$old.HasExited) { Stop-Process -Id $old.Id }
        if (!$helper.WaitForExit(60000)) { Stop-Process -Id $helper.Id; throw 'El actualizador no terminó.' }
        if ($helper.ExitCode -ne 0) { throw "Actualización portable fallida: $($helper.ExitCode)." }
        if (@(Get-ChildItem -LiteralPath $updateTest -Filter '*.bak').Count -ne 1) { throw 'No se creó el respaldo del ejecutable.' }
        if ((Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $application).Hash) { throw 'Hash incorrecto después de actualizar.' }
        Write-Host 'Actualizador portable: reemplazo y respaldo verificados.'
    } finally {
        if (!$old.HasExited) { Stop-Process -Id $old.Id }
        Get-Process -Name 'FrioCalc-test' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $target } | Stop-Process
    }
    Compress-Archive -LiteralPath $portable -DestinationPath (Join-Path $output "FrioCalc-Beta$BetaNumber-win-x64-Portable.zip")
    Invoke-Checked $InnoCompiler @('/Qp',"/DSourceDir=$stage","/DOutputDir=$output","/DBetaNumber=$BetaNumber",(Join-Path $PSScriptRoot 'FrioCalc-Beta1.iss'))
    $setup = Join-Path $output "FrioCalc-Beta$BetaNumber-win-x64-Setup.exe"
    if (!(Test-Path $setup -PathType Leaf)) { throw 'No se produjo el instalador.' }
    $files = @($portable, (Join-Path $output "FrioCalc-Beta$BetaNumber-win-x64-Portable.zip"), $setup)
    $files | ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
        "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_))"
    } | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding ASCII
    Get-Item -LiteralPath $files | Select-Object Name,Length
    Write-Host "Beta ${BetaNumber}: paquetes disponibles en $output"
} finally {
    Pop-Location
}
