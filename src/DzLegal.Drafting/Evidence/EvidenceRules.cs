using System.Collections.Immutable;

namespace DzLegal.Drafting.Evidence;

/// <summary>Four-valued evidence algebra. Absence and conflict are not converted into negative facts.</summary>
public static class EvidenceLogic
{
    public static EvidenceState Not(EvidenceState value) => value switch
    {
        EvidenceState.Supported => EvidenceState.Refuted,
        EvidenceState.Refuted => EvidenceState.Supported,
        _ => value,
    };

    public static EvidenceState All(IEnumerable<EvidenceState> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var list = values.ToArray();
        if (list.Length == 0) { return EvidenceState.Unknown; }
        if (list.Contains(EvidenceState.Refuted)) { return EvidenceState.Refuted; }
        if (list.Contains(EvidenceState.Conflicting)) { return EvidenceState.Conflicting; }
        return list.All(v => v == EvidenceState.Supported) ? EvidenceState.Supported : EvidenceState.Unknown;
    }

    public static EvidenceState Any(IEnumerable<EvidenceState> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var list = values.ToArray();
        if (list.Length == 0) { return EvidenceState.Unknown; }
        if (list.Contains(EvidenceState.Supported)) { return EvidenceState.Supported; }
        if (list.Contains(EvidenceState.Conflicting)) { return EvidenceState.Conflicting; }
        return list.All(v => v == EvidenceState.Refuted) ? EvidenceState.Refuted : EvidenceState.Unknown;
    }
}

public sealed record AuthorityClause(string Code, string Article, string Version, string Quote, string SourceHash, string ReviewState)
{
    public bool ReviewedSource => ReviewState == "reviewed" && !string.IsNullOrWhiteSpace(Code) && !string.IsNullOrWhiteSpace(Article)
        && !string.IsNullOrWhiteSpace(Version) && Version is not ("unknown" or "unqualified" or "unreviewed")
        && !string.IsNullOrWhiteSpace(Quote) && SourceHash.Length == 64 && SourceHash.All(Uri.IsHexDigit);
}

public sealed record ConditionResult(string Id, EvidenceState State, ImmutableArray<string> EvidenceIds, ImmutableArray<string> OpenPoints);

public sealed record RuleDecision(string RuleId, EvidenceState State, ImmutableArray<ConditionResult> Conditions, ImmutableArray<AuthorityClause> Authorities, bool Qualified);

/// <summary>A reviewable module specification, not an automatic authority to deploy legal conclusions.</summary>
public sealed record EvidenceRule(string Id, ImmutableArray<string> RequiredConditions, ImmutableArray<string> Exceptions, ImmutableArray<AuthorityClause> Authorities, string ReviewState)
{
    public RuleDecision Evaluate(IReadOnlyDictionary<string, ConditionResult> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ConditionResult Find(string id) => conditions.TryGetValue(id, out var result) ? result : new(id, EvidenceState.Unknown, [], [id]);
        var requirements = RequiredConditions.Select(Find).ToImmutableArray();
        var exceptions = Exceptions.Select(Find).ToImmutableArray();
        var state = EvidenceLogic.All(requirements.Select(c => c.State).Concat(exceptions.Select(c => EvidenceLogic.Not(c.State))));
        var qualified = ReviewState == "reviewed" && !Authorities.IsEmpty && Authorities.All(a => a.ReviewedSource);
        return new(Id, state, [.. requirements, .. exceptions], Authorities, qualified);
    }
}

/// <summary>Lease predicates for a harness slice. No current-law rule is asserted by this extractor.</summary>
public static class CivilLeaseConditions
{
    public static ImmutableDictionary<string, ConditionResult> Evaluate(AssessedEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var result = ImmutableDictionary.CreateBuilder<string, ConditionResult>();
        var proposed = evidence.Checked.Proposed;
        ConditionResult Event(string id, string kind, string? actor = null)
        {
            var candidates = evidence.Checked.StructurallyValid ? proposed.Events.Where(e => e.Kind == kind && (actor is null || e.Participants.Any(p => p.Role == "actor" && p.PartyId == actor))).ToArray() : [];
            var states = candidates.Select(e => evidence.StateOf(e.Id) != EvidenceState.Supported ? EvidenceState.Unknown
                : e.Polarity == FactPolarity.Disputed ? EvidenceState.Conflicting : e.Polarity == FactPolarity.Denied ? EvidenceState.Refuted : EvidenceState.Supported).ToArray();
            var state = states.Contains(EvidenceState.Supported) && states.Contains(EvidenceState.Refuted) ? EvidenceState.Conflicting : EvidenceLogic.Any(states);
            return new(id, state, [.. candidates.Select(c => c.Id)], state == EvidenceState.Supported ? [] : [id]);
        }

        result["lease.rent_unpaid"] = Event("lease.rent_unpaid", "rent_unpaid", "defendant");
        result["lease.notice_served"] = Event("lease.notice_served", "notice_served");
        result["lease.contract_concluded"] = Event("lease.contract_concluded", "contract_concluded");
        var requests = evidence.Checked.StructurallyValid ? proposed.Requests.Where(r => r.Kind == "payment").ToArray() : [];
        var requestState = EvidenceLogic.Any(requests.Select(r => evidence.StateOf(r.Id) == EvidenceState.Supported ? EvidenceState.Supported : EvidenceState.Unknown));
        result["lease.payment_requested"] = new("lease.payment_requested", requestState, [.. requests.Select(r => r.Id)], requestState == EvidenceState.Supported ? [] : ["lease.payment_requested"]);
        var relation = evidence.Checked.StructurallyValid ? proposed.Relations.Where(r => r.Kind == "lease" && r.PlaintiffPosition == "lessor").ToArray() : [];
        var relationSupported = relation.Length == 1 && evidence.StateOf("relation") == EvidenceState.Supported;
        result["lease.plaintiff_lessor"] = new("lease.plaintiff_lessor", relationSupported ? EvidenceState.Supported : EvidenceState.Unknown, relationSupported ? ["relation"] : [], relationSupported ? [] : ["lease.plaintiff_lessor"]);
        return result.ToImmutable();
    }
}
