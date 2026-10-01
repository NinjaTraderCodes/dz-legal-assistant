namespace DzLegal.ModelProviders.Ollama;

public sealed record OllamaOptions
{
    /// <summary>Ollama's default local address. Must stay a loopback address.</summary>
    public Uri Endpoint { get; init; } = new("http://127.0.0.1:11434");

    public string Model { get; init; } = "qwen2.5:7b-instruct";

    /// <summary>Low temperature: we want faithful rephrasing, not creativity.</summary>
    public double Temperature { get; init; } = 0.2;

    public int ContextLength { get; init; } = 4096;

    /// <summary>Upper bound on generated tokens per section, so a small model cannot ramble or loop.</summary>
    public int MaxTokens { get; init; } = 1000;

    public double RepeatPenalty { get; init; } = 1.1;

    /// <summary>
    /// Ollama's "think" switch for reasoning models (e.g. qwen3): false skips the slow reasoning phase.
    /// Null leaves it unset, which non-reasoning models require.
    /// </summary>
    public bool? Think { get; init; }

    /// <summary>CPU inference of a 7B model can take minutes per section on modest hardware.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);
}
