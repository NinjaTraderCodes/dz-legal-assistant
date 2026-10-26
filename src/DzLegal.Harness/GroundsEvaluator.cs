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

    /// <summary>
    /// The catalog's answer, as "code:number" (e.g. "civil-code:450"): what the analysis can propose today. Kept so
    /// that runs compare with the 26 October baseline; <see cref="Required"/> holds the legal reference when it differs.
    /// </summary>
    public IReadOnlyList<string> ExpectArticles { get; init; } = [];

    /// <summary>
    /// The authorities the law requires for these facts, taken from the official text or a published decision, even
    /// when the library does not hold them yet (those are reported as coverage gaps). Null: same as
    /// <see cref="ExpectArticles"/>.
    /// </summary>
    public IReadOnlyList<string>? Required { get; init; }

    /// <summary>Authorities that may rightly be cited as well; proposing them is not noise.</summary>
    public IReadOnlyList<string> Optional { get; init; } = [];

    /// <summary>Authorities that must not be proposed for these facts (another regime): proposing one is a critical error.</summary>
    public IReadOnlyList<string> Excluded { get; init; } = [];

    /// <summary>Other claim types that are also a right answer.</summary>
    public IReadOnlyList<string> AcceptableClaims { get; init; } = [];

    /// <summary>Decisive facts these facts leave open; a good answer notes them rather than guessing.</summary>
    public IReadOnlyList<string> Unresolved { get; init; } = [];

    /// <summary>How the reference answer was set: "engineering" (written by us, unchecked), "official-text" or "decision".</summary>
    public string ReferenceBasis { get; init; } = "engineering";

    /// <summary>Where the reference answer comes from (article text, decision number), when not "engineering".</summary>
    public string? Source { get; init; }

    /// <summary>"development" (used for tuning), "validation" or "release" (sealed: never looked at while tuning).</summary>
    public string Partition { get; init; } = "development";

    /// <summary>Cases built from the same scenario share a family, so a family never spans two partitions.</summary>
    public string? Family { get; init; }

    public IReadOnlyList<string> LegalReference => Required ?? ExpectArticles;
}

public sealed record GroundsCaseFile
{
    public IReadOnlyList<GroundsCase> Cases { get; init; } = [];
}

public sealed record GroundsCaseResult(GroundsCase Case, GroundsAnalysis Analysis, TimeSpan Elapsed, IReadOnlyList<string> Acceptable)
{
    /// <summary>Why the run failed (the engine errored), or null.</summary>
    public string? Error { get; init; }

    /// <summary>
    /// The articles the model itself chose in its selection answer, as "code:number". The final proposals also hold
    /// the claim type's required articles, added without the model; this shows what the model contributes.
    /// </summary>
    public IReadOnlyList<string> ModelChosen { get; init; } = [];

    public int ModelExpectedFound => Case.ExpectArticles.Count(ModelChosen.Contains);

    /// <summary>A failed run never counts as right, not even as a correct "nothing applies".</summary>
    public bool ClaimFound => Error is null && (Case.Expect.Length == 0 ? Analysis.ClaimIds.Count == 0 : Analysis.ClaimIds.Contains(Case.Expect));

    /// <summary>The expected claim is the first one recognised (or nothing is recognised when nothing applies).</summary>
    public bool ClaimExact => Error is null && (Case.Expect.Length == 0 ? Analysis.ClaimIds.Count == 0 : Analysis.ClaimIds.FirstOrDefault() == Case.Expect);

    /// <summary>A case outside the catalog that the analysis rightly declined: no claim type and no article proposed.</summary>
    public bool Abstained => Error is null && Analysis.ClaimIds.Count == 0 && Selected.Count == 0;

    /// <summary>Proposed articles outside the expected claim's catalog list (noise). For "nothing applies" cases, every proposal.</summary>
    public IReadOnlyList<string> OffClaim => Selected.Where(a => !Acceptable.Contains(a)).ToList();

    public IReadOnlyList<string> Selected => Analysis.Articles.Select(a => $"{a.Code}:{a.Number}").ToList();

    public int ExpectedFound => Case.ExpectArticles.Count(Selected.Contains);

    public IReadOnlyList<string> Unexpected => Selected.Where(s => !Case.ExpectArticles.Contains(s)).ToList();

