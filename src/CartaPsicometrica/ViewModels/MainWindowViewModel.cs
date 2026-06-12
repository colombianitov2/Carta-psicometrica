using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using CartaPsicometrica.Models;
using CartaPsicometrica.Services;
using Microsoft.Win32;

namespace CartaPsicometrica.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly PsicometriaService psicometriaService = new();
    private readonly ExportacionResultadosService exportacionService = new();
    private readonly ConfiguracionService configuracionService = new();

    private string presionKPaTexto = "101.325";
    private string flujoAireSecoTexto = "1.00";
    private string puntoATbsTexto = "26";
    private string puntoAHrTexto = "55";
    private string puntoBTbsTexto = "14";
    private string puntoBHrTexto = "90";
    private string estadoTexto = "Listo para calcular.";
    private string mensajeError = "";
    private double presionPaActual = 101325.0;
    private EstadoPsicometrico? puntoA;
    private EstadoPsicometrico? puntoB;
    private ProcesoPsicometrico? proceso;

    public MainWindowViewModel()
    {
        CalcularCommand = new RelayCommand(Calcular);
        LimpiarCommand = new RelayCommand(Limpiar);
        ExportarResultadosCommand = new RelayCommand(ExportarResultados);
        GuardarConfiguracionCommand = new RelayCommand(GuardarConfiguracion);

        Calcular();
    }

    public string PresionKPaTexto
    {
        get => presionKPaTexto;
        set => SetProperty(ref presionKPaTexto, value);
    }

    public string FlujoAireSecoTexto
    {
        get => flujoAireSecoTexto;
        set => SetProperty(ref flujoAireSecoTexto, value);
    }

    public string PuntoATbsTexto
    {
        get => puntoATbsTexto;
        set => SetProperty(ref puntoATbsTexto, value);
    }

    public string PuntoAHrTexto
    {
        get => puntoAHrTexto;
        set => SetProperty(ref puntoAHrTexto, value);
    }

    public string PuntoBTbsTexto
    {
        get => puntoBTbsTexto;
        set => SetProperty(ref puntoBTbsTexto, value);
    }

    public string PuntoBHrTexto
    {
        get => puntoBHrTexto;
        set => SetProperty(ref puntoBHrTexto, value);
    }

    public string EstadoTexto
    {
        get => estadoTexto;
        private set => SetProperty(ref estadoTexto, value);
    }

    public string MensajeError
    {
        get => mensajeError;
        private set => SetProperty(ref mensajeError, value);
    }

    public double PresionPaActual
    {
        get => presionPaActual;
        private set => SetProperty(ref presionPaActual, value);
    }

    public EstadoPsicometrico? PuntoA
    {
        get => puntoA;
        private set => SetProperty(ref puntoA, value);
    }

    public EstadoPsicometrico? PuntoB
    {
        get => puntoB;
        private set => SetProperty(ref puntoB, value);
    }

    public ProcesoPsicometrico? Proceso
    {
        get => proceso;
        private set => SetProperty(ref proceso, value);
    }

    public ObservableCollection<FilaResultadoViewModel> ResultadosPuntoA { get; } = new();

    public ObservableCollection<FilaResultadoViewModel> ResultadosPuntoB { get; } = new();

    public ObservableCollection<FilaResultadoViewModel> ResultadosProceso { get; } = new();

    public ICommand CalcularCommand { get; }

    public ICommand LimpiarCommand { get; }

    public ICommand ExportarResultadosCommand { get; }

    public ICommand GuardarConfiguracionCommand { get; }

    private void Calcular()
    {
        try
        {
            double presionKPa = LeerNumero(PresionKPaTexto, "presión atmosférica");
            double flujoAireSeco = LeerNumero(FlujoAireSecoTexto, "flujo de aire seco");
            double tbsA = LeerNumero(PuntoATbsTexto, "TBS del Punto A");
            double hrA = LeerNumero(PuntoAHrTexto, "HR del Punto A");
            double tbsB = LeerNumero(PuntoBTbsTexto, "TBS del Punto B");
            double hrB = LeerNumero(PuntoBHrTexto, "HR del Punto B");

            double presionPa = presionKPa * 1000.0;
            EstadoPsicometrico estadoA = psicometriaService.CalcularDesdeTbsHr("A", tbsA, hrA, presionPa);
            EstadoPsicometrico estadoB = psicometriaService.CalcularDesdeTbsHr("B", tbsB, hrB, presionPa);
            ProcesoPsicometrico procesoCalculado = psicometriaService.CalcularProceso(estadoA, estadoB, flujoAireSeco);

            PresionPaActual = presionPa;
            PuntoA = estadoA;
            PuntoB = estadoB;
            Proceso = procesoCalculado;

            CargarResultadosEstado(ResultadosPuntoA, estadoA);
            CargarResultadosEstado(ResultadosPuntoB, estadoB);
            CargarResultadosProceso(ResultadosProceso, procesoCalculado);

            MensajeError = "";
            EstadoTexto = $"Cálculo actualizado: {procesoCalculado.Tipo}.";
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
            EstadoTexto = "Revise las entradas marcadas por el mensaje.";
        }
    }

    private void Limpiar()
    {
        PuntoATbsTexto = "";
        PuntoAHrTexto = "";
        PuntoBTbsTexto = "";
        PuntoBHrTexto = "";
        ResultadosPuntoA.Clear();
        ResultadosPuntoB.Clear();
        ResultadosProceso.Clear();
        PuntoA = null;
        PuntoB = null;
        Proceso = null;
        MensajeError = "";
        EstadoTexto = "Entradas y resultados limpiados.";
    }

    private void ExportarResultados()
    {
        if (!AsegurarCalculoDisponible())
            return;

        SaveFileDialog dialog = new()
        {
            Title = "Exportar resultados psicrométricos",
            Filter = "Archivo CSV (*.csv)|*.csv",
            FileName = $"resultados_psicrometricos_{DateTime.Now:yyyyMMdd_HHmm}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() != true || PuntoA == null || PuntoB == null || Proceso == null)
            return;

        File.WriteAllText(dialog.FileName, exportacionService.CrearCsv(PuntoA, PuntoB, Proceso));
        EstadoTexto = $"Resultados exportados: {Path.GetFileName(dialog.FileName)}.";
    }

    private void GuardarConfiguracion()
    {
        try
        {
            ConfiguracionPsicometrica configuracion = CrearConfiguracionDesdeEntradas();

            SaveFileDialog dialog = new()
            {
                Title = "Guardar configuración psicrométrica",
                Filter = "Archivo JSON (*.json)|*.json",
                FileName = "configuracion_carta_psicometrica.json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            if (dialog.ShowDialog() != true)
                return;

            configuracionService.Guardar(dialog.FileName, configuracion);
            MensajeError = "";
            EstadoTexto = $"Configuración guardada: {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
            EstadoTexto = "No se pudo guardar la configuración.";
        }
    }

    private bool AsegurarCalculoDisponible()
    {
        if (PuntoA != null && PuntoB != null && Proceso != null)
            return true;

        Calcular();
        return PuntoA != null && PuntoB != null && Proceso != null;
    }

    private ConfiguracionPsicometrica CrearConfiguracionDesdeEntradas()
    {
        return new ConfiguracionPsicometrica(
            LeerNumero(PresionKPaTexto, "presión atmosférica"),
            LeerNumero(FlujoAireSecoTexto, "flujo de aire seco"),
            LeerNumero(PuntoATbsTexto, "TBS del Punto A"),
            LeerNumero(PuntoAHrTexto, "HR del Punto A"),
            LeerNumero(PuntoBTbsTexto, "TBS del Punto B"),
            LeerNumero(PuntoBHrTexto, "HR del Punto B"));
    }

    private static void CargarResultadosEstado(ObservableCollection<FilaResultadoViewModel> destino, EstadoPsicometrico estado)
    {
        destino.Clear();
        destino.Add(Fila("TBS", estado.TemperaturaBulboSecoC, "°C", "0.00"));
        destino.Add(Fila("HR", estado.HumedadRelativa * 100.0, "%", "0.00"));
        destino.Add(Fila("W", estado.RazonHumedadKgKg * 1000.0, "g/kg aire seco", "0.000"));
        destino.Add(Fila("TBH", estado.TemperaturaBulboHumedoC, "°C", "0.00"));
        destino.Add(Fila("Punto de rocío", estado.TemperaturaPuntoRocioC, "°C", "0.00"));
        destino.Add(Fila("Entalpía", estado.EntalpiaKJKgAireSeco, "kJ/kg aire seco", "0.00"));
        destino.Add(Fila("Presión parcial de vapor", estado.PresionParcialVaporPa / 1000.0, "kPa", "0.000"));
        destino.Add(Fila("Presión de vapor saturado", estado.PresionVaporSaturadoPa / 1000.0, "kPa", "0.000"));
        destino.Add(Fila("Calor específico aprox.", estado.CalorEspecificoKJKgK, "kJ/kg aire seco·K", "0.000"));
        destino.Add(Fila("Volumen específico", estado.VolumenEspecificoM3KgAireSeco, "m³/kg aire seco", "0.0000"));
        destino.Add(Fila("Densidad", estado.DensidadKgM3, "kg/m³", "0.000"));
        destino.Add(Fila("Grado de saturación", estado.GradoSaturacion * 100.0, "%", "0.00"));
    }

    private static void CargarResultadosProceso(ObservableCollection<FilaResultadoViewModel> destino, ProcesoPsicometrico proceso)
    {
        destino.Clear();
        destino.Add(new FilaResultadoViewModel("Tipo", proceso.Tipo, "-"));
        destino.Add(Fila("ΔT", proceso.DeltaTemperaturaC, "°C", "0.00"));
        destino.Add(Fila("ΔW", proceso.DeltaRazonHumedadKgKg * 1000.0, "g/kg aire seco", "0.000"));
        destino.Add(Fila("Δh", proceso.DeltaEntalpiaKJKg, "kJ/kg aire seco", "0.00"));
        destino.Add(Fila("Δh / ΔW", proceso.RelacionDeltaHDeltaW, "kJ/kg agua", "0.00"));
        destino.Add(Fila("Carga sensible", proceso.CargaSensibleKW, "kW", "0.00"));
        destino.Add(Fila("Carga latente", proceso.CargaLatenteKW, "kW", "0.00"));
        destino.Add(Fila("Carga total", proceso.CargaTotalKW, "kW", "0.00"));
        destino.Add(Fila("SHR / FCS", proceso.FactorCalorSensible, "-", "0.000"));
        destino.Add(Fila("Condensado", proceso.CondensadoKgH, "kg/h", "0.000"));
        destino.Add(Fila("Humidificación", proceso.HumidificacionKgH, "kg/h", "0.000"));
    }

    private static FilaResultadoViewModel Fila(string propiedad, double valor, string unidad, string formato)
    {
        string valorTexto = double.IsNaN(valor)
            ? "No definido"
            : valor.ToString(formato, CultureInfo.InvariantCulture);

        return new FilaResultadoViewModel(propiedad, valorTexto, unidad);
    }

    private static double LeerNumero(string texto, string nombreCampo)
    {
        string normalizado = (texto ?? "").Trim().Replace(",", ".");

        if (!double.TryParse(normalizado, NumberStyles.Float, CultureInfo.InvariantCulture, out double valor))
            throw new InvalidOperationException($"El valor de {nombreCampo} no es válido.");

        return valor;
    }
}
