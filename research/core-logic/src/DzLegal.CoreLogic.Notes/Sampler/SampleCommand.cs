using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Notes.Sampler;

/// <summary>
/// <c>notes sample</c> (specs/notes-pipeline.md, "The commands of the notes tool"): draws a set's scenarios into
/// <c>&lt;work&gt;\scenarios\&lt;set&gt;.jsonl</c>, adds one queue line per scenario (state <c>drawn</c>) and writes the
/// set's manifest (seed, the sampler's commit, the hashes of what it drew from, the rejected draws by reason). A set
/// already drawn is never redrawn. With <c>--out &lt;file&gt;</c> it only writes the scenarios there (a trial: no queue).
/// </summary>
public static class SampleCommand
{
    public static int Run(NotesFiles files, string set, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        int Int(string name, int fallback) =>
            options.TryGetValue(name, out var v) ? int.Parse(v, CultureInfo.InvariantCulture) : fallback;
        // --batch <k> (k >= 2) draws a later batch into a set already drawn (the owner, 17 November 2026: dev and challenge
        // grow batch by batch): its ids carry «<set>-b<k>», its record is sets\<set>.sample-b<k>.json, nothing drawn is redrawn.
        var batch = Int("--batch", 1);
        if (batch < 1)
        {
            throw new ArgumentException("--batch is 2 or more (1 is the set's first draw)");
        }

        // --difficulty <id> (sampler 1.4) draws one challenge recipe into set challenge: its ids carry «challenge-<id>», its
        // record is sets\challenge.<id>.sample.json, and its scenarios join the set's file.
        var difficulty = options.GetValueOrDefault("--difficulty");
        if (difficulty is not null)
        {
            ChallengeRecipes.Get(difficulty);
        }

        if ((set == "challenge") != (difficulty is not null))
        {
            throw new ArgumentException($"--difficulty <id> goes with --set challenge, and set challenge needs it (the implemented ones are {ChallengeRecipes.Ids})");
        }

        var tag = (difficulty is null ? set : $"{set}-{difficulty}") + (batch > 1 ? $"-b{batch}" : "");
        var request = new SampleRequest(set, Int("--count", 0), Int("--seed", -1), Int("--groups", 0),
            difficulty is null && batch == 1 ? null : tag, difficulty);
        if (request.Count <= 0 || request.Seed < 0)
        {
            throw new ArgumentException("notes sample needs --count <n> and --seed <n>");
        }

        if (options.ContainsKey("--pack"))
        {
            throw new ArgumentException("--pack is not in the sampler yet (phase B)");
        }

        if (options.ContainsKey("--half") && options.ContainsKey("--writer"))
        {
            throw new ArgumentException("--half <a,b> and --writer exclude each other");
        }

        var data = SamplerData.Load(files);
        var sampler = new ScenarioSampler(data, FinalRecords(files));
        var scenarios = sampler.Draw(request);
        var lines = string.Concat(scenarios.Select(s => s.ToJsonString(NotesJson.Compact) + "\n"));

        if (options.TryGetValue("--out", out var trial))
        {
            NotesLoader.Guard(trial);
            NotesJson.Write(trial, lines);
            Console.WriteLine($"{scenarios.Count} scenarios (trial, no queue) -> {trial}; rejected draws: {Rejected(sampler)}");
            return 0;
        }

        var (commit, dirty) = SamplerCommit(files);
        if (dirty)
        {
            throw new InvalidOperationException("the sampler or its seed files have uncommitted changes: commit them first (a set records the sampler's commit)");
        }

        var work = options.GetValueOrDefault("--work") ?? @"C:\Users\TheShadow\dz-core-research\notes-work";
        NotesLoader.Guard(work);
        var path = Path.Combine(work, "scenarios", set + ".jsonl");
        var stem = difficulty is null ? set : $"{set}.{difficulty}";
        var manifest = Path.Combine(work, "sets", stem + (batch > 1 ? $".sample-b{batch}.json" : ".sample.json"));
        var first = Path.Combine(work, "sets", stem + ".sample.json");
        if (batch == 1 && ((difficulty is null && File.Exists(path)) || File.Exists(manifest)))
        {
            throw new InvalidOperationException($"{stem} is already drawn ({manifest}): sets are never redrawn (a later batch: --batch <k>)");
        }

        if (batch > 1 && (!File.Exists(path) || !File.Exists(first) || File.Exists(manifest)))
        {
            throw new InvalidOperationException($"batch {batch} of {stem}: its first draw must exist and the batch must be new ({manifest})");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        if (batch > 1 || File.Exists(path))
        {
            File.AppendAllText(path, lines);
        }
        else
        {
            NotesJson.Write(path, lines);
        }
        var writerOf = WriterOf(scenarios, options);
        File.AppendAllText(Path.Combine(work, "queue.jsonl"), string.Concat(scenarios.Select(s => new JsonObject
        {
            ["scenario"] = (string)s["id"]!, ["set"] = set, ["family"] = (string)s["family"]!, ["writer"] = writerOf[(string)s["family"]!],
            ["state"] = "drawn", ["attempts"] = 0,
        }.ToJsonString(NotesJson.Compact) + "\n")));
        var record = new JsonObject
        {
            ["set"] = set, ["batch"] = batch, ["count"] = scenarios.Count, ["seed"] = request.Seed, ["groups"] = request.Groups,
            ["difficulty"] = difficulty,
            ["writers"] = new JsonObject([.. writerOf.Values.GroupBy(w => w, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new KeyValuePair<string, JsonNode?>(g.Key, scenarios.Count(s => writerOf[(string)s["family"]!] == g.Key)))]),
            ["sampler"] = new JsonObject { ["version"] = ScenarioSampler.Version, ["commit"] = commit },
            ["inputs"] = data.Hashes(),
            ["finalRecords"] = new JsonArray([.. Directory.GetFiles(FinalFolder(files), "*.json").Order(StringComparer.Ordinal)
                .Select(f => (JsonNode)Path.GetFileNameWithoutExtension(f))]),
            ["rejected"] = new JsonObject([.. sampler.Rejected.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, kv.Value))]),
            ["scenariosSha256"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(),
        };
        NotesJson.Write(manifest, record.ToJsonString(NotesJson.Options) + "\n");
        Console.WriteLine($"{scenarios.Count} scenarios of set {set} -> {path}; queue +{scenarios.Count} (drawn); rejected draws: {Rejected(sampler)}");
        return 0;
    }

