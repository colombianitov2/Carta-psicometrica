namespace CartaPsicometrica.Models;

public sealed record ConfiguracionPsicometrica(
    double PresionKPa,
    double FlujoAireSecoKgS,
    double PuntoATemperaturaBulboSecoC,
    double PuntoAHumedadRelativaPorcentaje,
    double PuntoBTemperaturaBulboSecoC,
    double PuntoBHumedadRelativaPorcentaje);
