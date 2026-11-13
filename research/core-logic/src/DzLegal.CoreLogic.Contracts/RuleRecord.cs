using System.Text.Json;
using DzLegal.Domain;

namespace DzLegal.CoreLogic.Contracts;

// The shapes below mirror docs/research/core-logic/rules/rule-record.schema.json, property for property.
// A test loads every example record through them with unknown members forbidden, so the two cannot drift apart.

public enum RecordKind
{
    Claim,
    Module,
}

public enum RecordStage
{
    DerivationA,
    DerivationB,
    DerivationC,
    Reconciled,
    Final,
}

/// <summary>Same values as <c>ArticleRole</c> in DzLegal.Law, so a statement maps onto a catalogue article without translation.</summary>
public enum StatementRole
{
    Basis,
    Proof,
    Damages,
    Procedure,
    Limit,
    Prerequisite,
}

public enum ChecklistSource
{
    DerivationA,
    DerivationB,
    Codex,

    /// <summary>A question taken from the app's own claim catalogue (data/law/claims): its articles and prerequisites are leads, not law.</summary>
    Catalogue,
    NovemberUnit,
    Adversary,
}

public enum ChecklistAnswer
{
    Applies,
    DoesNotApply,
    SearchedNotFound,
}

public enum ArticleStore
{
    Library,
    Corpus,
    Delta,
}

public enum CourtSection
{
    Civil,
    RealEstate,
    Family,
    Social,
    Commercial,
    Other,
}

public enum ExitDestination
{
    AdministrativeCourt,
    SpecialisedCommercialCourt,
    PaymentOrder,
    CriminalCourt,
    OtherSection,
    OutsideCatalogue,
}

public enum DeadlineKind
{
    Forfeiture,
    Limitation,
    Procedural,
}

public enum RaisedBy
{
    CourtOfOwnMotion,
    PleaOnly,
    Unclear,
}

/// <summary>How a period is counted; the meaning of each value is in the schema.</summary>
public enum CountingMethod
{
    ExcludeFirstAndLastDay,
    ExcludeFirstDay,
    Inclusive,
}

public enum PeriodUnit
{
    Day,
    Month,
    Year,
}

public enum PriorStepKind
{
    FormalNotice,
    LabourConciliation,
    CommercialConciliation,
    LandRegistryPublication,
    NoticeToQuit,
    ReconciliationAttempt,
    Mediation,
    Other,
}

public enum Burden
{
    Plaintiff,
    Defendant,
}

public enum ExampleAuthor
{
    DerivationA,
    DerivationB,
    DerivationC,
    BoundaryWriter,
    Reconciler,
    Verifier,
}

/// <summary>
/// What a finding does in the app. There is one blocking effect only, and it is the app's existing one: the clean export is
/// blocked and the export with the «مسودة» mark stays allowed (owner's decision of 12 November 2026). Never add a harder stop.
/// "To check" is not an effect: it is a mark on a warning (<see cref="Grading.ToCheck"/>, <c>RuleFinding.ToCheck</c>), so that
/// it can never be weaker than a warning (owner's decision of 13 November 2026).
/// </summary>
public enum FindingEffect
{
    BlockCleanExport,
    Warn,
    Ask,
    None,
}

/// <summary>The four grades of docs/research/core-logic/rules/GRADES.md. Kept as strings so that the JSON reads "A", not "a".</summary>
public static class Grades
{
    public const string A = "A";
    public const string B = "B";
    public const string C = "C";
    public const string D = "D";

    public static readonly IReadOnlyList<string> All = [A, B, C, D];
}

public sealed record Derivation
{
    public required string Role { get; init; }
    public required string Model { get; init; }

    /// <summary>yyyy-mm-dd.</summary>
    public required string Date { get; init; }
}

public sealed record ModuleImport
{
    public required string Module { get; init; }
    public required IReadOnlyList<string> Blocks { get; init; }
    public required LocalizedText How { get; init; }
}

public sealed record ChecklistItem
{
    public required string Id { get; init; }
    public required string Item { get; init; }
    public required IReadOnlyList<ChecklistSource> From { get; init; }
    public required ChecklistAnswer Answer { get; init; }
    public IReadOnlyList<string> Refs { get; init; } = [];
    public string? Note { get; init; }
}

/// <summary>A pointer to a text of the article store. Agents write pointers; a script writes the words.</summary>
public sealed record Cite
{
    public required string Code { get; init; }

