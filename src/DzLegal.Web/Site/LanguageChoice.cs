namespace DzLegal.Web.Site;

/// <summary>
/// Which language a visitor gets: a remembered choice first (the cookie the /lang endpoint sets), then an explicit
/// ?lang= query, then the browser: an Arabic browser gets Arabic, every other one English. Pure, so it is testable.
/// </summary>
internal static class LanguageChoice
{
    public const string CookieName = "dzlegal.lang";
    public const string QueryName = "lang";
    public const string Arabic = "ar";
    public const string English = "en";

    public static bool IsValid(string? code) => code is Arabic or English;

    public static string Dir(string language) => language == Arabic ? "rtl" : "ltr";

    public static string Resolve(string? cookie, string? query, string? acceptLanguage)
    {
        if (IsValid(cookie))
        {
            return cookie!;
        }

        if (IsValid(query))
        {
            return query!;
        }

        return PrefersArabic(acceptLanguage) ? Arabic : English;
    }

    public static string FromRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Resolve(request.Cookies[CookieName], request.Query[QueryName], request.Headers.AcceptLanguage);
    }

    /// <summary>True when the browser's preferred language (the highest quality value, first on ties) is Arabic.</summary>
    internal static bool PrefersArabic(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return false;
        }

        string? best = null;
        var bestQuality = -1.0;
        foreach (var entry in acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(';', StringSplitOptions.TrimEntries);
            var tag = parts[0];
            var quality = 1.0;
            foreach (var parameter in parts.Skip(1))
            {
                if (parameter.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(parameter.AsSpan(2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var q))
                {
                    quality = q;
                }
            }

            if (tag.Length > 0 && quality > bestQuality)
            {
                best = tag;
                bestQuality = quality;
            }
        }

        if (best is null)
        {
            return false;
        }

        var primary = best.Split('-', 2)[0];
        return primary.Equals(Arabic, StringComparison.OrdinalIgnoreCase);
    }
}
