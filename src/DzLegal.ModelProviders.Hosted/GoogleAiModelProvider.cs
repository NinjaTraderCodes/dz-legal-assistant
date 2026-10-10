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
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri($"models/{_options.Model}:streamGenerateContent?alt=sse", UriKind.Relative))
        {
            Content = HostedHttp.Json(Body(request)),
        };

        using var response = await HostedHttp.SendAsync(_http, message, Name, cancellationToken);
        await foreach (var data in HostedHttp.ReadEventsAsync(response, cancellationToken))
        {
            var chunk = JsonSerializer.Deserialize<Chunk>(data);
            if (chunk?.PromptFeedback?.BlockReason is { } blocked)
            {
                throw new ModelProviderException($"{Name} refused the request ({blocked}).");
            }

            foreach (var part in chunk?.Candidates is [var first, ..] ? first.Content?.Parts ?? [] : [])
            {
                // Models that think aloud mark those parts; only the answer goes to the document.
                if (part.Thought != true && part.Text is { Length: > 0 } piece)
                {
                    yield return piece;
                }
            }
        }
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
            using var message = new HttpRequestMessage(HttpMethod.Get, new Uri(address, UriKind.Relative));
            using var response = await HostedHttp.SendAsync(provider._http, message, "Google AI Studio", cancellationToken);
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

    private Request Body(ModelRequest request)
    {
        var maxTokens = request.MaxTokens is { } cap ? Math.Min(cap, _options.MaxTokens) : _options.MaxTokens;
        var config = new GenerationConfig(_options.Temperature, maxTokens);
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
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

    private sealed record Chunk(
        [property: JsonPropertyName("candidates")] IReadOnlyList<Candidate>? Candidates,
        [property: JsonPropertyName("promptFeedback")] PromptFeedback? PromptFeedback);

    private sealed record Candidate([property: JsonPropertyName("content")] Content? Content);

    private sealed record ModelList(
        [property: JsonPropertyName("models")] IReadOnlyList<ModelInfo>? Models,
        [property: JsonPropertyName("nextPageToken")] string? NextPageToken);

    private sealed record ModelInfo(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("supportedGenerationMethods")] IReadOnlyList<string>? Methods);

    private sealed record PromptFeedback([property: JsonPropertyName("blockReason")] string? BlockReason);
}
