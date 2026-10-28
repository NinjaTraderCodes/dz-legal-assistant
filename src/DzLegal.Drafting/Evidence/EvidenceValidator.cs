using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Drafting.Evidence;

/// <summary>Checks location, shape and internal consistency. It deliberately does not prove sentence meaning.</summary>
public static partial class EvidenceValidator
{
    public static CheckedEvidence Check(CaseSnapshot snapshot, CaseEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(evidence);
        var issues = ImmutableArray.CreateBuilder<EvidenceIssue>();
        void Issue(string code, string id, string detail) => issues.Add(new(code, id, detail));
        if (evidence.Schema != CaseEvidence.CurrentSchema) { Issue("schema", "case", evidence.Schema); }
        if (evidence.SnapshotFingerprint != snapshot.Fingerprint) { Issue("stale", "case", "snapshot mismatch"); }
        if (evidence.Events.IsDefault || evidence.Requests.IsDefault || evidence.Relations.IsDefault || evidence.Documents.IsDefault || evidence.Coverage.IsDefault || evidence.Unresolved.IsDefault)
        {
            Issue("shape", "case", "null collection");
            return new(snapshot, evidence, issues.ToImmutable());
        }

        bool Source(EvidenceSpan span, string id)
        {
            if (snapshot.Read(span) is null) { Issue("source", id, "invalid original-text span"); return false; }
            if (span.FieldId == "court.section") { Issue("hint_as_fact", id, "court section is only a hint"); return false; }
            return true;
        }

        void Sources(ImmutableArray<EvidenceSpan> spans, string id)
        {
            if (spans.IsDefaultOrEmpty) { Issue("source", id, "missing evidence"); return; }
            foreach (var span in spans) { Source(span, id); }
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        void Id(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) { Issue("id", id, "missing or duplicate item id"); }
        }

        void Values(ImmutableArray<EvidenceValue> values, string id, bool request)
        {
            if (values.IsDefault) { Issue("shape", id, "null values"); return; }
            foreach (var value in values)
            {
                if (!Source(value.Source, id)) { continue; }
                if (value.Kind is not ("date" or "amount") || (request && value.Kind != "amount")) { Issue("value_kind", id, value.Kind); }
                if (string.IsNullOrWhiteSpace(value.Role)) { Issue("value_role", id, "missing ownership role"); }
                if (NormalizeValue(snapshot.Read(value.Source)!, value.Kind) is null) { Issue("value", id, "unreadable or ambiguous value"); }
            }

            foreach (var group in values.GroupBy(v => (v.Kind, v.Role)).Where(g => g.Select(v => snapshot.Read(v.Source)).Distinct().Count() > 1))
            {
                Issue("conflicting_value", id, group.Key.ToString());
            }
        }

        foreach (var relation in evidence.Relations)
        {
            if (!FactVocabulary.Relations.Contains(relation.Kind) || !FactVocabulary.Positions.Contains(relation.PlaintiffPosition)) { Issue("relation", "relation", "unknown code"); }
            if (!FactVocabulary.Actors.Contains(relation.AssertedBy)) { Issue("attribution", "relation", relation.AssertedBy); }
            Sources(relation.Sources, "relation");
        }

        if (evidence.Relations.Select(r => (r.Kind, r.PlaintiffPosition)).Distinct().Count() > 1) { Issue("conflicting_relation", "relation", "unresolved alternatives"); }
        foreach (var ev in evidence.Events)
        {
            Id(ev.Id);
            if (!FactVocabulary.Events.Contains(ev.Kind)) { Issue("event_kind", ev.Id, ev.Kind); }
            if (!FactPolarity.All.Contains(ev.Polarity)) { Issue("polarity", ev.Id, ev.Polarity); }
            if (!FactVocabulary.Actors.Contains(ev.AssertedBy)) { Issue("attribution", ev.Id, ev.AssertedBy); }
            Sources(ev.Sources, ev.Id);
            Values(ev.Values, ev.Id, request: false);
            if (ev.Participants.IsDefault) { Issue("shape", ev.Id, "null participants"); continue; }
            foreach (var participant in ev.Participants)
            {
                if (!FactVocabulary.Actors.Contains(participant.PartyId)) { Issue("party", ev.Id, participant.PartyId); }
                if (string.IsNullOrWhiteSpace(participant.Role)) { Issue("participant_role", ev.Id, "missing role"); }
                Sources(participant.Sources, ev.Id);
            }

            foreach (var group in ev.Participants.GroupBy(p => p.Role).Where(g => g.Select(p => p.PartyId).Distinct().Count() > 1))
            {
                Issue("conflicting_participant", ev.Id, group.Key);
            }
        }

        foreach (var request in evidence.Requests)
        {
            Id(request.Id);
            if (!FactVocabulary.Requests.Contains(request.Kind)) { Issue("request_kind", request.Id, request.Kind); }
            Sources(request.Sources, request.Id);
            Values(request.Values, request.Id, request: true);
        }

        foreach (var document in evidence.Documents)
        {
            Id(document.Id);
            Source(document.Description, document.Id);
            if (document.Description.FieldId != "documents.list") { Issue("document", document.Id, "not a listed document"); }
        }

        var sentenceIds = snapshot.Sentences.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var coverage in evidence.Coverage)
        {
            if (!sentenceIds.Contains(coverage.SentenceId) || coverage.Status is not ("read" or "unresolved")) { Issue("coverage", coverage.SentenceId, "invalid coverage entry"); }
        }

