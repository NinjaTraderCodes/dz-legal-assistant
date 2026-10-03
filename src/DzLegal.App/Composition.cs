using DzLegal.App.Services;
using DzLegal.App.ViewModels;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.ModelProviders.Ollama;
using DzLegal.ModelProviders.Stub;
using DzLegal.Storage;

namespace DzLegal.App;

/// <summary>Wires the application together. Everything is local: data folder, settings, storage, model.</summary>
public static class Composition
{
    public static string DataRoot => Path.Combine(AppContext.BaseDirectory, "data", "document-types");

    /// <summary>The offline law library and claim catalog (never downloaded by the app).</summary>
    public static string LawRoot => Path.Combine(AppContext.BaseDirectory, "data", "law");

    public static IReadOnlyList<ProviderOption> CreateProviders(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var ollama = new OllamaOptions
        {
            Endpoint = new Uri(settings.Ollama.Endpoint),
            Model = settings.Ollama.Model,
            Timeout = TimeSpan.FromMinutes(settings.Ollama.TimeoutMinutes),
            Temperature = settings.Ollama.Temperature,
            MaxTokens = settings.Ollama.MaxTokens,
            RepeatPenalty = settings.Ollama.RepeatPenalty,
            Think = settings.Ollama.Think,
        };

        return
        [
            new ProviderOption("ollama", loc => loc.Format("ProviderOllama", ollama.Model), () => new OllamaModelProvider(ollama), ct => ProbeOllamaAsync(ollama, ct)),
            new ProviderOption("stub", loc => loc["ProviderStub"], () => new StubModelProvider()),
        ];
    }

    /// <summary>Loopback-only check: is Ollama running, and is the configured model installed?</summary>
    public static async Task<ModelStatus> ProbeOllamaAsync(OllamaOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        using var provider = new OllamaModelProvider(options with { Timeout = TimeSpan.FromSeconds(3) });
        try
        {
            var models = await provider.GetInstalledModelsAsync(cancellationToken);
            var wanted = options.Model.Contains(':', StringComparison.Ordinal) ? options.Model : options.Model + ":latest";
            return models.Any(m => string.Equals(m, wanted, StringComparison.OrdinalIgnoreCase))
                ? new ModelStatus(ModelState.Ready, options.Model)
                : new ModelStatus(ModelState.ModelMissing, options.Model);
        }
        catch (Exception ex) when (ex is ModelProviderException or TaskCanceledException)
        {
            return new ModelStatus(ModelState.NotRunning, options.Model);
        }
    }

    public static MainWindowViewModel CreateMainViewModel(AppSettings settings, IUserFiles files, AppPaths paths, Loc loc, IExternalLinks? links = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(loc);
        loc.SetLanguage(settings.Language);

        // Target 1 ships one document type; the loader already supports more.
        var definition = DefinitionLoader.Load(Path.Combine(DataRoot, "requete-introductive"));
        return new MainWindowViewModel(
            definition,
            CreateProviders(settings),
            files,
            new EvaluationStore(paths.Evaluations),
            loc,
            settings.DefaultProvider,
            autosaveFile: Path.Combine(paths.Drafts, "last-session" + DraftFile.Extension),
            knowledge: DzLegal.Law.LegalKnowledgeLoader.Load(LawRoot),
            links: links);
    }
}