    /// <summary>Required authorities the library does not hold, so no analysis could propose them (a coverage gap).</summary>
    public IReadOnlyList<string> NotInLibrary { get; init; } = [];

    /// <summary>Required authorities the library holds: the ones an analysis can be blamed for missing.</summary>
    public IReadOnlyList<string> Proposable => Case.LegalReference.Where(a => !NotInLibrary.Contains(a)).ToList();

    public int LegalFound => Proposable.Count(Selected.Contains);

    /// <summary>Proposed authorities the reference excludes for these facts.</summary>
    public IReadOnlyList<string> ExcludedProposed => Selected.Where(Case.Excluded.Contains).ToList();

    /// <summary>The first claim is the expected one or an accepted alternative.</summary>
    public bool ClaimAcceptable => ClaimExact || (Error is null && Analysis.ClaimIds.FirstOrDefault() is { } first && Case.AcceptableClaims.Contains(first));
}

public sealed record GroundsReport(string Model, IReadOnlyList<GroundsCaseResult> Results)
{
    private IReadOnlyList<GroundsCaseResult> Answered => Results.Where(r => r.Error is null).ToList();

    /// <summary>Share of the cases outside the catalog («nothing applies») where the analysis proposed nothing at all.</summary>
    public double OutOfScopeAbstention => Rate(Answered.Where(r => r.Case.Expect.Length == 0).ToList(), r => r.Abstained);

    /// <summary>Share of the cases inside the catalog where the analysis wrongly recognised no claim type.</summary>
    public double FalseAbstention => Rate(Answered.Where(r => r.Case.Expect.Length > 0).ToList(), r => r.Analysis.ClaimIds.Count == 0);

    private static double Rate(List<GroundsCaseResult> cases, Func<GroundsCaseResult, bool> predicate) =>
        cases.Count == 0 ? double.NaN : (double)cases.Count(predicate) / cases.Count;

    /// <summary>Cases in the batch; more than <see cref="Results"/> when the run stopped early.</summary>
    public int Planned { get; init; }

    /// <summary>The run gave up after too many failures in a row (the service is down or keeps refusing).</summary>
    public bool StoppedEarly => Planned > Results.Count;

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

    /// <summary>Share of expected articles the model chose by itself (required articles added by the catalog excluded).</summary>
    public double ModelRecall
    {
        get
        {
            var expected = Results.Sum(r => r.Case.ExpectArticles.Count);
            return expected == 0 ? 0 : (double)Results.Sum(r => r.ModelExpectedFound) / expected;
        }
    }

    public int Failures => Results.Count(r => r.Error is not null);

    /// <summary>Share of the required authorities the library holds that the analysis proposed.</summary>
    public double LegalRecall
    {
        get
        {
            var proposable = Results.Sum(r => r.Proposable.Count);
            return proposable == 0 ? double.NaN : (double)Results.Sum(r => r.LegalFound) / proposable;
        }
    }

    public double ClaimAcceptableRate => Results.Count == 0 ? 0 : (double)Results.Count(r => r.ClaimAcceptable) / Results.Count;

    /// <summary>Distinct required authorities missing from the library, with the cases that need them.</summary>
    public IReadOnlyList<(string Authority, IReadOnlyList<string> Cases)> CoverageGaps =>
        Results.SelectMany(r => r.NotInLibrary.Select(a => (a, r.Case.Name)))
            .GroupBy(x => x.a, StringComparer.Ordinal)
            .Select(g => (g.Key, (IReadOnlyList<string>)g.Select(x => x.Name).ToList()))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

    public int ExcludedProposedCount => Results.Sum(r => r.ExcludedProposed.Count);
}

