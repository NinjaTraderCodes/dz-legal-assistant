using System.Collections.Immutable;

namespace DzLegal.Drafting.Evidence;

public sealed record EvidenceParticipant(string Role, string PartyId, ImmutableArray<EvidenceSpan> Sources);

/// <summary>A reference to a value, not a model-retyped number. Its enclosing event owns the binding.</summary>
public sealed record EvidenceValue(string Kind, string Role, EvidenceSpan Source);

public sealed record EvidenceEvent(
    string Id, string Kind, string AssertedBy, string Polarity, string? DisputeGroup,
    ImmutableArray<EvidenceParticipant> Participants, ImmutableArray<EvidenceValue> Values, ImmutableArray<EvidenceSpan> Sources);

public sealed record EvidenceRequest(string Id, string Kind, ImmutableArray<EvidenceValue> Values, ImmutableArray<EvidenceSpan> Sources);

public sealed record EvidenceRelation(string Kind, string PlaintiffPosition, string AssertedBy, ImmutableArray<EvidenceSpan> Sources);

public sealed record EvidenceDocument(string Id, EvidenceSpan Description);

/// <summary>A passage read by the reader. This is coverage accounting, not evidence that it was understood.</summary>
public sealed record EvidenceCoverage(string SentenceId, string Status);

public sealed record CaseEvidence
{
    public const string CurrentSchema = "dzlegal.evidence/1";
    public string Schema { get; init; } = CurrentSchema;
    public string SnapshotFingerprint { get; init; } = string.Empty;
    public ImmutableArray<EvidenceRelation> Relations { get; init; } = [];
    public ImmutableArray<EvidenceEvent> Events { get; init; } = [];
    public ImmutableArray<EvidenceRequest> Requests { get; init; } = [];
    public ImmutableArray<EvidenceDocument> Documents { get; init; } = [];
    public ImmutableArray<EvidenceCoverage> Coverage { get; init; } = [];
    public ImmutableArray<string> Unresolved { get; init; } = [];
}

public sealed record EvidenceIssue(string Code, string ItemId, string Detail);

public sealed record CheckedEvidence(CaseSnapshot Snapshot, CaseEvidence Proposed, ImmutableArray<EvidenceIssue> Issues)
{
    public bool StructurallyValid => Issues.IsEmpty;
    public bool CompleteCoverage => StructurallyValid && Proposed.Coverage.All(c => c.Status == "read");
}

/// <summary>Semantic findings are separate from reader output; copying a source span does not create one.</summary>
public enum EvidenceState { Unknown, Supported, Refuted, Conflicting }

public sealed record SemanticFinding(string PropositionId, EvidenceState State, string Method, string Version);

public sealed record AssessedEvidence(CheckedEvidence Checked, ImmutableArray<SemanticFinding> Findings)
{
    public EvidenceState StateOf(string id)
    {
        if (!Checked.CompleteCoverage) { return EvidenceState.Unknown; }
        var states = Findings.Where(f => f.PropositionId == id && !string.IsNullOrWhiteSpace(f.Method) && !string.IsNullOrWhiteSpace(f.Version)).Select(f => f.State).Distinct().ToArray();
        if (states.Contains(EvidenceState.Conflicting) || (states.Contains(EvidenceState.Supported) && states.Contains(EvidenceState.Refuted))) { return EvidenceState.Conflicting; }
        return states.Length == 1 ? states[0] : EvidenceState.Unknown;
    }
}

public interface IEvidenceReader
{
    Task<CaseEvidence> ReadAsync(CaseSnapshot snapshot, CancellationToken cancellationToken);
}

public interface IEvidenceAssessor
{
    Task<ImmutableArray<SemanticFinding>> AssessAsync(CheckedEvidence evidence, CancellationToken cancellationToken);
}

/// <summary>Default until a semantic assessor is qualified. Syntax never upgrades itself to trusted meaning.</summary>
public sealed class UnqualifiedEvidenceAssessor : IEvidenceAssessor
{
    public Task<ImmutableArray<SemanticFinding>> AssessAsync(CheckedEvidence evidence, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ImmutableArray<SemanticFinding>.Empty);
    }
}
