using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>What an article does for a claim: founds it, proves it, measures damages, or governs procedure.</summary>
public enum ArticleRole
{
    Basis,
    Proof,
    Damages,
    Procedure,
}

/// <summary>A group of interchangeable words or phrases; the group counts once if any of them appears.</summary>
public sealed record TriggerGroup
{
    public IReadOnlyList<string> Any { get; init; } = [];

    public int Weight { get; init; } = 1;

    /// <summary>The claim is only suggested when this group matches.</summary>
    public bool Required { get; init; }
}

/// <summary>An article a claim type relies on, with why, as a lawyer would explain it.</summary>
public sealed record ClaimArticle
{
    public required string Code { get; init; }

    public required string Number { get; init; }

    public ArticleRole Role { get; init; } = ArticleRole.Basis;

    public required LocalizedText Why { get; init; }
}

/// <summary>
/// A type of claim as lawyers think of it ("repayment of a loan", "khulʿ"), with the articles it rests on and
/// what a requête for it usually asks and attaches. Curated data, checked by a lawyer.
/// </summary>
public sealed record ClaimType
{
    public required string Id { get; init; }

    /// <summary>Litigation area: civil, commercial, family or social. Set from the catalog file.</summary>
    public string Area { get; init; } = string.Empty;

    public required LocalizedText Label { get; init; }

    public bool Verified { get; init; }

    public IReadOnlyList<TriggerGroup> Triggers { get; init; } = [];

    /// <summary>Words that mean another claim type (e.g. «إيجار» for a loan); any of them rules this one out.</summary>
    public IReadOnlyList<string> Exclude { get; init; } = [];

    public IReadOnlyList<ClaimArticle> Articles { get; init; } = [];

    /// <summary>Steps required before or with filing (e.g. the labour conciliation report).</summary>
    public IReadOnlyList<LocalizedText> Prerequisites { get; init; } = [];

    /// <summary>Typical requests, in formal Arabic, to adapt.</summary>
    public IReadOnlyList<string> SuggestedRequests { get; init; } = [];

    public IReadOnlyList<string> RequiredDocuments { get; init; } = [];

    /// <summary>Facts the requête should state for this claim (shown as a checklist).</summary>
    public IReadOnlyList<string> FactsToState { get; init; } = [];
}

/// <summary>One catalog file (<c>data/law/claims/&lt;area&gt;.json</c>).</summary>
public sealed record ClaimCatalogFile
{
    public required string Area { get; init; }

    public IReadOnlyList<ClaimType> Claims { get; init; } = [];
}

/// <summary>Everything the grounds suggestions need: the texts, what is importable, and the claim catalog.</summary>
public sealed class LegalKnowledge(LawLibrary library, LawSourceManifest manifest, IReadOnlyList<ClaimType> claims, string claimPickPrompt = "")
{
    public static LegalKnowledge Empty { get; } = new(LawLibrary.Empty, new LawSourceManifest(), []);

    public LawLibrary Library { get; } = library;

    public LawSourceManifest Manifest { get; } = manifest;

    public IReadOnlyList<ClaimType> Claims { get; } = claims;

    /// <summary>Prompt template for the model fallback that chooses claim types from the catalog (empty: unavailable).</summary>
    public string ClaimPickPrompt { get; } = claimPickPrompt;

    /// <summary>
    /// Where an article of a code comes from: the official reference (with the Journal Officiel issue when known),
    /// its place in the code, and a link to check it — the exact official document it was imported from, or the
    /// official site while the code is not imported yet.
    /// </summary>
    public ArticleProvenance ProvenanceOf(string codeId, LawArticle? article)
    {
        var entry = Manifest.Codes.FirstOrDefault(c => c.Id == codeId);
        var code = Library.FindCode(codeId);
        var reference = code?.Reference ?? entry?.Reference ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(entry?.JoReference))
        {
            reference += " — " + entry.JoReference;
        }

        var imported = code?.Source?.Url;
        var exact = OfficialLinks.IsOfficial(imported);
        return new ArticleProvenance(
            reference,
            article is null ? string.Empty : string.Join(" › ", article.Path),
            exact ? imported : OfficialLinks.IsOfficial(entry?.OfficialSite) ? entry!.OfficialSite : null,
            exact,
            code?.Source?.RetrievedOn ?? string.Empty,
            code?.Verified == true);
    }

    /// <summary>«القانون المدني» for "civil-code", whether or not its text is imported yet.</summary>
    public string CitationName(string codeId) =>
        Library.FindCode(codeId)?.CitationName ?? Manifest.Codes.FirstOrDefault(c => c.Id == codeId)?.CitationName ?? codeId;
}
