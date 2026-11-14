using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>What a boundary case expects of the items its paragraph matches (specs/rule-evaluator.md, "Boundary cases").</summary>
public enum BoundaryOutcome
{
    InTime,
    Late,
    LastDay,
    StepMissing,
    StepDone,
    StepExempt,
    LeavesOrdinaryCourt,
    Ask,
    Unsettled,
}

public sealed record BoundaryExpectation
{
    /// <summary>The paragraph the case tests, as the texts label it: "[cpca 504 ¶1]".</summary>
    public required string About { get; init; }
    public required BoundaryOutcome Outcome { get; init; }

    /// <summary>yyyy-mm-dd, when the case is about a time limit and the writer could compute it.</summary>
    public string? LastDay { get; init; }
    public string? Why { get; init; }
}

/// <summary>One case of <c>rules/boundary-cases/&lt;unit&gt;.json</c>, written from the texts by an agent that saw no record.</summary>
public sealed record BoundaryCase
{
    public required string Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public required string PetitionDate { get; init; }
    public required IReadOnlyDictionary<string, JsonElement> Slots { get; init; }
    public required BoundaryExpectation Expect { get; init; }
}

/// <summary>A paragraph named as the texts label it: <c>[&lt;code&gt; &lt;article&gt; ¶&lt;n&gt;]</c>; the paragraph is optional.</summary>
public sealed partial record ParagraphRef(string Code, string Article, int? Paragraph)
{
    public static ParagraphRef? Parse(string about)
    {
        var match = Pattern().Match(about ?? string.Empty);
        if (!match.Success)
        {
            return null;
        }

        int? paragraph = match.Groups["p"].Success ? int.Parse(match.Groups["p"].Value, System.Globalization.CultureInfo.InvariantCulture) : null;
        return new ParagraphRef(match.Groups["code"].Value, match.Groups["article"].Value.Trim(), paragraph);
    }

    /// <summary>Whether a statement's cite covers this paragraph. A cite without paragraphs is the whole article.</summary>
    public bool IsCitedBy(Cite cite)
    {
        ArgumentNullException.ThrowIfNull(cite);
        return cite.Code == Code && cite.Article == Article
            && (Paragraph is not { } paragraph || cite.Paragraphs.Count == 0 || cite.Paragraphs.Contains(paragraph));
    }

    [GeneratedRegex(@"^\[\s*(?<code>\S+)\s+(?<article>.+?)(?:\s*¶\s*(?<p>\d+))?\s*\]$")]
    private static partial Regex Pattern();
}

/// <summary>Runs a boundary-case file through one or more records.</summary>
public static class BoundaryCases
{
    // Unknown members are skipped here, unlike in records: a case file is never edited after it is written, so a stray
    // field must not make it unusable. Enums and required fields stay strict.
    private static readonly JsonSerializerOptions Options = new(ContractJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip };

    public static IReadOnlyList<BoundaryCase> Parse(string json) =>
        JsonSerializer.Deserialize<List<BoundaryCase>>(json, Options) ?? throw new FormatException("A boundary-case file is a JSON array of cases.");

