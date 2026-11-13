using System.Text.Json;
using DzLegal.CoreLogic.Rules.Checks;
using DzLegal.Drafting;

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

    public static string ExampleText(string name) => File.ReadAllText(Path.Combine(Root, "examples", name));

    public static RuleRecord Example(string name) =>
        JsonSerializer.Deserialize<RuleRecord>(ExampleText(name), ContractJson.Options)!;

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

    /// <summary>
    /// What <c>rules check</c> checks a record against, built from the copies beside the tests. The example module stands in
    /// for rules/final, so that a claim record that is not yet flattened can be resolved.
    /// </summary>
    public static RecordCheckContext CheckContext(bool quoted = false, ArticleStoreIndex? store = null)
    {
        var module = ExamplePaths.Select(path => Example(Path.GetFileName(path))).Single(record => record.Kind == RecordKind.Module);
        return new RecordCheckContext
        {
            Schema = Schema(),
            Slots = Slots(),
            ClaimIds = ClaimIds(),
            ClaimNeeds = ClaimNeeds.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "app", "claim-elements.json"))),
            EventKinds = FactVocabulary.Events.ToHashSet(StringComparer.Ordinal),
            RequestKinds = FactVocabulary.Requests.ToHashSet(StringComparer.Ordinal),
            Module = id => id == module.Id ? module : null,
            Store = store,
            Quoted = quoted,
        };
    }

    private static JsonElement Parse(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        return document.RootElement.Clone();
    }
}
