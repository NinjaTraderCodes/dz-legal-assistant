using System.Globalization;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Judging;

/// <summary>
/// <c>notes compare --set &lt;set&gt;</c> (specs/notes-pipeline.md, sections 4b and 5): the writer's labels (the
/// scenario's) against the blind relabel, field by field. A test note is kept only when every field agrees; any
/// disagreement, any <c>unclear</c> and any quote not found in the note drops it. Kept notes go to
/// <c>&lt;work&gt;\notes\&lt;set&gt;.jsonl</c> with the writer's labels as gold; dropped ones to
/// <c>&lt;work&gt;\dropped\&lt;set&gt;.jsonl</c> with the fields that differ.
/// </summary>
public static class CompareCommand
{
    public static int Run(NotesFiles files, string set, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        var work = Filtering.FilterCommand.Work(options);
        var data = SamplerData.Load(files);
        var written = Filtering.FilterCommand.Written(work, set).ToDictionary(n => (string)n["id"]!, StringComparer.Ordinal);
        var path = Path.Combine(work, "relabel", set + ".jsonl");
        var relabels = File.ReadLines(path).Where(l => l.Trim().Length > 0).Select(l => JsonNode.Parse(l)!.AsObject())
            .GroupBy(r => (string)r["note"]!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);
        var queue = QueueFile.Load(work);
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var kinds = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in queue.Lines.Where(l => (string?)l["set"] == set && (string?)l["state"] == "written"))
        {
            var id = (string)line["note"]!;
            if (!relabels.TryGetValue(id, out var relabel))
            {
                continue;
            }

            var note = written[id];
            var writerLabels = note["provenance"]!["writerLabels"]!.AsObject();
            var differences = Differences(data, (JsonObject)note["text"]!, writerLabels, relabel["labels"]!.AsObject());
            var status = differences.Count == 0 ? "kept" : "dropped";
            var provenance = note["provenance"]!.DeepClone().AsObject();
            provenance["relabel"] = new JsonObject
            {
                ["family"] = "anthropic", ["model"] = (string)relabel["relabeller"]!, ["run"] = (string)relabel["run"]!,
                ["proof"] = (string)relabel["writer"]! == "sonnet" ? "oneFamily" : "twoFamilies",
            };
            var output = new JsonObject
            {
                ["id"] = id, ["set"] = set, ["text"] = note["text"]!.DeepClone(), ["gold"] = writerLabels.DeepClone(),
                ["blanked"] = new JsonArray(), ["provenance"] = provenance, ["status"] = status,
            };
            if (status == "dropped")
            {
                output["dropReason"] = "relabelDisagreement";
                output["differences"] = new JsonArray([.. differences.Select(d => (JsonNode)d)]);
                foreach (var d in differences)
                {
                    var kind = d.Split('/')[0];
                    kinds[kind] = kinds.GetValueOrDefault(kind) + 1;
                }
            }

            Append(Path.Combine(work, status == "dropped" ? "dropped" : "notes", set + ".jsonl"), output.ToJsonString(NotesJson.Compact));
            line["state"] = status;
            line["checkRun"] = "relabel:" + (string)relabel["run"]!;
            var key = $"{relabel["writer"]} {status}";
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        queue.Save();
        Console.WriteLine($"set {set}: " + string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}")));
        Console.WriteLine("differing fields by kind: " + (kinds.Count == 0 ? "none" : string.Join(", ", kinds.Select(kv => $"{kv.Key} {kv.Value}"))));
        return 0;
    }

    /// <summary>The fields on which the two labellings differ (an empty list: the note is kept).</summary>
    public static List<string> Differences(SamplerData data, JsonObject text, JsonObject writer, JsonObject relabel)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(relabel);
        var diff = new List<string>();
        var claimsW = Strings(writer["claims"]);
        var claimsR = Strings(relabel["claims"]);
        if (!claimsW.SetEquals(claimsR) || claimsR.Contains("unclear") ||
            (claimsW.Contains("outside") && (string?)writer["outsideKind"] != (string?)relabel["outsideKind"]))
        {
            return ["claims"];
        }

        if (Quotes(relabel["claimEvidence"]).Any(q => q["notFound"] is not null))
        {
            diff.Add("claims/evidence");
        }

