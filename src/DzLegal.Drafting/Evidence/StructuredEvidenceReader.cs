using System.Text.Json;

namespace DzLegal.Drafting.Evidence;

/// <summary>Experimental reader; a tokenizer supplied by its runtime must certify the complete request budget.</summary>
public sealed class StructuredEvidenceReader(IStructuredModelProvider provider, string instructions, Func<ModelRequest, int> countInputTokens, int contextTokens, int outputTokens = 1500) : IEvidenceReader
{
    public ModelRequest BuildRequest(CaseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(countInputTokens);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);
        ArgumentOutOfRangeException.ThrowIfLessThan(outputTokens, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(contextTokens, 1);
        if (contextTokens != provider.ContextTokenLimit || outputTokens > provider.OutputTokenLimit)
        {
            throw new ModelProviderException("Evidence token budget does not match the configured structured backend.");
        }
        var references = new EvidenceReferences(snapshot);
        var request = new ModelRequest("evidence.reading", instructions,
            JsonSerializer.Serialize(references.ModelInput, EvidenceJson.Options),
            string.Join('\n', snapshot.Fields.Values)) { OutputSchema = references.Schema(), MaxTokens = outputTokens };
        var inputTokens = countInputTokens(request);
        if (inputTokens < 0 || inputTokens > contextTokens - outputTokens)
        {
            throw new ModelProviderException("The complete evidence request does not fit the certified context budget. No passage was dropped.");
        }

        return request;
    }

    public async Task<CaseEvidence> ReadAsync(CaseSnapshot snapshot, CancellationToken cancellationToken)
    {
        var request = BuildRequest(snapshot);
        var answer = await provider.GenerateAsync(request, cancellationToken);
        return new EvidenceReferences(snapshot).Decode(answer);
    }
}
