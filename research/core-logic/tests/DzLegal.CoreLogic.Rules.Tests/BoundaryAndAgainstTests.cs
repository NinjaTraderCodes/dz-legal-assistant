using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>
/// <c>rules examples --boundary</c> and <c>--against</c>, on the example module and on altered copies of it. The cases are
/// fictitious and written here; the module's deadline d1 runs six months from the record's delivery, counted without the
/// first and last day and extended past the placeholder rest days.
/// </summary>
public sealed class BoundaryAndAgainstTests
{
    private static readonly RuleRecord Module = Build.Load(Build.ModuleJson);

    [Theory]
    [InlineData("[cpca 504 ¶1]", "late", "2025-06-01", "2026-03-01", "2025-12-02")]
    [InlineData("[cpca 504 ¶1]", "late", "2025-06-01", "2025-12-10", "2025-12-02")] // late but inside the band: a warning
    [InlineData("[cpca 504 ¶1]", "lastDay", "2025-06-01", "2025-12-02", "2025-12-02")]
    [InlineData("[cpca 504 ¶1]", "inTime", "2026-01-10", "2026-03-01", null)]
    [InlineData("[cpca 504 ¶1]", "unsettled", "2025-06-01", "2025-12-01", null)]
    [InlineData("[cpca 405 ¶4]", "lastDay", "2025-06-01", "2025-12-02", "2025-12-02")]
    [InlineData("[cpca 504]", "late", "2025-06-01", "2026-03-01", null)]
    public void ADeadlineCaseMatchesTheDeadlineByItsParagraph(string about, string outcome, string delivered, string petition, string? lastDay)
    {
        var result = Single(Case(about, outcome, petition, $"\"nonconciliation_record\": \"yes\", \"worker_is_public_servant\": \"no\", \"nonconciliation_record_delivered_on\": \"{delivered}\"", lastDay));
        Assert.True(result.Passed, string.Join("; ", result.Differences));
    }

