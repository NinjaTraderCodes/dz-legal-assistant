using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes;

/// <summary>What the notes are read for (specs/notes-pipeline.md, "The loader").</summary>
public enum LoadPurpose
{
    /// <summary>Computing a loss.</summary>
    Train,

    /// <summary>Every choice that changes the reader: early stopping, the encoder, a rung or head kept, temperature, thresholds.</summary>
    Tune,

    /// <summary>Reporting a score.</summary>
    Measure,
}

/// <summary>
/// One request. <see cref="Sets"/> names each part: a set (<c>train</c>, <c>dev</c> …), a set and a split
/// (<c>clean:train</c>), or a set and a writer's half (<c>dev:codex</c>, <c>dev:sonnet</c>, <c>challenge:codex</c>).
/// </summary>
public sealed record LoadRequest
{
    public required LoadPurpose Purpose { get; init; }
    public required IReadOnlyList<string> Sets { get; init; }

    /// <summary>A writer family whose notes are removed from the mix (notes/TERMS.md: the Codex notes can be replaced).</summary>
    public string? ExcludeWriter { get; init; }

    /// <summary>A slot pack named by its slot id: its notes join the sets; without it, pack notes are left out.</summary>
    public string? Pack { get; init; }

    /// <summary>The pilot's notes, only under the pilot's own rule (notes-pipeline.md, section 2).</summary>
    public bool AllowPilot { get; init; }

    /// <summary>The pre-commitment's scoring run of the mirror set, recorded as started in the mirror's RUNS.md.</summary>
    public int? MirrorRun { get; init; }

    /// <summary>The first <see cref="Size"/> notes of a fixed order (SHA-256 of the seed and the id): sizes nest.</summary>
    public int? Size { get; init; }
    public int SubsetSeed { get; init; } = 7;
}

/// <summary>The loader refuses by rule; every refusal has a fixed code and never names a note.</summary>
public sealed class LoaderRefusalException(string code, string message) : Exception($"{code}: {message}")
{
    public string Code { get; } = code;
}

public sealed record LoadResult(LoadPurpose Purpose, IReadOnlyList<JsonObject> Notes)
{
    /// <summary>The manifest beside an export: the purpose, the ids, the counts per set and per writer family.</summary>
    public JsonObject Manifest() => new()
    {
        ["purpose"] = Purpose.ToString().ToLowerInvariant(),
        ["count"] = Notes.Count,
        ["perSet"] = Counts(note => SetOf(note)),
        ["perWriter"] = Counts(WriterOf),
        ["ids"] = new JsonArray([.. Notes.Select(note => (JsonNode)JsonValue.Create(IdOf(note))!)]),
    };

    private JsonObject Counts(Func<JsonObject, string> key)
    {
        var counts = new JsonObject();
        foreach (var group in Notes.GroupBy(key).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            counts[group.Key] = group.Count();
        }

        return counts;
    }

    internal static string IdOf(JsonObject note) => note["id"]?.GetValue<string>() ?? "";

    internal static string SetOf(JsonObject note) =>
        note["set"]!.GetValue<string>() + (note["split"] is { } split ? ":" + split.GetValue<string>() : "");

    internal static string WriterOf(JsonObject note) =>
        note["provenance"]?["writer"]?["family"]?.GetValue<string>() ?? "none";
}

/// <summary>
/// The one door through which notes reach the reader's code. It reads <c>&lt;folder&gt;\&lt;set&gt;.jsonl</c> and refuses
/// by rule (notes-pipeline.md, "The loader"): a test note in training, a Claude-written note for tuning, a note without
/// its provenance, more than a quarter composed, a family cut by a split, the mirror set outside its scoring run, hidden
/// v3 always, and any path under C:\dz-hidden*.
/// </summary>
public sealed class NotesLoader(string folder, string testIdsPath, string? mirrorRunsPath = null)
{
    public const double ComposedCap = 0.25;

    private static readonly string[] TestSets = ["dev", "challenge", "clean:dev", "clean:test", "mirror", "probe"];
    private static readonly string[] TrainSets = ["train", "pilot", "composed", "clean:train"];

    public LoadResult Load(LoadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guard(folder);
        var notes = new List<JsonObject>();
        foreach (var part in request.Sets)
        {
            notes.AddRange(Part(request, part));
        }

        if (request.Pack is { } pack)
        {
            notes.AddRange(PackNotes(request, pack));
        }

        if (request.ExcludeWriter is { } family)
        {
            notes.RemoveAll(note => LoadResult.WriterOf(note) == family);
        }

        if (request.Purpose == LoadPurpose.Train)
        {
            CheckTraining(notes);
        }

        if (request.Size is { } size)
        {
            notes = [.. notes.OrderBy(note => Order(request.SubsetSeed, LoadResult.IdOf(note)), StringComparer.Ordinal).Take(size)];
        }

        return new LoadResult(request.Purpose, notes);
    }

