using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DzLegal.Drafting.Evidence;

public static class EvidenceJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static CaseEvidence Parse(string json)
    {
        ValidateSyntax(json);
        return JsonSerializer.Deserialize<CaseEvidence>(json, Options) ?? throw new JsonException("Missing evidence object.");
    }

    public static void ValidateSyntax(string json)
    {
        using var document = JsonDocument.Parse(json);
        ValidateMembers(document.RootElement);
    }

    private static void ValidateMembers(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) { throw new JsonException("Duplicate evidence property."); }
                if (property.Name == "disputeGroup" && property.Value.ValueKind == JsonValueKind.Null) { continue; }
                ValidateMembers(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in element.EnumerateArray()) { ValidateMembers(value); }
        }
        else if (element.ValueKind == JsonValueKind.Null) { throw new JsonException("Null evidence item."); }
    }

    public static JsonElement Schema(CaseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        static JsonObject Text() => new() { ["type"] = "string" };
        static JsonObject Choice(IEnumerable<string> choices) => new() { ["type"] = "string", ["enum"] = new JsonArray([.. choices.Select(c => (JsonNode?)JsonValue.Create(c))]) };
        static JsonObject Array(JsonObject items) => new() { ["type"] = "array", ["items"] = items };
        static JsonObject Object(params (string Name, JsonNode Value)[] fields) => new()
        {
            ["type"] = "object", ["additionalProperties"] = false,
            ["properties"] = new JsonObject(fields.Select(f => KeyValuePair.Create<string, JsonNode?>(f.Name, f.Value))),
            ["required"] = new JsonArray([.. fields.Select(f => (JsonNode?)JsonValue.Create(f.Name))]),
        };
        JsonObject Span() => Object(("fieldId", Choice(snapshot.Fields.Keys.Where(k => k != "court.section"))), ("start", new JsonObject { ["type"] = "integer", ["minimum"] = 0 }), ("length", new JsonObject { ["type"] = "integer", ["minimum"] = 1 }));
        JsonObject Spans() => Array(Span());
        JsonObject Values() => Array(Object(("kind", Choice(["date", "amount"])), ("role", Text()), ("source", Span())));
        var schema = Object(
            ("schema", new JsonObject { ["const"] = CaseEvidence.CurrentSchema }),
            ("snapshotFingerprint", new JsonObject { ["const"] = snapshot.Fingerprint }),
            ("relations", Array(Object(("kind", Choice(FactVocabulary.Relations)), ("plaintiffPosition", Choice(FactVocabulary.Positions)), ("assertedBy", Choice(FactVocabulary.Actors)), ("sources", Spans())))),
            ("events", Array(Object(("id", Text()), ("kind", Choice(FactVocabulary.Events)), ("assertedBy", Choice(FactVocabulary.Actors)), ("polarity", Choice(FactPolarity.All)),
                ("disputeGroup", new JsonObject { ["type"] = new JsonArray("string", "null") }),
                ("participants", Array(Object(("role", Text()), ("partyId", Choice(FactVocabulary.Actors)), ("sources", Spans())))), ("values", Values()), ("sources", Spans())))),
            ("requests", Array(Object(("id", Text()), ("kind", Choice(FactVocabulary.Requests)), ("values", Values()), ("sources", Spans())))),
            ("documents", Array(Object(("id", Text()), ("description", Span())))),
            ("coverage", Array(Object(("sentenceId", Choice(snapshot.Sentences.Select(s => s.Id))), ("status", Choice(["read", "unresolved"]))))),
            ("unresolved", Array(Text())));
        return JsonSerializer.SerializeToElement(schema);
    }
}
