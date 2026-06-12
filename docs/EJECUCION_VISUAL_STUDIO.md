# Ejecución desde Visual Studio

Requisitos: Windows 10/11, Visual Studio 2022, carga de trabajo .NET desktop development y SDK de .NET 8.

Abrir el archivo:

CartaPsicometrica.sln

En Visual Studio:

1. Seleccionar el proyecto CartaPsicometrica como proyecto de inicio.
2. Usar Build > Build Solution.
3. Ejecutar con F5.
4. Ejecutar sin depuración con Ctrl + F5.

Comandos alternativos:

    git clone https://github.com/colombianitov2/Carta-psicometrica.git
    cd Carta-psicometrica
    dotnet build .\CartaPsicometrica.sln
    dotnet run --project .\src\CartaPsicometrica\CartaPsicometrica.csproj

## Qué hace la base actual

- Abre una interfaz WPF independiente.
- Calcula dos puntos psicrométricos usando TBS + HR.
- Usa presión atmosférica definida por el usuario.
- Calcula bulbo húmedo, punto de rocío, razón de humedad, entalpía, presión de vapor, presión de vapor saturado, calor específico aproximado, volumen específico, densidad y grado de saturación.
- Calcula el proceso A a B: delta de temperatura, delta de humedad, delta de entalpía, carga total, sensible, latente, SHR, condensado y humidificación.
- Dibuja una carta psicrométrica inicial con grilla, curva de saturación, curvas de HR, puntos A/B y línea de proceso.

## Nota técnica

El paquete anterior disponible en los documentos del proyecto es una publicación compilada de Windows x64, no un proyecto fuente completo. Por eso la base subida al repositorio es una reconstrucción limpia en C#/.NET 8, usando la función psicrométrica previa como referencia funcional, pero preparada para ampliarse como proyecto independiente.
