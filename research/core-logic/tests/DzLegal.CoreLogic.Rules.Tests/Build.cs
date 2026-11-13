using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Contracts.Tests;
using DzLegal.Domain;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>Small records written in the test code (not the handbook's examples), and the helpers that run them.</summary>
internal static class Build
{
    public static readonly DateOnly Petition = new(2026, 3, 1);

    public static LocalizedText Text(string en) => new("نص", en);

    public static Statement Statement(string id, bool interpretive = false, string code = "cpca", string article = "1") => new()
    {
        Id = id,
        Role = StatementRole.Procedure,
        Cite = new Cite { Code = code, Article = article },
        Says = Text(id),
        Interpretive = interpretive,
    };

    public static RuleRecord Record(string id = "unit") => new()
    {
        SchemaVersion = "1.0",
        Kind = RecordKind.Module,
        Id = id,
        Label = Text(id),
        Verified = false,
        Stage = RecordStage.Final,
        DerivedBy = new Derivation { Role = "test", Model = "test", Date = "2026-11-13" },
        Basis = [Statement("s1")],
    };

    public static Grading Grade(string grade) => new() { Grade = grade, Reasons = [], ToCheck = grade == Grades.D };

    public static SlotCondition Is(string slot, string value) => new() { Slot = slot, Is = value };

    public static Deadline Deadline(
        string id = "d1",
        int days = 10,
        SlotCondition? when = null,
        FindingEffect expired = FindingEffect.BlockCleanExport,
        int? band = null,
        string grade = Grades.A) => new()
        {
            Id = id,
            Kind = DeadlineKind.Forfeiture,
            Period = new Period { Value = days, Unit = PeriodUnit.Day },
            StartsFrom = new PeriodStart { Slot = "start", Description = Text("start") },
            Counting = new CountingRule { Method = CountingMethod.ExcludeFirstDay, Basis = ["s1"] },
            RaisedBy = RaisedBy.Unclear,
            When = when,
            EffectWhenExpired = expired,
            WarnBandDays = band,
            Message = Text($"deadline {id}"),
            Basis = ["s1"],
            Grading = Grade(grade),
        };

    public static PriorStep Step(
        string id = "p1",
        SlotCondition? requiredWhen = null,
        IReadOnlyList<SlotCondition>? exemptions = null,
        FindingEffect missing = FindingEffect.BlockCleanExport,
        FindingEffect unknown = FindingEffect.Ask,
        string grade = Grades.A) => new()
        {
            Id = id,
            Kind = PriorStepKind.Other,
            RequiredWhen = requiredWhen,
            NotRequiredWhen = [.. (exemptions ?? []).Select(when => new StepExemption { When = when, Basis = ["s1"] })],
            Slots = ["done"],
            EffectWhenMissing = missing,
            EffectWhenUnknown = unknown,
            Message = Text($"step {id}"),
            Basis = ["s1"],
            Grading = Grade(grade),
        };

    public static CourtExit Exit(string id, SlotCondition when) => new()
    {
        Id = id,
        When = when,
        GoesTo = ExitDestination.AdministrativeCourt,
        Effect = FindingEffect.Warn,
        Message = Text($"exit {id}"),
        Basis = ["s1"],
        Grading = Grade(Grades.B),
    };

    public static Boundary Boundary(string id, SlotCondition? when) => new()
    {
        Id = id,
        LooksLike = Text("looks"),
        IsActually = "outside:test",
        TellingFacts = [Text("fact")],
        When = when,
        Message = Text($"boundary {id}"),
        Basis = ["s1"],
        Grading = Grade(Grades.B),
    };

    /// <summary>"a=yes;b=no;requests=wages,damages". A slot written "name~value" is proposed by the reader, not confirmed.</summary>
    public static CaseSlots Case(string slots = "", CourtSection? section = null)
    {
        var values = new List<SlotValue>();
        var requests = new List<string>();
        foreach (var pair in slots.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var proposed = pair.Contains('~', StringComparison.Ordinal);
            var parts = pair.Split(proposed ? '~' : '=');
            if (parts[0] == "requests")
            {
                requests.AddRange(parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries));
                continue;
            }

            values.Add(new SlotValue { Slot = parts[0], Value = parts[1], Origin = proposed ? SlotOrigin.Proposed : SlotOrigin.Confirmed });
        }

        return new CaseSlots { ClaimIds = [], Values = values, Requests = requests, Section = section };
    }

    public static RuleOutcome Run(RuleRecord record, CaseSlots slots, DateOnly? petition = null) =>
        new RuleEvaluator().Evaluate([record], slots, petition ?? Petition);

    public static RuleFinding Finding(this RuleOutcome outcome, string reference) => Assert.Single(outcome.Findings, finding => finding.Ref == reference);

    public static List<string> Asked(this RuleOutcome outcome) => [.. outcome.Questions.Select(question => question.Slot)];

    public static SlotCondition Condition(string json) => JsonSerializer.Deserialize<SlotCondition>(json, ContractJson.Options)!;

    public static RuleRecord Load(string json) => JsonSerializer.Deserialize<RuleRecord>(json, ContractJson.Options)!;

    public static string ModuleJson => HandbookFiles.ExampleText("module.labour-prelitigation.example.json");

    public static string ClaimJson => HandbookFiles.ExampleText("claim.unpaid-wages.example.json");

    /// <summary>A copy of a record's JSON with an edit applied (a planted error, a renamed id).</summary>
    public static string Edit(string json, Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        edit(root);
        return root.ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    public static JsonObject Item(this JsonObject record, string list, string id) =>
        record[list]!.AsArray().Single(item => item!["id"]!.GetValue<string>() == id)!.AsObject();

    /// <summary>The record as a deriver would return it: stage A, and none of the fields a script writes.</summary>
    public static string AsDerivation(string json) => Edit(json, root =>
    {
        root["stage"] = "derivationA";
        Strip(root);
    });

    private static void Strip(JsonNode? node)
    {
        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                Strip(item);
            }
        }
        else if (node is JsonObject obj)
        {
            foreach (var name in new[] { "quote", "source", "grading", "shownEffect", "toCheck", "agreementBeforeReconciliation" })
            {
                obj.Remove(name);
            }

            foreach (var (_, child) in obj.ToList())
            {
                Strip(child);
            }
        }
    }
}
