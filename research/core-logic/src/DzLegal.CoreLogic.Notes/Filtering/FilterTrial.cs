using System.Globalization;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Filtering;

/// <summary>
/// <c>notes filter-trial --notes 100 --seed &lt;n&gt; --model &lt;tag&gt;</c> (specs/notes-pipeline.md, "The filter trial"): the
/// filter on written pilot notes as they are, then on copies with one planted contradiction each (every sentence that holds
/// a quote of one fact deleted, when none of those sentences holds a quote of another fact). A plant counts only where the
/// unaltered copy's answer for that field was the label; it is caught when the planted copy's answer is not, or when rules
/// 1 to 3 drop the copy. Nothing is written to the queue or to a set: the planted copies are never trained on.
/// </summary>
public static class FilterTrial
{
    private static readonly string[] Fields = ["subject", "facts", "requests", "documents"];

    public static int Run(NotesFiles files, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        var work = FilterCommand.Work(options);
        var tag = options.GetValueOrDefault("--model") ?? throw new ArgumentException("--model <ollama tag>");
        var count = int.Parse(options.GetValueOrDefault("--notes") ?? "100", CultureInfo.InvariantCulture);
        var seed = int.Parse(options.GetValueOrDefault("--seed") ?? throw new ArgumentException("--seed <n>"), CultureInfo.InvariantCulture);
        var labels = new SheetLabels(SamplerData.Load(files));
        using var model = new LocalModel(tag, Path.Combine(files.Prompts, "schemas", "filter-sheet.schema.json"));
        var report = Measure(files, labels, model, work, count, seed, Console.WriteLine);
        var folder = Path.Combine(work, "filter-trial");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{tag.Replace(':', '_').Replace('/', '_')}-seed{seed}.json");
        NotesJson.Write(path, report.ToJsonString(NotesJson.Options) + "\n");
        Console.WriteLine(report["table"]!.ToJsonString(NotesJson.Options));
        Console.WriteLine(path);
        return 0;
    }

    public static JsonObject Measure(NotesFiles files, SheetLabels labels, ISheetAnswerer model, string work, int count, int seed, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        var written = FilterCommand.Written(work, "pilot")
            .Where(n => (string?)n["provenance"]?["writer"]?["family"] == "openai").ToList();
        var rng = new Rng(seed);
        var chosen = new List<JsonObject>();
        while (chosen.Count < count && written.Count > 0)
        {
            var i = rng.Between(0, written.Count - 1);
            chosen.Add(written[i]);
            written.RemoveAt(i);
        }

        var scenarios = WriteCommand.Scenarios(work, "pilot");
        var notes = new JsonArray();
        int dropped = 0, plants = 0, caught = 0, decoys = 0, decoysRight = 0;
        var seconds = new List<double>();
        foreach (var note in chosen)
        {
            var scenario = scenarios[(string)note["provenance"]!["scenario"]!];
            var plain = FilterCommand.Filter(files, labels, model, scenario, note);
            seconds.Add(plain.Asked.Sum(a => a.Seconds) / Math.Max(1, plain.Asked.Count));
            var (asked, right) = Decoys(plain.Sheet, plain.Asked[0].Letters);
            decoys += asked;
            decoysRight += right;
            var plainDropped = plain.Result.Verdict is not (FilterVerdict.Kept or FilterVerdict.KeptWithBlanks);
            dropped += plainDropped ? 1 : 0;
            var record = new JsonObject { ["note"] = (string)note["id"]!, ["plain"] = plain.Result.Verdict.ToString(), ["reason"] = plain.Reason };
            var plant = Plant(note, plain, Rng.Derive(seed, chosen.IndexOf(note)));
            if (plant is not null)
            {
                var (path, altered) = plant.Value;
                record["plantedField"] = path;
                var labelHeld = plain.Result.Answers.TryGetValue(path, out var before) &&
                                plain.Sheet.Questions.First(q => q.Path == path).Expected.Contains(before);
                record["counted"] = labelHeld;
                if (labelHeld)
                {
                    var planted = FilterCommand.Filter(files, labels, model, scenario, altered);
                    var dropByRule = planted.Result.Verdict is FilterVerdict.Invalid or FilterVerdict.Unsure or FilterVerdict.ClaimDisagreement;
                    var wrong = planted.Result.Answers.TryGetValue(path, out var after) &&
                                !planted.Sheet.Questions.First(q => q.Path == path).Expected.Contains(after);
                    plants++;
                    caught += dropByRule || wrong ? 1 : 0;
                    record["caught"] = dropByRule || wrong;
                    record["planted"] = planted.Result.Verdict.ToString();
                }
            }

            notes.Add(record);
            log?.Invoke($"{record["note"]}: {record["plain"]}{(record["plantedField"] is null ? "" : $"; plant {record["plantedField"]} counted={record["counted"]} caught={record["caught"]}")}");
        }

        seconds.Sort();
        return new JsonObject
        {
            ["model"] = model.Model, ["digest"] = model.Digest, ["sheet"] = FilterSheet.Version, ["seed"] = seed, ["notes"] = chosen.Count,
            ["table"] = new JsonObject
            {
                ["countedPlants"] = plants,
                ["caught"] = caught,
                ["caughtShare"] = plants == 0 ? 0 : Math.Round((double)caught / plants, 3),
                ["unalteredDropped"] = dropped,
                ["decoysRightShare"] = decoys == 0 ? 0 : Math.Round((double)decoysRight / decoys, 3),
                ["secondsPerNoteMedian"] = seconds.Count == 0 ? 0 : Math.Round(seconds[seconds.Count / 2], 2),
                ["passes"] = plants >= 60 && caught >= 0.8 * plants && dropped <= 15 * chosen.Count / 100.0,
                ["enoughPlants"] = plants >= 60,
            },
            ["perNote"] = notes,
        };
    }

