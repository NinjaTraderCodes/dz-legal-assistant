using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DzLegal.Drafting;

namespace DzLegal.CoreLogic.Contracts.Tests;

/// <summary>
/// The handbook's example records are the format every record of step 1 follows. These tests keep four things in step:
/// the examples, the schema, the contract's types and the slot list. A real record must pass the same checks
/// (docs/research/core-logic/specs/record-checks.md lists them with the others).
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
    public void AnExampleMatchesTheSchema(string name) =>
        Assert.Empty(MiniSchema.Validate(HandbookFiles.Schema(), HandbookFiles.ExampleJson(name)));

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AnExampleLoadsThroughTheContractWithNoUnknownField(string name)
    {
        var record = HandbookFiles.Example(name);
        Assert.Equal("1.0", record.SchemaVersion);
        Assert.False(record.Verified);
        Assert.NotEmpty(record.Basis);
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void EveryReferenceInsideARecordPointsToSomethingThatExists(string name)
    {
        var record = HandbookFiles.Example(name);
        var statements = record.Basis.Select(statement => statement.Id).ToHashSet(StringComparer.Ordinal);
        var items = ItemIds(record);

        Assert.Equal(items.Count, AllIds(record).Count);
        Assert.All(BasisRefs(record), reference => Assert.Contains(reference, statements));
        Assert.All(record.Checklist.SelectMany(line => line.Refs), reference => Assert.Contains(reference, items));
        Assert.All(record.OpenPoints.SelectMany(point => point.Refs), reference => Assert.Contains(reference, items));
        Assert.All(record.Examples.SelectMany(example => example.Expect.Findings), finding => Assert.Contains(finding.Ref, items));
        Assert.All(record.Deadlines.SelectMany(deadline => deadline.Counting?.Basis ?? []), reference => Assert.Contains(reference, statements));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void EverySlotEventRequestAndClaimTypeARecordNamesExists(string name)
    {
        var record = HandbookFiles.Example(name);
        var catalogue = HandbookFiles.Slots().Slots.Select(slot => slot.Id).ToHashSet(StringComparer.Ordinal);
        var declared = record.Slots.ToHashSet(StringComparer.Ordinal);
        var claims = HandbookFiles.ClaimIds();

        Assert.All(record.Slots.Where(slot => !HandbookFiles.ReservedSlots.Contains(slot)), slot => Assert.Contains(slot, catalogue));
        Assert.All(SlotsUsed(record), slot => Assert.Contains(slot, declared));
        Assert.All(record.Examples.SelectMany(example => example.Expect.Questions), slot => Assert.Contains(slot, declared));

        var events = record.Elements.SelectMany(element => element.Events)
            .Concat(record.Deadlines.Select(deadline => deadline.StartsFrom.Event).OfType<string>());
        Assert.All(events, kind => Assert.Contains(kind, FactVocabulary.Events));
        Assert.All(record.Requests, request => Assert.Contains(request.Kind, FactVocabulary.Requests));

        if (record.Kind == RecordKind.Claim)
        {
            Assert.Contains(record.Id, claims);
            Assert.NotNull(record.Area);
        }

        Assert.All(record.AppliesTo.Where(id => id != "*"), id => Assert.Contains(id, claims));
        var neighbours = record.Boundaries.Select(boundary => boundary.IsActually).Where(target => target.StartsWith("claim:", StringComparison.Ordinal));
        Assert.All(neighbours, target => Assert.Contains(target["claim:".Length..], claims));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AFinalRecordCarriesItsQuotesAndTheQuotesAreIntact(string name)
    {
        var record = HandbookFiles.Example(name);
        Assert.Equal(RecordStage.Final, record.Stage);
        Assert.All(record.Basis, statement =>
        {
            Assert.NotEmpty(statement.Quote);
            Assert.NotNull(statement.Source);
            Assert.NotNull(statement.Grading);
            Assert.Contains(statement.Grading.Grade, Grades.All);
            Assert.All(statement.Quote, paragraph => Assert.Equal(Sha256(paragraph.Text), paragraph.Sha256));
        });

        var byId = record.Basis.ToDictionary(statement => statement.Id, StringComparer.Ordinal);
        foreach (var deadline in record.Deadlines)
        {
            var quoted = string.Join(' ', deadline.Basis.SelectMany(id => byId[id].Quote).Select(paragraph => paragraph.Text));
            if (deadline.Extract?.Period is { } period)
            {
                Assert.Contains(period, quoted, StringComparison.Ordinal);
            }

            if (deadline.Extract?.Sanction is { } sanction)
            {
                Assert.Contains(sanction, quoted, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void OnlyAFirmForfeitureOrPriorStepMayDeclareTheBlockOfTheCleanExport(string name)
    {
        var record = HandbookFiles.Example(name);
        Assert.All(record.Deadlines, deadline =>
        {
            Assert.Contains(deadline.EffectWhenExpired, new[] { FindingEffect.BlockCleanExport, FindingEffect.Warn });
            if (deadline.Kind != DeadlineKind.Forfeiture || deadline.Interpretive)
            {
                Assert.Equal(FindingEffect.Warn, deadline.EffectWhenExpired);
            }
        });
        Assert.All(record.PriorSteps, step =>
        {
            Assert.Contains(step.EffectWhenMissing, new[] { FindingEffect.BlockCleanExport, FindingEffect.Warn });
            Assert.Contains(step.EffectWhenUnknown, new[] { FindingEffect.Ask, FindingEffect.Warn });
            if (step.Interpretive)
            {
                Assert.Equal(FindingEffect.Warn, step.EffectWhenMissing);
            }
        });
        Assert.All(record.Court?.Exits ?? [], exit => Assert.Equal(FindingEffect.Warn, exit.Effect));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void ToCheckIsAMarkOnGradeDAndNothingElse(string name)
    {
        var record = HandbookFiles.Example(name);
        var gradings = record.Basis.Select(statement => statement.Grading)
            .Concat(record.Elements.Select(element => element.Grading))
            .Concat(record.Requests.Select(request => request.Grading))
            .Concat(record.Deadlines.Select(deadline => deadline.Grading))
            .Concat(record.PriorSteps.Select(step => step.Grading))
            .Concat(record.Documents.Select(document => document.Grading))
            .Concat(record.Boundaries.Select(boundary => boundary.Grading))
            .Concat(record.Court?.Territory.Select(rule => rule.Grading) ?? [])
            .Concat(record.Court?.Exits.Select(exit => exit.Grading) ?? [])
            .Append(record.Court?.Section?.Grading)
            .OfType<Grading>()
            .ToList();
        Assert.NotEmpty(gradings);
        Assert.All(gradings, grading => Assert.Equal(grading.Grade == Grades.D, grading.ToCheck));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void WhatTheAppShowsFollowsTheGradeAndIsNeverWeakerThanAWarning(string name)
    {
        var record = HandbookFiles.Example(name);
        var grades = new Dictionary<string, Grading?>(StringComparer.Ordinal);
        foreach (var deadline in record.Deadlines) { grades[deadline.Id] = deadline.Grading; }
        foreach (var step in record.PriorSteps) { grades[step.Id] = step.Grading; }
        foreach (var document in record.Documents) { grades[document.Id] = document.Grading; }
        foreach (var boundary in record.Boundaries) { grades[boundary.Id] = boundary.Grading; }
        foreach (var exit in record.Court?.Exits ?? []) { grades[exit.Id] = exit.Grading; }

        var expected = record.Examples.SelectMany(example => example.Expect.Findings).ToList();
        Assert.NotEmpty(expected);
        Assert.All(expected, finding =>
        {
            var grading = grades[finding.Ref];
            Assert.NotNull(grading);
            Assert.NotNull(finding.ShownEffect);
            Assert.Equal(grading.Grade == Grades.D, finding.ToCheck);
            var shown = finding.Effect == FindingEffect.BlockCleanExport && grading.Grade != Grades.A ? FindingEffect.Warn : finding.Effect;
            Assert.Equal(shown, finding.ShownEffect);
        });

        // The owner's rule of 13 November 2026, in one line: a rule that declares a warning or a block is never shown as less than a warning.
        Assert.DoesNotContain(expected, finding =>
            finding.Effect is FindingEffect.BlockCleanExport or FindingEffect.Warn
            && finding.ShownEffect is not (FindingEffect.BlockCleanExport or FindingEffect.Warn));
    }

    [Theory]
    [MemberData(nameof(HandbookFiles.ExampleNames), MemberType = typeof(HandbookFiles))]
    public void AFinalRecordProposesNoSlot(string name) => Assert.Empty(HandbookFiles.Example(name).ProposedSlots);

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
    public void TheClaimExampleCarriesTheModuleBlocksItImports()
    {
        var claim = HandbookFiles.ExamplePaths.Select(path => HandbookFiles.Example(Path.GetFileName(path))).Single(record => record.Kind == RecordKind.Claim);
        var module = HandbookFiles.ExamplePaths.Select(path => HandbookFiles.Example(Path.GetFileName(path))).Single(record => record.Kind == RecordKind.Module);
        var import = Assert.Single(claim.Imports);
        Assert.Equal(module.Id, import.Module);
        Assert.Contains(claim.Id, module.AppliesTo);

        var copied = ItemIds(claim);
        Assert.All(import.Blocks.Where(block => block != "section"), block => Assert.Contains($"{module.Id}.{block}", copied));
        var moduleQuotes = module.Basis.ToDictionary(statement => $"{module.Id}.{statement.Id}", statement => statement.Quote, StringComparer.Ordinal);
        Assert.All(claim.Basis.Where(statement => moduleQuotes.ContainsKey(statement.Id)), statement =>
            Assert.Equal(moduleQuotes[statement.Id].Select(paragraph => paragraph.Sha256), statement.Quote.Select(paragraph => paragraph.Sha256)));
        Assert.Equal(module.Court!.Section!.Value, claim.Court!.Section!.Value);
    }

    private static List<string> AllIds(RuleRecord record) =>
    [
        .. record.Basis.Select(statement => statement.Id),
        .. record.Elements.Select(element => element.Id),
        .. record.Requests.Select(request => request.Id),
        .. record.Deadlines.Select(deadline => deadline.Id),
        .. record.PriorSteps.Select(step => step.Id),
        .. record.Documents.Select(document => document.Id),
        .. record.Boundaries.Select(boundary => boundary.Id),
        .. record.OpenPoints.Select(point => point.Id),
        .. record.Court?.Territory.Select(rule => rule.Id) ?? [],
        .. record.Court?.Exits.Select(exit => exit.Id) ?? [],
    ];

    private static HashSet<string> ItemIds(RuleRecord record) => AllIds(record).ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<string> BasisRefs(RuleRecord record) =>
        record.Elements.SelectMany(element => element.Basis.Concat(element.Proof.SelectMany(proof => proof.Basis)))
            .Concat(record.Requests.SelectMany(request => request.Basis))
            .Concat(record.Deadlines.SelectMany(deadline => deadline.Basis.Concat(deadline.Readings.SelectMany(reading => reading.Basis)).Concat(deadline.Counting?.Basis ?? [])))
            .Concat(record.PriorSteps.SelectMany(step => step.Basis
                .Concat(step.NotRequiredWhen.SelectMany(exemption => exemption.Basis))
                .Concat(step.AcceptedMeans.SelectMany(means => means.Basis))))
            .Concat(record.Documents.SelectMany(document => document.Basis))
            .Concat(record.Boundaries.SelectMany(boundary => boundary.Basis))
            .Concat(record.OpenPoints.SelectMany(point => point.Readings.SelectMany(reading => reading.Basis)))
            .Concat(record.Court?.Section?.Basis ?? [])
            .Concat(record.Court?.Section?.Alternatives.SelectMany(alternative => alternative.Basis) ?? [])
            .Concat(record.Court?.Territory.SelectMany(rule => rule.Basis) ?? [])
            .Concat(record.Court?.Exits.SelectMany(exit => exit.Basis) ?? []);

    private static IEnumerable<string> SlotsUsed(RuleRecord record)
    {
        var conditions = record.Requests.Select(request => request.When)
            .Concat(record.Deadlines.Select(deadline => deadline.When))
            .Concat(record.PriorSteps.Select(step => step.RequiredWhen))
            .Concat(record.PriorSteps.SelectMany(step => step.NotRequiredWhen.Select(exemption => (SlotCondition?)exemption.When)))
            .Concat(record.Documents.Select(document => document.RequiredWhen))
            .Concat(record.Boundaries.Select(boundary => boundary.When))
            .Concat(record.Court?.Section?.Alternatives.Select(alternative => (SlotCondition?)alternative.When) ?? [])
            .Concat(record.Court?.Territory.Select(rule => rule.When) ?? [])
            .Concat(record.Court?.Exits.Select(exit => (SlotCondition?)exit.When) ?? []);

        return conditions.OfType<SlotCondition>().SelectMany(ConditionSlots)
            .Concat(record.Deadlines.Select(deadline => deadline.StartsFrom.Slot))
            .Concat(record.PriorSteps.SelectMany(step => step.Slots))
            .Concat(record.Elements.SelectMany(element => element.Slots))
            .Concat(record.Boundaries.SelectMany(boundary => boundary.Slots))
            .Concat(record.Examples.SelectMany(example => example.Slots.Keys));
    }

    private static IEnumerable<string> ConditionSlots(SlotCondition condition)
    {
        if (condition.Slot is not null)
        {
            yield return condition.Slot;
        }

        var children = (condition.All ?? []).Concat(condition.Any ?? []);
        if (condition.Not is not null)
        {
            children = children.Append(condition.Not);
        }

        foreach (var slot in children.SelectMany(ConditionSlots))
        {
            yield return slot;
        }
    }

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
