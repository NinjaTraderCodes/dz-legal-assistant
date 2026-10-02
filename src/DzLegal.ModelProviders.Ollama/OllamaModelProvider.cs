using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DzLegal.Drafting;

namespace DzLegal.ModelProviders.Ollama;

/// <summary>
/// Calls a local Ollama server (<c>POST /api/chat</c>). This is inter-process communication on the
/// same machine: the endpoint is checked by <see cref="LoopbackGuard"/>, the system proxy is bypassed,
/// and there is no fallback to any other endpoint.
/// </summary>
public sealed class OllamaModelProvider : IModelProvider, IDisposable
{
    private readonly OllamaOptions _options;
    private readonly HttpClient _http;

    public OllamaModelProvider(OllamaOptions options, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        LoopbackGuard.EnsureLoopback(options.Endpoint);
        _options = options;

        // UseProxy = false: a configured HTTP proxy must never see the request.
        _http = new HttpClient(handler ?? new SocketsHttpHandler { UseProxy = false }, disposeHandler: true)
        {
            BaseAddress = options.Endpoint,
            Timeout = options.Timeout,
        };
    }

    public string Name => $"ollama {_options.Model} @ {_options.Endpoint.Authority}";

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var body = Body(request, stream: false);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(new Uri("api/chat", UriKind.Relative), body, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ModelProviderException(
                $"The local model server is not reachable at {_options.Endpoint}. Start Ollama, or switch to template mode.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ModelProviderException($"The local model did not answer within {_options.Timeout.TotalMinutes:0} minutes.", ex);
        }

        using (response)
        {
            await EnsureSuccessAsync(response, cancellationToken);
            var reply = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
            return reply?.Message?.Content
                ?? throw new ModelProviderException("The local model returned an empty answer.");
        }
    }

    /// <summary>Streams the answer token by token (<c>"stream": true</c>), so the lawyer sees it appear.</summary>
    public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri("api/chat", UriKind.Relative))
        {
            Content = JsonContent.Create(Body(request, stream: true)),
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ModelProviderException(
                $"The local model server is not reachable at {_options.Endpoint}. Start Ollama, or switch to template mode.", ex);
        }

        using (response)
        {
            await EnsureSuccessAsync(response, cancellationToken);
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (line.Length == 0)
                {
                    continue;
                }

                var chunk = System.Text.Json.JsonSerializer.Deserialize<ChatResponse>(line);
                if (chunk?.Message?.Content is { Length: > 0 } piece)
                {
                    yield return piece;
                }

                if (chunk?.Done == true)
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>Asks the local server which models are installed (<c>GET /api/tags</c>); used for the status indicator.</summary>
    public async Task<IReadOnlyList<string>> GetInstalledModelsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(new Uri("api/tags", UriKind.Relative), cancellationToken);
            response.EnsureSuccessStatusCode();
            var tags = await response.Content.ReadFromJsonAsync<TagsResponse>(cancellationToken);
            return tags?.Models?.Select(m => m.Name).ToList() ?? [];
        }
        catch (HttpRequestException ex)
        {
            throw new ModelProviderException($"The local model server is not reachable at {_options.Endpoint}.", ex);
        }
    }

    public string ModelName => _options.Model;

    public void Dispose() => _http.Dispose();

    private ChatRequest Body(ModelRequest request, bool stream) => new(
        _options.Model,
        [new ChatMessage("system", request.SystemPrompt), new ChatMessage("user", request.UserPrompt)],
        Stream: stream,
        _options.Think,
        new ChatOptions(_options.Temperature, _options.ContextLength, _options.MaxTokens, _options.RepeatPenalty));

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ModelProviderException($"Model '{_options.Model}' is not installed locally. Run: ollama pull {_options.Model}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ModelProviderException($"Local model error {(int)response.StatusCode}: {detail}");
        }
    }

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("think"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Think,
        [property: JsonPropertyName("options")] ChatOptions Options);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("num_ctx")] int ContextLength,
        [property: JsonPropertyName("num_predict")] int MaxTokens,
        [property: JsonPropertyName("repeat_penalty")] double RepeatPenalty);

    private sealed record ChatResponse(
        [property: JsonPropertyName("message")] ChatMessage? Message,
        [property: JsonPropertyName("done")] bool Done = false);

    private sealed record TagsResponse([property: JsonPropertyName("models")] IReadOnlyList<TagModel>? Models);

    private sealed record TagModel([property: JsonPropertyName("name")] string Name);
}
