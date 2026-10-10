using System.Runtime.CompilerServices;
using DzLegal.Drafting;

namespace DzLegal.Web.Services;

/// <summary>
/// One queue for every visitor: at most <see cref="Capacity"/> model calls run at once, the others wait their turn.
/// A burst of judges then queues for a few seconds instead of exhausting the hosted model's per-minute limit.
/// </summary>
public sealed class ModelGate(int capacity) : IDisposable
{
    private readonly SemaphoreSlim _slots = new(capacity, capacity);
    private int _waiting;

    public int Capacity { get; } = capacity;

    /// <summary>Calls waiting for a free slot right now.</summary>
    public int Waiting => Volatile.Read(ref _waiting);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _waiting);
        try
        {
            await _slots.WaitAsync(cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _waiting);
        }

        return new Slot(_slots);
    }

    public void Dispose() => _slots.Dispose();

    private sealed class Slot(SemaphoreSlim slots) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                slots.Release();
            }
        }
    }
}

/// <summary>Passes each call through the <see cref="ModelGate"/>: it waits for a free slot, then holds it until the answer ends.</summary>
public sealed class GatedModelProvider(IModelProvider inner, ModelGate gate) : IModelProvider, IDisposable
{
    public string Name => inner.Name;

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        using var slot = await gate.EnterAsync(cancellationToken);
        return await inner.GenerateAsync(request, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var slot = await gate.EnterAsync(cancellationToken);
        await foreach (var piece in inner.StreamAsync(request, cancellationToken))
        {
            yield return piece;
        }
    }

    public void Dispose() => (inner as IDisposable)?.Dispose();
}

/// <summary>
/// Counts the AI runs of one browser session in a sliding hour. A public link should not let one visitor spend the whole
/// shared quota; a judge trying the demo stays far below the limit.
/// </summary>
public sealed class SessionQuota(int perHour, Func<DateTimeOffset>? now = null)
{
    private readonly Queue<DateTimeOffset> _runs = new();
    private readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.UtcNow);

    public bool TryTake()
    {
        var now = _now();
        while (_runs.Count > 0 && now - _runs.Peek() > TimeSpan.FromHours(1))
        {
            _runs.Dequeue();
        }

        if (_runs.Count >= perHour)
        {
            return false;
        }

        _runs.Enqueue(now);
        return true;
    }
}

/// <summary>Stands in for the engine once a session has used its hourly quota: every call explains the limit.</summary>
internal sealed class QuotaReachedModelProvider(string name, int perHour) : IModelProvider
{
    public string Name => name;

    public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) =>
        throw new ModelProviderException($"This demo session has used its {perHour} AI runs for this hour. The rule check, editing and export still work; AI is available again within the hour.");
}