    /// <summary>Canonical number, as <c>ArticleNumbers.Canonical</c> writes it: "504", "187 مكرر", "73-4".</summary>
    public required string Article { get; init; }

    /// <summary>1-based numbers of the paragraphs cited. Empty: the whole article.</summary>
    public IReadOnlyList<int> Paragraphs { get; init; } = [];

    /// <summary>The publication id of the version cited; null for the version in force.</summary>
    public string? Version { get; init; }
}

public sealed record QuotedParagraph
{
    public required int Paragraph { get; init; }
    public required string Text { get; init; }
    public required string Sha256 { get; init; }
}

public sealed record Provenance
{
    /// <summary>1: Official Journal page. 2: official consolidated text. 3: anything else (a lead only).</summary>
    public required int Tier { get; init; }
    public required ArticleStore Store { get; init; }
    public string? TextAsOf { get; init; }
    public IReadOnlyList<string> LedgerRefs { get; init; } = [];
    public bool? LedgerClean { get; init; }
}

public sealed record Grading
{
    public required string Grade { get; init; }
    public required IReadOnlyList<string> Reasons { get; init; }

    /// <summary>
    /// True exactly when the grade is D: the item rests on no official text, or on a text not yet verified. It still warns,
    /// and the warning carries the mark "to check". It never blocks the clean export and is never silent.
    /// </summary>
    public required bool ToCheck { get; init; }
}

/// <summary>One way of reading an unclear text. An open point keeps every reading, not only the one chosen.</summary>
public sealed record LegalReading
{
    public required LocalizedText Reading { get; init; }
    public IReadOnlyList<string> Basis { get; init; } = [];
}

public sealed record Statement
{
    public required string Id { get; init; }
    public required StatementRole Role { get; init; }
    public required Cite Cite { get; init; }
    public required LocalizedText Says { get; init; }
    public bool Required { get; init; }
    public bool Interpretive { get; init; }
    public string? InterpretiveNote { get; init; }

    /// <summary>Written by the quoting script, never by an agent.</summary>
    public IReadOnlyList<QuotedParagraph> Quote { get; init; } = [];
    public Provenance? Source { get; init; }
    public Grading? Grading { get; init; }
}

/// <summary>
/// A test on the case's slots. Exactly one of <see cref="All"/>, <see cref="Any"/>, <see cref="Not"/> or <see cref="Slot"/> is set.
/// A slot that is not known makes the test unknown, never false: the evaluator then asks (specs/rule-evaluator.md).
/// </summary>
public sealed record SlotCondition
{
    public IReadOnlyList<SlotCondition>? All { get; init; }
    public IReadOnlyList<SlotCondition>? Any { get; init; }
    public SlotCondition? Not { get; init; }
    public string? Slot { get; init; }
    public string? Is { get; init; }
    public IReadOnlyList<string>? In { get; init; }
    public string? Includes { get; init; }
    public string? Before { get; init; }
    public string? After { get; init; }
    public bool? Known { get; init; }
}

public sealed record ProofMeans
{
    public required string Means { get; init; }
    public IReadOnlyList<string> Basis { get; init; } = [];
    public LocalizedText? Note { get; init; }
}

public sealed record ClaimElement
{
    public required string Id { get; init; }
    public required LocalizedText ToProve { get; init; }
    public IReadOnlyList<LocalizedText> FactsToState { get; init; } = [];
    public IReadOnlyList<string> Events { get; init; } = [];
    public IReadOnlyList<string> Slots { get; init; } = [];
    public Burden? Burden { get; init; }
    public IReadOnlyList<ProofMeans> Proof { get; init; } = [];
    public required IReadOnlyList<string> Basis { get; init; }
    public Grading? Grading { get; init; }
}

public sealed record UsualRequest
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required bool Accessory { get; init; }
    public required LocalizedText Text { get; init; }
    public bool Amount { get; init; }
    public SlotCondition? When { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
    public Grading? Grading { get; init; }
}

