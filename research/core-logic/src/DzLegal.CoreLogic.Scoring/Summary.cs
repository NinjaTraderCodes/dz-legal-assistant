using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Scoring;

/// <summary>One answer line: <c>{ "id": "&lt;note id&gt;", "reading": &lt;NoteReading&gt; }</c>, read with the contract's strict options.</summary>
public sealed record AnswerLine
{
    public required string Id { get; init; }
    public required NoteReading Reading { get; init; }
}

public static class Answers
{
    /// <summary>
    /// The answers of one setting, by note id. An id that is not in the gold, or two answers for one id, is a fixed error;
    /// the message never holds the id.
    /// </summary>
    public static IReadOnlyDictionary<string, NoteReading> Load(string path, IReadOnlySet<string> goldIds)
    {
        var answers = new Dictionary<string, NoteReading>(StringComparer.Ordinal);
        var number = 0;
        foreach (var line in File.ReadLines(path))
        {
            number++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            AnswerLine answer;
            try
            {
                answer = JsonSerializer.Deserialize<AnswerLine>(line, ContractJson.Options)
                    ?? throw new ScoreException("A01", $"answer line {number.ToString(CultureInfo.InvariantCulture)} is empty");
            }
            catch (JsonException)
            {
                throw new ScoreException("A02", $"answer line {number.ToString(CultureInfo.InvariantCulture)} is not a reading under the contract");
            }

            if (!goldIds.Contains(answer.Id))
            {
                throw new ScoreException("A03", $"answer line {number.ToString(CultureInfo.InvariantCulture)} names a note that is not in the gold");
            }

            if (!answers.TryAdd(answer.Id, answer.Reading))
            {
                throw new ScoreException("A04", $"answer line {number.ToString(CultureInfo.InvariantCulture)} answers a note a second time");
            }
        }

        return answers;
    }
}

/// <summary>
/// summary.md and summary.json: numbers only. No note text, no quote, no note id, no per-note line; every count of a
/// table under 3 is merged into "other" (specs/scorer.md, "summary.md for a hidden set"). Ordering is fixed and nothing
/// holds a timestamp, so the same inputs give the same bytes.
/// </summary>
public static class Summary
{
    public const int SmallCell = 3;

    public static string Markdown(ScoreReport report, string setName, string scorerHash, string? firstLine = null)
    {
        var b = new StringBuilder();
        if (firstLine is not null)
        {
            b.Append(firstLine).Append("\n\n");
        }

        b.Append("# Score: ").Append(setName).Append("\n\n")
            .Append("- Notes: ").Append(I(report.Notes)).Append("; answers missing (scored as a full abstention): ").Append(I(report.AnswersMissing)).Append('\n')
            .Append("- Scorer: ").Append(scorerHash).Append("\n\n");

        b.Append("## Targets\n\n| Target | Goal | Headline setting | Verdict |\n|---|---:|---|---|\n");
        foreach (var t in report.Targets)
        {
            b.Append("| ").Append(t.Target.Measure).Append(" | ").Append(P(t.Target.Goal)).Append(" | ").Append(t.Target.Headline)
                .Append(" | ").Append(Word(t.Verdict)).Append(" |\n");
        }

        b.Append("\n## Lines\n\n| Measure | Line | Setting | n | Right | Rate | 95 % interval | Coverage | Abstention counted wrong |\n|---|---|---|---:|---:|---:|---|---:|---:|\n");
        foreach (var line in report.Lines)
        {
            var (low, high) = Interval.Wilson(line.Right, line.Answered);
            b.Append("| ").Append(line.Measure).Append(" | ").Append(line.Part).Append(" | ").Append(line.Setting).Append(" | ")
                .Append(I(line.Answered)).Append(" | ").Append(I(line.Right)).Append(" | ")
                .Append(line.Answered == 0 ? "not measurable" : P(line.Rate)).Append(" | ")
                .Append(line.Answered == 0 ? "–" : P(low) + " to " + P(high)).Append(" | ")
                .Append(line.HasCoverage ? P(line.Coverage) : "n/a").Append(" | ")
                .Append(line.HasCoverage ? P(line.RateCountingAbstentionsWrong) : "n/a").Append(" |\n");
        }

        b.Append("\n## Claim type, per type (end to end)\n\n| Claim type | Right | Of |\n|---|---:|---:|\n");
        int otherRight = 0, otherTotal = 0;
        foreach (var (claim, (right, total)) in report.PerClaim)
        {
            if (total < SmallCell)
            {
                otherRight += right;
                otherTotal += total;
                continue;
            }

            b.Append("| `").Append(claim).Append("` | ").Append(I(right)).Append(" | ").Append(I(total)).Append(" |\n");
        }

        if (otherTotal > 0)
        {
            b.Append("| other (types with fewer than 3 notes) | ").Append(I(otherRight)).Append(" | ").Append(I(otherTotal)).Append(" |\n");
        }

        string[] classes = ["stated", "denied", "disputed", "notMentioned"];
        b.Append("\n## Polarity: gold (rows) and answer (columns), type given\n\n| | ").Append(string.Join(" | ", classes)).Append(" |\n|---|")
            .Append(string.Concat(classes.Select(_ => "---:|"))).Append('\n');
        foreach (var row in classes)
        {
            b.Append("| ").Append(row);
            foreach (var column in classes)
            {
                var count = report.PolarityConfusion.GetValueOrDefault(row)?.GetValueOrDefault(column) ?? 0;
                b.Append(" | ").Append(count is > 0 and < SmallCell ? "under 3" : I(count));
            }

            b.Append(" |\n");
        }

        b.Append("\n## Counts\n\n");
        foreach (var (name, count) in report.Counts)
        {
            b.Append("- ").Append(name).Append(": ").Append(count is > 0 and < SmallCell ? "under 3" : I(count)).Append('\n');
        }

        b.Append("\n## Calibration (answers given)\n\n");
        foreach (var (name, bins) in new[] { ("Claim answers", report.ClaimCalibration), ("Event answers", report.EventCalibration) })
        {
            b.Append(name).Append(": expected calibration error ").Append(P(Scorer.CalibrationError(bins))).Append(".\n\n| Bin | Answers | Mean confidence | Accuracy |\n|---|---:|---:|---:|\n");
            foreach (var bin in Merge(bins))
            {
                b.Append("| ").Append(bin.Label).Append(" | ").Append(I(bin.Count)).Append(" | ").Append(P(bin.Confidence)).Append(" | ").Append(P(bin.Accuracy)).Append(" |\n");
            }

            b.Append('\n');
        }

        if (report.Tuning is { } tuning)
        {
            b.Append("## Tuning score\n\n").Append(tuning.ToString("0.0000", CultureInfo.InvariantCulture)).Append('\n');
        }

        return b.ToString();
    }

