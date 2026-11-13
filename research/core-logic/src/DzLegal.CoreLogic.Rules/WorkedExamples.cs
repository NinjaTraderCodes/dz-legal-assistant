using System.Text.Json;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>One worked example or boundary case run through the evaluator: every difference from what was expected.</summary>
public sealed record CaseResult(string RecordId, string CaseId, string Title, IReadOnlyList<string> Differences)
{
    public bool Passed => Differences.Count == 0;
}

/// <summary>
/// Runs the worked examples of a record (specs/rule-evaluator.md, "How a worked example is checked"): findings are partial,
/// each listed one is checked on its declared effect (and, in a final record, on the shown effect and the mark); questions
/// are a set, checked when the example states them; the section, when stated, must be the proposal.
/// </summary>
public static class WorkedExamples
{
    /// <summary>The case an example or a boundary case describes. Every value is taken as confirmed by the lawyer.</summary>
    public static CaseSlots CaseSlotsOf(IReadOnlyDictionary<string, JsonElement> slots, IReadOnlyList<string>? claimIds = null, CourtSection? section = null)
    {
        ArgumentNullException.ThrowIfNull(slots);
        var values = new List<SlotValue>();
        var requests = new List<string>();
        foreach (var (slot, value) in slots)
        {
            if (slot == CaseSlotReader.RequestsSlot)
            {
                if (value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(kind => kind.ValueKind != JsonValueKind.String))
                {
                    throw new FormatException("The slot requests is a list of request kinds.");
                }

                requests.AddRange(value.EnumerateArray().Select(kind => kind.GetString()!));
                continue;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                throw new FormatException($"The slot {slot} must be written as a string; found {value.ValueKind}.");
            }

            values.Add(new SlotValue { Slot = slot, Value = value.GetString()! });
        }

        return new CaseSlots { ClaimIds = claimIds ?? [], Values = values, Requests = requests, Section = section };
    }

    /// <summary>Runs every worked example of the record on the record itself.</summary>
    public static IReadOnlyList<CaseResult> Run(IRuleEvaluator evaluator, RuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(record);
        var compareShown = record.Stage == RecordStage.Final;
        return
        [
            .. record.Examples.Select(example =>
            {
                var outcome = evaluator.Evaluate([record], CaseSlotsOf(example.Slots), Dates.Parse(example.PetitionDate, $"{example.Id}.petitionDate"));
                return new CaseResult(record.Id, example.Id, example.Title, Compare(outcome, record.Id, example.Expect, compareShown, id => id));
            }),
        ];
    }

    /// <summary>
    /// Runs the worked examples of <paramref name="source"/> on <paramref name="other"/>, with item ids mapped through the id
    /// map (specs/rule-evaluator.md, <c>--against</c>). Besides the example's own expectations, the two runs are compared on
    /// every matched item: declared effect, last day and days left; and on the section and the questions. Comparisons use
    /// declared effects only: the two records may be graded differently, or not at all.
    /// </summary>
    public static IReadOnlyList<CaseResult> RunAgainst(IRuleEvaluator evaluator, RuleRecord source, RuleRecord other, IdMap map)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(map);
        var from = IdMap.RoleOf(source);
        var to = IdMap.RoleOf(other);
        string? Map(string id) => id == RuleEvaluator.SectionRef ? id : map.Map(id, from, to);