        if (evidence.Coverage.Select(c => c.SentenceId).Distinct().Count() != evidence.Coverage.Length) { Issue("coverage", "case", "duplicate coverage entry"); }
        foreach (var sentence in sentenceIds.Except(evidence.Coverage.Select(c => c.SentenceId))) { Issue("unread_passage", sentence, "not accounted for"); }
        return new(snapshot, evidence, issues.ToImmutable());
    }

    public static string? NormalizeValue(string text, string kind)
    {
        ArgumentNullException.ThrowIfNull(text);
        var ascii = ArabicScript.ToAsciiDigits(text).Trim();
        if (kind == "date")
        {
            if (Year().IsMatch(ascii)) { return ascii; }
            var match = Date().Match(ascii);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var day) || !int.TryParse(match.Groups[2].Value, out var month) || !int.TryParse(match.Groups[3].Value, out var year)
                || year is < 1900 or > 2099 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) { return null; }
            return new DateOnly(year, month, day).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        // A full date or a bare year must not silently become a monetary amount.
        if (kind != "amount" || Date().IsMatch(ascii) || Year().IsMatch(ascii) || !Amount().IsMatch(ascii)) { return null; }
        var amountMatch = Amount().Match(ascii);
        if (!long.TryParse(amountMatch.Groups[1].Value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(",", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal), NumberStyles.None, CultureInfo.InvariantCulture, out var number)) { return null; }
        var unit = amountMatch.Groups[2].Value;
        try { return checked(number * (unit == "مليون" ? 1_000_000 : unit.Length > 0 ? 1000 : 1)).ToString(CultureInfo.InvariantCulture); }
        catch (OverflowException) { return null; }
    }

    [GeneratedRegex(@"^(19|20)\d{2}$")]
    private static partial Regex Year();
    [GeneratedRegex(@"^(\d{1,2})\s*[/.-]\s*(\d{1,2})\s*[/.-]\s*(\d{4})$")]
    private static partial Regex Date();
    [GeneratedRegex(@"^(\d{1,3}(?:[., ]\d{3})+|\d+)(?:\s*(ألف|الف|مليون))?(?:\s*(?:دج|دينار جزائري|دينار|DA|DZD))?$", RegexOptions.IgnoreCase)]
    private static partial Regex Amount();
}
