using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DzLegal.Alpha03.Measurement;
using DzLegal.Drafting;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.Harness;

/// <summary>Opt-in controls around the existing app benchmarks; never used by their default paths.</summary>
public sealed class BenchmarkMeasurement : IAsyncDisposable
{
    private readonly string _model;
    private readonly string _root;
    private readonly TextWriter _log;
    private readonly HttpClient _http;
    private readonly Func<Task> _guard;
    private readonly Func<DateTimeOffset, Task> _wait;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DateTimeOffset _started = DateTimeOffset.UtcNow;
    private readonly List<object> _loads = [];
    private readonly List<object> _calls = [];
    private readonly List<object> _cases = [];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private string? _pending;
    private string? _failure;
    private bool _complete;
    private ModelTimings? _last;
    private int _sharingRetries;
    private int? _failureHResult;
    private Exception? _requestError;

    public BenchmarkMeasurement(string model, Uri endpoint, string root, TextWriter log,
        HttpMessageHandler? handler = null, Func<Task>? guard = null, Func<DateTimeOffset, Task>? wait = null)
    {
        ArgumentNullException.ThrowIfNull(log);
        _model = model;
        _root = root;
        _log = log;
        _http = new HttpClient(handler ?? LoopbackGuard.CreateHandler())
        { BaseAddress = LoopbackGuard.EnsureLoopback(endpoint), Timeout = TimeSpan.FromMinutes(2) };
        _guard = guard ?? (() => MeasurementGuard.CheckAsync(root));
        _wait = wait ?? (after => MeasurementGuard.WaitAfterAsync(root, after));
    }