        // The plaintiff's role, per claim.
        foreach (var claim in claimsW.Where(c => c != "outside").Order(StringComparer.Ordinal))
        {
            var w = Find(writer["roles"], "claim", claim);
            var r = Find(relabel["roles"], "claim", claim);
            var pw = w is null ? "unknown/unknown" : $"{w["relation"]}/{w["position"]}";
            var pr = r is null ? "unknown/unknown" : $"{r["relation"]}/{r["position"]}";
            if (pw != pr || (pr != "unknown/unknown" && !SameEvidence(text, w, r)))
            {
                diff.Add($"roles/{claim}");
            }
        }

        // Each element of each claim type.
        var eventsW = Items(writer["events"]).ToDictionary(e => $"{e["claim"]}/{e["kind"]}", StringComparer.Ordinal);
        var eventsR = Items(relabel["events"]).GroupBy(e => $"{e["claim"]}/{e["kind"]}", StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var key in eventsW.Keys.Union(eventsR.Keys).Order(StringComparer.Ordinal))
        {
            var w = eventsW.GetValueOrDefault(key);
            var r = eventsR.GetValueOrDefault(key);
            var polW = (string?)w?["polarity"] ?? "notMentioned";
            var polR = (string?)r?["polarity"] ?? "notMentioned";
            if (polW != polR)
            {
                diff.Add($"events/{key}/polarity");
                continue;
            }

            if (polW == "notMentioned")
            {
                continue;
            }

            if (!SameEvidence(text, w, r))
            {
                diff.Add($"events/{key}/evidence");
            }

            var actorW = (string?)w!["actor"] ?? "unknown";
            var actorR = (string?)r!["actor"] ?? "unknown";
            if (actorW != "unknown" && actorR != "unknown" && actorW != actorR)
            {
                diff.Add($"events/{key}/actor");
            }

            if (polW == "disputed" && w["disputedBy"] is not null && r["disputedBy"] is not null &&
                (string?)w["disputedBy"] != (string?)r["disputedBy"])
            {
                diff.Add($"events/{key}/disputedBy");
            }

            foreach (var part in new[] { "date", "amount" })
            {
                if (!SameValue(w[part], r[part]))
                {
                    diff.Add($"events/{key}/{part}");
                }
            }
        }

        // The slots asked for the note's claim types.
        var asked = data.Slots.Where(s => claimsW.Any(s.AskedForClaim)).Select(s => s.Id)
            .Union(Items(writer["slots"]).Select(s => (string)s["slot"]!), StringComparer.Ordinal).Order(StringComparer.Ordinal);
        foreach (var slot in asked)
        {
            var w = Find(writer["slots"], "slot", slot);
            var r = Find(relabel["slots"], "slot", slot);
            var vw = (string?)w?["value"] ?? "unknown";
            var vr = (string?)r?["value"] ?? "unknown";
            if (vw != vr || (vw != "unknown" && !SameEvidence(text, w, r)))
            {
                diff.Add($"slots/{slot}");
            }
        }

        // Requests: the same kinds, and the same amount for each.
        var reqW = Items(writer["requests"]).GroupBy(q => (string)q["kind"]!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var reqR = Items(relabel["requests"]).GroupBy(q => (string)q["kind"]!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var kind in reqW.Keys.Union(reqR.Keys).Order(StringComparer.Ordinal))
        {
            if (!reqW.TryGetValue(kind, out var w) || !reqR.TryGetValue(kind, out var r) || !SameEvidence(text, w, r))
            {
                diff.Add($"requests/{kind}");
            }
            else if (!SameValue(w["amount"], r["amount"]))
            {
                diff.Add($"requests/{kind}/amount");
            }
        }

        // Every written value, matched by overlapping quotes, in both directions.
        var valuesW = Items(writer["values"]).ToList();
        var valuesR = Items(relabel["values"]).ToList();
        foreach (var v in valuesW.Where(v => !Matched(v, valuesR)))
        {
            diff.Add($"values/{At(v)}");
        }

        foreach (var v in valuesR.Where(v => !Matched(v, valuesW)))
        {
            var at = $"values/{At(v)}";
            if (!diff.Contains(at))
            {
                diff.Add(at);
            }
        }

        return diff;
    }

