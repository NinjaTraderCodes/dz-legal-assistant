using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using DzLegal.Drafting.Evidence;
using DzLegal.Law;

namespace DzLegal.Harness;

/// <summary>A known fictitious fixture, not an extractor and not a source of reviewed training labels.</summary>
public static partial class EvidenceDemo
{
    private static readonly string[] ReviewQuestions = ["Check participant roles and both attributed notice assertions.", "Check each value's event and role.", "Review the rule's requirements, exceptions and missing conditions.", "Establish applicable source versions and transitions; current bundled texts may be incomplete or outdated.", "Review which quoted provisions are applicable, excluded or merely candidates."];
    public static void Create(string inputPath, string lawRoot, string output)
    {
        if (Directory.Exists(output)) { throw new IOException("Choose a fresh demo output folder."); }
        var input = JsonSerializer.Deserialize<EvidenceCaseInput>(File.ReadAllText(inputPath), EvidenceJson.Options) ?? throw new InvalidDataException("Missing fixture.");
        if (input.CaseId != "fictional-civil-lease-contrast") { throw new InvalidDataException("The demo builder only handles its named fictitious fixture."); }
        var snapshot = CaseSnapshot.Create(input.CaseId, input.Revision, input.Inputs);
        var sentences = snapshot.Sentences.Where(s => s.FieldId == "notes.facts").ToArray();
        if (sentences.Length != 6) { throw new InvalidDataException("Fixture shape changed: review its annotations."); }
        static EvidenceSpan Span(SnapshotSentence sentence) => new(sentence.FieldId, sentence.Start, sentence.Text.Length);
        static ImmutableArray<EvidenceValue> Value(SnapshotSentence sentence, string kind, string role)
        {
            var match = (kind == "date" ? Date() : Amount()).Match(sentence.Text);
            return match.Success ? [new(kind, role, new(sentence.FieldId, sentence.Start + match.Index, match.Length))] : [];
        }

        EvidenceEvent Event(int index, string kind, string assertedBy, string polarity, string? actor, string? dateRole, string? dispute = null) => new(
            "event" + index, kind, assertedBy, polarity, dispute,
            actor is null ? [] : [new("actor", actor, [Span(sentences[index])])],
            dateRole is null ? [] : Value(sentences[index], "date", dateRole), [Span(sentences[index])]);
        var rent = Event(2, "lease_started", "plaintiff", "affirmed", null, "commencement_date");
        rent = rent with { Values = [.. rent.Values, .. Value(sentences[2], "amount", "monthly_rent")] };
        var requestSentence = snapshot.Sentences.Single(s => s.FieldId == "notes.requests");
        var documentSentence = snapshot.Sentences.Single(s => s.FieldId == "documents.list");
        var evidence = new CaseEvidence
        {
            SnapshotFingerprint = snapshot.Fingerprint,
            Relations = [new("lease", "lessor", "plaintiff", [Span(sentences[0])])],
            Events = [Event(1, "contract_concluded", "plaintiff", "affirmed", null, "signature_date"), rent,
                Event(3, "rent_unpaid", "plaintiff", "affirmed", "defendant", null),
                Event(4, "notice_served", "plaintiff", "affirmed", "plaintiff", "service_date", "notice1"),
                Event(5, "notice_served", "defendant", "denied", "plaintiff", null, "notice1")],
            Requests = [new("request1", "payment", Value(requestSentence, "amount", "requested_total"), [Span(requestSentence)])],
            Documents = [new("document1", Span(documentSentence))],
            Coverage = [.. snapshot.Sentences.Select(s => new EvidenceCoverage(s.Id, "read"))],
            Unresolved = [],
        };
        var validation = EvidenceValidator.Check(snapshot, evidence);
        if (!validation.StructurallyValid) { throw new InvalidDataException(string.Join(", ", validation.Issues.Select(i => i.Code))); }
        var knowledge = LegalKnowledgeLoader.Load(lawRoot);
        var claim = knowledge.Claims.Single(c => c.Id == "unpaid-rent");
        var authorities = knowledge.Resolve(claim).Where(a => a.Article is not null).Select(a => new AuthorityClause(
            a.Reference.Code, a.Reference.Number, knowledge.Library.FindCode(a.Reference.Code)?.ConsolidatedAsOf ?? "unknown",
            a.Article!.Text, RunManifest.FolderHash(Path.Combine(lawRoot, "codes", a.Reference.Code + ".json")), "unreviewed")).ToImmutableArray();
        EvidenceRule[] rules = [new("civil-residential-lease.payment.candidate",
            ["lease.plaintiff_lessor", "lease.contract_concluded", "lease.rent_unpaid", "lease.payment_requested", "lease.form_checked", "lease.temporal_regime_checked"],
            ["lease.payment_satisfied"], authorities, "unreviewed")];
        Directory.CreateDirectory(output);
        void Write(string name, object value) => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(value, EvidenceJson.Options));
        Write("input.json", input);
        Write("evidence.json", evidence);
        Write("rules.for-review.json", rules);
        Write("snapshot.json", snapshot);
        Write("schema.json", EvidenceJson.Schema(snapshot));
        var references = new EvidenceReferences(snapshot);
        Write("reference-input.json", references.ModelInput);
        Write("reference-target.json", references.Encode(evidence));
        Write("reference-schema.json", references.Schema());
        Write("authority-candidates.json", EvidenceAuthorityCandidates.Discover(knowledge, snapshot, [claim], authorities));
        Write("review-package.json", new
        {
            status = "unreviewed fictional engineering fixture; not qualified legal advice or sealed evaluation",
            inputFingerprint = snapshot.Fingerprint,
            questions = ReviewQuestions,
            authorities,
            proposedFindings = Array.Empty<SemanticFinding>(),
        });
    }

    [GeneratedRegex(@"\d{2}/\d{2}/\d{4}")]
    private static partial Regex Date();
    [GeneratedRegex(@"\d+ دج")]
    private static partial Regex Amount();
}
