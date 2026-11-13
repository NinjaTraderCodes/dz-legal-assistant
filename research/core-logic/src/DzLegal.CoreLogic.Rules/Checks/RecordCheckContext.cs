using System.Text.Json;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Checks;

/// <summary>What the app's claim-elements.json says a claim type needs: the events of its elements and its request kinds.</summary>
public sealed record ClaimNeeds(IReadOnlyList<string> Events, IReadOnlyList<string> Requests)
{
    /// <summary>Reads <c>data/law/claim-elements.json</c> without the app's loader.</summary>
    public static IReadOnlyDictionary<string, ClaimNeeds> Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var needs = new Dictionary<string, ClaimNeeds>(StringComparer.Ordinal);
        foreach (var claim in document.RootElement.GetProperty("claims").EnumerateArray())
        {
            needs[claim.GetProperty("claim").GetString()!] = new ClaimNeeds(
                [.. claim.GetProperty("elements").EnumerateArray().Select(element => element.GetProperty("event").GetString()!)],
                [.. claim.GetProperty("requests").EnumerateArray().Select(request => request.GetProperty("kind").GetString()!)]);
        }

        return needs;
    }
}

/// <summary>
/// Everything a record is checked against. The tool reads these from the worktree; the tests from the copies beside them.
/// The vocabularies are passed in, not read here, so that the evaluator's project depends on the contract only.
/// </summary>
public sealed record RecordCheckContext
{
    /// <summary>rules/rule-record.schema.json.</summary>
    public required JsonElement Schema { get; init; }

    /// <summary>slots.json.</summary>
    public required SlotCatalogue Slots { get; init; }

    /// <summary>The claim ids of data/law/claims.</summary>
    public required IReadOnlySet<string> ClaimIds { get; init; }

    /// <summary>data/law/claim-elements.json, by claim id.</summary>
    public required IReadOnlyDictionary<string, ClaimNeeds> ClaimNeeds { get; init; }

    /// <summary>FactVocabulary.Events.</summary>
    public required IReadOnlySet<string> EventKinds { get; init; }

    /// <summary>FactVocabulary.Requests.</summary>
    public required IReadOnlySet<string> RequestKinds { get; init; }

    /// <summary>
    /// The final record of a module (rules/final/&lt;module&gt;.json), or null when there is none: used to resolve a reference
    /// <c>&lt;module&gt;.&lt;id&gt;</c> of a claim record that is not yet flattened.
    /// </summary>
    public Func<string, RuleRecord?> Module { get; init; } = _ => null;

    /// <summary>The article store, for <c>--store</c>; null to skip that check.</summary>
    public ArticleStoreIndex? Store { get; init; }

    /// <summary>True for a quoted copy (<c>--quoted</c>): the script fields quote and source are then accepted before the final stage.</summary>
    public bool Quoted { get; init; }
}
