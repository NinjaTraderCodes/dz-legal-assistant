using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DzLegal.Drafting;

namespace DzLegal.ModelProviders.Hosted;

/// <summary>
/// Calls any API that speaks OpenAI's chat-completions protocol (<c>POST chat/completions</c>, streamed), e.g. Groq or
/// OpenRouter. Web demo only: the prompt, and the case notes in it, leave the server.
/// </summary>
public sealed class OpenAiCompatibleModelProvider : IModelProvider, IDisposable
{
    private readonly OpenAiCompatibleOptions _options;
    private readonly HttpClient _http;

    public OpenAiCompatibleModelProvider(OpenAiCompatibleOptions options, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ApiKey);
        _options = options;
        _http = new HttpClient(handler ?? new SocketsHttpHandler(), disposeHandler: true)
        {
            BaseAddress = options.Endpoint,
            Timeout = options.Timeout,
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    public string Name => $"{_options.Model} ({_options.Label})";

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        await foreach (var piece in StreamAsync(request, cancellationToken))
        {
            text.Append(piece);
        }

        return text.Length > 0 ? text.ToString() : throw new ModelProviderException($"{Name} returned an empty answer.");
    }

    public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var turns = request.Turns(_options.MergeSystemPrompt).Select(t => new Message(t.Role, t.Text));
        IReadOnlyList<Message> messages = _options.MergeSystemPrompt
            ? [.. turns]
            : [new Message("system", SystemPrompt(request)), .. turns];
        var maxTokens = request.MaxTokens is { } cap ? Math.Min(cap, _options.MaxTokens) : _options.MaxTokens;
        var body = new Request(_options.Model, messages, _options.Temperature, maxTokens, Stream: true)
        {
            Provider = _options.ProviderSort is { } sort ? new ProviderPreference(sort, AllowFallbacks: true) : null,
            Reasoning = _options.DisableReasoning ? new ReasoningOptions(Enabled: false) : null,
        };
        using var response = await HostedHttp.SendAsync(
            _http,
            () => new HttpRequestMessage(HttpMethod.Post, new Uri("chat/completions", UriKind.Relative)) { Content = HostedHttp.Json(body) },
            Name,
            _options.RateLimit,
            cancellationToken);
        var written = false;
        string? finish = null;
        string? provider = null;
        int? tokens = null;
        await foreach (var data in HostedHttp.ReadEventsAsync(response, Name, _options.IdleTimeout, cancellationToken))
        {
            var chunk = HostedHttp.Parse<Chunk>(data, Name);
            provider ??= chunk?.Provider;
            tokens = chunk?.Usage?.CompletionTokens ?? tokens;
            finish = (chunk?.Choices is [var end, ..] ? end.FinishReason : null) ?? finish;
            if (chunk?.Choices is [var first, ..] && first.Delta?.Content is { Length: > 0 } piece)
            {
                written |= !string.IsNullOrWhiteSpace(piece);
                yield return piece;
            }

            // A cut-off or filtered answer is not an answer: unfinished legal text must never pass as complete.
            switch (chunk?.Choices is [var last, ..] ? last.FinishReason : null)
            {
                case "length":
                    throw new ModelProviderException($"{Name} stopped at its answer length limit: the answer was cut off.") { IsCutOff = true };
                case "content_filter":
                    throw new ModelProviderException($"{Name} stopped its answer (content filter).");
            }
        }

        // Nothing written is not an answer either; say why the model stopped, so the cause can be found.
        if (!written)
        {
            throw new ModelProviderException(
                $"{Name} returned an empty answer (finish reason: {finish ?? "none"}, answer tokens: {tokens?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "not reported"}{(provider is null ? string.Empty : ", served by " + provider)}).");
        }
    }

    public void Dispose() => _http.Dispose();

    private sealed record Request(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<Message> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("stream")] bool Stream)
    {
        [JsonPropertyName("provider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ProviderPreference? Provider { get; init; }

        [JsonPropertyName("reasoning")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ReasoningOptions? Reasoning { get; init; }
    }

    /// <summary>
    /// Nemotron reads its reasoning switch from the system prompt («detailed thinking off»); the shared rules, if any,
    /// follow it. Other models get the rules unchanged.
    /// </summary>
    private string SystemPrompt(ModelRequest request) =>
        _options.DisableReasoning && _options.Model.Contains("nemotron", StringComparison.OrdinalIgnoreCase)
            ? request.SystemPrompt.Length == 0 ? "detailed thinking off" : "detailed thinking off\n\n" + request.SystemPrompt
            : request.SystemPrompt;

    /// <summary>OpenRouter's unified reasoning switch: https://openrouter.ai/docs/use-cases/reasoning-tokens.</summary>
    private sealed record ReasoningOptions([property: JsonPropertyName("enabled")] bool Enabled);

    /// <summary>OpenRouter's provider routing: https://openrouter.ai/docs/features/provider-routing.</summary>
    private sealed record ProviderPreference(
        [property: JsonPropertyName("sort")] string Sort,
        [property: JsonPropertyName("allow_fallbacks")] bool AllowFallbacks);

    private sealed record Message(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record Chunk(
        [property: JsonPropertyName("choices")] IReadOnlyList<Choice>? Choices,
        [property: JsonPropertyName("usage")] Usage? Usage = null,
        [property: JsonPropertyName("provider")] string? Provider = null);

    private sealed record Usage([property: JsonPropertyName("completion_tokens")] int? CompletionTokens);

    private sealed record Choice(
        [property: JsonPropertyName("delta")] Delta? Delta,
        [property: JsonPropertyName("finish_reason")] string? FinishReason = null);

    private sealed record Delta([property: JsonPropertyName("content")] string? Content);
}