        var results = new List<CaseResult>();
        foreach (var example in source.Examples)
        {
            var slots = CaseSlotsOf(example.Slots);
            var date = Dates.Parse(example.PetitionDate, $"{example.Id}.petitionDate");
            var mine = evaluator.Evaluate([source], slots, date);
            var theirs = evaluator.Evaluate([other], slots, date);

            var differences = Compare(theirs, other.Id, example.Expect, compareShown: false, Map).ToList();
            foreach (var finding in mine.Findings)
            {
                if (Map(finding.Ref) is not { } mapped || theirs.Findings.FirstOrDefault(f => f.Ref == mapped) is not { } match)
                {
                    continue;
                }

                if (finding.DeclaredEffect != match.DeclaredEffect)
                {
                    differences.Add($"{finding.Ref} -> {mapped}: declared {RuleEvaluator.Name(finding.DeclaredEffect)} against {RuleEvaluator.Name(match.DeclaredEffect)}");
                }

                if (finding.LastDay != match.LastDay || finding.DaysLeft != match.DaysLeft)
                {
                    differences.Add($"{finding.Ref} -> {mapped}: last day {Day(finding)} against {Day(match)}");
                }
            }

            if (mine.Section?.Section != theirs.Section?.Section)
            {
                differences.Add($"section: {mine.Section?.Section.ToString() ?? "none"} against {theirs.Section?.Section.ToString() ?? "none"}");
            }

            var asked = mine.Questions.Select(question => question.Slot).ToHashSet(StringComparer.Ordinal);
            var askedThere = theirs.Questions.Select(question => question.Slot).ToHashSet(StringComparer.Ordinal);
            if (!asked.SetEquals(askedThere))
            {
                differences.Add($"questions: {Set(asked)} against {Set(askedThere)}");
            }

            results.Add(new CaseResult($"{source.Id} on {other.Id}", example.Id, example.Title, differences));
        }

        return results;
    }

    /// <param name="mapRef">Maps an item id of the example to the id in the record run; null when it has no counterpart.</param>
    public static IReadOnlyList<string> Compare(RuleOutcome outcome, string recordId, Expectation expect, bool compareShown, Func<string, string?> mapRef)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(expect);
        ArgumentNullException.ThrowIfNull(mapRef);
        var differences = new List<string>();
        foreach (var expected in expect.Findings)
        {
            if (mapRef(expected.Ref) is not { } reference)
            {
                differences.Add($"{expected.Ref}: no counterpart in the id map");
                continue;
            }

            var label = reference == expected.Ref ? reference : $"{expected.Ref} -> {reference}";
            var finding = outcome.Findings.FirstOrDefault(f => f.RecordId == recordId && f.Ref == reference);
            if (finding is null)
            {
                differences.Add($"{label}: expected {RuleEvaluator.Name(expected.Effect)}, and the item gave no finding");
                continue;
            }

            if (finding.DeclaredEffect != expected.Effect)
            {
                differences.Add($"{label}: declared {RuleEvaluator.Name(finding.DeclaredEffect)}, expected {RuleEvaluator.Name(expected.Effect)} ({finding.Trace})");
            }

            if (compareShown && expected.ShownEffect is { } shown && finding.Effect != shown)
            {
                differences.Add($"{label}: shown {RuleEvaluator.Name(finding.Effect)}, the example says {RuleEvaluator.Name(shown)} ({finding.Trace})");
            }

            if (compareShown && expected.ToCheck is { } toCheck && finding.ToCheck != toCheck)
            {
                differences.Add($"{label}: toCheck {finding.ToCheck}, the example says {toCheck}");
            }
        }

        if (expect.Questions is { } questions)
        {
            var wanted = questions.ToHashSet(StringComparer.Ordinal);
            var asked = outcome.Questions.Select(question => question.Slot).ToHashSet(StringComparer.Ordinal);
            if (!wanted.SetEquals(asked))
            {
                differences.Add($"questions: asked {Set(asked)}, expected {Set(wanted)}");
            }
        }

        if (expect.Section is { } section && outcome.Section?.Section != section)
        {
            differences.Add($"section: proposed {outcome.Section?.Section.ToString() ?? "none"}, expected {section}");
        }

        return differences;
    }

    internal static string Set(IEnumerable<string> slots) => "{" + string.Join(", ", slots.Order(StringComparer.Ordinal)) + "}";

    private static string Day(RuleFinding finding) =>
        finding.LastDay is { } day ? $"{Dates.Write(day)} ({finding.DaysLeft} days left)" : "none";
}
