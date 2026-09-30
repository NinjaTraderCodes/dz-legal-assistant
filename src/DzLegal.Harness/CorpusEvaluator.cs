using DzLegal.Domain;
using DzLegal.FormEngine;

namespace DzLegal.Harness;

public sealed record ElementScore(string ElementId, int Cases, int Correct, int TruePositives, int FalsePositives, int FalseNegatives)
{
    public double Accuracy => Cases == 0 ? 1 : (double)Correct / Cases;

    /// <summary>Of the times the engine flagged this element missing, how often was it right.</summary>
    public double Precision => TruePositives + FalsePositives == 0 ? 1 : (double)TruePositives / (TruePositives + FalsePositives);

    /// <summary>Of the times the element really was missing, how often the engine caught it.</summary>
    public double Recall => TruePositives + FalseNegatives == 0 ? 1 : (double)TruePositives / (TruePositives + FalseNegatives);
}

public sealed record Mismatch(string CaseName, string ElementId, ElementStatus Expected, ElementStatus Actual);

public sealed record HarnessResult(IReadOnlyList<ElementScore> Scores, IReadOnlyList<Mismatch> Mismatches, int CaseCount)
{
    public int Checks => Scores.Sum(s => s.Cases);

    public double OverallAccuracy => Checks == 0 ? 1 : (double)Scores.Sum(s => s.Correct) / Checks;

    public bool Perfect => Mismatches.Count == 0;
}

/// <summary>Runs the Form Engine over the corpus and scores every element of every case.</summary>
public static class CorpusEvaluator
{
    public static HarnessResult Evaluate(DocumentTypeDefinition definition, IEnumerable<CorpusCase> cases)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(cases);
        var relevant = cases.Where(c => c.Draft.DocumentTypeId == definition.Id).ToList();
        var mismatches = new List<Mismatch>();
        var tallies = definition.Elements.ToDictionary(e => e.Id, _ => new int[5]); // cases, correct, tp, fp, fn

        foreach (var corpusCase in relevant)
        {
            var unknown = corpusCase.ExpectedMissing.Concat(corpusCase.ExpectedNotApplicable)
                .Where(id => definition.Elements.All(e => e.Id != id))
                .ToList();
            if (unknown.Count > 0)
            {
                throw new InvalidDataException($"Case '{corpusCase.Name}' names unknown elements: {string.Join(", ", unknown)}");
            }

            var report = ComplianceChecker.Check(definition, corpusCase.Draft);
            foreach (var finding in report.Findings)
            {
                var expected = corpusCase.ExpectedStatus(finding.ElementId);
                var t = tallies[finding.ElementId];
                t[0]++;
                if (expected == finding.Status)
                {
                    t[1]++;
                }
                else
                {
                    mismatches.Add(new Mismatch(corpusCase.Name, finding.ElementId, expected, finding.Status));
                }

                var expectedMissing = expected == ElementStatus.Missing;
                var flagged = finding.Status == ElementStatus.Missing;
                if (expectedMissing && flagged)
                {
                    t[2]++;
                }
                else if (!expectedMissing && flagged)
                {
                    t[3]++;
                }
                else if (expectedMissing && !flagged)
                {
                    t[4]++;
                }
            }
        }

        var scores = tallies.Select(kv => new ElementScore(kv.Key, kv.Value[0], kv.Value[1], kv.Value[2], kv.Value[3], kv.Value[4])).ToList();
        return new HarnessResult(scores, mismatches, relevant.Count);
    }
}
