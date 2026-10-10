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

/// <summary>
/// What every model call of a web visitor goes through:
/// 1. the visitor's limits (per browser session and per network address), charged per call, not per click;
/// 2. the shared queue (<see cref="ModelGate"/>), held until the answer ends;
/// 3. a plain message instead of the service's raw error, which goes to the server log only.
/// </summary>
public sealed class ProtectedModelProvider(
    IModelProvider inner,
    ModelGate gate,
    IReadOnlyList<CallQuota> quotas,
    Action<string>? logFailure = null) : IModelProvider, IDisposable
{
    public const string Unavailable =
        "The AI service did not answer. Try again in a minute; the formal check, editing and export keep working.";

    public string Name => inner.Name;

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var text = new System.Text.StringBuilder();
        await foreach (var piece in StreamAsync(request, cancellationToken))
        {
            text.Append(piece);
        }

        return text.Length > 0 ? text.ToString() : throw new ModelProviderException(Unavailable);
    }

    public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var quota in quotas)
        {
            if (!quota.TryTake())
            {
                throw new ModelProviderException(quota.ReachedMessage);
            }
        }

        using var slot = await gate.EnterAsync(cancellationToken);
        await using var pieces = inner.StreamAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool more;
            try
            {
                more = await pieces.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logFailure?.Invoke(ex.Message);
                throw new ModelProviderException(Unavailable, ex);
            }

            if (!more)
            {
                yield break;
            }

            yield return pieces.Current;
        }
    }

    public void Dispose() => (inner as IDisposable)?.Dispose();
}

/// <summary>
/// Counts model calls in a sliding hour: one per browser session, and one per network address shared by all of its
/// sessions, so reloading the page does not reset the limit. A judge trying the demo stays far below both.
/// </summary>
public sealed class CallQuota(int perHour, string reachedMessage, Func<DateTimeOffset>? now = null)
{
    private readonly Queue<DateTimeOffset> _calls = new();
    private readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.UtcNow);
    private readonly Lock _lock = new();

    public string ReachedMessage { get; } = reachedMessage;

    public bool TryTake()
    {
        lock (_lock)
        {
            var now = _now();
            while (_calls.Count > 0 && now - _calls.Peek() > TimeSpan.FromHours(1))
            {
                _calls.Dequeue();
            }

            if (_calls.Count >= perHour)
            {
                return false;
            }

            _calls.Enqueue(now);
            return true;
        }
    }

    /// <summary>True when nothing was counted in the last hour, so the entry can be forgotten.</summary>
    public bool IsIdle
    {
        get
        {
            lock (_lock)
            {
                return _calls.Count == 0 || _now() - _calls.Last() > TimeSpan.FromHours(1);
            }
        }
    }
}
