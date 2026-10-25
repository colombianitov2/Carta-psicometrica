@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Actualizar-FrioCalc.ps1" -AbrirVisualStudio
set "FRIOCALC_EXIT=%ERRORLEVEL%"
if not "%FRIOCALC_EXIT%"=="0" pause
exit /b %FRIOCALC_EXIT%