    /// <summary>
    /// The writer of each family. <c>--half codex,sonnet</c> (dev and challenge: specs/notes-pipeline.md, section 6)
    /// gives the families to the two writers in turn, in draw order, so both halves come from one seed stream and a
    /// polarity group, which is one family, is never split; otherwise every family goes to <c>--writer</c> (codex).
    /// </summary>
    public static Dictionary<string, string> WriterOf(IReadOnlyList<JsonObject> scenarios, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        ArgumentNullException.ThrowIfNull(options);
        var families = scenarios.Select(s => (string)s["family"]!).Distinct(StringComparer.Ordinal).ToList();
        if (options.TryGetValue("--half", out var half))
        {
            var pair = half.Split(',', StringSplitOptions.TrimEntries);
            if (pair.Length != 2 || pair.Any(w => w is not ("codex" or "sonnet")) || pair[0] == pair[1])
            {
                throw new ArgumentException("--half codex,sonnet");
            }

            return families.Select((f, i) => (f, i)).ToDictionary(x => x.f, x => pair[x.i % 2], StringComparer.Ordinal);
        }

        var writer = options.GetValueOrDefault("--writer") ?? "codex";
        return families.ToDictionary(f => f, _ => writer, StringComparer.Ordinal);
    }

    private static string Rejected(ScenarioSampler sampler) =>
        sampler.Rejected.Count == 0 ? "none" : string.Join("; ", sampler.Rejected.Select(kv => $"{kv.Key}: {kv.Value}"));

    private static string FinalFolder(NotesFiles files) => Path.Combine(files.CoreLogic, "rules", "final");

    /// <summary>The final rule records (rules/final/*.json): the sampler's outcomes come from these only.</summary>
    public static IReadOnlyList<RuleRecord> FinalRecords(NotesFiles files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var folder = FinalFolder(files);
        return !Directory.Exists(folder)
            ? []
            : [.. Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal).Select(f =>
                JsonSerializer.Deserialize<RuleRecord>(File.ReadAllText(f), ContractJson.Options)
                ?? throw new InvalidDataException($"{f} is empty"))];
    }

    /// <summary>The repository's commit, and whether the sampler's code or seed files differ from it.</summary>
    private static (string Commit, bool Dirty) SamplerCommit(NotesFiles files)
    {
        string Git(params string[] arguments)
        {
            var info = new ProcessStartInfo("git") { RedirectStandardOutput = true, UseShellExecute = false };
            info.ArgumentList.Add("-C");
            info.ArgumentList.Add(files.Repository);
            foreach (var a in arguments)
            {
                info.ArgumentList.Add(a);
            }

            using var process = Process.Start(info) ?? throw new InvalidOperationException("git did not start");
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }

        var commit = Git("rev-parse", "HEAD");
        var status = Git("status", "--porcelain", "--", "research/core-logic/src/DzLegal.CoreLogic.Notes",
            "docs/research/core-logic/notes/sampler");
        return (commit, status.Length > 0);
    }
}
