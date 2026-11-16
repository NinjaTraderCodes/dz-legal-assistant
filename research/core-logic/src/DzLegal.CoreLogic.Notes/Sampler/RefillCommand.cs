using System.Globalization;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Sampler;

/// <summary>
/// <c>notes refill --set &lt;set&gt; --defect role-sex [--dry-run]</c>: the owner's instruction of 16 November 2026 on the
/// sampler's role defect. The defective scenarios are found by script from the scenario records alone (a claim's role
/// that gives the plaintiff a sex other than the one the cast records, or two claims that give two sexes: no model reads
/// a note). Each one's note leaves the set's kept notes for <c>dropped\&lt;set&gt;.jsonl</c> (reason
/// <c>samplerDefect:role-sex</c>), and its queue line is marked dropped; the scenario is drawn again by the current
/// sampler from the set's own seed and request, and the draw with the same id is appended as a new scenario
/// (<c>s-&lt;set&gt;-r&lt;n&gt;</c>, the same family and writer), queued for writing. The set's original scenarios are never
/// changed; the refill is recorded in <c>sets\&lt;set&gt;.refill-&lt;n&gt;.json</c>.
/// </summary>
public static class RefillCommand
{
    /// <summary>The scenario's roles against the plaintiff's sex: true when they disagree, or give two sexes.</summary>
    public static bool RoleSexDefect(JsonObject scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var sexes = (scenario["roles"]?.AsObject() ?? []).Select(kv => ScenarioSampler.SexOf(kv.Key, (string)kv.Value!["position"]!))
            .Where(x => x is not null).Distinct(StringComparer.Ordinal).ToList();
        var gender = (string?)scenario["cast"]?["plaintiff"]?["gender"];
        return sexes.Count > 1 || (sexes.Count == 1 && gender is not null && sexes[0] != gender);
    }

    public static int Run(NotesFiles files, string set, IReadOnlyDictionary<string, string> options, ISet<string> flags)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(flags);
        if (options.GetValueOrDefault("--defect") != "role-sex")
        {
            throw new ArgumentException("--defect role-sex (the one defect this command knows)");
        }

