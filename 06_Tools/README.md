# Herramientas de FrioCalc

Repositorio local: `D:\Proyectos de desarrollo de Software\FrioCalc_Psychrometrics_Git`.
Rama requerida: `chatgpt/friocalc-psychrometrics`.

- **↓ Actualizar Git**: verifica carpeta, remoto, rama y estado limpio; ejecuta `git fetch origin` y `git pull --ff-only origin chatgpt/friocalc-psychrometrics`.
- **▶ Actualizar y probar**: realiza la misma actualizacion, compila Debug y abre el ejecutable solo si la compilacion devuelve exito.
- **Abrir-FrioCalc.cmd**: doble clic desde Windows para actualizar y abrir la solucion en Visual Studio; no compila.

Antes de actualizar, usa **Guardar todo** en Visual Studio. Git detecta cambios guardados en disco, no ediciones que todavia existen solamente en memoria del editor.
Si hay cambios, otra rama, una operacion Git pendiente, falta autenticacion o las ramas divergieron, la herramienta se detiene. No guarda cambios con stash, no resuelve conflictos, no cambia ramas y no publica commits.
Un fallo de compilacion devuelve codigo distinto de cero y nunca inicia el programa. Los detalles quedan en Salida de Visual Studio; desde Windows, el acceso CMD mantiene visible cualquier error.

## External Tools

Comando: `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`

Argumentos para actualizar:
`-NoProfile -ExecutionPolicy Bypass -File "$(SolutionDir)06_Tools\Actualizar-FrioCalc.ps1"`

Argumentos para actualizar y probar:
`-NoProfile -ExecutionPolicy Bypass -File "$(SolutionDir)06_Tools\Actualizar-Y-Probar-FrioCalc.ps1"`

Directorio inicial: `$(SolutionDir)`. Activar **Usar ventana de salida**.
Los botones se instalan en el perfil local de Visual Studio; clonar el repositorio en otro PC no configura automaticamente ese perfil.
