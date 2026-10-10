using System.Net;
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
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            if (detail.Length > 300)
            {
                detail = detail[..300] + "…";
            }

            throw new ModelProviderException($"{engine} error {(int)response.StatusCode}: {detail}") { IsTransient = IsTransient(response.StatusCode) };
        }
    }

    /// <summary>
    /// Worth trying another engine: busy or briefly down, or this model is not served (404: Google retires models). A bad
    /// key or a refused request is not.
    /// </summary>
    public static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    /// <summary>The payload of each <c>data:</c> line of a server-sent-events stream, until <c>[DONE]</c> or the end.</summary>
    public static async IAsyncEnumerable<string> ReadEventsAsync(HttpResponseMessage response, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
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

    /// <summary>
    /// The request body as JSON with a known length. Some gateways and proxies refuse chunked uploads, which is what
    /// JsonContent sends.
    /// </summary>
    public static StringContent Json<T>(T body) =>
        new(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");

    /// <summary>The system rules on top of the user turn, for models that refuse a separate system instruction.</summary>
    public static string Merged(ModelRequest request) => request.SystemPrompt + "\n\n" + request.UserPrompt;
}
