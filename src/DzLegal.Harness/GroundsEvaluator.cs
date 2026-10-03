using System.Diagnostics;
using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Law;

namespace DzLegal.Harness;

public sealed record GroundsCase
{
    public required string Name { get; init; }

    public string? CourtSection { get; init; }

    public string Subject { get; init; } = string.Empty;

    public required string Facts { get; init; }

    /// <summary>The catalog claim type a lawyer would start from; empty when the catalog does not cover the case.</summary>
    public string Expect { get; init; } = string.Empty;

    /// <summary>Articles a lawyer would cite, as "code:number" (e.g. "civil-code:450").</summary>
    public IReadOnlyList<string> ExpectArticles { get; init; } = [];
}

public sealed record GroundsCaseFile
{
    public IReadOnlyList<GroundsCase> Cases { get; init; } = [];
}

public sealed record GroundsCaseResult(GroundsCase Case, GroundsAnalysis Analysis, TimeSpan Elapsed)
{
    public bool ClaimFound => Case.Expect.Length == 0 ? Analysis.ClaimIds.Count == 0 : Analysis.ClaimIds.Contains(Case.Expect);

    public IReadOnlyList<string> Selected => Analysis.Articles.Select(a => $"{a.Code}:{a.Number}").ToList();

    public int ExpectedFound => Case.ExpectArticles.Count(Selected.Contains);

    public IReadOnlyList<string> Unexpected => Selected.Where(s => !Case.ExpectArticles.Contains(s)).ToList();
}

public sealed record GroundsReport(string Model, IReadOnlyList<GroundsCaseResult> Results)
{
    public double ClaimAccuracy => Results.Count == 0 ? 0 : (double)Results.Count(r => r.ClaimFound) / Results.Count;

    /// <summary>Share of all expected articles that the analysis proposed.</summary>
    public double ArticleRecall
    {
        get
        {
            var expected = Results.Sum(r => r.Case.ExpectArticles.Count);
            return expected == 0 ? 0 : (double)Results.Sum(r => r.ExpectedFound) / expected;
        }
    }

    public TimeSpan AverageTime => Results.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)Results.Average(r => r.Elapsed.Ticks));
}

/// <summary>
/// Measures the grounds analysis end to end with a local model, on fact patterns with the claim type and articles a
/// lawyer would start from. Reported, not gated: model quality is measured on the lawyer's own machine.
/// </summary>
public static class GroundsEvaluator
{
    public static IReadOnlyList<GroundsCase> LoadCases(string folder) =>
        Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal)
            .SelectMany(f => JsonSerializer.Deserialize<GroundsCaseFile>(File.ReadAllText(f), DefinitionJson.Options)?.Cases ?? [])
            .ToList();

    public static async Task<GroundsReport> EvaluateAsync(LegalKnowledge knowledge, IReadOnlyList<GroundsCase> cases, IModelProvider provider, TextWriter log)
    {
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(log);
        var analyzer = new GroundsAnalyzer(provider);
        var results = new List<GroundsCaseResult>();
        foreach (var c in cases)
        {
            var started = Stopwatch.StartNew();
            var analysis = await analyzer.AnalyseAsync(knowledge, c.Subject, c.Facts, c.CourtSection);
            var result = new GroundsCaseResult(c, analysis, started.Elapsed);
            log.WriteLine($"  [{c.Name}] {started.Elapsed.TotalSeconds:F0}s  claim: {(analysis.ClaimIds.Count == 0 ? "—" : string.Join(", ", analysis.ClaimIds))}  articles: {(result.Selected.Count == 0 ? "—" : string.Join(", ", result.Selected))}  ({result.ExpectedFound}/{c.ExpectArticles.Count} expected)");
            results.Add(result);
        }

        return new GroundsReport(provider.Name, results);
    }

    public static void Print(GroundsReport report, LegalKnowledge knowledge, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(output);
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        output.WriteLine(string.Format(ci, "{0}: claim type recognised {1:P0} · expected articles proposed {2:P0} · {3:F0}s per case", report.Model, report.ClaimAccuracy, report.ArticleRecall, report.AverageTime.TotalSeconds));
        if (knowledge.Library.Codes.Count == 0)
        {
            output.WriteLine("  Note: no official text is imported yet, so only catalog articles can be proposed and library search finds nothing.");
        }
    }
}
