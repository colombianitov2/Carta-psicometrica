using CartaPsicometrica.Services;

PsicometriaService psicometria = new();

var puntoA = psicometria.CalcularDesdeTbsHr("A", 26.0, 55.0, 101325.0);
var puntoB = psicometria.CalcularDesdeTbsHr("B", 14.0, 90.0, 101325.0);
var proceso = psicometria.CalcularProceso(puntoA, puntoB, 1.0);

AssertRange(puntoA.RazonHumedadKgKg * 1000.0, 10.5, 12.5, "W Punto A");
AssertRange(puntoA.EntalpiaKJKgAireSeco, 53.0, 58.0, "Entalpía Punto A");
AssertRange(puntoA.TemperaturaPuntoRocioC, 16.0, 18.0, "Punto de rocío Punto A");
AssertRange(puntoB.RazonHumedadKgKg * 1000.0, 8.5, 10.5, "W Punto B");
AssertRange(proceso.CargaTotalKW, -25.0, -15.0, "Carga total A-B");

if (proceso.CondensadoKgH <= 0)
    throw new InvalidOperationException("El proceso de ejemplo debe producir condensado positivo.");

Console.WriteLine("Verificaciones psicrométricas básicas correctas.");

static void AssertRange(double value, double min, double max, string name)
{
    if (double.IsNaN(value) || value < min || value > max)
        throw new InvalidOperationException($"{name}: {value:0.###} fuera del rango esperado [{min:0.###}, {max:0.###}].");
}
