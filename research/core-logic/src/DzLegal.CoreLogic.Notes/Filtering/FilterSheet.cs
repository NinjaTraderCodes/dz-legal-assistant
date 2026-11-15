using System.Text;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;

namespace DzLegal.CoreLogic.Notes.Filtering;

/// <summary>One option of a sheet question: its letter, its Arabic text, and the label value it stands for.</summary>
public sealed record SheetOption(string Letter, string Text, string Value);

/// <summary>
/// One question of the closed sheet. <see cref="Path"/> is the blank path of the field it checks (notes-pipeline.md,
/// section 5); <see cref="Expected"/> the values the scenario's label accepts; <see cref="Counted"/> whether the field counts
/// for the one-fifth rule on the scenario's side (a label other than notMentioned or unknown).
/// </summary>
public sealed record SheetQuestion(string Kind, string Path, string Text, IReadOnlyList<SheetOption> Options,
    IReadOnlySet<string> Expected, bool Counted);

/// <summary>
/// The closed sheet of prompts/notes-filter-local.md, version <c>filter-sheet-1</c>, built by code from the scenario and the
/// note's written labels: never the answers. Options are shuffled per note by a seed, "the note does not mention it" last.
/// </summary>
public sealed class FilterSheet
{
    public const string Version = "filter-sheet-1";
    public const string NotMentioned = "notMentioned";
    public const string NoneOfThese = "none";
    private const string Letters = "abcdefghijkl";
    private const string SaysNothing = "لا تذكر المذكرة ذلك";

    public FilterSheet(IReadOnlyList<SheetQuestion> questions) => Questions = questions;

    public IReadOnlyList<SheetQuestion> Questions { get; }

    /// <summary>The questions as the prompt shows them: numbered from 1, each option on its own line with its letter.</summary>
    public string Text()
    {
        var text = new StringBuilder();
        for (var i = 0; i < Questions.Count; i++)
        {
            text.Append(i + 1).Append(". ").Append(Questions[i].Text).Append('\n');
            foreach (var o in Questions[i].Options)
            {
                text.Append("   ").Append(o.Letter).Append(") ").Append(o.Text).Append('\n');
            }
        }

        return text.ToString().TrimEnd('\n');
    }

    /// <summary>The sheet of one note. <paramref name="seed"/> fixes the decoys and the order of options.</summary>
    public static FilterSheet Build(SheetLabels labels, JsonObject scenario, JsonObject written, int seed)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(written);
        var rng = new Rng(seed);
        var data = labels.Data;
        var questions = new List<SheetQuestion>();
        var claims = scenario["claims"]!.AsArray().Select(c => (string)c!).Where(c => c != "outside").ToList();
        var outside = claims.Count == 0;
        var gold = written["provenance"]!["writerLabels"]!.AsObject();

        SheetQuestion Ask(string kind, string path, string text, IReadOnlyList<(string Text, string Value)> options,
            IEnumerable<string> expected, bool counted, bool lastFixed)
        {
            var shuffled = (lastFixed ? options.Take(options.Count - 1) : options).ToList();
            for (var k = shuffled.Count - 1; k > 0; k--)
            {
                var j = rng.Between(0, k);
                (shuffled[k], shuffled[j]) = (shuffled[j], shuffled[k]);
            }

            if (lastFixed)
            {
                shuffled.Add(options[^1]);
            }

            return new SheetQuestion(kind, path, text, [.. shuffled.Select((o, i) => new SheetOption(Letters[i].ToString(), o.Text, o.Value))],
                expected.ToHashSet(StringComparer.Ordinal), counted);
        }

        (string, string)[] yesNo = [("نعم", "yes"), ("لا", "no")];

        // Claim types: the scenario's own, and three distractors from other areas. None for an `outside` scenario.
        if (!outside)
        {
            foreach (var claim in claims)
            {
                questions.Add(Ask("claim", "claim", $"هل تتعلق هذه المذكرة بدعوى: «{labels.ClaimLabel[claim]}»؟", yesNo, ["yes"], true, false));
            }

            var areas = claims.Select(labels.Area).ToHashSet(StringComparer.Ordinal);
            var others = data.Claims.Select(c => c.Id).Where(c => !areas.Contains(labels.Area(c))).ToList();
            foreach (var c in Draw(rng, others, 3))
            {
                questions.Add(Ask("distractorClaim", "decoy", $"هل تتعلق هذه المذكرة بدعوى: «{labels.ClaimLabel[c]}»؟", yesNo, ["no"], false, false));
            }
        }

