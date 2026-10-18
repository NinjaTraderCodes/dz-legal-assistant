using System.Text.Json;
using System.Text.Json.Nodes;
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

    /// <summary>
    /// Reads the shipped settings file and merges an optional <c>appsettings.local.json</c> beside it on top,
    /// key by key, so personal choices (e.g. the model) live in a file git ignores.
    /// </summary>
    public static AppSettings Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var merged = Read(path) ?? new JsonObject();
        var localPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, LocalFileName);
        if (Read(localPath) is { } local)
        {
            Merge(merged, local);
        }

        return merged.Deserialize<AppSettings>(DefinitionJson.Options) ?? new AppSettings();
    }

    public const string LocalFileName = "appsettings.local.json";

    private static JsonObject? Read(string path) =>
        File.Exists(path)
            ? JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })?.AsObject()
            : null;

    private static void Merge(JsonObject target, JsonObject overrides)
    {
        foreach (var (key, value) in overrides)
        {
            if (value is JsonObject child && target[key] is JsonObject existing)
            {
                Merge(existing, child);
            }
            else
            {
                target[key] = value?.DeepClone();
            }
        }
    }
}

public sealed record OllamaSettings
{
    public string Endpoint { get; init; } = "http://127.0.0.1:11434";
    public string Model { get; init; } = "gemma3:4b";

    /// <summary>
    /// Preferred when installed: the same language weights as <see cref="Model"/> without the unused image part (about
    /// 0.8 GB less memory, identical answers, CPU study of 18 Oct 2026). Created by tools/model/New-TextOnlyModel.ps1.
    /// Empty: always use <see cref="Model"/>.
    /// </summary>
    public string? TextOnlyModel { get; init; } = "dz-gemma3-4b-text";
    public double TimeoutMinutes { get; init; } = 10;
    public double Temperature { get; init; } = 0.2;
    public int MaxTokens { get; init; } = 1500;
    public double RepeatPenalty { get; init; } = 1.1;

    /// <summary>Set to false for reasoning models such as qwen3; leave null otherwise.</summary>
    public bool? Think { get; init; }

    /// <summary>Load the model into memory in the background when the app starts, so the first request is faster.</summary>
    public bool WarmUp { get; init; } = true;

    /// <summary>How long the model stays in memory after each request (Ollama duration, e.g. "60m").</summary>
    public string KeepAlive { get; init; } = "60m";
}
