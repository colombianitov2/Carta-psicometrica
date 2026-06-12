namespace CartaPsicometrica.Models;

public sealed record ProcesoPsicometrico(
    EstadoPsicometrico PuntoA,
    EstadoPsicometrico PuntoB,
    double FlujoAireSecoKgS,
    double DeltaTemperaturaC,
    double DeltaRazonHumedadKgKg,
    double DeltaEntalpiaKJKg,
    double RelacionDeltaHDeltaW,
    double CargaSensibleKW,
    double CargaLatenteKW,
    double CargaTotalKW,
    double FactorCalorSensible,
    double CondensadoKgH,
    double HumidificacionKgH,
    string Tipo);
