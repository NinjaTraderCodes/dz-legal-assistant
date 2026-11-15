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
        var request = new SampleRequest(set, Int("--count", 0), Int("--seed", -1), Int("--groups", 0));
        if (request.Count <= 0 || request.Seed < 0)
        {
            throw new ArgumentException("notes sample needs --count <n> and --seed <n>");
        }

        if (options.ContainsKey("--difficulty") || options.ContainsKey("--half") || options.ContainsKey("--pack"))
        {
            throw new ArgumentException("--difficulty, --half and --pack are not in sampler version 1 yet (phase B)");
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
        var manifest = Path.Combine(work, "sets", set + ".sample.json");
        if (File.Exists(path) || File.Exists(manifest))
        {
            throw new InvalidOperationException($"set {set} is already drawn ({path}): sets are never redrawn");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        NotesJson.Write(path, lines);
        var writer = options.GetValueOrDefault("--writer") ?? "codex";
        File.AppendAllText(Path.Combine(work, "queue.jsonl"), string.Concat(scenarios.Select(s => new JsonObject
        {
            ["scenario"] = (string)s["id"]!, ["set"] = set, ["family"] = (string)s["family"]!, ["writer"] = writer,
            ["state"] = "drawn", ["attempts"] = 0,
        }.ToJsonString(NotesJson.Compact) + "\n")));
        var record = new JsonObject
        {
            ["set"] = set, ["count"] = scenarios.Count, ["seed"] = request.Seed, ["groups"] = request.Groups,
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
