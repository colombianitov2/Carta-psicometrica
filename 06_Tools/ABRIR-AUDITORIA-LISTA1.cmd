@echo off
setlocal
echo FrioCalc Lista 1 - Preparacion y auditoria en copia aislada
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Preparar-Y-Auditar-FrioCalc-Lista1.ps1" -AbrirEnVisualStudio
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" (
  echo.
  echo La auditoria se detuvo para proteger los archivos del equipo.
  echo Revise el mensaje de error anterior. No se ejecuta reset ni clean.
  pause
)
exit /b %RESULT%