public sealed record SectionAlternative
{
    public required SlotCondition When { get; init; }
    public required CourtSection Value { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
}

public sealed record SectionRule
{
    public required CourtSection Value { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
    public IReadOnlyList<SectionAlternative> Alternatives { get; init; } = [];
    public required LocalizedText Message { get; init; }
    public bool Interpretive { get; init; }
    public string? InterpretiveNote { get; init; }
    public Grading? Grading { get; init; }
}

public sealed record TerritoryRule
{
    public required string Id { get; init; }
    public required LocalizedText Rule { get; init; }
    public SlotCondition? When { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
    public Grading? Grading { get; init; }
}

public sealed record CourtExit
{
    public required string Id { get; init; }
    public required SlotCondition When { get; init; }
    public required ExitDestination GoesTo { get; init; }
    public required FindingEffect Effect { get; init; }
    public required LocalizedText Message { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
    public bool Interpretive { get; init; }
    public string? InterpretiveNote { get; init; }
    public Grading? Grading { get; init; }
}

public sealed record CourtRules
{
    public SectionRule? Section { get; init; }
    public IReadOnlyList<TerritoryRule> Territory { get; init; } = [];
    public IReadOnlyList<CourtExit> Exits { get; init; } = [];
}

public sealed record Period
{
    public required int Value { get; init; }
    public required PeriodUnit Unit { get; init; }
}

public sealed record PeriodStart
{
    public required string Slot { get; init; }
    public string? Event { get; init; }
    public required LocalizedText Description { get; init; }
}

/// <summary>The exact words of the quoted text that carry a value; a script checks them against the structured field.</summary>
public sealed record ValueExtract
{
    public string? Period { get; init; }
    public string? Sanction { get; init; }
}

/// <summary>The counting rule of a deadline, in a form the evaluator can run, with the statements it comes from.</summary>
public sealed record CountingRule
{
    public required CountingMethod Method { get; init; }
    public bool ExtendToWorkingDay { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
}

public sealed record Deadline
{
    public required string Id { get; init; }
    public required DeadlineKind Kind { get; init; }
    public required Period Period { get; init; }
    public required PeriodStart StartsFrom { get; init; }

    /// <summary>Null when the texts do not settle how the period is counted; the deadline is then interpretive.</summary>
    public CountingRule? Counting { get; init; }
    public required RaisedBy RaisedBy { get; init; }
    public SlotCondition? When { get; init; }

    /// <summary><see cref="FindingEffect.BlockCleanExport"/> only for a forfeiture graded A; the evaluator lowers it to a warning otherwise.</summary>
    public required FindingEffect EffectWhenExpired { get; init; }
    public int? WarnBandDays { get; init; }
    public required LocalizedText Message { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
    public ValueExtract? Extract { get; init; }
    public bool Interpretive { get; init; }
    public string? InterpretiveNote { get; init; }
    public IReadOnlyList<LegalReading> Readings { get; init; } = [];
    public Grading? Grading { get; init; }
}

public sealed record StepExemption
{
    public required SlotCondition When { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
    public LocalizedText? Note { get; init; }
}

public sealed record AcceptedMeans
{
    public required string Id { get; init; }
    public required LocalizedText Label { get; init; }
    public IReadOnlyList<string> Basis { get; init; } = [];
}

public sealed record PriorStep
{
    public required string Id { get; init; }
    public required PriorStepKind Kind { get; init; }
    public SlotCondition? RequiredWhen { get; init; }
    public IReadOnlyList<StepExemption> NotRequiredWhen { get; init; } = [];
    public required IReadOnlyList<string> Slots { get; init; }
    public IReadOnlyList<AcceptedMeans> AcceptedMeans { get; init; } = [];
    public required FindingEffect EffectWhenMissing { get; init; }
    public required FindingEffect EffectWhenUnknown { get; init; }
    public required LocalizedText Message { get; init; }
    public required IReadOnlyList<string> Basis { get; init; }
    public bool Interpretive { get; init; }
    public string? InterpretiveNote { get; init; }
    public Grading? Grading { get; init; }
}

public sealed record RequiredDocument
{
    public required string Id { get; init; }
    public required LocalizedText Name { get; init; }
    public SlotCondition? RequiredWhen { get; init; }
    public required LocalizedText Why { get; init; }
    public IReadOnlyList<string> Basis { get; init; } = [];
    public Grading? Grading { get; init; }
}

public sealed record Boundary
{
    public required string Id { get; init; }
    public required LocalizedText LooksLike { get; init; }

