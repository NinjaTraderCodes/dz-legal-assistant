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
        var body = new ChatRequest(
            _options.Model,
            [new ChatMessage("system", request.SystemPrompt), new ChatMessage("user", request.UserPrompt)],
            Stream: false,
            new ChatOptions(_options.Temperature, _options.ContextLength));

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
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ModelProviderException(
                    $"Model '{_options.Model}' is not installed locally. Run: ollama pull {_options.Model}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new ModelProviderException($"Local model error {(int)response.StatusCode}: {detail}");
            }

            var reply = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
            return reply?.Message?.Content
                ?? throw new ModelProviderException("The local model returned an empty answer.");
        }
    }

    public void Dispose() => _http.Dispose();

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("options")] ChatOptions Options);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("num_ctx")] int ContextLength);

    private sealed record ChatResponse(
        [property: JsonPropertyName("message")] ChatMessage? Message);
}
