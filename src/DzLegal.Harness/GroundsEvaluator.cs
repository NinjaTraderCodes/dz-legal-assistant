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

public sealed record GroundsCaseResult(GroundsCase Case, GroundsAnalysis Analysis, TimeSpan Elapsed, IReadOnlyList<string> Acceptable)
{
    public bool ClaimFound => Case.Expect.Length == 0 ? Analysis.ClaimIds.Count == 0 : Analysis.ClaimIds.Contains(Case.Expect);

    /// <summary>The expected claim is the first one recognised (or nothing is recognised when nothing applies).</summary>
    public bool ClaimExact => Case.Expect.Length == 0 ? Analysis.ClaimIds.Count == 0 : Analysis.ClaimIds.FirstOrDefault() == Case.Expect;

    /// <summary>Proposed articles outside the expected claim's catalog list (noise). For "nothing applies" cases, every proposal.</summary>
    public IReadOnlyList<string> OffClaim => Selected.Where(a => !Acceptable.Contains(a)).ToList();

    public IReadOnlyList<string> Selected => Analysis.Articles.Select(a => $"{a.Code}:{a.Number}").ToList();

    public int ExpectedFound => Case.ExpectArticles.Count(Selected.Contains);

    public IReadOnlyList<string> Unexpected => Selected.Where(s => !Case.ExpectArticles.Contains(s)).ToList();
}

public sealed record GroundsReport(string Model, IReadOnlyList<GroundsCaseResult> Results)
{
    public double ClaimAccuracy => Results.Count == 0 ? 0 : (double)Results.Count(r => r.ClaimFound) / Results.Count;

    public double ClaimExactRate => Results.Count == 0 ? 0 : (double)Results.Count(r => r.ClaimExact) / Results.Count;

    /// <summary>Share of all proposed articles that are outside the expected claim's catalog list.</summary>
    public double OffClaimShare
    {
        get
        {
            var proposed = Results.Sum(r => r.Selected.Count);
            return proposed == 0 ? 0 : (double)Results.Sum(r => r.OffClaim.Count) / proposed;
        }
    }

    public double ArticlesPerCase => Results.Count == 0 ? 0 : Results.Average(r => r.Selected.Count);

    public int EmptyWhenSomethingExpected => Results.Count(r => r.Case.Expect.Length > 0 && r.Analysis.ClaimIds.Count == 0 && r.Selected.Count == 0);

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

    public static async Task<GroundsReport> EvaluateAsync(LegalKnowledge knowledge, IReadOnlyList<GroundsCase> cases, IModelProvider provider, TextWriter log, string? outFolder = null)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(provider);
        var results = new List<GroundsCaseResult>();
        var modelFolder = outFolder is null ? null : Path.Combine(outFolder, Safe(provider.Name));
        if (modelFolder is not null)
        {
            Directory.CreateDirectory(modelFolder);
        }

