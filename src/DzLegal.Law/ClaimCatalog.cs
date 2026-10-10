using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>
/// What an article does for a claim: founds it, proves it, measures damages, governs procedure, limits what may be
/// asked (no interest on a loan between individuals), or sets a step needed first (formal notice before damages).
/// </summary>
public enum ArticleRole
{
    Basis,
    Proof,
    Damages,
    Procedure,
    Limit,
    Prerequisite,
}

/// <summary>An article a claim type relies on, with why, as a lawyer would explain it.</summary>
public sealed record ClaimArticle
{
    public required string Code { get; init; }

    public required string Number { get; init; }

    public ArticleRole Role { get; init; } = ArticleRole.Basis;

    /// <summary>
    /// The article a requête of this type rests on: always proposed when the claim type is recognised, whatever the
    /// model chooses, so a small model cannot drop the loan article from a loan claim.
    /// </summary>
    public bool Required { get; init; }

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

    /// <summary>
    /// One Arabic line saying when this claim type applies and how it differs from its neighbours (who asks whom
    /// for what). The model recognises claim types from the label and this line; a lawyer checks it.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    public bool Verified { get; init; }

    /// <summary>The claim arises from a contract (loan, sale, lease…), so liability for fault (CC 124) is the wrong basis.</summary>
    public bool Contractual { get; init; }

    public IReadOnlyList<ClaimArticle> Articles { get; init; } = [];

    /// <summary>Steps required before or with filing (e.g. the labour conciliation report).</summary>
    public IReadOnlyList<LocalizedText> Prerequisites { get; init; } = [];

    /// <summary>Typical requests, in formal Arabic, to adapt.</summary>
    public IReadOnlyList<string> SuggestedRequests { get; init; } = [];

    public IReadOnlyList<string> RequiredDocuments { get; init; } = [];

    /// <summary>Facts the requête should state for this claim (shown as a checklist).</summary>
    public IReadOnlyList<string> FactsToState { get; init; } = [];
}

/// <summary>Prompt templates of the grounds analysis, from <c>data/law/prompts</c>.</summary>
public sealed record AnalysisPrompts(string ReadFacts, string SelectArticles)
{
    public static AnalysisPrompts None { get; } = new(string.Empty, string.Empty);

    public bool IsAvailable => ReadFacts.Length > 0 && SelectArticles.Length > 0;
}

/// <summary>One catalog file (<c>data/law/claims/&lt;area&gt;.json</c>).</summary>
public sealed record ClaimCatalogFile
{
    public required string Area { get; init; }

    public IReadOnlyList<ClaimType> Claims { get; init; } = [];
}

/// <summary>Everything the grounds suggestions need: the texts, what is importable, and the claim catalog.</summary>
public sealed class LegalKnowledge(LawLibrary library, LawSourceManifest manifest, IReadOnlyList<ClaimType> claims, AnalysisPrompts? prompts = null)
{
    public static LegalKnowledge Empty { get; } = new(LawLibrary.Empty, new LawSourceManifest(), []);

    public LawLibrary Library { get; } = library;

    public LawSourceManifest Manifest { get; } = manifest;

    public IReadOnlyList<ClaimType> Claims { get; } = claims;

    /// <summary>Prompts of the grounds analysis (reading the facts, choosing articles); empty when not shipped.</summary>
    public AnalysisPrompts Prompts { get; } = prompts ?? AnalysisPrompts.None;

    /// <summary>The catalog's articles for a claim type, with their official text when imported and their provenance.</summary>
    public IReadOnlyList<ArticleSuggestion> Resolve(ClaimType claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return claim.Articles
            .Select(a =>
            {
                var article = Library.FindArticle(a.Code, a.Number);
                return new ArticleSuggestion(a, CitationName(a.Code), article, ProvenanceOf(a.Code, article));
            })
            .ToList();
    }

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
        var localFile = imported is not null && !imported.Contains("://", StringComparison.Ordinal) ? imported : null;
        return new ArticleProvenance(
            reference,
            article is null ? string.Empty : string.Join(" › ", article.Path),
            exact ? imported : OfficialLinks.IsOfficial(entry?.OfficialSite) ? entry!.OfficialSite : null,
            exact,
            code?.Source?.RetrievedOn ?? string.Empty,
            code?.Verified == true || article?.Verified == true)
        {
            SourceFile = article is null ? null : localFile,
            Page = article?.Page,
            Edition = code?.Edition,
            VerifiedBy = article?.VerifiedBy,
            VerifiedOn = article?.VerifiedOn,
        };
    }

    /// <summary>«القانون المدني» for "civil-code", whether or not its text is imported yet.</summary>
    public string CitationName(string codeId) =>
        Library.FindCode(codeId)?.CitationName ?? Manifest.Codes.FirstOrDefault(c => c.Id == codeId)?.CitationName ?? codeId;
}
