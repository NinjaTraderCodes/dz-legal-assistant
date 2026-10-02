namespace DzLegal.Domain;

/// <summary>
/// Everything that defines one document type (e.g. the requête introductive d'instance):
/// the input fields the lawyer fills, the sections of the document, and the legally
/// required elements the Form Engine checks. Loaded from data, never hardcoded.
/// </summary>
public sealed record DocumentTypeDefinition
{
    public required string Id { get; init; }
    public required string Version { get; init; }
    public required LocalizedText Name { get; init; }
    public LocalizedText? Description { get; init; }

    /// <summary>System prompt file (relative to the definition folder) shared by all narrative sections.</summary>
    public string? SystemPrompt { get; init; }
    public IReadOnlyList<FieldGroupDefinition> FieldGroups { get; init; } = [];
    public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];
    public IReadOnlyList<SectionDefinition> Sections { get; init; } = [];
    public IReadOnlyList<RequiredElement> Elements { get; init; } = [];

    /// <summary>Exact text fixes applied to model-written text only (e.g. small-model grammar slips).</summary>
    public IReadOnlyList<TextCorrection> Corrections { get; init; } = [];

    /// <summary>
    /// Legal qualifications (contract types, liability regimes…) that model-written text may use only when the
    /// lawyer's own inputs contain them; otherwise they are flagged as possibly invented.
    /// </summary>
    public IReadOnlyList<string> GroundedTerms { get; init; } = [];

    /// <summary>Folder the definition was loaded from; templates and prompts resolve against it.</summary>
    public string SourceDirectory { get; init; } = string.Empty;

    public FieldDefinition? FindField(string id) => Fields.FirstOrDefault(f => f.Id == id);

    public SectionDefinition? FindSection(string id) => Sections.FirstOrDefault(s => s.Id == id);
}

public sealed record FieldGroupDefinition(string Id, LocalizedText Label);

/// <summary>An exact find/replace fix, e.g. «إن المادتان» → «إن المادتين» (the noun after «إنّ» is accusative).</summary>
public sealed record TextCorrection(string Find, string Replace);

public enum FieldKind
{
    Text,
    MultilineText,
    Boolean,
    Date,

    /// <summary>One value out of <see cref="FieldDefinition.Options"/>.</summary>
    Choice,
}

/// <summary>One allowed value of a <see cref="FieldKind.Choice"/> field.</summary>
public sealed record FieldOption(string Value, LocalizedText Label);

/// <summary>One input the lawyer provides. Values are stored as strings keyed by <see cref="Id"/>.</summary>
public sealed record FieldDefinition
{
    public required string Id { get; init; }
    public required LocalizedText Label { get; init; }
    public FieldKind Kind { get; init; } = FieldKind.Text;
    public string? Group { get; init; }
    public LocalizedText? Hint { get; init; }

    /// <summary>Initial value. For <see cref="FieldKind.Date"/> the literal "today" means the current date.</summary>
    public string? Default { get; init; }

    /// <summary>The field is shown only when all conditions hold.</summary>
    public IReadOnlyList<Condition> VisibleWhen { get; init; } = [];

    /// <summary>Allowed values of a <see cref="FieldKind.Choice"/> field.</summary>
    public IReadOnlyList<FieldOption> Options { get; init; } = [];
}

public enum SectionKind
{
    /// <summary>Rendered deterministically from a template and the case inputs. Edited through the form.</summary>
    Generated,

    /// <summary>Written by the drafting model from the lawyer's notes. Freely editable.</summary>
    Narrative,
}

public sealed record SectionDefinition
{
    public required string Id { get; init; }
    public required LocalizedText Title { get; init; }
    public SectionKind Kind { get; init; } = SectionKind.Generated;

    /// <summary>Whether the section title is printed as a heading in the document.</summary>
    public bool ShowTitle { get; init; }

    /// <summary>Template file (relative to the definition folder). Used by generated sections.</summary>
    public string? Template { get; init; }

    /// <summary>Prompt file (relative to the definition folder). Used by narrative sections.</summary>
    public string? Prompt { get; init; }

    /// <summary>Field holding the lawyer's notes that a narrative section is grounded on.</summary>
    public string? SourceField { get; init; }

    /// <summary>The section is included only when all conditions hold.</summary>
    public IReadOnlyList<Condition> IncludeWhen { get; init; } = [];

    /// <summary>Review hints shown under the section while their conditions hold (not legal findings).</summary>
    public IReadOnlyList<SectionHint> Hints { get; init; } = [];
}

/// <summary>A drafting hint, e.g. "choose the plaintiff's gender so the wording agrees".</summary>
public sealed record SectionHint
{
    public IReadOnlyList<Condition> When { get; init; } = [];
    public required LocalizedText Message { get; init; }
}

/// <summary>
/// A simple condition on a field value. Exactly one of <see cref="Is"/> / <see cref="IsNot"/> is set.
/// Missing values compare as the empty string.
/// </summary>
public sealed record Condition
{
    public required string Field { get; init; }
    public string? Is { get; init; }
    public string? IsNot { get; init; }

    public bool Evaluate(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var actual = values.TryGetValue(Field, out var v) ? v.Trim() : string.Empty;
        if (Is is not null)
        {
            return string.Equals(actual, Is, StringComparison.OrdinalIgnoreCase);
        }

        return IsNot is null || !string.Equals(actual, IsNot, StringComparison.OrdinalIgnoreCase);
    }

    public static bool All(IReadOnlyList<Condition> conditions, IReadOnlyDictionary<string, string> values) =>
        conditions.All(c => c.Evaluate(values));
}
