using DzLegal.Drafting;
using DzLegal.Law;

namespace DzLegal.Harness;

/// <summary>
/// Alpha-01 in the fact-reading bench (docs/ALPHA-01.md): the case is read against its claim type. With
/// <see cref="Given"/>, the claim type is the one the lawyer would confirm (the answer key's), which measures the reading
/// alone; with <see cref="Read"/>, it is the first one the analysis's reading step proposes, which measures the chain as
/// the app would run it before the lawyer confirms.
/// </summary>
public static class AlphaFacts
{
    public const string Given = "given";
    public const string Read = "read";

    public static Func<FactCase, CaseText, CancellationToken, Task<FactReading>> Reader(IModelProvider provider, string lawRoot, string claims)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var knowledge = LegalKnowledgeLoader.Load(lawRoot);
        var reader = new ClaimFactReader(provider, ClaimFactReader.LoadPrompt(lawRoot), ClaimFactReader.LoadElements(lawRoot));
        var labels = knowledge.Claims.ToDictionary(c => c.Id, c => c.Label.Ar, StringComparer.Ordinal);
        if (knowledge.Claims.FirstOrDefault(c => !reader.Knows(c.Id)) is { } missing)
        {
            throw new InvalidDataException($"{ClaimFactReader.ElementsFile} has no elements for the claim type '{missing.Id}'.");
        }

        var analyzer = new GroundsAnalyzer(provider);
        return async (c, text, ct) =>
        {
            var expected = c.Claim.Length > 0 ? c.Claim : FactCase.OutOfScope;
            string claim;
            var errors = new List<FactError>();
            var claimAnswer = string.Empty;
            if (claims == Given)
            {
                if (expected == FactCase.OutOfScope)
                {
                    return new FactReading(new CaseFacts(), string.Empty) { Claim = expected, NotRead = true, Excluded = true };
                }

                claim = expected;
            }
            else
            {
                var reading = await analyzer.ReadClaimsAsync(
                    knowledge,
                    c.Inputs.GetValueOrDefault("claim.subject") ?? string.Empty,
                    c.Inputs.GetValueOrDefault("notes.facts") ?? string.Empty,
                    c.Inputs.GetValueOrDefault("court.section"),
                    ct);
                claim = reading.ClaimIds.FirstOrDefault(id => labels.ContainsKey(id)) ?? FactCase.OutOfScope;
                claimAnswer = $"[claim type read: {claim}]\n";
                if (claim == FactCase.OutOfScope || expected == FactCase.OutOfScope)
                {
                    if (claim != expected)
                    {
                        errors.Add(claim == FactCase.OutOfScope
                            ? new FactError("claim not recognised", $"none instead of {expected}", true)
                            : new FactError("claim proposed outside the catalogue", claim, true));
                    }

                    return new FactReading(new CaseFacts(), claimAnswer) { Claim = claim, NotRead = true, ClaimErrors = errors };
                }

                if (claim != expected)
                {
                    errors.Add(new FactError("wrong claim type", $"{claim} instead of {expected}", true));
                }
            }

            var (facts, answer) = await reader.ReadAsync(text, claim, labels[claim], ct);
            return new FactReading(facts, claimAnswer + answer) { Claim = claim, ClaimErrors = errors };
        };
    }
}
