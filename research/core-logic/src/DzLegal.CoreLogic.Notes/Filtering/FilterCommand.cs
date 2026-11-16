using System.Globalization;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Filtering;

/// <summary>What filtering one note gave: its result, the sheet that decided, the answers asked, and the seconds spent.</summary>
/// <param name="Result">The rule's result on the last sheet asked.</param>
/// <param name="Sheet">The last sheet asked (a decoy failure rebuilds it once).</param>
/// <param name="Asked">Every answer, in order; the first answers <paramref name="FirstSheet"/>.</param>
/// <param name="Reason">Why the note was dropped, or empty.</param>
/// <param name="FirstSheet">The first sheet asked; null means the same as <paramref name="Sheet"/>.</param>
public sealed record FilteredNote(FilterResult Result, FilterSheet Sheet, IReadOnlyList<ModelAnswer> Asked, string Reason,
    FilterSheet? FirstSheet = null);

/// <summary>
/// <c>notes filter-sheet --note &lt;id&gt; --set &lt;set&gt;</c> and <c>notes filter --set &lt;set&gt; --model &lt;tag&gt;</c>
/// (specs/notes-pipeline.md, section 4a): the closed sheet of each written training note, the local model's letters, and
/// the five-step rule with its two retries (an answer that does not fit is asked once more; a decoy answered as present
/// rebuilds the sheet with other decoys once). A kept note goes into <c>&lt;work&gt;\notes\&lt;set&gt;.jsonl</c> with its gold;
/// a dropped one into <c>&lt;work&gt;\dropped\&lt;set&gt;.jsonl</c>; the letters stay in <c>&lt;work&gt;\filter\</c>, never in the note.
/// </summary>
public static class FilterCommand
{
    public static string Prompt(NotesFiles files, JsonObject written, FilterSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(written);
        ArgumentNullException.ThrowIfNull(sheet);
        string Field(string name) => (string?)written["text"]?[name] is { Length: > 0 } s ? s : "(فارغ)";
        return PromptTemplate.Fill(PromptTemplate.Body(files.Prompts, "notes-filter-local"), new Dictionary<string, string>
        {
            ["subject"] = Field("subject"), ["facts"] = Field("facts"), ["requests"] = Field("requests"), ["documents"] = Field("documents"),
            ["questions"] = sheet.Text(), ["count"] = sheet.Questions.Count.ToString(CultureInfo.InvariantCulture),
        });
    }

    /// <summary>The seed of a note's sheet: stable for the note and the attempt (0 first, 1 after a decoy failure).</summary>
    public static int SheetSeed(string noteId, int attempt)
    {
        ArgumentNullException.ThrowIfNull(noteId);
        var h = 2166136261u;
        foreach (var c in noteId)
        {
            h = unchecked((h ^ c) * 16777619u);
        }

        return Rng.Derive((int)(h & 0x7FFFFFFF), attempt);
    }

    /// <summary>Filters one note with its retries.</summary>
    public static FilteredNote Filter(NotesFiles files, SheetLabels labels, ISheetAnswerer model, JsonObject scenario, JsonObject written)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(written);
        var asked = new List<ModelAnswer>();
        FilterSheet sheet = null!;
        FilterResult result = null!;
        FilterSheet? first = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            sheet = FilterSheet.Build(labels, scenario, written, SheetSeed((string)written["id"]!, attempt));
            first ??= sheet;
            var prompt = Prompt(files, written, sheet);
            var answer = model.Answer(prompt, sheet.Questions.Select(q => (IReadOnlyList<string>)[.. q.Options.Select(o => o.Letter)]).ToList());
            asked.Add(answer);
            result = FilterRule.Apply(sheet, answer.Letters);
            if (result.Verdict == FilterVerdict.Invalid)
            {
                answer = model.Answer(prompt, sheet.Questions.Select(q => (IReadOnlyList<string>)[.. q.Options.Select(o => o.Letter)]).ToList());
                asked.Add(answer);
                result = FilterRule.Apply(sheet, answer.Letters);
                if (result.Verdict == FilterVerdict.Invalid)
                {
                    return new FilteredNote(result, sheet, asked, "filterInvalid", first);
                }
            }

