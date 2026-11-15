using System.Globalization;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;

namespace DzLegal.CoreLogic.Notes.Writing;

/// <summary>
/// <c>notes ingest --call &lt;id&gt; [--reply &lt;file&gt;] [--rejected]</c> (specs/notes-pipeline.md, section 3): the call's
/// answer through <c>writer-labels</c>, <c>build-spans</c> and the code checks. A note that passes is <c>written</c> (in
/// <c>&lt;work&gt;\written\&lt;set&gt;.jsonl</c>, waiting for its checker; it belongs to no set yet); one that fails is invalid
/// (kept with its reasons in <c>&lt;work&gt;\invalid\&lt;set&gt;.jsonl</c>) and its scenario goes back to the queue once. With
/// <c>--rejected</c> (the pool rejected the call) the answer is never read: its scenarios go back to the queue once. A
/// scenario has one retry in all; on a second failure it is dropped (a failed note) or set aside (a rejected call).
/// </summary>
public static class IngestCommand
{
    public const string SchemeVersion = "1.0@ff4e46b";
    public const string CodexModel = "codex-cli 0.167.0 (default model)";

    public static int Run(NotesFiles files, string call, IReadOnlyDictionary<string, string> options, ISet<string> flags)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(flags);
        var work = options.GetValueOrDefault("--work") ?? @"C:\Users\TheShadow\dz-core-research\notes-work";
        NotesLoader.Guard(work);
        var record = NotesJson.Load(Path.Combine(work, "calls", call + ".json")).AsObject();
        var set = (string)record["set"]!;
        var writer = (string)record["writer"]!;
        var ids = record["scenarios"]!.AsArray().Select(x => (string)x!).ToList();
        var queue = QueueFile.Load(work);
        var lines = ids.Select(queue.Line).ToList();
        if (lines.Any(l => (string?)l["writeCall"] != call))
        {
            throw new InvalidOperationException($"{call}: its scenarios are no longer waiting for it (ingested already?)");
        }

        if (flags.Contains("--rejected"))
        {
            foreach (var line in lines)
            {
                Retry(line, "callRejected", setAside: true);
            }

            queue.Save();
            Console.WriteLine($"{call} rejected: {lines.Count(l => (string?)l["state"] == "drawn")} scenarios back in the queue, " +
                              $"{lines.Count(l => (string?)l["state"] == "setAside")} set aside (their second call)");
            return 0;
        }

