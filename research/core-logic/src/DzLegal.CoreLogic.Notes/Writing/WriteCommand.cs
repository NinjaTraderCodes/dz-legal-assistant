using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;

namespace DzLegal.CoreLogic.Notes.Writing;

/// <summary>One fact the writer must point at: its reference, and whether the writer may leave it out.</summary>
public sealed record FactRef(string Ref, bool Optional);

/// <summary>
/// <c>notes write</c> (specs/notes-pipeline.md, section 3): 20 drawn scenarios per call, never two of one family except the
/// members of one polarity group, which always go together; the prompt prompts/notes-write.md with the labelling guide
/// inside it. For Codex it appends the calls to <c>&lt;work&gt;\calls-&lt;date&gt;.jsonl</c> for the writer pool; for Sonnet it
/// writes one prompt file per call for a fresh no-tool agent.
/// </summary>
public static class WriteCommand
{
    public const int PerCall = 20;

    /// <summary>An English gloss of each request kind of FactVocabulary.Requests, for the prompt's list of kinds.</summary>
    public static readonly IReadOnlyList<(string Kind, string Gloss)> RequestKinds =
    [
        ("payment", "payment of a sum owed"), ("eviction", "eviction from the premises"),
        ("rescission", "rescission (ending) of the contract"), ("damages", "compensation for harm"),
        ("annulment", "annulment of the contract"), ("restitution", "return of what was paid or handed over"),
        ("delivery", "delivery of what was sold"), ("divorce", "divorce at the husband's will"),
        ("tatliq", "judicial divorce at the wife's request, on a ground"), ("khul", "divorce by khul, the wife giving compensation"),
        ("maintenance", "maintenance (nafaqa)"), ("custody", "award of custody of the children"),
        ("custody_transfer", "transfer of custody away from the present custodian"), ("visitation", "visiting rights"),
        ("housing", "housing for the custodian, or a housing allowance"), ("reinstatement", "reinstatement in the job"),
        ("wages", "payment of wages"), ("partition", "partition of jointly owned property"),
        ("cancel_decision", "annulment of an administrative decision"), ("correction", "correction of a civil-status record"),
        ("dissolution", "dissolution of a company"), ("guardianship", "guardianship over a child or a person"),
        ("provisional_execution", "provisional execution of the judgment"), ("other", "any other request"),
    ];

    /// <summary>
    /// The four framings of version 3 of the writer prompt (the owner, 17 November 2026: "several Codex prompt variants"):
    /// who writes the note and when. A call takes the framing of its number (call 1 the first, call 5 the first again).
    /// </summary>
    public static readonly IReadOnlyList<(string Id, string Text)> Framings =
    [
        ("desk", "Write each note as the lawyer writes it at the desk after reading the client's file, before drafting."),
        ("visit", "Write each note as the lawyer takes it down during the client's visit: in the order the client tells it, with the jumps and the repetitions of a conversation, but exact on every fact."),
        ("handover", "Write each note as a lawyer's quick handover to a colleague who will draft the claim: only what the colleague needs, in the fewest words."),
        ("endOfDay", "Write each note as a lawyer's own reminder written in a hurry at the end of the day: uneven and unpolished, but exact on every fact."),
    ];

    public static (string Id, string Text) FramingOf(int callNumber) => Framings[(callNumber - 1 + Framings.Count) % Framings.Count];