    /// <summary>The decoy questions of a sheet, and how many the first answer got right ("not mentioned" or "no").</summary>
    private static (int Asked, int Right) Decoys(FilterSheet sheet, IReadOnlyList<string>? letters)
    {
        var asked = 0;
        var right = 0;
        for (var i = 0; i < sheet.Questions.Count; i++)
        {
            var q = sheet.Questions[i];
            if (q.Kind is not ("distractorClaim" or "decoyElement" or "decoyRequest"))
            {
                continue;
            }

            asked++;
            var letter = letters is not null && i < letters.Count ? letters[i] : null;
            right += q.Options.FirstOrDefault(o => o.Letter == letter) is SheetOption o && q.Expected.Contains(o.Value) ? 1 : 0;
        }

        return (asked, right);
    }

    /// <summary>
    /// One plant: a stated, denied or disputed element, a slot with a known value, or a request, chosen by seed among those
    /// whose sentences hold no quote of another fact; the copy without those sentences. Null when no fact can be planted.
    /// </summary>
    public static (string Path, JsonObject Altered)? Plant(JsonObject note, FilteredNote plain, int seed)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(plain);
        var labels = note["provenance"]!["writerLabels"]!.AsObject();
        var facts = Facts(labels);
        var candidates = new List<(string Path, List<JsonObject> Quotes)>();
        foreach (var (path, quotes) in facts.Where(f => plain.Sheet.Questions.Any(q => q.Path == f.Path)))
        {
            var sentences = quotes.Select(q => Sentence(note, q)).Distinct().ToList();
            var others = facts.Where(f => f.Path != path).SelectMany(f => f.Quotes)
                .Concat(Quotes(labels["claimEvidence"]))
                .Where(q => !quotes.Any(own => Same(own, q)));
            if (others.All(q => !sentences.Contains(Sentence(note, q))))
            {
                candidates.Add((path, quotes));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var (chosenPath, chosenQuotes) = candidates[new Rng(seed).Between(0, candidates.Count - 1)];
        var altered = note.DeepClone().AsObject();
        foreach (var field in Fields)
        {
            var text = (string?)note["text"]?[field] ?? "";
            var cut = chosenQuotes.Where(q => (string)q["field"]! == field).Select(q => Sentence(note, q)).Distinct()
                .OrderByDescending(s => s.Start).ToList();
            foreach (var (_, start, end) in cut)
            {
                text = text.Remove(start, end - start);
            }

            altered["text"]![field] = text;
        }

        return (chosenPath, altered);
    }

    /// <summary>The facts a plant may target, with every quote that carries them (their evidence and their values' words).</summary>
    private static List<(string Path, List<JsonObject> Quotes)> Facts(JsonObject labels)
    {
        var facts = new List<(string, List<JsonObject>)>();
        foreach (var e in labels["events"]?.AsArray() ?? [])
        {
            if ((string)e!["polarity"]! is "stated" or "denied" or "disputed")
            {
                var quotes = Quotes(e["evidence"]);
                quotes.AddRange(new[] { e["date"]?["at"], e["amount"]?["at"] }.Where(a => a is not null).Select(a => a!.AsObject()));
                facts.Add(($"events/{e["claim"]}/{e["kind"]}/polarity", quotes));
            }
        }

        foreach (var s in labels["slots"]?.AsArray() ?? [])
        {
            if ((string)s!["value"]! != "unknown" && Quotes(s["evidence"]).Count > 0)
            {
                facts.Add(($"slots/{s["slot"]}", Quotes(s["evidence"])));
            }
        }

        foreach (var r in labels["requests"]?.AsArray() ?? [])
        {
            var quotes = Quotes(r!["evidence"]);
            if (r["amount"]?["at"] is JsonNode at)
            {
                quotes.Add(at.AsObject());
            }

            facts.Add(($"requests/{r["kind"]}", quotes));
        }

        return facts;
    }

    private static List<JsonObject> Quotes(JsonNode? array) => [.. (array?.AsArray() ?? []).Where(q => q is not null).Select(q => q!.AsObject())];

    private static bool Same(JsonObject a, JsonObject b) =>
        (string)a["field"]! == (string)b["field"]! && (int)a["start"]! == (int)b["start"]! && (int)a["length"]! == (int)b["length"]!;

    /// <summary>
    /// The sentence that holds a quote: the stretch between two marks (a full stop, «؛», «؟», «!», a line break); a point
    /// between two digits is part of a number, not a mark. In a note of dashes or numbered points each line is a sentence.
    /// </summary>
    public static (string Field, int Start, int End) Sentence(JsonObject note, JsonObject quote)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(quote);
        var field = (string)quote["field"]!;
        var text = (string?)note["text"]?[field] ?? "";
        var at = (int)quote["start"]!;
        var start = at;
        while (start > 0 && !Mark(text, start - 1))
        {
            start--;
        }

        var end = at + (int)quote["length"]!;
        while (end < text.Length && !Mark(text, end))
        {
            end++;
        }

        if (end < text.Length)
        {
            end++;  // the mark goes with its sentence
        }

        return (field, start, end);
    }

    private static bool Mark(string text, int i) => text[i] switch
    {
        '؛' or '؟' or '!' or '\n' => true,
        '.' => !(i > 0 && i + 1 < text.Length && char.IsDigit(text[i - 1]) && char.IsDigit(text[i + 1])),
        _ => false,
    };
}