        var work = Filtering.FilterCommand.Work(options);
        var scenarios = WriteCommand.Scenarios(work, set);
        var defective = scenarios.Values.Where(RoleSexDefect).Select(s => (string)s["id"]!).Order(StringComparer.Ordinal).ToList();
        var manifest = NotesJson.Load(Path.Combine(work, "sets", set + ".sample.json")).AsObject();
        var request = new SampleRequest(set, (int)manifest["count"]!, (int)manifest["seed"]!, (int)manifest["groups"]!);
        var redrawn = new ScenarioSampler(SamplerData.Load(files), SampleCommand.FinalRecords(files)).Draw(request)
            .ToDictionary(s => (string)s["id"]!, StringComparer.Ordinal);
        var queue = QueueFile.Load(work);
        var refills = Directory.Exists(Path.Combine(work, "sets")) ? Directory.GetFiles(Path.Combine(work, "sets"), $"{set}.refill-*.json").Length : 0;
        var next = scenarios.Keys.Count(k => k.StartsWith($"s-{set}-r", StringComparison.Ordinal));
        var report = new JsonArray();
        var newScenarios = new List<JsonObject>();
        var newLines = new List<JsonObject>();
        var droppedNotes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in defective)
        {
            var old = scenarios[id];
            var line = queue.Lines.First(l => (string?)l["scenario"] == id);
            if (line["refilledBy"] is not null)
            {
                continue;  // already refilled
            }

            var fresh = redrawn[id];
            var sameClaims = old["claims"]!.ToJsonString() == fresh["claims"]!.ToJsonString();
            if (!sameClaims || RoleSexDefect(fresh))
            {
                throw new InvalidDataException($"{id}: the current sampler does not draw the same claim types without the defect");
            }

            var newId = $"s-{set}-r{++next:00000}";
            fresh["id"] = newId;
            fresh["family"] = old["family"]!.DeepClone();
            fresh["refillOf"] = id;
            newScenarios.Add(fresh);
            newLines.Add(new JsonObject
            {
                ["scenario"] = newId, ["set"] = set, ["family"] = (string)old["family"]!, ["writer"] = (string)line["writer"]!,
                ["state"] = "drawn", ["attempts"] = 0, ["refillOf"] = id,
            });
            var note = (string?)line["note"];
            report.Add(new JsonObject
            {
                ["scenario"] = id, ["note"] = note, ["stateBefore"] = (string)line["state"]!, ["refill"] = newId,
                ["rolesBefore"] = old["roles"]!.DeepClone(), ["rolesNow"] = fresh["roles"]!.DeepClone(),
                ["plaintiffGender"] = (string?)old["cast"]?["plaintiff"]?["gender"],
            });
            if (!flags.Contains("--dry-run"))
            {
                if (note is not null && (string)line["state"]! is "kept" or "keptWithBlanks" or "written")
                {
                    droppedNotes.Add(note);
                }

                line["state"] = "dropped";
                line["dropReason"] = "samplerDefect:role-sex";
                line["refilledBy"] = newId;
            }
        }

        Console.WriteLine($"set {set}: {defective.Count} scenarios with the role-sex defect; {report.Count} to refill");
        foreach (var r in report)
        {
            Console.WriteLine($"  {r!["scenario"]} (note {r["note"] ?? "none"}, {r["stateBefore"]}) -> {r["refill"]}: roles {r["rolesBefore"]!.ToJsonString()} -> {r["rolesNow"]!.ToJsonString()}");
        }

        if (flags.Contains("--dry-run") || report.Count == 0)
        {
            return 0;
        }

        MoveOut(Path.Combine(work, "notes", set + ".jsonl"), Path.Combine(work, "dropped", set + ".jsonl"), droppedNotes);
        File.AppendAllText(Path.Combine(work, "scenarios", set + ".jsonl"), string.Concat(newScenarios.Select(s => s.ToJsonString(NotesJson.Compact) + "\n")));
        queue.Lines.AddRange(newLines);
        queue.Save();
        NotesJson.Write(Path.Combine(work, "sets", $"{set}.refill-{refills + 1}.json"), new JsonObject
        {
            ["set"] = set, ["defect"] = "role-sex", ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["why"] = "owner, 16 November 2026: drop the notes hit by sampler 1's role defect, found by script; write their scenarios again with the fixed sampler",
            ["sampler"] = ScenarioSampler.Version, ["request"] = new JsonObject { ["seed"] = request.Seed, ["count"] = request.Count, ["groups"] = request.Groups },
            ["scenarios"] = report,
        }.ToJsonString(NotesJson.Options) + "\n");
        Console.WriteLine($"{droppedNotes.Count} notes moved out of the kept notes; {newScenarios.Count} scenarios queued for writing");
        return 0;
    }

    /// <summary>Moves the notes with these ids from a kept-notes file to the dropped file, marked with the defect.</summary>
    private static void MoveOut(string kept, string dropped, HashSet<string> ids)
    {
        if (ids.Count == 0 || !File.Exists(kept))
        {
            return;
        }

        var keep = new List<string>();
        var move = new List<string>();
        foreach (var line in File.ReadLines(kept).Where(l => l.Trim().Length > 0))
        {
            var note = JsonNode.Parse(line)!.AsObject();
            if (ids.Contains((string)note["id"]!))
            {
                note["status"] = "dropped";
                note["dropReason"] = "samplerDefect:role-sex";
                move.Add(note.ToJsonString(NotesJson.Compact));
            }
            else
            {
                keep.Add(line);
            }
        }

        File.WriteAllText(kept, string.Concat(keep.Select(l => l + "\n")));
        Directory.CreateDirectory(Path.GetDirectoryName(dropped)!);
        File.AppendAllText(dropped, string.Concat(move.Select(l => l + "\n")));
    }
}