    public static int Run(NotesFiles files, string set, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        var calls = int.Parse(options.GetValueOrDefault("--calls") ?? throw new ArgumentException("--calls <n>"), CultureInfo.InvariantCulture);
        var writer = options.GetValueOrDefault("--writer") ?? throw new ArgumentException("--writer codex|sonnet");
        if (writer is not ("codex" or "sonnet"))
        {
            throw new ArgumentException("--writer codex|sonnet");
        }

        var work = options.GetValueOrDefault("--work") ?? @"C:\Users\TheShadow\dz-core-research\notes-work";
        NotesLoader.Guard(work);
        var data = SamplerData.Load(files);
        var scenarios = Scenarios(work, set);
        var queue = QueueFile.Load(work);
        var eligible = queue.Lines.Where(l => (string?)l["set"] == set && (string?)l["state"] == "drawn" && l["writeCall"] is null &&
                                              (int?)l["attempts"] < 2 && (string?)l["writer"] == writer).ToList();
        var made = new List<string>();
        var callsFolder = Path.Combine(work, "calls");
        Directory.CreateDirectory(callsFolder);
        var number = Directory.GetFiles(callsFolder, $"w-{set}-*.json").Length;
        while (made.Count < calls && eligible.Count > 0)
        {
            var batch = Batch(eligible, scenarios);
            if (batch.Count == 0)
            {
                break;
            }

            var call = $"w-{set}-{++number:0000}";
            var chosen = batch.Select(l => scenarios[(string)l["scenario"]!]).ToList();
            var framing = FramingOf(number);
            var prompt = Prompt(files, data, chosen, framing.Text);
            var promptPath = Path.Combine(callsFolder, call + ".prompt.md");
            NotesJson.Write(promptPath, prompt);
            NotesJson.Write(Path.Combine(callsFolder, call + ".json"), new JsonObject
            {
                ["call"] = call, ["set"] = set, ["writer"] = writer,
                ["created"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                ["prompt"] = "notes-write.md@" + TemplateHash(files), ["framing"] = framing.Id,
                ["scenarios"] = new JsonArray([.. chosen.Select(s => (JsonNode)(string)s["id"]!)]),
            }.ToJsonString(NotesJson.Options) + "\n");
            if (writer == "codex")
            {
                File.AppendAllText(Path.Combine(work, $"calls-{DateTime.UtcNow:yyyy-MM-dd}.jsonl"), new JsonObject
                {
                    ["call"] = call, ["prompt"] = promptPath, ["set"] = set,
                    ["schema"] = Path.Combine(files.Prompts, "schemas", "notes-write.schema.json"),
                    ["out"] = Path.Combine(work, "answers", call + ".json"),
                }.ToJsonString(NotesJson.Compact) + "\n");
            }

            foreach (var line in batch)
            {
                line["writeCall"] = call;
                line["attempts"] = ((int?)line["attempts"] ?? 0) + 1;
                eligible.Remove(line);
            }

            made.Add($"{call} ({batch.Count} scenarios)");
        }

        queue.Save();
        Console.WriteLine(made.Count == 0 ? "no call: no drawn scenario left for this writer" : string.Join("\n", made));
        if (writer == "codex" && made.Count > 0)
        {
            Console.WriteLine($"calls file: {Path.Combine(work, $"calls-{DateTime.UtcNow:yyyy-MM-dd}.jsonl")} (codex_run.py pool --queue <it> --writers <2..4>)");
        }

        return 0;
    }

    public static Dictionary<string, JsonObject> Scenarios(string work, string set) =>
        File.ReadLines(Path.Combine(work, "scenarios", set + ".jsonl")).Where(l => l.Trim().Length > 0)
            .Select(l => JsonNode.Parse(l)!.AsObject()).ToDictionary(s => (string)s["id"]!, StringComparer.Ordinal);

    /// <summary>The next call's lines: up to 20, a polarity group whole or not at all, never two of one family otherwise.</summary>
    private static List<JsonObject> Batch(List<JsonObject> eligible, Dictionary<string, JsonObject> scenarios)
    {
        var batch = new List<JsonObject>();
        var families = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in eligible)
        {
            if (batch.Contains(line))
            {
                continue;
            }

            var scenario = scenarios[(string)line["scenario"]!];
            var group = (string?)scenario["group"];
            var members = group is null
                ? [line]
                : eligible.Where(l => (string?)scenarios[(string)l["scenario"]!]["group"] == group).ToList();
            var family = (string)scenario["family"]!;
            if (batch.Count + members.Count > PerCall || families.Contains(family))
            {
                continue;
            }

            batch.AddRange(members);
            families.Add(family);
            if (batch.Count == PerCall)
            {
                break;
            }
        }

        return batch;
    }

