using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>A place in a record, written as a path for messages: "deadlines[0].when", "basis[3]".</summary>
public sealed record Located<T>(string Path, T Value);

/// <summary>
/// The walks over a record that the evaluator, the checks and the example runners share, so that each list (the items
/// with ids, the references to statements, the conditions) is written once.
/// </summary>
public static class RecordItems
{
    /// <summary>The kinds of items a worked example or a boundary case can name.</summary>
    public static IReadOnlyList<Located<string>> FindingItemIds(RuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return
        [
            .. record.Deadlines.Select((item, index) => new Located<string>($"deadlines[{index}]", item.Id)),
            .. record.PriorSteps.Select((item, index) => new Located<string>($"priorSteps[{index}]", item.Id)),
            .. (record.Court?.Exits ?? []).Select((item, index) => new Located<string>($"court.exits[{index}]", item.Id)),
            .. record.Documents.Select((item, index) => new Located<string>($"documents[{index}]", item.Id)),
            .. record.Boundaries.Select((item, index) => new Located<string>($"boundaries[{index}]", item.Id)),
        ];
    }

    /// <summary>Every item that has an id, in one namespace (ids must be unique across all of them).</summary>
    public static IReadOnlyList<Located<string>> ItemIds(RuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return
        [
            .. record.Basis.Select((item, index) => new Located<string>($"basis[{index}]", item.Id)),
            .. record.Elements.Select((item, index) => new Located<string>($"elements[{index}]", item.Id)),
            .. record.Requests.Select((item, index) => new Located<string>($"requests[{index}]", item.Id)),
            .. FindingItemIds(record),
            .. record.OpenPoints.Select((item, index) => new Located<string>($"openPoints[{index}]", item.Id)),
            .. (record.Court?.Territory ?? []).Select((item, index) => new Located<string>($"court.territory[{index}]", item.Id)),
        ];
    }

    /// <summary>Every reference that must name a statement of the record (the <c>basis</c> lists, counting, readings, proof, means).</summary>
    public static IReadOnlyList<Located<string>> StatementRefs(RuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var refs = new List<Located<string>>();
        void Add(string path, IEnumerable<string> ids) => refs.AddRange(ids.Select((id, index) => new Located<string>($"{path}[{index}]", id)));

        for (var i = 0; i < record.Elements.Count; i++)
        {
            Add($"elements[{i}].basis", record.Elements[i].Basis);
            for (var j = 0; j < record.Elements[i].Proof.Count; j++)
            {
                Add($"elements[{i}].proof[{j}].basis", record.Elements[i].Proof[j].Basis);
            }
        }

        for (var i = 0; i < record.Requests.Count; i++)
        {
            Add($"requests[{i}].basis", record.Requests[i].Basis);
        }

        for (var i = 0; i < record.Deadlines.Count; i++)
        {
            var deadline = record.Deadlines[i];
            Add($"deadlines[{i}].basis", deadline.Basis);
            Add($"deadlines[{i}].counting.basis", deadline.Counting?.Basis ?? []);
            for (var j = 0; j < deadline.Readings.Count; j++)
            {
                Add($"deadlines[{i}].readings[{j}].basis", deadline.Readings[j].Basis);
            }
        }

        for (var i = 0; i < record.PriorSteps.Count; i++)
        {
            var step = record.PriorSteps[i];
            Add($"priorSteps[{i}].basis", step.Basis);
            for (var j = 0; j < step.NotRequiredWhen.Count; j++)
            {
                Add($"priorSteps[{i}].notRequiredWhen[{j}].basis", step.NotRequiredWhen[j].Basis);
            }

            for (var j = 0; j < step.AcceptedMeans.Count; j++)
            {
                Add($"priorSteps[{i}].acceptedMeans[{j}].basis", step.AcceptedMeans[j].Basis);
            }
        }

        for (var i = 0; i < record.Documents.Count; i++)
        {
            Add($"documents[{i}].basis", record.Documents[i].Basis);
        }

        for (var i = 0; i < record.Boundaries.Count; i++)
        {
            Add($"boundaries[{i}].basis", record.Boundaries[i].Basis);
        }

        for (var i = 0; i < record.OpenPoints.Count; i++)
        {
            for (var j = 0; j < record.OpenPoints[i].Readings.Count; j++)
            {
                Add($"openPoints[{i}].readings[{j}].basis", record.OpenPoints[i].Readings[j].Basis);
            }
        }

        if (record.Court is { } court)
        {
            if (court.Section is { } section)
            {
                Add("court.section.basis", section.Basis);
                for (var j = 0; j < section.Alternatives.Count; j++)
                {
                    Add($"court.section.alternatives[{j}].basis", section.Alternatives[j].Basis);
                }
            }

            for (var j = 0; j < court.Territory.Count; j++)
            {
                Add($"court.territory[{j}].basis", court.Territory[j].Basis);
            }

            for (var j = 0; j < court.Exits.Count; j++)
            {
                Add($"court.exits[{j}].basis", court.Exits[j].Basis);
            }
        }

        return refs;
    }

