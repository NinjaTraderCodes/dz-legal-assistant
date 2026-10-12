using DzLegal.Drafting;
using DzLegal.ModelProviders.Hosted;
using DzLegal.ModelProviders.Ollama;
using DzLegal.ModelProviders.Stub;

namespace DzLegal.Harness;

/// <summary>
/// The engines the harness can measure, from a <c>--models</c> entry:
/// <list type="bullet">
/// <item><c>stub</c>: template mode, no AI.</item>
/// <item><c>openrouter</c> or <c>openrouter:&lt;model&gt;</c>: OpenRouter (default google/gemma-3-4b-it, the web demo's
/// engine), key from OPENROUTER_API_KEY. OPENROUTER_ENDPOINT overrides the address (a local mock in tests).</item>
/// <item><c>google:&lt;model&gt;</c>: Google AI Studio, key from GOOGLE_API_KEY.</item>
/// <item>anything else: a local Ollama model (gemma3:4b…).</item>
/// </list>
/// Hosted engines send the fictitious test cases to that service: never run them on real client files.
/// </summary>
public static class HarnessModels
{
    public const string DefaultOpenRouterModel = "google/gemma-3-4b-it";

    /// <summary>
    /// Gemma 4 E4B: free on Google AI Studio (no card) and runnable on a lawyer's laptop as <c>gemma4:e4b</c>. Google no
    /// longer serves Gemma 3 to new keys, and OpenRouter's Gemma 3 4B has a single, often saturated, provider.
    /// </summary>
    public const string DefaultGoogleModel = "gemma-4-e4b-it";

    /// <summary>
    /// DeepInfra, called directly with the user's own key: the company that actually serves Gemma 3 4B behind OpenRouter,
    /// without OpenRouter's shared, often saturated, pool.
    /// </summary>
    public const string DefaultDeepInfraModel = "google/gemma-3-4b-it";

    /// <summary>DeepInfra's OpenAI-compatible API, or DEEPINFRA_ENDPOINT (a local mock in tests).</summary>
    public static Uri DeepInfraEndpoint =>
        Environment.GetEnvironmentVariable("DEEPINFRA_ENDPOINT") is { Length: > 0 } e ? new Uri(e) : new Uri("https://api.deepinfra.com/v1/openai/");

    /// <summary>The DeepInfra model of a <c>deepinfra</c> or <c>deepinfra:&lt;model&gt;</c> spec.</summary>
    public static string DeepInfraModel(string spec) =>
        spec.Length > "deepinfra:".Length && spec.StartsWith("deepinfra:", StringComparison.Ordinal) ? spec["deepinfra:".Length..] : DefaultDeepInfraModel;

    /// <param name="spec">One <c>--models</c> entry.</param>
    /// <param name="ollamaEndpoint">Where the local Ollama listens.</param>
    /// <param name="timeout">Longest wait for one answer.</param>
    /// <param name="speed">Receives Ollama's timings, when the engine is local.</param>
    /// <param name="rateLimit">For hosted engines: how long to wait out "too many requests" before a call fails.</param>
    /// <exception cref="ArgumentException">A hosted engine without its key.</exception>
    public static IModelProvider Create(string spec, Uri ollamaEndpoint, TimeSpan timeout, SpeedLog? speed = null, RateLimitPolicy? rateLimit = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spec);
        if (spec == "stub")
        {
            return new StubModelProvider();
        }

        if (spec == "openrouter" || spec.StartsWith("openrouter:", StringComparison.Ordinal))
        {
            var model = spec.Length > "openrouter:".Length ? spec["openrouter:".Length..] : DefaultOpenRouterModel;
            return new OpenAiCompatibleModelProvider(new OpenAiCompatibleOptions
            {
                Label = "OpenRouter",
                Endpoint = Environment.GetEnvironmentVariable("OPENROUTER_ENDPOINT") is { Length: > 0 } e ? new Uri(e) : new Uri("https://openrouter.ai/api/v1/"),
                ApiKey = Key("OPENROUTER_API_KEY", spec),
                Model = model,
                Timeout = timeout,
                MergeSystemPrompt = model.Contains("gemma", StringComparison.OrdinalIgnoreCase) || model == "mock",
                RateLimit = rateLimit ?? RateLimitPolicy.None,
                // Prefer whichever provider of this same model has capacity, instead of queuing on a busy one.
                ProviderSort = "throughput",
            });
        }