    public static string TemplateHash(NotesFiles files) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(files.Prompts, "notes-write.md"))))[..12].ToLowerInvariant();

    public static string Prompt(NotesFiles files, SamplerData data, IReadOnlyList<JsonObject> scenarios, string? framing = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(scenarios);
        var cards = data.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var views = new JsonArray([.. scenarios.Select((s, i) => (JsonNode)WriterView(s, i + 1, cards))]);
        var claims = scenarios.SelectMany(s => s["claims"]!.AsArray().Select(c => (string)c!)).Where(c => c != "outside")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return PromptTemplate.Fill(PromptTemplate.Body(files.Prompts, "notes-write"), new Dictionary<string, string>
        {
            ["framing"] = framing ?? Framings[0].Text,
            ["guide"] = File.ReadAllText(files.Guide).ReplaceLineEndings("\n").Trim(),
            ["elements"] = Elements(data, claims),
            ["requestKinds"] = RequestList(data),
            ["scenarios"] = views.ToJsonString(NotesJson.Options).ReplaceLineEndings("\n"),
        });
    }

    /// <summary>
    /// The scenario as the writer sees it (prompts/notes-write.md, "Placeholders"): without its id, family, group, set,
    /// outcome and petition date; a call-local number; the style card's text and parameters in place of its id; and the
    /// list of the facts to point at, by reference.
    /// </summary>
    public static JsonObject WriterView(JsonObject scenario, int n, IReadOnlyDictionary<string, StyleCard> cards)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(cards);
        var hidden = new HashSet<string>(StringComparer.Ordinal)
        {
            "id", "set", "family", "group", "outcome", "petitionDate", "difficulty", "style", "target", "styleForced",
        };
        var view = new JsonObject { ["n"] = n };
        foreach (var (key, value) in scenario.Where(kv => !hidden.Contains(kv.Key) && kv.Value is not null))
        {
            view[key] = value!.DeepClone();
        }

        // A challenge recipe's forced style value (sampler 1.4) reaches the writer as the card's own values do.
        var forced = (scenario["styleForced"]?.AsObject() ?? []).ToDictionary(kv => kv.Key, kv => (string)kv.Value!, StringComparer.Ordinal);
        var (values, text) = StyleCards.Effective(cards[(string)scenario["style"]!], forced, (string?)scenario["target"]);
        var parameters = new JsonObject();
        foreach (var (name, value) in values)
        {
            parameters[name] = value;
        }

        view["style"] = new JsonObject { ["text"] = text, ["parameters"] = parameters };
        view["factsToPointAt"] = new JsonArray([.. Refs(scenario).Select(r => (JsonNode)(r.Optional ? r.Ref + " (optional)" : r.Ref))]);
        return view;
    }

    /// <summary>Every fact of the scenario the writer must point at (prompts/notes-write.md, step 2), in a fixed order.</summary>
    public static IReadOnlyList<FactRef> Refs(JsonObject scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var refs = new List<FactRef> { new("claim", false) };
        foreach (var (claim, _) in scenario["roles"]?.AsObject() ?? [])
        {
            refs.Add(new($"role:{claim}", false));
        }

        foreach (var e in scenario["events"]?.AsArray() ?? [])
        {
            var key = $"{e!["claim"]}/{e["kind"]}";
            if ((string)e["polarity"]! == "notMentioned")
            {
                continue;
            }

            refs.Add(new($"event:{key}", false));
            if (e["date"] is not null)
            {
                refs.Add(new($"event-date:{key}", false));
            }

            if (e["amount"] is not null)
            {
                refs.Add(new($"event-amount:{key}", false));
            }

            if (e["contest"]?["amount"] is not null)
            {
                refs.Add(new($"contest-amount:{key}", false));
            }
        }

        foreach (var (slot, value) in scenario["slots"]?.AsObject() ?? [])
        {
            if ((string?)value is not null and not "unknown")
            {
                refs.Add(new($"slot:{slot}", false));
            }
        }

        foreach (var r in scenario["requests"]?.AsArray() ?? [])
        {
            refs.Add(new($"request:{r!["kind"]}", false));
            if (r["amount"] is not null)
            {
                refs.Add(new($"request-amount:{r["kind"]}", false));
            }
        }

        foreach (var v in scenario["extraValues"]?.AsArray() ?? [])
        {
            refs.Add(new($"value:{v!["what"]}", (bool?)v["optional"] ?? false));
        }

        return refs;
    }

    private static string Elements(SamplerData data, IEnumerable<string> claims)
    {
        var text = new StringBuilder();
        foreach (var claim in claims)
        {
            text.Append(claim).Append('\n');
            foreach (var e in data.ClaimById[claim].Elements)
            {
                text.Append("  ").Append(e.Kind).Append(": ").Append(e.Label).Append('\n');
            }
        }

        return text.Length == 0 ? "(none: every scenario of this batch is outside the catalogue)" : text.ToString().TrimEnd('\n');
    }

    public static string RequestList(SamplerData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var c in NotesJson.Load(data.Files.ClaimElements)["claims"]!.AsArray())
        {
            foreach (var r in c!["requests"]!.AsArray())
            {
                labels.TryAdd((string)r!["kind"]!, (string)r["label"]!);
            }
        }

        return string.Join("\n", RequestKinds.Select(k => labels.TryGetValue(k.Kind, out var ar)
            ? $"{k.Kind}: {k.Gloss} (for example «{ar}»)"
            : $"{k.Kind}: {k.Gloss}"));
    }
}
