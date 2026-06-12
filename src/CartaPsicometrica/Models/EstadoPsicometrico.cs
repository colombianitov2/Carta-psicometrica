namespace CartaPsicometrica.Models;

public sealed record EstadoPsicometrico(
    string Nombre,
    double PresionPa,
    double TemperaturaBulboSecoC,
    double HumedadRelativa,
    double RazonHumedadKgKg,
    double RazonHumedadSaturacionKgKg,
    double TemperaturaBulboHumedoC,
    double TemperaturaPuntoRocioC,
    double EntalpiaKJKgAireSeco,
    double PresionParcialVaporPa,
    double PresionVaporSaturadoPa,
    double CalorEspecificoKJKgK,
    double VolumenEspecificoM3KgAireSeco,
    double DensidadKgM3,
    double GradoSaturacion);
