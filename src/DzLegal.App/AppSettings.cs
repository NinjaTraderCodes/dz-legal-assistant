using System.Text.Json;
using DzLegal.Domain.Serialization;

namespace DzLegal.App;

/// <summary>
/// Settings read from appsettings.json next to the executable. The model endpoint must be on this
/// machine; a remote endpoint is refused when the provider is created.
/// </summary>
public sealed record AppSettings
{
    /// <summary>"ollama" (local model) or "stub" (template mode, no AI).</summary>
    public string DefaultProvider { get; init; } = "ollama";

    /// <summary>"ar" or "en".</summary>
    public string Language { get; init; } = "ar";

    public OllamaSettings Ollama { get; init; } = new();

    public static AppSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), DefinitionJson.Options) ?? new AppSettings();
    }
}

public sealed record OllamaSettings
{
    public string Endpoint { get; init; } = "http://127.0.0.1:11434";
    public string Model { get; init; } = "qwen2.5:7b-instruct";
    public double TimeoutMinutes { get; init; } = 10;
}
