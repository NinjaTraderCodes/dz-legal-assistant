using System.Collections.Immutable;

namespace DzLegal.Harness;

public sealed record QualificationCase(string CaseId, string ScenarioFamily, string SourceDecision, string Partition, bool Complete, int CriticalErrors, bool GoldReviewed);

public sealed record QualificationDecision(bool Qualified, int Cases, int CompleteCases, int CriticalCases, double Coverage, double? ZeroErrorUpperBound95, ImmutableArray<string> Reasons);

/// <summary>Finite-test deployment gate. A passing report is not a guarantee for an individual future case.</summary>
public static class EvidenceQualification
{
    public static QualificationDecision Evaluate(IReadOnlyList<QualificationCase> release, IReadOnlyList<QualificationCase> development, bool reviewedRules, bool physical8GbPassed)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(development);
        var reasons = ImmutableArray.CreateBuilder<string>();
        if (release.Count < 300) { reasons.Add("fewer than 300 sealed scenarios"); }
        if (release.Select(c => c.ScenarioFamily).Distinct(StringComparer.Ordinal).Count() < 300 || release.Select(c => c.SourceDecision).Distinct(StringComparer.Ordinal).Count() < 300) { reasons.Add("fewer than 300 independent scenario/source groups"); }
        if (release.Select(c => c.CaseId).Distinct(StringComparer.Ordinal).Count() != release.Count) { reasons.Add("duplicate case identities"); }
        if (release.Any(c => c.Partition != "release" || !c.GoldReviewed || string.IsNullOrWhiteSpace(c.ScenarioFamily) || string.IsNullOrWhiteSpace(c.SourceDecision))) { reasons.Add("unreviewed or unsealed gold"); }
        if (release.Any(c => development.Any(d => d.ScenarioFamily == c.ScenarioFamily || d.SourceDecision == c.SourceDecision || d.CaseId == c.CaseId))) { reasons.Add("training or calibration leakage"); }
        if (release.Any(c => c.CriticalErrors < 0)) { reasons.Add("invalid error count"); }
        var critical = release.Count(c => c.CriticalErrors > 0);
        if (critical > 0) { reasons.Add("critical errors remain"); }
        var complete = release.Count(c => c.Complete);
        var coverage = release.Count == 0 ? 0 : (double)complete / release.Count;
        if (coverage < 0.9) { reasons.Add("complete coverage below 90 percent"); }
        if (!reviewedRules) { reasons.Add("rules awaiting legal review"); }
        if (!physical8GbPassed) { reasons.Add("physical 8 GB qualification missing"); }
        // One-sided binomial bound for zero errors among completed outputs; independence is a stated assumption.
        var independentComplete = Math.Min(release.Where(c => c.Complete).Select(c => c.SourceDecision).Distinct(StringComparer.Ordinal).Count(), release.Where(c => c.Complete).Select(c => c.ScenarioFamily).Distinct(StringComparer.Ordinal).Count());
        double? bound = critical == 0 && independentComplete > 0 ? 1 - Math.Pow(0.05, 1.0 / independentComplete) : null;
        return new(reasons.Count == 0, release.Count, complete, critical, coverage, bound, reasons.ToImmutable());
    }
}
