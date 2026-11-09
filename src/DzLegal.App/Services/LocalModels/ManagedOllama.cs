using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace DzLegal.App.Services.LocalModels;

public sealed record LocalEngineSession(Uri Endpoint, bool IsManaged);

public interface ILocalEngine : IDisposable
{
    Task<LocalEngineSession> EnsureAsync(CancellationToken cancellationToken);
    bool IsManaged { get; }
}

public interface IEngineProcess : IDisposable
{
    bool HasExited { get; }
    bool OwnsListener(int port);
}

public interface IEngineLauncher
{
    IEngineProcess Start(int port);
}

/// <summary>Owns only a process it launched. Discovery and startup never pull models.</summary>
public sealed class ManagedOllama : ILocalEngine
{
    private readonly Uri existing;
    private readonly IEngineLauncher launcher;
    private readonly Func<Uri, CancellationToken, Task<bool>> compatible;
    private readonly Func<int> reservePort;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private readonly object sync = new();
    private IEngineProcess? process;
    private LocalEngineSession? session;
    private bool disposed;

    public ManagedOllama(Uri existing, IEngineLauncher launcher,
        Func<Uri, CancellationToken, Task<bool>>? compatible = null, Func<int>? reservePort = null)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(launcher);
        if (!DzLegal.ModelProviders.Ollama.LoopbackGuard.IsLoopback(existing))
            throw new ArgumentException("Only local model endpoints are accepted.", nameof(existing));
        this.existing = existing;
        this.launcher = launcher;
        this.compatible = compatible ?? ProbeAsync;
        this.reservePort = reservePort ?? AvailablePort;
    }

    public bool IsManaged { get { lock (sync) return session?.IsManaged == true; } }

    public Task<LocalEngineSession> EnsureAsync(CancellationToken cancellationToken) => EnsureCoreAsync(false, cancellationToken);

    public Task<LocalEngineSession> EnsureManagedAsync(CancellationToken cancellationToken) => EnsureCoreAsync(true, cancellationToken);

    private async Task<LocalEngineSession> EnsureCoreAsync(bool managedOnly, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        await gate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            lock (sync)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (session is { IsManaged: true } && process?.HasExited == false) return session;
            }
            if (!managedOnly && await compatible(existing, linked.Token).ConfigureAwait(false))
            {
                lock (sync)
                {
                    ObjectDisposedException.ThrowIf(disposed, this);
                    session = new LocalEngineSession(existing, false);
                    return session;
                }
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                linked.Token.ThrowIfCancellationRequested();
                var port = reservePort();
                var endpoint = new Uri($"http://127.0.0.1:{port}");
                var candidate = launcher.Start(port);
                var accepted = false;
                try
                {
                    using var ready = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                    ready.CancelAfter(TimeSpan.FromSeconds(15));
                    while (!candidate.HasExited)
                    {
                        ready.Token.ThrowIfCancellationRequested();
                        if (candidate.OwnsListener(port) && await compatible(endpoint, ready.Token).ConfigureAwait(false))
                        {
                            lock (sync)
                            {
                                ObjectDisposedException.ThrowIf(disposed, this);
                                process?.Dispose();
                                process = candidate;
                                session = new LocalEngineSession(endpoint, true);
                                accepted = true;
                                return session;
                            }
                        }
                        await Task.Delay(100, ready.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (!linked.IsCancellationRequested) { }
                finally { if (!accepted) candidate.Dispose(); }
            }
            throw new IOException("The bundled local engine did not become ready.");
        }
        finally { gate.Release(); }
    }

    private static async Task<bool> ProbeAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            using var api = new LocalOllamaApi(endpoint);
            return await api.IsCompatibleAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException
            || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested) { return false; }
    }

    private static int AvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            process?.Dispose();
            process = null;
            session = null;
        }
    }
}