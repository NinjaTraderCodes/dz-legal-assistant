using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Scoring;

/// <summary>
/// One line of a measure: of <see cref="Total"/> items, <see cref="Answered"/> were answered and <see cref="Right"/> of those
/// were right. <see cref="Apart"/> counts abstentions reported apart (outside notes). A line without abstention (values,
/// requests) has <see cref="HasCoverage"/> false: its coverage is "n/a".
/// </summary>
public sealed record ScoreLine(string Measure, string Part, string Setting, int Total, int Answered, int Right, int Apart = 0,
    bool HasCoverage = true)
{
    public double Rate => Answered == 0 ? 0 : (double)Right / Answered;

    public double Coverage => Total == 0 ? 0 : (double)Answered / Total;

    public double RateCountingAbstentionsWrong => Total == 0 ? 0 : (double)Right / Total;
}

/// <summary>A target of the brief: its goal, its coverage floor, its headline setting.</summary>
public sealed record Target(string Measure, double Goal, double? CoverageFloor, string Headline);

public sealed record TargetVerdict(Target Target, Verdict Verdict, IReadOnlyList<ScoreLine> Lines);

public sealed record CalibrationBin(int Bin, int Count, double MeanConfidence, double Accuracy);

public sealed class ScoreReport
{
    public required int Notes { get; init; }
    public required int AnswersMissing { get; init; }
    public required IReadOnlyList<ScoreLine> Lines { get; init; }
    public required IReadOnlyList<TargetVerdict> Targets { get; init; }

    /// <summary>Gold polarity → answered polarity → count (type given).</summary>
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> PolarityConfusion { get; init; }

    /// <summary>Counts reported without a target: critical flips, wrong-type elements, outside false alarms, values apart.</summary>
    public required IReadOnlyDictionary<string, int> Counts { get; init; }

    /// <summary>Claim type → (right, total) in the end-to-end setting.</summary>
    public required IReadOnlyDictionary<string, (int Right, int Total)> PerClaim { get; init; }
    public required IReadOnlyList<CalibrationBin> ClaimCalibration { get; init; }
    public required IReadOnlyList<CalibrationBin> EventCalibration { get; init; }
    public required double? Tuning { get; init; }
}

/// <summary>The measures of specs/scorer.md, in both settings, on one gold file.</summary>
public static class Scorer
{
    /// <summary>
    /// 1.0: the scorer whose hash was fixed before any model. 1.1 (the owner, 17 November 2026): the evidence line accepts
    /// every place where the note writes the same fact, as guide 1.3 allows; no other line changes.
    /// </summary>
    public const string Version = "1.1";

    public const string TypeGiven = "type given";
    public const string EndToEnd = "end to end";

    public static readonly IReadOnlyList<Target> Targets =
    [
        new("claim type", 0.95, 0.90, EndToEnd),
        new("outside recognised", 0.95, null, EndToEnd),
        new("polarity", 0.95, 0.90, TypeGiven),
        new("dates and amounts", 0.90, null, TypeGiven),
        new("requests", 0.90, null, TypeGiven),
    ];

    private static readonly string[] Mentioned = ["stated", "denied", "disputed"];

