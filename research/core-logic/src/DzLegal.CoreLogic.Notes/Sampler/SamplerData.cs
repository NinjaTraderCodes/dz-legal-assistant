using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DzLegal.CoreLogic.Notes.Sampler;

public sealed record ElementDef(string Claim, string Kind, string? Actor, string Label, bool HasDate, bool HasAmount);

public sealed record RoleDef(string Relation, string Position);

public sealed record RequestDef(string Kind, bool HasAmount);

public sealed record ClaimDef(string Id, IReadOnlyList<RoleDef> Roles, IReadOnlyList<ElementDef> Elements, IReadOnlyList<RequestDef> Requests);

public sealed record ReadSlot(string Id, string Type, IReadOnlyList<string> Choices, IReadOnlyList<string> AskedFor)
{
    public bool AskedForClaim(string claim) => AskedFor.Contains("*") || AskedFor.Contains(claim);
}

/// <summary>The English seeds of one element (notes/sampler/elements.json, a kind's lists with its claim type's overrides).</summary>
public sealed record ElementSeeds(IReadOnlyList<string> Concepts, IReadOnlyList<string> Denied, IReadOnlyList<string> Disputed,
    IReadOnlyList<string> Cause, string DisputedBy);

/// <summary>
/// Everything the sampler draws from (specs/scenario-sampler.md, version 1): the claim catalogue and the slots (read only),
/// the cells table of SCHEME.md section 8, and the sampler's own seed files under notes/sampler.
/// </summary>
public sealed partial class SamplerData
{
    private readonly JsonObject seeds;

    private SamplerData(NotesFiles files)
    {
        Files = files;
        var catalogue = NotesJson.Load(files.ClaimElements)["claims"]!.AsArray();
        Claims = catalogue.Select(c => new ClaimDef(
            (string)c!["claim"]!,
            [.. c["roles"]!.AsArray().Select(r => new RoleDef((string)r!["relation"]!, (string)r["position"]!))],
            [.. c["elements"]!.AsArray().Select(e => new ElementDef((string)c["claim"]!, (string)e!["event"]!, (string?)e["actor"],
                (string)e["label"]!, (bool?)e["date"] ?? false, (bool?)e["amount"] ?? false))],
            [.. c["requests"]!.AsArray().Select(r => new RequestDef((string)r!["kind"]!, (bool?)r["amount"] ?? false))])).ToList();
        ClaimById = Claims.ToDictionary(c => c.Id, StringComparer.Ordinal);
        Slots = [.. NotesJson.Load(Path.Combine(files.CoreLogic, "slots.json"))["slots"]!.AsArray()
            .Where(s => (bool?)s!["readFromNote"] ?? false)
            .Select(s => new ReadSlot((string)s!["id"]!, (string)s["type"]!,
                [.. (s["choices"]?.AsArray() ?? []).Select(c => (string)c!["id"]!)],
                [.. s["askedFor"]!.AsArray().Select(a => (string)a!)]))];
        DeniedDrawn = ReadCells(File.ReadAllText(files.Scheme));
        seeds = NotesJson.Load(Path.Combine(Folder, "elements.json"))["kinds"]!.AsObject();
        Cast = NotesJson.Load(Path.Combine(Folder, "cast.json")).AsObject();
        Tables = NotesJson.Load(Path.Combine(Folder, "tables.json")).AsObject();
        Cards = File.Exists(Path.Combine(Folder, "style-cards.json"))
            ? StyleCards.Load(Path.Combine(Folder, "style-cards.json"))
            : [];
    }

    public NotesFiles Files { get; }

    public string Folder => Path.Combine(Files.Notes, "sampler");

    public IReadOnlyList<ClaimDef> Claims { get; }

    public IReadOnlyDictionary<string, ClaimDef> ClaimById { get; }

    public IReadOnlyList<ReadSlot> Slots { get; }

    /// <summary>For each element ("claim/kind"), whether its <c>denied</c> cell is drawn (SCHEME.md, section 8).</summary>
    public IReadOnlyDictionary<string, bool> DeniedDrawn { get; }

    public JsonObject Cast { get; }

    public JsonObject Tables { get; }

    public IReadOnlyList<StyleCard> Cards { get; }

    public static SamplerData Load(NotesFiles files) => new(files);

