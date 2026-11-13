using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>A condition is true, false or unknown. Unknown is not false: it makes the rule ask.</summary>
public enum Truth
{
    False,
    True,
    Unknown,
}

/// <summary>
/// The value of a condition, and when it is unknown, the slots to ask for: the unknown slots that could still change the
/// result, in the order they are written, each once.
/// </summary>
public sealed record ConditionResult(Truth Value, IReadOnlyList<string> Ask)
{
    public static ConditionResult Of(bool value) => new(value ? Truth.True : Truth.False, []);
}

/// <summary>The three-valued conditions of specs/rule-evaluator.md ("Conditions: three values").</summary>
public static class Conditions
{
    public static ConditionResult Evaluate(SlotCondition condition, CaseSlots slots) =>
        Evaluate(condition, new CaseSlotReader(slots), []);

    /// <param name="used">Receives every slot the condition reads, in the order read (the finding's SlotsUsed).</param>
    public static ConditionResult Evaluate(SlotCondition condition, CaseSlotReader slots, ICollection<string> used)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(slots);
        ArgumentNullException.ThrowIfNull(used);

        var problems = ShapeProblems(condition, "condition");
        if (problems.Count > 0)
        {
            throw new RuleRecordException(string.Join("; ", problems));
        }

        return Run(condition, slots, used);
    }

    /// <summary>
    /// What is wrong with the shape of a condition, with its path; empty when it can be evaluated. <c>rules check</c> calls
    /// this, and the evaluator refuses a condition for which it is not empty.
    /// </summary>
    public static IReadOnlyList<string> ShapeProblems(SlotCondition condition, string path)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var problems = new List<string>();
        Shape(condition, path, problems);
        return problems;
    }

    /// <summary>Every slot a condition names, in the order written, each once.</summary>
    public static IReadOnlyList<string> SlotsOf(SlotCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var slots = new List<string>();
        Collect(condition, slots);
        return slots;
    }

    private static ConditionResult Run(SlotCondition condition, CaseSlotReader slots, ICollection<string> used)
    {
        if (condition.All is { } all)
        {
            // No short cut: every part is read, so that every slot used is known and every useful question is found.
            var parts = all.Select(part => Run(part, slots, used)).ToList();
            if (parts.Any(part => part.Value == Truth.False))
            {
                return ConditionResult.Of(false);
            }

            return parts.Any(part => part.Value == Truth.Unknown) ? UnknownOf(parts) : ConditionResult.Of(true);
        }

        if (condition.Any is { } any)
        {
            var parts = any.Select(part => Run(part, slots, used)).ToList();
            if (parts.Any(part => part.Value == Truth.True))
            {
                return ConditionResult.Of(true);
            }

            return parts.Any(part => part.Value == Truth.Unknown) ? UnknownOf(parts) : ConditionResult.Of(false);
        }

        if (condition.Not is { } inner)
        {
            var result = Run(inner, slots, used);
            return result.Value switch
            {
                Truth.True => ConditionResult.Of(false),
                Truth.False => ConditionResult.Of(true),
                _ => result,
            };
        }

        return Leaf(condition, slots, used);
    }

    // For "all", the parts already true are skipped; for "any", the parts already false: only unknown parts can still change it.
    private static ConditionResult UnknownOf(IEnumerable<ConditionResult> parts) =>
        new(Truth.Unknown, parts.Where(part => part.Value == Truth.Unknown).SelectMany(part => part.Ask).Distinct(StringComparer.Ordinal).ToList());

    private static ConditionResult Leaf(SlotCondition condition, CaseSlotReader slots, ICollection<string> used)
    {
        var slot = condition.Slot!;
        if (!used.Contains(slot))
        {
            used.Add(slot);
        }

        if (condition.Includes is { } kind)
        {
            return ConditionResult.Of(slots.HasRequest(kind));
        }

        if (condition.Known is { } wanted)
        {
            return ConditionResult.Of(slots.IsKnown(slot) == wanted);
        }

        var value = slots.Value(slot);
        if (value is null)
        {
            return new ConditionResult(Truth.Unknown, [slot]);
        }

        if (condition.Is is { } expected)
        {
            return ConditionResult.Of(string.Equals(value, expected, StringComparison.Ordinal));
        }

        if (condition.In is { } list)
        {
            return ConditionResult.Of(list.Contains(value, StringComparer.Ordinal));
        }

        var date = slots.Date(slot);
        if (condition.Before is { } before)
        {
            return ConditionResult.Of(date < Dates.Parse(before, "before"));
        }

        return ConditionResult.Of(date >= Dates.Parse(condition.After!, "after"));
    }

    private static void Shape(SlotCondition condition, string path, List<string> problems)
    {
        var kinds = new[] { condition.All is not null, condition.Any is not null, condition.Not is not null, condition.Slot is not null }.Count(set => set);
        if (kinds != 1)
        {
            problems.Add($"{path}: a condition sets exactly one of all, any, not, slot; this one sets {kinds}");
            return;
        }

        var operators = new[]
        {
            condition.Is is not null, condition.In is not null, condition.Includes is not null,
            condition.Before is not null, condition.After is not null, condition.Known is not null,
        }.Count(set => set);

        if (condition.Slot is { } slot)
        {
            if (operators != 1)
            {
                problems.Add($"{path}: a slot condition sets exactly one of is, in, includes, before, after, known; this one sets {operators}");
            }
            else if (slot == CaseSlotReader.RequestsSlot && condition.Includes is null)
            {
                problems.Add($"{path}: the reserved slot requests is tested with includes only");
            }
            else if (slot != CaseSlotReader.RequestsSlot && condition.Includes is not null)
            {
                problems.Add($"{path}: includes tests the reserved slot requests only, not {slot}");
            }

            foreach (var (name, date) in new[] { ("before", condition.Before), ("after", condition.After) })
            {
                if (date is not null && !Dates.TryParse(date, out _))
                {
                    problems.Add($"{path}.{name}: \"{date}\" is not a date written yyyy-mm-dd");
                }
            }

            return;
        }

        if (operators != 0)
        {
            problems.Add($"{path}: is, in, includes, before, after and known belong to a slot condition");
        }

        if (condition.Not is { } inner)
        {
            Shape(inner, $"{path}.not", problems);
            return;
        }

        var (word, parts) = condition.All is not null ? ("all", condition.All) : ("any", condition.Any!);
        if (parts.Count == 0)
        {
            problems.Add($"{path}.{word}: an empty list");
        }

        for (var index = 0; index < parts.Count; index++)
        {
            Shape(parts[index], $"{path}.{word}[{index}]", problems);
        }
    }

    private static void Collect(SlotCondition condition, List<string> slots)
    {
        if (condition.Slot is { } slot && !slots.Contains(slot))
        {
            slots.Add(slot);
        }

        foreach (var part in (condition.All ?? []).Concat(condition.Any ?? []))
        {
            Collect(part, slots);
        }

        if (condition.Not is { } inner)
        {
            Collect(inner, slots);
        }
    }
}
