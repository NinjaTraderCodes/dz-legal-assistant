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
        if (LoopbackGuard.IsCloudModel(options.Model))
        {
            throw new ArgumentException(
                $"Refusing model '{options.Model}': Ollama sends prompts for cloud models to ollama.com, and case data must never leave this computer.",
                nameof(options));
        }

        _options = options;

        // No proxy, no redirects, loopback connections only (see LoopbackGuard.CreateHandler).
        _http = new HttpClient(handler ?? LoopbackGuard.CreateHandler(), disposeHandler: true)
        {
            BaseAddress = options.Endpoint,
            Timeout = options.Timeout,
        };
    }

    public string Name => $"ollama {_options.Model} @ {_options.Endpoint.Authority}";

    /// <summary>Receives the server's timings after each call (load, prompt reading, writing). Used by the benches.</summary>
    public Action<ModelTimings>? OnTimings { get; set; }

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var body = Body(request, stream: false);
        var clock = System.Diagnostics.Stopwatch.StartNew();

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
            Report(request, reply, clock.Elapsed);
            if (reply is { Done: false } || reply?.DoneReason == "length")
            {
                throw new ModelProviderException($"{Name} stopped at its answer length limit: the answer was cut off.");
            }

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
        var clock = System.Diagnostics.Stopwatch.StartNew();
        TimeSpan? firstWord = null;

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
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ModelProviderException($"The local model did not answer within {_options.Timeout.TotalMinutes:0} minutes.", ex);
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
                    firstWord ??= clock.Elapsed;
                    yield return piece;
                }

                if (chunk?.Done == true)
                {
                    Report(request, chunk, firstWord ?? clock.Elapsed);
                    if (chunk.DoneReason == "length")
                    {
                        throw new ModelProviderException($"{Name} stopped at its answer length limit: the answer was cut off.");
                    }

                    yield break;
                }
            }

            // The stream ended without the server's final record: the answer is incomplete.
            throw new ModelProviderException($"{Name} stopped before the answer was complete.");
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

    /// <summary>
    /// Asks the local server to load the model into memory now (<c>POST /api/generate</c> with no prompt), so the
    /// lawyer's first request does not wait for it. Nothing is generated; the model stays loaded for 30 minutes.
    /// </summary>
    public async Task WarmUpAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Same context window as the real requests: a different one would make Ollama load the model a second time.
            using var response = await _http.PostAsJsonAsync(
                new Uri("api/generate", UriKind.Relative),
                new WarmUpRequest(_options.Model, _options.KeepAlive, new WarmUpOptions(_options.ContextLength)),
                cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ModelProviderException($"The local model server is not reachable at {_options.Endpoint}.", ex);
        }
    }


    public void Dispose() => _http.Dispose();

    private ChatRequest Body(ModelRequest request, bool stream) => new(
        _options.Model,
        [new ChatMessage("system", request.SystemPrompt), new ChatMessage("user", request.UserPrompt)],
        Stream: stream,
        _options.Think,
        _options.KeepAlive,
        new ChatOptions(_options.Temperature, _options.ContextLength, MaxTokens(request), _options.RepeatPenalty));

    /// <summary>The call's own cap, never above the configured maximum.</summary>
    private int MaxTokens(ModelRequest request) => request.MaxTokens is { } cap ? Math.Min(cap, _options.MaxTokens) : _options.MaxTokens;

    private void Report(ModelRequest request, ChatResponse? done, TimeSpan firstWord)
    {
        if (OnTimings is null || done is null)
        {
            return;
        }

        static TimeSpan Ns(long ns) => TimeSpan.FromTicks(ns / 100);
        // The prompt filled the window: Ollama drops its beginning silently, so the bench says so.
        var truncated = done.PromptEvalCount + MaxTokens(request) > _options.ContextLength;
        OnTimings(new ModelTimings(
            request.SectionId,
            Ns(done.LoadDuration),
            done.PromptEvalCount,
            Ns(done.PromptEvalDuration),
            done.EvalCount,
            Ns(done.EvalDuration),
            firstWord,
            truncated));
    }

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
        [property: JsonPropertyName("keep_alive")] string KeepAlive,
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
        [property: JsonPropertyName("done")] bool Done = false,
        [property: JsonPropertyName("done_reason")] string? DoneReason = null,
        [property: JsonPropertyName("load_duration")] long LoadDuration = 0,
        [property: JsonPropertyName("prompt_eval_count")] int PromptEvalCount = 0,
        [property: JsonPropertyName("prompt_eval_duration")] long PromptEvalDuration = 0,
        [property: JsonPropertyName("eval_count")] int EvalCount = 0,
        [property: JsonPropertyName("eval_duration")] long EvalDuration = 0);

    private sealed record WarmUpRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("keep_alive")] string KeepAlive,
        [property: JsonPropertyName("options")] WarmUpOptions Options);

    private sealed record WarmUpOptions([property: JsonPropertyName("num_ctx")] int ContextLength);

    private sealed record TagsResponse([property: JsonPropertyName("models")] IReadOnlyList<TagModel>? Models);

    private sealed record TagModel([property: JsonPropertyName("name")] string Name);
}
