using System.Net;
using System.Net.Sockets;

namespace DzLegal.ModelProviders.Ollama;

/// <summary>
/// Makes sure a model endpoint is on this machine. Only literal loopback addresses (127.0.0.0/8, ::1)
/// and "localhost" are accepted. Other host names are refused without a DNS lookup, so a name that
/// resolves to a remote address can never be used.
/// </summary>
public static class LoopbackGuard
{
    public static bool IsLoopback(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(endpoint.UserInfo))
        {
            return false;
        }

        if (string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(endpoint.Host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address);
    }

    /// <summary>
    /// The HTTP handler for the local model server. Checking the endpoint once is not enough, so the handler itself
    /// refuses to leave the machine:
    /// <list type="bullet">
    /// <item>no proxy: a configured HTTP proxy must never see the request;</item>
    /// <item>no redirects: a redirect to another host would otherwise carry the case data there;</item>
    /// <item>every connection is opened by hand, to a loopback address only, whatever host the request names.</item>
    /// </list>
    /// </summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        ConnectCallback = ConnectLoopbackAsync,
    };

    private static async ValueTask<Stream> ConnectLoopbackAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        IPAddress[] addresses = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            ? [IPAddress.Loopback, IPAddress.IPv6Loopback]
            : IPAddress.TryParse(host.Trim('[', ']'), out var literal) && IPAddress.IsLoopback(literal)
                ? [literal]
                : throw new HttpRequestException($"Refusing to connect to '{host}': only on-device (loopback) connections are allowed.");

        Exception? last = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                last = ex;
            }
        }

        throw new HttpRequestException($"The local model server is not reachable on port {context.DnsEndPoint.Port}.", last);
    }

    /// <summary>
    /// Ollama can also run "cloud" models (names ending in <c>-cloud</c>, or the <c>cloud</c> tag): the local server then
    /// forwards the prompt to ollama.com. Those models are refused, because case data must never leave this computer.
    /// </summary>
    public static bool IsCloudModel(string model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var name = model.Trim().ToLowerInvariant();
        return name.EndsWith("-cloud", StringComparison.Ordinal) || name.EndsWith(":cloud", StringComparison.Ordinal)
            || name.Contains("-cloud:", StringComparison.Ordinal);
    }

    public static Uri EnsureLoopback(Uri endpoint)
    {
        if (!IsLoopback(endpoint))
        {
            throw new ArgumentException(
                $"Refusing model endpoint '{endpoint}': only on-device (loopback) endpoints are allowed, " +
                "because case data must never leave this computer.",
                nameof(endpoint));
        }

        return endpoint;
    }
}
