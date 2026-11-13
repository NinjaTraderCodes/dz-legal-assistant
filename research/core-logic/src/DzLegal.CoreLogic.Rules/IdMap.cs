using System.Text.Json;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>
/// The item ids of derivations A and B and of the reconciled record of one unit, matched by content
/// (<c>reports/&lt;unit&gt;.idmap.json</c>, written by <c>compare-derivations</c>). The file is a JSON array of entries
/// <c>{ "kind", "a", "b", "reconciled" }</c>; an id is null when that record has no such item.
/// </summary>
public sealed class IdMap
{
    public const string A = "a";
    public const string B = "b";
    public const string Reconciled = "reconciled";

    private readonly IReadOnlyList<IReadOnlyDictionary<string, string?>> entries;

    private IdMap(IReadOnlyList<IReadOnlyDictionary<string, string?>> entries) => this.entries = entries;

    public static IdMap Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("An id map is a JSON array of { kind, a, b, reconciled }.");
        }

        var entries = new List<IReadOnlyDictionary<string, string?>>();
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            var ids = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var role in new[] { A, B, Reconciled })
            {
                ids[role] = entry.TryGetProperty(role, out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
            }

            entries.Add(ids);
        }

        return new IdMap(entries);
    }

    /// <summary>
    /// The column of the id map a record's ids are in, from its stage. A final record keeps the ids of the reconciled record
    /// it was graded from. Codex's derivation C is never turned into a record, so it has no column.
    /// </summary>
    public static string RoleOf(RuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return record.Stage switch
        {
            RecordStage.DerivationA => A,
            RecordStage.DerivationB => B,
            RecordStage.Reconciled or RecordStage.Final => Reconciled,
            _ => throw new ArgumentException($"A record at stage {record.Stage} has no column in an id map.", nameof(record)),
        };
    }

    /// <summary>The id in the other record of the item <paramref name="id"/>; null when the map gives it no counterpart.</summary>
    public string? Map(string id, string fromRole, string toRole)
    {
        if (fromRole == toRole)
        {
            return id;
        }

        var matches = entries.Where(entry => entry[fromRole] == id).Select(entry => entry[toRole]).Distinct(StringComparer.Ordinal).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}
