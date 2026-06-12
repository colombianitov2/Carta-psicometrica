using System.Text.Json;
using System.IO;
using CartaPsicometrica.Models;

namespace CartaPsicometrica.Services;

public sealed class ConfiguracionService
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = true
    };

    public void Guardar(string rutaArchivo, ConfiguracionPsicometrica configuracion)
    {
        string json = JsonSerializer.Serialize(configuracion, OpcionesJson);
        File.WriteAllText(rutaArchivo, json);
    }
}
