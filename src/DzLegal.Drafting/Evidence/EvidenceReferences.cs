using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Drafting.Evidence;

public sealed record EvidenceSourceReference(string Id, string FieldId, string Text, string Kind, EvidenceSpan Span);
public sealed record ReferencedParticipant(string Role, string PartyId, ImmutableArray<string> Sources);
public sealed record ReferencedValue(string Kind, string Role, string SourceId);
public sealed record ReferencedEvent(string Id, string Kind, string AssertedBy, string Polarity, string? DisputeGroup, ImmutableArray<ReferencedParticipant> Participants, ImmutableArray<ReferencedValue> Values, ImmutableArray<string> Sources);
public sealed record ReferencedRequest(string Id, string Kind, ImmutableArray<ReferencedValue> Values, ImmutableArray<string> Sources);
public sealed record ReferencedRelation(string Kind, string PlaintiffPosition, string AssertedBy, ImmutableArray<string> Sources);
public sealed record ReferencedDocument(string Id, string Description);
public sealed record ReferencedEvidence(ImmutableArray<ReferencedRelation> Relations, ImmutableArray<ReferencedEvent> Events, ImmutableArray<ReferencedRequest> Requests, ImmutableArray<ReferencedDocument> Documents, ImmutableArray<EvidenceCoverage> Coverage, ImmutableArray<string> Unresolved);

/// <summary>Generative wire format: select enumerated references; the host supplies offsets, values and revision identity.</summary>
public sealed partial class EvidenceReferences
{
    private readonly CaseSnapshot _snapshot;
    public ImmutableArray<EvidenceSourceReference> Sources { get; }

    public EvidenceReferences(CaseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot;
        var sources = ImmutableArray.CreateBuilder<EvidenceSourceReference>();
        var valueIndex = 0;
        foreach (var sentence in snapshot.Sentences)
        {
            sources.Add(new(sentence.Id, sentence.FieldId, sentence.Text, "sentence", new(sentence.FieldId, sentence.Start, sentence.Text.Length)));
            if (sentence.FieldId == "court.section") { continue; }
            var ascii = ArabicScript.ToAsciiDigits(sentence.Text);
            var dateMatches = DateCandidate().Matches(ascii).ToArray();
            var fullDates = dateMatches.Where(d => d.Value.Contains('/', StringComparison.Ordinal) || d.Value.Contains('.', StringComparison.Ordinal) || d.Value.Contains('-', StringComparison.Ordinal)).ToArray();
            var amounts = AmountCandidate().Matches(ascii).Where(m => !fullDates.Any(d => m.Index < d.Index + d.Length && m.Index + m.Length > d.Index)
                && EvidenceValidator.NormalizeValue(m.Value, "amount") is not null).ToArray();
            var dates = dateMatches.Where(d => !amounts.Any(m => m.Index < d.Index + d.Length && m.Index + m.Length > d.Index)).ToArray();
            foreach (var match in dates.Concat(amounts))
            {
                var span = new EvidenceSpan(sentence.FieldId, sentence.Start + match.Index, match.Length);
                var raw = snapshot.Read(span)!;
                var kind = EvidenceValidator.NormalizeValue(raw, "date") is not null ? "date" : EvidenceValidator.NormalizeValue(raw, "amount") is not null ? "amount" : null;
                if (kind is not null) { sources.Add(new("v" + ++valueIndex, sentence.FieldId, raw, kind, span)); }
            }
        }

        Sources = sources.ToImmutable();
    }

    public object ModelInput => new { _snapshot.CaseId, _snapshot.Revision, _snapshot.Fingerprint, _snapshot.Parties, sources = Sources.Select(s => new { s.Id, s.FieldId, s.Text, s.Kind }) };

    public CaseEvidence Decode(string json)
    {
        // First validate null/duplicate members with the shared parser's syntax policy, then deserialize this wire shape.
        EvidenceJson.ValidateSyntax(json);
        var wire = JsonSerializer.Deserialize<ReferencedEvidence>(json, EvidenceJson.Options) ?? throw new JsonException("Missing referenced evidence.");
        EvidenceSpan Sentence(string id) => Sources.FirstOrDefault(s => s.Id == id && s.Kind == "sentence" && s.FieldId != "court.section")?.Span ?? throw new JsonException("Expected a factual sentence reference.");
        EvidenceSpan Document(string id) => Sources.FirstOrDefault(s => s.Id == id && s.Kind == "sentence" && s.FieldId == "documents.list")?.Span ?? throw new JsonException("Expected a listed-document reference.");
        ImmutableArray<EvidenceSpan> Spans(ImmutableArray<string> ids) => [.. ids.Select(Sentence)];
        ImmutableArray<EvidenceValue> Values(ImmutableArray<ReferencedValue> values) => [.. values.Select(v => new EvidenceValue(v.Kind, v.Role,
            Sources.FirstOrDefault(s => s.Id == v.SourceId && s.Kind == v.Kind && s.Kind != "sentence")?.Span ?? throw new JsonException("Value reference has the wrong kind.")))];
        return new CaseEvidence
        {
            SnapshotFingerprint = _snapshot.Fingerprint,
            Relations = [.. wire.Relations.Select(r => new EvidenceRelation(r.Kind, r.PlaintiffPosition, r.AssertedBy, Spans(r.Sources)))],
            Events = [.. wire.Events.Select(e => new EvidenceEvent(e.Id, e.Kind, e.AssertedBy, e.Polarity, e.DisputeGroup,
                [.. e.Participants.Select(p => new EvidenceParticipant(p.Role, p.PartyId, Spans(p.Sources)))], Values(e.Values), Spans(e.Sources)))],
            Requests = [.. wire.Requests.Select(r => new EvidenceRequest(r.Id, r.Kind, Values(r.Values), Spans(r.Sources)))],
            Documents = [.. wire.Documents.Select(d => new EvidenceDocument(d.Id, Document(d.Description)))],
            Coverage = wire.Coverage, Unresolved = wire.Unresolved,
        };
    }