    private IEnumerable<JsonObject> Part(LoadRequest request, string part)
    {
        var (set, qualifier) = part.Contains(':', StringComparison.Ordinal) ? (part[..part.IndexOf(':', StringComparison.Ordinal)], part[(part.IndexOf(':', StringComparison.Ordinal) + 1)..]) : (part, null);
        if (set == "hiddenV3")
        {
            throw new LoaderRefusalException("L02", "hidden set v3 is never read by the loader");
        }

        var key = set == "clean" ? part : set;
        var accepted = request.Purpose switch
        {
            LoadPurpose.Train => TrainSets.Contains(key, StringComparer.Ordinal),
            LoadPurpose.Tune => key is "dev" or "challenge" or "clean:dev",
            _ => TestSets.Contains(key, StringComparer.Ordinal) && key != "probe",
        };
        if (!accepted)
        {
            throw new LoaderRefusalException("L03", $"the set {part} is not read for {request.Purpose.ToString().ToLowerInvariant()}");
        }

        if (set == "pilot" && !request.AllowPilot)
        {
            throw new LoaderRefusalException("L04", "the pilot's notes are trained on only under the pilot's own rule");
        }

        if (set == "mirror" && !MirrorRunStarted(request.MirrorRun))
        {
            throw new LoaderRefusalException("L05", "the mirror set is read only by the pre-commitment's scoring run, recorded as started");
        }

        foreach (var note in Read(set).Where(note => !IsPack(note)))
        {
            if (qualifier is "train" or "dev" or "test" && note["split"]?.GetValue<string>() != qualifier)
            {
                continue;
            }

            var writer = LoadResult.WriterOf(note);
            if ((qualifier == "codex" && writer != "openai") || (qualifier == "sonnet" && writer != "anthropic"))
            {
                continue;
            }

            if (request.Purpose == LoadPurpose.Tune && writer == "anthropic")
            {
                throw new LoaderRefusalException("L20", "a note written by a Claude model never tunes the reader (ask for the Codex half)");
            }

            yield return note;
        }
    }

    private IEnumerable<JsonObject> PackNotes(LoadRequest request, string pack)
    {
        var sets = request.Purpose == LoadPurpose.Train ? new[] { "train" } : ["dev"];
        foreach (var set in sets)
        {
            foreach (var note in Read(set).Where(note => note["provenance"]?["pack"]?.GetValue<string>() == pack))
            {
                if (request.Purpose == LoadPurpose.Tune && LoadResult.WriterOf(note) == "anthropic")
                {
                    continue;  // a pack's dev notes tune through their Codex half only
                }

                yield return note;
            }
        }
    }

    private void CheckTraining(List<JsonObject> notes)
    {
        var testIds = File.Exists(testIdsPath)
            ? File.ReadAllLines(testIdsPath).Select(line => line.Trim()).Where(line => line.Length > 0).ToHashSet(StringComparer.Ordinal)
            : [];
        foreach (var note in notes)
        {
            var provenance = note["provenance"] as JsonObject;
            if (provenance?["writer"] is null || provenance["filter"] is null)
            {
                throw new LoaderRefusalException("L12", "a training note without its writer or its filter in the provenance");
            }

            if (LoadResult.WriterOf(note) == "anthropic")
            {
                throw new LoaderRefusalException("L11", "a note written by a Claude model is never trained on");
            }

            if (provenance["trainingAllowed"]?.GetValue<bool>() != true)
            {
                throw new LoaderRefusalException("L10", "a note with trainingAllowed false");
            }

            if (testIds.Contains(LoadResult.IdOf(note)))
            {
                throw new LoaderRefusalException("L13", "a test note in a training mix");
            }
        }

        var composed = notes.Count(note => note["provenance"]?["composedByCode"]?.GetValue<bool>() == true);
        if (notes.Count > 0 && composed > ComposedCap * notes.Count)
        {
            throw new LoaderRefusalException("L14", $"more than a quarter of the mix is composed ({composed.ToString(CultureInfo.InvariantCulture)} of {notes.Count.ToString(CultureInfo.InvariantCulture)})");
        }

        var testFamilies = TestSets.Select(set => set.Split(':')[0]).Distinct(StringComparer.Ordinal)
            .SelectMany(set => Read(set).Where(note => set != "clean" || note["split"]?.GetValue<string>() is "dev" or "test"))
            .Select(FamilyOf).Where(family => family is not null).ToHashSet(StringComparer.Ordinal);
        if (notes.Any(note => FamilyOf(note) is { } family && testFamilies.Contains(family)))
        {
            throw new LoaderRefusalException("L15", "a scenario family on both sides of a split");
        }
    }

    private IEnumerable<JsonObject> Read(string set)
    {
        var path = Path.Combine(folder, set + ".jsonl");
        Guard(path);
        if (!File.Exists(path))
        {
            yield break;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var note = JsonNode.Parse(line) as JsonObject ?? throw new InvalidDataException("a line that is not a note");
            if (note["status"]?.GetValue<string>() is "kept" or "keptWithBlanks")
            {
                yield return note;
            }
        }
    }

    private bool MirrorRunStarted(int? run)
    {
        if (run is not { } n || mirrorRunsPath is null || !File.Exists(mirrorRunsPath))
        {
            return false;
        }

        var wanted = $"| {n.ToString(CultureInfo.InvariantCulture)} |";
        return File.ReadLines(mirrorRunsPath).Any(line => line.StartsWith(wanted, StringComparison.Ordinal)
            && line.Contains("started", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPack(JsonObject note) => note["provenance"]?["pack"] is not null;

    private static string? FamilyOf(JsonObject note) => note["provenance"]?["scenarioFamily"]?.GetValue<string>();

    private static string Order(int seed, string id) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed.ToString(CultureInfo.InvariantCulture) + ":" + id)));

    /// <summary>No path under C:\dz-hidden*, whatever the purpose.</summary>
    public static void Guard(string path)
    {
        var full = Path.GetFullPath(path);
        if (full.StartsWith(@"C:\dz-hidden", StringComparison.OrdinalIgnoreCase))
        {
            throw new LoaderRefusalException("L01", "a path under C:\\dz-hidden* is never read by the loader");
        }
    }
}