    public static ScoreReport Score(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> endToEnd,
        IReadOnlyDictionary<string, NoteReading> given, bool tuning = false, string evidenceRule = Version)
    {
        if (evidenceRule is not ("1.0" or "1.1"))
        {
            throw new ScoreException("U03", "the evidence rule is 1.0 or 1.1");
        }

        var lines = new List<ScoreLine>();
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var missing = gold.Count(note => !endToEnd.ContainsKey(note.Id) || !given.ContainsKey(note.Id));

        // Claim type and outside (end to end only).
        int claimTotal = 0, claimAnswered = 0, claimRight = 0, outTotal = 0, outRight = 0, outAbstained = 0, falseAlarms = 0;
        var perClaim = new SortedDictionary<string, (int, int)>(StringComparer.Ordinal);
        var claimCal = new List<(double Confidence, bool Right)>();
        foreach (var note in gold)
        {
            var reading = endToEnd.GetValueOrDefault(note.Id);
            var answers = Answered(reading);
            var abstained = reading is null || reading.Claim.Abstained || answers.Count == 0;
            if (note.IsOutside)
            {
                outTotal++;
                if (abstained)
                {
                    outAbstained++;
                }
                else if (answers.SetEquals([ClaimGuess.Outside]))
                {
                    outRight++;
                }

                continue;
            }

            claimTotal++;
            var right = !abstained && answers.SetEquals(note.Claims);
            if (!abstained)
            {
                claimAnswered++;
                claimRight += right ? 1 : 0;
                falseAlarms += answers.Contains(ClaimGuess.Outside) ? 1 : 0;
                claimCal.Add((reading!.Claim.Answers.Max(answer => answer.Confidence), right));
            }

            foreach (var claim in note.Claims)
            {
                var (r, t) = perClaim.GetValueOrDefault(claim);
                perClaim[claim] = (r + (right ? 1 : 0), t + 1);
            }
        }

        lines.Add(new ScoreLine("claim type", "in-catalogue notes", EndToEnd, claimTotal, claimAnswered, claimRight));
        lines.Add(new ScoreLine("outside recognised", "outside notes", EndToEnd, outTotal, outTotal, outRight, outAbstained, HasCoverage: false));
        counts["outside: false alarms (in-catalogue notes answered outside)"] = falseAlarms;
        counts["outside: abstentions on outside notes"] = outAbstained;

        // Polarity, slots, values, requests: both settings.
        var confusion = new SortedDictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal);
        var eventCal = new List<(double Confidence, bool Right)>();
        foreach (var setting in new[] { TypeGiven, EndToEnd })
        {
            var readings = setting == TypeGiven ? given : endToEnd;
            Polarity(gold, readings, setting, lines, counts, setting == TypeGiven ? confusion : null, setting == TypeGiven ? eventCal : null);
            Slots(gold, readings, setting, lines);
            Values(gold, readings, setting, lines, counts);
            Requests(gold, readings, setting, lines);
            Details(gold, readings, setting, lines);
        }

        Role(gold, given, lines);
        Evidence(gold, endToEnd, given, lines, evidenceRule == "1.1");

