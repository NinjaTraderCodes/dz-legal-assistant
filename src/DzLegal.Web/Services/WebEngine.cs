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
        _engines = CreateEngines();
    }

    // One set of engines (and their HTTP connections) for every visitor, rebuilt only when the model list changes.
    private volatile IReadOnlyList<IModelProvider> _engines;

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CallQuota> _addressQuotas = new(StringComparer.Ordinal);

    public EngineSettings Settings { get; }

    public DocumentTypeDefinition Definition { get; }

    public LegalKnowledge Knowledge { get; }

    public IReadOnlyList<SampleCase> Samples { get; }

    public ModelGate Gate { get; }

    /// <summary>
    /// How long the analysis and a full draft took on this server, shared by every visitor (in memory only), so the
    /// progress panel can say how much is left, as the desktop does from the lawyer's own earlier runs.
    /// </summary>
    public TaskTimings Timings { get; } = new();

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
        _engines = CreateEngines();
        LogModels(_log, kept);
    }

    /// <summary>Which engines are configured, in order (names only, never keys); for the server log.</summary>
    public IReadOnlyList<string> EngineNames => _engines.Select(e => e.Name).ToList();

    /// <summary>
    /// A fresh view model for one browser session, with the hosted engine first and "without AI" as the alternative.
    /// <paramref name="address"/> is the visitor's network address, for the per-address limit.
    /// </summary>
    public MainWindowViewModel CreateSession(IUserFiles files, IExternalLinks links, string language, string? address = null)
    {
        var loc = new Loc();
        loc.SetLanguage(language);
        var providers = new List<ProviderOption>();
        if (Settings.HasAnyEngine)
        {
            var reached = "This demo allows {0} AI calls per hour {1}; the limit is reached. The formal check, editing and export keep working, and AI is available again within the hour.";
            var quotas = new List<CallQuota>
            {
                new(Settings.CallsPerSessionPerHour, string.Format(System.Globalization.CultureInfo.InvariantCulture, reached, Settings.CallsPerSessionPerHour, "per visitor")),
            };
            if (!string.IsNullOrEmpty(address))
            {
                quotas.Add(AddressQuota(address));
            }

            providers.Add(new ProviderOption(
                "hosted",
                l => l.Language == "en" ? $"{PrimaryModel} (demo server)" : $"{PrimaryModel} (خادم العرض)",
                () => new ProtectedModelProvider(CreateChain(), Gate, quotas, reason => LogFailed(_log, reason)),
                sendsCaseData: true));
        }

        providers.Add(new ProviderOption("stub", l => l["ProviderStub"], () => new StubModelProvider()));
        return new MainWindowViewModel(
            Definition,
            providers,
            files,
            // Never written to: the web page has no evaluation form, and the store creates its folder only on save.
            new EvaluationStore(Path.Combine(Path.GetTempPath(), "dzlegal-web-unused")),
            loc,
            defaultProvider: providers[0].Key,
            knowledge: Knowledge,
            links: links)
        {
            Timings = Timings,
        };
    }

    private CallQuota AddressQuota(string address)
    {
        if (_addressQuotas.Count > 10_000)
        {
            foreach (var (key, quota) in _addressQuotas)
            {
                if (quota.IsIdle)
                {
                    _addressQuotas.TryRemove(key, out _);
                }
            }
        }

        return _addressQuotas.GetOrAdd(address, _ => new CallQuota(
            Settings.CallsPerAddressPerHour,
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "This demo allows {0} AI calls per hour from one network; the limit is reached. The formal check, editing and export keep working, and AI is available again within the hour.",
                Settings.CallsPerAddressPerHour)));
    }

    private string PrimaryModel => EngineModels().Select(Pretty).FirstOrDefault() ?? string.Empty;

    /// <summary>The model of every engine, in the order they are tried.</summary>
    private IEnumerable<string> EngineModels() => Families().SelectMany(f => f.Models);

    /// <summary>"gemma-3-4b-it" → "Gemma 3 4B", "gemma-4-26b-a4b-it" → "Gemma 4 26B A4B", "google/gemma-3-4b-it:free" → "Gemma 3 4B".</summary>
    internal static string Pretty(string model)
    {
        var bare = model.StartsWith("google/", StringComparison.Ordinal) ? model["google/".Length..] : model;
        bare = bare.Split(':')[0];
        if (bare != model)
        {
            var pretty = Pretty(bare);
            return pretty == bare ? model : pretty;
        }

        if (!model.StartsWith("gemma-", StringComparison.Ordinal) || !model.EndsWith("-it", StringComparison.Ordinal))
        {
            return model;
        }

        var parts = model["gemma-".Length..^"-it".Length].Split('-');
        return parts.Length < 2 ? model : $"Gemma {parts[0]} " + string.Join(' ', parts[1..]).ToUpperInvariant();
    }

    private FallbackModelProvider CreateChain() => new(_engines, ownsEngines: false)
    {
        OnSkipped = (engine, ex) => LogSkipped(_log, engine.Name, ex.Message),
    };

    /// <summary>The services with a key, in the configured order, each with its models.</summary>
    private IEnumerable<(string Service, IReadOnlyList<string> Models)> Families()
    {
        foreach (var service in Settings.Order.Select(o => o.Trim().ToLowerInvariant()).Distinct())
        {
            switch (service)
            {
                case "openrouter" when Settings.OpenRouterApiKey is not null:
                    yield return (service, [Settings.OpenRouterModel]);
                    break;
                case "google" when Settings.GoogleApiKey is not null && GoogleModels.Count > 0:
                    yield return (service, GoogleModels);
                    break;
                case "groq" when Settings.GroqApiKey is not null:
                    yield return (service, [Settings.GroqModel]);
                    break;
            }
        }
    }

    private List<IModelProvider> CreateEngines()
    {
        var engines = new List<IModelProvider>();
        foreach (var (service, models) in Families())
        {
            engines.AddRange(models.Select(model => Create(service, model)));
        }

        return engines;
    }

    private IModelProvider Create(string service, string model) => service switch
    {
        "google" => new GoogleAiModelProvider(new GoogleAiOptions
        {
            ApiKey = Settings.GoogleApiKey!,
            Model = model,
            Temperature = Settings.Temperature,
            MaxTokens = Settings.MaxTokens,
        }),
        "groq" => new OpenAiCompatibleModelProvider(new OpenAiCompatibleOptions
        {
            Label = "Groq",
            Endpoint = Settings.GroqEndpoint,
            ApiKey = Settings.GroqApiKey!,
            Model = model,
            Temperature = Settings.Temperature,
            MaxTokens = Settings.MaxTokens,
        }),
        _ => new OpenAiCompatibleModelProvider(new OpenAiCompatibleOptions
        {
            Label = "OpenRouter",
            Endpoint = new Uri("https://openrouter.ai/api/v1/"),
            ApiKey = Settings.OpenRouterApiKey!,
            Model = model,
            Temperature = Settings.Temperature,
            MaxTokens = Settings.MaxTokens,
            MergeSystemPrompt = model.Contains("gemma", StringComparison.OrdinalIgnoreCase),
        }),
    };

    public void Dispose()
    {
        Gate.Dispose();
        foreach (var engine in _engines)
        {
            (engine as IDisposable)?.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI call failed: {Reason}")]
    private static partial void LogFailed(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Engine {Engine} skipped ({Reason}); the next engine in the chain answers instead")]
    private static partial void LogSkipped(ILogger logger, string engine, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Gemma models in use: {Models}")]
    private static partial void LogModels(ILogger logger, IEnumerable<string> models);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not list the models of the Google key, keeping the configured ones: {Reason}")]
    private static partial void LogListFailed(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No Gemma model is available to the Google key. Models it can use: {Models}")]
    private static partial void LogNoGemma(ILogger logger, IEnumerable<string> models);
}