    [Theory]
    [InlineData("[cpca 504 ¶1]", "inTime", "2026-03-01", null, "the outcome InTime needs none")]
    [InlineData("[cpca 504 ¶1]", "late", "2026-03-01", "2025-12-03", "the case says 2025-12-03")]
    [InlineData("[cpca 504 ¶1]", "unsettled", "2025-07-01", null, "must not be silent")]
    [InlineData("[cpca 504 ¶1]", "unsettled", "2026-07-01", null, "must not block")]
    [InlineData("[cpca 999 ¶1]", "late", "2026-03-01", null, "matches no item")]
    [InlineData("[labour-disputes 19 ¶1]", "late", "2026-03-01", null, "matches no Deadline")]
    [InlineData("cpca 504", "late", "2026-03-01", null, "is not written")]
    public void ACaseTheRecordDecidesOtherwiseIsADifference(string about, string outcome, string petition, string? lastDay, string difference)
    {
        var result = Single(Case(about, outcome, petition, "\"nonconciliation_record\": \"yes\", \"worker_is_public_servant\": \"no\", \"nonconciliation_record_delivered_on\": \"2025-06-01\"", lastDay));
        Assert.Contains(result.Differences, text => text.Contains(difference, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("[labour-disputes 19 ¶1]", "stepMissing", "\"nonconciliation_record\": \"no\", \"defendant_resides_abroad\": \"no\", \"employer_in_bankruptcy_or_settlement\": \"no\", \"worker_is_public_servant\": \"no\"")]
    [InlineData("[labour-disputes 19 ¶2]", "stepExempt", "\"nonconciliation_record\": \"no\", \"defendant_resides_abroad\": \"yes\", \"worker_is_public_servant\": \"no\"")] // through the exemption's basis
    [InlineData("[labour-disputes 19 ¶3]", "leavesOrdinaryCourt", "\"worker_is_public_servant\": \"yes\"")]
    [InlineData("[labour-disputes 19 ¶3]", "ask", "")]
    public void StepExitAndQuestionCases(string about, string outcome, string slots)
    {
        var result = Single(Case(about, outcome, "2026-03-01", slots, null));
        Assert.True(result.Passed, string.Join("; ", result.Differences));
    }

    [Fact]
    public void ACountingParagraphMatchesTheDeadlineThroughItsCountingBasis()
    {
        var record = Build.Load(Build.Edit(Build.ModuleJson, root => root.Item("deadlines", "d1")["basis"] = new JsonArray("b7")));
        Assert.Equal([(FindingKind.Deadline, "d1")], BoundaryCases.Match(record, ParagraphRef.Parse("[cpca 405 ¶4]")!));
    }

    [Fact]
    public void TwoRecordsThatCountDifferentlyDifferOnTheSameCaseEvenInsideTheBand()
    {
        var other = Build.Load(Build.Edit(Build.ModuleJson, root => root.Item("deadlines", "d1")["counting"]!["method"] = "excludeFirstDay"));
        var cases = BoundaryCases.Parse(Case("[cpca 504 ¶1]", "late", "2025-12-05", "\"nonconciliation_record\": \"yes\", \"worker_is_public_servant\": \"no\", \"nonconciliation_record_delivered_on\": \"2025-06-01\"", null));

        var same = BoundaryCases.Run(new RuleEvaluator(), [Module, Module], cases);
        Assert.All(same, result => Assert.True(result.Passed));

        var results = BoundaryCases.Run(new RuleEvaluator(), [Module, other], cases);
        Assert.Equal(3, results.Count);
        Assert.True(results[0].Passed);
        Assert.True(results[1].Passed); // both warn: the difference is in the last day only
        Assert.Equal(2, results[2].Differences.Count);
    }

    [Fact]
    public void ACaseFileMayCarryAFieldNoRecordHasButNotAnUnknownOutcome()
    {
        Assert.Single(BoundaryCases.Parse("""[ { "id": "c1", "petitionDate": "2026-03-01", "slots": {}, "expect": { "about": "[cpca 504 ¶1]", "outcome": "late" }, "note": "kept" } ]"""));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => BoundaryCases.Parse("""[ { "id": "c1", "petitionDate": "2026-03-01", "slots": {}, "expect": { "about": "[cpca 504 ¶1]", "outcome": "soon" } } ]"""));
    }

    [Theory]
    [InlineData("[cpca 504 ¶1]", "cpca", "504", 1)]
    [InlineData("[commercial-code 187 مكرر ¶2]", "commercial-code", "187 مكرر", 2)]
    [InlineData("[cpca 536 مكرر 4]", "cpca", "536 مكرر 4", null)]
    public void AParagraphIsReadAsTheTextsLabelIt(string about, string code, string article, int? paragraph) =>
        Assert.Equal(new ParagraphRef(code, article, paragraph), ParagraphRef.Parse(about));

    [Fact]
    public void TheExamplesOfOneDerivationRunOnTheOtherThroughTheIdMap()
    {
        var a = Build.Load(Build.Edit(Build.ModuleJson, root => root["stage"] = "derivationA"));
        var b = Build.Load(Renamed(Build.ModuleJson, stage: "derivationB"));
        var map = IdMap.Parse("""
            [ { "kind": "deadline", "a": "d1", "b": "dl1", "reconciled": null },
              { "kind": "priorStep", "a": "p1", "b": "ps1", "reconciled": null },
              { "kind": "exit", "a": "x1", "b": "xt1", "reconciled": null },
              { "kind": "boundary", "a": "n1", "b": "nb1", "reconciled": null },
              { "kind": "document", "a": "doc1", "b": "dc1", "reconciled": null } ]
            """);

        Assert.All(WorkedExamples.RunAgainst(new RuleEvaluator(), a, b, map), result => Assert.True(result.Passed, string.Join("; ", result.Differences)));

        // Seven months instead of six: ex2 is late either way, but the last day differs, and that is a difference.
        var longer = Build.Load(Renamed(Build.Edit(Build.ModuleJson, root => root.Item("deadlines", "d1")["period"]!["value"] = 7), stage: "derivationB"));
        var results = WorkedExamples.RunAgainst(new RuleEvaluator(), a, longer, map);
        Assert.Contains(results.Single(result => result.CaseId == "ex2").Differences, text => text.StartsWith("d1 -> dl1: last day 2025-12-02", StringComparison.Ordinal));

        var partial = IdMap.Parse("""[ { "kind": "deadline", "a": "d1", "b": "dl1", "reconciled": null } ]""");
        Assert.Contains(WorkedExamples.RunAgainst(new RuleEvaluator(), a, b, partial).Single(result => result.CaseId == "ex1").Differences,
            text => text == "p1: no counterpart in the id map");
    }

    [Fact]
    public void ARecordOfStageCHasNoColumnInTheIdMap() =>
        Assert.Throws<ArgumentException>(() => IdMap.RoleOf(Build.Record() with { Stage = RecordStage.DerivationC }));

    private static string Renamed(string json, string stage) => Build.Edit(json, root =>
    {
        root["stage"] = stage;
        foreach (var (list, from, to) in new[] { ("deadlines", "d1", "dl1"), ("priorSteps", "p1", "ps1"), ("boundaries", "n1", "nb1"), ("documents", "doc1", "dc1") })
        {
            root.Item(list, from)["id"] = to;
        }

        root["court"]!["exits"]![0]!["id"] = "xt1";
    });

    private static string Case(string about, string outcome, string petition, string slots, string? lastDay) =>
        $$"""[ { "id": "c1", "title": "case", "petitionDate": "{{petition}}", "slots": { {{slots}} }, "expect": { "about": "{{about}}", "outcome": "{{outcome}}"{{(lastDay is null ? string.Empty : $", \"lastDay\": \"{lastDay}\"")}}, "why": "test" } } ]""";

    private static CaseResult Single(string cases) => Assert.Single(BoundaryCases.Run(new RuleEvaluator(), [Module], BoundaryCases.Parse(cases)));
}