        return new ScoreReport
        {
            Notes = gold.Count,
            AnswersMissing = missing,
            Lines = lines,
            Targets = Verdicts(lines),
            PolarityConfusion = confusion,
            Counts = counts,
            PerClaim = perClaim,
            ClaimCalibration = Calibrate(claimCal),
            EventCalibration = Calibrate(eventCal),
            Tuning = tuning ? TuningScore(gold, endToEnd, given) : null,
        };
    }

    private static HashSet<string> Answered(NoteReading? reading) =>
        reading is null ? [] : reading.Claim.Answers.Select(answer => answer.ClaimId).ToHashSet(StringComparer.Ordinal);

    private static string Name(Polarity polarity) => polarity switch
    {
        Contracts.Polarity.Stated => "stated",
        Contracts.Polarity.Denied => "denied",
        Contracts.Polarity.Disputed => "disputed",
        _ => "notMentioned",
    };

    /// <summary>
    /// The reader's answer for a gold element, or null for an abstention. End to end: an element of a claim type the reader
    /// did not answer is an abstention when it abstained on the claim, and "wrong" (an empty string) when it answered others.
    /// </summary>
    private static string? Answer(GoldNote note, GoldEvent element, NoteReading? reading, string setting, out double confidence)
    {
        confidence = 0;
        if (reading is null)
        {
            return null;
        }

        if (setting == EndToEnd)
        {
            var answers = Answered(reading);
            if (reading.Claim.Abstained || answers.Count == 0)
            {
                return null;
            }

            if (!answers.Contains(element.Claim))
            {
                return "";
            }
        }

        var found = reading.Events.FirstOrDefault(e => e.Claim == element.Claim && e.Kind == element.Kind);
        if (found is null || found.Abstained)
        {
            return null;
        }

        confidence = found.Confidence;
        return Name(found.Polarity);
    }

    private static void Polarity(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> readings, string setting,
        List<ScoreLine> lines, SortedDictionary<string, int> counts, SortedDictionary<string, IReadOnlyDictionary<string, int>>? confusion,
        List<(double, bool)>? calibration)
    {
        int mTotal = 0, mAnswered = 0, mRight = 0, aTotal = 0, aAnswered = 0, aRight = 0, flips = 0, disputedAsStated = 0, wrongType = 0;
        var table = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        foreach (var note in gold)
        {
            var reading = readings.GetValueOrDefault(note.Id);
            foreach (var element in note.Events.Where(e => e.Polarity is "stated" or "denied" or "disputed" or "notMentioned"))
            {
                var answer = Answer(note, element, reading, setting, out var confidence);
                var mentioned = Mentioned.Contains(element.Polarity);
                if (mentioned)
                {
                    mTotal++;
                }
                else
                {
                    aTotal++;
                }

                if (answer is null)
                {
                    continue;
                }

                var right = answer == element.Polarity;
                if (mentioned)
                {
                    mAnswered++;
                    mRight += right ? 1 : 0;
                    flips += (element.Polarity, answer) is ("stated", "denied") or ("denied", "stated") ? 1 : 0;
                    disputedAsStated += (element.Polarity, answer) is ("disputed", "stated") ? 1 : 0;
                }
                else
                {
                    aAnswered++;
                    aRight += right ? 1 : 0;
                }

                if (answer.Length > 0)
                {
                    var row = table.TryGetValue(element.Polarity, out var r) ? r : table[element.Polarity] = new(StringComparer.Ordinal);
                    row[answer] = row.GetValueOrDefault(answer) + 1;
                    calibration?.Add((confidence, right));
                }
            }

            if (reading is not null)
            {
                wrongType += reading.Events.Count(e => !note.Claims.Contains(e.Claim) && e.Polarity != Contracts.Polarity.NotMentioned);
            }
        }

        lines.Add(new ScoreLine("polarity", "mentioned", setting, mTotal, mAnswered, mRight));
        lines.Add(new ScoreLine("polarity", "absent", setting, aTotal, aAnswered, aRight));
        if (confusion is not null)
        {
            foreach (var (goldPolarity, row) in table)
            {
                confusion[goldPolarity] = row;
            }

            counts["critical flips: stated and denied confused"] = flips;
            counts["critical flips: disputed read as stated"] = disputedAsStated;
            foreach (var cls in Mentioned)
            {
                var truePositive = table.GetValueOrDefault(cls)?.GetValueOrDefault(cls) ?? 0;
                var goldAnswered = table.GetValueOrDefault(cls)?.Values.Sum() ?? 0;
                var predicted = table.Values.Sum(row => row.GetValueOrDefault(cls));
                lines.Add(new ScoreLine("polarity per class", cls + ", recall", setting, goldAnswered, goldAnswered, truePositive, HasCoverage: false));
                lines.Add(new ScoreLine("polarity per class", cls + ", precision", setting, predicted, predicted, truePositive, HasCoverage: false));
            }
        }

        counts[$"elements of a wrong type ({setting})"] = wrongType;
    }

    private static void Slots(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> readings, string setting, List<ScoreLine> lines)
    {
        foreach (var slot in gold.SelectMany(note => note.Slots).Select(s => s.Slot).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            int allT = 0, allA = 0, allR = 0, knownT = 0, knownA = 0, knownR = 0;
            foreach (var note in gold)
            {
                var g = note.Slots.FirstOrDefault(s => s.Slot == slot);
                if (g is null || g.Value is "blanked" or "unclear")
                {
                    continue;
                }

                var known = g.Value != "unknown";
                allT++;
                knownT += known ? 1 : 0;
                var answer = readings.GetValueOrDefault(note.Id)?.Slots.FirstOrDefault(s => s.Slot == slot);
                if (answer is null || answer.Abstained)
                {
                    continue;
                }

                var right = Normal(answer.Value) == Normal(g.Value);
                allA++;
                allR += right ? 1 : 0;
                if (known)
                {
                    knownA++;
                    knownR += right ? 1 : 0;
                }
            }

            lines.Add(new ScoreLine("slot " + slot, "all notes", setting, allT, allA, allR));
            lines.Add(new ScoreLine("slot " + slot, "known values", setting, knownT, knownA, knownR));
        }
    }

    /// <summary>A slot value or a normalised value as compared: trimmed, digits without separators, case ignored.</summary>
    public static string Normal(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length > 0 && trimmed.All(c => char.IsAsciiDigit(c) || c is ' ' or ',' or ' ')
            ? new string([.. trimmed.Where(char.IsAsciiDigit)]).TrimStart('0')
            : trimmed.ToUpperInvariant();
    }

    private static bool Overlaps(GoldSpan? gold, TextSpan span) =>
        gold is not null && gold.Field == span.Field && span.Start < gold.Start + gold.Length && gold.Start < span.Start + span.Length;

    private static void Values(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> readings, string setting,
        List<ScoreLine> lines, SortedDictionary<string, int> counts)
    {
        int total = 0, right = 0, apart = 0, predicted = 0, matched = 0;
        foreach (var note in gold)
        {
            var reading = readings.GetValueOrDefault(note.Id);
            var used = new HashSet<ValueReading>();
            foreach (var value in note.Values)
            {
                if (value.CountedApart)
                {
                    apart++;
                    continue;
                }

                if (value.Normalized is null)
                {
                    continue;
                }

                total++;
                var hit = reading?.Values.FirstOrDefault(v => v.Kind == value.Kind && Overlaps(value.At, v.Span)
                    && Normal(v.Normalized) == Normal(value.Normalized)
                    && ((value.Roles.Count == 0 && v.Roles.Count == 0) || v.Roles.Intersect(value.Roles, StringComparer.Ordinal).Any()));
                if (hit is not null)
                {
                    right++;
                    used.Add(hit);
                }
            }

            predicted += reading?.Values.Count ?? 0;
            matched += used.Count;
        }

        lines.Add(new ScoreLine("dates and amounts", "recall", setting, total, total, right, HasCoverage: false));
        lines.Add(new ScoreLine("dates and amounts, precision", "precision", setting, predicted, predicted, matched, HasCoverage: false));
        counts[$"values counted apart: relative, Hijri or foreign currency ({setting})"] = apart;
    }

    private static void Requests(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> readings, string setting,
        List<ScoreLine> lines)
    {
        int pairs = 0, right = 0, amountsTotal = 0, amountsRight = 0;
        foreach (var note in gold)
        {
            var asked = note.Requests.Select(r => r.Kind).ToHashSet(StringComparer.Ordinal);
            var answered = readings.GetValueOrDefault(note.Id)?.Requests ?? [];
            var answeredKinds = answered.Select(r => r.Kind).ToHashSet(StringComparer.Ordinal);
            pairs += asked.Union(answeredKinds).Count();
            right += asked.Intersect(answeredKinds).Count();
            foreach (var request in note.Requests.Where(r => r.Amount is not null && answeredKinds.Contains(r.Kind)))
            {
                amountsTotal++;
                var answer = answered.First(r => r.Kind == request.Kind);
                amountsRight += answer.Amount is { } amount && Normal(amount.Normalized) == Normal(request.Amount!) ? 1 : 0;
            }
        }

        lines.Add(new ScoreLine("requests", "pairs", setting, pairs, pairs, right, HasCoverage: false));
        lines.Add(new ScoreLine("request amounts", "asked and answered", setting, amountsTotal, amountsTotal, amountsRight, HasCoverage: false));
    }

    /// <summary>Actor and disputedBy, on mentioned elements where gold gives one and the reader answered.</summary>
    private static void Details(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> readings, string setting, List<ScoreLine> lines)
    {
        int actorT = 0, actorR = 0, byT = 0, byR = 0;
        foreach (var note in gold)
        {
            var reading = readings.GetValueOrDefault(note.Id);
            foreach (var element in note.Events.Where(e => Mentioned.Contains(e.Polarity)))
            {
                var found = reading?.Events.FirstOrDefault(e => e.Claim == element.Claim && e.Kind == element.Kind && !e.Abstained);
                if (found is null)
                {
                    continue;
                }

                if (element.Actor is { } actor && actor != "unknown")
                {
                    actorT++;
                    actorR += found.Actor == actor ? 1 : 0;
                }

                if (element.DisputedBy is { } by)
                {
                    byT++;
                    byR += found.DisputedBy == by ? 1 : 0;
                }
            }
        }

        lines.Add(new ScoreLine("actor", "mentioned elements", setting, actorT, actorT, actorR, HasCoverage: false));
        lines.Add(new ScoreLine("disputedBy", "disputed elements", setting, byT, byT, byR, HasCoverage: false));
    }

    /// <summary>The role line: the notes of claim types with a choice of roles, type given (specs/scorer.md).</summary>
    private static void Role(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> given, List<ScoreLine> lines)
    {
        int total = 0, answered = 0, right = 0;
        foreach (var note in gold)
        {
            foreach (var role in note.Roles.Where(r => r.Relation != "unknown"))
            {
                total++;
                var answer = given.GetValueOrDefault(note.Id)?.Claim.Answers.FirstOrDefault(a => a.ClaimId == role.Claim)?.Role;
                if (answer is null || answer.Abstained)
                {
                    continue;
                }

                answered++;
                right += answer.Relation == role.Relation && answer.Position == role.Position ? 1 : 0;
            }
        }

        lines.Add(new ScoreLine("plaintiff's role", "claim types with a choice", TypeGiven, total, answered, right));
    }

    /// <summary>
    /// Evidence: every right answer that carries words: a predicted span overlapping a gold span of the same field, or in the
    /// same sentence. A claim answer without evidence counts only here, as unsupported. Since scorer 1.1 (guide 1.3: a fact
    /// written twice may be quoted at either place) the gold spans are joined by the other places of the same fact
    /// (<see cref="Places"/>).
    /// </summary>
    private static void Evidence(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> endToEnd,
        IReadOnlyDictionary<string, NoteReading> given, List<ScoreLine> lines, bool everyPlace)
    {
        int total = 0, supported = 0;
        void Check(GoldNote note, IReadOnlyList<GoldSpan> goldSpans, IReadOnlyList<TextSpan> spans)
        {
            total++;
            var places = everyPlace ? Places(note, goldSpans) : goldSpans;
            supported += spans.Any(span => places.Any(g => Overlaps(g, span) || SameSentence(note, g, span))) ? 1 : 0;
        }

        foreach (var note in gold)
        {
            var e2e = endToEnd.GetValueOrDefault(note.Id);
            if (e2e is not null && !note.IsOutside && !e2e.Claim.Abstained && Answered(e2e).SetEquals(note.Claims) && note.ClaimEvidence.Count > 0)
            {
                foreach (var answer in e2e.Claim.Answers)
                {
                    Check(note, note.ClaimEvidence, answer.Evidence);
                }
            }

            var reading = given.GetValueOrDefault(note.Id);
            if (reading is null)
            {
                continue;
            }

            foreach (var element in note.Events.Where(e => Mentioned.Contains(e.Polarity) && e.Evidence.Count > 0))
            {
                var found = reading.Events.FirstOrDefault(e => e.Claim == element.Claim && e.Kind == element.Kind && !e.Abstained);
                if (found is not null && Name(found.Polarity) == element.Polarity)
                {
                    Check(note, element.Evidence, found.Evidence);
                }
            }

            foreach (var slot in note.Slots.Where(s => s.Value != "unknown" && s.Evidence.Count > 0))
            {
                var found = reading.Slots.FirstOrDefault(s => s.Slot == slot.Slot && !s.Abstained);
                if (found is not null && Normal(found.Value) == Normal(slot.Value))
                {
                    Check(note, slot.Evidence, found.Evidence);
                }
            }

            foreach (var request in note.Requests.Where(r => r.Evidence.Count > 0))
            {
                var found = reading.Requests.FirstOrDefault(r => r.Kind == request.Kind);
                if (found is not null)
                {
                    Check(note, request.Evidence, found.Evidence);
                }
            }
        }

        lines.Add(new ScoreLine("evidence", "right answers with words", TypeGiven, total, total, supported, HasCoverage: false));
    }

    /// <summary>
    /// The places where the note writes the fact a list of gold spans quotes (scorer 1.1): the spans themselves; every other
    /// place, in any field, where the same words are written letter for letter (at least three letters); and, for a gold
    /// span on a date or an amount, every other place the note writes that value with a role in common (the gold has one
    /// value entry for each place, guide section 8).
    /// </summary>
    public static IReadOnlyList<GoldSpan> Places(GoldNote note, IReadOnlyList<GoldSpan> goldSpans)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(goldSpans);
        var places = new List<GoldSpan>(goldSpans);
        foreach (var g in goldSpans)
        {
            if (note.Text.TryGetValue(g.Field, out var own) && g.Start >= 0 && g.Start + g.Length <= own.Length)
            {
                var words = own.Substring(g.Start, g.Length).Trim();
                if (words.Length >= 3)
                {
                    foreach (var (field, text) in note.Text)
                    {
                        for (var at = text.IndexOf(words, StringComparison.Ordinal); at >= 0; at = text.IndexOf(words, at + 1, StringComparison.Ordinal))
                        {
                            places.Add(new GoldSpan(field, at, words.Length));
                        }
                    }
                }
            }

            foreach (var value in note.Values.Where(v => v.At is not null && v.Normalized is not null && SpansOverlap(v.At!, g)))
            {
                places.AddRange(note.Values.Where(o => o.At is not null && o.Normalized == value.Normalized && o.Roles.Intersect(value.Roles).Any())
                    .Select(o => o.At!));
            }
        }

        return [.. places.Distinct()];
    }

    private static bool SpansOverlap(GoldSpan a, GoldSpan b) =>
        a.Field == b.Field && a.Start < b.Start + b.Length && b.Start < a.Start + a.Length;

    private static bool SameSentence(GoldNote note, GoldSpan gold, TextSpan span)
    {
        if (gold.Field != span.Field || !note.Text.TryGetValue(gold.Field, out var text))
        {
            return false;
        }

        static int SentenceOf(string text, int position)
        {
            var count = 0;
            for (var i = 0; i < Math.Min(position, text.Length); i++)
            {
                count += text[i] is '.' or '؟' or '?' or '!' or '\n' ? 1 : 0;
            }

            return count;
        }

        return SentenceOf(text, gold.Start) == SentenceOf(text, span.Start);
    }

    private static List<TargetVerdict> Verdicts(IReadOnlyList<ScoreLine> lines)
    {
        var verdicts = new List<TargetVerdict>();
        var targets = Targets.ToList();
        foreach (var slot in lines.Where(line => line.Measure.StartsWith("slot ", StringComparison.Ordinal)).Select(line => line.Measure).Distinct(StringComparer.Ordinal))
        {
            targets.Add(new Target(slot, 0.90, 0.90, TypeGiven));
        }

        foreach (var target in targets)
        {
            var mine = lines.Where(line => line.Setting == target.Headline && line.Measure == target.Measure
                && !(target.Measure == "dates and amounts" && line.Part != "recall")).ToList();
            var each = mine.Select(line => Interval.Of(line.Right, line.Answered, target.Goal)).ToList();
            Verdict verdict;
            if (target.CoverageFloor is { } floor && mine.Any(line => line.HasCoverage && line.Total > 0 && line.Coverage < floor))
            {
                verdict = Verdict.NotMetCoverage;
            }
            else if (each.Count > 0 && each.All(v => v == Verdict.Met))
            {
                verdict = Verdict.Met;
            }
            else if (each.Any(v => v == Verdict.NotMet))
            {
                verdict = Verdict.NotMet;
            }
            else
            {
                verdict = Verdict.Inconclusive;
            }

            verdicts.Add(new TargetVerdict(target, verdict, mine));
        }

        return verdicts;
    }

    private static List<CalibrationBin> Calibrate(List<(double Confidence, bool Right)> answers)
    {
        var bins = new List<CalibrationBin>();
        for (var b = 0; b < 10; b++)
        {
            var mine = answers.Where(a => Math.Min(9, (int)(a.Confidence * 10)) == b).ToList();
            bins.Add(new CalibrationBin(b, mine.Count, mine.Count == 0 ? 0 : mine.Average(a => a.Confidence),
                mine.Count == 0 ? 0 : mine.Count(a => a.Right) / (double)mine.Count));
        }

        return bins;
    }

    /// <summary>The expected calibration error of a reliability table.</summary>
    public static double CalibrationError(IReadOnlyList<CalibrationBin> bins)
    {
        var n = bins.Sum(bin => bin.Count);
        return n == 0 ? 0 : bins.Sum(bin => bin.Count * Math.Abs(bin.Accuracy - bin.MeanConfidence)) / n;
    }

    /// <summary>
    /// The tuning score: the mean of four numbers (specs/scorer.md, "The tuning score"); an abstention counts as wrong.
    /// </summary>
    public static double TuningScore(IReadOnlyList<GoldNote> gold, IReadOnlyDictionary<string, NoteReading> endToEnd,
        IReadOnlyDictionary<string, NoteReading> given)
    {
        var claim = gold.Count == 0 ? 0 : gold.Count(note => endToEnd.GetValueOrDefault(note.Id) is { } r && !r.Claim.Abstained
            && Answered(r).SetEquals(note.Claims)) / (double)gold.Count;

        var shares = new List<double>();
        foreach (var cls in new[] { "stated", "denied", "disputed", "notMentioned" })
        {
            int t = 0, r = 0;
            foreach (var note in gold)
            {
                foreach (var element in note.Events.Where(e => e.Polarity == cls))
                {
                    t++;
                    r += Answer(note, element, given.GetValueOrDefault(note.Id), TypeGiven, out _) == cls ? 1 : 0;
                }
            }

            if (t > 0)
            {
                shares.Add((double)r / t);
            }
        }

        var polarity = shares.Count == 0 ? 0 : shares.Average();

        int kT = 0, kR = 0, uT = 0, uR = 0;
        foreach (var note in gold)
        {
            foreach (var slot in note.Slots.Where(s => s.Value is not ("blanked" or "unclear")))
            {
                var answer = given.GetValueOrDefault(note.Id)?.Slots.FirstOrDefault(s => s.Slot == slot.Slot);
                var right = answer is not null && !answer.Abstained && Normal(answer.Value) == Normal(slot.Value);
                if (slot.Value == "unknown")
                {
                    uT++;
                    uR += right ? 1 : 0;
                }
                else
                {
                    kT++;
                    kR += right ? 1 : 0;
                }
            }
        }

        var slotShares = new List<double>();
        if (kT > 0)
        {
            slotShares.Add((double)kR / kT);
        }

        if (uT > 0)
        {
            slotShares.Add((double)uR / uT);
        }

        var slots = slotShares.Count == 0 ? 0 : slotShares.Average();

        int tp = 0, asked = 0, answeredCount = 0;
        foreach (var note in gold)
        {
            var goldKinds = note.Requests.Select(r => r.Kind).ToHashSet(StringComparer.Ordinal);
            var kinds = (given.GetValueOrDefault(note.Id)?.Requests ?? []).Select(r => r.Kind).ToHashSet(StringComparer.Ordinal);
            tp += goldKinds.Intersect(kinds).Count();
            asked += goldKinds.Count;
            answeredCount += kinds.Count;
        }

        var requests = asked + answeredCount == 0 ? 0 : 2.0 * tp / (asked + answeredCount);
        return (claim + polarity + slots + requests) / 4;
    }
}