    /// <summary>"claim:&lt;id&gt;" for another claim type of the catalogue, or "outside:&lt;short kind&gt;".</summary>
    public required string IsActually { get; init; }
    public required IReadOnlyList<LocalizedText> TellingFacts { get; init; }
    public SlotCondition? When { get; init; }
    public IReadOnlyList<string> Slots { get; init; } = [];
    public required LocalizedText Message { get; init; }
    public IReadOnlyList<string> Basis { get; init; } = [];
    public Grading? Grading { get; init; }
}

public sealed record ExpectedFinding
{
    public required string Ref { get; init; }

    /// <summary>The item's declared effect, before the grade.</summary>
    public required FindingEffect Effect { get; init; }

    /// <summary>Written by the grading script in a final record: what the app shows once the grade is applied.</summary>
    public FindingEffect? ShownEffect { get; init; }

    /// <summary>Written by the grading script in a final record: true when the item is grade D.</summary>
    public bool? ToCheck { get; init; }
}

public sealed record Expectation
{
    public CourtSection? Section { get; init; }
    public required IReadOnlyList<ExpectedFinding> Findings { get; init; }

    /// <summary>
    /// The slots the evaluator must ask for, compared as a set. Null when the example does not state them (not checked);
    /// empty when nothing may be asked. The two must stay apart, so this is not defaulted to an empty list.
    /// </summary>
    public IReadOnlyList<string>? Questions { get; init; }
}

public sealed record WorkedExample
{
    public required string Id { get; init; }
    public required ExampleAuthor By { get; init; }
    public required string Title { get; init; }

    /// <summary>yyyy-mm-dd.</summary>
    public required string PetitionDate { get; init; }

    /// <summary>Slot id to value: "yes", "no", "unknown", a date yyyy-mm-dd, digits for an amount, a choice id; "requests" is a list.</summary>
    public required IReadOnlyDictionary<string, JsonElement> Slots { get; init; }
    public required Expectation Expect { get; init; }
}

public sealed record OpenPoint
{
    public required string Id { get; init; }
    public required LocalizedText Question { get; init; }
    public required LocalizedText Why { get; init; }
    public IReadOnlyList<LegalReading> Readings { get; init; } = [];
    public required LocalizedText ProtectiveReading { get; init; }
    public required string Grade { get; init; }
    public IReadOnlyList<string> Refs { get; init; } = [];
}

/// <summary>A slot a derivation needs that slots.json does not have yet. A final record has none.</summary>
public sealed record ProposedSlot
{
    public required string Id { get; init; }
    public required SlotType Type { get; init; }
    public required LocalizedText Question { get; init; }
    public IReadOnlyList<SlotChoice> Choices { get; init; } = [];
    public required string Why { get; init; }
}

public sealed record RecordHistory
{
    public string? Reconciliation { get; init; }
    public string? Attacks { get; init; }
    public string? AgreementBeforeReconciliation { get; init; }
}

/// <summary>One rule record: a claim type of data/law/claims, or a shared module.</summary>
public sealed record RuleRecord
{
    public required string SchemaVersion { get; init; }
    public required RecordKind Kind { get; init; }
    public required string Id { get; init; }
    public required LocalizedText Label { get; init; }

    /// <summary>Always false: no lawyer has verified any record.</summary>
    public required bool Verified { get; init; }
    public required RecordStage Stage { get; init; }
    public required Derivation DerivedBy { get; init; }
    public string? Area { get; init; }
    public IReadOnlyList<string> AppliesTo { get; init; } = [];
    public IReadOnlyList<ModuleImport> Imports { get; init; } = [];
    public IReadOnlyList<ChecklistItem> Checklist { get; init; } = [];
    public required IReadOnlyList<Statement> Basis { get; init; }
    public IReadOnlyList<ClaimElement> Elements { get; init; } = [];
    public IReadOnlyList<UsualRequest> Requests { get; init; } = [];
    public CourtRules? Court { get; init; }
    public IReadOnlyList<Deadline> Deadlines { get; init; } = [];
    public IReadOnlyList<PriorStep> PriorSteps { get; init; } = [];
    public IReadOnlyList<RequiredDocument> Documents { get; init; } = [];
    public IReadOnlyList<Boundary> Boundaries { get; init; } = [];
    public IReadOnlyList<string> Slots { get; init; } = [];
    public IReadOnlyList<ProposedSlot> ProposedSlots { get; init; } = [];
    public IReadOnlyList<WorkedExample> Examples { get; init; } = [];
    public IReadOnlyList<OpenPoint> OpenPoints { get; init; } = [];
    public RecordHistory? History { get; init; }
}
