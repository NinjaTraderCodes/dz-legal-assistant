using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;

namespace DzLegal.Harness;

public sealed record GoldEvent
{
    /// <summary>The event kind, or accepted alternatives separated by «|».</summary>
    public required string Kind { get; init; }

    public string? Date { get; init; }

    public string? Amount { get; init; }

    /// <summary>Who did it, when it matters (plaintiff or defendant): a swap is a critical error.</summary>
    public string? Actor { get; init; }

    public string Polarity { get; init; } = FactPolarity.Affirmed;

    /// <summary>A correct reading must find it; missing it is a critical error.</summary>
    public bool Decisive { get; init; } = true;
}

/// <summary>What must never be read: the event as affirmed, or (with a value) that value bound to that event.</summary>
public sealed record GoldForbidden
{
    public required string Kind { get; init; }

    public string? Date { get; init; }

    public string? Amount { get; init; }
}

public sealed record GoldRequest
{
    public required string Kind { get; init; }

    public string? Amount { get; init; }
}

public sealed record FactGold
{
    public string Relation { get; init; } = string.Empty;

    public string Position { get; init; } = string.Empty;

    public IReadOnlyList<GoldEvent> Events { get; init; } = [];

    public IReadOnlyList<GoldForbidden> Forbidden { get; init; } = [];

    public IReadOnlyList<GoldRequest> Requests { get; init; } = [];
}

public sealed record FactCase
{
    public required string Name { get; init; }

    public string Partition { get; init; } = "development";

    public string Family { get; init; } = string.Empty;

    /// <summary>The catalog claim type the case falls under, as the lawyer would confirm it; <see cref="OutOfScope"/> when none does.</summary>
    public string Claim { get; init; } = string.Empty;

    public const string OutOfScope = "out-of-scope";

    public Dictionary<string, string> Inputs { get; init; } = [];

    public required FactGold Gold { get; init; }
}

public sealed record FactCaseFile
{
    public IReadOnlyList<FactCase> Cases { get; init; } = [];
}

/// <summary>One error found when comparing a reading with the answer key.</summary>
/// <param name="Critical">Would make the draft or the legal decision wrong: a reversed role, a missing decisive event, a
/// date or amount bound to the wrong event, a denial read as a fact, an invented value.</param>
public sealed record FactError(string Type, string Detail, bool Critical);

/// <summary>What a reader returned for one case. <see cref="NotRead"/>: no claim type to read the case against, so only
/// <see cref="ClaimErrors"/> are scored.</summary>
public sealed record FactReading(CaseFacts Facts, string Answer)
{
    public string Claim { get; init; } = string.Empty;

    public IReadOnlyList<FactError> ClaimErrors { get; init; } = [];

    public bool NotRead { get; init; }

    /// <summary>Outside the catalog with the claim type given: the app abstains, nothing to measure; left out of the tables.</summary>
    public bool Excluded { get; init; }
}

public sealed record FactCaseResult(FactCase Case, CaseFacts Facts, string Answer, TimeSpan Elapsed, IReadOnlyList<FactError> Errors)
{
    public string? Failure { get; init; }

    public string Claim { get; init; } = string.Empty;

    public bool Excluded { get; init; }

    /// <summary>No reading to score (the claim type was not recognised, or rightly or wrongly outside the catalog).</summary>
    public bool NotRead { get; init; }

    public bool HasCritical => Failure is not null || Errors.Any(e => e.Critical);
}

public sealed record FactsReport(string Reader, IReadOnlyList<FactCaseResult> Results)
{
    public int CasesWithCritical => Results.Count(r => r.HasCritical);

    public int Failures => Results.Count(r => r.Failure is not null);

    public IReadOnlyDictionary<string, int> ErrorsByType(bool critical) =>
        Results.SelectMany(r => r.Errors).Where(e => e.Critical == critical).GroupBy(e => e.Type).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count());

    public double Rate(Func<FactCaseResult, (int Hit, int Of)> score)
    {
        var (hit, of) = Results.Where(r => r.Failure is null && !r.NotRead).Select(score).Aggregate((0, 0), (a, b) => (a.Item1 + b.Hit, a.Item2 + b.Of));
        return of == 0 ? double.NaN : (double)hit / of;
    }

    public TimeSpan AverageTime => Results.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)Results.Average(r => r.Elapsed.Ticks));
}

