using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Filtering;

/// <summary>The laptop's power: on battery or on mains, and the charge left.</summary>
public sealed record Power(bool OnBattery, int Percent);

/// <summary>Reads the power status (kernel32 GetSystemPowerStatus); a stand-in for tests.</summary>
public interface IPowerSource
{
    Power Read();
}

public sealed class SystemPower : IPowerSource
{
    public Power Read()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            return new Power(false, 100);
        }

        return new Power(status.AcLineStatus == 0, status.BatteryLifePercent == 255 ? 100 : status.BatteryLifePercent);
    }
}

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemPowerStatus(out SystemPowerStatus status);
}

/// <summary>
/// <c>notes filter-trial --notes 100 --seed &lt;n&gt; --model &lt;tag&gt; [--min-battery 20]</c> (specs/notes-pipeline.md, "The
/// filter trial"): the filter on written pilot notes as they are, then on copies with one planted contradiction each (every
/// sentence that holds a quote of one fact deleted, when none of those sentences holds a quote of another fact). A plant
/// counts only where the unaltered copy's answer for that field was the label; it is caught when the planted copy's answer
/// is not, or when rules 1 to 3 drop the copy. Nothing is written to the queue or to a set: the planted copies are never
/// trained on.
/// <para>
/// The owner's conditions of 16 November 2026 (the trial ran on battery): the notes are chosen once and kept in
/// <c>trial-seed&lt;n&gt;.chosen.json</c> for every model; each model runs two passes, the notes as written and then the
/// planted copies; each note's result is appended to its pass's file as soon as it is done, and a resumed trial skips the
/// notes already done; before each note the battery is read, and at or under <c>--min-battery</c> percent on battery the
/// trial stops (exit 3) with everything saved. Each record keeps the power state, so the seconds are read as battery or
/// mains figures.
/// </para>
/// </summary>
public static class FilterTrial
{
    public const int BatteryStop = 3;
    private static readonly string[] Fields = ["subject", "facts", "requests", "documents"];

    public static int Run(NotesFiles files, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        var work = FilterCommand.Work(options);
        var tag = options.GetValueOrDefault("--model") ?? throw new ArgumentException("--model <ollama tag>");
        var count = int.Parse(options.GetValueOrDefault("--notes") ?? "100", CultureInfo.InvariantCulture);
        var seed = int.Parse(options.GetValueOrDefault("--seed") ?? throw new ArgumentException("--seed <n>"), CultureInfo.InvariantCulture);
        var minBattery = int.Parse(options.GetValueOrDefault("--min-battery") ?? "20", CultureInfo.InvariantCulture);
        var labels = new SheetLabels(SamplerData.Load(files));
        using var model = new LocalModel(tag);
        var (report, stopped) = Measure(files, labels, model, work, count, seed, new SystemPower(), minBattery, Console.WriteLine);
        Console.WriteLine(report["table"]!.ToJsonString(NotesJson.Options));
        Console.WriteLine(ReportPath(work, tag, seed));
        return stopped ? BatteryStop : 0;
    }

    private static string Folder(string work) => Path.Combine(work, "filter-trial");

    private static string Stem(string work, string tag, int seed) =>
        Path.Combine(Folder(work), $"{tag.Replace(':', '_').Replace('/', '_')}-seed{seed}");

    public static string ReportPath(string work, string tag, int seed) => Stem(work, tag, seed) + ".json";

    /// <summary>
    /// The trial's notes, chosen once by the seed among the written pilot notes and kept for every model and resume; a
    /// trial of fewer notes takes the first of that list, so that every model is measured on the same notes.
    /// </summary>
    public static List<string> Chosen(string work, int count, int seed)
    {
        var path = Path.Combine(Folder(work), $"trial-seed{seed}.chosen.json");
        if (File.Exists(path))
        {
            // A smaller trial takes the first notes of the same list (the owner's decision of 16 November 2026: Jais on 10).
            return [.. NotesJson.Load(path)["notes"]!.AsArray().Select(n => (string)n!).Take(count)];
        }

        var written = FilterCommand.Written(work, "pilot")
            .Where(n => (string?)n["provenance"]?["writer"]?["family"] == "openai").Select(n => (string)n["id"]!).ToList();
        var rng = new Rng(seed);
        var chosen = new List<string>();
        while (chosen.Count < count && written.Count > 0)
        {
            var i = rng.Between(0, written.Count - 1);
            chosen.Add(written[i]);
            written.RemoveAt(i);
        }

        Directory.CreateDirectory(Folder(work));
        NotesJson.Write(path, new JsonObject { ["seed"] = seed, ["notes"] = new JsonArray([.. chosen.Select(c => (JsonNode)c)]) }
            .ToJsonString(NotesJson.Options) + "\n");
        return chosen;
    }

