using System.Runtime.CompilerServices;
using DzLegal.Drafting;
using DzLegal.Presentation;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Services.LocalModels;

/// <summary>A click during startup cannot send case notes before the local checks finish.</summary>
internal sealed class VerifiedLocalProvider(Func<CancellationToken, Task<ModelStatus>> probe, Func<IModelProvider> create,
    Func<string> name, Loc loc) : IModelProvider, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IModelProvider? provider;
    public string Name => provider?.Name ?? name();

    private async Task<IModelProvider> ReadyAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (provider is not null) return provider;
            var status = await probe(cancellationToken).ConfigureAwait(false);
            if (status.State != ModelState.Ready)
                throw new ModelProviderException(loc[status.State == ModelState.MemoryLow ? "ModelMemoryLow" : "ModelSetupEngineFailed"]);
            provider = create();
            return provider;
        }
        finally { gate.Release(); }
    }

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) =>
        await (await ReadyAsync(cancellationToken).ConfigureAwait(false)).GenerateAsync(request, cancellationToken).ConfigureAwait(false);

    public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ready = await ReadyAsync(cancellationToken).ConfigureAwait(false);
        await foreach (var piece in ready.StreamAsync(request, cancellationToken).ConfigureAwait(false)) yield return piece;
    }

    public void Dispose()
    {
        (provider as IDisposable)?.Dispose();
        gate.Dispose();
    }
}