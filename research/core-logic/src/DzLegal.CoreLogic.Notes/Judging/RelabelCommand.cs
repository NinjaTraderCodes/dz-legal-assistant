using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Judging;

/// <summary>
/// <c>notes relabel --set &lt;set&gt; --prepare</c> and <c>notes relabel --run &lt;id&gt; --ingest &lt;reply&gt;</c>
/// (specs/notes-pipeline.md, section 4b): the blind relabel of test notes. A note written by Codex or a local model is
/// relabelled by Sonnet 5.5, one written by Sonnet by Opus 5.5; the relabeller sees the notes' text and the guide only
/// (prompts/notes-judge.md), 25 notes a run, and two members of one polarity group never share a run. The answer's quotes
/// are found in the note by code; a quote that is not found is kept with no position, and the comparison counts it as a
/// disagreement on its field.
/// </summary>
public static class RelabelCommand
{
    public const int PerRun = 25;

    // The owner, 19 November 2026: "Codex relabels the Sonnet-written dev and challenge notes; Sonnet relabels the
    // Codex-written ones. No Opus relabels." Every test note is then relabelled by the other family.
    public static string RelabellerOf(string writer) => writer switch
    {
        "codex" or "local" => "sonnet",
        "sonnet" => "codex",
        _ => throw new InvalidDataException($"no relabeller for writer {writer}: Opus writes no note"),
    };

    /// <summary>The model family of a writer or relabeller: codex is OpenAI's, sonnet and opus Anthropic's.</summary>
    public static string FamilyOf(string model) => model switch
    {
        "codex" => "openai",
        "sonnet" or "opus" => "anthropic",
        "local" => "local",
        _ => throw new InvalidDataException($"unknown model {model}"),
    };

    /// <summary>The proof a relabel gives: two families when writer and relabeller differ in family, else one.</summary>
    public static string ProofOf(string writer, string relabeller) => FamilyOf(writer) == FamilyOf(relabeller) ? "oneFamily" : "twoFamilies";