            if (result.Verdict != FilterVerdict.Unsure)
            {
                break;
            }
        }

        var reason = result.Verdict switch
        {
            FilterVerdict.Unsure => "filterUnsure",
            FilterVerdict.ClaimDisagreement => "claimDisagreement",
            FilterVerdict.TooManyBlanks => "tooManyBlanks",
            _ => "",
        };
        return new FilteredNote(result, sheet, asked, reason, first);
    }

    public static int RunSheet(NotesFiles files, string noteId, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var work = Work(options);
        var set = options.GetValueOrDefault("--set") ?? throw new ArgumentException("--set <set>");
        var written = Written(work, set).Single(n => (string)n["id"]! == noteId);
        var scenario = WriteCommand.Scenarios(work, set)[(string)written["provenance"]!["scenario"]!];
        var attempt = int.Parse(options.GetValueOrDefault("--attempt") ?? "0", CultureInfo.InvariantCulture);
        var sheet = FilterSheet.Build(new SheetLabels(SamplerData.Load(files)), scenario, written, SheetSeed(noteId, attempt));
        if (options.ContainsKey("--json"))
        {
            // The sheet itself, for inspection: each question's kind, path, options and the values the label accepts.
            Console.WriteLine(new JsonObject
            {
                ["prompt"] = Prompt(files, written, sheet),
                ["questions"] = new JsonArray([.. sheet.Questions.Select(q => (JsonNode)new JsonObject
                {
                    ["kind"] = q.Kind, ["path"] = q.Path, ["text"] = q.Text,
                    ["options"] = new JsonArray([.. q.Options.Select(o => (JsonNode)new JsonObject { ["letter"] = o.Letter, ["value"] = o.Value, ["text"] = o.Text })]),
                    ["expected"] = new JsonArray([.. q.Expected.Select(e => (JsonNode)e)]),
                })]),
            }.ToJsonString(NotesJson.Compact));
            return 0;
        }

        Console.WriteLine(Prompt(files, written, sheet));
        return 0;
    }

    public static int Run(NotesFiles files, string set, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        var work = Work(options);
        var tag = options.GetValueOrDefault("--model") ?? throw new ArgumentException("--model <ollama tag>");
        var limit = options.TryGetValue("--limit", out var l) ? int.Parse(l, CultureInfo.InvariantCulture) : int.MaxValue;
        var labels = new SheetLabels(SamplerData.Load(files));
        using var model = new LocalModel(tag);
        var scenarios = WriteCommand.Scenarios(work, set);
        var notes = Written(work, set).ToDictionary(n => (string)n["id"]!, StringComparer.Ordinal);
        var queue = QueueFile.Load(work);
        var todo = queue.Lines.Where(x => (string?)x["set"] == set && (string?)x["state"] == "written" && (string?)x["writer"] == "codex")
            .Take(limit).ToList();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in todo)
        {
            var written = notes[(string)line["note"]!];
            var scenario = scenarios[(string)line["scenario"]!];
            var done = Filter(files, labels, model, scenario, written);
            var status = done.Result.Verdict switch
            {
                FilterVerdict.Kept => "kept",
                FilterVerdict.KeptWithBlanks => "keptWithBlanks",
                _ => "dropped",
            };
            var provenance = written["provenance"]!.DeepClone().AsObject();
            provenance["filter"] = new JsonObject
            {
                ["family"] = "local", ["model"] = tag, ["version"] = $"{FilterSheet.Version}+{model.Digest[..12]}",
                ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            };
            var note = new JsonObject
            {
                ["id"] = (string)written["id"]!, ["set"] = set, ["text"] = written["text"]!.DeepClone(),
                ["gold"] = FilterRule.Gold(written["provenance"]!["writerLabels"]!.AsObject(), done.Result.Blanked),
                ["blanked"] = new JsonArray([.. done.Result.Blanked.Select(b => (JsonNode)b)]),
                ["provenance"] = provenance, ["status"] = status,
            };
            if (status == "dropped")
            {
                note["dropReason"] = done.Reason;
            }

            Append(Path.Combine(work, status == "dropped" ? "dropped" : "notes", set + ".jsonl"), note.ToJsonString(NotesJson.Compact));
            Append(Path.Combine(work, "filter", set + ".jsonl"), new JsonObject
            {
                ["note"] = (string)written["id"]!, ["model"] = tag, ["version"] = FilterSheet.Version, ["status"] = status, ["reason"] = done.Reason,
                ["counted"] = done.Result.Counted, ["blanked"] = new JsonArray([.. done.Result.Blanked.Select(b => (JsonNode)b)]),
                ["answers"] = new JsonObject([.. done.Result.Answers.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, kv.Value))]),
                ["seconds"] = Math.Round(done.Asked.Sum(a => a.Seconds), 2), ["calls"] = done.Asked.Count,
            }.ToJsonString(NotesJson.Compact));
            line["state"] = status;
            line["checkRun"] = $"filter:{tag}";
            queue.Save();
            var key = status == "dropped" ? "dropped:" + done.Reason : status;
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        Console.WriteLine($"{todo.Count} notes filtered by {tag}: " + string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}")));
        return 0;
    }

    public static string Work(IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var work = options.GetValueOrDefault("--work") ?? @"C:\Users\TheShadow\dz-core-research\notes-work";
        NotesLoader.Guard(work);
        return work;
    }

    public static List<JsonObject> Written(string work, string set)
    {
        var path = Path.Combine(work, "written", set + ".jsonl");
        return File.Exists(path) ? [.. File.ReadLines(path).Where(x => x.Trim().Length > 0).Select(x => JsonNode.Parse(x)!.AsObject())] : [];
    }

    private static void Append(string path, string line)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, line + "\n");
    }
}