        var answerPath = Path.Combine(work, "answers", call + ".json");
        if (options.TryGetValue("--reply", out var reply))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(answerPath)!);
            File.Copy(reply, answerPath, overwrite: false);
        }

        var answer = NotesJson.Load(answerPath);
        var notes = (answer["notes"]?.AsArray() ?? []).Select(n => n!.AsObject()).ToDictionary(n => (int)n["n"]!);
        var data = SamplerData.Load(files);
        var cards = data.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var scenarios = WriteCommand.Scenarios(work, set);
        var checks = new CodeChecks(data, [File.ReadAllText(files.Guide), File.ReadAllText(Path.Combine(files.Prompts, "notes-write.md")),
            .. data.Claims.SelectMany(c => c.Elements.Select(e => e.Label))]);
        var manifest = Path.Combine(work, "sets", set + ".sample.json");
        var sampled = File.Exists(manifest) ? NotesJson.Load(manifest) : null;
        var written = new List<string>();
        var invalid = new List<string>();
        var reasons = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < ids.Count; i++)
        {
            var scenario = scenarios[ids[i]];
            var line = lines[i];
            var noteId = "n-" + ids[i][2..];
            if (!notes.TryGetValue(i + 1, out var note))
            {
                Count(reasons, "noNote");
                invalid.Add(Invalid(noteId, set, scenario, null, ["no note for this scenario in the answer"]));
                Retry(line, "noNote", setAside: false);
                continue;
            }

            var pointed = WriterLabels.Point(scenario, note);
            var problems = checks.Run(scenario, note, pointed, cards[(string)scenario["style"]!]);
            if (problems.Count > 0)
            {
                foreach (var kind in problems.Select(p => p[..p.IndexOf(':', StringComparison.Ordinal)]).Distinct(StringComparer.Ordinal))
                {
                    Count(reasons, kind);
                }

                invalid.Add(Invalid(noteId, set, scenario, note, problems));
                Retry(line, "codeCheck", setAside: false);
                continue;
            }

            var text = new JsonObject();
            foreach (var field in new[] { "subject", "facts", "requests", "documents" })
            {
                text[field] = (string?)note[field] ?? "";
            }

            var provenance = new JsonObject
            {
                ["writer"] = new JsonObject
                {
                    ["family"] = writer == "codex" ? "openai" : "anthropic",
                    ["model"] = writer == "codex" ? CodexModel : "claude-sonnet-5-5",
                    ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["call"] = call,
                },
                ["scenario"] = ids[i],
                ["scenarioFamily"] = (string)scenario["family"]!,
                ["styleCard"] = (string)scenario["style"]!,
                ["trainingAllowed"] = writer == "codex" && set is "train" or "pilot",
                ["composedByCode"] = false,
                ["guideVersion"] = "1.2",
                ["writerLabels"] = WriterLabels.Labels(data, scenario, pointed.Quotes),
                ["schemeVersion"] = SchemeVersion,
            };
            if ((string?)scenario["group"] is string group)
            {
                provenance["polarityFamily"] = group;
            }

            if (sampled is not null)
            {
                provenance["samplerCommit"] = (string?)sampled["sampler"]?["commit"];
                provenance["seed"] = ((int?)sampled["seed"])?.ToString(CultureInfo.InvariantCulture);
            }

            if ((string?)scenario["difficulty"] is string difficulty)
            {
                provenance["difficulty"] = difficulty;
            }

            written.Add(new JsonObject { ["id"] = noteId, ["set"] = set, ["text"] = text, ["provenance"] = provenance }.ToJsonString(NotesJson.Compact));
            line["state"] = "written";
            line["note"] = noteId;
        }

        Append(Path.Combine(work, "written", set + ".jsonl"), written);
        Append(Path.Combine(work, "invalid", set + ".jsonl"), invalid);
        queue.Save();
        var rate = ids.Count == 0 ? 0 : 100.0 * invalid.Count / ids.Count;
        Console.WriteLine($"{call}: {written.Count} written, {invalid.Count} invalid ({rate:0}%)" +
                          (reasons.Count == 0 ? "" : "; " + string.Join(", ", reasons.Select(kv => $"{kv.Key} {kv.Value}"))));
        if (rate > 30)
        {
            Console.WriteLine("OVER 30 % invalid: read ten failures (notes-pipeline.md, decision table)");
        }

        return 0;
    }

    /// <summary>A scenario's one retry: back to the queue as drawn, or, on its second failure, dropped or set aside.</summary>
    private static void Retry(JsonObject line, string reason, bool setAside)
    {
        line.Remove("writeCall");
        if (((int?)line["attempts"] ?? 1) >= 2)
        {
            line["state"] = setAside ? "setAside" : "dropped";
            line["reason"] = reason;
        }
        else
        {
            line["state"] = "drawn";
            line["lastFailure"] = reason;
        }
    }

    private static string Invalid(string id, string set, JsonObject scenario, JsonObject? note, IReadOnlyList<string> problems) =>
        new JsonObject
        {
            ["id"] = id, ["set"] = set, ["scenario"] = (string)scenario["id"]!, ["note"] = note?.DeepClone(),
            ["reasons"] = new JsonArray([.. problems.Select(p => (JsonNode)p)]),
        }.ToJsonString(NotesJson.Compact);

    private static void Count(Dictionary<string, int> reasons, string kind) => reasons[kind] = reasons.GetValueOrDefault(kind) + 1;

    private static void Append(string path, List<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, string.Concat(lines.Select(l => l + "\n")));
    }
}
