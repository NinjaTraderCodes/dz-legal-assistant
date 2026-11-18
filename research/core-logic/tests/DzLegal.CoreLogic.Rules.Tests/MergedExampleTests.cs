using DzLegal.CoreLogic.Contracts;
using DzLegal.Domain;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>
/// Worked examples on a record whose imports are merged in (the owner, 18 November 2026): questions are checked strictly
/// for the claim's own items; a question only imported items ask is a note, not a difference.
/// </summary>
public sealed class MergedExampleTests
{
    private static readonly Func<string, bool> Imported = id => id.StartsWith("court-sections.", StringComparison.Ordinal);

    private static RuleOutcome Outcome(params (string Slot, string[] NeededBy)[] questions) => new()
    {
        Findings = [],
        Questions = [.. questions.Select(q => new SlotQuestion { Slot = q.Slot, NeededBy = q.NeededBy })],
    };

    private static Expectation Expect(params string[] questions) => new() { Findings = [], Questions = questions };

    [Fact]
    public void AQuestionOnlyImportedItemsAskIsANoteNotADifference()
    {
        var outcome = Outcome(("own_slot", ["d1"]), ("section_slot", ["court-sections.b4"]));
        var notes = new List<string>();
        Assert.Empty(WorkedExamples.Compare(outcome, "c", Expect("own_slot"), false, id => id, Imported, notes));
        Assert.Equal(["questions asked only by imported items, not listed: {section_slot}"], notes);
    }

    [Fact]
    public void AnOwnQuestionTheExampleDoesNotListFails()
    {
        var outcome = Outcome(("own_slot", ["d1"]), ("shared_slot", ["court-sections.b4", "n2"]));
        var differences = WorkedExamples.Compare(outcome, "c", Expect("own_slot"), false, id => id, Imported, []);
        Assert.Single(differences);
        Assert.Contains("not expected: {shared_slot}", differences[0], StringComparison.Ordinal);
    }

    [Fact]
    public void AnExpectedQuestionNobodyAsksFailsAndAListedImportedQuestionPasses()
    {
        var outcome = Outcome(("own_slot", ["d1"]), ("section_slot", ["court-sections.b4"]));
        Assert.Empty(WorkedExamples.Compare(outcome, "c", Expect("own_slot", "section_slot"), false, id => id, Imported, []));
        var differences = WorkedExamples.Compare(outcome, "c", Expect("own_slot", "ghost_slot"), false, id => id, Imported, []);
        Assert.Contains("expected and never asked: {ghost_slot}", Assert.Single(differences), StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutImportsTheQuestionsStayOneExactSet()
    {
        var outcome = Outcome(("own_slot", ["d1"]), ("other_slot", ["d2"]));
        Assert.Single(WorkedExamples.Compare(outcome, "c", Expect("own_slot"), false, id => id));
    }

    private static RuleFinding Finding(string id, FindingEffect declared, FindingEffect shown) => new()
    {
        Ref = id,
        RecordId = "c",
        Kind = FindingKind.PriorStep,
        DeclaredEffect = declared,
        Effect = shown,
        Grade = "B",
        ToCheck = false,
        Message = new LocalizedText("رسالة.", "Message."),
        Basis = [],
    };

    private static RuleOutcome WithFindings(params RuleFinding[] findings) => new() { Findings = findings, Questions = [] };

    private static ExpectedFinding Listed(string id, FindingEffect effect) => new() { Ref = id, Effect = effect };

    // The owner, 18 November 2026: "Any block that an example does not list fails it, on the merged record."
    [Fact]
    public void AnUnlistedBlockFailsTheExampleImportedOrNot()
    {
        var outcome = WithFindings(Finding("d1", FindingEffect.Warn, FindingEffect.Warn), Finding("court-sections.p1", FindingEffect.BlockCleanExport, FindingEffect.Warn));
        var expect = new Expectation { Findings = [Listed("d1", FindingEffect.Warn)] };
        var differences = WorkedExamples.Compare(outcome, "c", expect, true, id => id, Imported, [], unlistedBlockFails: true);
        Assert.Contains("court-sections.p1: blocks the clean export", Assert.Single(differences), StringComparison.Ordinal);
        var listed = new Expectation { Findings = [Listed("d1", FindingEffect.Warn), Listed("court-sections.p1", FindingEffect.BlockCleanExport)] };
        Assert.Empty(WorkedExamples.Compare(outcome, "c", listed, true, id => id, Imported, [], unlistedBlockFails: true));
    }

    [Fact]
    public void AnUnlistedShownBlockFailsAndUnlistedWarningsStayFree()
    {
        var shown = WithFindings(Finding("p2", FindingEffect.Warn, FindingEffect.BlockCleanExport), Finding("n1", FindingEffect.Warn, FindingEffect.Warn));
        var expect = new Expectation { Findings = [] };
        Assert.Single(WorkedExamples.Compare(shown, "c", expect, true, id => id, null, [], unlistedBlockFails: true));
        Assert.Empty(WorkedExamples.Compare(WithFindings(Finding("n1", FindingEffect.Warn, FindingEffect.Warn)), "c", expect, true, id => id, null, [], unlistedBlockFails: true));
        // Off (the --against comparison): an unlisted block stays free there.
        Assert.Empty(WorkedExamples.Compare(shown, "c", expect, true, id => id));
    }
}