    public ReferencedEvidence Encode(CaseEvidence evidence)
    {
        if (!EvidenceValidator.Check(_snapshot, evidence).StructurallyValid) { throw new InvalidDataException("Only structurally checked evidence can become a reference target."); }
        string Reference(EvidenceSpan span) => Sources.FirstOrDefault(s => s.Span == span)?.Id
            ?? Sources.FirstOrDefault(s => s.Kind == "sentence" && s.FieldId == span.FieldId && s.Span.Start <= span.Start && s.Span.Start + s.Span.Length >= span.Start + span.Length)?.Id
            ?? throw new InvalidDataException("Evidence span is not represented in the reference catalogue.");
        ImmutableArray<string> Refs(ImmutableArray<EvidenceSpan> spans) => [.. spans.Select(Reference).Distinct(StringComparer.Ordinal)];
        ImmutableArray<ReferencedValue> Values(ImmutableArray<EvidenceValue> values) => [.. values.Select(v => new ReferencedValue(v.Kind, v.Role,
            Sources.FirstOrDefault(s => s.Kind == v.Kind && s.Span == v.Source)?.Id ?? throw new InvalidDataException("Value needs an exact candidate reference.")))];
        return new([.. evidence.Relations.Select(r => new ReferencedRelation(r.Kind, r.PlaintiffPosition, r.AssertedBy, Refs(r.Sources)))],
            [.. evidence.Events.Select(e => new ReferencedEvent(e.Id, e.Kind, e.AssertedBy, e.Polarity, e.DisputeGroup, [.. e.Participants.Select(p => new ReferencedParticipant(p.Role, p.PartyId, Refs(p.Sources)))], Values(e.Values), Refs(e.Sources)))],
            [.. evidence.Requests.Select(r => new ReferencedRequest(r.Id, r.Kind, Values(r.Values), Refs(r.Sources)))],
            [.. evidence.Documents.Select(d => new ReferencedDocument(d.Id, Reference(d.Description)))], evidence.Coverage, evidence.Unresolved);
    }

    public JsonElement Schema()
    {
        static JsonObject Choice(IEnumerable<string> values) => new() { ["type"] = "string", ["enum"] = new JsonArray([.. values.Select(v => (JsonNode?)JsonValue.Create(v))]) };
        static JsonObject Text() => new() { ["type"] = "string" };
        static JsonObject Array(JsonObject items) => new() { ["type"] = "array", ["items"] = items };
        static JsonObject Object(params (string Name, JsonNode Value)[] fields) => new()
        {
            ["type"] = "object", ["additionalProperties"] = false,
            ["properties"] = new JsonObject(fields.Select(f => KeyValuePair.Create<string, JsonNode?>(f.Name, f.Value))),
            ["required"] = new JsonArray([.. fields.Select(f => (JsonNode?)JsonValue.Create(f.Name))]),
        };
        static JsonObject EmptyArray() => new() { ["type"] = "array", ["maxItems"] = 0 };
        var sentences = Sources.Where(s => s.Kind == "sentence" && s.FieldId != "court.section").Select(s => s.Id).ToArray();
        var values = Sources.Where(s => s.Kind != "sentence").Select(s => s.Id).ToArray();
        var documents = Sources.Where(s => s.Kind == "sentence" && s.FieldId == "documents.list").Select(s => s.Id).ToArray();
        JsonObject Refs() => sentences.Length == 0 ? EmptyArray() : Array(Choice(sentences));
        JsonObject Values() => values.Length == 0 ? EmptyArray() : Array(Object(("kind", Choice(["date", "amount"])), ("role", Text()), ("sourceId", Choice(values))));
        var schema = Object(
            ("relations", Array(Object(("kind", Choice(FactVocabulary.Relations)), ("plaintiffPosition", Choice(FactVocabulary.Positions)), ("assertedBy", Choice(FactVocabulary.Actors)), ("sources", Refs())))),
            ("events", Array(Object(("id", Text()), ("kind", Choice(FactVocabulary.Events)), ("assertedBy", Choice(FactVocabulary.Actors)), ("polarity", Choice(FactPolarity.All)), ("disputeGroup", new JsonObject { ["type"] = new JsonArray("string", "null") }),
                ("participants", Array(Object(("role", Text()), ("partyId", Choice(FactVocabulary.Actors)), ("sources", Refs())))), ("values", Values()), ("sources", Refs())))),
            ("requests", Array(Object(("id", Text()), ("kind", Choice(FactVocabulary.Requests)), ("values", Values()), ("sources", Refs())))),
            ("documents", documents.Length == 0 ? EmptyArray() : Array(Object(("id", Text()), ("description", Choice(documents))))),
            ("coverage", Array(Object(("sentenceId", Choice(_snapshot.Sentences.Select(s => s.Id))), ("status", Choice(["read", "unresolved"]))))), ("unresolved", Array(Text())));
        return JsonSerializer.SerializeToElement(schema);
    }

    [GeneratedRegex(@"\b\d{1,2}[/.-]\d{1,2}[/.-]\d{4}\b|\b(?:19|20)\d{2}\b")]
    private static partial Regex DateCandidate();
    [GeneratedRegex(@"\d+(?:[., ]\d{3})*(?:\s*(?:ألف|الف|مليون))?(?:\s*(?:دج|دينار جزائري|دينار|DA|DZD))?")]
    private static partial Regex AmountCandidate();
}
