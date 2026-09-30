using DzLegal.Drafting;

namespace DzLegal.ModelProviders.Stub;

/// <summary>
/// "Template mode": no model at all. Each narrative section receives the lawyer's notes unchanged,
/// placed inside the deterministic scaffold. Useful on machines without a local model and in tests.
/// </summary>
public sealed class StubModelProvider : IModelProvider
{
    public string Name => "stub (template mode, no AI)";

    public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(request.SourceText.Trim());
    }
}
