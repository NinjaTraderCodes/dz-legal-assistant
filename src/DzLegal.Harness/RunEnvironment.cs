using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DzLegal.Harness;

/// <summary>
/// The machine state a bench ran in, for the run manifest: the hidden run of 30 October 2026 read prompts 1.5 times
/// slower than the same laptop the next day, and nothing had recorded whether it was on battery. Only what bears on the
/// timings is kept: the power source and scheme, the processor count, the OS, the local server's version and the
/// digests of the local models. No machine or user name.
/// </summary>
public static partial class RunEnvironment
{
    private static readonly TimeSpan Quick = TimeSpan.FromSeconds(3);

    public static IReadOnlyDictionary<string, string> Capture(IReadOnlyList<string> models, string? ollamaEndpoint)
    {
        ArgumentNullException.ThrowIfNull(models);
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["power"] = PowerSource(),
            ["powerScheme"] = PowerScheme(),
            ["processors"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            ["os"] = RuntimeInformation.OSDescription,
        };

        var local = models.Where(m => !HarnessModels.IsHosted(m) && m != "stub").ToList();
        if (local.Count > 0 && ollamaEndpoint is { Length: > 0 } && Uri.TryCreate(ollamaEndpoint, UriKind.Absolute, out var endpoint))
        {
            foreach (var (key, value) in Ollama(endpoint, local))
            {
                env[key] = value;
            }
        }

        return env;
    }

    /// <summary>"mains", "battery" or "unknown" (not Windows, no battery to ask, or the query failed).</summary>
    public static string PowerSource()
    {
        // Windows' battery status through CIM: BatteryStatus 2 = on mains, 1 = discharging. No native call needed.
        var output = Run("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_Battery | Select-Object -First 1).BatteryStatus\"");
        return output?.Trim() switch { "2" or "3" or "6" or "7" or "8" or "9" => "mains", "1" or "4" or "5" => "battery", _ => "unknown" };
    }

    /// <summary>The active Windows power scheme's name («Balanced», «High performance»), or "unknown".</summary>
    public static string PowerScheme()
    {
        var output = Run("powercfg", "/getactivescheme");
        return output is not null && SchemeName().Match(output) is { Success: true } m ? m.Groups[1].Value.Trim() : "unknown";
    }

    /// <summary>Runs a short system command and returns its output, or null off Windows or when it fails or hangs.</summary>
    private static string? Run(string file, string arguments)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(file, arguments) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            return process.WaitForExit((int)Quick.TotalMilliseconds) ? output : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>The local server's version and each local model's digest, when the server answers within a few seconds.</summary>
    private static List<KeyValuePair<string, string>> Ollama(Uri endpoint, IReadOnlyList<string> models)
    {
        var result = new List<KeyValuePair<string, string>>();
        try
        {
            using var http = new HttpClient { BaseAddress = endpoint, Timeout = Quick };
            var version = http.GetFromJsonAsync<JsonElement>(new Uri("api/version", UriKind.Relative)).GetAwaiter().GetResult();
            if (version.TryGetProperty("version", out var v))
            {
                result.Add(new("ollamaVersion", v.GetString() ?? "unknown"));
            }

            var tags = http.GetFromJsonAsync<JsonElement>(new Uri("api/tags", UriKind.Relative)).GetAwaiter().GetResult();
            if (tags.TryGetProperty("models", out var list))
            {
                foreach (var model in models)
                {
                    var digest = list.EnumerateArray()
                        .Where(m => m.TryGetProperty("name", out var n) && string.Equals(n.GetString(), model, StringComparison.Ordinal))
                        .Select(m => m.TryGetProperty("digest", out var d) ? d.GetString() : null)
                        .FirstOrDefault();
                    result.Add(new("digest:" + model, digest is { Length: >= 12 } ? digest[..12] : "unknown"));
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            result.Add(new("ollamaVersion", "unreachable"));
        }

        return result;
    }

    [GeneratedRegex(@"\(([^)]+)\)\s*$", RegexOptions.Multiline)]
    private static partial Regex SchemeName();
}
