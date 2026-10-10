using System.Runtime.CompilerServices;
using DzLegal.Drafting;

namespace DzLegal.ModelProviders.Hosted;

/// <summary>
/// Tries engines in order: when one is busy or briefly down (<see cref="ModelProviderException.IsTransient"/>) before it
/// has written anything, the next one takes the request. Once text has been shown, a failure is reported as is: switching
/// engines mid-answer would mix two answers in one section.
/// </summary>
public sealed class FallbackModelProvider : IModelProvider, IDisposable
{
    private readonly IReadOnlyList<IModelProvider> _engines;
    private readonly bool _ownsEngines;

    /// <param name="engines">Tried in order.</param>
    /// <param name="ownsEngines">False when the engines are shared (the web demo reuses one set, and its connections, for every visitor).</param>
    public FallbackModelProvider(IReadOnlyList<IModelProvider> engines, bool ownsEngines = true)
    {
        ArgumentNullException.ThrowIfNull(engines);
        if (engines.Count == 0)
        {
            throw new ArgumentException("At least one engine is needed.", nameof(engines));
        }

        _engines = engines;
        _ownsEngines = ownsEngines;
    }

    /// <summary>The engine that answered the last request (the first one until then).</summary>
    public IModelProvider? Current { get; private set; }

    public string Name => (Current ?? _engines[0]).Name;

    /// <summary>Told when an engine is skipped, with the reason (for the server log).</summary>
    public Action<IModelProvider, ModelProviderException>? OnSkipped { get; set; }

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        for (var i = 0; ; i++)
        {
            try
            {
                Current = _engines[i];
                return await _engines[i].GenerateAsync(request, cancellationToken);
            }
            catch (ModelProviderException ex) when (ex.IsTransient && i + 1 < _engines.Count)
            {
                OnSkipped?.Invoke(_engines[i], ex);
            }
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < _engines.Count; i++)
        {
            Current = _engines[i];
            await using var pieces = _engines[i].StreamAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);

            // The first piece decides: an engine that fails before writing anything hands over to the next one.
            bool hasFirst;
            try
            {
                hasFirst = await pieces.MoveNextAsync();
            }
            catch (ModelProviderException ex) when (ex.IsTransient && i + 1 < _engines.Count)
            {
                OnSkipped?.Invoke(_engines[i], ex);
                continue;
            }

            if (!hasFirst)
            {
                yield break;
            }

            yield return pieces.Current;
            while (await pieces.MoveNextAsync())
            {
                yield return pieces.Current;
            }

            yield break;
        }
    }

    public void Dispose()
    {
        if (!_ownsEngines)
        {
            return;
        }

        foreach (var engine in _engines)
        {
            (engine as IDisposable)?.Dispose();
        }
    }
}
