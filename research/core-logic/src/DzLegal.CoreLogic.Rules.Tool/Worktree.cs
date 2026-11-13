using System.Text.Json;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Rules.Checks;
using DzLegal.Drafting;

namespace DzLegal.CoreLogic.Rules.Tool;

/// <summary>
/// The files a check reads, taken from the worktree at run time rather than copied at build time: slots.json and the final
/// modules change while agents keep running the same built tool.
/// </summary>
internal sealed class Worktree
{
    private Worktree(string root) => Root = root;

    public string Root { get; }

    private string CoreLogic => Path.Combine(Root, "docs", "research", "core-logic");

    public string FinalRecords => Path.Combine(CoreLogic, "rules", "final");

    /// <summary>The worktree given with --root, or else the one the tool was built in (found above the tool's own folder).</summary>
    public static Worktree Find(string? root)
    {
        if (root is not null)
        {
            return File.Exists(Path.Combine(root, "docs", "research", "core-logic", "slots.json"))
                ? new Worktree(Path.GetFullPath(root))
                : throw new DirectoryNotFoundException($"{root} holds no docs/research/core-logic/slots.json.");
        }

        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "docs", "research", "core-logic", "slots.json")))
            {
                return new Worktree(folder.FullName);
            }
        }

        throw new DirectoryNotFoundException("No worktree above the tool's folder holds docs/research/core-logic/slots.json; give --root.");
    }

    public RecordCheckContext CheckContext(string? modules, string? store, bool quoted)
    {
        var moduleFolder = modules ?? FinalRecords;
        var cache = new Dictionary<string, RuleRecord?>(StringComparer.Ordinal);
        RuleRecord? Module(string id)
        {
            if (!cache.TryGetValue(id, out var record))
            {
                var path = Path.Combine(moduleFolder, $"{id}.json");
                cache[id] = record = File.Exists(path) ? Records.Load(path) : null;
            }

            return record;
        }

        return new RecordCheckContext
        {
            Schema = Parse(Path.Combine(CoreLogic, "rules", "rule-record.schema.json")),
            Slots = JsonSerializer.Deserialize<SlotCatalogue>(File.ReadAllText(Path.Combine(CoreLogic, "slots.json")), ContractJson.Options)!,
            ClaimIds = ClaimIds(),
            ClaimNeeds = ClaimNeeds.Parse(File.ReadAllText(Path.Combine(Root, "data", "law", "claim-elements.json"))),
            EventKinds = FactVocabulary.Events.ToHashSet(StringComparer.Ordinal),
            RequestKinds = FactVocabulary.Requests.ToHashSet(StringComparer.Ordinal),
            Module = Module,
            Store = store is null ? null : ArticleStoreIndex.Parse(File.ReadAllText(store)),
            Quoted = quoted,
        };
    }

    // The claim ids of the app's catalogue, read without the app's loader.
    private HashSet<string> ClaimIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(Path.Combine(Root, "data", "law", "claims"), "*.json"))
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

internal static class Records
{
    public static RuleRecord Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<RuleRecord>(File.ReadAllText(path), ContractJson.Options)
                ?? throw new FormatException($"{path} holds null.");
        }
        catch (JsonException exception)
        {
            throw new FormatException($"{path} does not load through the contract (run rules check): {exception.Message}", exception);
        }
    }
}
