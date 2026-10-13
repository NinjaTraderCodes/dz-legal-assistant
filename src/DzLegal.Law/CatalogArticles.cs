using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>An article of a claim type, with its official text when that code has been imported, and its provenance.</summary>
public sealed record ArticleSuggestion(ClaimArticle Reference, string CitationName, LawArticle? Article, ArticleProvenance Provenance)
{
    public bool IsAvailable => Article is { Repealed: false };

    /// <summary>«المادة 106 من القانون المدني».</summary>
    public string Citation => $"المادة {Reference.Number} من {CitationName}";
}

/// <summary>Litigation areas of the claim catalog, and how a court section names them.</summary>
public static class LegalAreas
{
    /// <summary>Administrative courts: no claim type of the catalog belongs there (they are out of the app's scope).</summary>
    public const string Administrative = "administrative";

    /// <summary>The litigation area named by a court section («القسم التجاري» → commercial), or null when unclear.</summary>
    public static string? AreaOf(string? courtSection)
    {
        var tokens = ArabicScript.Tokens(courtSection);
        bool Has(string word) => tokens.Any(t => ArabicScript.MatchesWord(t, ArabicScript.Tokens(word)[0]));
        return Has("الإدارية") || Has("الإداري") || Has("إداري") ? Administrative
            : Has("الاجتماعي") || Has("العمل") ? "social"
            : Has("الأسرة") || Has("شؤون") ? "family"
            : Has("التجاري") || Has("البحري") ? "commercial"
            : Has("المدني") || Has("العقاري") ? "civil"
            : null;
    }
}