    /// <summary>
    /// Every case on every record, then, when several records are given, the records against each other: per case, the
    /// matched items' declared effects, last days and days left, and the questions.
    /// </summary>
    public static IReadOnlyList<CaseResult> Run(IRuleEvaluator evaluator, IReadOnlyList<RuleRecord> records, IReadOnlyList<BoundaryCase> cases)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(cases);
        var results = new List<CaseResult>();
        var signatures = new Dictionary<string, List<(string Record, string Signature)>>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            foreach (var boundaryCase in cases)
            {
                var (differences, signature) = RunOne(evaluator, record, boundaryCase);
                results.Add(new CaseResult(record.Id, boundaryCase.Id, boundaryCase.Title, differences));
                if (!signatures.TryGetValue(boundaryCase.Id, out var list))
                {
                    signatures[boundaryCase.Id] = list = [];
                }

                list.Add(($"{record.Id} ({record.Stage})", signature));
            }
        }

        if (records.Count > 1)
        {
            foreach (var boundaryCase in cases)
            {
                var runs = signatures[boundaryCase.Id];
                var differences = runs.Select(run => run.Signature).Distinct(StringComparer.Ordinal).Count() > 1
                    ? runs.Select(run => $"{run.Record}: {run.Signature}").ToList()
                    : [];
                results.Add(new CaseResult(string.Join(" | ", runs.Select(run => run.Record)), boundaryCase.Id, boundaryCase.Title, differences));
            }
        }

        return results;
    }

    /// <summary>
    /// The items a paragraph matches: every deadline, prior step, exit and boundary whose basis holds a statement citing it.
    /// A deadline's counting basis and a prior step's exemption bases count as its basis here: a case about the paragraph
    /// that sets the counting or an exemption tests that item.
    /// </summary>
    public static IReadOnlyList<(FindingKind Kind, string Id)> Match(RuleRecord record, ParagraphRef about)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(about);
        var citing = record.Basis.Where(statement => about.IsCitedBy(statement.Cite)).Select(statement => statement.Id).ToHashSet(StringComparer.Ordinal);
        bool Cites(IEnumerable<string> basis) => basis.Any(citing.Contains);

        return
        [
            .. record.Deadlines.Where(item => Cites(item.Basis.Concat(item.Counting?.Basis ?? []))).Select(item => (FindingKind.Deadline, item.Id)),
            .. record.PriorSteps.Where(item => Cites(item.Basis.Concat(item.NotRequiredWhen.SelectMany(exemption => exemption.Basis)))).Select(item => (FindingKind.PriorStep, item.Id)),
            .. (record.Court?.Exits ?? []).Where(item => Cites(item.Basis)).Select(item => (FindingKind.Exit, item.Id)),
            .. record.Boundaries.Where(item => Cites(item.Basis)).Select(item => (FindingKind.Boundary, item.Id)),
        ];
    }

    private static (List<string> Differences, string Signature) RunOne(IRuleEvaluator evaluator, RuleRecord record, BoundaryCase boundaryCase)
    {
        var differences = new List<string>();
        var about = ParagraphRef.Parse(boundaryCase.Expect.About);
        if (about is null)
        {
            return ([$"about \"{boundaryCase.Expect.About}\" is not written [<code> <article> ¶<n>]"], "unreadable");
        }

        var outcome = evaluator.Evaluate([record], WorkedExamples.CaseSlotsOf(boundaryCase.Slots), Dates.Parse(boundaryCase.PetitionDate, $"{boundaryCase.Id}.petitionDate"));
        var matched = Match(record, about);
        var kind = boundaryCase.Expect.Outcome switch
        {
            BoundaryOutcome.InTime or BoundaryOutcome.Late or BoundaryOutcome.LastDay => FindingKind.Deadline,
            BoundaryOutcome.StepMissing or BoundaryOutcome.StepDone or BoundaryOutcome.StepExempt => FindingKind.PriorStep,
            BoundaryOutcome.LeavesOrdinaryCourt => FindingKind.Exit,
            _ => (FindingKind?)null,
        };
        var relevant = matched.Where(item => kind is null || item.Kind == kind).ToList();
        var questions = WorkedExamples.Set(outcome.Questions.Select(question => question.Slot));

        if (matched.Count == 0)
        {
            differences.Add($"{boundaryCase.Expect.About} matches no item: a rule the record may have missed");
        }
        else if (relevant.Count == 0)
        {
            differences.Add($"{boundaryCase.Expect.About} matches no {kind} (it matches {string.Join(", ", matched.Select(item => item.Id))})");
        }

        var signature = new List<string>();
        foreach (var (itemKind, id) in relevant)
        {
            var finding = outcome.Findings.FirstOrDefault(f => f.Ref == id);
            if (finding is null)
            {
                differences.Add($"{id}: the item gave no finding");
                signature.Add($"{itemKind}: no finding");
                continue;
            }

            signature.Add($"{itemKind} {RuleEvaluator.Name(finding.DeclaredEffect)} last day {(finding.LastDay is { } day ? Dates.Write(day) : "none")} days left {finding.DaysLeft?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"}");
            if (Problem(record, boundaryCase.Expect.Outcome, id, finding) is { } problem)
            {
                differences.Add($"{id}: {problem} ({finding.Trace})");
            }

            if (itemKind == FindingKind.Deadline && boundaryCase.Expect.LastDay is { } lastDay)
            {
                var expected = Dates.Parse(lastDay, $"{boundaryCase.Id}.expect.lastDay");
                if (finding.LastDay != expected)
                {
                    differences.Add($"{id}: last day {(finding.LastDay is { } got ? Dates.Write(got) : "none")}, the case says {lastDay}");
                }
            }
        }

        signature.Sort(StringComparer.Ordinal);
        return (differences, $"[{string.Join("; ", signature)}] questions {questions}");
    }

    // What the matched item must declare, by the case's outcome (the table of specs/rule-evaluator.md); null when it does.
    private static string? Problem(RuleRecord record, BoundaryOutcome outcome, string id, RuleFinding finding)
    {
        var declared = finding.DeclaredEffect;
        string? Expect(FindingEffect wanted) =>
            declared == wanted ? null : $"declared {RuleEvaluator.Name(declared)}, the outcome {outcome} needs {RuleEvaluator.Name(wanted)}";

        switch (outcome)
        {
            case BoundaryOutcome.InTime or BoundaryOutcome.Late or BoundaryOutcome.LastDay:
                var deadline = record.Deadlines.Single(item => item.Id == id);
                if (finding.DaysLeft is not { } daysLeft)
                {
                    return $"declared {RuleEvaluator.Name(declared)} without a last day, the outcome {outcome} needs one";
                }

                var inBand = Math.Abs(daysLeft) <= (deadline.WarnBandDays ?? RuleEvaluator.DefaultWarnBandDays);
                return outcome switch
                {
                    BoundaryOutcome.InTime => Expect(inBand ? FindingEffect.Warn : FindingEffect.None),
                    BoundaryOutcome.Late => Expect(inBand ? FindingEffect.Warn : deadline.EffectWhenExpired),
                    _ => daysLeft != 0 ? $"{daysLeft} days left, the outcome lastDay needs 0" : Expect(FindingEffect.Warn),
                };
            case BoundaryOutcome.StepMissing:
                return Expect(record.PriorSteps.Single(item => item.Id == id).EffectWhenMissing);
            case BoundaryOutcome.StepDone or BoundaryOutcome.StepExempt:
                return Expect(FindingEffect.None);
            case BoundaryOutcome.LeavesOrdinaryCourt:
                return Expect(FindingEffect.Warn);
            case BoundaryOutcome.Ask:
                return Expect(FindingEffect.Ask);
            default:
                // Unsettled: never silent, and never a block unless the item is a matter of reading (which caps it at a warning).
                var interpretive = record.Deadlines.FirstOrDefault(item => item.Id == id) is { } d ? RecordItems.IsInterpretive(record, d)
                    : record.PriorSteps.FirstOrDefault(item => item.Id == id) is { } s ? RecordItems.IsInterpretive(record, s)
                    : record.Court?.Exits.FirstOrDefault(item => item.Id == id) is { } x ? RecordItems.IsInterpretive(record, x.Interpretive, x.Basis)
                    : RecordItems.IsInterpretive(record, false, record.Boundaries.First(item => item.Id == id).Basis);
                return declared == FindingEffect.None ? "declared none: an unsettled case must not be silent"
                    : declared == FindingEffect.BlockCleanExport && !interpretive ? "declared blockCleanExport: an unsettled case must not block"
                    : null;
        }
    }
}
