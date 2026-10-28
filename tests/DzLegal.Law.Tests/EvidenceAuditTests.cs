using DzLegal.Drafting;
using DzLegal.Drafting.Evidence;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class EvidenceAuditTests
{
    private static readonly FactGold Gold = new()
    {
        Relation = "lease", Position = "lessor", Events = [new() { Kind = "contract_concluded", Date = "15/02/2024", Amount = "35000", Actor = "plaintiff" }], Requests = [new() { Kind = "payment", Amount = "35000" }],
    };

    private static FactEvent Event(string polarity = "affirmed") => new("contract_concluded", new("15/02/2024", "15/02/2024", "cited"), new("35000", "35000", "cited"), "plaintiff", polarity, ["s1"]);

    private static CaseFacts Correct => new() { Relation = "lease", Position = "lessor", Events = [Event()], Requests = [new("payment", new("35000", "35000", "cited"), ["s2"])] };

    [Fact]
    public void ACompleteCorrectReadingStillPassesTheStricterAudit() => Assert.Empty(StrictFactsEvaluator.Score(Gold, Correct));

    [Fact]
    public void MissingDecisiveValuesRequestsAndActorsCannotHideBehindSyntax()
    {
        var result = StrictFactsEvaluator.Score(Gold, Correct with { Events = [Event() with { Date = null, Amount = null, Actor = "unknown", Sentences = [] }], Requests = [] });
        Assert.Contains(result, e => e is { Type: "missing date", Critical: true });
        Assert.Contains(result, e => e is { Type: "missing amount", Critical: true });
        Assert.Contains(result, e => e is { Type: "missing request", Critical: true });
        Assert.Contains(result, e => e is { Type: "actor", Critical: true });
        Assert.Contains(result, e => e is { Type: "missing source", Critical: true });
    }

    [Fact]
    public void ACorrectDuplicateCannotCancelAWrongPrediction()
    {
        var result = StrictFactsEvaluator.Score(Gold, Correct with { Events = [Event("denied"), Event()] });
        Assert.Contains(result, e => e is { Type: "polarity", Critical: true });
        Assert.Contains(result, e => e is { Type: "ambiguous duplicate events", Critical: true });
    }

    [Fact]
    public void MissingEventsRemainInDateAndPolarityDenominators()
    {
        Assert.Equal((0, 1), StrictFactsEvaluator.Dates(Gold, Correct with { Events = [] }));
        Assert.Equal((0, 1), StrictFactsEvaluator.Polarities(Gold, Correct with { Events = [] }));
    }

    [Fact]
    public void IncompleteGoldDoesNotFalselyCertifyPrecisionOrCallEveryAdditionFalse()
    {
        var result = StrictFactsEvaluator.Score(Gold, Correct with { Events = [Event(), Event() with { Kind = "lease_started" }] });
        Assert.Contains(result, e => e is { Type: "unannotated event", Critical: false });
        Assert.DoesNotContain(result, e => e.Critical);
    }

    [Fact]
    public void LegacyMarkdownReplayReadsOnlyTheAnswerBlock() => Assert.Equal("event | notice_served", EvidenceCommands.LegacyAnswer("## Case\n```\nwrong\n```\n## Answer\n```\nevent | notice_served\n```\n## Errors\nwrong"));

    [Fact]
    public void SealedQualificationRequiresCoverageLegalReviewHardwareAndNoLeakage()
    {
        var release = Enumerable.Range(0, 300).Select(i => new QualificationCase($"case{i}", $"family{i}", $"synthetic:{i}", "release", true, 0, true)).ToArray();
        var result = EvidenceQualification.Evaluate(release, [], reviewedRules: true, physical8GbPassed: true);
        Assert.True(result.Qualified);
        Assert.InRange(result.ZeroErrorUpperBound95!.Value, 0.009, 0.011);
        Assert.False(EvidenceQualification.Evaluate(release, [release[0] with { Partition = "train" }], true, true).Qualified);
        Assert.False(EvidenceQualification.Evaluate(release, [], false, true).Qualified);
        Assert.False(EvidenceQualification.Evaluate(release, [], true, false).Qualified);
        Assert.False(EvidenceQualification.Evaluate(release.Take(299).ToArray(), [], true, true).Qualified);
        Assert.False(EvidenceQualification.Evaluate(release.Select(c => c with { ScenarioFamily = "one-family", SourceDecision = "one-source" }).ToArray(), [], true, true).Qualified);
        Assert.False(EvidenceQualification.Evaluate(release.Select((c, i) => c with { Complete = i >= 31 }).ToArray(), [], true, true).Qualified);
        Assert.False(EvidenceQualification.Evaluate(release.Select((c, i) => c with { CriticalErrors = i == 299 ? 1 : 0 }).ToArray(), [], true, true).Qualified);
    }

    [Fact]
    public void TrainingExportRejectsUnreviewedLabelsAndSourceFamilyLeakage()
    {
        var input = new Dictionary<string, string> { ["notes.facts"] = "قضية وهمية." };
        var snapshot = CaseSnapshot.Create("fictional", 0, input);
        var target = new CaseEvidence { SnapshotFingerprint = snapshot.Fingerprint, Coverage = [new("s1", "read")] };
        var example = new EvidenceDatasetCase("fictional", "family", "synthetic:1", "train", true, "unreviewed", input, target);
        Assert.NotEmpty(EvidenceDataset.Validate([example]));
        Assert.Empty(EvidenceDataset.Validate([example with { LabelReview = "reviewed" }]));
        Assert.Contains(EvidenceDataset.Validate([example with { LabelReview = "reviewed" }, example with { CaseId = "fictional2", Partition = "release", LabelReview = "reviewed" }]), e => e.StartsWith("family crosses", StringComparison.Ordinal));
        Assert.NotEmpty(EvidenceDataset.Validate([example with { Fictitious = false, LabelReview = "reviewed" }]));
    }

    [Fact]
    public async Task TheLeaseDemoPreservesBothAccountsAndExistingReviewRequirements()
    {
        var folder = Directory.CreateTempSubdirectory("dzlegal-evidence-");
        try
        {
            var output = Path.Combine(folder.FullName, "demo");
            using var log = new StringWriter();
            Assert.Equal(0, await EvidenceCommands.RunAsync(["evidence", "demo", "--out", output], log, CancellationToken.None));
            var evidence = EvidenceJson.Parse(File.ReadAllText(Path.Combine(output, "evidence.json")));
            Assert.Contains(evidence.Events, e => e.Kind == "notice_served" && e.AssertedBy == "plaintiff" && e.Polarity == "affirmed");
            Assert.Contains(evidence.Events, e => e.Kind == "notice_served" && e.AssertedBy == "defendant" && e.Polarity == "denied");
            var petition = File.ReadAllText(Path.Combine(output, "evaluation", "petition.ar.txt"));
            Assert.Contains("15/02/2024", petition, StringComparison.Ordinal);
            Assert.Contains("01/03/2024", petition, StringComparison.Ordinal);
            Assert.Contains("ينكر المدعى عليه", petition, StringComparison.Ordinal);
            Assert.Contains("140000", petition, StringComparison.Ordinal);
            Assert.Contains("(...)", petition, StringComparison.Ordinal);
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "evaluation", "petition.json")));
            Assert.All(document.RootElement.GetProperty("sections").EnumerateArray().Where(s => s.GetProperty("id").GetString() is "facts" or "grounds"), s =>
            {
                Assert.True(s.GetProperty("aiWritten").GetBoolean());
                Assert.False(s.GetProperty("reviewed").GetBoolean());
            });
            using var result = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "evaluation", "result.json")));
            Assert.All(result.RootElement.GetProperty("decisions").EnumerateArray(), d => Assert.False(d.GetProperty("qualified").GetBoolean()));
        }
        finally { folder.Delete(recursive: true); }
    }

    [Fact]
    public async Task UnreviewedTrainingDataReturnsACleanErrorAndCreatesNoExport()
    {
        var folder = Directory.CreateTempSubdirectory("dzlegal-evidence-rejection-");
        try
        {
            var input = new Dictionary<string, string> { ["notes.facts"] = "قضية وهمية." };
            var snapshot = CaseSnapshot.Create("fictional", 0, input);
            var target = new CaseEvidence { SnapshotFingerprint = snapshot.Fingerprint, Coverage = [new("s1", "read")] };
            var example = new EvidenceDatasetCase("fictional", "family", "synthetic:1", "train", true, "unreviewed", input, target);
            var file = Path.Combine(folder.FullName, "dataset.json");
            File.WriteAllText(file, System.Text.Json.JsonSerializer.Serialize(new[] { example }, EvidenceJson.Options));
            using var log = new StringWriter();
            var output = Path.Combine(folder.FullName, "must-not-exist");
            Assert.Equal(2, await EvidenceCommands.RunAsync(["evidence", "dataset", "--dataset", file, "--out", output], log, CancellationToken.None));
            Assert.Contains("reviewed", log.ToString(), StringComparison.Ordinal);
            Assert.False(Directory.Exists(output));
        }
        finally { folder.Delete(recursive: true); }
    }
}
