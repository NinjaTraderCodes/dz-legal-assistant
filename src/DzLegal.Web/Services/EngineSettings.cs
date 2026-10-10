namespace DzLegal.Web.Services;

/// <summary>
/// Which hosted engines the demo may use, in order. Model names live in appsettings.json; the API keys come only from
/// environment variables (the host's secret settings), never from a file in the repository.
/// </summary>
public sealed class EngineSettings
{
    /// <summary>
    /// Gemma models on Google AI Studio, tried in order. Each has its own rate limit, so the next one absorbs a burst.
    /// At startup the demo keeps only those Google still serves (<see cref="WebEngine.ResolveModelsAsync"/>).
    /// </summary>
    public IReadOnlyList<string> GoogleModels { get; init; } = DefaultGoogleModels;

    private static readonly string[] DefaultGoogleModels =
    [
        // Gemma 4: a mixture of experts with about 4B parameters active per word (fast), then the dense 31B.
        "gemma-4-26b-a4b-it", "gemma-4-31b-it",
        // Gemma 3, the desktop default's family, for keys that still have it.
        "gemma-3-27b-it", "gemma-3-12b-it", "gemma-3-4b-it",
    ];

    /// <summary>Optional last resort on Groq (OpenAI-compatible), used only when its key is set.</summary>
    public string GroqModel { get; init; } = "llama-3.3-70b-versatile";

    public Uri GroqEndpoint { get; init; } = new("https://api.groq.com/openai/v1/");

    /// <summary>
    /// Gemma 3 4B on OpenRouter (OpenAI-compatible, paid per token, a few cents for a whole demo): the exact model the
    /// desktop app runs offline. Used only when its key is set.
    /// </summary>
    public string OpenRouterModel { get; init; } = "google/gemma-3-4b-it";

    /// <summary>Which services are tried first: "openrouter", "google", "groq". Services without a key are skipped.</summary>
    public IReadOnlyList<string> Order { get; init; } = DefaultOrder;

    private static readonly string[] DefaultOrder = ["openrouter", "google", "groq"];

    /// <summary>
    /// How many model calls run at once across all visitors; the others wait their turn. Keeps a burst of judges within
    /// the free tier's per-minute limits.
    /// </summary>
    public int MaxConcurrentCalls { get; init; } = 2;

    /// <summary>
    /// Model calls one browser session may make per hour. A full run of the demo (analysis, then drafting) takes about
    /// six calls, so 60 is ten complete tries.
    /// </summary>
    public int CallsPerSessionPerHour { get; init; } = 60;

    /// <summary>Model calls per hour for all sessions from one network address together, so reloading the page does not reset the limit.</summary>
    public int CallsPerAddressPerHour { get; init; } = 150;

    public double Temperature { get; init; } = 0.2;

    public int MaxTokens { get; init; } = 1000;

    public string? GoogleApiKey { get; init; }

    public string? GroqApiKey { get; init; }

    public string? OpenRouterApiKey { get; init; }

    /// <summary>Reads the "Engines" section, then the keys from GOOGLE_API_KEY, GROQ_API_KEY and OPENROUTER_API_KEY.</summary>
    public static EngineSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var settings = configuration.GetSection("Engines").Get<EngineSettings>() ?? new EngineSettings();
        return new EngineSettings
        {
            // Read as a list on its own: binding would append the configured models to the defaults instead of replacing them.
            GoogleModels = configuration.GetSection("Engines:GoogleModels").Get<string[]>() is { Length: > 0 } models ? models : DefaultGoogleModels,
            Order = configuration.GetSection("Engines:Order").Get<string[]>() is { Length: > 0 } order ? order : DefaultOrder,
            GroqModel = settings.GroqModel,
            GroqEndpoint = settings.GroqEndpoint,
            OpenRouterModel = settings.OpenRouterModel,
            MaxConcurrentCalls = Math.Max(1, settings.MaxConcurrentCalls),
            CallsPerSessionPerHour = Math.Max(1, settings.CallsPerSessionPerHour),
            CallsPerAddressPerHour = Math.Max(1, settings.CallsPerAddressPerHour),
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            GoogleApiKey = NonEmpty(configuration["GOOGLE_API_KEY"]),
            GroqApiKey = NonEmpty(configuration["GROQ_API_KEY"]),
            OpenRouterApiKey = NonEmpty(configuration["OPENROUTER_API_KEY"]),
        };
    }

    public bool HasAnyEngine => GoogleApiKey is not null || GroqApiKey is not null || OpenRouterApiKey is not null;

    private static string? NonEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
