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

    public DraftSection? FindSection(string id) => Sections.FirstOrDefault(s => s.Id == id);

    /// <summary>Deep copy, so a snapshot is not affected by later edits.</summary>
    public DraftDocument DeepCopy() => this with
    {
        Inputs = new Dictionary<string, string>(Inputs, StringComparer.Ordinal),
        Sections = Sections.Select(s => new DraftSection { Id = s.Id, Text = s.Text, DraftingNote = s.DraftingNote }).ToList(),
    };
}

public sealed class DraftSection
{
    public required string Id { get; init; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// What happened while the model wrote this section, when anything did: see <see cref="DraftingNotes"/>.
    /// Null for a normal draft or for text the lawyer wrote.
    /// </summary>
    public string? DraftingNote { get; set; }
}

/// <summary>Codes stored in <see cref="DraftSection.DraftingNote"/>.</summary>
public static class DraftingNotes
{
    /// <summary>The first answer was unusable; the second one was kept.</summary>
    public const string Retried = "retried";

    /// <summary>Both answers were unusable; the lawyer's notes were inserted unchanged.</summary>
    public const string FellBackToNotes = "fellBackToNotes";
}
