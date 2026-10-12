namespace DzLegal.ModelProviders.Ollama;

public sealed record OllamaOptions
{
    /// <summary>Ollama's default local address. Must stay a loopback address.</summary>
    public Uri Endpoint { get; init; } = new("http://127.0.0.1:11434");

    public string Model { get; init; } = "gemma3:4b";

    /// <summary>Low temperature: we want faithful rephrasing, not creativity.</summary>
    public double Temperature { get; init; } = 0.2;

    /// <summary>
    /// Context window (prompt + answer, tokens). Fixed for every call, the warm-up included: Ollama reloads the model when
    /// it changes, which would cost the whole loading time again.
    /// </summary>
    public int ContextLength { get; init; } = 4096;

    /// <summary>
    /// How long Ollama keeps the model in memory after each request (Ollama duration, e.g. "60m"; "-1" = until it stops).
    /// Ollama's own default is 5 minutes, after which the next request pays the loading time again.
    /// </summary>
    public string KeepAlive { get; init; } = "60m";

    /// <summary>Upper bound on generated tokens per section, so a small model cannot ramble or loop.</summary>
    public int MaxTokens { get; init; } = 1500;

    public double RepeatPenalty { get; init; } = 1.1;

    /// <summary>
    /// Ollama's "think" switch for reasoning models (e.g. qwen3): false skips the slow reasoning phase.
    /// Null leaves it unset, which non-reasoning models require.
    /// </summary>
    public bool? Think { get; init; }

    /// <summary>CPU inference of a 7B model can take minutes per section on modest hardware.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);
}
