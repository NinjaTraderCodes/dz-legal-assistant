using DzLegal.Domain;

namespace DzLegal.CoreLogic.Contracts;

/// <summary>How sure a slot's value is. Only <see cref="Confirmed"/> values may produce a blocking finding.</summary>
public enum SlotOrigin
{
    /// <summary>Typed or confirmed by the lawyer.</summary>
    Confirmed,

    /// <summary>Proposed by the note reader and not yet confirmed by the lawyer.</summary>
    Proposed,
}

/// <summary>One answer about the case: "yes", "no", "unknown", a date yyyy-mm-dd, digits for an amount, or a choice id.</summary>
public sealed record SlotValue
{
    public required string Slot { get; init; }
    public required string Value { get; init; }
    public SlotOrigin Origin { get; init; } = SlotOrigin.Confirmed;
}

/// <summary>
/// Everything the rules may read about one case. A slot that is absent from <see cref="Values"/> has not been answered:
/// the evaluator treats it as unknown and returns a question for it.
/// </summary>
public sealed record CaseSlots
{
    /// <summary>The claim types the lawyer confirmed. Rules never run on a type that is only proposed.</summary>
    public required IReadOnlyList<string> ClaimIds { get; init; }
    public IReadOnlyList<SlotValue> Values { get; init; } = [];

    /// <summary>The request kinds the petition asks (FactVocabulary.Requests).</summary>
    public IReadOnlyList<string> Requests { get; init; } = [];

    /// <summary>The section the lawyer wrote, mapped to the catalogue's sections; null when not given or not recognised.</summary>
    public CourtSection? Section { get; init; }
}

public enum FindingKind
{
    Deadline,
    PriorStep,
    CourtSection,
    Exit,
    Document,
    Boundary,
}

/// <summary>One thing the rules have to say about the case.</summary>
public sealed record RuleFinding
{
    /// <summary>The id of the record's item that produced it (for example "labour-prelitigation.d1").</summary>
    public required string Ref { get; init; }
    public required string RecordId { get; init; }
    public required FindingKind Kind { get; init; }

    /// <summary>What the record's item says should happen, before the grade is looked at. Worked examples state this one.</summary>
    public required FindingEffect DeclaredEffect { get; init; }

    /// <summary>
    /// What the app does: <see cref="DeclaredEffect"/> lowered by the grade (rules/GRADES.md) and by the origin of the slots
    /// (a value the lawyer has not confirmed never blocks).
    /// </summary>
    public required FindingEffect Effect { get; init; }

    /// <summary>"A" to "D"; "D" when the item carries no grading yet.</summary>
    public required string Grade { get; init; }

    /// <summary>
    /// True for a grade D item: the app shows the finding as a warning with the mark "to check". Never weaker than a warning:
    /// a finding with this mark has <see cref="Effect"/> = <see cref="FindingEffect.Warn"/> wherever its item declares a warning
    /// or a block.
    /// </summary>
    public required bool ToCheck { get; init; }

    /// <summary>The text for the lawyer, from the record. Arabic and English, like every text that lives in data.</summary>
    public required LocalizedText Message { get; init; }

    /// <summary>The texts the finding rests on, to be shown with it.</summary>
    public required IReadOnlyList<Cite> Basis { get; init; }
    public IReadOnlyList<string> SlotsUsed { get; init; } = [];

    /// <summary>For a deadline: its last day, counted by the periods module.</summary>
    public DateOnly? LastDay { get; init; }

    /// <summary>For a deadline: the days left on the petition's date (negative when it has passed).</summary>
    public int? DaysLeft { get; init; }

    /// <summary>Why the effect is what it is, in English, for logs and tests; never shown to the lawyer.</summary>
    public string? Trace { get; init; }
}

/// <summary>A slot the rules need and do not have. The app asks the lawyer the slot's question (slots.json).</summary>
public sealed record SlotQuestion
{
    public required string Slot { get; init; }

    /// <summary>The ids of the record's items that wait for the answer.</summary>
    public required IReadOnlyList<string> NeededBy { get; init; }
}

public sealed record SectionProposal
{
    public required CourtSection Section { get; init; }
    public required string Grade { get; init; }
    public required IReadOnlyList<Cite> Basis { get; init; }
}

public sealed record RuleOutcome
{
    public required IReadOnlyList<RuleFinding> Findings { get; init; }
    public required IReadOnlyList<SlotQuestion> Questions { get; init; }
    public SectionProposal? Section { get; init; }
}

/// <summary>
/// Applies rule records to one case. A pure function: no file, no clock, no network; the same inputs give the same outcome.
/// It is the single implementation of the rules: the note sampler, the tests and the app all call it.
/// </summary>
public interface IRuleEvaluator
{
    /// <param name="records">The final records of the confirmed claim types, with their modules already copied in.</param>
    /// <param name="slots">The answers about the case.</param>
    /// <param name="petitionDate">The date written on the petition (the form's document.date).</param>
    RuleOutcome Evaluate(IReadOnlyList<RuleRecord> records, CaseSlots slots, DateOnly petitionDate);
}