    private static bool Matched(JsonObject v, List<JsonObject> others)
    {
        var at = v["at"]?.AsObject();
        if (at is null || at["start"] is null)
        {
            return false;
        }

        var near = others.Where(o => (string?)o["kind"] == (string?)v["kind"] && Overlap(at, o["at"]?.AsObject())).ToList();
        var roles = Strings(v["roles"]);
        return near.Any(o => (string?)o["normalized"] == (string?)v["normalized"] && (string?)v["normalized"] != "unclear" &&
                             ((bool?)o["amountDisputed"] ?? false) == ((bool?)v["amountDisputed"] ?? false) &&
                             (roles.Count == 0 && Strings(o["roles"]).Count == 0 || roles.Overlaps(Strings(o["roles"]))));
    }

    private static bool Overlap(JsonObject a, JsonObject? b) =>
        b?["start"] is not null && (string?)a["field"] == (string?)b["field"] &&
        (int)a["start"]! < (int)b["start"]! + (int)b["length"]! && (int)b["start"]! < (int)a["start"]! + (int)a["length"]!;

    private static string At(JsonObject v) => v["at"] is JsonObject at
        ? $"{at["field"]}:{(at["start"] is null ? "notFound" : ((int)at["start"]!).ToString(CultureInfo.InvariantCulture))}"
        : "none";

    /// <summary>Two dates or amounts: both absent, or the same normal form and the same <c>amountDisputed</c>.</summary>
    private static bool SameValue(JsonNode? a, JsonNode? b) =>
        a is null && b is null ||
        a is not null && b is not null && (string?)a["normalized"] == (string?)b["normalized"] && (string?)a["normalized"] != "unclear" &&
        ((bool?)a["amountDisputed"] ?? false) == ((bool?)b["amountDisputed"] ?? false) && b["at"]?["notFound"] is null;

    /// <summary>
    /// The guide's rule for quotes (section 9): each must be found in the note, and the two sides' quotes for one field
    /// must overlap or lie in the same sentence (some pair of them).
    /// </summary>
    public static bool SameEvidence(JsonObject text, JsonObject? writer, JsonObject? relabel)
    {
        ArgumentNullException.ThrowIfNull(text);
        var w = Quotes(writer?["evidence"]).ToList();
        var r = Quotes(relabel?["evidence"]).ToList();
        if (r.Any(q => q["notFound"] is not null || q["start"] is null))
        {
            return false;
        }

        if (w.Count == 0 || r.Count == 0)
        {
            return w.Count == r.Count;
        }

        return w.Any(a => r.Any(b => Overlap(a, b) || (string?)a["field"] == (string?)b["field"] &&
            Sentence((string?)text[(string)a["field"]!] ?? "", (int)a["start"]!) ==
            Sentence((string?)text[(string)a["field"]!] ?? "", (int)b["start"]!)));
    }

    /// <summary>The index of the sentence a position lies in: sentences end at «.» (not between digits), «؟», «?», «!», «؛» and a line break.</summary>
    public static int Sentence(string text, int at)
    {
        ArgumentNullException.ThrowIfNull(text);
        var n = 0;
        for (var i = 0; i < Math.Min(at, text.Length); i++)
        {
            var c = text[i];
            if (c is '؟' or '?' or '!' or '؛' or '\n' ||
                c == '.' && !(i > 0 && char.IsDigit(text[i - 1]) && i + 1 < text.Length && char.IsDigit(text[i + 1])))
            {
                n++;
            }
        }

        return n;
    }

    private static IEnumerable<JsonObject> Items(JsonNode? list) => list?.AsArray().Select(x => x!.AsObject()) ?? [];

    private static IEnumerable<JsonObject> Quotes(JsonNode? list) => Items(list);

    private static JsonObject? Find(JsonNode? list, string key, string value) =>
        Items(list).FirstOrDefault(x => (string?)x[key] == value);

    private static HashSet<string> Strings(JsonNode? list) =>
        new(list?.AsArray().Select(x => (string)x!) ?? [], StringComparer.Ordinal);

    private static void Append(string path, string line)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, line + "\n");
    }
}
