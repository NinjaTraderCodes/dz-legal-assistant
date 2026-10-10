using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Law;
using DzLegal.ModelProviders.Hosted;
using DzLegal.ModelProviders.Stub;
using DzLegal.Presentation;
using DzLegal.Presentation.Services;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.Web.Services;

/// <summary>
/// Everything the visitors share, loaded once: the document definition, the law library and claim catalog, the sample
/// cases, the hosted engines and the queue in front of them. Each browser session gets its own view model from here.
/// </summary>
public sealed partial class WebEngine : IDisposable
{
    private readonly ILogger<WebEngine> _log;

    public WebEngine(EngineSettings settings, string baseDirectory, ILogger<WebEngine> log)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings;
        _log = log;
        Definition = DefinitionLoader.Load(Path.Combine(baseDirectory, "data", "document-types", "requete-introductive"));
        Knowledge = LegalKnowledgeLoader.Load(Path.Combine(baseDirectory, "data", "law"));
        Samples = SampleCase.LoadAll(Path.Combine(baseDirectory, "Samples"));
        Gate = new ModelGate(settings.MaxConcurrentCalls);
        GoogleModels = settings.GoogleModels;
        EvaluationsFolder = Path.Combine(Path.GetTempPath(), "dzlegal-web-evaluations");
    }

    public EngineSettings Settings { get; }

    public DocumentTypeDefinition Definition { get; }

    public LegalKnowledge Knowledge { get; }

    public IReadOnlyList<SampleCase> Samples { get; }

    public ModelGate Gate { get; }

    /// <summary>The Gemma models in use: the configured ones that Google still serves (see <see cref="ResolveModelsAsync"/>).</summary>
    public IReadOnlyList<string> GoogleModels { get; private set; }

    /// <summary>
    /// Asks Google which models this key may call and keeps the configured ones that exist, in order. Google retires
    /// models (gemma-3-4b-it answered 404 one day), so when none of them is left, any Gemma model the key can use takes
    /// over. If Google can't be asked, the configured list stays and the fallback chain skips what is gone.
    /// </summary>
    public async Task ResolveModelsAsync(HttpMessageHandler? handler, CancellationToken cancellationToken)
    {
        if (Settings.GoogleApiKey is not { } key)
        {
            return;
        }

        IReadOnlyList<string> available;
        try
        {
            available = await GoogleAiModelProvider.ListModelsAsync(new GoogleAiOptions { ApiKey = key }, handler, cancellationToken);
        }
        catch (Exception ex) when (ex is ModelProviderException or JsonException or OperationCanceledException)
        {
            LogListFailed(_log, ex.Message);
            return;
        }

        var kept = Settings.GoogleModels.Where(available.Contains).ToList();
        if (kept.Count == 0)
        {
            kept = available
                .Where(m => m.StartsWith("gemma-", StringComparison.Ordinal) && m.EndsWith("-it", StringComparison.Ordinal))
                .OrderDescending(StringComparer.Ordinal)
                .ToList();
        }

        if (kept.Count == 0)
        {
            LogNoGemma(_log, available);
            return;
        }

        GoogleModels = kept;
        LogModels(_log, kept);
    }

    private string EvaluationsFolder { get; }

    /// <summary>A short description for the health endpoint: which engines are configured (never the keys).</summary>
    public IReadOnlyList<string> EngineNames => CreateEngines().Select(e =>
    {
        var name = e.Name;
        (e as IDisposable)?.Dispose();
        return name;
    }).ToList();

    /// <summary>A fresh view model for one browser session, with the hosted engine first and template mode as the fallback.</summary>
    public MainWindowViewModel CreateSession(IUserFiles files, IExternalLinks links, string language)
    {
        var loc = new Loc();
        loc.SetLanguage(language);
        var quota = new SessionQuota(Settings.RunsPerSessionPerHour);
        var providers = new List<ProviderOption>();
        if (Settings.HasAnyEngine)
        {
            providers.Add(new ProviderOption(
                "hosted",
                l => l.Language == "en" ? $"{PrimaryModel} (demo server)" : $"{PrimaryModel} (خادم العرض)",
                () => quota.TryTake() ? new GatedModelProvider(CreateChain(), Gate) : new QuotaReachedModelProvider(PrimaryModel, Settings.RunsPerSessionPerHour)));
        }

        providers.Add(new ProviderOption("stub", l => l["ProviderStub"], () => new StubModelProvider()));
        return new MainWindowViewModel(
            Definition,
            providers,
            files,
            new EvaluationStore(EvaluationsFolder),
            loc,
            defaultProvider: providers[0].Key,
            knowledge: Knowledge,
            links: links);
    }

    private string PrimaryModel => Settings.GoogleApiKey is not null && GoogleModels.Count > 0
        ? Pretty(GoogleModels[0])
        : Settings.GroqApiKey is not null ? Settings.GroqModel : Settings.OpenRouterModel;

    /// <summary>"gemma-3-4b-it" → "Gemma 3 4B", "gemma-4-26b-a4b-it" → "Gemma 4 26B A4B".</summary>
    internal static string Pretty(string model)
    {
        if (!model.StartsWith("gemma-", StringComparison.Ordinal) || !model.EndsWith("-it", StringComparison.Ordinal))
        {
            return model;
        }

        var parts = model["gemma-".Length..^"-it".Length].Split('-');
        return parts.Length < 2 ? model : $"Gemma {parts[0]} " + string.Join(' ', parts[1..]).ToUpperInvariant();
    }

    private FallbackModelProvider CreateChain() => new(CreateEngines())
    {
        OnSkipped = (engine, ex) => LogSkipped(_log, engine.Name, ex.Message),
    };

    private List<IModelProvider> CreateEngines()
    {
        var engines = new List<IModelProvider>();
        if (Settings.GoogleApiKey is { } google)
        {
            engines.AddRange(GoogleModels.Select(m => new GoogleAiModelProvider(new GoogleAiOptions
            {
                ApiKey = google,
                Model = m,
                Temperature = Settings.Temperature,
                MaxTokens = Settings.MaxTokens,
            })));
        }

        if (Settings.GroqApiKey is { } groq)
        {
            engines.Add(new OpenAiCompatibleModelProvider(new OpenAiCompatibleOptions
            {
                Label = "Groq",
                Endpoint = Settings.GroqEndpoint,
                ApiKey = groq,
                Model = Settings.GroqModel,
                Temperature = Settings.Temperature,
                MaxTokens = Settings.MaxTokens,
            }));
        }

        if (Settings.OpenRouterApiKey is { } openRouter)
        {
            engines.Add(new OpenAiCompatibleModelProvider(new OpenAiCompatibleOptions
            {
                Label = "OpenRouter",
                Endpoint = new Uri("https://openrouter.ai/api/v1/"),
                ApiKey = openRouter,
                Model = Settings.OpenRouterModel,
                Temperature = Settings.Temperature,
                MaxTokens = Settings.MaxTokens,
                MergeSystemPrompt = Settings.OpenRouterModel.Contains("gemma", StringComparison.OrdinalIgnoreCase),
            }));
        }

        return engines;
    }

    public void Dispose() => Gate.Dispose();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Engine {Engine} skipped: {Reason}")]
    private static partial void LogSkipped(ILogger logger, string engine, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Gemma models in use: {Models}")]
    private static partial void LogModels(ILogger logger, IEnumerable<string> models);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not list the models of the Google key, keeping the configured ones: {Reason}")]
    private static partial void LogListFailed(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No Gemma model is available to the Google key. Models it can use: {Models}")]
    private static partial void LogNoGemma(ILogger logger, IEnumerable<string> models);
}

/// <summary>A fictitious case a visitor can open in one click: case details only, so every step runs live.</summary>
public sealed record SampleCase(string Id, LocalizedText Title, LocalizedText Summary, IReadOnlyDictionary<string, string> Inputs)
{
    public static IReadOnlyList<SampleCase> LoadAll(string folder) =>
        Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.json")
                .Order(StringComparer.Ordinal)
                .Select(f => JsonSerializer.Deserialize<SampleCase>(File.ReadAllText(f), DefinitionJson.Options)!)
                .ToList()
            : [];
}
