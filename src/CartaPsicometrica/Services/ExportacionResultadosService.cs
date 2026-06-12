using System.Globalization;
using System.Text;
using CartaPsicometrica.Models;

namespace CartaPsicometrica.Services;

public sealed class ExportacionResultadosService
{
    public string CrearCsv(EstadoPsicometrico puntoA, EstadoPsicometrico puntoB, ProcesoPsicometrico proceso)
    {
        StringBuilder csv = new();

        csv.AppendLine("Seccion;Propiedad;Valor;Unidad");
        AgregarEstado(csv, "Punto A", puntoA);
        AgregarEstado(csv, "Punto B", puntoB);
        AgregarProceso(csv, proceso);

        return csv.ToString();
    }

    private static void AgregarEstado(StringBuilder csv, string seccion, EstadoPsicometrico estado)
    {
        Agregar(csv, seccion, "Temperatura de bulbo seco", estado.TemperaturaBulboSecoC, "°C");
        Agregar(csv, seccion, "Humedad relativa", estado.HumedadRelativa * 100.0, "%");
        Agregar(csv, seccion, "Razon de humedad", estado.RazonHumedadKgKg * 1000.0, "g/kg aire seco");
        Agregar(csv, seccion, "Temperatura de bulbo humedo", estado.TemperaturaBulboHumedoC, "°C");
        Agregar(csv, seccion, "Punto de rocio", estado.TemperaturaPuntoRocioC, "°C");
        Agregar(csv, seccion, "Entalpia", estado.EntalpiaKJKgAireSeco, "kJ/kg aire seco");
        Agregar(csv, seccion, "Presion parcial de vapor", estado.PresionParcialVaporPa / 1000.0, "kPa");
        Agregar(csv, seccion, "Presion de vapor saturado", estado.PresionVaporSaturadoPa / 1000.0, "kPa");
        Agregar(csv, seccion, "Calor especifico aproximado", estado.CalorEspecificoKJKgK, "kJ/kg aire seco·K");
        Agregar(csv, seccion, "Volumen especifico", estado.VolumenEspecificoM3KgAireSeco, "m³/kg aire seco");
        Agregar(csv, seccion, "Densidad", estado.DensidadKgM3, "kg/m³");
        Agregar(csv, seccion, "Grado de saturacion", estado.GradoSaturacion * 100.0, "%");
    }

    private static void AgregarProceso(StringBuilder csv, ProcesoPsicometrico proceso)
    {
        Agregar(csv, "Proceso A-B", "Tipo de proceso", proceso.Tipo, "-");
        Agregar(csv, "Proceso A-B", "Delta T", proceso.DeltaTemperaturaC, "°C");
        Agregar(csv, "Proceso A-B", "Delta W", proceso.DeltaRazonHumedadKgKg * 1000.0, "g/kg aire seco");
        Agregar(csv, "Proceso A-B", "Delta h", proceso.DeltaEntalpiaKJKg, "kJ/kg aire seco");
        Agregar(csv, "Proceso A-B", "Relacion Delta h / Delta W", proceso.RelacionDeltaHDeltaW, "kJ/kg agua");
        Agregar(csv, "Proceso A-B", "Carga sensible", proceso.CargaSensibleKW, "kW");
        Agregar(csv, "Proceso A-B", "Carga latente", proceso.CargaLatenteKW, "kW");
        Agregar(csv, "Proceso A-B", "Carga total", proceso.CargaTotalKW, "kW");
        Agregar(csv, "Proceso A-B", "Factor de calor sensible", proceso.FactorCalorSensible, "-");
        Agregar(csv, "Proceso A-B", "Condensado", proceso.CondensadoKgH, "kg/h");
        Agregar(csv, "Proceso A-B", "Humidificacion", proceso.HumidificacionKgH, "kg/h");
    }

    private static void Agregar(StringBuilder csv, string seccion, string propiedad, double valor, string unidad)
    {
        string texto = double.IsNaN(valor) ? "No definido" : valor.ToString("0.######", CultureInfo.InvariantCulture);
        Agregar(csv, seccion, propiedad, texto, unidad);
    }

    private static void Agregar(StringBuilder csv, string seccion, string propiedad, string valor, string unidad)
    {
        csv.Append(Escape(seccion));
        csv.Append(';');
        csv.Append(Escape(propiedad));
        csv.Append(';');
        csv.Append(Escape(valor));
        csv.Append(';');
        csv.AppendLine(Escape(unidad));
    }

    private static string Escape(string value)
    {
        return value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