    public static int Prepare(NotesFiles files, string set, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        if (set is "pilot" or "train" or "composed" || set.StartsWith("clean-train", StringComparison.Ordinal))
        {
            throw new ArgumentException($"set {set} is a training set: its notes are never relabelled by a Claude model (TERMS.md)");
        }

        var work = Filtering.FilterCommand.Work(options);
        var data = SamplerData.Load(files);
        var written = Filtering.FilterCommand.Written(work, set).ToDictionary(n => (string)n["id"]!, StringComparer.Ordinal);
        var scenarios = WriteCommand.Scenarios(work, set);
        var queue = QueueFile.Load(work);
        var folder = Path.Combine(work, "relabel");
        Directory.CreateDirectory(folder);
        var number = Directory.GetFiles(folder, $"r-{set}-*.json").Length;
        var made = new List<string>();
        var todo = queue.Lines.Where(l => (string?)l["set"] == set && (string?)l["state"] == "written" && l["relabelRun"] is null).ToList();
        foreach (var byWriter in todo.GroupBy(l => (string)l["writer"]!, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var relabeller = RelabellerOf(byWriter.Key);
            foreach (var run in Runs([.. byWriter], scenarios))
            {
                var id = $"r-{set}-{++number:0000}";
                var notes = run.Select(l => written[(string)l["note"]!]).ToList();
                NotesJson.Write(Path.Combine(folder, id + ".prompt.md"), Prompt(files, data, notes));
                NotesJson.Write(Path.Combine(folder, id + ".json"), new JsonObject
                {
                    ["run"] = id, ["set"] = set, ["writer"] = byWriter.Key, ["relabeller"] = relabeller,
                    ["prompt"] = "notes-judge.md@" + Hash(files),
                    ["created"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    ["notes"] = new JsonArray([.. notes.Select(n => (JsonNode)(string)n["id"]!)]),
                }.ToJsonString(NotesJson.Options) + "\n");
                foreach (var line in run)
                {
                    line["relabelRun"] = id;
                }

                made.Add($"{id}: {run.Count} notes written by {byWriter.Key} -> {relabeller}");
            }
        }

        queue.Save();
        Console.WriteLine(made.Count == 0 ? "no run: no written note of this set waits for a relabel" : string.Join("\n", made));
        return 0;
    }

    /// <summary>Runs of at most 25 notes; the members of a polarity group are dealt to different runs.</summary>
    public static List<List<JsonObject>> Runs(List<JsonObject> lines, IReadOnlyDictionary<string, JsonObject> scenarios)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(scenarios);
        var count = Math.Max(1, (lines.Count + PerRun - 1) / PerRun);
        var largestGroup = lines.GroupBy(l => (string?)scenarios[(string)l["scenario"]!]["group"] ?? (string)l["scenario"]!)
            .Max(g => g.Count());
        count = Math.Max(count, largestGroup);
        var runs = Enumerable.Range(0, count).Select(_ => new List<JsonObject>()).ToList();
        var next = 0;
        foreach (var g in lines.GroupBy(l => (string?)scenarios[(string)l["scenario"]!]["group"] ?? (string)l["scenario"]!))
        {
            foreach (var member in g)
            {
                runs[next % count].Add(member);
                next++;
            }
        }

        return [.. runs.Where(r => r.Count > 0)];
    }

    public static string Hash(NotesFiles files) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(files.Prompts, "notes-judge.md"))))[..12]
            .ToLowerInvariant();

    public static string Prompt(NotesFiles files, SamplerData data, IReadOnlyList<JsonObject> notes, string template = "notes-judge")
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(notes);
        var text = new StringBuilder();
        for (var i = 0; i < notes.Count; i++)
        {
            string Field(string name) => (string?)notes[i]["text"]?[name] is { Length: > 0 } s ? s : "(فارغ)";
            text.Append(CultureInfo.InvariantCulture, $"### Note {i + 1}\n\n- subject: {Field("subject")}\n- facts: {Field("facts")}\n")
                .Append(CultureInfo.InvariantCulture, $"- requests: {Field("requests")}\n- documents: {Field("documents")}\n\n");
        }

        return PromptTemplate.Fill(PromptTemplate.Body(files.Prompts, template), new Dictionary<string, string>
        {
            ["guide"] = File.ReadAllText(files.Guide).ReplaceLineEndings("\n").Trim(),
            ["elements"] = Elements(files, data),
            ["requestKinds"] = WriteCommand.RequestList(data),
            ["slots"] = string.Join("\n", data.Slots.Select(s => s.Choices.Count > 0
                ? $"{s.Id} ({s.Type}): {string.Join(", ", s.Choices)}"
                : $"{s.Id} ({s.Type})")),
            ["notes"] = text.ToString().TrimEnd('\n'),
        });
    }

    /// <summary>All 21 claim types: the roles offered (relation, position, Arabic label) and the elements' kinds and propositions.</summary>
    private static string Elements(NotesFiles files, SamplerData data)
    {
        var text = new StringBuilder();
        foreach (var c in NotesJson.Load(files.ClaimElements)["claims"]!.AsArray())
        {
            var id = (string)c!["claim"]!;
            text.Append(id).Append('\n');
            var roles = c["roles"]?.AsArray() ?? [];
            text.Append("  roles: ").Append(string.Join("; ", roles.Select(r => $"{r!["relation"]}/{r["position"]} «{r["label"]}»")))
                .Append(roles.Count > 1 ? " (more than one: say which)" : "").Append('\n');
            foreach (var e in data.ClaimById[id].Elements)
            {
                text.Append("  ").Append(e.Kind).Append(": ").Append(e.Label).Append('\n');
            }
        }

        return text.ToString().TrimEnd('\n');
    }

    public static int Ingest(NotesFiles files, string run, string reply, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        var work = Filtering.FilterCommand.Work(options);
        var meta = NotesJson.Load(Path.Combine(work, "relabel", run + ".json")).AsObject();
        var set = (string)meta["set"]!;
        var ids = meta["notes"]!.AsArray().Select(n => (string)n!).ToList();
        var written = Filtering.FilterCommand.Written(work, set).ToDictionary(n => (string)n["id"]!, StringComparer.Ordinal);
        var answer = NotesJson.Load(reply)["notes"]!.AsArray().Select(n => n!.AsObject()).ToList();
        var queue = QueueFile.Load(work);
        var lines = new StringBuilder();
        var back = new List<string>();
        var badQuotes = 0;
        for (var i = 0; i < ids.Count; i++)
        {
            var entry = answer.FirstOrDefault(a => (int?)a["n"] == i + 1);
            if (entry is null)
            {
                back.Add(ids[i]);
                continue;
            }

            var labels = entry.DeepClone().AsObject();
            labels.Remove("n");
            var remarks = (string?)labels["remarks"] ?? "";
            labels.Remove("remarks");
            Normalise(labels);
            badQuotes += Locate((JsonObject)written[ids[i]]["text"]!, labels);
            lines.Append(new JsonObject
            {
                ["note"] = ids[i], ["set"] = set, ["run"] = run, ["writer"] = (string)meta["writer"]!,
                ["relabeller"] = (string)meta["relabeller"]!, ["labels"] = labels, ["remarks"] = remarks,
            }.ToJsonString(NotesJson.Compact)).Append('\n');
        }

        File.AppendAllText(Path.Combine(work, "relabel", set + ".jsonl"), lines.ToString());
        foreach (var id in back)
        {
            queue.Lines.First(l => (string?)l["note"] == id)["relabelRun"] = null;
        }

        queue.Save();
        Console.WriteLine($"{run}: {ids.Count - back.Count} relabelled, {back.Count} left out (back to the queue), {badQuotes} quotes not found");
        return 0;
    }

    /// <summary>
    /// A date or an amount given as a bare normal form ("2022-11-23") becomes a value with that normal form and no quote
    /// (the comparison reads only the normal form of an event's or a request's value). The schema accepts the bare form
    /// since 16 November 2026: a Sonnet relabeller wrote it so, and the prompt's other omissions it made were allowed.
    /// </summary>
    public static void Normalise(JsonObject labels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        foreach (var part in new[] { "events", "requests" })
        {
            foreach (var item in labels[part]?.AsArray() ?? [])
            {
                foreach (var (key, kind) in new[] { ("date", "date"), ("amount", "amount") })
                {
                    if (item![key] is JsonValue bare && bare.TryGetValue<string>(out var normal))
                    {
                        item[key] = new JsonObject { ["kind"] = kind, ["normalized"] = normal };
                    }
                }
            }
        }
    }

    /// <summary>Adds start and length to every quote of the labels; returns how many were not found (left without them).</summary>
    public static int Locate(JsonObject text, JsonObject labels)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(labels);
        var missing = 0;
        void One(JsonNode? quote)
        {
            if (quote is not JsonObject q)
            {
                return;
            }

            var found = WriterLabels.Span(text, (string?)q["field"] ?? "", (string?)q["quote"] ?? "", (int?)q["occurrence"] ?? 1);
            if (found is null)
            {
                missing++;
                q["notFound"] = true;
                return;
            }

            q["start"] = (int)found["start"]!;
            q["length"] = (int)found["length"]!;
        }

        void Many(JsonNode? list)
        {
            foreach (var q in list?.AsArray() ?? [])
            {
                One(q);
            }
        }

        Many(labels["claimEvidence"]);
        foreach (var part in new[] { "roles", "events", "slots", "requests" })
        {
            foreach (var item in labels[part]?.AsArray() ?? [])
            {
                Many(item!["evidence"]);
                One(item["date"]?["at"]);
                One(item["amount"]?["at"]);
            }
        }

        foreach (var v in labels["values"]?.AsArray() ?? [])
        {
            One(v!["at"]);
        }

        return missing;
    }
}
