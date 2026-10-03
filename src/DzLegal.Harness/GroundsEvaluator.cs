using System.Text.Json;
using DzLegal.Domain.Serialization;
using DzLegal.Law;

namespace DzLegal.Harness;

public sealed record GroundsCase
{
    public required string Name { get; init; }

    public string? CourtSection { get; init; }

    public required string Facts { get; init; }

    /// <summary>The claim type a lawyer would start from; empty when the catalog does not cover the case.</summary>
    public required string Expect { get; init; }
}

public sealed record GroundsCaseFile
{
    public IReadOnlyList<GroundsCase> Cases { get; init; } = [];
}

public sealed record GroundsCaseResult(GroundsCase Case, IReadOnlyList<string> Suggested)
{
    public bool ExpectsNothing => Case.Expect.Length == 0;

    public bool Top1 => ExpectsNothing ? Suggested.Count == 0 : Suggested.Count > 0 && Suggested[0] == Case.Expect;

    public bool Top3 => ExpectsNothing ? Suggested.Count == 0 : Suggested.Contains(Case.Expect);
}

public sealed record GroundsReport(IReadOnlyList<GroundsCaseResult> Results)
{
    /// <summary>The CI gate: the expected claim must be among the first three suggestions in at least 90% of cases.</summary>
    public const double RequiredTop3 = 0.9;

    public double Top1 => Results.Count == 0 ? 0 : (double)Results.Count(r => r.Top1) / Results.Count;

    public double Top3 => Results.Count == 0 ? 0 : (double)Results.Count(r => r.Top3) / Results.Count;

    public bool Passes => Results.Count > 0 && Top3 >= RequiredTop3;
}

/// <summary>Measures the grounds suggester on fact patterns with a known starting claim.</summary>
public static class GroundsEvaluator
{
    public static IReadOnlyList<GroundsCase> LoadCases(string folder) =>
        Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal)
            .SelectMany(f => JsonSerializer.Deserialize<GroundsCaseFile>(File.ReadAllText(f), DefinitionJson.Options)?.Cases ?? [])
            .ToList();

    public static GroundsReport Evaluate(LegalKnowledge knowledge, IReadOnlyList<GroundsCase> cases)
    {
        var suggester = new GroundsSuggester(knowledge);
        return new GroundsReport(cases
            .Select(c => new GroundsCaseResult(c, suggester.Suggest(c.Facts, GroundsSuggester.AreaOf(c.CourtSection)).Select(s => s.Claim.Id).ToList()))
            .ToList());
    }

    /// <summary>
    /// Scores the model fallback: for each case the model chooses claim ids from the catalog (restricted to the
    /// case's area, like the app), and its picks are scored like keyword suggestions.
    /// </summary>
    public static async Task<GroundsReport> EvaluateModelAsync(LegalKnowledge knowledge, IReadOnlyList<GroundsCase> cases, DzLegal.Drafting.IModelProvider provider, TextWriter log)
    {
        var picker = new DzLegal.Drafting.ClaimPicker(provider);
        var results = new List<GroundsCaseResult>();
        foreach (var c in cases)
        {
            var area = GroundsSuggester.AreaOf(c.CourtSection);
            var options = knowledge.Claims.Where(x => area is null || x.Area == area).Select(x => new DzLegal.Drafting.ClaimOption(x.Id, x.Label.Ar)).ToList();
            var started = System.Diagnostics.Stopwatch.StartNew();
            var picks = await picker.PickAsync(knowledge.ClaimPickPrompt, string.Empty, c.Facts, options);
            log.WriteLine($"  [{c.Name}] {started.Elapsed.TotalSeconds:F0}s → {(picks.Count == 0 ? "nothing" : string.Join(", ", picks))}");
            results.Add(new GroundsCaseResult(c, picks));
        }

        return new GroundsReport(results);
    }

    public static void Print(GroundsReport report, TextWriter output)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        output.WriteLine($"Grounds suggestions — {report.Results.Count} cases");
        foreach (var r in report.Results.Where(r => !r.Top1))
        {
            output.WriteLine($"  {(r.Top3 ? "not first" : "MISSED   ")} [{r.Case.Name}] expected {(r.ExpectsNothing ? "nothing" : r.Case.Expect)}, got {(r.Suggested.Count == 0 ? "nothing" : string.Join(", ", r.Suggested))}");
        }

        output.WriteLine(string.Format(ci, "Top-1: {0:P0}   Top-3: {1:P0} (required {2:P0})", report.Top1, report.Top3, GroundsReport.RequiredTop3));
    }
}
