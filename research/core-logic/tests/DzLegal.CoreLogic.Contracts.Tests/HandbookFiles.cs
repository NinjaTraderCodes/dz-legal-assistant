using System.Text.Json;

namespace DzLegal.CoreLogic.Contracts.Tests;

/// <summary>The handbook's files, as the build copied them beside the tests.</summary>
internal static class HandbookFiles
{
    /// <summary>Slot names every record may use without declaring them: they are part of <see cref="CaseSlots"/> itself.</summary>
    public static readonly IReadOnlySet<string> ReservedSlots = new HashSet<string> { "requests" };

    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "handbook");

    public static IReadOnlyList<string> ExamplePaths { get; } =
        [.. Directory.GetFiles(Path.Combine(Root, "examples"), "*.json").Order(StringComparer.Ordinal)];

    public static TheoryData<string> ExampleNames() => [.. ExamplePaths.Select(path => Path.GetFileName(path))];

    public static JsonElement Schema() => Parse(Path.Combine(Root, "rule-record.schema.json"));

    public static JsonElement ExampleJson(string name) => Parse(Path.Combine(Root, "examples", name));

    public static RuleRecord Example(string name) =>
        JsonSerializer.Deserialize<RuleRecord>(File.ReadAllText(Path.Combine(Root, "examples", name)), ContractJson.Options)!;

    public static SlotCatalogue Slots() =>
        JsonSerializer.Deserialize<SlotCatalogue>(File.ReadAllText(Path.Combine(Root, "slots.json")), ContractJson.Options)!;

    /// <summary>The claim ids of the app's catalogue (data/law/claims), read without the app's loader.</summary>
    public static IReadOnlySet<string> ClaimIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "app", "claims"), "*.json"))
        {
            foreach (var claim in Parse(path).GetProperty("claims").EnumerateArray())
            {
                ids.Add(claim.GetProperty("id").GetString()!);
            }
        }

        return ids;
    }

    private static JsonElement Parse(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        return document.RootElement.Clone();
    }
}