/// <summary>How much one case's answers vary over repeated runs of the same engine.</summary>
public sealed record CaseStability(string Case, int Runs, double ClaimAgreement, string MostCommonClaim, double ArticleAgreement, double ModelArticleAgreement, int Failures, double ClaimExactRate, double ModelRecall);

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

    /// <param name="maxConsecutiveFailures">
    /// Stop the batch after this many failed cases in a row (0: never), rather than recording a failure for every
    /// remaining case while a service is down.
    /// </param>
    public static async Task<GroundsReport> EvaluateAsync(LegalKnowledge knowledge, IReadOnlyList<GroundsCase> cases, IModelProvider provider, TextWriter log, string? outFolder = null, int maxConsecutiveFailures = 0)
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

        var consecutiveFailures = 0;
        foreach (var c in cases)
        {
            var trace = new List<(string Step, string Text)>();
            var analyzer = new GroundsAnalyzer(provider) { Trace = (step, text) => trace.Add((step, text)) };
            var started = Stopwatch.StartNew();
            GroundsAnalysis analysis;
            string? error = null;
            try
            {
                analysis = await analyzer.AnalyseAsync(knowledge, c.Subject, c.Facts, c.CourtSection);
            }
            catch (Exception ex) when (ex is ModelProviderException or HttpRequestException or IOException or TaskCanceledException)
            {
                // A failed run counts as a failure of the engine, and the batch goes on.
                error = ex.Message;
                analysis = new GroundsAnalysis { FactsHash = string.Empty, Model = provider.Name };
            }

            var acceptable = knowledge.Claims.FirstOrDefault(x => x.Id == c.Expect)?.Articles.Select(a => $"{a.Code}:{a.Number}").ToList() ?? [];
            var result = new GroundsCaseResult(c, analysis, started.Elapsed, [.. acceptable, .. c.Optional])
            {
                Error = error,
                ModelChosen = ModelChosen(trace),
                NotInLibrary = c.LegalReference.Where(a => !InLibrary(knowledge, a)).ToList(),
            };
            log.WriteLine(error is not null ? $"  [{c.Name}] FAILED: {error}" : $"  [{c.Name}] {started.Elapsed.TotalSeconds:F0}s  claim: {(analysis.ClaimIds.Count == 0 ? "—" : string.Join(", ", analysis.ClaimIds))}  articles: {(result.Selected.Count == 0 ? "—" : string.Join(", ", result.Selected))}  ({result.ExpectedFound}/{c.ExpectArticles.Count} expected, {result.OffClaim.Count} off-claim)");
            results.Add(result);
            if (modelFolder is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(modelFolder, Safe(c.Name) + ".md"), CaseReport(result, trace));
            }

            consecutiveFailures = error is null ? 0 : consecutiveFailures + 1;
            if (maxConsecutiveFailures > 0 && consecutiveFailures >= maxConsecutiveFailures && results.Count < cases.Count)
            {
                log.WriteLine($"  stopped: the engine failed {consecutiveFailures} cases in a row (last error: {error})");
                break;
            }
        }

        var report = new GroundsReport(provider.Name, results) { Planned = cases.Count };
        if (outFolder is not null)
        {
            await File.AppendAllTextAsync(Path.Combine(outFolder, "summary.md"), Summary(report, knowledge));
        }

        return report;
    }

    /// <summary>The authority ("code:number") has its official text in the library.</summary>
    public static bool InLibrary(LegalKnowledge knowledge, string authority)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(authority);
        var parts = authority.Split(':', 2);
        return parts.Length == 2 && knowledge.Library.FindArticle(parts[0], parts[1]) is { Repealed: false };
    }

    /// <summary>
    /// The candidate keys the model wrote in its selection answer ("civil-code:450 | …"), read from the trace: only keys
    /// that were offered as candidates count.
    /// </summary>
    public static IReadOnlyList<string> ModelChosen(IReadOnlyList<(string Step, string Text)> trace)
    {
        var candidates = trace.Where(t => t.Step == "candidates")
            .SelectMany(t => t.Text.Split('\n'))
            .Select(l => l.Split(" — ")[0].Trim())
            .Where(k => k.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var answer = trace.LastOrDefault(t => t.Step == "selection.answer").Text ?? string.Empty;
        return answer.Split('\n')
            .Select(l => ArabicScript.ToAsciiDigits(l.Split('|')[0]).Trim().Trim('-', '*', '•', '.', ' ').ToLowerInvariant())
            .Where(candidates.Contains)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Agreement across repeated runs of the same cases: how often the most common claim type came back, and the mean
    /// pairwise overlap (Jaccard) of the proposed article sets, final and model-chosen.
    /// </summary>
    public static IReadOnlyList<CaseStability> Stability(IReadOnlyList<GroundsReport> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);
        return runs.SelectMany(r => r.Results)
            .GroupBy(r => r.Case.Name)
            .Select(g =>
            {
                var all = g.ToList();
                var failures = all.Count(r => r.Error is not null);

                // Failed runs are left out: an engine that always fails must read "n/a", not perfectly stable.
                var results = all.Where(r => r.Error is null).ToList();
                if (results.Count == 0)
                {
                    return new CaseStability(g.Key, all.Count, double.NaN, "n/a", double.NaN, double.NaN, failures, double.NaN, double.NaN);
                }

                var claims = results.Select(r => r.Analysis.ClaimIds.FirstOrDefault() ?? "—").ToList();
                var top = claims.GroupBy(x => x).OrderByDescending(x => x.Count()).First();
                var expected = results[0].Case.ExpectArticles.Count;
                return new CaseStability(
                    g.Key,
                    all.Count,
                    (double)top.Count() / results.Count,
                    top.Key,
                    MeanJaccard(results.Select(r => r.Selected).ToList()),
                    MeanJaccard(results.Select(r => r.ModelChosen).ToList()),
                    failures,
                    (double)results.Count(r => r.ClaimExact) / results.Count,
                    expected == 0 ? 0 : results.Average(r => (double)r.ModelExpectedFound / expected));
            })
            .ToList();
    }

    private static double MeanJaccard(List<IReadOnlyList<string>> sets)
    {
        if (sets.Count < 2)
        {
            return 1;
        }

        var scores = new List<double>();
        for (var i = 0; i < sets.Count; i++)
        {
            for (var j = i + 1; j < sets.Count; j++)
            {
                var union = sets[i].Union(sets[j]).Count();
                scores.Add(union == 0 ? 1 : (double)sets[i].Intersect(sets[j]).Count() / union);
            }
        }

        return scores.Average();
    }

    /// <summary>The stability table for summary.md.</summary>
    public static string StabilityTable(string model, IReadOnlyList<CaseStability> stability)
    {
        ArgumentNullException.ThrowIfNull(stability);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Ci, $"## Stability over {(stability.Count > 0 ? stability[0].Runs : 0)} runs — {model}").AppendLine();
        sb.AppendLine("Claim agreement: share of runs giving the most common claim type. Article agreement: mean overlap (Jaccard) of the proposed sets between runs; 100% = identical every time. Model: the articles the model chose itself.").AppendLine();
        sb.AppendLine("| case | most common claim | claim agreement | claim exact | article agreement | model article agreement | model recall | failures |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
        foreach (var s in stability)
        {
            sb.AppendLine(Ci, $"| {s.Case} | {s.MostCommonClaim} | {P(s.ClaimAgreement)} | {P(s.ClaimExactRate)} | {P(s.ArticleAgreement)} | {P(s.ModelArticleAgreement)} | {P(s.ModelRecall)} | {s.Failures} |");
        }

        if (stability.Count > 0)
        {
            sb.AppendLine(Ci, $"| **all** | | {P(Mean(stability, s => s.ClaimAgreement))} | {P(Mean(stability, s => s.ClaimExactRate))} | {P(Mean(stability, s => s.ArticleAgreement))} | {P(Mean(stability, s => s.ModelArticleAgreement))} | {P(Mean(stability, s => s.ModelRecall))} | {stability.Sum(s => s.Failures)} |");
        }

        return sb.AppendLine().ToString();
    }

    /// <summary>A percentage, or "n/a" when nothing was measured (every run failed, or no case of that kind).</summary>
    public static string P(double value) => double.IsNaN(value) ? "n/a" : value.ToString("P0", Ci);

    private static double Mean(IReadOnlyList<CaseStability> stability, Func<CaseStability, double> metric)
    {
        var measured = stability.Select(metric).Where(v => !double.IsNaN(v)).ToList();
        return measured.Count == 0 ? double.NaN : measured.Average();
    }

    private static string CaseReport(GroundsCaseResult r, List<(string Step, string Text)> trace)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Ci, $"# {r.Case.Name} — {r.Analysis.Model}").AppendLine();
        sb.AppendLine(Ci, $"- Court section: {r.Case.CourtSection ?? "—"}");
        sb.AppendLine(Ci, $"- Expected claim: {(r.Case.Expect.Length == 0 ? "none" : r.Case.Expect)} · expected articles: {string.Join(", ", r.Case.ExpectArticles)}");
        if (r.Case.Required is not null || r.Case.Excluded.Count > 0 || r.Case.Unresolved.Count > 0)
        {
            sb.AppendLine(Ci, $"- Legal reference ({r.Case.ReferenceBasis}{(r.Case.Source is null ? string.Empty : ": " + r.Case.Source)}): required {string.Join(", ", r.Case.LegalReference)} · not in library: {(r.NotInLibrary.Count == 0 ? "—" : string.Join(", ", r.NotInLibrary))} · excluded: {(r.Case.Excluded.Count == 0 ? "—" : string.Join(", ", r.Case.Excluded))}");
            foreach (var open in r.Case.Unresolved)
            {
                sb.AppendLine(Ci, $"- Left open by the facts: {open}");
            }
        }
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
        if (report.StoppedEarly)
        {
            sb.AppendLine(Ci, $"**Stopped early after {report.Results.Count} of {report.Planned} cases**: the engine failed several cases in a row. The figures below cover only those cases.").AppendLine();
        }

        sb.AppendLine("| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        sb.AppendLine(Ci, $"| {report.ClaimExactRate:P0} | {report.ClaimAccuracy:P0} | {report.ArticleRecall:P0} | {report.ModelRecall:P0} | {report.OffClaimShare:P0} | {P(report.OutOfScopeAbstention)} | {P(report.FalseAbstention)} | {report.ArticlesPerCase:F1} | {report.EmptyWhenSomethingExpected} | {report.Failures} | {report.AverageTime.TotalSeconds:F0} |").AppendLine();
        if (knowledge.Library.Codes.Count == 0)
        {
            sb.AppendLine("No official text imported: only catalog articles could be proposed.").AppendLine();
        }

        sb.AppendLine("Legal reference (the authorities the law requires, which may differ from the catalog's answer):").AppendLine();
        sb.AppendLine("| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine(Ci, $"| {report.ClaimAcceptableRate:P0} | {P(report.LegalRecall)} | {report.CoverageGaps.Count} | {report.ExcludedProposedCount} |").AppendLine();
        foreach (var (authority, cases) in report.CoverageGaps)
        {
            sb.AppendLine(Ci, $"- Coverage gap: `{authority}` (needed by {string.Join(", ", cases)})");
        }

        if (report.CoverageGaps.Count > 0)
        {
            sb.AppendLine();
        }

        foreach (var r in report.Results)
        {
            var gaps = r.NotInLibrary.Count > 0 ? $" (+{r.NotInLibrary.Count} not in library)" : string.Empty;
            var legal = r.Case.Required is null ? string.Empty : $" · legal {r.LegalFound}/{r.Proposable.Count}{gaps}";
            var excluded = r.ExcludedProposed.Count == 0 ? string.Empty : $" · **excluded proposed: {string.Join(", ", r.ExcludedProposed)}**";
            sb.AppendLine(Ci, $"- {(r.ClaimExact ? "✓" : r.ClaimAcceptable ? "≈" : "✗")} `{r.Case.Name}` expected {(r.Case.Expect.Length == 0 ? "none" : r.Case.Expect)} → {(r.Analysis.ClaimIds.Count == 0 ? "—" : string.Join(", ", r.Analysis.ClaimIds))} · {r.ExpectedFound}/{r.Case.ExpectArticles.Count} core{legal} · {r.OffClaim.Count} off-claim{excluded} · {r.Elapsed.TotalSeconds:F0}s");
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
        output.WriteLine(string.Format(ci, "{0}: claim exact {1:P0} · claim found {2:P0} · core recall {3:P0} · model recall {8:P0} · off-claim {4:P0} · out-of-scope abstained {10} · false abstention {11} · {5:F1} articles/case · {6} empty · {9} failed · {7:F0}s per case", report.Model, report.ClaimExactRate, report.ClaimAccuracy, report.ArticleRecall, report.OffClaimShare, report.ArticlesPerCase, report.EmptyWhenSomethingExpected, report.AverageTime.TotalSeconds, report.ModelRecall, report.Failures, P(report.OutOfScopeAbstention), P(report.FalseAbstention)));
        if (report.StoppedEarly)
        {
            output.WriteLine(string.Format(ci, "  Stopped early after {0} of {1} cases.", report.Results.Count, report.Planned));
        }

        if (knowledge.Library.Codes.Count == 0)
        {
            output.WriteLine("  Note: no official text is imported yet, so only catalog articles can be proposed and library search finds nothing.");
        }
    }
}
