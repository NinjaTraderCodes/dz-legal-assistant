namespace DzLegal.ModelProviders.Hosted;

/// <summary>Google AI Studio (Gemini API), which also serves the open Gemma models the desktop app runs locally.</summary>
public sealed record GoogleAiOptions
{
    /// <summary>Secret: read from the host's environment, never from a file in the repository.</summary>
    public required string ApiKey { get; init; }

    /// <summary>Gemma 3 4B: the same open model the desktop app runs offline through Ollama.</summary>
    public string Model { get; init; } = "gemma-3-4b-it";

    public Uri Endpoint { get; init; } = new("https://generativelanguage.googleapis.com/v1beta/");

    public double Temperature { get; init; } = 0.2;

    public int MaxTokens { get; init; } = 1000;

    /// <summary>
    /// Gemma models on the Gemini API refuse a separate system instruction, so the shared drafting rules are sent at the
    /// top of the user turn instead. Gemini models accept it and can set this to false.
    /// </summary>
    public bool MergeSystemPrompt { get; init; } = true;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>How long an answer may go silent mid-stream before the engine counts as stalled.</summary>
    public TimeSpan IdleTimeout { get; init; } = HostedHttp.DefaultIdleTimeout;

    /// <summary>
    /// <c>thinkingConfig.thinkingLevel</c>. Null means automatic: "minimal" for Gemma 4, which otherwise thinks first by
    /// default and spends the short answer caps of the grounds analysis on it (the answer then comes back empty); not
    /// sent for other models.
    /// </summary>
    public string? ThinkingLevel { get; init; }

    /// <inheritdoc cref="RateLimitPolicy"/>
    public RateLimitPolicy RateLimit { get; init; } = RateLimitPolicy.None;

    internal string? EffectiveThinkingLevel =>
        ThinkingLevel ?? (Model.StartsWith("gemma-4", StringComparison.Ordinal) ? "minimal" : null);
}

/// <summary>Any API that speaks OpenAI's chat-completions protocol (Groq, OpenRouter, Together…).</summary>
public sealed record OpenAiCompatibleOptions
{
    /// <summary>Shown in the UI, e.g. "groq" or "openrouter".</summary>
    public required string Label { get; init; }

    /// <summary>Base address ending with a slash, e.g. https://api.groq.com/openai/v1/.</summary>
    public required Uri Endpoint { get; init; }

    /// <summary>Secret: read from the host's environment, never from a file in the repository.</summary>
    public required string ApiKey { get; init; }

    public required string Model { get; init; }

    public double Temperature { get; init; } = 0.2;

    public int MaxTokens { get; init; } = 1000;

    public bool MergeSystemPrompt { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>How long an answer may go silent mid-stream before the engine counts as stalled.</summary>
    public TimeSpan IdleTimeout { get; init; } = HostedHttp.DefaultIdleTimeout;

    /// <inheritdoc cref="RateLimitPolicy"/>
    public RateLimitPolicy RateLimit { get; init; } = RateLimitPolicy.None;

    /// <summary>
    /// OpenRouter only: how to choose among the providers that serve the same model ("throughput", "price",
    /// "latency"), with fallbacks to the others allowed. Null sends no preference (OpenRouter's default routing).
    /// </summary>
    public string? ProviderSort { get; init; }
}

/// <summary>
/// How long to keep waiting when the service says "too many requests" (429) or "busy" (503), in total, before giving up.
/// None (the default) gives up at once, which suits the web demo: its fallback chain moves to the next engine instead.
/// The desktop testing build and the harness wait, because they test one engine on purpose.
/// </summary>
/// <param name="MaxWait">Total waiting allowed for one request.</param>
/// <param name="OnWait">Told before each wait: how long, which attempt comes next, and what the service said (e.g.
/// "google/gemma-3-4b-it is temporarily rate-limited upstream"), so a log shows whose limit it is.</param>
public sealed record RateLimitPolicy(TimeSpan MaxWait, Action<TimeSpan, int, string>? OnWait = null)
{
    public static readonly RateLimitPolicy None = new(TimeSpan.Zero);
}
