using CartaPsicometrica.Models;

namespace CartaPsicometrica.Services;

public sealed class PsicometriaService
{
    public const double TemperaturaMinimaOperativaC = -40.0;
    public const double TemperaturaMaximaOperativaC = 90.0;
    public const double RazonHumedadMaximaOperativa = 0.500;

    private const double EpsilonRazonHumedad = 1e-9;
    private const double RelacionMasaAguaAireSeco = 0.621945;
    private const double ConstanteGasAireSeco = 287.055;

    public EstadoPsicometrico CalcularDesdeTbsHr(
        string nombre,
        double temperaturaBulboSecoC,
        double humedadRelativaPorcentaje,
        double presionPa)
    {
        ValidarPresion(presionPa);

        if (temperaturaBulboSecoC < TemperaturaMinimaOperativaC || temperaturaBulboSecoC > TemperaturaMaximaOperativaC)
            throw new InvalidOperationException($"La temperatura de bulbo seco debe estar entre {TemperaturaMinimaOperativaC:0} °C y {TemperaturaMaximaOperativaC:0} °C.");

        if (humedadRelativaPorcentaje < 0.0 || humedadRelativaPorcentaje > 100.0)
            throw new InvalidOperationException("La humedad relativa debe estar entre 0% y 100%.");

        double humedadRelativa = humedadRelativaPorcentaje / 100.0;
        double razonHumedad = RazonHumedadDesdeHumedadRelativa(temperaturaBulboSecoC, humedadRelativa, presionPa);
        return CrearEstadoDesdeTbsW(nombre, temperaturaBulboSecoC, razonHumedad, presionPa);
    }

    public ProcesoPsicometrico CalcularProceso(
        EstadoPsicometrico puntoA,
        EstadoPsicometrico puntoB,
        double flujoAireSecoKgS)
    {
        if (flujoAireSecoKgS < 0)
            throw new InvalidOperationException("El flujo de aire seco no puede ser negativo.");

        double deltaT = puntoB.TemperaturaBulboSecoC - puntoA.TemperaturaBulboSecoC;
        double deltaW = puntoB.RazonHumedadKgKg - puntoA.RazonHumedadKgKg;
        double deltaH = puntoB.EntalpiaKJKgAireSeco - puntoA.EntalpiaKJKgAireSeco;
        double wPromedio = (puntoA.RazonHumedadKgKg + puntoB.RazonHumedadKgKg) / 2.0;
        double cargaSensible = flujoAireSecoKgS * (1.006 + 1.86 * wPromedio) * deltaT;
        double cargaTotal = flujoAireSecoKgS * deltaH;
        double cargaLatente = cargaTotal - cargaSensible;
        double factorCalorSensible = Math.Abs(cargaTotal) < 1e-9 ? double.NaN : cargaSensible / cargaTotal;
        double relacionDeltaHDeltaW = Math.Abs(deltaW) < 1e-12 ? double.NaN : deltaH / deltaW;

        return new ProcesoPsicometrico(
            puntoA,
            puntoB,
            flujoAireSecoKgS,
            deltaT,
            deltaW,
            deltaH,
            relacionDeltaHDeltaW,
            cargaSensible,
            cargaLatente,
            cargaTotal,
            factorCalorSensible,
            Math.Max(0.0, -deltaW * flujoAireSecoKgS * 3600.0),
            Math.Max(0.0, deltaW * flujoAireSecoKgS * 3600.0),
            ClasificarProceso(deltaT, deltaW));
    }

    public EstadoPsicometrico CrearEstadoDesdeTbsW(
        string nombre,
        double temperaturaBulboSecoC,
        double razonHumedadKgKg,
        double presionPa)
    {
        ValidarPresion(presionPa);

        if (razonHumedadKgKg < 0)
            throw new InvalidOperationException("La razón de humedad no puede ser negativa.");

        if (razonHumedadKgKg > RazonHumedadMaximaOperativa)
            throw new InvalidOperationException($"La razón de humedad supera el máximo operativo de {RazonHumedadMaximaOperativa * 1000.0:0} g/kg de aire seco.");

        double pws = PresionSaturacionPa(temperaturaBulboSecoC);
        double pv = PresionVaporDesdeRazonHumedad(razonHumedadKgKg, presionPa);
        double humedadRelativa = pv / pws;
        double ws = RazonHumedadSaturada(temperaturaBulboSecoC, presionPa);

        if (humedadRelativa > 1.001 || razonHumedadKgKg > ws * 1.002)
            throw new InvalidOperationException("El estado calculado supera la condición de saturación.");

        double entalpia = Entalpia(temperaturaBulboSecoC, razonHumedadKgKg);
        double volumen = VolumenEspecifico(temperaturaBulboSecoC, razonHumedadKgKg, presionPa);
        double calorEspecifico = 1.006 + 1.86 * razonHumedadKgKg;
        double densidad = (1.0 + razonHumedadKgKg) / volumen;

        return new EstadoPsicometrico(
            nombre,
            presionPa,
            temperaturaBulboSecoC,
            humedadRelativa,
            razonHumedadKgKg,
            ws,
            TemperaturaBulboHumedoDesdeEntalpia(entalpia, presionPa),
            TemperaturaPuntoRocioDesdePresionVapor(pv),
            entalpia,
            pv,
            pws,
            calorEspecifico,
            volumen,
            densidad,
            ws > EpsilonRazonHumedad ? razonHumedadKgKg / ws : 0.0);
    }

