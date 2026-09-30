using System.Net;

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