        // The plaintiff's role, for a claim type that offers a choice.
        foreach (var claim in claims.Where(c => data.ClaimById[c].Roles.Count > 1))
        {
            var role = scenario["roles"]?[claim];
            var options = labels.RoleLabels[claim].Select(r => (r.Label, r.Position)).Append((SaysNothing, "unknown")).ToList();
            questions.Add(Ask("role", $"roles/{claim}", "ما صفة المدعي في هذه الدعوى؟", options,
                [role is null ? "unknown" : (string)role["position"]!], role is not null, true));
        }

        // Every element of the scenario's claim types, and two decoys.
        (string, string)[] polarity = [("تذكر أن ذلك حصل", "stated"), ("تذكر أن ذلك لم يحصل", "denied"),
            ("تذكر أن الطرفين مختلفان في حصوله", "disputed"), ("لا تذكره المذكرة", NotMentioned)];
        foreach (var e in scenario["events"]?.AsArray() ?? [])
        {
            var claim = (string)e!["claim"]!;
            var kind = (string)e["kind"]!;
            var label = data.ClaimById[claim].Elements.First(x => x.Kind == kind).Label;
            var p = (string)e["polarity"]!;
            questions.Add(Ask("element", $"events/{claim}/{kind}/polarity", $"ماذا تقول المذكرة عن: «{label}»؟", polarity, [p],
                p != NotMentioned, true));
        }

        var near = new HashSet<string>(claims, StringComparer.Ordinal);
        if ((string?)scenario["resembles"] is string resembled)
        {
            near.Add(resembled);
        }

        var ownKinds = near.SelectMany(c => data.ClaimById[c].Elements.Select(x => x.Kind)).ToHashSet(StringComparer.Ordinal);
        var decoys = data.Claims.SelectMany(c => c.Elements).Where(x => !ownKinds.Contains(x.Kind))
            .GroupBy(x => x.Kind).Select(g => g.First()).ToList();
        foreach (var d in Draw(rng, decoys, 2))
        {
            questions.Add(Ask("decoyElement", "decoy", $"ماذا تقول المذكرة عن: «{d.Label}»؟", polarity, [NotMentioned], false, true));
        }

        // The slots of yes/no or choice type asked for the claim types (not for a note outside the catalogue).
        if (!outside)
        {
            foreach (var (id, value) in scenario["slots"]?.AsObject() ?? [])
            {
                var slot = data.Slots.First(s => s.Id == id);
                if (slot.Type is not ("yesNoUnknown" or "choice"))
                {
                    continue;
                }

                var options = slot.Type == "choice"
                    ? labels.ChoiceLabels[id].Select(c => (c.Label, c.Id)).Append((SaysNothing, "unknown")).ToList()
                    : [("نعم", "yes"), ("لا", "no"), (SaysNothing, "unknown")];
                var v = (string)value!;
                questions.Add(Ask("slot", $"slots/{id}", labels.SlotQuestion[id], options, [v], v != "unknown", true));
            }
        }

        // Each value of the scenario that has a role: what it is for.
        foreach (var value in gold["values"]?.AsArray() ?? [])
        {
            var roles = value!["roles"]!.AsArray().Select(r => (string)r!).ToList();
            if (roles.Count == 0)
            {
                continue;
            }

            var kind = (string)value["kind"]!;
            var candidates = labels.RoleCandidates(scenario, kind);
            var chosen = candidates.Where(c => roles.Contains(c.Role)).ToList();
            chosen.AddRange(Draw(rng, [.. candidates.Where(c => !roles.Contains(c.Role))], 10 - chosen.Count));
            var options = chosen.Select(c => (c.Label, c.Role)).Append(("لا شيء مما سبق", NoneOfThese)).ToList();
            var at = value["at"]!;
            var noun = kind == "date" ? "هذا التاريخ" : "هذا المبلغ";
            questions.Add(Ask("value", $"values/{at["field"]}:{at["start"]}/roles", $"إلى ماذا يعود {noun}: «{at["quote"]}»؟", options, roles, true, true));
        }

        // The scenario's requests, and one decoy request.
        var requestKinds = (scenario["requests"]?.AsArray() ?? []).Select(r => (string)r!["kind"]!).ToList();
        foreach (var kind in requestKinds.Where(labels.RequestLabel.ContainsKey))
        {
            questions.Add(Ask("request", $"requests/{kind}", $"هل تطلب المذكرة: «{labels.RequestLabel[kind]}»؟", yesNo, ["yes"], true, false));
        }

