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
}
