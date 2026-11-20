using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;

namespace FrioCalc.Psychrometrics.Core;

// [1.12] Actualizaciones únicamente desde releases del repositorio oficial.
public static class ReleaseUpdateService
{
    public const string Repository = "colombianitov2/Carta-psicometrica";
    public const string RenamedRepository = "colombianitov2/Calculadora-de-refrigeracion-Parte-1-Carta-psicometrica";
    public const string RenamedReleasesPage = "https://github.com/" + RenamedRepository + "/releases";
    public const string OfficialProductName = "Calculadora de refrigeración – Parte 1 – Carta psicrométrica";
    public const string LegacyProductName = "FrioCalc.Psychrometrics";
    // Conservar el ID y las rutas históricas durante la migración de clientes.
    public static bool IsCompatibleProductName(string? name) =>
        name == LegacyProductName || name == OfficialProductName;
    public const string ReleasesPage = "https://github.com/" + Repository + "/releases";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(5) };
    static ReleaseUpdateService() => Client.DefaultRequestHeaders.UserAgent.ParseAdd("CalculadoraRefrigeracionParte1/1.0");

    public readonly record struct ReleaseVersion(int Major, int Minor, int Patch, int? Beta) : IComparable<ReleaseVersion>
    {
        public int CompareTo(ReleaseVersion other)
        {
            int comparison = Major.CompareTo(other.Major);
            if (comparison == 0) comparison = Minor.CompareTo(other.Minor);
            if (comparison == 0) comparison = Patch.CompareTo(other.Patch);
            return comparison != 0 ? comparison : (Beta ?? int.MaxValue).CompareTo(other.Beta ?? int.MaxValue);
        }
        public override string ToString() => $"{Major}.{Minor}.{Patch}" + (Beta.HasValue ? $"-beta.{Beta}" : "");
        public static bool TryParse(string? value, out ReleaseVersion version)
        {
            version = default;
            if (string.IsNullOrEmpty(value)) return false;
            var match = System.Text.RegularExpressions.Regex.Match(value, @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-beta\.([1-9]\d*))?(?:\+[A-Za-z0-9.-]+)?$");
            if (!match.Success || !int.TryParse(match.Groups[1].Value,out int major) ||
                !int.TryParse(match.Groups[2].Value,out int minor) || !int.TryParse(match.Groups[3].Value,out int patch)) return false;
            int? beta = null;
            if (match.Groups[4].Success)
            {
                if (!int.TryParse(match.Groups[4].Value,out int n) || n == int.MaxValue) return false;
                beta = n;
            }
            version = new(major,minor,patch,beta);
            return true;
        }
    }

    public sealed record ReleaseAsset(string Name, Uri Url, long Size, string? Digest);
    public sealed record UpdateRelease(ReleaseVersion Version, ReleaseAsset Executable, ReleaseAsset Checksums);
    public static ReleaseVersion CurrentVersion
    {
        get
        {
            var value = typeof(ReleaseUpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return ReleaseVersion.TryParse(value,out var version) ? version : new(1,0,0,4);
        }
    }

    public static bool IsPublished => Environment.ProcessPath is { } path &&
        !File.Exists(Path.Combine(Path.GetDirectoryName(path)!,"FrioCalc.Psychrometrics.dll"));
    public static bool IsInstalled
    {
        get
        {
            var path = Environment.ProcessPath;
            if (path is null) return false;
            var location = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\{BFE39269-7325-4B56-A122-8623E6CD85DF}_is1", "InstallLocation", null) as string;
            return !string.IsNullOrWhiteSpace(location) && string.Equals(Path.GetFullPath(Path.Combine(location,"FrioCalc.Psychrometrics.exe")),path,StringComparison.OrdinalIgnoreCase);
        }
    }

    // Incluye betas: /releases/latest excluye las versiones preliminares.
    public static async Task<UpdateRelease?> CheckAsync(bool installed, CancellationToken cancellationToken)
    {
        UpdateRelease? best = null;
        for (int page = 1; ; page++)
        {
            using var response = await Client.GetAsync($"https://api.github.com/repos/{Repository}/releases?per_page=100&page={page}",cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var candidate = FindUpdate(document.RootElement,CurrentVersion,installed);
            if (candidate is not null && (best is null || candidate.Version.CompareTo(best.Version)>0)) best = candidate;
            if (document.RootElement.GetArrayLength()<100) return best;
        }
    }

    public static UpdateRelease? FindUpdate(JsonElement releases, ReleaseVersion current, bool installed)
    {
        UpdateRelease? best = null;
        foreach (var release in releases.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean() || !ReleaseVersion.TryParse(release.GetProperty("tag_name").GetString(),out var version) ||
                version.CompareTo(current)<=0 || (best is not null && version.CompareTo(best.Version)<=0)) continue;
            // Una versión estable no debe migrar inadvertidamente a una beta futura.
            if (current.Beta is null && version.Beta is not null) continue;
            var tag = release.GetProperty("tag_name").GetString()!;
            string stem = version.Beta.HasValue && version is { Major:1,Minor:0,Patch:0 }
                ? $"FrioCalc-Beta{version.Beta}" : $"FrioCalc-{version}";
            string name = $"{stem}-win-x64-{(installed?"Setup":"Portable")}.exe";
            ReleaseAsset? package = null, hashes = null;
            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                var assetName=asset.GetProperty("name").GetString();
                if (assetName!=name && assetName!="SHA256SUMS.txt") continue;
                if (asset.TryGetProperty("state",out var state) && state.GetString()!="uploaded") continue;
                string legacyUrl=$"{ReleasesPage}/download/{tag}/{assetName}";
                string renamedUrl=$"{RenamedReleasesPage}/download/{tag}/{assetName}";
                string? url=asset.GetProperty("browser_download_url").GetString();
                if (url is null || (url != legacyUrl && url != renamedUrl)) continue;
                long size=asset.GetProperty("size").GetInt64();
                if (size<=0 || (assetName=="SHA256SUMS.txt" && size>65536)) continue;
                var item=new ReleaseAsset(assetName!,new Uri(url),size,asset.TryGetProperty("digest",out var digest)?digest.GetString():null);
                if (assetName==name) package=item; else hashes=item;
            }
            if (package is not null && hashes is not null) best=new(version,package,hashes);
        }
        return best;
    }

    public static string ReadChecksum(string manifest, string name)
    {
        var matches=manifest.Split('\n').Select(line=>line.Trim()).Select(line=>line.Split(' ',StringSplitOptions.RemoveEmptyEntries))
            .Where(parts=>parts.Length==2 && parts[1]==name).ToArray();
        if(matches.Length!=1 || matches[0][0].Length!=64 || !matches[0][0].All(Uri.IsHexDigit))
            throw new InvalidDataException("No se encontró una suma SHA-256 válida y única para el paquete.");
        return matches[0][0];
    }

    public static async Task<string> DownloadAsync(UpdateRelease release, IProgress<int> progress, CancellationToken cancellationToken)
    {
        string manifest=await Client.GetStringAsync(release.Checksums.Url,cancellationToken);
        string expected=ReadChecksum(manifest,release.Executable.Name);
        if(release.Executable.Digest is {Length:>0} digest && !string.Equals(digest,"sha256:"+expected,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("La suma de publicación no coincide con el registro de GitHub.");
        string folder=Path.Combine(Path.GetTempPath(),"FrioCalc-update-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,release.Executable.Name);
        try
        {
            using var response=await Client.GetAsync(release.Executable.Url,HttpCompletionOption.ResponseHeadersRead,cancellationToken);
            response.EnsureSuccessStatusCode();
            using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using var input=await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true))
            {
                byte[] buffer=new byte[81920]; long total=0; int read;
                while((read=await input.ReadAsync(buffer,cancellationToken))>0)
                {
                    total+=read;
                    if(total>release.Executable.Size) throw new InvalidDataException("Tamaño del paquete incorrecto.");
                    hash.AppendData(buffer,0,read); await output.WriteAsync(buffer.AsMemory(0,read),cancellationToken);
                    progress.Report((int)(100*total/release.Executable.Size));
                }
                if(total!=release.Executable.Size || !string.Equals(Convert.ToHexString(hash.GetHashAndReset()),expected,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("El paquete no superó la verificación SHA-256.");
            }
            return path;
        }
        catch { if(File.Exists(path)) File.Delete(path); Directory.Delete(folder); throw; }
    }

    public static void StartUpdate(string package, bool installed)
    {
        var start=new ProcessStartInfo(package) {UseShellExecute=false};
        if(!installed)
        {
            start.ArgumentList.Add("--apply-portable-update");
            start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            start.ArgumentList.Add(Environment.ProcessPath ?? throw new InvalidOperationException("No se encontró el ejecutable actual."));
        }
        using var process=Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar la actualización.");
    }

    // Ejecutado por la nueva copia, después de cerrar la anterior. File.Replace conserva una copia de seguridad.
    public static void ApplyPortableUpdate(string[] args)
    {
        if(args.Length!=3 || !int.TryParse(args[1],out int pid) || pid<=0 || pid==Environment.ProcessId) throw new InvalidDataException("Solicitud de actualización incorrecta.");
        string source=Environment.ProcessPath ?? throw new InvalidOperationException("No se encontró el paquete.");
        string target=Path.GetFullPath(args[2]);
        if(!File.Exists(target) || string.Equals(source,target,StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(target),".exe",StringComparison.OrdinalIgnoreCase) ||
            !IsCompatibleProductName(FileVersionInfo.GetVersionInfo(target).ProductName) ||
            !IsCompatibleProductName(FileVersionInfo.GetVersionInfo(source).ProductName)) throw new InvalidDataException("El destino no corresponde a la calculadora.");
        try
        {
            using var former=Process.GetProcessById(pid);
            if (!former.HasExited && !string.Equals(former.MainModule?.FileName,target,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El proceso de destino no corresponde a la calculadora.");
            if (!former.WaitForExit(30000)) throw new IOException("Cierre todas las ventanas de la calculadora e inténtelo nuevamente.");
        }
        catch(ArgumentException) { /* El proceso anterior ya se cerró. */ }
        string staged=target+".new-"+Guid.NewGuid().ToString("N");
        string backup=target+".previous-"+Guid.NewGuid().ToString("N")+".bak";
        try
        {
            File.Copy(source,staged);
            File.Replace(staged,target,backup);
            try { using var restarted=Process.Start(new ProcessStartInfo(target){UseShellExecute=true}) ?? throw new IOException("No se pudo iniciar la calculadora."); }
            catch { File.Replace(backup,target,null); throw; }
        }
        finally { if(File.Exists(staged)) File.Delete(staged); }
    }
}