        foreach (var c in cases)
        {
            var trace = new List<(string Step, string Text)>();
            var analyzer = new GroundsAnalyzer(provider) { Trace = (step, text) => trace.Add((step, text)) };
            var started = Stopwatch.StartNew();
            var analysis = await analyzer.AnalyseAsync(knowledge, c.Subject, c.Facts, c.CourtSection);
            var acceptable = knowledge.Claims.FirstOrDefault(x => x.Id == c.Expect)?.Articles.Select(a => $"{a.Code}:{a.Number}").ToList() ?? [];
            var result = new GroundsCaseResult(c, analysis, started.Elapsed, acceptable);
            log.WriteLine($"  [{c.Name}] {started.Elapsed.TotalSeconds:F0}s  claim: {(analysis.ClaimIds.Count == 0 ? "—" : string.Join(", ", analysis.ClaimIds))}  articles: {(result.Selected.Count == 0 ? "—" : string.Join(", ", result.Selected))}  ({result.ExpectedFound}/{c.ExpectArticles.Count} expected, {result.OffClaim.Count} off-claim)");
            results.Add(result);
            if (modelFolder is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(modelFolder, Safe(c.Name) + ".md"), CaseReport(result, trace));
            }
        }

        var report = new GroundsReport(provider.Name, results);
        if (outFolder is not null)
        {
            await File.AppendAllTextAsync(Path.Combine(outFolder, "summary.md"), Summary(report, knowledge));
        }

        return report;
    }

    private static string CaseReport(GroundsCaseResult r, List<(string Step, string Text)> trace)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Ci, $"# {r.Case.Name} — {r.Analysis.Model}").AppendLine();
        sb.AppendLine(Ci, $"- Court section: {r.Case.CourtSection ?? "—"}");
        sb.AppendLine(Ci, $"- Expected claim: {(r.Case.Expect.Length == 0 ? "none" : r.Case.Expect)} · expected articles: {string.Join(", ", r.Case.ExpectArticles)}");
        sb.AppendLine(Ci, $"- Recognised: {(r.Analysis.ClaimIds.Count == 0 ? "—" : string.Join(", ", r.Analysis.ClaimIds))} · proposed: {(r.Selected.Count == 0 ? "—" : string.Join(", ", r.Selected))}");
        sb.AppendLine(Ci, $"- Off-claim: {(r.OffClaim.Count == 0 ? "—" : string.Join(", ", r.OffClaim))} · time: {r.Elapsed.TotalSeconds:F0}s").AppendLine();
        sb.AppendLine("## Facts").AppendLine().AppendLine(r.Case.Facts).AppendLine();
        foreach (var (step, text) in trace)
        {
            sb.AppendLine(Ci, $"## {step}").AppendLine().AppendLine("```").AppendLine(text).AppendLine("```").AppendLine();
        }

        sb.AppendLine("## Parsed").AppendLine();
        sb.AppendLine(Ci, $"- العلاقة القانونية: {r.Analysis.Relationship}");
        sb.AppendLine(Ci, $"- الالتزام المخل به: {r.Analysis.Breach}");
        sb.AppendLine(Ci, $"- المطلوب: {r.Analysis.Sought}");
        foreach (var a in r.Analysis.Articles)
        {
            sb.AppendLine(Ci, $"- {a.Code}:{a.Number} | {a.Role} | {a.Source} | {a.Reason}");
        }

        return sb.ToString();
    }

    private static string Summary(GroundsReport report, LegalKnowledge knowledge)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Ci, $"## {report.Model}").AppendLine();
        sb.AppendLine("| claim exact | claim found | core recall | off-claim share | articles / case | empty (should not be) | seconds / case |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        sb.AppendLine(Ci, $"| {report.ClaimExactRate:P0} | {report.ClaimAccuracy:P0} | {report.ArticleRecall:P0} | {report.OffClaimShare:P0} | {report.ArticlesPerCase:F1} | {report.EmptyWhenSomethingExpected} | {report.AverageTime.TotalSeconds:F0} |").AppendLine();
        if (knowledge.Library.Codes.Count == 0)
        {
            sb.AppendLine("No official text imported: only catalog articles could be proposed.").AppendLine();
        }

        foreach (var r in report.Results)
        {
            sb.AppendLine(Ci, $"- {(r.ClaimExact ? "✓" : "✗")} `{r.Case.Name}` expected {(r.Case.Expect.Length == 0 ? "none" : r.Case.Expect)} → {(r.Analysis.ClaimIds.Count == 0 ? "—" : string.Join(", ", r.Analysis.ClaimIds))} · {r.ExpectedFound}/{r.Case.ExpectArticles.Count} core · {r.OffClaim.Count} off-claim · {r.Elapsed.TotalSeconds:F0}s");
        }

        return sb.AppendLine().ToString();
    }

    private static readonly System.Globalization.CultureInfo Ci = System.Globalization.CultureInfo.InvariantCulture;

    private static string Safe(string name) => string.Concat(name.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_'));

    public static void Print(GroundsReport report, LegalKnowledge knowledge, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(output);
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        output.WriteLine(string.Format(ci, "{0}: claim exact {1:P0} · claim found {2:P0} · core recall {3:P0} · off-claim {4:P0} · {5:F1} articles/case · {6} empty · {7:F0}s per case", report.Model, report.ClaimExactRate, report.ClaimAccuracy, report.ArticleRecall, report.OffClaimShare, report.ArticlesPerCase, report.EmptyWhenSomethingExpected, report.AverageTime.TotalSeconds));
        if (knowledge.Library.Codes.Count == 0)
        {
            output.WriteLine("  Note: no official text is imported yet, so only catalog articles can be proposed and library search finds nothing.");
        }
    }
}
