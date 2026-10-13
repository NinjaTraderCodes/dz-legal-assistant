using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DzLegal.Drafting;

namespace DzLegal.ModelProviders.Hosted;

/// <summary>
/// Calls the Gemini API (<c>models/{model}:streamGenerateContent</c>), which serves the open Gemma models. Web demo only:
/// the prompt, and the case notes in it, leave the server.
/// </summary>
public sealed class GoogleAiModelProvider : IModelProvider, IDisposable
{
    private readonly GoogleAiOptions _options;
    private readonly HttpClient _http;

    public GoogleAiModelProvider(GoogleAiOptions options, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ApiKey);
        _options = options;
        _http = new HttpClient(handler ?? new SocketsHttpHandler(), disposeHandler: true)
        {
            BaseAddress = options.Endpoint,
            Timeout = options.Timeout,
        };

        // In a header, not in the URL, so the key never shows up in a log of request addresses.
        _http.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
    }

    public string Name => $"{_options.Model} (Google AI Studio)";

    public string Model => _options.Model;

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
        using var response = await SendAsync(request, cancellationToken);
        var answered = false;
        string? finish = null;
        await foreach (var data in HostedHttp.ReadEventsAsync(response, Name, _options.IdleTimeout, cancellationToken))
        {
            var chunk = HostedHttp.Parse<Chunk>(data, Name);
            if (chunk?.PromptFeedback?.BlockReason is { } blocked)
            {
                throw new ModelProviderException($"{Name} refused the request ({blocked}).");
            }

            var candidate = chunk?.Candidates is [var first, ..] ? first : null;
            finish = candidate?.FinishReason ?? finish;
            foreach (var part in candidate?.Content?.Parts ?? [])
            {
                // Models that think aloud mark those parts; only the answer goes to the document.
                if (part.Thought != true && part.Text is { Length: > 0 } piece)
                {
                    answered |= !string.IsNullOrWhiteSpace(piece);
                    yield return piece;
                }
            }
        }

        // A cut-off or blocked answer is not an answer: plausible but unfinished legal text must never pass as complete.
        if (finish is "MAX_TOKENS")
        {
            throw new ModelProviderException(answered
                ? $"{Name} stopped at its answer length limit: the answer was cut off."
                : $"{Name} reached its answer length limit before writing anything (it was thinking).") { IsCutOff = true };
        }

        if (finish is "SAFETY" or "RECITATION" or "BLOCKLIST" or "PROHIBITED_CONTENT")
        {
            throw new ModelProviderException($"{Name} stopped its answer ({finish}).");
        }

        if (!answered)
        {
            throw new ModelProviderException($"{Name} returned an empty answer (finish reason: {finish ?? "none"}).");
        }
    }

    /// <summary>
    /// Sends the request with the thinking level; if the model refuses that setting (400 "Thinking level is not
    /// supported"), sends it once more without it.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var thinking = _options.EffectiveThinkingLevel;
        if (thinking is not null)
        {
            try
            {
                return await SendOnceAsync(request, thinking, cancellationToken);
            }
            catch (ModelProviderException ex) when (ex.Message.Contains(" error 400:", StringComparison.Ordinal)
                && ex.Message.Contains("hinking", StringComparison.Ordinal))
            {
                // Falls through to a request without thinkingConfig.
            }
        }

        return await SendOnceAsync(request, null, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(ModelRequest request, string? thinking, CancellationToken cancellationToken)
    {
        var body = Body(request, thinking);
        return await HostedHttp.SendAsync(
            _http,
            () => new HttpRequestMessage(HttpMethod.Post, new Uri($"models/{_options.Model}:streamGenerateContent?alt=sse", UriKind.Relative)) { Content = HostedHttp.Json(body) },
            Name,
            _options.RateLimit,
            cancellationToken);
    }

    /// <summary>
    /// The models this key may call with <c>generateContent</c> (<c>GET models</c>), e.g. "gemma-4-26b-a4b-it". Google
    /// retires and renames models, so the demo checks which configured ones still exist instead of trusting the list.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ListModelsAsync(GoogleAiOptions options, HttpMessageHandler? handler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        using var provider = new GoogleAiModelProvider(options, handler);
        var models = new List<string>();
        string? page = null;
        do
        {
            var address = "models?pageSize=1000" + (page is null ? string.Empty : "&pageToken=" + Uri.EscapeDataString(page));
            using var response = await HostedHttp.SendAsync(
                provider._http, () => new HttpRequestMessage(HttpMethod.Get, new Uri(address, UriKind.Relative)), "Google AI Studio", RateLimitPolicy.None, cancellationToken);
            var list = JsonSerializer.Deserialize<ModelList>(await response.Content.ReadAsStringAsync(cancellationToken));
            models.AddRange((list?.Models ?? [])
                .Where(m => m.Name is not null && (m.Methods ?? []).Any(x => x is "generateContent" or "streamGenerateContent"))
                .Select(m => m.Name!.StartsWith("models/", StringComparison.Ordinal) ? m.Name["models/".Length..] : m.Name!));
            page = string.IsNullOrEmpty(list?.NextPageToken) ? null : list.NextPageToken;
        }
        while (page is not null);

        return models;
    }

    public void Dispose() => _http.Dispose();

    private Request Body(ModelRequest request, string? thinking)
    {
        var maxTokens = request.MaxTokens is { } cap ? Math.Min(cap, _options.MaxTokens) : _options.MaxTokens;
        var config = new GenerationConfig(_options.Temperature, maxTokens, thinking is null ? null : new ThinkingConfig(thinking));
        return _options.MergeSystemPrompt
            ? new Request([new Content("user", [new Part(HostedHttp.Merged(request))])], null, config)
            : new Request([new Content("user", [new Part(request.UserPrompt)])], new Content(null, [new Part(request.SystemPrompt)]), config);
    }

    private sealed record Request(
        [property: JsonPropertyName("contents")] IReadOnlyList<Content> Contents,
        [property: JsonPropertyName("systemInstruction"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Content? SystemInstruction,
        [property: JsonPropertyName("generationConfig")] GenerationConfig GenerationConfig);

    private sealed record Content(
        [property: JsonPropertyName("role"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<Part>? Parts);

    private sealed record Part(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("thought"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Thought = null);

    private sealed record GenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens,
        [property: JsonPropertyName("thinkingConfig"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ThinkingConfig? ThinkingConfig);

    private sealed record ThinkingConfig([property: JsonPropertyName("thinkingLevel")] string ThinkingLevel);

    private sealed record Chunk(
        [property: JsonPropertyName("candidates")] IReadOnlyList<Candidate>? Candidates,
        [property: JsonPropertyName("promptFeedback")] PromptFeedback? PromptFeedback);

    private sealed record Candidate(
        [property: JsonPropertyName("content")] Content? Content,
        [property: JsonPropertyName("finishReason")] string? FinishReason);

    private sealed record ModelList(
        [property: JsonPropertyName("models")] IReadOnlyList<ModelInfo>? Models,
        [property: JsonPropertyName("nextPageToken")] string? NextPageToken);

    private sealed record ModelInfo(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("supportedGenerationMethods")] IReadOnlyList<string>? Methods);

    private sealed record PromptFeedback([property: JsonPropertyName("blockReason")] string? BlockReason);
}
