using System.Collections.Immutable;
using System.Text.Json;
using DzLegal.Drafting.Evidence;

namespace DzLegal.Harness;

public sealed record EvidenceDatasetCase(string CaseId, string ScenarioFamily, string SourceDecision, string Partition, bool Fictitious, string LabelReview, Dictionary<string, string> Inputs, CaseEvidence Target);

/// <summary>Exports only explicitly reviewed training labels. Calibration and sealed answers never enter training files.</summary>
public static class EvidenceDataset
{
    private static readonly JsonSerializerOptions Compact = new(EvidenceJson.Options) { WriteIndented = false };
    public static ImmutableArray<string> Validate(IReadOnlyList<EvidenceDatasetCase> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);
        var errors = ImmutableArray.CreateBuilder<string>();
        if (cases.Count == 0) { errors.Add("empty dataset"); }
        if (cases.Select(c => c.CaseId).Distinct(StringComparer.Ordinal).Count() != cases.Count) { errors.Add("duplicate case id"); }
        foreach (var item in cases)
        {
            if (!item.Fictitious || item.LabelReview != "reviewed" || string.IsNullOrWhiteSpace(item.CaseId) || string.IsNullOrWhiteSpace(item.ScenarioFamily) || string.IsNullOrWhiteSpace(item.SourceDecision)) { errors.Add($"{item.CaseId}: fictitious, reviewed, source-linked labels required"); }
            if (item.Partition is not ("train" or "calibration" or "release")) { errors.Add($"{item.CaseId}: unknown partition"); }
            var snapshot = CaseSnapshot.Create(item.CaseId, 0, item.Inputs);
            errors.AddRange(EvidenceValidator.Check(snapshot, item.Target).Issues.Select(i => $"{item.CaseId}: {i.Code}"));
        }

        foreach (var group in cases.GroupBy(c => c.ScenarioFamily).Where(g => g.Select(c => c.Partition).Distinct().Count() > 1)) { errors.Add($"family crosses partitions: {group.Key}"); }
        foreach (var group in cases.GroupBy(c => c.SourceDecision).Where(g => g.Select(c => c.Partition).Distinct().Count() > 1)) { errors.Add($"source crosses partitions: {group.Key}"); }
        return errors.ToImmutable();
    }

    public static void ExportTraining(IReadOnlyList<EvidenceDatasetCase> cases, string folder)
    {
        var errors = Validate(cases);
        if (!errors.IsEmpty) { throw new InvalidDataException(string.Join(Environment.NewLine, errors)); }
        if (Directory.Exists(folder)) { throw new IOException("Choose a fresh dataset output folder; existing artifacts are preserved."); }
        Directory.CreateDirectory(folder);
        var training = cases.Where(c => c.Partition == "train").Select(c => JsonSerializer.Serialize(new
        {
            id = c.CaseId,
            input = CaseSnapshot.Create(c.CaseId, 0, c.Inputs),
            output = c.Target,
            referenceInput = new EvidenceReferences(CaseSnapshot.Create(c.CaseId, 0, c.Inputs)).ModelInput,
            referenceOutput = new EvidenceReferences(CaseSnapshot.Create(c.CaseId, 0, c.Inputs)).Encode(c.Target),
        }, Compact));
        File.WriteAllLines(Path.Combine(folder, "training.jsonl"), training);
        File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonSerializer.Serialize(new
        {
            schema = CaseEvidence.CurrentSchema,
            trainingSha256 = RunManifest.FolderHash(Path.Combine(folder, "training.jsonl")),
            cases = cases.Select(c => new { c.CaseId, c.ScenarioFamily, c.SourceDecision, c.Partition, c.LabelReview, c.Fictitious }),
            paidExecutionAuthorized = false,
            proposedComputeCeilingUsd = 100,
        }, EvidenceJson.Options));
    }
}
