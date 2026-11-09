using System.Net.Http.Json;
using System.Text.Json;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.App.Services.LocalModels;

/// <summary>Setup requests contain model identities only; case inference keeps its existing provider.</summary>
public sealed class LocalOllamaApi : IDisposable
{
    // The pinned Gemma 4 GGUF config (7ca1ae56...) declares requires=0.30.9;
    // Gemma 3 support predates it (0.6.0). See MODEL-SETUP-IMPLEMENTATION.md.
    public static Version MinimumVersion { get; } = new(0, 30, 9);

    private readonly HttpClient http;

    public LocalOllamaApi(Uri endpoint, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!LoopbackGuard.IsLoopback(endpoint))
            throw new ArgumentException("Only local model endpoints are accepted.", nameof(endpoint));
        http = new HttpClient(handler ?? LoopbackGuard.CreateHandler())
        {
            BaseAddress = new Uri(endpoint.AbsoluteUri.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    public async Task<bool> IsCompatibleAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync("api/version", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        return json.RootElement.TryGetProperty("version", out var version) && version.ValueKind == JsonValueKind.String
            && Version.TryParse(version.GetString(), out var parsed) && parsed.Build >= 0 && parsed >= MinimumVersion;
    }

    public async Task<IReadOnlyList<string>> InstalledAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!json.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
            return [];
        return models.EnumerateArray().Where(m => m.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
            .Select(m => m.GetProperty("name").GetString()!).ToArray();
    }

    public async Task<bool> IsLocalModelAsync(string model, CancellationToken cancellationToken)
    {
        if (LoopbackGuard.IsCloudModel(model)) return false;
        using var response = await http.PostAsJsonAsync("api/show", new { model }, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return false;
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = json.RootElement;
        return !HasRemote(root, "remote_host") && !HasRemote(root, "remote_model")
            && root.TryGetProperty("details", out var details)
            && details.TryGetProperty("format", out var format) && format.GetString() == "gguf"
            && root.TryGetProperty("model_info", out var info) && info.ValueKind == JsonValueKind.Object
            && info.EnumerateObject().Any();
    }

    private static bool HasRemote(JsonElement root, string property) => root.TryGetProperty(property, out var value)
        && value.ValueKind != JsonValueKind.Null && (value.ValueKind != JsonValueKind.String || !string.IsNullOrWhiteSpace(value.GetString()));

    public void Dispose() => http.Dispose();
}