    public ElementSeeds Seeds(string claim, string kind)
    {
        var node = seeds[kind]?.AsObject() ?? throw new KeyNotFoundException($"notes/sampler/elements.json has no kind {kind}.");
        var over = node["byClaim"]?[claim]?.AsObject();
        IReadOnlyList<string> List(string name) =>
            [.. ((over?[name] ?? node[name])?.AsArray() ?? []).Select(x => (string)x!)];
        return new ElementSeeds(List("concepts"), List("denied"), List("disputed"), List("cause"),
            (string?)(over?["disputedBy"] ?? node["disputedBy"]) ?? "defendant");
    }

    public static IReadOnlyList<string> Strings(JsonNode? node) => [.. (node?.AsArray() ?? []).Select(x => (string)x!)];

    /// <summary>The hashes of the files the sampler drew from, for a set's manifest.</summary>
    public JsonObject Hashes()
    {
        var o = new JsonObject();
        foreach (var path in new[] { Files.ClaimElements, Files.Scheme, Path.Combine(Files.CoreLogic, "slots.json") }
                     .Concat(Directory.GetFiles(Folder, "*.json").Order(StringComparer.Ordinal)))
        {
            o[Path.GetRelativePath(Files.Repository, path).Replace('\\', '/')] =
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        }

        return o;
    }

    /// <summary>The rows of SCHEME.md section 8: <c>| `claim` | `kind` | yes/no | … |</c>, between its heading and section 9's.</summary>
    public static IReadOnlyDictionary<string, bool> ReadCells(string scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        var start = scheme.IndexOf("## 8.", StringComparison.Ordinal);
        var end = scheme.IndexOf("## 9.", StringComparison.Ordinal);
        if (start < 0 || end < start)
        {
            throw new InvalidDataException("SCHEME.md has no section 8 before section 9.");
        }

        var cells = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (Match m in CellRow().Matches(scheme[start..end]))
        {
            cells[m.Groups[1].Value + "/" + m.Groups[2].Value] = m.Groups[3].Value == "yes";
        }

        return cells;
    }

    [GeneratedRegex(@"^\| `([a-z-]+)` \| `([a-z_]+)` \| (yes|no) \|", RegexOptions.Multiline)]
    private static partial Regex CellRow();
}

/// <summary>
/// A seeded random stream with the few draws the sampler needs: SplitMix64, written out here so that the same seed gives
/// the same scenarios on any runtime (the sampler's reproducibility does not rest on a library's generator).
/// </summary>
public sealed class Rng(int seed)
{
    private ulong state = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL;

    public double Next()
    {
        var z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 11) * (1.0 / (1UL << 53));
    }

    public int Between(int min, int maxInclusive) => min + (int)Math.Min(maxInclusive - min, Math.Floor(Next() * (maxInclusive - min + 1)));

    public bool Chance(double p) => Next() < p;

    public T Pick<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Count == 0 ? throw new ArgumentException("nothing to pick from") : items[Between(0, items.Count - 1)];
    }

    /// <summary>A value of a weighted table: (value, weight) pairs; the weights need not add up to one.</summary>
    public T Weighted<T>(IReadOnlyList<(T Value, double Weight)> table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var x = Next() * table.Sum(t => t.Weight);
        foreach (var (value, weight) in table)
        {
            if ((x -= weight) < 0)
            {
                return value;
            }
        }

        return table[^1].Value;
    }

    /// <summary>A round amount between the bounds: four significant digits at most, as a lawyer writes a sum.</summary>
    public long Amount(long min, long max)
    {
        var raw = min + (long)(Next() * (max - min));
        var step = (long)Math.Pow(10, Math.Max(0, Math.Floor(Math.Log10(Math.Max(raw, 1))) - 2));
        return Math.Max(step, raw / step * step);
    }

    /// <summary>A seed for a sub-stream: stable for (seed, parts), so that one draw does not shift another.</summary>
    public static int Derive(int seed, params int[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var h = (uint)seed ^ 0x9E3779B9u;
        foreach (var p in parts)
        {
            h = unchecked((h ^ (uint)p) * 0x01000193u);
            h ^= h >> 15;
        }

        return (int)(h & 0x7FFFFFFF);
    }

    public static string Day(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
