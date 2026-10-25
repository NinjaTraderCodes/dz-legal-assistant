namespace DzLegal.Law.Corpus;

/// <summary>Publication evidence is not lawyer verification. Dates are ISO calendar dates.</summary>
public sealed record CorpusDownload(string Url, string Sha256, DateOnly RetrievedOn);
public sealed record CorpusJournal(string Issue, DateOnly Date, IReadOnlyList<int> Pages);
public sealed record CorpusDiscovery(string Url, DateOnly AccessedOn, string Note);

public sealed record CorpusPublication
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string Number { get; init; }
    public DateOnly Date { get; init; }
    public required CorpusJournal Jo { get; init; }
    public required CorpusDownload Arabic { get; init; }
    public CorpusDownload? French { get; init; }
    public IReadOnlyList<string> ArticlesTouched { get; init; } = [];
    public DateOnly? CommencementDate { get; init; }
    public string CommencementBasis { get; init; } = string.Empty;
    public IReadOnlyList<string> TransitionalProvisions { get; init; } = [];
    public IReadOnlyList<CorpusDiscovery> Discovery { get; init; } = [];
}

public sealed record CorpusChapter(string Id, IReadOnlyList<string> Path, IReadOnlyList<string> Articles);

public sealed record CorpusScope
{
    public required string Kind { get; init; }
    public IReadOnlyList<string> TargetArticles { get; init; } = [];
    public DateOnly AmendmentDiscoveryThrough { get; init; }
    public bool Complete { get; init; }
    public IReadOnlyList<string> Unsettled { get; init; } = [];
    public IReadOnlyList<CorpusChapter> CoveredChapters { get; init; } = [];
}

public sealed record CorpusLedger
{
    public string SchemaVersion { get; init; } = "1.0";
    public required string Id { get; init; }
    public required string CitationName { get; init; }
    public required string Reference { get; init; }
    public required CorpusScope Scope { get; init; }
    public IReadOnlyList<CorpusPublication> Publications { get; init; } = [];
}

public sealed record CorpusCheck(string Check, string Method, DateOnly Date, string Result, string Note = "",
    string? TextSha256 = null, IReadOnlyList<string>? ImageSha256s = null);
public sealed record CorpusVerification(string State, IReadOnlyList<CorpusCheck> Checks, IReadOnlyList<string> Reasons);
public sealed record CorpusCrossReference(string TargetId, int Paragraph, string Quote);
public sealed record CorpusSourceFragment(string PublicationId, int Page, string TextLayer, string Ocr,
    string EvidenceId, IReadOnlyList<double> Region, int? Paragraph = null,
    string? SourceSha256 = null, string? ParagraphTextSha256 = null, string Role = "body");

/// <summary>Page is one-based PDF page, not the journal's printed page label. Raw streams stay separate.</summary>
public sealed record CorpusArticleSource
{
    public required string PublicationId { get; init; }
    public int Page { get; init; }
    public int? EndPage { get; init; }
    public string? PrintedPage { get; init; }
    public required string TextLayer { get; init; }
    public required string Ocr { get; init; }
    public string? EvidenceId { get; init; }
    public IReadOnlyList<double> Region { get; init; } = [];
    /// <summary>Multiple pages or amendment sources; keeps each fragment's own provenance.</summary>
    public IReadOnlyList<CorpusSourceFragment> Fragments { get; init; } = [];
}