    public static BenchmarkMeasurement Create(string model, Uri endpoint, string root, string? output,
        int modelCount, int runs, TextWriter log)
    {
        var workspace = Path.GetFullPath(Directory.GetCurrentDirectory()).TrimEnd(Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        if (output is null || !full.Equals(Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
            || full.Equals(workspace, StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || modelCount != 1 || runs != 1 || model == "stub" || HarnessModels.IsHosted(model))
        {
            throw new ArgumentException("Measured runs require one local model, one run, and matching output/monitor folders outside the repository.");
        }

        return new BenchmarkMeasurement(model, endpoint, full, log);
    }

    public static Dictionary<string, string> Options(Dictionary<string, string> options, bool? think, string? root)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (think.HasValue) { options["think"] = think.Value ? "true" : "false"; }
        if (root is not null) { options["measurement-root"] = root; options["residence"] = "reload every10 cases;512MiB;app resident;mains/Best performance"; }
        return options;
    }

    public IModelProvider Guard(IModelProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (provider is OllamaModelProvider local)
        {
            var previous = local.OnTimings;
            local.OnTimings = timing => { previous?.Invoke(timing); _last = timing; };
        }

        return new Guarded(this, provider);
    }

    public async Task BeforeCaseAsync(string name)
    {
        await CheckAsync();
        if (_pending is not null) { throw new InvalidOperationException("Previous measured case has not finished."); }
        _pending = name;
        var ordinal = _cases.Count + 1;
        if (_cases.Count % 10 == 0)
        {
            await File.WriteAllTextAsync(Path.Combine(_root, "phase.txt"), "reload");
            _log.WriteLine($"GUARD reload before case {ordinal}");
            var clock = Stopwatch.StartNew();
            await GenerateAsync(new { model = _model, keep_alive = 0 }, $"load-{ordinal}-unload");
            var unload = clock.Elapsed.TotalSeconds;
            await _wait(DateTimeOffset.UtcNow);
            await CheckAsync();
            clock.Restart();
            var native = await GenerateAsync(new { model = _model, keep_alive = "15m", options = new { num_ctx = 4096, num_gpu = 0 } }, $"load-{ordinal}-warmup");
            var warm = clock.Elapsed.TotalSeconds;
            await _wait(DateTimeOffset.UtcNow);
            await CheckAsync();
            _loads.Add(new { BeforeOrdinal = ordinal, UnloadWallSeconds = unload, WarmupWallSeconds = warm,
                NativeLoadSeconds = native.TryGetProperty("load_duration", out var duration) ? duration.GetDouble() / 1e9 : (double?)null });
            await SaveAsync("measurement-loads.json", _loads);
        }

        await File.WriteAllTextAsync(Path.Combine(_root, "phase.txt"), "reading");
        await CheckAsync();
    }

    public async Task AfterCaseAsync(string name, TimeSpan elapsed, string? error, object metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        if (_pending != name) { throw new InvalidOperationException("Measured case boundary mismatch."); }
        _cases.Add(new { Name = name, Seconds = elapsed.TotalSeconds, Error = error, Metrics = metrics });
        _pending = null;
        await SaveAsync("measurement-cases.json", _cases);
        _log.WriteLine($"PROGRESS {_cases.Count}");
        if (error is not null) { _failure ??= error; }
        await CheckAsync();
    }

    public void Complete() => _complete = true;

    public void Fail(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (_failure is null)
        {
            _failure = error.GetType().Name + ": " + error.Message;
            _failureHResult = error.HResult;
        }
    }

    private async Task CheckAsync()
    {
        if (_failure is not null) { throw new IOException(_failure); }
        for (var attempt = 0; ; attempt++)
        {
            try { await _guard(); return; }
            catch (IOException error) when (attempt < 10 && (error.HResult & 0xffff) is 32 or 33)
            {
                // PowerShell's append handle can briefly deny reads. Re-read fresh state; never retry a model call.
                _sharingRetries++;
                await Task.Delay(25);
            }
            catch (Exception error) { Fail(error); throw; }
        }
    }

    private async Task BeforeRequestAsync()
    {
        await CheckAsync();
        _last = null;
        _requestError = null;
    }

    private async Task AfterRequestAsync(ModelRequest request, double seconds, bool stream)
    {
        var cap = Math.Min(request.MaxTokens ?? 1500, 1500);
        _calls.Add(new { Case = _pending, request.SectionId, Stream = stream, Cap = cap, Seconds = seconds,
            Native = _last, LimitReached = _last is null ? (bool?)null : _last.AnswerTokens >= cap,
            RequestError = _requestError is null ? null : new
            {
                Type = _requestError.GetType().Name, _requestError.Message,
                CutOff = _requestError is ModelProviderException { IsCutOff: true },
                Timeout = IsTimeout(_requestError),
                EmptyAnswer = _requestError.Message.Contains("empty answer", StringComparison.OrdinalIgnoreCase),
            } });
        await SaveAsync("measurement-calls.json", _calls);
        if (_last?.PromptMayBeTruncated == true) { _failure ??= "Native context precondition failed."; }
        await CheckAsync();
    }

    private Task SaveAsync<T>(string name, T value) => File.WriteAllTextAsync(Path.Combine(_root, name), JsonSerializer.Serialize(value, Json));

    private void RequestFailed(Exception error)
    {
        _requestError = error;
        if (error is not ModelProviderException { IsCutOff: true }) { Fail(error); }
    }

    private static bool IsTimeout(Exception? error) => error is TimeoutException or TaskCanceledException
        || error?.InnerException is { } inner && IsTimeout(inner);

    private async Task<bool> MoveNextAsync(IAsyncEnumerator<string> stream)
    {
        try { return await stream.MoveNextAsync(); }
        catch (Exception error) { RequestFailed(error); throw; }
    }

    private async Task<JsonElement> GenerateAsync<T>(T body, string name)
    {
        using var content = System.Net.Http.Json.JsonContent.Create(body);
        await File.WriteAllBytesAsync(Path.Combine(_root, name + "-request.json"), await content.ReadAsByteArrayAsync());
        using var response = await _http.PostAsync("api/generate", content);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(Path.Combine(_root, name + "-response.json"), bytes);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(bytes);
        if (!document.RootElement.GetProperty("done").GetBoolean()
            || document.RootElement.TryGetProperty("eval_count", out var count) && count.GetInt32() != 0)
        {
            throw new InvalidDataException("Empty load/unload generated tokens.");
        }

        return document.RootElement.Clone();
    }

    public async ValueTask DisposeAsync()
    {
        var cleanupFailed = false;
        try { await GenerateAsync(new { model = _model, keep_alive = 0 }, "final-unload"); }
        catch (Exception error) { Fail(error); cleanupFailed = true; }
        _clock.Stop();
        await SaveAsync("measurement-completion.json", new { Model = _model, StartedUtc = _started, FinishedUtc = DateTimeOffset.UtcNow,
            Complete = _complete && _failure is null && _pending is null, Failure = _failure, PendingCase = _pending,
            Cases = _cases.Count, Calls = _calls.Count, Loads = _loads.Count, BatchSeconds = _clock.Elapsed.TotalSeconds,
            ResourceSharingRetries = _sharingRetries, FailureHResult = _failureHResult });
        _http.Dispose();
        if (cleanupFailed && _complete) { throw new IOException("Final model unload failed; measured run incomplete."); }
    }

    private sealed class Guarded(BenchmarkMeasurement owner, IModelProvider inner) : IModelProvider, IDisposable
    {
        public string Name => inner.Name;
        public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
        {
            await owner.BeforeRequestAsync();
            var clock = Stopwatch.StartNew();
            try { return await inner.GenerateAsync(request, cancellationToken); }
            catch (Exception error) { owner.RequestFailed(error); throw; }
            finally { await owner.AfterRequestAsync(request, clock.Elapsed.TotalSeconds, false); }
        }

        public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await owner.BeforeRequestAsync();
            var clock = Stopwatch.StartNew();
            await using var stream = inner.StreamAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);
            try { while (await owner.MoveNextAsync(stream)) { yield return stream.Current; } }
            finally { await owner.AfterRequestAsync(request, clock.Elapsed.TotalSeconds, true); }
        }

        public void Dispose() => (inner as IDisposable)?.Dispose();
    }
}
