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
        Sections = Sections.Select(s => new DraftSection { Id = s.Id, Text = s.Text }).ToList(),
    };
}

public sealed class DraftSection
{
    public required string Id { get; init; }
    public string Text { get; set; } = string.Empty;
}
