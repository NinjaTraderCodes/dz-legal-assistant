using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Checks;

/// <summary>The checks of <c>rules check</c> (specs/record-checks.md), numbered as there; the last two come from the frame's tests.</summary>
public enum RecordCheck
{
    /// <summary>1. The record matches rule-record.schema.json.</summary>
    Schema,

    /// <summary>2. It loads through ContractJson.Options; it rests on at least one statement.</summary>
    Contract,

    /// <summary>3. Ids are unique; every reference resolves, in the record or in an imported module's final record.</summary>
    References,

    /// <summary>4. Every slot used is declared; every slot declared exists; slot types fit their use; a final record proposes none.</summary>
    Slots,

    /// <summary>5. Events, request kinds and claim ids exist.</summary>
    Vocabulary,

    /// <summary>6. Only a firm forfeiture or a prior step may declare a block; an interpretive item and an exit declare a warning.</summary>
    Effects,

    /// <summary>7. Each condition has exactly one of all, any, not, slot (and a slot condition one operator).</summary>
    Conditions,

    /// <summary>Things the evaluator cannot run: a prior step without its own slot, a period below one, a negative band.</summary>
    Structure,

    /// <summary>8. Script fields: never in an agent's file; always in a final record.</summary>
    ScriptFields,

    /// <summary>9. A claim record carries every element and request kind of claim-elements.json, or says why not.</summary>
    ClaimElements,

    /// <summary>10. With --store: every cite is in the store, not repealed, with the paragraphs it names.</summary>
    Store,

    /// <summary>A quote's hash is the hash of its text; a deadline's extracted words are in its quotes.</summary>
    Quotes,

    /// <summary>The mark "to check" is grade D exactly; an example's shown effect follows the grade table.</summary>
    Grades,
}

public sealed record RecordProblem(RecordCheck Check, string Message)
{
    public override string ToString() => $"[{Check}] {Message}";
}

public sealed record RecordCheckResult(RuleRecord? Record, IReadOnlyList<RecordProblem> Problems)
{
    public bool Passed => Problems.Count == 0;

    public IEnumerable<RecordProblem> Of(RecordCheck check) => Problems.Where(problem => problem.Check == check);
}

/// <summary>
/// The one copy of the checks every rule record must pass. The <c>rules check</c> command and the frame's tests both call
/// <see cref="Check"/>; nothing else checks records.
/// </summary>
public static class RecordChecks
{
    private static readonly string[] ScriptOnlyStatementFields = ["quote", "source"];

