namespace DzLegal.Web.Site;

/// <summary>The "Site" configuration section: where the built website lives and whether it takes "/" from the demo.</summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    /// <summary>Folder of the built site (site/dist); relative paths resolve against the content root. Default: "site" next to the app.</summary>
    public string? Root { get; set; }

    /// <summary>When true, "/" serves the site in the visitor's language and the demo lives at /app only. False while the site is reviewed at /ar/ and /en/.</summary>
    public bool ServeRoot { get; set; }
}
