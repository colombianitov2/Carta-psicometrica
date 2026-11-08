using System;

namespace FrioCalc.Psychrometrics.Core;

// [1.15.1] Entropía relativa del gas ideal por kg de aire seco.
// NO es la "desviación de entalpía", ni una propiedad absoluta tabulada RP-1485.
// Referencia explícita: T*=273,15 K, p_da*=101,325 kPa, p_v*=0,611657 kPa.
// s*(T,W,P)=c_p,da ln(T/T*)−R_da ln(p_da/p_da*) +
// W[c_p,v ln(T/T*)−R_v ln(p_v/p_v*)].
// Los términos de referencia son convencionales; comparaciones de estados
// requieren la misma referencia, composición y base de masa.
public static class MoistAirSpecificEntropy
{
    public const double ReferenceTemperatureK=273.15;
    public const double ReferenceDryAirPressureKPa=101.325;
    public const double ReferenceVaporPressureKPa=0.611657;
    public const double DryAirCpKJPerKgK=1.006;
    public const double WaterVaporCpKJPerKgK=1.86;
    public const double DryAirGasConstantKJPerKgK=0.287042;
    public const double WaterVaporGasConstantKJPerKgK=0.461522;
    private const double MolecularRatio=0.621945;

    public static double FromDryBulbAndHumidityRatio(double dryBulbC,
        double humidityRatioKgPerKg,double totalPressureKPa)
    {
        if(!double.IsFinite(dryBulbC)||dryBulbC<=-273.15||
           dryBulbC<SaturationVaporPressure.MinSupportedTemperatureC||
           dryBulbC>SaturationVaporPressure.MaxSupportedTemperatureC||
           !double.IsFinite(humidityRatioKgPerKg)||humidityRatioKgPerKg<0 ||
           !double.IsFinite(totalPressureKPa)||totalPressureKPa<=0)
            return double.NaN;
        double vapor=totalPressureKPa*humidityRatioKgPerKg/
            (MolecularRatio+humidityRatioKgPerKg);
        double dry=totalPressureKPa-vapor;
        if(dry<=0 || vapor<0 || vapor>=totalPressureKPa) return double.NaN;
        double saturation=SaturationVaporPressure.GetPressureKPa(dryBulbC);
        if(saturation<totalPressureKPa && vapor>saturation*(1+1e-7))
            return double.NaN;
        double t=(dryBulbC+273.15)/ReferenceTemperatureK;
        double result=DryAirCpKJPerKgK*Math.Log(t)-
            DryAirGasConstantKJPerKgK*Math.Log(dry/ReferenceDryAirPressureKPa);
        if(humidityRatioKgPerKg>0)
            result+=humidityRatioKgPerKg*(WaterVaporCpKJPerKgK*Math.Log(t)-
                WaterVaporGasConstantKJPerKgK*Math.Log(vapor/ReferenceVaporPressureKPa));
        return double.IsFinite(result)?result:double.NaN;
    }
}