    /// <summary>
    /// The per-writer rows (owner, 14 November 2026): for each headline line, the Codex half, the Sonnet half, both, and
    /// the gap between the halves' rates.
    /// </summary>
    public static string ByWriter(ScoreReport both, ScoreReport codex, ScoreReport sonnet)
    {
        var b = new StringBuilder("\n## By writer (the Codex half, the Sonnet half, both)\n\n| Measure | Line | Codex half | Sonnet half | Both | Gap (Codex minus Sonnet) |\n|---|---|---|---|---|---:|\n");
        foreach (var target in both.Targets)
        {
            foreach (var line in target.Lines)
            {
                ScoreLine? Of(ScoreReport r) => r.Lines.FirstOrDefault(x => x.Measure == line.Measure && x.Part == line.Part && x.Setting == line.Setting);
                string Cell(ScoreLine? x) => x is null || x.Answered == 0 ? "not measurable"
                    : $"{I(x.Right)} of {I(x.Answered)} ({P(x.Rate)}; {P(Interval.Wilson(x.Right, x.Answered).Low)} to {P(Interval.Wilson(x.Right, x.Answered).High)})";
                var (c, s) = (Of(codex), Of(sonnet));
                var gap = c is { Answered: > 0 } && s is { Answered: > 0 } ? P(c.Rate - s.Rate) : "–";
                b.Append("| ").Append(line.Measure).Append(" | ").Append(line.Part).Append(" (").Append(line.Setting).Append(") | ")
                    .Append(Cell(c)).Append(" | ").Append(Cell(s)).Append(" | ").Append(Cell(line)).Append(" | ").Append(gap).Append(" |\n");
            }
        }

        return b.ToString();
    }

    /// <summary>Bins with fewer than 3 answers are merged with their neighbour (upwards; the last with the one below).</summary>
    private static List<(string Label, int Count, double Confidence, double Accuracy)> Merge(IReadOnlyList<CalibrationBin> bins)
    {
        var merged = new List<(int From, int To, int Count, double ConfSum, double RightSum)>();
        foreach (var bin in bins)
        {
            if (merged.Count > 0 && merged[^1].Count < SmallCell)
            {
                var last = merged[^1];
                merged[^1] = (last.From, bin.Bin, last.Count + bin.Count, last.ConfSum + bin.MeanConfidence * bin.Count, last.RightSum + bin.Accuracy * bin.Count);
            }
            else
            {
                merged.Add((bin.Bin, bin.Bin, bin.Count, bin.MeanConfidence * bin.Count, bin.Accuracy * bin.Count));
            }
        }

        if (merged.Count > 1 && merged[^1].Count < SmallCell)
        {
            var last = merged[^1];
            var before = merged[^2];
            merged[^2] = (before.From, last.To, before.Count + last.Count, before.ConfSum + last.ConfSum, before.RightSum + last.RightSum);
            merged.RemoveAt(merged.Count - 1);
        }

        return [.. merged.Where(m => m.Count > 0).Select(m => ($"{P(m.From / 10.0)} to {P((m.To + 1) / 10.0)}", m.Count,
            m.ConfSum / m.Count, m.RightSum / m.Count))];
    }

    public static string Json(ScoreReport report, string setName, string scorerHash)
    {
        var root = new JsonObject
        {
            ["set"] = setName,
            ["scorer"] = scorerHash,
            ["notes"] = report.Notes,
            ["answersMissing"] = report.AnswersMissing,
            ["targets"] = new JsonArray([.. report.Targets.Select(t => (JsonNode)new JsonObject
            {
                ["measure"] = t.Target.Measure, ["goal"] = t.Target.Goal, ["headline"] = t.Target.Headline, ["verdict"] = Word(t.Verdict),
            })]),
            ["lines"] = new JsonArray([.. report.Lines.Select(line => (JsonNode)new JsonObject
            {
                ["measure"] = line.Measure, ["line"] = line.Part, ["setting"] = line.Setting, ["total"] = line.Total,
                ["answered"] = line.Answered, ["right"] = line.Right, ["apart"] = line.Apart, ["hasCoverage"] = line.HasCoverage,
            })]),
            ["tuning"] = report.Tuning,
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
    }

    public static string Word(Verdict verdict) => verdict switch
    {
        Verdict.Met => "met",
        Verdict.NotMet => "not met",
        Verdict.NotMetCoverage => "not met (coverage)",
        Verdict.NotMeasurable => "not measurable",
        _ => "inconclusive",
    };

    private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string P(double value) => (value * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %";
}
