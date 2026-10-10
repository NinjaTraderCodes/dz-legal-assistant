using System.Net;
using System.Text.Json;
using System.Runtime.CompilerServices;
using DzLegal.Drafting;

namespace DzLegal.ModelProviders.Hosted;

/// <summary>What the hosted providers share: sending a request, reading server-sent events, classifying errors.</summary>
internal static class HostedHttp
{
    /// <summary>Sends the request; turns network failures, timeouts and error statuses into <see cref="ModelProviderException"/>.</summary>
    public static async Task<HttpResponseMessage> SendAsync(HttpClient http, HttpRequestMessage message, string engine, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
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

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            var detail = Truncate(await response.Content.ReadAsStringAsync(cancellationToken));
            throw new ModelProviderException($"{engine} error {(int)response.StatusCode}: {detail}") { IsTransient = IsTransient(response.StatusCode) };
        }
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
                var message = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var m) ? m.ToString() : error.ToString();
                throw new ModelProviderException($"{engine} error: {Truncate(message)}") { IsTransient = true };
            }

            return document.RootElement.Deserialize<T>()!;
        }
        catch (JsonException ex)
        {
            throw new ModelProviderException($"{engine} sent an unreadable answer.", ex) { IsTransient = true };
        }
    }

    private static string Truncate(string detail) => detail.Length > 300 ? detail[..300] + "…" : detail;

    /// <summary>
    /// The request body as JSON with a known length. Some gateways and proxies refuse chunked uploads, which is what
    /// JsonContent sends.
    /// </summary>
    public static StringContent Json<T>(T body) =>
        new(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

    /// <summary>The system rules on top of the user turn, for models that refuse a separate system instruction.</summary>
    public static string Merged(ModelRequest request) => request.SystemPrompt + "\n\n" + request.UserPrompt;
}
