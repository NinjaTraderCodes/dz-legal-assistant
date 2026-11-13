using DzLegal.Domain;

namespace DzLegal.CoreLogic.Contracts;

public enum SlotType
{
    /// <summary>"yes", "no" or "unknown".</summary>
    YesNoUnknown,

    /// <summary>yyyy-mm-dd.</summary>
    Date,

    /// <summary>Digits only, in dinars.</summary>
    Amount,

    /// <summary>One of <see cref="SlotDefinition.Choices"/>.</summary>
    Choice,
}

public sealed record SlotChoice
{
    public required string Id { get; init; }
    public required LocalizedText Label { get; init; }
}

/// <summary>
/// One fact the rules test, written as the question the app asks the lawyer. The reader may propose an answer with the
/// words it comes from; the lawyer confirms or corrects it.
/// </summary>
public sealed record SlotDefinition
{
    public required string Id { get; init; }
    public required SlotType Type { get; init; }

    /// <summary>The question, in Arabic and English.</summary>
    public required LocalizedText Question { get; init; }
    public LocalizedText? Help { get; init; }
    public IReadOnlyList<SlotChoice> Choices { get; init; } = [];

    /// <summary>The claim types the question is asked for; "*" for all.</summary>
    public required IReadOnlyList<string> AskedFor { get; init; }

    /// <summary>A further test on other slots; null when the question is always asked for those claim types.</summary>
    public SlotCondition? AskedWhen { get; init; }

    /// <summary>The event kind of FactVocabulary whose polarity or date fills the slot when the reader finds it; null otherwise.</summary>
    public string? FromEvent { get; init; }

    /// <summary>True when the note reader proposes a value for this slot (step 4 measures it on hidden set v3).</summary>
    public bool ReadFromNote { get; init; }

    /// <summary>Where the slot comes from: "brief" for the brief's own list, or the id of the rule record that needs it.</summary>
    public required string Origin { get; init; }
}

public sealed record SlotCatalogue
{
    public required string SchemaVersion { get; init; }
    public required IReadOnlyList<SlotDefinition> Slots { get; init; }
}
