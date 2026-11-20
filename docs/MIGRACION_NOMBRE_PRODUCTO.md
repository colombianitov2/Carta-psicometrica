# Migración del nombre de la aplicación

Nombre oficial: **Calculadora de refrigeración – Parte 1 – Carta psicrométrica**.

Repositorio actual: `colombianitov2/Carta-psicometrica` (ID 1267068454). Nombre propuesto para la URL: `colombianitov2/Calculadora-de-refrigeracion-Parte-1-Carta-psicometrica`.

## Restricción de compatibilidad

La Beta 3 publicada valida literalmente tanto el nombre del archivo `FrioCalc-BetaN-win-x64-{Portable|Setup}.exe` como la URL antigua de cada activo. Tras renombrar el repositorio, las redirecciones de GitHub no bastan: la URL devuelta por la API de Releases puede cambiar y el cliente antiguo descartaría el activo. No se cambia todavía el nombre del repositorio ni se reemplazan los activos o etiquetas publicados.

## 1. Versión puente

- Integrar esta rama después de la prueba en Windows. La versión puente usa el nombre nuevo en la interfaz, instalador, metadatos y documentación; mantiene el identificador de aplicación de Inno Setup, nombre del ensamblado, nombre de ejecutable instalado, rutas de recuperación, extensión de sesiones y nombres de paquetes de compatibilidad. No se cambia la estructura ni las ecuaciones.
- Verificar que el nombre del producto de los binarios ya publicados sea el literal previsto en LegacyProductName. Si difiere, permitir ese literal exacto también, no cualquier producto. Probar actualización real desde Beta 3 portable y Beta 3 instalada, no solamente reemplazo de una copia de la propia versión nueva.
- Configurar un workflow separado para Beta 4 (o siguiente versión no publicada si existe), compilaciones y pruebas completas; los activos con nombre histórico y SHA256SUMS.txt deben publicar primero en el repositorio actual. No reetiquetar ni sobreescribir Beta 3. Comprobar recuperación de sesiones, acceso directo y registro de desinstalación.
- Probar la API, integridad y actualización con URL original y URL propuesta mediante fixtures; una vez publicada la versión puente, comprobar los dos tipos de cliente real.

## 2. Nombre en GitHub y PC

- Mostrar siempre el nombre oficial en README del repositorio y descripción de GitHub. Mantener la URL histórica como canal de releases mientras pueda existir algún cliente antiguo sin actualizar. No renombrar la ruta remota real si no se ha verificado la migración de todos los clientes que dependan de ella; un cambio de URL visible no tiene prioridad sobre continuidad de actualizaciones.
- En Windows partir de la copia de trabajo existente, identificar rama y worktrees, cambios locales y referencias externas. Cerrar Visual Studio antes de cambiar nombre de carpeta. No descartar archivos ni cambios no comprometidos; mover de forma recuperable solo con ruta exacta verificada, actualizar los worktrees y accesos directos necesarios; validar solución y git status. No modificar el directorio de instalación o almacenamiento de versiones existentes simplemente renombrándolo.
- Si se autoriza y verifica la migración de todos los clientes, renombrar en la configuración de GitHub el mismo repositorio (no crear otro); comprobar el ID 1267068454 y que siguen resolviendo enlaces de commits, releases y descargas antiguas; actualizar origin de cada copia. Nunca reutilizar el slug histórico. Validar la actualización desde la versión puente a una posterior en ambas modalidades.
- Los nuevos activos con nomenclatura distinta deben publicarse con alias de compatibilidad y manifiesto exacto mientras haya clientes que exijan los nombres históricos. Las rutas de descarga se restringen al repositorio oficial; no desactivar las comprobaciones de URL, versión, tamaño y SHA-256.

## Criterio de cierre

Compilación con 0 errores y 0 advertencias; suite lógica y WPF en los dos idiomas; pruebas cruzadas de actualización portable e instalada; preservación de sesiones y respaldo; nombre oficial en todas las superficies visibles, README y documentación; listado explícito de identificadores históricos conservados por compatibilidad.
