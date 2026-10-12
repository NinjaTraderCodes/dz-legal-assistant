using DzLegal.App.Services;
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
            KeepAlive = settings.Ollama.KeepAlive,
        };

        List<ProviderOption> options =
        [
            new ProviderOption(
                "ollama",
                loc => loc.Format("ProviderOllama", ollama.Model),
                () => new OllamaModelProvider(ollama),
                ct => ProbeOllamaAsync(ollama, ct),
                settings.Ollama.WarmUp ? ct => WarmUpOllamaAsync(ollama, ct) : null),
            new ProviderOption("stub", loc => loc["ProviderStub"], () => new StubModelProvider()),
        ];
#if ONLINE_TESTING
        if (OnlineTestingOption(settings) is { } online)
        {
            options.Insert(1, online);
        }
#endif
        return options;
    }

#if ONLINE_TESTING
    /// <summary>
    /// TEMPORARY, testing build only (docs/ONLINE-TESTING.md): the web demo's engine, Gemma 3 4B on OpenRouter, so the
    /// team can measure the AI over many runs. Only when OPENROUTER_API_KEY is set; one engine, no fallback, with the
    /// desktop's own temperature and length so results compare with the local model. The case notes leave the computer.
    /// </summary>
    private static ProviderOption? OnlineTestingOption(AppSettings settings)
    {
        var key = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var model = Environment.GetEnvironmentVariable("OPENROUTER_MODEL") is { Length: > 0 } m ? m : "google/gemma-3-4b-it";
        var endpoint = Environment.GetEnvironmentVariable("OPENROUTER_ENDPOINT") is { Length: > 0 } e ? new Uri(e) : new Uri("https://openrouter.ai/api/v1/");
        var options = new DzLegal.ModelProviders.Hosted.OpenAiCompatibleOptions
        {
            Label = "OpenRouter",
            Endpoint = endpoint,
            ApiKey = key.Trim(),
            Model = model,
            Temperature = settings.Ollama.Temperature,
            MaxTokens = settings.Ollama.MaxTokens,
            MergeSystemPrompt = model.Contains("gemma", StringComparison.OrdinalIgnoreCase),
            // OpenRouter's shared capacity is sometimes rate-limited for a minute or two: wait it out before reporting.
            RateLimit = new DzLegal.ModelProviders.Hosted.RateLimitPolicy(TimeSpan.FromSeconds(90)),
        };
        return new ProviderOption(
            "online",
            loc => loc.Format("ProviderOnlineTest", model),
            () => new DzLegal.ModelProviders.Hosted.OpenAiCompatibleModelProvider(options),
            sendsCaseData: true);
    }

    private const string BuildLabel = "online testing build";
#else
    private const string BuildLabel = "";
#endif

    /// <summary>Loopback-only check: is Ollama running, and is the configured model installed?</summary>
    public static async Task<ModelStatus> ProbeOllamaAsync(OllamaOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (DzLegal.ModelProviders.Ollama.LoopbackGuard.IsCloudModel(options.Model))
        {
            return new ModelStatus(ModelState.Refused, options.Model);
        }

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

    /// <summary>Loads the model into the local Ollama's memory (loopback only), so the first request does not wait for it.</summary>
    public static async Task WarmUpOllamaAsync(OllamaOptions options, CancellationToken cancellationToken)
    {
        using var provider = new OllamaModelProvider(options);
        await provider.WarmUpAsync(cancellationToken);
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
            links: links)
        {
            BuildLabel = BuildLabel,
        };
    }
}
