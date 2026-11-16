using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;

namespace DzLegal.CoreLogic.Notes.Judging;

/// <summary>
/// <c>notes noise --from &lt;set&gt; --sample 100 --seed &lt;n&gt; --prepare | --run &lt;id&gt; --ingest &lt;reply&gt; | --compare</c>
/// (specs/notes-pipeline.md, "The noise measurement"; prompts/notes-noise-audit.md): Opus relabels a random sample of
/// kept training notes blind, 25 a run, and the fields where its labels and the kept labels differ are listed for the
/// sorting run. It measures and changes nothing: no note, no queue line, no set is touched, and the answers stay in
/// <c>&lt;work&gt;\noise\&lt;set&gt;-&lt;seed&gt;\</c>, never in a note.
/// </summary>
public static class NoiseCommand
{
    public static string Folder(string work, string set, int seed) => Path.Combine(work, "noise", $"{set}-{seed.ToString(CultureInfo.InvariantCulture)}");

    /// <summary>The kept notes of a training set, in a fixed random order for the seed.</summary>
    public static List<JsonObject> Sample(string work, string set, int seed, int count)
    {
        var path = Path.Combine(work, "notes", set + ".jsonl");
        var kept = File.ReadLines(path).Where(l => l.Trim().Length > 0).Select(l => JsonNode.Parse(l)!.AsObject())
            .Where(n => (string?)n["status"] is "kept" or "keptWithBlanks").OrderBy(n => (string)n["id"]!, StringComparer.Ordinal).ToList();
        var rng = new Random(seed);
        return [.. kept.OrderBy(_ => rng.Next()).Take(count)];
    }

    public static int Run(NotesFiles files, IReadOnlyDictionary<string, string> options, ISet<string> flags)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(flags);
        var work = Filtering.FilterCommand.Work(options);
        var set = options.GetValueOrDefault("--from") ?? throw new ArgumentException("--from <set>");
        if (set is not ("pilot" or "train"))
        {
            throw new ArgumentException("the noise measurement reads training notes only (pilot, train)");
        }

        var seed = int.Parse(options.GetValueOrDefault("--seed") ?? throw new ArgumentException("--seed <n>"), CultureInfo.InvariantCulture);
        var folder = Folder(work, set, seed);
        var data = SamplerData.Load(files);
        if (flags.Contains("--prepare"))
        {
            if (Directory.Exists(folder))
            {
                throw new InvalidOperationException($"{folder} exists: a measurement is never redrawn");
            }

            var count = int.Parse(options.GetValueOrDefault("--sample") ?? "100", CultureInfo.InvariantCulture);
            var notes = Sample(work, set, seed, count);
            Directory.CreateDirectory(folder);
            for (var r = 0; r * RelabelCommand.PerRun < notes.Count; r++)
            {
                var part = notes.Skip(r * RelabelCommand.PerRun).Take(RelabelCommand.PerRun).ToList();
                var id = $"noise-{set}-{seed.ToString(CultureInfo.InvariantCulture)}-{r + 1}";
                NotesJson.Write(Path.Combine(folder, id + ".prompt.md"), RelabelCommand.Prompt(files, data, part, "notes-noise-audit"));
                NotesJson.Write(Path.Combine(folder, id + ".json"), new JsonObject
                {
                    ["run"] = id, ["set"] = set, ["seed"] = seed, ["relabeller"] = "opus",
                    ["notes"] = new JsonArray([.. part.Select(n => (JsonNode)(string)n["id"]!)]),
                }.ToJsonString(NotesJson.Options) + "\n");
                Console.WriteLine($"{id}: {part.Count} notes");
            }

            return 0;
        }

        if (options.TryGetValue("--run", out var run))
        {
            var reply = options.GetValueOrDefault("--ingest") ?? throw new ArgumentException("--ingest <reply>");
            var meta = NotesJson.Load(Path.Combine(folder, run + ".json")).AsObject();
            var kept = Kept(work, set);
            var ids = meta["notes"]!.AsArray().Select(n => (string)n!).ToList();
            var answer = NotesJson.Load(reply)["notes"]!.AsArray().Select(n => n!.AsObject()).ToList();
            var lines = new StringBuilder();
            var missing = 0;
            var bad = 0;
            for (var i = 0; i < ids.Count; i++)
            {
                var entry = answer.FirstOrDefault(a => (int?)a["n"] == i + 1);
                if (entry is null)
                {
                    missing++;
                    continue;
                }

                var labels = entry.DeepClone().AsObject();
                labels.Remove("n");
                var remarks = (string?)labels["remarks"] ?? "";
                labels.Remove("remarks");
                RelabelCommand.Normalise(labels);
                bad += RelabelCommand.Locate((JsonObject)kept[ids[i]]["text"]!, labels);
                lines.Append(new JsonObject { ["note"] = ids[i], ["run"] = run, ["labels"] = labels, ["remarks"] = remarks }.ToJsonString(NotesJson.Compact)).Append('\n');
            }

            File.AppendAllText(Path.Combine(folder, "labels.jsonl"), lines.ToString());
            Console.WriteLine($"{run}: {ids.Count - missing} relabelled, {missing} left out, {bad} quotes not found");
            return 0;
        }

        if (flags.Contains("--compare"))
        {
            var kept = Kept(work, set);
            var output = new JsonArray();
            int compared = 0, differing = 0;
            foreach (var line in File.ReadLines(Path.Combine(folder, "labels.jsonl")).Where(l => l.Trim().Length > 0))
            {
                var r = JsonNode.Parse(line)!.AsObject();
                var note = kept[(string)r["note"]!];
                var gold = note["gold"]!.AsObject();
                var labels = r["labels"]!.AsObject();
                var blanked = new HashSet<string>((note["blanked"]?.AsArray() ?? []).Select(b => (string)b!), StringComparer.Ordinal);
                var c = CompareCommand.Compared(data, gold, labels).Where(f => !blanked.Contains(f)).ToList();
                var d = CompareCommand.Differences(data, (JsonObject)note["text"]!, gold, labels).Where(f => !blanked.Contains(f)).ToList();
                compared += c.Count;
                differing += d.Count;
                output.Add(new JsonObject
                {
                    ["note"] = (string)r["note"]!,
                    ["compared"] = new JsonArray([.. c.Select(x => (JsonNode)x)]),
                    ["differing"] = new JsonArray([.. d.Select(x => (JsonNode)x)]),
                });
            }

            NotesJson.Write(Path.Combine(folder, "differences.json"), output.ToJsonString(NotesJson.Options) + "\n");
            Console.WriteLine($"{output.Count} notes; {compared} compared fields; {differing} differing (evidence-only differences included) -> {Path.Combine(folder, "differences.json")}");
            return 0;
        }

        throw new ArgumentException("notes noise --from <set> --seed <n> --prepare | --run <id> --ingest <reply> | --compare");
    }

    private static Dictionary<string, JsonObject> Kept(string work, string set) =>
        File.ReadLines(Path.Combine(work, "notes", set + ".jsonl")).Where(l => l.Trim().Length > 0).Select(l => JsonNode.Parse(l)!.AsObject())
            .ToDictionary(n => (string)n["id"]!, StringComparer.Ordinal);
}