public sealed record CorpusArticleVersion
{
    public required string VersionId { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<string> Paragraphs { get; init; } = [];
    /// <summary>Headings applicable to this version; an empty list retains the article-level path.</summary>
    public IReadOnlyList<string> Path { get; init; } = [];
    public DateOnly? InForceFrom { get; init; }
    public DateOnly? InForceTo { get; init; }
    /// <summary>Unsettled null bounds never mean an interval from minus to plus infinity.</summary>
    public required string TemporalStatus { get; init; }
    public required string IntroducedBy { get; init; }
    public required string Status { get; init; }
    public required CorpusArticleSource Source { get; init; }
    public IReadOnlyList<CorpusCrossReference> CrossReferences { get; init; } = [];
    public required CorpusVerification Verification { get; init; }
}

public sealed record CorpusArticle
{
    public required string Id { get; init; }
    public required string Number { get; init; }
    public required string CanonicalNumber { get; init; }
    public IReadOnlyList<string> Path { get; init; } = [];
    /// <summary>Reference-only entries identify an explicitly cited target, without inventing its missing text.</summary>
    public string Availability { get; init; } = "text";
    public IReadOnlyList<CorpusArticleVersion> Versions { get; init; } = [];
}

public sealed record CorpusArticlesFile
{
    public string SchemaVersion { get; init; } = "1.0";
    public required string CodeId { get; init; }
    public IReadOnlyList<CorpusArticle> Articles { get; init; } = [];
}

public sealed record CorpusUnsettledItem(string Id, string Reason);
public sealed record CorpusInventory
{
    public required string Scope { get; init; }
    public IReadOnlyList<string> Expected { get; init; } = [];
    public IReadOnlyList<string> Present { get; init; } = [];
    public IReadOnlyList<string> Gaps { get; init; } = [];
    public IReadOnlyList<string> UnappliedPublications { get; init; } = [];
    public bool Complete { get; init; }
}

public sealed record CorpusReport
{
    public string SchemaVersion { get; init; } = "1.0";
    public required string CodeId { get; init; }
    public DateOnly GeneratedOn { get; init; }
    public required CorpusInventory Inventory { get; init; }
    public IReadOnlyDictionary<string, int> CountsByState { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<CorpusUnsettledItem> Unsettled { get; init; } = [];
    public string Coverage { get; init; } = "partial";
    public IReadOnlyList<CorpusChapter> CoveredChapters { get; init; } = [];
}

public sealed record CorpusCode(CorpusLedger Ledger, CorpusArticlesFile Content, CorpusReport Report);
public sealed record CorpusVersionResolution(string State, CorpusArticleVersion? Version, string Reason);

/// <summary>Offline data only; resolving a version does not decide temporal legal applicability to a case.</summary>
public sealed class LawCorpus(IReadOnlyList<CorpusCode> codes)
{
    public IReadOnlyList<CorpusCode> Codes { get; } = codes;

    public CorpusVersionResolution VersionOn(string articleId, DateOnly date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);
        var matches = Codes.SelectMany(c => c.Content.Articles.Select(a => (Code: c, Article: a)))
            .Where(x => x.Article.Id == articleId).ToList();
        if (matches.Count == 0)
        {
            return new("missing", null, "Article is outside the loaded corpus.");
        }

        var (code, article) = matches.Single();
        if (article.Availability == "reference-only" || article.Versions.Count == 0)
        {
            return new("unsettled", null, "The cited identity is recorded but its article text is not imported.");
        }

        if (date > code.Ledger.Scope.AmendmentDiscoveryThrough)
        {
            return new("unsettled", null, "Requested date is after the recorded amendment-discovery cut-off.");
        }

        if (article.Versions.Any(v => v.TemporalStatus != "resolved"))
        {
            return new("unsettled", null, "At least one version has unresolved temporal bounds.");
        }

        var version = article.Versions.SingleOrDefault(v => v.InForceFrom <= date && (v.InForceTo is null || date < v.InForceTo));
        if (version is null)
        {
            return new("not-in-force", null, "No recorded version covers this date.");
        }

        if (version.Verification.State == "unsettled")
        {
            return new("unsettled", null, "A dated candidate exists, but its transcription remains unsettled.");
        }

        return new(version.Status == "repealed" ? "repealed" : version.Verification.State == "checked" ? "resolved" : "partly-checked",
            version, "Recorded interval only; inspect verification and transitional applicability separately.");
    }
}