    public double PresionSaturacionPa(double temperaturaC)
    {
        double temperaturaK = temperaturaC + 273.15;
        double lnPws;

        if (temperaturaC <= 0.0)
        {
            lnPws =
                -5.6745359E+03 / temperaturaK +
                6.3925247 -
                9.6778430E-03 * temperaturaK +
                6.2215701E-07 * Math.Pow(temperaturaK, 2) +
                2.0747825E-09 * Math.Pow(temperaturaK, 3) -
                9.4840240E-13 * Math.Pow(temperaturaK, 4) +
                4.1635019 * Math.Log(temperaturaK);
        }
        else
        {
            lnPws =
                -5.8002206E+03 / temperaturaK +
                1.3914993 -
                4.8640239E-02 * temperaturaK +
                4.1764768E-05 * Math.Pow(temperaturaK, 2) -
                1.4452093E-08 * Math.Pow(temperaturaK, 3) +
                6.5459673 * Math.Log(temperaturaK);
        }

        return Math.Exp(lnPws);
    }

    public double RazonHumedadDesdeHumedadRelativa(double temperaturaC, double humedadRelativa, double presionPa)
    {
        double pv = humedadRelativa * PresionSaturacionPa(temperaturaC);
        return RazonHumedadDesdePresionVapor(pv, presionPa);
    }

    public double RazonHumedadSaturada(double temperaturaC, double presionPa)
    {
        return RazonHumedadDesdePresionVapor(PresionSaturacionPa(temperaturaC), presionPa);
    }

    public double Entalpia(double temperaturaC, double razonHumedadKgKg)
    {
        return 1.006 * temperaturaC + razonHumedadKgKg * (2501.0 + 1.86 * temperaturaC);
    }

    public double VolumenEspecifico(double temperaturaC, double razonHumedadKgKg, double presionPa)
    {
        double temperaturaK = temperaturaC + 273.15;
        return ConstanteGasAireSeco * temperaturaK * (1.0 + 1.607858 * razonHumedadKgKg) / presionPa;
    }

    public double PresionVaporDesdeRazonHumedad(double razonHumedadKgKg, double presionPa)
    {
        return presionPa * razonHumedadKgKg / (RelacionMasaAguaAireSeco + razonHumedadKgKg);
    }

    private double RazonHumedadDesdePresionVapor(double presionVaporPa, double presionPa)
    {
        if (presionVaporPa < 0 || presionVaporPa >= presionPa)
            throw new InvalidOperationException("La presión parcial de vapor está fuera del rango físico para la presión atmosférica indicada.");

        return RelacionMasaAguaAireSeco * presionVaporPa / (presionPa - presionVaporPa);
    }

    private double TemperaturaPuntoRocioDesdePresionVapor(double presionVaporPa)
    {
        double baja = -90.0;
        double alta = 90.0;

        for (int i = 0; i < 90; i++)
        {
            double media = (baja + alta) / 2.0;
            double presionMedia = PresionSaturacionPa(media);

            if (presionMedia > presionVaporPa)
                alta = media;
            else
                baja = media;
        }

        return (baja + alta) / 2.0;
    }

    private double TemperaturaBulboHumedoDesdeEntalpia(double entalpia, double presionPa)
    {
        double baja = -30.0;
        double alta = 65.0;

        for (int i = 0; i < 90; i++)
        {
            double media = (baja + alta) / 2.0;
            double ws = RazonHumedadSaturada(media, presionPa);
            double entalpiaSaturada = Entalpia(media, ws);

            if (entalpiaSaturada < entalpia)
                baja = media;
            else
                alta = media;
        }

        return (baja + alta) / 2.0;
    }

    private static void ValidarPresion(double presionPa)
    {
        if (presionPa < 50000.0 || presionPa > 120000.0)
            throw new InvalidOperationException("La presión atmosférica debe estar entre 50 kPa y 120 kPa para esta primera versión.");
    }

    private static string ClasificarProceso(double deltaT, double deltaW)
    {
        const double toleranciaT = 1e-6;
        const double toleranciaW = 1e-8;

        bool calienta = deltaT > toleranciaT;
        bool enfria = deltaT < -toleranciaT;
        bool humidifica = deltaW > toleranciaW;
        bool deshumidifica = deltaW < -toleranciaW;

        return (calienta, enfria, humidifica, deshumidifica) switch
        {
            (true, false, false, false) => "Calentamiento sensible",
            (false, true, false, false) => "Enfriamiento sensible",
            (false, false, true, false) => "Humidificación",
            (false, false, false, true) => "Deshumidificación",
            (true, false, true, false) => "Calentamiento con humidificación",
            (true, false, false, true) => "Calentamiento con deshumidificación",
            (false, true, true, false) => "Enfriamiento con humidificación",
            (false, true, false, true) => "Enfriamiento con deshumidificación",
            _ => "Sin cambio apreciable"
        };
    }
}