    private static Dictionary<string, JsonObject> Done(string path) =>
        File.Exists(path)
            ? File.ReadLines(path).Where(l => l.Trim().Length > 0).Select(l => JsonNode.Parse(l)!.AsObject())
                .ToDictionary(r => (string)r["note"]!, StringComparer.Ordinal)
            : new Dictionary<string, JsonObject>(StringComparer.Ordinal);

    private static FileStream? TryLock(string path)
    {
        try
        {
            return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static JsonObject PowerNode(Power p) => new() { ["onBattery"] = p.OnBattery, ["percent"] = p.Percent };

    public static (JsonObject Report, bool Stopped) Measure(NotesFiles files, SheetLabels labels, ISheetAnswerer model, string work,
        int count, int seed, IPowerSource power, int minBattery, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(power);
        var chosen = Chosen(work, count, seed);
        var notes = FilterCommand.Written(work, "pilot").ToDictionary(n => (string)n["id"]!, StringComparer.Ordinal);
        var scenarios = WriteCommand.Scenarios(work, "pilot");
        var stem = Stem(work, model.Model, seed);
        var plainPath = stem + ".plain.jsonl";
        var plantedPath = stem + ".planted.jsonl";
        Directory.CreateDirectory(Folder(work));

        // One trial per model at a time: two processes appending to the same files recorded notes twice (16 November
        // 2026). The lock is released when the process ends, however it ends.
        using var trialLock = TryLock(stem + ".lock")
            ?? throw new InvalidOperationException($"another trial of {model.Model} is running (lock {stem}.lock)");

        bool LowBattery()
        {
            var p = power.Read();
            if (p.OnBattery && p.Percent <= minBattery)
            {
                log?.Invoke($"STOP: battery at {p.Percent} % (floor {minBattery} %); everything done so far is saved");
                return true;
            }

            return false;
        }

        // Pass 1: every chosen note as written.
        var plain = Done(plainPath);
        foreach (var id in chosen.Where(id => !plain.ContainsKey(id)))
        {
            if (LowBattery())
            {
                return (Report(model, seed, chosen, Done(plainPath), Done(plantedPath)), true);
            }

            var note = notes[id];
            var scenario = scenarios[(string)note["provenance"]!["scenario"]!];
            var p = power.Read();
            var done = FilterCommand.Filter(files, labels, model, scenario, note);
            // The decoys are scored on the sheet the first answer answered (a decoy failure rebuilds the sheet with others).
            var (asked, right) = Decoys(done.FirstSheet ?? done.Sheet, done.Asked[0].Letters);
            var record = new JsonObject
            {
                ["note"] = id, ["verdict"] = done.Result.Verdict.ToString(), ["reason"] = done.Reason,
                ["rightPaths"] = new JsonArray([.. done.Sheet.Questions
                    .Where(q => done.Result.Answers.TryGetValue(q.Path, out var a) && q.Expected.Contains(a))
                    .Select(q => (JsonNode)q.Path)]),
                ["blanked"] = new JsonArray([.. done.Result.Blanked.Select(b => (JsonNode)b)]),
                ["decoysAsked"] = asked, ["decoysRight"] = right,
                ["letters"] = new JsonArray([.. done.Asked.Select(a => (JsonNode)new JsonArray([.. (a.Letters ?? []).Select(l => (JsonNode)l)]))]),
                ["seconds"] = Math.Round(done.Asked.Sum(a => a.Seconds) / Math.Max(1, done.Asked.Count), 2),
                ["calls"] = done.Asked.Count, ["power"] = PowerNode(p),
            };
            File.AppendAllText(plainPath, record.ToJsonString(NotesJson.Compact) + "\n");
            plain[id] = record;
            log?.Invoke($"plain {id}: {record["verdict"]} ({record["seconds"]} s, {(p.OnBattery ? "battery" : "mains")} {p.Percent} %)");
        }

        // Pass 2: the planted copies; a plant counts where the plain run answered its field as the label.
        var planted = Done(plantedPath);
        foreach (var id in chosen.Where(id => !planted.ContainsKey(id)))
        {
            if (LowBattery())
            {
                return (Report(model, seed, chosen, plain, Done(plantedPath)), true);
            }

            var note = notes[id];
            var scenario = scenarios[(string)note["provenance"]!["scenario"]!];
            var sheet = FilterSheet.Build(labels, scenario, note, FilterCommand.SheetSeed(id, 0));
            var stub = new FilteredNote(new FilterResult(FilterVerdict.Kept, [], 0, new Dictionary<string, string>()), sheet, [], "");
            var plant = Plant(note, stub, Rng.Derive(seed, chosen.IndexOf(id)));
            var record = new JsonObject { ["note"] = id };
            if (plant is { } pl)
            {
                var counted = plain[id]["rightPaths"]!.AsArray().Any(x => (string?)x == pl.Path);
                record["plantedField"] = pl.Path;
                record["counted"] = counted;
                if (counted)
                {
                    var p = power.Read();
                    var run = FilterCommand.Filter(files, labels, model, scenario, pl.Altered);
                    var dropByRule = run.Result.Verdict is FilterVerdict.Invalid or FilterVerdict.Unsure or FilterVerdict.ClaimDisagreement;
                    var wrong = run.Result.Answers.TryGetValue(pl.Path, out var after) &&
                                !run.Sheet.Questions.First(q => q.Path == pl.Path).Expected.Contains(after);
                    record["caught"] = dropByRule || wrong;
                    record["verdict"] = run.Result.Verdict.ToString();
                    record["seconds"] = Math.Round(run.Asked.Sum(a => a.Seconds) / Math.Max(1, run.Asked.Count), 2);
                    record["power"] = PowerNode(p);
                }
            }

            File.AppendAllText(plantedPath, record.ToJsonString(NotesJson.Compact) + "\n");
            planted[id] = record;
            log?.Invoke($"planted {id}: " + (record["plantedField"] is null
                ? "no plant possible"
                : $"{record["plantedField"]} counted={record["counted"]} caught={record["caught"]}"));
        }

        var report = Report(model, seed, chosen, plain, planted);
        NotesJson.Write(ReportPath(work, model.Model, seed), report.ToJsonString(NotesJson.Options) + "\n");
        return (report, false);
    }

    /// <summary>The trial's table from the per-note records (complete or not).</summary>
    private static JsonObject Report(ISheetAnswerer model, int seed, List<string> chosen, Dictionary<string, JsonObject> plain,
        Dictionary<string, JsonObject> planted)
    {
        var counted = planted.Values.Where(r => (bool?)r["counted"] == true).ToList();
        var caught = counted.Count(r => (bool?)r["caught"] == true);
        var dropped = plain.Values.Count(r => (string?)r["verdict"] is not ("Kept" or "KeptWithBlanks"));
        var decoys = plain.Values.Sum(r => (int?)r["decoysAsked"] ?? 0);
        var decoysRight = plain.Values.Sum(r => (int?)r["decoysRight"] ?? 0);
        var seconds = plain.Values.Select(r => (double?)r["seconds"] ?? 0).Order().ToList();
        var onBattery = plain.Values.Concat(planted.Values).Count(r => (bool?)r["power"]?["onBattery"] == true);
        var timed = plain.Values.Concat(planted.Values).Count(r => r["power"] is not null);
        return new JsonObject
        {
            ["model"] = model.Model, ["digest"] = model.Digest, ["sheet"] = FilterSheet.Version, ["seed"] = seed,
            ["constrainedOutput"] = model is not LocalModel local || local.Constrained,
            ["complete"] = plain.Count == chosen.Count && planted.Count == chosen.Count,
            ["notes"] = chosen.Count, ["plainDone"] = plain.Count, ["plantedDone"] = planted.Count,
            ["table"] = new JsonObject
            {
                ["countedPlants"] = counted.Count,
                ["caught"] = caught,
                ["caughtShare"] = counted.Count == 0 ? 0 : Math.Round((double)caught / counted.Count, 3),
                ["unalteredDropped"] = dropped,
                ["decoysRightShare"] = decoys == 0 ? 0 : Math.Round((double)decoysRight / decoys, 3),
                ["secondsPerNoteMedian"] = seconds.Count == 0 ? 0 : Math.Round(seconds[seconds.Count / 2], 2),
                ["notesTimedOnBattery"] = $"{onBattery} of {timed}",
                ["passes"] = counted.Count >= 60 && caught >= 0.8 * counted.Count && dropped <= 15 * chosen.Count / 100.0,
                ["enoughPlants"] = counted.Count >= 60,
            },
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