/// <summary>
/// Measures a fact reader against the answer key (tests/fact-cases): roles, decisive events, the binding of dates and
/// amounts to their events, polarity, requests and invented values. Critical errors are counted per case and never
/// averaged away. Readers outside .NET (an extraction encoder in Python, say) are scored from a predictions file.
/// </summary>
public static class FactsEvaluator
{
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    // Arabic kept readable in the exported file (it is read by people and by the external reader, never by a browser).
    private static readonly JsonSerializerOptions ExportOptions = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static IReadOnlyList<FactCase> LoadCases(string folder) =>
        Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal)
            .SelectMany(f => JsonSerializer.Deserialize<FactCaseFile>(File.ReadAllText(f), DefinitionJson.Options)?.Cases ?? [])
            .ToList();

    public static Task<FactsReport> RunAsync(IReadOnlyList<FactCase> cases, IModelProvider provider, string prompt, TextWriter log, string? outFolder = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var reader = new FactReader(provider, prompt);
        return RunAsync(cases, provider.Name, async (_, text, ct) =>
        {
            var (facts, answer) = await reader.ReadAsync(text, ct);
            return new FactReading(facts, answer);
        }, log, outFolder);
    }

    /// <summary>Runs any reader over the cases and scores each reading as it comes.</summary>
    public static async Task<FactsReport> RunAsync(IReadOnlyList<FactCase> cases, string readerName, Func<FactCase, CaseText, CancellationToken, Task<FactReading>> read, TextWriter log, string? outFolder = null)
    {
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(log);
        var results = new List<FactCaseResult>();
        foreach (var c in cases)
        {
            var text = CaseText.From(c.Inputs);
            var clock = Stopwatch.StartNew();
            FactCaseResult result;
            try
            {
                var reading = await read(c, text, CancellationToken.None);
                var errors = reading.NotRead ? reading.ClaimErrors : [.. reading.ClaimErrors, .. Score(c.Gold, reading.Facts)];
                result = new FactCaseResult(c, reading.Facts, reading.Answer, clock.Elapsed, errors) { Claim = reading.Claim, Excluded = reading.Excluded, NotRead = reading.NotRead };
            }
            catch (Exception ex) when (ex is ModelProviderException or HttpRequestException or IOException or TaskCanceledException)
            {
                result = new FactCaseResult(c, new CaseFacts(), string.Empty, clock.Elapsed, []) { Failure = ex.Message };
            }

            var status = result.Failure is not null ? "FAILED: " + result.Failure
                : result.Excluded ? "outside the catalogue (not read)"
                : result.HasCritical ? "critical: " + string.Join("; ", result.Errors.Where(e => e.Critical).Select(e => e.Type))
                : "ok";
            log.WriteLine($"  [{c.Name}] {result.Elapsed.TotalSeconds:F0}s  {status}");
            results.Add(result);
            await WriteCaseAsync(outFolder, readerName, result, text);
        }

        return new FactsReport(readerName, results);
    }

    /// <summary>
    /// Scores readings produced elsewhere: a JSON object keyed by case name, each value either {"answer": "coded lines"}
    /// or {"relation", "position", "events": [...], "requests": [...]} in the same vocabulary.
    /// </summary>
    public static FactsReport ScorePredictions(IReadOnlyList<FactCase> cases, string reader, string predictionsFile)
    {
        ArgumentNullException.ThrowIfNull(cases);
        var predictions = JsonNode.Parse(File.ReadAllText(predictionsFile))?.AsObject() ?? [];
        var results = new List<FactCaseResult>();
        foreach (var c in cases)
        {
            var text = CaseText.From(c.Inputs);
            if (predictions[c.Name] is not JsonObject p)
            {
                results.Add(new FactCaseResult(c, new CaseFacts(), string.Empty, TimeSpan.Zero, []) { Failure = "no prediction" });
                continue;
            }

            var answer = p["answer"]?.GetValue<string>() ?? ToLines(p);
            var seconds = p["seconds"]?.GetValue<double>() ?? 0;
            var facts = FactReader.Parse(answer, text);
            results.Add(new FactCaseResult(c, facts, answer, TimeSpan.FromSeconds(seconds), Score(c.Gold, facts)));
        }

        return new FactsReport(reader, results);
    }

    /// <summary>The cases as an external reader needs them: numbered sentences and the court section, no answers.</summary>
    public static string Export(IReadOnlyList<FactCase> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);
        var list = cases.Select(c =>
        {
            var text = CaseText.From(c.Inputs);
            return new
            {
                name = c.Name,
                partition = c.Partition,
                courtSection = text.CourtSection,
                sentences = text.Sentences.Select(s => new { id = s.Id, field = s.FieldId, text = s.Text }),
            };
        });
        return JsonSerializer.Serialize(list, ExportOptions);
    }

    private static string ToLines(JsonObject p)
    {
        static string S(JsonNode? n) => n is null ? "-" : n is JsonArray a ? string.Join(',', a.Select(x => x?.GetValue<string>())) : n.GetValue<string>() is { Length: > 0 } s ? s : "-";
        var lines = new List<string> { $"علاقة | {S(p["relation"])} | {S(p["position"])} | -" };
        lines.AddRange((p["events"] as JsonArray ?? []).OfType<JsonObject>().Select(e =>
            $"حدث | {S(e["kind"])} | {S(e["date"])} | {S(e["amount"])} | {S(e["actor"])} | {S(e["polarity"] ?? FactPolarity.Affirmed)} | {S(e["sentences"])}"));
        lines.AddRange((p["requests"] as JsonArray ?? []).OfType<JsonObject>().Select(r => $"طلب | {S(r["kind"])} | {S(r["amount"])} | {S(r["sentences"])}"));
        return string.Join('\n', lines);
    }

    private static string[] Alternatives(string value) => value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Every difference between a reading and the answer key, critical or not.</summary>
    public static IReadOnlyList<FactError> Score(FactGold gold, CaseFacts facts)
    {
        ArgumentNullException.ThrowIfNull(gold);
        ArgumentNullException.ThrowIfNull(facts);
        var errors = new List<FactError>();
        if (facts.Relation.Length == 0)
        {
            errors.Add(new("no relation", "no readable relation line", true));
        }
        else
        {
            if (gold.Relation.Length > 0 && !Alternatives(gold.Relation).Contains(facts.Relation))
            {
                errors.Add(new("relation", $"{facts.Relation} instead of {gold.Relation}", false));
            }

            if (gold.Position.Length > 0 && !Alternatives(gold.Position).Contains(facts.Position))
            {
                errors.Add(new("plaintiff position", $"{facts.Position} instead of {gold.Position}", true));
            }
        }

        foreach (var g in gold.Events)
        {
            var kinds = Alternatives(g.Kind);
            var found = facts.Events.Where(e => kinds.Contains(e.Kind)).ToList();
            if (found.Count == 0)
            {
                errors.Add(new(g.Decisive ? "missing decisive event" : "missing event", g.Kind, g.Decisive));
                continue;
            }

            var best = found.OrderByDescending(e => (g.Date is not null && e.Date?.Normalized == FactValues.NormalizeDate(g.Date) ? 2 : 0) + (g.Amount is not null && e.Amount?.Normalized == g.Amount ? 2 : 0) + (e.Polarity == g.Polarity ? 1 : 0)).First();
            if (best.Polarity != g.Polarity)
            {
                errors.Add(new("polarity", $"{g.Kind}: {best.Polarity} instead of {g.Polarity}", true));
            }

            if (g.Date is not null)
            {
                var expected = FactValues.NormalizeDate(g.Date);
                if (best.Date is null)
                {
                    errors.Add(new("date not given", $"{g.Kind}: {expected}", false));
                }
                else if (best.Date.Normalized != expected)
                {
                    errors.Add(new("date on wrong event", $"{g.Kind}: {best.Date.Text} instead of {expected}", true));
                }
            }

            if (g.Amount is not null)
            {
                if (best.Amount is null)
                {
                    errors.Add(new("amount not given", $"{g.Kind}: {g.Amount}", false));
                }
                else if (best.Amount.Normalized != g.Amount)
                {
                    errors.Add(new("amount on wrong event", $"{g.Kind}: {best.Amount.Text} instead of {g.Amount}", true));
                }
            }

            if (g.Actor is "plaintiff" or "defendant" && best.Actor is "plaintiff" or "defendant" && best.Actor != g.Actor)
            {
                errors.Add(new("actor swapped", $"{g.Kind}: {best.Actor} instead of {g.Actor}", true));
            }
        }

        foreach (var f in gold.Forbidden)
        {
            var kinds = Alternatives(f.Kind);
            foreach (var e in facts.Events.Where(e => kinds.Contains(e.Kind)))
            {
                var withValue = f.Date is not null || f.Amount is not null;
                var hit = withValue
                    ? (f.Date is not null && e.Date?.Normalized == FactValues.NormalizeDate(f.Date)) || (f.Amount is not null && e.Amount?.Normalized == f.Amount)
                    : e.Polarity == FactPolarity.Affirmed;
                if (hit)
                {
                    errors.Add(new("forbidden reading", withValue ? $"{e.Kind} with {f.Date ?? f.Amount}" : $"{e.Kind} affirmed", true));
                }
            }
        }

        foreach (var v in facts.Values)
        {
            if (v.Check == ValueCheck.NotInCase)
            {
                errors.Add(new("invented value", v.Text, true));
            }
            else if (v.Check == ValueCheck.ElsewhereInCase)
            {
                errors.Add(new("value cited from the wrong sentence", v.Text, false));
            }
        }

        foreach (var g in gold.Requests)
        {
            var kinds = Alternatives(g.Kind);
            var found = facts.Requests.FirstOrDefault(r => kinds.Contains(r.Kind));
            if (found is null)
            {
                errors.Add(new("missing request", g.Kind, false));
            }
            else if (g.Amount is not null && found.Amount?.Normalized != g.Amount)
            {
                errors.Add(new("request amount", $"{g.Kind}: {found.Amount?.Text ?? "-"} instead of {g.Amount}", true));
            }
        }

        if (facts.Rejected.Count > 0)
        {
            errors.Add(new("unreadable lines", facts.Rejected.Count.ToString(Ci), false));
        }

        return errors;
    }

    /// <summary>summary.md: one table per partition (development, validation, robustness), then the errors.</summary>
    public static string Summary(FactsReport report, string? timings = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        var sb = new StringBuilder();
        sb.AppendLine(Ci, $"## {report.Reader}").AppendLine();
        sb.AppendLine("A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.").AppendLine();
        sb.AppendLine("| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var part in new[] { "development", "validation", "robustness", "release" })
        {
            var subset = new FactsReport(report.Reader, report.Results.Where(r => r.Case.Partition == part && !r.Excluded).ToList());
            if (subset.Results.Count == 0)
            {
                continue;
            }

            sb.AppendLine(Ci, $"| {part} | {subset.Results.Count} | {subset.CasesWithCritical} | {P(subset.Rate(PositionScore))} | {P(subset.Rate(DecisiveScore))} | {P(subset.Rate(DateScore))} | {P(subset.Rate(PolarityScore))} | {P(subset.Rate(RequestScore))} | {subset.Failures} | {subset.AverageTime.TotalSeconds:F0} |");
        }

        sb.AppendLine().AppendLine("Critical errors: " + Counts(report.ErrorsByType(critical: true)));
        sb.AppendLine().AppendLine("Other differences: " + Counts(report.ErrorsByType(critical: false))).AppendLine();
        var excluded = report.Results.Count(r => r.Excluded);
        if (excluded > 0)
        {
            sb.AppendLine(Ci, $"{excluded} case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.").AppendLine();
        }

        foreach (var r in report.Results)
        {
            var mark = r.Failure is not null ? "✗ failed" : r.Excluded ? "–" : r.HasCritical ? "✗" : "✓";
            var detail = r.Failure ?? string.Join("; ", r.Errors.Where(e => e.Critical).Select(e => $"{e.Type} ({e.Detail})"));
            var claim = r.Claim.Length > 0 ? $" ({r.Claim})" : string.Empty;
            sb.AppendLine(Ci, $"- {mark} `{r.Case.Name}`{claim} [{r.Case.Partition}] {r.Elapsed.TotalSeconds:F0}s{(detail.Length > 0 ? " · " + detail : string.Empty)}");
        }

        if (timings is not null)
        {
            sb.AppendLine().AppendLine(Ci, $"### Where the time goes ({report.Reader})").AppendLine().AppendLine(timings);
        }

        return sb.AppendLine().ToString();
    }

    private static string Counts(IReadOnlyDictionary<string, int> counts) =>
        counts.Count == 0 ? "none" : string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}"));

    private static string P(double v) => double.IsNaN(v) ? "n/a" : v.ToString("P0", Ci);

    private static (int, int) PositionScore(FactCaseResult r) =>
        r.Case.Gold.Position.Length == 0 ? (0, 0) : (Alternatives(r.Case.Gold.Position).Contains(r.Facts.Position) ? 1 : 0, 1);

    private static (int, int) DecisiveScore(FactCaseResult r)
    {
        var decisive = r.Case.Gold.Events.Where(g => g.Decisive).ToList();
        return (decisive.Count(g => r.Facts.Events.Any(e => Alternatives(g.Kind).Contains(e.Kind))), decisive.Count);
    }

    private static (int, int) DateScore(FactCaseResult r)
    {
        var dated = r.Case.Gold.Events.Where(g => g.Date is not null && r.Facts.Events.Any(e => Alternatives(g.Kind).Contains(e.Kind))).ToList();
        return (dated.Count(g => r.Facts.Events.Any(e => Alternatives(g.Kind).Contains(e.Kind) && e.Date?.Normalized == FactValues.NormalizeDate(g.Date))), dated.Count);
    }

    private static (int, int) PolarityScore(FactCaseResult r)
    {
        var found = r.Case.Gold.Events.Where(g => r.Facts.Events.Any(e => Alternatives(g.Kind).Contains(e.Kind))).ToList();
        return (found.Count(g => r.Facts.Events.Any(e => Alternatives(g.Kind).Contains(e.Kind) && e.Polarity == g.Polarity)), found.Count);
    }

    private static (int, int) RequestScore(FactCaseResult r) =>
        (r.Case.Gold.Requests.Count(g => r.Facts.Requests.Any(q => Alternatives(g.Kind).Contains(q.Kind))), r.Case.Gold.Requests.Count);

    private static async Task WriteCaseAsync(string? outFolder, string reader, FactCaseResult r, CaseText text)
    {
        if (outFolder is null)
        {
            return;
        }

        var folder = Path.Combine(outFolder, string.Concat(reader.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_')));
        Directory.CreateDirectory(folder);
        var sb = new StringBuilder().AppendLine(Ci, $"# {r.Case.Name} — {reader}").AppendLine();
        if (r.Claim.Length > 0)
        {
            sb.AppendLine(Ci, $"Claim type read against: {r.Claim} (expected: {r.Case.Claim})").AppendLine();
        }

        sb.AppendLine("## Case").AppendLine().AppendLine("```").AppendLine(text.Render()).AppendLine("```").AppendLine();
        sb.AppendLine("## Answer").AppendLine().AppendLine("```").AppendLine(r.Failure ?? r.Answer).AppendLine("```").AppendLine();
        sb.AppendLine("## Errors").AppendLine();
        foreach (var e in r.Errors)
        {
            sb.AppendLine(Ci, $"- {(e.Critical ? "**critical**" : "minor")} {e.Type}: {e.Detail}");
        }

        await File.WriteAllTextAsync(Path.Combine(folder, r.Case.Name + ".md"), sb.ToString());
    }
}
