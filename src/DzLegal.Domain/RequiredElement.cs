using System.Text.Json.Serialization;

namespace DzLegal.Domain;

public enum Severity
{
    /// <summary>Omission makes the filing inadmissible in form (irrecevable en la forme).</summary>
    Inadmissible,

    /// <summary>Recommended or "where applicable" element; omission is flagged but not fatal by itself.</summary>
    Warning,
}

/// <summary>
/// A legally mandated element of a document, traceable to the article it derives from.
/// </summary>
public sealed record RequiredElement
{
    public required string Id { get; init; }
    public required LocalizedText Label { get; init; }

    /// <summary>Human-readable legal reference, e.g. "CPCA art. 15-2".</summary>
    public required string Article { get; init; }

    public Severity Severity { get; init; } = Severity.Inadmissible;
    public required RuleCheck Check { get; init; }

    /// <summary>The element is required only when all conditions hold; otherwise it is not applicable.</summary>
    public IReadOnlyList<Condition> AppliesWhen { get; init; } = [];

    /// <summary>Message shown when the element is missing.</summary>
    public required LocalizedText MissingMessage { get; init; }

    /// <summary>False until a practicing lawyer has confirmed this element against the official text.</summary>
    public bool Verified { get; init; }

    /// <summary>Where the rule comes from and anything still to confirm.</summary>
    public string? SourceNote { get; init; }
}

/// <summary>
/// The closed set of deterministic checks the Form Engine understands.
/// Serialized with a "kind" discriminator.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(FieldsPresentCheck), "fieldsPresent")]
[JsonDerivedType(typeof(SectionNotEmptyCheck), "sectionNotEmpty")]
[JsonDerivedType(typeof(SectionMatchesCheck), "sectionMatches")]
[JsonDerivedType(typeof(SectionOrderCheck), "sectionOrder")]
[JsonDerivedType(typeof(PredominantlyArabicCheck), "predominantlyArabic")]
[JsonDerivedType(typeof(AllOfCheck), "allOf")]
[JsonDerivedType(typeof(AnyOfCheck), "anyOf")]
public abstract record RuleCheck;

/// <summary>Every listed input field has a non-blank value.</summary>
public sealed record FieldsPresentCheck(IReadOnlyList<string> Fields) : RuleCheck;

/// <summary>The section exists in the document and its text is not blank.</summary>
public sealed record SectionNotEmptyCheck(string Section) : RuleCheck;

/// <summary>The section's text matches a regular expression.</summary>
public sealed record SectionMatchesCheck(string Section, string Pattern) : RuleCheck;

/// <summary>The listed sections, where present, appear in this relative order.</summary>
public sealed record SectionOrderCheck(IReadOnlyList<string> Sections) : RuleCheck;

/// <summary>
/// The letters of the listed sections are predominantly Arabic script (at least <see cref="MinRatio"/>).
/// Latin names and French terms inline are tolerated below that threshold.
/// </summary>
public sealed record PredominantlyArabicCheck(IReadOnlyList<string> Sections, double MinRatio = 0.6) : RuleCheck;

public sealed record AllOfCheck(IReadOnlyList<RuleCheck> Checks) : RuleCheck;

public sealed record AnyOfCheck(IReadOnlyList<RuleCheck> Checks) : RuleCheck;
