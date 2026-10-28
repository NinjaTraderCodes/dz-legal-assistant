using System.Text.Json;
using DzLegal.Drafting.Evidence;

namespace DzLegal.Drafting.Tests;

public sealed class EvidenceReferenceTests
{
    [Fact]
    public void TheHostResolvesSelectedValuesWithoutAskingTheModelToCountOffsets()
    {
        var snapshot = CaseSnapshot.Create("fictional", 0, new Dictionary<string, string> { ["notes.facts"] = "😀 بدأ الإيجار يوم ٠١/٠٣/٢٠٢٤ بمبلغ 35000 دج." });
        var references = new EvidenceReferences(snapshot);
        var date = Assert.Single(references.Sources.Where(s => s.Kind == "date"));
        var amount = Assert.Single(references.Sources.Where(s => s.Kind == "amount"));
        Assert.Equal("٠١/٠٣/٢٠٢٤", snapshot.Read(date.Span));
        Assert.Equal("35000 دج", snapshot.Read(amount.Span));
        var target = new CaseEvidence { SnapshotFingerprint = snapshot.Fingerprint, Events = [new("e1", "lease_started", "plaintiff", "affirmed", null, [], [new("date", "commencement_date", date.Span), new("amount", "rent", amount.Span)], [references.Sources[0].Span])], Coverage = [new("s1", "read")] };
        var wire = JsonSerializer.Serialize(references.Encode(target), EvidenceJson.Options);
        Assert.DoesNotContain("\"start\"", wire, StringComparison.Ordinal);
        Assert.DoesNotContain("35000", wire, StringComparison.Ordinal);
        var decoded = references.Decode(wire);
        Assert.Equal(date.Span, decoded.Events[0].Values[0].Source);
        Assert.True(EvidenceValidator.Check(snapshot, decoded).StructurallyValid);
    }

    [Fact]
    public void AnUnknownReferenceIsRejectedInsteadOfBeingReboundToTheNearestValue()
    {
        var snapshot = CaseSnapshot.Create("fictional", 0, new Dictionary<string, string> { ["notes.facts"] = "يوم 01/03/2024." });
        var references = new EvidenceReferences(snapshot);
        const string wire = """{"relations":[],"events":[{"id":"e1","kind":"lease_started","assertedBy":"plaintiff","polarity":"affirmed","disputeGroup":null,"participants":[],"values":[],"sources":["not-a-source"]}],"requests":[],"documents":[],"coverage":[{"sentenceId":"s1","status":"read"}],"unresolved":[]}""";
        Assert.Throws<JsonException>(() => references.Decode(wire));
    }

    [Fact]
    public void NoValueOrDocumentCandidatesMeansThoseOutputArraysMustStayEmpty()
    {
        var references = new EvidenceReferences(CaseSnapshot.Create("fictional", 0, new Dictionary<string, string> { ["notes.facts"] = "واقعة غير مؤرخة." }));
        var schema = references.Schema();
        Assert.Equal(0, schema.GetProperty("properties").GetProperty("documents").GetProperty("maxItems").GetInt32());
        var values = schema.GetProperty("properties").GetProperty("events").GetProperty("items").GetProperty("properties").GetProperty("values");
        Assert.Equal(0, values.GetProperty("maxItems").GetInt32());
    }

    [Fact]
    public void AYearShapedAmountWithCurrencyIsNotStolenByTheDateCandidates()
    {
        var snapshot = CaseSnapshot.Create("fictional", 0, new Dictionary<string, string> { ["notes.facts"] = "دفع 2000 دج سنة 2024 بتاريخ 01/03/2024." });
        var references = new EvidenceReferences(snapshot);
        Assert.Contains(references.Sources, s => s.Kind == "amount" && s.Text == "2000 دج");
        Assert.DoesNotContain(references.Sources, s => s.Kind == "date" && s.Text == "2000");
        Assert.Contains(references.Sources, s => s.Kind == "date" && s.Text == "2024");
        Assert.DoesNotContain(references.Sources, s => s.Kind == "amount" && s.Text.Contains("01/03", StringComparison.Ordinal));
    }
}
