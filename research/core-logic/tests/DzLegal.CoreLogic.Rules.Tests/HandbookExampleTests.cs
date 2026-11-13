using System.Globalization;
using System.Text.Json;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Contracts.Tests;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>Both handbook examples through the evaluator, and determinism.</summary>
public sealed class HandbookExampleTests
{
    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void EveryWorkedExampleGivesWhatItStates(string name)
    {
        var record = HandbookFiles.Example(name);
        var results = WorkedExamples.Run(new RuleEvaluator(), record);
        Assert.Equal(record.Examples.Count, results.Count);
        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.True(result.Passed, $"{result.CaseId}: {string.Join("; ", result.Differences)}"));
    }

    [Fact]
    public void AnExampleThatStatesSomethingElseIsReported()
    {
        var json = Build.Edit(Build.ModuleJson, root =>
        {
            var ex2 = root["examples"]!.AsArray().Single(example => example!["id"]!.GetValue<string>() == "ex2")!;
            ex2["expect"]!["findings"]![0]!["effect"] = "warn";
            ex2["expect"]!["questions"] = new System.Text.Json.Nodes.JsonArray("nonconciliation_record");
            ex2["expect"]!["section"] = "civil";
        });

        var result = WorkedExamples.Run(new RuleEvaluator(), Build.Load(json)).Single(result => result.CaseId == "ex2");
        Assert.Equal(3, result.Differences.Count);
        Assert.Contains(result.Differences, difference => difference.StartsWith("d1: declared blockCleanExport, expected warn", StringComparison.Ordinal));
        Assert.Contains(result.Differences, difference => difference.StartsWith("questions:", StringComparison.Ordinal));
        Assert.Contains(result.Differences, difference => difference.StartsWith("section:", StringComparison.Ordinal));
    }

    [Fact]
    public void AFinalExampleIsAlsoHeldToItsShownEffectAndMark()
    {
        var json = Build.Edit(Build.ModuleJson, root =>
        {
            var finding = root["examples"]!.AsArray().Single(example => example!["id"]!.GetValue<string>() == "ex2")!["expect"]!["findings"]![0]!;
            finding["shownEffect"] = "blockCleanExport";
            finding["toCheck"] = false;
        });

        var result = WorkedExamples.Run(new RuleEvaluator(), Build.Load(json)).Single(result => result.CaseId == "ex2");
        Assert.Equal(2, result.Differences.Count);
    }

    [Fact]
    public void TheTraceSaysWhichBranchWasTakenWithTheDates()
    {
        var module = HandbookFiles.Example("module.labour-prelitigation.example.json");
        var example = module.Examples.Single(example => example.Id == "ex2");
        var outcome = new RuleEvaluator().Evaluate([module], WorkedExamples.CaseSlotsOf(example.Slots), Build.Petition);
        Assert.Equal(
            "d1: start 2025-06-01, last day 2025-12-02, petition 2026-03-01, 89 days late, outside band 15 -> blockCleanExport; grade D -> warn, to check",
            outcome.Finding("d1").Trace);
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void TheSameCallTwiceGivesTheSameOutcomeWhateverTheMachinesCulture(string name)
    {
        var record = HandbookFiles.Example(name);
        var reference = Outcomes(record);
        Assert.Equal(reference, Outcomes(record));

        var culture = CultureInfo.CurrentCulture;
        try
        {
            // A Hijri calendar and Arabic digits would show at once in a date parsed or written with the current culture.
            foreach (var other in new[] { "ar-SA", "fr-FR", "ar-DZ" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(other);
                Assert.Equal(reference, Outcomes(record));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    private static List<string> Outcomes(RuleRecord record) =>
    [
        .. record.Examples.Select(example => JsonSerializer.Serialize(
            new RuleEvaluator().Evaluate([record], WorkedExamples.CaseSlotsOf(example.Slots), Dates.Parse(example.PetitionDate, "petitionDate")),
            ContractJson.Options)),
    ];
}