        if (spec == "deepinfra" || spec.StartsWith("deepinfra:", StringComparison.Ordinal))
        {
            var model = DeepInfraModel(spec);
            return new OpenAiCompatibleModelProvider(new OpenAiCompatibleOptions
            {
                Label = "DeepInfra",
                Endpoint = DeepInfraEndpoint,
                ApiKey = Key("DEEPINFRA_API_KEY", spec),
                Model = model,
                Timeout = timeout,
                MergeSystemPrompt = model.Contains("gemma", StringComparison.OrdinalIgnoreCase) || model == "mock",
                RateLimit = rateLimit ?? RateLimitPolicy.None,
            });
        }

        if (spec == "google" || spec.StartsWith("google:", StringComparison.Ordinal))
        {
            return new GoogleAiModelProvider(new GoogleAiOptions
            {
                ApiKey = Key("GOOGLE_API_KEY", spec),
                Model = GoogleModel(spec),
                Endpoint = GoogleEndpoint,
                Timeout = timeout,
                RateLimit = rateLimit ?? RateLimitPolicy.None,
            });
        }

        return new OllamaModelProvider(new OllamaOptions
        {
            Endpoint = ollamaEndpoint,
            Model = spec,
            Timeout = timeout,
            // Reasoning models answer far faster with thinking disabled; others reject the flag.
            Think = spec.StartsWith("qwen3", StringComparison.OrdinalIgnoreCase) ? false : null,
        })
        { OnTimings = speed is null ? null : speed.Add };
    }

    /// <summary>
    /// Waits out a hosted service's "too many requests" for up to <paramref name="maxWait"/> per call, saying so in the
    /// log, so a busy spell pauses the batch instead of failing its cases.
    /// </summary>
    public static RateLimitPolicy Waiting(TimeSpan maxWait, TextWriter log) =>
        new(maxWait, (delay, attempt, reason) => log.WriteLine($"    rate-limited ({reason}), waiting {delay.TotalSeconds:0} s (attempt {attempt})…"));

    /// <summary>The pause before each call: as given, or by default 3 s for a hosted engine (shared capacity) and 0 locally.</summary>
    public static TimeSpan Pace(string spec, double seconds) =>
        TimeSpan.FromSeconds(seconds >= 0 ? seconds : IsHosted(spec) ? 3 : 0);

    /// <summary>True for an engine on someone else's server (the cases leave the machine).</summary>
    public static bool IsHosted(string spec) =>
        spec == "openrouter" || spec.StartsWith("openrouter:", StringComparison.Ordinal) || spec == "google" || spec.StartsWith("google:", StringComparison.Ordinal)
        || spec == "deepinfra" || spec.StartsWith("deepinfra:", StringComparison.Ordinal);

    /// <summary>The Google model of a <c>google</c> or <c>google:&lt;model&gt;</c> spec.</summary>
    public static string GoogleModel(string spec) =>
        spec.Length > "google:".Length && spec.StartsWith("google:", StringComparison.Ordinal) ? spec["google:".Length..] : DefaultGoogleModel;

    /// <summary>Google AI Studio, or GOOGLE_ENDPOINT (a local mock in tests).</summary>
    public static Uri GoogleEndpoint =>
        Environment.GetEnvironmentVariable("GOOGLE_ENDPOINT") is { Length: > 0 } e ? new Uri(e) : new Uri("https://generativelanguage.googleapis.com/v1beta/");

    private static string Key(string variable, string spec) =>
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 } key
            ? key.Trim()
            : throw new ArgumentException($"{spec} needs the {variable} environment variable.");
}

/// <summary>Waits before each model call, to stay under a hosted service's rate limit during long batch runs.</summary>
public sealed class PacedModelProvider(IModelProvider inner, TimeSpan delay) : IModelProvider, IDisposable
{
    public IModelProvider Inner { get; } = inner;

    public string Name => Inner.Name;

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return await Inner.GenerateAsync(request, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        await foreach (var piece in Inner.StreamAsync(request, cancellationToken))
        {
            yield return piece;
        }
    }

    public void Dispose() => (Inner as IDisposable)?.Dispose();
}