    public static RecordCheckResult Check(string json, RecordCheckContext context)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(context);
        var problems = new List<RecordProblem>();

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            root = document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            return new RecordCheckResult(null, [new RecordProblem(RecordCheck.Contract, $"not JSON: {exception.Message}")]);
        }

        problems.AddRange(MiniSchema.Validate(context.Schema, root).Select(message => new RecordProblem(RecordCheck.Schema, message)));

        RuleRecord? record;
        try
        {
            record = JsonSerializer.Deserialize<RuleRecord>(json, ContractJson.Options);
        }
        catch (JsonException exception)
        {
            problems.Add(new RecordProblem(RecordCheck.Contract, exception.Message));
            return new RecordCheckResult(null, problems);
        }

        if (record is null)
        {
            problems.Add(new RecordProblem(RecordCheck.Contract, "the file holds null"));
            return new RecordCheckResult(null, problems);
        }

        if (record.Basis.Count == 0)
        {
            problems.Add(new RecordProblem(RecordCheck.Contract, "basis is empty: a record rests on at least one statement"));
        }

        var report = new Report(problems);
        References(record, context, report);
        Slots(record, context, report);
        Vocabulary(record, context, report);
        Effects(record, report);
        ConditionShapes(record, report);
        Structure(record, report);
        ScriptFields(record, root, context, report);
        ClaimElements(record, context, report);
        Store(record, context, report);
        Quotes(record, report);
        GradeMarks(record, report);
        return new RecordCheckResult(record, problems);
    }

    private sealed class Report(List<RecordProblem> problems)
    {
        public void Add(RecordCheck check, string message) => problems.Add(new RecordProblem(check, message));
    }

    // 3. Ids and references.
    private static void References(RuleRecord record, RecordCheckContext context, Report report)
    {
        foreach (var (name, ids) in new[]
        {
            ("item", RecordItems.ItemIds(record)),
            ("checklist line", record.Checklist.Select((line, i) => new Located<string>($"checklist[{i}]", line.Id)).ToList()),
            ("example", record.Examples.Select((example, i) => new Located<string>($"examples[{i}]", example.Id)).ToList()),
        })
        {
            foreach (var duplicate in ids.GroupBy(id => id.Value, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                report.Add(RecordCheck.References, $"the {name} id {duplicate.Key} is used {duplicate.Count()} times ({string.Join(", ", duplicate.Select(id => id.Path))})");
            }
        }

        var resolver = new Resolver(record, context);
        foreach (var reference in RecordItems.StatementRefs(record))
        {
            if (resolver.Problem(reference.Value, statementOnly: true) is { } problem)
            {
                report.Add(RecordCheck.References, $"{reference.Path}: {problem}");
            }
        }

        for (var i = 0; i < record.Checklist.Count; i++)
        {
            foreach (var id in record.Checklist[i].Refs)
            {
                if (resolver.Problem(id, statementOnly: false) is { } problem)
                {
                    report.Add(RecordCheck.References, $"checklist[{i}].refs: {problem}");
                }
            }
        }

        for (var i = 0; i < record.OpenPoints.Count; i++)
        {
            foreach (var id in record.OpenPoints[i].Refs)
            {
                if (resolver.Problem(id, statementOnly: false) is { } problem)
                {
                    report.Add(RecordCheck.References, $"openPoints[{i}].refs: {problem}");
                }
            }
        }

        var findingItems = RecordItems.FindingItemIds(record).Select(item => item.Value).ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < record.Examples.Count; i++)
        {
            foreach (var finding in record.Examples[i].Expect.Findings)
            {
                var known = finding.Ref == RuleEvaluator.SectionRef ? record.Court?.Section is not null : findingItems.Contains(finding.Ref);
                if (!known)
                {
                    report.Add(RecordCheck.References, $"examples[{i}] ({record.Examples[i].Id}): {finding.Ref} is not a deadline, prior step, exit, document or boundary of the record");
                }
            }
        }

        for (var i = 0; i < record.Imports.Count; i++)
        {
            var import = record.Imports[i];
            var module = context.Module(import.Module);
            foreach (var block in import.Blocks)
            {
                var present = block == "section"
                    ? record.Court?.Section is not null || module?.Court?.Section is not null
                    : resolver.IsLocal($"{import.Module}.{block}") || (module is not null && RecordItems.ItemIds(module).Any(item => item.Value == block));
                if (!present)
                {
                    report.Add(RecordCheck.References, module is null
                        ? $"imports[{i}]: the block {block} is not in the record, and rules/final/{import.Module}.json is not available"
                        : $"imports[{i}]: the module {import.Module} has no block {block}");
                }
            }
        }
    }

    /// <summary>Resolves an id in the record, or, for "&lt;module&gt;.&lt;id&gt;" in a claim not yet flattened, in the module it imports.</summary>
    private sealed class Resolver(RuleRecord record, RecordCheckContext context)
    {
        private readonly HashSet<string> items = RecordItems.ItemIds(record).Select(item => item.Value).ToHashSet(StringComparer.Ordinal);
        private readonly HashSet<string> statements = record.Basis.Select(statement => statement.Id).ToHashSet(StringComparer.Ordinal);

        public bool IsLocal(string id) => items.Contains(id);

        public string? Problem(string id, bool statementOnly)
        {
            if (statementOnly ? statements.Contains(id) : items.Contains(id))
            {
                return null;
            }

            var dot = id.IndexOf('.', StringComparison.Ordinal);
            var import = dot > 0 ? record.Imports.FirstOrDefault(line => line.Module == id[..dot]) : null;
            if (import is null)
            {
                return statementOnly ? $"{id} names no statement of the record" : $"{id} names no item of the record";
            }

            if (context.Module(import.Module) is not { } module)
            {
                return $"{id} is not in the record, and rules/final/{import.Module}.json is not available to resolve it";
            }

            // What flatten-imports would copy: the blocks named, and every statement they cite.
            var local = id[(dot + 1)..];
            var blockPaths = RecordItems.ItemIds(module).Where(item => import.Blocks.Contains(item.Value)).Select(item => item.Path).ToList();
            if (import.Blocks.Contains("section"))
            {
                blockPaths.Add("court.section");
            }

            var imported = import.Blocks.Concat(RecordItems.StatementRefs(module)
                .Where(reference => blockPaths.Any(path => reference.Path.StartsWith(path + ".", StringComparison.Ordinal)))
                .Select(reference => reference.Value)).ToHashSet(StringComparer.Ordinal);
            var exists = statementOnly ? module.Basis.Any(statement => statement.Id == local) : RecordItems.ItemIds(module).Any(item => item.Value == local);
            return imported.Contains(local) && exists ? null : $"{id}: the record does not import {local} from {import.Module}";
        }
    }

    // 4. Slots.
    private static void Slots(RuleRecord record, RecordCheckContext context, Report report)
    {
        var catalogue = context.Slots.Slots.ToDictionary(slot => slot.Id, StringComparer.Ordinal);
        var proposed = new Dictionary<string, ProposedSlot>(StringComparer.Ordinal);
        foreach (var slot in record.ProposedSlots)
        {
            proposed.TryAdd(slot.Id, slot);
        }

        var declared = record.Slots.ToHashSet(StringComparer.Ordinal);
        foreach (var slot in record.Slots)
        {
            if (slot != CaseSlotReader.RequestsSlot && !catalogue.ContainsKey(slot) && !proposed.ContainsKey(slot))
            {
                report.Add(RecordCheck.Slots, $"slots: {slot} is neither in slots.json nor in the record's proposedSlots");
            }
        }

        if (record.Stage == RecordStage.Final && record.ProposedSlots.Count > 0)
        {
            report.Add(RecordCheck.Slots, $"a final record proposes no slot (move {string.Join(", ", proposed.Keys)} into slots.json first)");
        }

        (SlotType Type, IReadOnlyList<string> Choices)? TypeOf(string slot) =>
            catalogue.TryGetValue(slot, out var known) ? (known.Type, known.Choices.Select(choice => choice.Id).ToList())
            : proposed.TryGetValue(slot, out var mine) ? (mine.Type, mine.Choices.Select(choice => choice.Id).ToList())
            : null;

        void Used(string path, string slot, SlotType? needs = null)
        {
            if (!declared.Contains(slot))
            {
                report.Add(RecordCheck.Slots, $"{path}: the slot {slot} is not in the record's slots");
            }

            if (needs is { } type && TypeOf(slot) is { } actual && actual.Type != type)
            {
                report.Add(RecordCheck.Slots, $"{path}: the slot {slot} is a {actual.Type} slot; this use needs a {type} slot");
            }
        }

        void Value(string path, string slot, string value)
        {
            if (value == CaseSlotReader.UnknownValue || TypeOf(slot) is not { } type)
            {
                return;
            }

            var fits = type.Type switch
            {
                SlotType.YesNoUnknown => value is "yes" or "no",
                SlotType.Date => Dates.TryParse(value, out _),
                SlotType.Amount => value.Length > 0 && value.All(char.IsAsciiDigit),
                _ => type.Choices.Contains(value),
            };
            if (!fits)
            {
                report.Add(RecordCheck.Slots, $"{path}: \"{value}\" is not a value of the {type.Type} slot {slot}");
            }
        }

        foreach (var condition in RecordItems.ConditionsOf(record))
        {
            Leaves(condition.Value, condition.Path, (path, leaf) =>
            {
                var slot = leaf.Slot!;
                Used(path, slot, leaf.Before is not null || leaf.After is not null ? SlotType.Date : null);
                if (leaf.Is is { } value)
                {
                    Value(path, slot, value);
                }

                foreach (var item in leaf.In ?? [])
                {
                    Value(path, slot, item);
                }
            });
        }

        for (var i = 0; i < record.Deadlines.Count; i++)
        {
            Used($"deadlines[{i}].startsFrom", record.Deadlines[i].StartsFrom.Slot, SlotType.Date);
        }

        for (var i = 0; i < record.PriorSteps.Count; i++)
        {
            var steps = record.PriorSteps[i].Slots;
            for (var j = 0; j < steps.Count; j++)
            {
                // The first slot of a prior step is the step's own: was it done?
                Used($"priorSteps[{i}].slots[{j}]", steps[j], j == 0 ? SlotType.YesNoUnknown : null);
            }
        }

        for (var i = 0; i < record.Elements.Count; i++)
        {
            foreach (var slot in record.Elements[i].Slots)
            {
                Used($"elements[{i}].slots", slot);
            }
        }

        for (var i = 0; i < record.Boundaries.Count; i++)
        {
            foreach (var slot in record.Boundaries[i].Slots)
            {
                Used($"boundaries[{i}].slots", slot);
            }
        }

        for (var i = 0; i < record.Examples.Count; i++)
        {
            var example = record.Examples[i];
            foreach (var (slot, value) in example.Slots)
            {
                Used($"examples[{i}].slots", slot);
                if (slot != CaseSlotReader.RequestsSlot && value.ValueKind == JsonValueKind.String)
                {
                    Value($"examples[{i}].slots.{slot}", slot, value.GetString()!);
                }
                else if (slot != CaseSlotReader.RequestsSlot)
                {
                    report.Add(RecordCheck.Slots, $"examples[{i}].slots.{slot}: a slot's value is written as a string");
                }
            }

            foreach (var slot in example.Expect.Questions ?? [])
            {
                Used($"examples[{i}].expect.questions", slot);
            }
        }
    }

    // 5. Events, request kinds, claim ids.
    private static void Vocabulary(RuleRecord record, RecordCheckContext context, Report report)
    {
        void Event(string path, string kind)
        {
            if (!context.EventKinds.Contains(kind))
            {
                report.Add(RecordCheck.Vocabulary, $"{path}: {kind} is not an event of FactVocabulary");
            }
        }

        void Request(string path, string kind)
        {
            if (!context.RequestKinds.Contains(kind))
            {
                report.Add(RecordCheck.Vocabulary, $"{path}: {kind} is not a request kind of FactVocabulary");
            }
        }

        void Claim(string path, string id)
        {
            if (!context.ClaimIds.Contains(id))
            {
                report.Add(RecordCheck.Vocabulary, $"{path}: {id} is not a claim id of data/law/claims");
            }
        }

        for (var i = 0; i < record.Elements.Count; i++)
        {
            foreach (var kind in record.Elements[i].Events)
            {
                Event($"elements[{i}].events", kind);
            }
        }

        for (var i = 0; i < record.Deadlines.Count; i++)
        {
            if (record.Deadlines[i].StartsFrom.Event is { } kind)
            {
                Event($"deadlines[{i}].startsFrom.event", kind);
            }
        }

        for (var i = 0; i < record.Requests.Count; i++)
        {
            Request($"requests[{i}].kind", record.Requests[i].Kind);
        }

        foreach (var condition in RecordItems.ConditionsOf(record))
        {
            Leaves(condition.Value, condition.Path, (path, leaf) =>
            {
                if (leaf.Includes is { } kind)
                {
                    Request(path, kind);
                }
            });
        }

        for (var i = 0; i < record.Examples.Count; i++)
        {
            if (record.Examples[i].Slots.TryGetValue(CaseSlotReader.RequestsSlot, out var requests) && requests.ValueKind == JsonValueKind.Array)
            {
                foreach (var kind in requests.EnumerateArray())
                {
                    Request($"examples[{i}].slots.requests", kind.GetString() ?? string.Empty);
                }
            }
        }

        if (record.Kind == RecordKind.Claim)
        {
            Claim("id", record.Id);
            if (record.Area is null)
            {
                report.Add(RecordCheck.Vocabulary, "area: a claim record names the area of its catalogue file");
            }
        }

        foreach (var id in record.AppliesTo.Where(id => id != "*"))
        {
            Claim("appliesTo", id);
        }

        for (var i = 0; i < record.Boundaries.Count; i++)
        {
            var target = record.Boundaries[i].IsActually;
            if (target.StartsWith("claim:", StringComparison.Ordinal))
            {
                Claim($"boundaries[{i}].isActually", target["claim:".Length..]);
            }
            else if (!target.StartsWith("outside:", StringComparison.Ordinal))
            {
                report.Add(RecordCheck.Vocabulary, $"boundaries[{i}].isActually: \"{target}\" is neither claim:<id> nor outside:<kind>");
            }
        }
    }

    // 6. What a record may declare (rules/GRADES.md, "What a record may declare").
    private static void Effects(RuleRecord record, Report report)
    {
        for (var i = 0; i < record.Deadlines.Count; i++)
        {
            var deadline = record.Deadlines[i];
            if (deadline.EffectWhenExpired is not (FindingEffect.BlockCleanExport or FindingEffect.Warn))
            {
                report.Add(RecordCheck.Effects, $"deadlines[{i}] ({deadline.Id}): effectWhenExpired is blockCleanExport or warn");
            }
            else if (deadline.EffectWhenExpired == FindingEffect.BlockCleanExport && deadline.Kind != DeadlineKind.Forfeiture)
            {
                report.Add(RecordCheck.Effects, $"deadlines[{i}] ({deadline.Id}): only a forfeiture may declare blockCleanExport; a {deadline.Kind} declares warn");
            }
            else if (deadline.EffectWhenExpired == FindingEffect.BlockCleanExport && RecordItems.IsInterpretive(record, deadline))
            {
                report.Add(RecordCheck.Effects, $"deadlines[{i}] ({deadline.Id}): an interpretive deadline (flagged, resting on an interpretive statement, or without counting) declares warn");
            }
        }

        for (var i = 0; i < record.PriorSteps.Count; i++)
        {
            var step = record.PriorSteps[i];
            if (step.EffectWhenMissing is not (FindingEffect.BlockCleanExport or FindingEffect.Warn))
            {
                report.Add(RecordCheck.Effects, $"priorSteps[{i}] ({step.Id}): effectWhenMissing is blockCleanExport or warn");
            }
            else if (step.EffectWhenMissing == FindingEffect.BlockCleanExport && RecordItems.IsInterpretive(record, step))
            {
                report.Add(RecordCheck.Effects, $"priorSteps[{i}] ({step.Id}): an interpretive prior step declares warn");
            }

            if (step.EffectWhenUnknown is not (FindingEffect.Ask or FindingEffect.Warn))
            {
                report.Add(RecordCheck.Effects, $"priorSteps[{i}] ({step.Id}): effectWhenUnknown is ask or warn");
            }
        }

        var exits = record.Court?.Exits ?? [];
        for (var i = 0; i < exits.Count; i++)
        {
            if (exits[i].Effect != FindingEffect.Warn)
            {
                report.Add(RecordCheck.Effects, $"court.exits[{i}] ({exits[i].Id}): an exit declares warn");
            }
        }
    }

    // 7. Conditions.
    private static void ConditionShapes(RuleRecord record, Report report)
    {
        foreach (var condition in RecordItems.ConditionsOf(record))
        {
            foreach (var problem in Conditions.ShapeProblems(condition.Value, condition.Path))
            {
                report.Add(RecordCheck.Conditions, problem);
            }
        }
    }

    private static void Structure(RuleRecord record, Report report)
    {
        for (var i = 0; i < record.PriorSteps.Count; i++)
        {
            if (record.PriorSteps[i].Slots.Count == 0)
            {
                report.Add(RecordCheck.Structure, $"priorSteps[{i}] ({record.PriorSteps[i].Id}): slots is empty; its first slot is the step's own (was it done?)");
            }
        }

        for (var i = 0; i < record.Deadlines.Count; i++)
        {
            var deadline = record.Deadlines[i];
            if (deadline.Period.Value < 1)
            {
                report.Add(RecordCheck.Structure, $"deadlines[{i}] ({deadline.Id}): period.value is at least 1");
            }

            if (deadline.WarnBandDays is < 0)
            {
                report.Add(RecordCheck.Structure, $"deadlines[{i}] ({deadline.Id}): warnBandDays is not negative");
            }
        }
    }

    // 8. Script fields: an agent never types them; a final record always has them.
    private static void ScriptFields(RuleRecord record, JsonElement root, RecordCheckContext context, Report report)
    {
        if (record.Stage != RecordStage.Final)
        {
            WalkScriptFields(root, "$", parentIsGrading: false, context.Quoted, report);
            return;
        }

        for (var i = 0; i < record.Basis.Count; i++)
        {
            var statement = record.Basis[i];
            if (statement.Quote.Count == 0 || statement.Source is null || statement.Grading is null)
            {
                report.Add(RecordCheck.ScriptFields, $"basis[{i}] ({statement.Id}): a final record carries quote, source and grading for every statement");
            }
        }

        // Statements were reported above; the other items here.
        foreach (var grading in RecordItems.Gradings(record).Where(grading => grading.Value is null && !grading.Path.StartsWith("basis[", StringComparison.Ordinal)))
        {
            report.Add(RecordCheck.ScriptFields, $"{grading.Path}: a final record carries a grading for every item that can be graded");
        }

        for (var i = 0; i < record.Examples.Count; i++)
        {
            foreach (var finding in record.Examples[i].Expect.Findings.Where(finding => finding.ShownEffect is null || finding.ToCheck is null))
            {
                report.Add(RecordCheck.ScriptFields, $"examples[{i}] ({record.Examples[i].Id}): {finding.Ref} lacks shownEffect or toCheck, which the grading script writes in a final record");
            }
        }
    }

    private static void WalkScriptFields(JsonElement node, string path, bool parentIsGrading, bool quoted, Report report)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in node.EnumerateArray())
            {
                WalkScriptFields(item, $"{path}[{index++}]", parentIsGrading: false, quoted, report);
            }

            return;
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var member in node.EnumerateObject())
        {
            var forbidden = member.Name switch
            {
                "grading" or "shownEffect" or "agreementBeforeReconciliation" => true,
                "toCheck" => !parentIsGrading,
                _ => ScriptOnlyStatementFields.Contains(member.Name) && !quoted,
            };

            if (forbidden)
            {
                report.Add(RecordCheck.ScriptFields, $"{path}.{member.Name}: written by a script, never in a file an agent wrote{(member.Name is "quote" or "source" ? " (a quoted copy is checked with --quoted)" : string.Empty)}");
                continue;
            }

            WalkScriptFields(member.Value, $"{path}.{member.Name}", member.Name == "grading", quoted, report);
        }
    }

    // 9. Every element and request kind of the claim, or a checklist line that says why not.
    private static void ClaimElements(RuleRecord record, RecordCheckContext context, Report report)
    {
        if (record.Kind != RecordKind.Claim)
        {
            return;
        }

        if (!context.ClaimNeeds.TryGetValue(record.Id, out var needs))
        {
            report.Add(RecordCheck.ClaimElements, $"data/law/claim-elements.json has no entry for {record.Id}");
            return;
        }

        var events = record.Elements.SelectMany(element => element.Events).ToHashSet(StringComparer.Ordinal);
        var requests = record.Requests.Select(request => request.Kind).ToHashSet(StringComparer.Ordinal);
        foreach (var import in record.Imports)
        {
            if (context.Module(import.Module) is { } module)
            {
                requests.UnionWith(module.Requests.Where(request => import.Blocks.Contains(request.Id)).Select(request => request.Kind));
            }
        }

        // A checklist line answers a missing one when it is not "applies" and names the code in its question or its note.
        bool Explained(string code) => record.Checklist.Any(line =>
            line.Answer != ChecklistAnswer.Applies
            && (line.Item.Contains(code, StringComparison.Ordinal) || (line.Note?.Contains(code, StringComparison.Ordinal) ?? false)));

        foreach (var kind in needs.Events.Where(kind => !events.Contains(kind) && !Explained(kind)))
        {
            report.Add(RecordCheck.ClaimElements, $"the element {kind} of claim-elements.json is carried by no elements[].events, and no checklist line says why");
        }

        foreach (var kind in needs.Requests.Where(kind => !requests.Contains(kind) && !Explained(kind)))
        {
            report.Add(RecordCheck.ClaimElements, $"the request kind {kind} of claim-elements.json is in no requests[] (nor imported), and no checklist line says why");
        }
    }

    // 10. Every cite is in the store.
    private static void Store(RuleRecord record, RecordCheckContext context, Report report)
    {
        if (context.Store is not { } store)
        {
            return;
        }

        for (var i = 0; i < record.Basis.Count; i++)
        {
            var cite = record.Basis[i].Cite;
            var name = $"basis[{i}] ({record.Basis[i].Id}): {cite.Code} {cite.Article}{(cite.Version is null ? string.Empty : $" version {cite.Version}")}";
            var entry = store.Get(cite.Code, cite.Article, cite.Version);
            if (entry is null)
            {
                report.Add(RecordCheck.Store, $"{name} is not in the store");
                continue;
            }

            if (entry.Repealed)
            {
                report.Add(RecordCheck.Store, $"{name} is repealed");
                continue;
            }

            foreach (var paragraph in cite.Paragraphs.Where(paragraph => paragraph < 1 || paragraph > entry.ParagraphCount))
            {
                report.Add(RecordCheck.Store, $"{name}: paragraph {paragraph} does not exist (the article has {entry.ParagraphCount})");
            }
        }
    }

    // From the frame: a quote is intact, and a deadline's extracted words are in its quotes.
    private static void Quotes(RuleRecord record, Report report)
    {
        foreach (var statement in record.Basis)
        {
            foreach (var paragraph in statement.Quote.Where(paragraph => paragraph.Sha256 != Sha256(paragraph.Text)))
            {
                report.Add(RecordCheck.Quotes, $"{statement.Id}: the sha256 of paragraph {paragraph.Paragraph} is not the hash of its text");
            }
        }

        var byId = new Dictionary<string, Statement>(StringComparer.Ordinal);
        foreach (var statement in record.Basis)
        {
            byId.TryAdd(statement.Id, statement);
        }

        foreach (var deadline in record.Deadlines)
        {
            var quotes = deadline.Basis.Where(byId.ContainsKey).SelectMany(id => byId[id].Quote).Select(paragraph => paragraph.Text).ToList();
            if (quotes.Count == 0)
            {
                continue;
            }

            var quoted = string.Join(' ', quotes);
            foreach (var (name, words) in new[] { ("period", deadline.Extract?.Period), ("sanction", deadline.Extract?.Sanction) })
            {
                if (words is not null && !quoted.Contains(words, StringComparison.Ordinal))
                {
                    report.Add(RecordCheck.Quotes, $"{deadline.Id}: extract.{name} «{words}» is not in the quoted text of its basis");
                }
            }
        }
    }

    // From the frame: the mark is grade D exactly, and an example's shown effect is the grade table applied to its effect.
    private static void GradeMarks(RuleRecord record, Report report)
    {
        foreach (var grading in RecordItems.Gradings(record))
        {
            if (grading.Value is { } value && value.ToCheck != (value.Grade == Grades.D))
            {
                report.Add(RecordCheck.Grades, $"{grading.Path}: toCheck is true exactly for grade D (grade {value.Grade}, toCheck {value.ToCheck})");
            }
        }

        for (var i = 0; i < record.Examples.Count; i++)
        {
            foreach (var finding in record.Examples[i].Expect.Findings.Where(finding => finding.ShownEffect is not null || finding.ToCheck is not null))
            {
                if (!RecordItems.TryGetFindingGrading(record, finding.Ref, out var grading))
                {
                    continue;
                }

                var (shown, toCheck) = GradeTable.Apply(finding.Effect, GradeTable.GradeOf(grading), usesProposedSlot: false);
                if (finding.ShownEffect is { } written && written != shown)
                {
                    report.Add(RecordCheck.Grades, $"examples[{i}] ({record.Examples[i].Id}): {finding.Ref} shows {RuleEvaluator.Name(written)}; the grade table gives {RuleEvaluator.Name(shown)}");
                }

                if (finding.ToCheck is { } mark && mark != toCheck)
                {
                    report.Add(RecordCheck.Grades, $"examples[{i}] ({record.Examples[i].Id}): {finding.Ref} has toCheck {mark}; its grade gives {toCheck}");
                }
            }
        }
    }

    private static void Leaves(SlotCondition condition, string path, Action<string, SlotCondition> visit)
    {
        if (condition.Slot is not null)
        {
            visit(path, condition);
        }

        for (var i = 0; i < (condition.All?.Count ?? 0); i++)
        {
            Leaves(condition.All![i], $"{path}.all[{i}]", visit);
        }

        for (var i = 0; i < (condition.Any?.Count ?? 0); i++)
        {
            Leaves(condition.Any![i], $"{path}.any[{i}]", visit);
        }

        if (condition.Not is { } inner)
        {
            Leaves(inner, $"{path}.not", visit);
        }
    }

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
