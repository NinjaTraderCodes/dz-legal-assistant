using DzLegal.CoreLogic.Rules.Checks;

namespace DzLegal.CoreLogic.Contracts.Tests;

/// <summary>
/// The handbook's example records are the format every record of step 1 follows. These tests keep four things in step:
/// the examples, the schema, the contract's types and the slot list. The checks themselves live in one place,
/// <see cref="RecordChecks"/> (the <c>rules check</c> command runs the same code on every real record); each test here
/// names the checks it holds the examples to.
/// </summary>
public sealed class ExampleRecordTests
{
    [Fact]
    public void TheSchemaUsesOnlyTheKeywordsTheValidatorChecks() =>
        Assert.Empty(MiniSchema.UnsupportedKeywords(HandbookFiles.Schema()));

    [Fact]
    public void ThereIsOneExampleOfAClaimTypeAndOneOfAModule()
    {
        var kinds = HandbookFiles.ExamplePaths.Select(path => HandbookFiles.Example(Path.GetFileName(path)).Kind).Order().ToList();
        Assert.Equal([RecordKind.Claim, RecordKind.Module], kinds);
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AnExamplePassesEveryCheckOfRulesCheck(string name) =>
        Assert.Empty(Check(name).Problems);

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AnExampleMatchesTheSchema(string name) =>
        Assert.Empty(Check(name).Of(RecordCheck.Schema));

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AnExampleLoadsThroughTheContractWithNoUnknownField(string name)
    {
        var result = Check(name);
        Assert.Empty(result.Of(RecordCheck.Contract));
        Assert.NotNull(result.Record);
        Assert.Equal("1.0", result.Record.SchemaVersion);
        Assert.False(result.Record.Verified);
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void EveryReferenceInsideARecordPointsToSomethingThatExists(string name) =>
        Assert.Empty(Check(name).Of(RecordCheck.References));

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void EverySlotEventRequestAndClaimTypeARecordNamesExists(string name)
    {
        var result = Check(name);
        Assert.Empty(result.Of(RecordCheck.Slots));
        Assert.Empty(result.Of(RecordCheck.Vocabulary));
        Assert.Empty(result.Of(RecordCheck.ClaimElements));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AFinalRecordCarriesItsQuotesAndTheQuotesAreIntact(string name)
    {
        var result = Check(name);
        Assert.Equal(RecordStage.Final, result.Record!.Stage);
        Assert.Empty(result.Of(RecordCheck.ScriptFields));
        Assert.Empty(result.Of(RecordCheck.Quotes));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void OnlyAFirmForfeitureOrPriorStepMayDeclareTheBlockOfTheCleanExport(string name)
    {
        var result = Check(name);
        Assert.Empty(result.Of(RecordCheck.Effects));
        Assert.Empty(result.Of(RecordCheck.Conditions));
        Assert.Empty(result.Of(RecordCheck.Structure));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void ToCheckIsAMarkOnGradeDAndNothingElse(string name)
    {
        var result = Check(name);
        Assert.Contains(Rules.RecordItems.Gradings(result.Record!), grading => grading.Value is not null);
        Assert.Empty(result.Of(RecordCheck.Grades));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void WhatTheAppShowsFollowsTheGradeAndIsNeverWeakerThanAWarning(string name)
    {
        var result = Check(name);
        Assert.Empty(result.Of(RecordCheck.Grades));

        var expected = result.Record!.Examples.SelectMany(example => example.Expect.Findings).ToList();
        Assert.NotEmpty(expected);
        Assert.All(expected, finding => Assert.NotNull(finding.ShownEffect));

        // The owner's rule of 13 November 2026, in one line: a rule that declares a warning or a block is never shown as less than a warning.
        Assert.DoesNotContain(expected, finding =>
            finding.Effect is FindingEffect.BlockCleanExport or FindingEffect.Warn
            && finding.ShownEffect is not (FindingEffect.BlockCleanExport or FindingEffect.Warn));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AFinalRecordProposesNoSlot(string name)
    {
        Assert.Empty(HandbookFiles.Example(name).ProposedSlots);
        Assert.Empty(Check(name).Of(RecordCheck.Slots));
    }

    [Fact]
    public void TheContractRefusesAWrongCaseAnUnknownFieldAndAnEnumWrittenAsANumber()
    {
        const string Good = """{ "id": "s", "type": "date", "question": { "ar": "؟", "en": "?" }, "why": "w" }""";
        Assert.Equal(SlotType.Date, System.Text.Json.JsonSerializer.Deserialize<ProposedSlot>(Good, ContractJson.Options)!.Type);
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<ProposedSlot>(Good.Replace("\"id\"", "\"Id\"", StringComparison.Ordinal), ContractJson.Options));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<ProposedSlot>(Good.Replace("\"date\"", "1", StringComparison.Ordinal), ContractJson.Options));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<ProposedSlot>(Good.Replace("\"why\"", "\"extra\": 1, \"why\"", StringComparison.Ordinal), ContractJson.Options));
    }

    [Fact]
    public void TheOnlyBlockingEffectIsTheBlockOfTheCleanExport() =>
        Assert.Equal(["BlockCleanExport", "Warn", "Ask", "None"], Enum.GetNames<FindingEffect>());

    [Fact]
    public void AnExampleThatStatesNoQuestionsIsNotAnExampleThatAllowsNone()
    {
        var module = HandbookFiles.Example("module.labour-prelitigation.example.json");
        Assert.Null(module.Examples.Single(example => example.Id == "ex1").Expect.Questions);
        Assert.Empty(module.Examples.Single(example => example.Id == "ex8").Expect.Questions!);
    }

    [Fact]
    public void TheClaimExampleCarriesTheModuleBlocksItImports()
    {
        var claim = HandbookFiles.ExamplePaths.Select(path => HandbookFiles.Example(Path.GetFileName(path))).Single(record => record.Kind == RecordKind.Claim);
        var module = HandbookFiles.ExamplePaths.Select(path => HandbookFiles.Example(Path.GetFileName(path))).Single(record => record.Kind == RecordKind.Module);
        var import = Assert.Single(claim.Imports);
        Assert.Equal(module.Id, import.Module);
        Assert.Contains(claim.Id, module.AppliesTo);

        var copied = Rules.RecordItems.ItemIds(claim).Select(item => item.Value).ToHashSet(StringComparer.Ordinal);
        Assert.All(import.Blocks.Where(block => block != "section"), block => Assert.Contains($"{module.Id}.{block}", copied));
        var moduleQuotes = module.Basis.ToDictionary(statement => $"{module.Id}.{statement.Id}", statement => statement.Quote, StringComparer.Ordinal);
        Assert.All(claim.Basis.Where(statement => moduleQuotes.ContainsKey(statement.Id)), statement =>
            Assert.Equal(moduleQuotes[statement.Id].Select(paragraph => paragraph.Sha256), statement.Quote.Select(paragraph => paragraph.Sha256)));
        Assert.Equal(module.Court!.Section!.Value, claim.Court!.Section!.Value);
    }

    private static RecordCheckResult Check(string name) => RecordChecks.Check(HandbookFiles.ExampleText(name), HandbookFiles.CheckContext());
}
