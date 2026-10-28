using DzLegal.Domain;
using DzLegal.Drafting.Evidence;
using DzLegal.Law;

namespace DzLegal.Drafting.Tests;

public sealed class EvidenceCandidateTests
{
    private static LegalKnowledge Knowledge => new(new LawLibrary([new LawCode
    {
        Id = "fictional-code", Title = new("قانون تجريبي", "Fictitious test code"), CitationName = "قانون تجريبي", Reference = "synthetic fixture", ConsolidatedAsOf = "2024-01-01",
        Articles = [new() { Number = "1", Text = "بدل الإيجار. باستثناء حالة تجريبية." }, new() { Number = "2", Text = "موضوع مختلف." }],
    }]), new LawSourceManifest(), []);

    [Fact]
    public void ExactReferencesDependenciesAndRawSearchSurviveAMisleadingCourtHint()
    {
        var snapshot = CaseSnapshot.Create("fictional", 0, new Dictionary<string, string> { ["court.section"] = "قسم آخر", ["notes.facts"] = "المواد ١ و٢. بدل الإيجار." });
        var candidates = EvidenceAuthorityCandidates.Discover(Knowledge, snapshot, [], [new("fictional-code", "999", "unknown", "", "", "unreviewed")]);
        Assert.Contains(candidates.Candidates, c => c.Article == "1" && c.Origins.Contains("exact-reference") && c.Origins.Contains("raw-text-bm25"));
        Assert.Contains(candidates.Candidates, c => c.Article == "2" && c.Origins.Contains("exact-reference"));
        Assert.Contains(candidates.Candidates, c => c.Article == "999" && c.Text is null && c.Origins.Contains("rule-dependency"));
        Assert.Contains(candidates.CoverageGaps, g => g.StartsWith("unavailable dependency", StringComparison.Ordinal));
        Assert.Contains("باستثناء", candidates.Candidates.Single(c => c.Article == "1").Text!, StringComparison.Ordinal);
    }

    [Fact]
    public void APlainNumberInTheFactsDoesNotBecomeAnExactArticleReference()
    {
        var snapshot = CaseSnapshot.Create("fictional", 0, new Dictionary<string, string> { ["notes.facts"] = "كان ذلك في سنة 2024 وقيمته 35000 دج." });
        var candidates = EvidenceAuthorityCandidates.Discover(Knowledge, snapshot, [], []);
        Assert.DoesNotContain(candidates.Candidates, c => c.Origins.Contains("exact-reference"));
    }
}