    /// <summary>Every condition of the record, with its path.</summary>
    public static IReadOnlyList<Located<SlotCondition>> ConditionsOf(RuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var conditions = new List<Located<SlotCondition>>();
        void Add(string path, SlotCondition? condition)
        {
            if (condition is not null)
            {
                conditions.Add(new Located<SlotCondition>(path, condition));
            }
        }

        for (var i = 0; i < record.Requests.Count; i++)
        {
            Add($"requests[{i}].when", record.Requests[i].When);
        }

        for (var i = 0; i < record.Deadlines.Count; i++)
        {
            Add($"deadlines[{i}].when", record.Deadlines[i].When);
        }

        for (var i = 0; i < record.PriorSteps.Count; i++)
        {
            Add($"priorSteps[{i}].requiredWhen", record.PriorSteps[i].RequiredWhen);
            for (var j = 0; j < record.PriorSteps[i].NotRequiredWhen.Count; j++)
            {
                Add($"priorSteps[{i}].notRequiredWhen[{j}].when", record.PriorSteps[i].NotRequiredWhen[j].When);
            }
        }

        for (var i = 0; i < record.Documents.Count; i++)
        {
            Add($"documents[{i}].requiredWhen", record.Documents[i].RequiredWhen);
        }

        for (var i = 0; i < record.Boundaries.Count; i++)
        {
            Add($"boundaries[{i}].when", record.Boundaries[i].When);
        }

        if (record.Court is { } court)
        {
            for (var j = 0; j < (court.Section?.Alternatives.Count ?? 0); j++)
            {
                Add($"court.section.alternatives[{j}].when", court.Section!.Alternatives[j].When);
            }

            for (var j = 0; j < court.Territory.Count; j++)
            {
                Add($"court.territory[{j}].when", court.Territory[j].When);
            }

            for (var j = 0; j < court.Exits.Count; j++)
            {
                Add($"court.exits[{j}].when", court.Exits[j].When);
            }
        }

        return conditions;
    }

    /// <summary>Every grading the record carries, with its path; null where an item that can be graded has none.</summary>
    public static IReadOnlyList<Located<Grading?>> Gradings(RuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return
        [
            .. record.Basis.Select((item, index) => new Located<Grading?>($"basis[{index}]", item.Grading)),
            .. record.Elements.Select((item, index) => new Located<Grading?>($"elements[{index}]", item.Grading)),
            .. record.Requests.Select((item, index) => new Located<Grading?>($"requests[{index}]", item.Grading)),
            .. record.Deadlines.Select((item, index) => new Located<Grading?>($"deadlines[{index}]", item.Grading)),
            .. record.PriorSteps.Select((item, index) => new Located<Grading?>($"priorSteps[{index}]", item.Grading)),
            .. record.Documents.Select((item, index) => new Located<Grading?>($"documents[{index}]", item.Grading)),
            .. record.Boundaries.Select((item, index) => new Located<Grading?>($"boundaries[{index}]", item.Grading)),
            .. (record.Court?.Territory ?? []).Select((item, index) => new Located<Grading?>($"court.territory[{index}]", item.Grading)),
            .. (record.Court?.Exits ?? []).Select((item, index) => new Located<Grading?>($"court.exits[{index}]", item.Grading)),
            .. record.Court?.Section is { } section ? [new Located<Grading?>("court.section", section.Grading)] : Array.Empty<Located<Grading?>>(),
        ];
    }

    /// <summary>
    /// The grading of the item a finding names (a deadline, prior step, exit, document or boundary, or "section"), and
    /// whether such an item exists.
    /// </summary>
    public static bool TryGetFindingGrading(RuleRecord record, string reference, out Grading? grading)
    {
        ArgumentNullException.ThrowIfNull(record);
        grading = null;
        if (reference == RuleEvaluator.SectionRef)
        {
            grading = record.Court?.Section?.Grading;
            return record.Court?.Section is not null;
        }

        var item = (object?)record.Deadlines.FirstOrDefault(x => x.Id == reference)
            ?? (object?)record.PriorSteps.FirstOrDefault(x => x.Id == reference)
            ?? (object?)record.Court?.Exits.FirstOrDefault(x => x.Id == reference)
            ?? (object?)record.Documents.FirstOrDefault(x => x.Id == reference)
            ?? record.Boundaries.FirstOrDefault(x => x.Id == reference);

        grading = item switch
        {
            Deadline deadline => deadline.Grading,
            PriorStep step => step.Grading,
            CourtExit exit => exit.Grading,
            RequiredDocument document => document.Grading,
            Boundary boundary => boundary.Grading,
            _ => null,
        };
        return item is not null;
    }

    /// <summary>
    /// Whether an item is a matter of reading, as rules/GRADES.md defines it (condition 3): the item or a statement of its
    /// basis says <c>interpretive</c>, or it is a deadline without <c>counting</c>. An interpretive item declares a warning at most.
    /// </summary>
    public static bool IsInterpretive(RuleRecord record, bool itemFlag, IEnumerable<string> basis, bool isDeadlineWithoutCounting = false)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(basis);
        if (itemFlag || isDeadlineWithoutCounting)
        {
            return true;
        }

        var interpretive = record.Basis.Where(statement => statement.Interpretive).Select(statement => statement.Id).ToHashSet(StringComparer.Ordinal);
        return basis.Any(interpretive.Contains);
    }

    public static bool IsInterpretive(RuleRecord record, Deadline deadline)
    {
        ArgumentNullException.ThrowIfNull(deadline);
        return IsInterpretive(record, deadline.Interpretive, deadline.Basis, deadline.Counting is null);
    }

    public static bool IsInterpretive(RuleRecord record, PriorStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return IsInterpretive(record, step.Interpretive, step.Basis);
    }
}