        var listed = near.SelectMany(c => data.ClaimById[c].Requests.Select(r => r.Kind)).Concat(requestKinds).ToHashSet(StringComparer.Ordinal);
        foreach (var kind in Draw(rng, [.. labels.RequestLabel.Keys.Where(k => !listed.Contains(k)).Order(StringComparer.Ordinal)], 1))
        {
            questions.Add(Ask("decoyRequest", "decoy", $"هل تطلب المذكرة: «{labels.RequestLabel[kind]}»؟", yesNo, ["no"], false, false));
        }

        return new FilterSheet(questions);
    }

    private static List<T> Draw<T>(Rng rng, IReadOnlyList<T> items, int count)
    {
        var pool = items.ToList();
        var chosen = new List<T>();
        while (chosen.Count < count && pool.Count > 0)
        {
            var i = rng.Between(0, pool.Count - 1);
            chosen.Add(pool[i]);
            pool.RemoveAt(i);
        }

        return chosen;
    }
}

/// <summary>The Arabic labels the sheet uses: claim types (data/law/claims), roles and requests (claim-elements.json), slots.</summary>
public sealed class SheetLabels
{
    public SheetLabels(SamplerData data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        ClaimLabel = Directory.GetFiles(data.Files.Claims, "*.json").SelectMany(f => NotesJson.Load(f)["claims"]!.AsArray())
            .ToDictionary(c => (string)c!["id"]!, c => (string)c!["label"]!["ar"]!, StringComparer.Ordinal);
        var catalogue = NotesJson.Load(data.Files.ClaimElements)["claims"]!.AsArray();
        RoleLabels = catalogue.ToDictionary(c => (string)c!["claim"]!,
            c => (IReadOnlyList<(string Position, string Label)>)[.. c!["roles"]!.AsArray().Select(r => ((string)r!["position"]!, (string)r["label"]!))],
            StringComparer.Ordinal);
        var requests = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var c in catalogue)
        {
            foreach (var r in c!["requests"]!.AsArray())
            {
                requests.TryAdd((string)r!["kind"]!, (string)r["label"]!);
            }
        }

        RequestLabel = requests;
        var slots = NotesJson.Load(Path.Combine(data.Files.CoreLogic, "slots.json"))["slots"]!.AsArray();
        SlotQuestion = slots.ToDictionary(s => (string)s!["id"]!, s => (string)s!["question"]!["ar"]!, StringComparer.Ordinal);
        ChoiceLabels = slots.ToDictionary(s => (string)s!["id"]!,
            s => (IReadOnlyList<(string Id, string Label)>)[.. (s!["choices"]?.AsArray() ?? []).Select(c => ((string)c!["id"]!, (string)c["label"]!["ar"]!))],
            StringComparer.Ordinal);
    }

    public SamplerData Data { get; }

    public IReadOnlyDictionary<string, string> ClaimLabel { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<(string Position, string Label)>> RoleLabels { get; }

    public IReadOnlyDictionary<string, string> RequestLabel { get; }

    public IReadOnlyDictionary<string, string> SlotQuestion { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<(string Id, string Label)>> ChoiceLabels { get; }

    public string Area(string claim) => Data.Tables["areas"]!.AsObject().First(a => SamplerData.Strings(a.Value).Contains(claim)).Key;

    /// <summary>The roles a value of this kind could have in the scenario: its events that take one, its requests, its date slots.</summary>
    public List<(string Role, string Label)> RoleCandidates(JsonObject scenario, string kind)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var list = new List<(string, string)>();
        var claims = scenario["claims"]!.AsArray().Select(c => (string)c!).Where(c => c != "outside");
        foreach (var e in claims.SelectMany(c => Data.ClaimById[c].Elements).Where(e => kind == "date" ? e.HasDate : e.HasAmount)
                     .GroupBy(e => e.Kind).Select(g => g.First()))
        {
            list.Add((e.Kind, e.Label));
        }

        if (kind == "amount")
        {
            foreach (var r in scenario["requests"]?.AsArray() ?? [])
            {
                var k = (string)r!["kind"]!;
                if (RequestLabel.TryGetValue(k, out var label) && list.All(x => x.Item1 != k))
                {
                    list.Add((k, label));
                }
            }
        }
        else
        {
            foreach (var slot in Writing.WriterLabels.DateSlots.Where(s => scenario["slots"]?[s] is not null))
            {
                list.Add((slot, SlotQuestion[slot].TrimEnd('؟')));
            }
        }

        return list;
    }
}
