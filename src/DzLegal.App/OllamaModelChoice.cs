using System.Text.Json;
using DzLegal.App.Services.LocalModels;
using DzLegal.Drafting;
using DzLegal.ModelProviders.Ollama;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App;

/// <summary>
/// Chooses verified local installed models; automatic selection prefers Gemma 4, then Gemma 3.
/// Explicit advanced model settings are preserved. Thinking is resolved after each selection.
/// </summary>
public sealed class OllamaModelChoice
{
    private readonly OllamaOptions configured;
    private readonly string? textOnlyModel;
    private readonly ILocalEngine? engine;

    /// <summary>The model the text-only one is built from (tools/model/New-TextOnlyModel.ps1).</summary>
    public const string TextOnlySource = "gemma3:4b";

    private volatile OllamaOptions _current;

    public OllamaModelChoice(OllamaOptions configured, string? textOnlyModel, ILocalEngine? engine = null)
    {
        ArgumentNullException.ThrowIfNull(configured);
        this.configured = configured;
        this.textOnlyModel = textOnlyModel;
        this.engine = engine;
        _current = configured with { Think = ThinkFor(configured.Model, configured.Think) };
    }

    /// <summary>
    /// The thinking switch to send: the setting when it is given; otherwise off for Gemma 4, and unset for every other
    /// model (Ollama refuses the switch for models without it). Gemma 4 thinks before answering unless told not to, and
    /// with thinking on it spent the whole answer allowance thinking on every fact reading (gemma4:e4b, 40 of 40, 7 Nov
    /// 2026); with it off it answered all 40 (docs/PRODUCT-DETAILS.md).
    /// </summary>
    public static bool? ThinkFor(string model, bool? configured) => configured ?? (IsGemma4(model) ? false : null);

    /// <summary>gemma4, gemma4:e4b, gemma4:e2b…: the Gemma 4 family as Ollama names it.</summary>
    public static bool IsGemma4(string? model) =>
        model is not null && (model.Equals("gemma4", StringComparison.OrdinalIgnoreCase) || model.StartsWith("gemma4:", StringComparison.OrdinalIgnoreCase));

    /// <summary>The text-only model to prefer for these settings: only in place of <see cref="TextOnlySource"/>.</summary>
    public static string? TextOnlyModelFor(string configuredModel, string? textOnlyModel) =>
        string.Equals(configuredModel, TextOnlySource, StringComparison.OrdinalIgnoreCase) ? textOnlyModel : null;

    /// <summary>The options to send now: the configured ones, with the model the last probe found installed.</summary>
    public OllamaOptions Options => _current;

    /// <summary>Loopback-only check: is Ollama running, and which wanted model is installed?</summary>
    public async Task<ModelStatus> ProbeAsync(CancellationToken cancellationToken, HttpMessageHandler? handler = null)
    {
        if (LoopbackGuard.IsCloudModel(configured.Model))
        {
            return new ModelStatus(ModelState.Refused, configured.Model);
        }

        try
        {
            var endpoint = engine is null ? configured.Endpoint : (await engine.EnsureAsync(cancellationToken)).Endpoint;
            using var api = new LocalOllamaApi(endpoint, handler);
            var installed = await api.InstalledAsync(cancellationToken);
            var automatic = configured.Model is "gemma3:4b" or "gemma4:e4b";
            var candidates = automatic ? new[] { "gemma4:e4b", textOnlyModel, "gemma3:4b" } : new[] { configured.Model };
            foreach (var model in candidates.Where(m => Installed(installed, m)))
            {
                if (!await api.IsLocalModelAsync(model!, cancellationToken)) continue;
                _current = configured with { Endpoint = endpoint, Model = model!, Think = ThinkFor(model!, configured.Think) };
                return new ModelStatus(ModelState.Ready, model!);
            }
            return new ModelStatus(ModelState.ModelMissing, configured.Model);
        }
        catch (Exception ex) when (ex is ModelProviderException or HttpRequestException or OperationCanceledException
            or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or JsonException or InvalidOperationException)
        {
            return new ModelStatus(ModelState.NotRunning, configured.Model);
        }
    }

    private static bool Installed(IReadOnlyList<string> installed, string? model)
    {
        if (string.IsNullOrWhiteSpace(model) || LoopbackGuard.IsCloudModel(model))
        {
            return false;
        }

        var wanted = model.Contains(':', StringComparison.Ordinal) ? model : model + ":latest";
        return installed.Any(m => string.Equals(m, wanted, StringComparison.OrdinalIgnoreCase));
    }
}
