using System.Net;
using System.Text.Json;
using System.Runtime.CompilerServices;
using DzLegal.Drafting;

namespace DzLegal.ModelProviders.Hosted;

/// <summary>What the hosted providers share: sending a request, reading server-sent events, classifying errors.</summary>
internal static class HostedHttp
{
    /// <summary>
    /// Sends the request built by <paramref name="create"/>; turns network failures, timeouts and error statuses into
    /// <see cref="ModelProviderException"/>. When the service answers "too many requests" (429) or "busy" (503), it waits
    /// (as long as the service asks, else 10 s, 20 s, 40 s, then 60 s) and sends again, within the policy's budget. This
    /// happens before any text is read, so nothing is ever duplicated. A server error (500, 502, 504) is sent again up to
    /// twice after a short pause: Google AI Studio answers "500 Internal error" in bursts that pass within seconds.
    /// </summary>
    public static async Task<HttpResponseMessage> SendAsync(HttpClient http, Func<HttpRequestMessage> create, string engine, RateLimitPolicy policy, CancellationToken cancellationToken)
    {
        var waited = TimeSpan.Zero;
        var serverRetries = 0;
        for (var attempt = 1; ; attempt++)
        {
            HttpResponseMessage response;
            using (var message = create())
            {
                try
                {
                    response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    throw new ModelProviderException($"{engine} is not reachable.", ex) { IsTransient = true };
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new ModelProviderException($"{engine} did not answer in time.", ex) { IsTransient = true };
                }
            }

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            using (response)
            {
                var remaining = policy.MaxWait - waited;
                if (IsRateLimit(response.StatusCode) && remaining > TimeSpan.Zero && attempt < MaxAttempts)
                {
                    var delay = RetryAfter(response) ?? Backoff(attempt);
                    delay = delay > remaining ? remaining : delay;
                    if (policy.OnWait is not null)
                    {
                        policy.OnWait(delay, attempt + 1, Truncate(Explanation(await response.Content.ReadAsStringAsync(cancellationToken)), 160));
                    }

                    await Task.Delay(delay, cancellationToken);
                    waited += delay;
                    continue;
                }

                if (IsServerError(response.StatusCode) && serverRetries < MaxServerRetries)
                {
                    serverRetries++;
                    await Task.Delay(ServerErrorPause * serverRetries, cancellationToken);
                    continue;
                }

                throw Error(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken), engine);
            }
        }
    }

    /// <summary>A ceiling on attempts, for a service that keeps saying "retry now" (Retry-After: 0).</summary>
    private const int MaxAttempts = 12;

    private const int MaxServerRetries = 2;

    /// <summary>The pause before resending after a server error (then twice as long); tests set it to zero.</summary>
    internal static TimeSpan ServerErrorPause { get; set; } = TimeSpan.FromSeconds(3);

    public static bool IsServerError(HttpStatusCode status) =>
        status is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout;

    public static bool IsRateLimit(HttpStatusCode status) => status is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;

    private static TimeSpan? RetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        var delay = header?.Delta ?? (header?.Date is { } date ? date - DateTimeOffset.UtcNow : null);
        return delay is null ? null : delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    private static TimeSpan Backoff(int attempt)
    {
        var seconds = Math.Min(60, 10 * Math.Pow(2, attempt - 1));
        return TimeSpan.FromSeconds(seconds * (1 + (Random.Shared.NextDouble() * 0.2)));
    }

    /// <summary>
    /// The error for a refused request, with the service's own explanation rather than its raw JSON. A rate limit says
    /// so plainly (<see cref="ModelProviderException.IsRateLimited"/>), so the app can tell the lawyer to wait.
    /// </summary>
    internal static ModelProviderException Error(HttpStatusCode status, string body, string engine) =>
        IsRateLimit(status)
            ? new ModelProviderException($"{engine} is rate-limited by its provider (too many requests): {Truncate(Explanation(body), 160)}") { IsTransient = true, IsRateLimited = true }
            : new ModelProviderException($"{engine} error {(int)status}: {Truncate(Explanation(body))}") { IsTransient = IsTransient(status) };

    /// <summary>
    /// What the service says went wrong: OpenRouter's upstream detail (<c>error.metadata.raw</c>) when present, else
    /// <c>error.message</c>, else the body as it came.
    /// </summary>
    internal static string Explanation(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object
                        && metadata.TryGetProperty("raw", out var raw) && raw.ValueKind == JsonValueKind.String)
                    {
                        return raw.GetString()!;
                    }

                    if (error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString()!;
                    }
                }
                else if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON: shown as it came.
        }

        return body.Trim();
    }

    /// <summary>
    /// Worth trying another engine: busy or briefly down, this model is not served (404: Google retires models), or this
    /// service refuses the key or has no credit left (401, 402, 403): the next service has its own key and quota. A
    /// refused request (400) is not: the next engine would refuse it too.
    /// </summary>
    public static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.Unauthorized or HttpStatusCode.PaymentRequired or HttpStatusCode.Forbidden
            or HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    /// <summary>How long an answer may go silent before the engine counts as stalled.</summary>
    public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromSeconds(45);

    /// <summary>
    /// The payload of each <c>data:</c> line of a server-sent-events stream, until <c>[DONE]</c> or the end. A connection
    /// that drops, or an answer that stays silent longer than <paramref name="idleTimeout"/>, becomes a
    /// <see cref="ModelProviderException"/>: the caller then shows a message instead of losing the visitor's page, and the
    /// queue slot is freed.
    /// </summary>
    public static async IAsyncEnumerable<string> ReadEventsAsync(
        HttpResponseMessage response,
        string engine,
        TimeSpan idleTimeout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        StreamReader reader;
        try
        {
            reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException)
        {
            throw new ModelProviderException($"{engine} stopped answering.", ex) { IsTransient = true };
        }

        using (reader)
        {
            while (true)
            {
                idle.CancelAfter(idleTimeout);
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(idle.Token);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new ModelProviderException($"{engine} went silent for {idleTimeout.TotalSeconds:0} seconds.", ex) { IsTransient = true };
                }
                catch (Exception ex) when (ex is IOException or HttpRequestException)
                {
                    throw new ModelProviderException($"{engine} stopped answering.", ex) { IsTransient = true };
                }

                if (line is null)
                {
                    yield break;
                }

                if (!line.StartsWith("data:", StringComparison.Ordinal))
                {
                    continue;
                }

                var data = line[5..].Trim();
                if (data == "[DONE]")
                {
                    yield break;
                }

                if (data.Length > 0)
                {
                    yield return data;
                }
            }
        }
    }

    /// <summary>
    /// Reads one event. Malformed JSON, or an error the service reports inside a successful stream (OpenRouter does this
    /// when credit runs out mid-request), becomes a <see cref="ModelProviderException"/> that lets the next engine try.
    /// </summary>
    public static T Parse<T>(string data, string engine)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind != JsonValueKind.Null)
            {
                var code = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
                var explanation = Explanation(data);
                throw code == 429
                    ? new ModelProviderException($"{engine} is rate-limited by its provider (too many requests): {Truncate(explanation, 160)}") { IsTransient = true, IsRateLimited = true }
                    : new ModelProviderException($"{engine} error: {Truncate(explanation)}") { IsTransient = true };
            }

            return document.RootElement.Deserialize<T>()!;
        }
        catch (JsonException ex)
        {
            throw new ModelProviderException($"{engine} sent an unreadable answer.", ex) { IsTransient = true };
        }
    }

    private static string Truncate(string detail, int max = 300) => detail.Length > max ? detail[..max] + "…" : detail;

    /// <summary>
    /// The request body as JSON with a known length. Some gateways and proxies refuse chunked uploads, which is what
    /// JsonContent sends.
    /// </summary>
    public static StringContent Json<T>(T body) =>
        new(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

    /// <summary>The system rules on top of the user turn, for models that refuse a separate system instruction.</summary>
    public static string Merged(ModelRequest request) => request.SystemPrompt + "\n\n" + request.UserPrompt;
}
