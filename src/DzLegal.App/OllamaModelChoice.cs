using DzLegal.Drafting;
using DzLegal.ModelProviders.Ollama;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App;

/// <summary>
/// Which local model the app talks to. The text-only model (the same Gemma language weights without the unused image
/// part, about 0.8 GB less memory, identical answers; created by tools/model/New-TextOnlyModel.ps1) is preferred when
/// it is installed; otherwise the configured model. Decided by each probe, before the warm-up and the first request.
/// </summary>
public sealed class OllamaModelChoice
{
    private readonly OllamaOptions configured;
    private readonly string? textOnlyModel;

    /// <summary>The model the text-only one is built from (tools/model/New-TextOnlyModel.ps1).</summary>
    public const string TextOnlySource = "gemma3:4b";

    private volatile OllamaOptions _current;

    public OllamaModelChoice(OllamaOptions configured, string? textOnlyModel)
    {
        ArgumentNullException.ThrowIfNull(configured);
        this.configured = configured;
        this.textOnlyModel = textOnlyModel;
        _current = configured;
    }

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

        using var provider = new OllamaModelProvider(configured with { Timeout = TimeSpan.FromSeconds(3) }, handler);
        try
        {
            var installed = await provider.GetInstalledModelsAsync(cancellationToken);
            var model = Installed(installed, textOnlyModel) ? textOnlyModel!
                : Installed(installed, configured.Model) ? configured.Model
                : null;
            if (model is null)
            {
                return new ModelStatus(ModelState.ModelMissing, configured.Model);
            }

            _current = configured with { Model = model };
            return new ModelStatus(ModelState.Ready, model);
        }
        catch (Exception ex) when (ex is ModelProviderException or TaskCanceledException)
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
