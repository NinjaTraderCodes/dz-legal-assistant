using System.Collections.Immutable;
using System.Diagnostics;

namespace DzLegal.Drafting.Evidence;

public sealed record DraftEvidenceClause(string Id, string Category, string AssertedBy, ImmutableArray<EvidenceSpan> Sources);

public sealed record EvidenceDraftPlan(AnalysisIdentity Identity, ImmutableArray<DraftEvidenceClause> Clauses, ImmutableArray<AuthorityClause> Authorities, ImmutableArray<string> OpenPoints);

public sealed record EvidenceRun(AssessedEvidence Evidence, ImmutableArray<RuleDecision> Decisions, EvidenceDraftPlan DraftPlan, TimeSpan Elapsed, long PeakWorkingSetBytes, bool Current);

/// <summary>Harness-first orchestration. No app behavior or export gate depends on this pipeline.</summary>
public sealed class EvidencePipeline(IEvidenceReader reader, IEvidenceAssessor assessor)
{
    public async Task<EvidenceRun> RunAsync(CaseSnapshot snapshot, AnalysisIdentity identity, Func<AnalysisIdentity> currentIdentity, IReadOnlyList<EvidenceRule> rules, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(currentIdentity);
        ArgumentNullException.ThrowIfNull(rules);
        if (identity.Schema != CaseEvidence.CurrentSchema) { throw new ArgumentException("Analysis schema does not match the evidence reader.", nameof(identity)); }
        cancellationToken.ThrowIfCancellationRequested();
        var watch = Stopwatch.StartNew();
        var proposed = await reader.ReadAsync(snapshot, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var check = EvidenceValidator.Check(snapshot, proposed);
        var findings = check.StructurallyValid ? await assessor.AssessAsync(check, cancellationToken) : [];
        var assessed = new AssessedEvidence(check, findings);
        var conditions = CivilLeaseConditions.Evaluate(assessed);
        var decisions = rules.Select(r => r.Evaluate(conditions)).ToImmutableArray();
        var plan = Plan(assessed, identity, decisions);
        cancellationToken.ThrowIfCancellationRequested();
        using var process = Process.GetCurrentProcess();
        return new(assessed, decisions, plan, watch.Elapsed, process.PeakWorkingSet64, identity.IsCurrent(snapshot, currentIdentity()));
    }

    public static EvidenceDraftPlan Plan(AssessedEvidence evidence, AnalysisIdentity identity, ImmutableArray<RuleDecision> decisions)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(identity);
        var snapshot = evidence.Checked.Snapshot;
        // Preserve complete lawyer text. Unqualified extraction never silently deletes the difficult passages.
        var clauses = snapshot.Sentences.Where(s => s.FieldId is "notes.facts" or "claim.subject" or "notes.requests" or "documents.list")
            .Select(s => new DraftEvidenceClause(s.Id, s.FieldId, "unknown", [new(s.FieldId, s.Start, s.Text.Length)])).ToImmutableArray();
        var authorities = decisions.Where(d => d.Qualified && d.State == EvidenceState.Supported).SelectMany(d => d.Authorities).Distinct().ToImmutableArray();
        var open = evidence.Checked.Issues.Select(i => i.Code).Concat(evidence.Checked.Proposed.Unresolved.IsDefault ? [] : evidence.Checked.Proposed.Unresolved)
            .Concat(decisions.Where(d => !d.Qualified).Select(_ => "EvidenceRuleUnreviewed"))
            .Concat(decisions.SelectMany(d => d.Conditions.Where(c => c.State is EvidenceState.Unknown or EvidenceState.Conflicting).Select(_ => "EvidenceConditionOpen")))
            .Concat(evidence.Findings.IsEmpty ? ["EvidenceMeaningUnassessed"] : Array.Empty<string>()).Distinct().ToImmutableArray();
        if (!evidence.Checked.CompleteCoverage) { open = [.. open, "EvidenceCoverageIncomplete"]; }
        return new(identity, clauses, authorities, open);
    }
}

/// <summary>Only exact original spans and reviewed authority quotes reach the renderer. No model prose is accepted.</summary>
public static class EvidenceDraftRenderer
{
    public static string Render(CaseSnapshot snapshot, EvidenceDraftPlan plan, AnalysisIdentity current, Func<string, string> localize, Func<string, object?[], string> format)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(localize);
        ArgumentNullException.ThrowIfNull(format);
        if (!plan.Identity.IsCurrent(snapshot, current)) { throw new InvalidOperationException("Evidence draft is stale."); }
        var lines = new List<string> { localize("EvidenceDraftTitle") };
        foreach (var clause in plan.Clauses)
        {
            foreach (var span in clause.Sources)
            {
                var source = snapshot.Read(span) ?? throw new InvalidOperationException("Draft source no longer exists.");
                var key = clause.Category switch { "notes.requests" => "EvidenceRequested", "documents.list" => "EvidenceListedDocument", _ => "EvidenceLawyerAccount" };
                lines.Add(format(key, [source]));
            }
        }

        lines.AddRange(plan.Authorities.Where(a => a.ReviewedSource).Select(a => a.Quote));
        if (!plan.OpenPoints.IsEmpty) { lines.Add(localize("EvidenceOpenPoints")); }
        return string.Join(Environment.NewLine, lines);
    }
}
