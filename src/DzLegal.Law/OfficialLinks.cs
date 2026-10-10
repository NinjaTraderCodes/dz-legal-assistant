namespace DzLegal.Law;

/// <summary>
/// The only sites a link in the law data may point to: the official publications. A bad data edit can therefore
/// never put an arbitrary link in front of the lawyer, and the importer only downloads from these hosts.
/// </summary>
public static class OfficialLinks
{
    public static readonly IReadOnlyList<string> Hosts = ["joradp.dz", "mjustice.dz"];

    /// <summary>An absolute https URL on an official host (or one of its subdomains, e.g. www.joradp.dz).</summary>
    public static bool IsOfficial(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && Hosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Where an article's text comes from and who vouches for it, shown under each suggested article.</summary>
public sealed record ArticleProvenance(
    string Reference,
    string Location,
    string? SourceUrl,
    bool SourceIsExactText,
    string RetrievedOn,
    bool IsTextVerified)
{
    /// <summary>The official document the text was taken from (repository path of the PDF), when it is a local copy.</summary>
    public string? SourceFile { get; init; }

    /// <summary>Page of <see cref="SourceFile"/> where the article is printed.</summary>
    public int? Page { get; init; }

    /// <summary>Which publication: «الجريدة الرسمية العدد 21 …» or «النسخة المحينة …».</summary>
    public string? Edition { get; init; }

    /// <summary>Who checked the text against the source, and when (yyyy-mm-dd).</summary>
    public string? VerifiedBy { get; init; }

    public string? VerifiedOn { get; init; }
}
