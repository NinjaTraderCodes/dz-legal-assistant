using System.Collections.Immutable;
using System.Globalization;
using DzLegal.Drafting.Evidence;

namespace DzLegal.Harness;

public sealed record EvidenceEvaluation(string Version, ImmutableArray<FactError> Errors, bool Complete, bool ExhaustiveGold)
{
    public bool HasCritical => Errors.Any(e => e.Critical);
}

/// <summary>Scores the richer contract using assertion identity, never gold values to pick a convenient event.</summary>
public static class EvidenceEvaluator
{
    public const string Version = "evidence.score/1";

    public static EvidenceEvaluation Score(CaseSnapshot snapshot, CaseEvidence expected, CaseEvidence predicted, bool exhaustiveGold)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(predicted);
        var goldCheck = EvidenceValidator.Check(snapshot, expected);
        if (!goldCheck.CompleteCoverage) { throw new InvalidDataException("The answer key must have structurally valid, complete evidence coverage."); }
        var actualCheck = EvidenceValidator.Check(snapshot, predicted);
        var errors = ImmutableArray.CreateBuilder<FactError>();
        void Error(string code, string detail, bool critical = true) => errors.Add(new(code, detail, critical));
        foreach (var issue in actualCheck.Issues) { Error(issue.Code, issue.ItemId); }
        if (!actualCheck.StructurallyValid) { return new(Version, errors.ToImmutable(), false, exhaustiveGold); }

        static string Sources(IEnumerable<EvidenceSpan> spans) => string.Join(';', spans.OrderBy(s => s.FieldId, StringComparer.Ordinal).ThenBy(s => s.Start).ThenBy(s => s.Length)
            .Select(s => string.Create(CultureInfo.InvariantCulture, $"{s.FieldId}:{s.Start}:{s.Length}")));
        static string EventIdentity(EvidenceEvent e) => e.Kind + "|" + e.AssertedBy + "|" + Sources(e.Sources);
        string Values(ImmutableArray<EvidenceValue> values) => string.Join(';', values.Select(v => v.Kind + ":" + v.Role + ":" + EvidenceValidator.NormalizeValue(snapshot.Read(v.Source)!, v.Kind) + ":" + Sources([v.Source])).Order(StringComparer.Ordinal));
        static string Participants(ImmutableArray<EvidenceParticipant> participants) => string.Join(';', participants.Select(p => p.Role + ":" + p.PartyId).Order(StringComparer.Ordinal));

        var used = new HashSet<int>();
        foreach (var gold in expected.Events)
        {
            var candidates = Enumerable.Range(0, predicted.Events.Length).Where(i => !used.Contains(i) && EventIdentity(predicted.Events[i]) == EventIdentity(gold)).ToArray();
            if (candidates.Length == 0) { Error("missing assertion", gold.Id); continue; }
            var first = candidates[0];
            used.Add(first);
            var actual = predicted.Events[first];
            if (actual.Polarity != gold.Polarity) { Error("polarity", gold.Id); }
            if (Participants(actual.Participants) != Participants(gold.Participants)) { Error("participants", gold.Id); }
            if (Values(actual.Values) != Values(gold.Values)) { Error("value binding or ownership", gold.Id); }
            if (actual.DisputeGroup != gold.DisputeGroup && gold.DisputeGroup is not null) { Error("dispute grouping", gold.Id); }
            foreach (var duplicate in candidates.Skip(1))
            {
                used.Add(duplicate);
                var other = predicted.Events[duplicate];
                var contradictory = other.Polarity != actual.Polarity || Values(other.Values) != Values(actual.Values) || Participants(other.Participants) != Participants(actual.Participants);
                Error(contradictory ? "conflicting duplicate assertion" : "duplicate assertion", gold.Id, contradictory);
            }
        }

        foreach (var i in Enumerable.Range(0, predicted.Events.Length).Where(i => !used.Contains(i))) { Error("additional assertion", predicted.Events[i].Id, exhaustiveGold); }
        var requestsUsed = new HashSet<int>();
        foreach (var gold in expected.Requests)
        {
            var candidates = Enumerable.Range(0, predicted.Requests.Length).Where(i => !requestsUsed.Contains(i) && predicted.Requests[i].Kind == gold.Kind && Sources(predicted.Requests[i].Sources) == Sources(gold.Sources)).ToArray();
            if (candidates.Length == 0) { Error("missing request", gold.Id); continue; }
            requestsUsed.Add(candidates[0]);
            if (Values(predicted.Requests[candidates[0]].Values) != Values(gold.Values)) { Error("request amount or ownership", gold.Id); }
        }

        foreach (var i in Enumerable.Range(0, predicted.Requests.Length).Where(i => !requestsUsed.Contains(i))) { Error("additional request", predicted.Requests[i].Id, exhaustiveGold); }
        static string Relation(EvidenceRelation r) => r.Kind + "|" + r.PlaintiffPosition + "|" + r.AssertedBy + "|" + Sources(r.Sources);
        foreach (var relation in expected.Relations.Select(Relation).Except(predicted.Relations.Select(Relation), StringComparer.Ordinal)) { Error("missing relationship or role", relation); }
        foreach (var relation in predicted.Relations.Select(Relation).Except(expected.Relations.Select(Relation), StringComparer.Ordinal)) { Error("additional relationship", relation, exhaustiveGold); }
        foreach (var document in expected.Documents.Where(d => !predicted.Documents.Any(p => p.Description == d.Description))) { Error("missing document reference", document.Id, critical: false); }
        var complete = actualCheck.CompleteCoverage && predicted.Unresolved.IsEmpty && !errors.Any(e => e.Critical);
        return new(Version, errors.ToImmutable(), complete, exhaustiveGold);
    }
}
