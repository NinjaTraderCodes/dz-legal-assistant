namespace DzLegal.Web.Site;

/// <summary>The two lines the server logs about the website at start-up.</summary>
internal static partial class SiteLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Website served from {Root}; '/' serves the site: {ServeRoot}")]
    public static partial void SiteServed(ILogger logger, string root, bool serveRoot);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Website not found at {Root}: '/' stays the demo")]
    public static partial void SiteMissing(ILogger logger, string root);
}
