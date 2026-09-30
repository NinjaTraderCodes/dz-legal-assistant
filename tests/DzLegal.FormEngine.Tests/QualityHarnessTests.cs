using DzLegal.Domain;
using DzLegal.Harness;
using static DzLegal.FormEngine.Tests.TestData;

namespace DzLegal.FormEngine.Tests;

/// <summary>
/// The quality harness as a test: the Form Engine must reproduce the known-correct outcome of every
/// element in every corpus case. A failure here means a rule or a corpus expectation is wrong.
/// </summary>
public class QualityHarnessTests
{
    public static TheoryData<string> CaseFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(CorpusRoot, "*.case.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetRelativePath(CorpusRoot, file));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CaseFiles))]
    public void CorpusCaseMatchesExpectedOutcome(string relativePath)
    {
        var corpusCase = CorpusLoader.Load(Path.Combine(CorpusRoot, relativePath));
        var result = CorpusEvaluator.Evaluate(Requete, [corpusCase]);
        Assert.True(result.Perfect, string.Join("\n", result.Mismatches));
    }

    [Fact]
    public void WholeCorpusScoresOneHundredPercent()
    {
        var result = CorpusEvaluator.Evaluate(Requete, CorpusLoader.LoadAll(CorpusRoot));
        Assert.True(result.CaseCount >= 20);
        Assert.Equal(1.0, result.OverallAccuracy);
    }

    [Fact]
    public void EveryElementIsExercisedAsMissingAtLeastOnce()
    {
        var cases = CorpusLoader.LoadAll(CorpusRoot);
        var exercised = cases.SelectMany(c => c.ExpectedMissing).ToHashSet();
        var neverMissing = Requete.Elements.Select(e => e.Id).Where(id => !exercised.Contains(id)).ToList();
        Assert.True(neverMissing.Count == 0, "Never exercised as missing: " + string.Join(", ", neverMissing));
    }

    [Fact]
    public void HarnessDetectsAWrongExpectation()
    {
        var good = CorpusLoader.Load(Path.Combine(CorpusRoot, "requete-introductive", "missing-facts.case.json"));
        var wrong = good with { ExpectedMissing = [] };

        var result = CorpusEvaluator.Evaluate(Requete, [wrong]);

        var mismatch = Assert.Single(result.Mismatches);
        Assert.Equal("art15.5.facts", mismatch.ElementId);
        Assert.Equal(ElementStatus.Missing, mismatch.Actual);
        var score = result.Scores.Single(s => s.ElementId == "art15.5.facts");
        Assert.Equal(1, score.FalsePositives);
        Assert.Equal(0, score.Precision);
    }

    [Fact]
    public void HarnessRejectsUnknownElementIds()
    {
        var good = CorpusLoader.Load(Path.Combine(CorpusRoot, "requete-introductive", "missing-facts.case.json"));
        Assert.Throws<InvalidDataException>(() => CorpusEvaluator.Evaluate(Requete, [good with { ExpectedMissing = ["art99"] }]));
    }
}
