namespace DzLegal.Web.Site;

/// <summary>
/// The /lang/{code} endpoint's decision: a valid code becomes the remembered language, and the visitor goes back to
/// a local page (never to another site). Pure, so it is testable.
/// </summary>
internal static class LanguageSwitch
{
    public const string ThemeCookieName = "dzlegal.theme";

    /// <summary>Dark is the default of the site and the demo alike; only an explicit "light" choice changes it.</summary>
    public static bool IsDarkTheme(string? themeCookie) => !string.Equals(themeCookie, "light", StringComparison.Ordinal);

    /// <summary>Returns the local path to redirect to, or null when the code is not a language the site has.</summary>
    public static string? Plan(string? code, string? returnTo)
    {
        if (!LanguageChoice.IsValid(code))
        {
            return null;
        }

        return IsLocalPath(returnTo) ? returnTo! : $"/{code}/";
    }

    /// <summary>A path on this site only: it starts with a single slash and cannot name a scheme or another host.</summary>
    internal static bool IsLocalPath(string? path)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/' || path.Length > 2048)
        {
            return false;
        }

        if (path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        return !path.Contains('\\', StringComparison.Ordinal) && !path.Contains("://", StringComparison.Ordinal)
            && !path.Contains('\r', StringComparison.Ordinal) && !path.Contains('\n', StringComparison.Ordinal);
    }

    public static CookieOptions CookieOptions(bool secure) => new()
    {
        Path = "/",
        MaxAge = TimeSpan.FromDays(365),
        SameSite = SameSiteMode.Lax,
        Secure = secure,
        IsEssential = true,
        HttpOnly = false,
    };
}
