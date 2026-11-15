using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Scoring;

/// <summary>A quote of the gold, with the start and length the notes tool wrote by script.</summary>
public sealed record GoldSpan(NoteField Field, int Start, int Length);

public sealed record GoldValue(ValueKind Kind, string? Normalized, GoldSpan? At, IReadOnlyList<string> Roles, bool CountedApart);

public sealed record GoldEvent(string Claim, string Kind, string Polarity, string? Actor, string? DisputedBy, IReadOnlyList<GoldSpan> Evidence);

public sealed record GoldSlot(string Slot, string Value, IReadOnlyList<GoldSpan> Evidence);

public sealed record GoldRequest(string Kind, string? Amount, IReadOnlyList<GoldSpan> Evidence);

public sealed record GoldRole(string Claim, string Relation, string Position);

/// <summary>
/// One gold note (notes/note.schema.json): its id, set, writer family, text, and the agreed labels. Polarities
/// <c>blanked</c> and <c>unclear</c> are never scored; a slot is scored only when the gold holds it (the notes tool writes
/// a slot only for the claim types it is asked for: specs/scorer.md).
/// </summary>
public sealed record GoldNote(
    string Id,
    string Set,
    string Writer,
    IReadOnlyDictionary<NoteField, string> Text,
    IReadOnlyList<string> Claims,
    IReadOnlyList<GoldSpan> ClaimEvidence,
    IReadOnlyList<GoldRole> Roles,
    IReadOnlyList<GoldEvent> Events,
    IReadOnlyList<GoldSlot> Slots,
    IReadOnlyList<GoldRequest> Requests,
    IReadOnlyList<GoldValue> Values)
{
    public bool IsOutside => Claims.Count == 1 && Claims[0] == ClaimGuess.Outside;

    public static GoldNote Parse(string line)
    {
        var node = JsonNode.Parse(line) as JsonObject ?? throw new ScoreException("G01", "a gold line that is not a note");
        var gold = node["gold"] as JsonObject ?? throw new ScoreException("G02", "a gold note without its labels");
        var text = new Dictionary<NoteField, string>();
        if (node["text"] is JsonObject fields)
        {
            foreach (var (name, value) in fields)
            {
                if (Enum.TryParse<NoteField>(name, ignoreCase: true, out var field) && value is not null)
                {
                    text[field] = value.GetValue<string>();
                }
            }
        }

        return new GoldNote(
            Str(node, "id") ?? throw new ScoreException("G03", "a gold note without its id"),
            Str(node, "set") ?? "",
            node["provenance"]?["writer"]?["family"]?.GetValue<string>() ?? "",
            text,
            Strings(gold["claims"]),
            Spans(gold["claimEvidence"]),
            [.. Items(gold["roles"]).Select(r => new GoldRole(Str(r, "claim")!, Str(r, "relation")!, Str(r, "position")!))],
            [.. Items(gold["events"]).Select(e => new GoldEvent(Str(e, "claim")!, Str(e, "kind")!, Str(e, "polarity")!,
                Str(e, "actor"), Str(e, "disputedBy"), Spans(e["evidence"])))],
            [.. Items(gold["slots"]).Select(s => new GoldSlot(Str(s, "slot")!, Str(s, "value")!, Spans(s["evidence"])))],
            [.. Items(gold["requests"]).Select(r => new GoldRequest(Str(r, "kind")!, r["amount"]?["normalized"]?.GetValue<string>(), Spans(r["evidence"])))],
            [.. Items(gold["values"]).Select(Value)]);
    }

    private static GoldValue Value(JsonObject v)
    {
        var kind = Str(v, "kind") == "amount" ? ValueKind.Amount : ValueKind.Date;
        var apart = v["relative"]?.GetValue<bool>() == true || Str(v, "calendar") == "hijri" || v["currency"] is not null;
        return new GoldValue(kind, Str(v, "normalized"), Span(v["at"] as JsonObject), Strings(v["roles"]), apart);
    }

    private static IEnumerable<JsonObject> Items(JsonNode? array) =>
        array is JsonArray list ? list.OfType<JsonObject>() : [];

    private static List<string> Strings(JsonNode? array) =>
        array is JsonArray list ? [.. list.Select(item => item!.GetValue<string>())] : [];

    private static List<GoldSpan> Spans(JsonNode? array) =>
        [.. Items(array).Select(Span).OfType<GoldSpan>()];

    private static GoldSpan? Span(JsonObject? quote)
    {
        if (quote is null || quote["start"] is null || quote["length"] is null)
        {
            return null;
        }

        var field = Enum.Parse<NoteField>(Str(quote, "field")!, ignoreCase: true);
        return new GoldSpan(field, quote["start"]!.GetValue<int>(), quote["length"]!.GetValue<int>());
    }

    private static string? Str(JsonObject node, string name) => node[name]?.GetValue<string>();
}

/// <summary>Every failure of the scorer is a fixed code; a message never holds a note's id or text.</summary>
public sealed class ScoreException(string code, string message) : Exception($"{code}: {message}")
{
    public string Code { get; } = code;
}
