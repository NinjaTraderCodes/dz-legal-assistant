using System.Text.Json;

namespace DzLegal.CoreLogic.Rules.Checks;

/// <summary>One wording of one article in the article store (specs/article-store.md, "The store's entry"), as far as the checks need it.</summary>
public sealed record StoreEntry(string Code, string Article, string? Version, bool InForce, bool Repealed, int ParagraphCount);

/// <summary>
/// Lookups in the article store file (<c>{ "schemaVersion", "inputs", "entries": [...] }</c>, built by scripts/store.py), the
/// same way <c>Store.get</c> does there: a version given selects that wording; without one, the wording in force, and when
/// none is in force, a repealed one (so that the cite is reported as repealed, not as missing).
/// </summary>
public sealed class ArticleStoreIndex
{
    private readonly Dictionary<(string Code, string Article, string Version), StoreEntry> byVersion = [];
    private readonly Dictionary<(string Code, string Article), StoreEntry> inForce = [];
    private readonly Dictionary<(string Code, string Article), StoreEntry> repealed = [];

    private ArticleStoreIndex()
    {
    }

    public static ArticleStoreIndex Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var index = new ArticleStoreIndex();
        foreach (var entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            var version = entry.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var paragraphs = entry.TryGetProperty("paragraphs", out var p) && p.ValueKind == JsonValueKind.Array ? p.GetArrayLength() : 0;
            var item = new StoreEntry(
                entry.GetProperty("code").GetString()!,
                entry.GetProperty("article").GetString()!,
                version,
                entry.GetProperty("inForce").GetBoolean(),
                entry.GetProperty("repealed").GetBoolean(),
                paragraphs);

            if (version is not null)
            {
                index.byVersion[(item.Code, item.Article, version)] = item;
            }

            if (item.InForce)
            {
                index.inForce[(item.Code, item.Article)] = item;
            }

            if (item.Repealed)
            {
                index.repealed.TryAdd((item.Code, item.Article), item);
            }
        }

        return index;
    }

    public StoreEntry? Get(string code, string article, string? version)
    {
        if (version is not null)
        {
            return byVersion.GetValueOrDefault((code, article, version));
        }

        return inForce.GetValueOrDefault((code, article)) ?? repealed.GetValueOrDefault((code, article));
    }
}
