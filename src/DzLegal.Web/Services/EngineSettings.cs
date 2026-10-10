namespace DzLegal.Web.Services;

/// <summary>
/// Which hosted engines the demo may use, in order. Model names live in appsettings.json; the API keys come only from
/// environment variables (the host's secret settings), never from a file in the repository.
/// </summary>
public sealed class EngineSettings
{
    /// <summary>Gemma models on Google AI Studio, tried in order. Each has its own rate limit, so the next one absorbs a burst.</summary>
    public IReadOnlyList<string> GoogleModels { get; init; } = DefaultGoogleModels;

    private static readonly string[] DefaultGoogleModels = ["gemma-3-4b-it", "gemma-3-12b-it", "gemma-3-27b-it"];

    /// <summary>Optional last resort on Groq (OpenAI-compatible), used only when its key is set.</summary>
    public string GroqModel { get; init; } = "llama-3.3-70b-versatile";

    public Uri GroqEndpoint { get; init; } = new("https://api.groq.com/openai/v1/");

    /// <summary>Optional last resort on OpenRouter (OpenAI-compatible), used only when its key is set.</summary>
    public string OpenRouterModel { get; init; } = "google/gemma-3-27b-it";

    /// <summary>
    /// How many model calls run at once across all visitors; the others wait their turn. Keeps a burst of judges within
    /// the free tier's per-minute limits.
    /// </summary>
    public int MaxConcurrentCalls { get; init; } = 2;

    /// <summary>AI runs (analysis, drafting, assistant) one browser session may start per hour, against abuse of a public link.</summary>
    public int RunsPerSessionPerHour { get; init; } = 30;

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
            GroqModel = settings.GroqModel,
            GroqEndpoint = settings.GroqEndpoint,
            OpenRouterModel = settings.OpenRouterModel,
            MaxConcurrentCalls = Math.Max(1, settings.MaxConcurrentCalls),
            RunsPerSessionPerHour = Math.Max(1, settings.RunsPerSessionPerHour),
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
