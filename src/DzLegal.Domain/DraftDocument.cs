namespace DzLegal.Domain;

/// <summary>
/// A draft for the lawyer's review: the inputs it was built from plus its ordered sections.
/// A draft is never final; the lawyer reviews, edits and signs it.
/// </summary>
public sealed record DraftDocument
{
    public required string DocumentTypeId { get; init; }
    public string DefinitionVersion { get; init; } = string.Empty;
    public Dictionary<string, string> Inputs { get; init; } = [];
    public List<DraftSection> Sections { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>Name of the model provider that wrote the narrative sections (for evaluation records).</summary>
    public string? GeneratedBy { get; init; }

    /// <summary>Articles from the law library the lawyer chose to rely on; their official text grounds the model.</summary>
    public List<ArticleRef> SelectedArticles { get; init; } = [];

    /// <summary>The last grounds analysis (the model's reading of the facts and the articles it proposed), if any.</summary>
    public GroundsAnalysis? GroundsAnalysis { get; init; }

    public DraftSection? FindSection(string id) => Sections.FirstOrDefault(s => s.Id == id);

    /// <summary>Deep copy, so a snapshot is not affected by later edits.</summary>
    public DraftDocument DeepCopy() => this with
    {
        Inputs = new Dictionary<string, string>(Inputs, StringComparer.Ordinal),
        Sections = Sections.Select(s => new DraftSection { Id = s.Id, Text = s.Text, DraftingNotes = [.. s.DraftingNotes], Overridden = s.Overridden }).ToList(),
        SelectedArticles = [.. SelectedArticles],
    };
}

/// <summary>An article of a code in the law library, e.g. ("civil-code", "106").</summary>
public sealed record ArticleRef(string Code, string Number);

/// <summary>
/// The result of the grounds analysis: the model's reading of the facts, the catalog claim types it recognised,
/// and the articles it proposed (each one from the official library, with its role and the model's reason).
/// Assistive only: nothing here is used until the lawyer ticks it.
/// </summary>
public sealed record GroundsAnalysis
{
    /// <summary>Hash of the facts, subject and court section analysed, to tell when the facts have changed since.</summary>
    public required string FactsHash { get; init; }

    public string Relationship { get; init; } = string.Empty;

    public string Breach { get; init; } = string.Empty;

    public string Sought { get; init; } = string.Empty;

    public List<string> ClaimIds { get; init; } = [];

    public List<AnalysedArticle> Articles { get; init; } = [];

    public string Model { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}

/// <summary>An article the model proposed: its role (basis, proof, damages, procedure), its reason, and where the candidate came from.</summary>
public sealed record AnalysedArticle(string Code, string Number, string Role, string Reason, string Source)
{
    public const string FromCatalog = "catalog";
    public const string FromSearch = "search";

    public ArticleRef Ref => new(Code, Number);
}

public sealed class DraftSection
{
    public required string Id { get; init; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// What happened while the model wrote this section, as <see cref="DraftingNoteCodes"/>.
    /// Empty for a normal draft or for text the lawyer wrote.
    /// </summary>
    public List<string> DraftingNotes { get; init; } = [];

    /// <summary>
    /// A generated section the lawyer chose to edit by hand: it keeps their text and no longer follows the form
    /// until restored.
    /// </summary>
    public bool Overridden { get; set; }
}

/// <summary>Codes stored in <see cref="DraftSection.DraftingNotes"/>.</summary>
public static class DraftingNoteCodes
{
    /// <summary>The first answer was unusable; the second one was kept.</summary>
    public const string Retried = "retried";

    /// <summary>Both answers were unusable; the lawyer's notes were inserted unchanged.</summary>
    public const string FellBackToNotes = "fellBackToNotes";

    /// <summary>Paragraphs copied from the prompt's worked example were removed.</summary>
    public const string RemovedCopiedExample = "removedCopiedExample";
}
