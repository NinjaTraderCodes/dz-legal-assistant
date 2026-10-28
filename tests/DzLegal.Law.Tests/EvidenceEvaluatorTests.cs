using DzLegal.Drafting.Evidence;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class EvidenceEvaluatorTests
{
    private static (CaseSnapshot Snapshot, CaseEvidence Gold) Fixture()
    {
        var snapshot = CaseSnapshot.Create("fictional", 0, new Dictionary<string, string> { ["notes.facts"] = "وقع العقد يوم 15/02/2024. بدأ الإيجار يوم 01/03/2024." });
        var first = snapshot.Sentences[0];
        var last = snapshot.Sentences[1];
        var date = new EvidenceSpan("notes.facts", first.Text.IndexOf("15/02/2024", StringComparison.Ordinal), 10);
        var gold = new CaseEvidence
        {
            SnapshotFingerprint = snapshot.Fingerprint,
            Events = [new("signed", "contract_concluded", "plaintiff", "affirmed", null, [], [new("date", "signature_date", date)], [new(first.FieldId, first.Start, first.Text.Length)]),
                new("commenced", "lease_started", "plaintiff", "affirmed", null, [], [new("date", "commencement_date", new("notes.facts", last.Start + last.Text.IndexOf("01/03/2024", StringComparison.Ordinal), 10))], [new(last.FieldId, last.Start, last.Text.Length)])],
            Coverage = [new("s1", "read"), new("s2", "read")],
        };
        return (snapshot, gold);
    }

    [Fact]
    public void EventOrderAndModelAssignedIdsDoNotChangeTheAnswer()
    {
        var (snapshot, gold) = Fixture();
        var prediction = gold with { Events = [gold.Events[1] with { Id = "e9" }, gold.Events[0] with { Id = "e3" }] };
        var score = EvidenceEvaluator.Score(snapshot, gold, prediction, exhaustiveGold: true);
        Assert.Empty(score.Errors);
        Assert.True(score.Complete);
    }

    [Fact]
    public void ADateInTheInputCanStillBelongToTheWrongEvent()
    {
        var (snapshot, gold) = Fixture();
        var wrong = gold.Events[0] with { Values = [gold.Events[1].Values[0] with { Role = "signature_date" }] };
        var score = EvidenceEvaluator.Score(snapshot, gold, gold with { Events = [wrong, gold.Events[1]] }, exhaustiveGold: true);
        Assert.Contains(score.Errors, e => e is { Type: "value binding or ownership", Critical: true });
    }

    [Fact]
    public void ConflictingDuplicatesAndUnsupportedAdditionsCannotIncreaseAccuracy()
    {
        var (snapshot, gold) = Fixture();
        var prediction = gold with { Events = [.. gold.Events, gold.Events[0] with { Id = "duplicate", Polarity = "denied" }, gold.Events[0] with { Id = "invented", Kind = "death" }] };
        var score = EvidenceEvaluator.Score(snapshot, gold, prediction, exhaustiveGold: true);
        Assert.Contains(score.Errors, e => e is { Type: "conflicting duplicate assertion", Critical: true });
        Assert.Contains(score.Errors, e => e is { Type: "additional assertion", Critical: true });
        Assert.False(score.Complete);
    }
}
