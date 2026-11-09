using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using DzLegal.Alpha03.Measurement;
using DzLegal.Drafting;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.Harness;

/// <summary>Opt-in owner benchmark controls. Case loading, scoring and private output stay inside the harness.</summary>
public sealed class FactRunMeasurement : IDisposable
{
    private readonly string _model;
    private readonly string _root;
    private readonly TextWriter _progress;
    private readonly HttpClient _http;
    private readonly Func<Task> _guard;
    private readonly Func<DateTimeOffset, Task> _wait;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly string[] OverlayKeys = ["actualOverlayGuid", "effectiveOverlayGuid", "configuredAcGuid"];

    public FactRunMeasurement(string model, Uri endpoint, string root, TextWriter progress,
        HttpMessageHandler? handler = null, Func<Task>? guard = null, Func<DateTimeOffset, Task>? wait = null)
    {
        ArgumentNullException.ThrowIfNull(progress);
        _model = model;
        _root = Path.GetFullPath(root);
        _progress = progress;
        _http = new HttpClient(handler ?? LoopbackGuard.CreateHandler())
        { BaseAddress = LoopbackGuard.EnsureLoopback(endpoint), Timeout = TimeSpan.FromMinutes(2) };
        _guard = guard ?? (() => MeasurementGuard.CheckAsync(_root));
        _wait = wait ?? (after => MeasurementGuard.WaitAfterAsync(_root, after));
    }

    public static async Task<int> RunCommandAsync(string casesRoot, string lawRoot, string output, string model,
        bool? think, string measurementRoot, string endpoint, TextWriter progress, string? expectedDigest = null)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var root = Path.GetFullPath(output);
        var workspace = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (root == workspace || root.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || root != Path.GetFullPath(measurementRoot))
        {
            progress.WriteLine("GUARD output and measurement folder must match outside the repository.");
            return 2;
        }

        try
        {
            var digest = ResolveDigest(model, think, expectedDigest);
            Directory.CreateDirectory(root);
            var address = new Uri(endpoint);
            var speed = new SpeedLog();
            using var provider = (OllamaModelProvider)HarnessModels.Create(model, address, TimeSpan.FromMinutes(10), think: think);
            ModelTimings? last = null;
            provider.OnTimings = value => { last = value; speed.Add(value); };
            using var run = new FactRunMeasurement(model, address, root, progress);
            await run._guard();
            var tags = await run._http.GetStringAsync("api/tags");
            using var models = JsonDocument.Parse(tags);
            var found = models.RootElement.GetProperty("models").EnumerateArray().Single(m => m.GetProperty("name").GetString() == model);
            if (found.GetProperty("digest").GetString() != digest)
            {
                throw new InvalidDataException("Installed model digest differs from the frozen candidate.");
            }

            await run.SaveAsync("model-identity.json", new { Model = model, Digest = digest, Think = think });
            var cases = FactsEvaluator.LoadCases(casesRoot);
            if (cases.Count == 0 || cases.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count() != cases.Count)
            {
                throw new InvalidDataException("Expected nonempty cases with unique names.");
            }

            var options = new Dictionary<string, string> { ["reader"] = "alpha-01, claims given", ["endpoint"] = endpoint, ["digest"] = digest,
                ["residence"] = "unload/reload every10 cases;512MiB guard; app resident; mains/Best performance", ["console"] = "counts and guard events only" };
            if (think.HasValue)
            {
                options["think"] = think.Value ? "true" : "false";
            }

            RunManifest.Create("facts", [model], new Dictionary<string, string>
            { ["prompt"] = Path.Combine(lawRoot, ClaimFactReader.PromptFile), ["elements"] = Path.Combine(lawRoot, ClaimFactReader.ElementsFile), ["cases"] = casesRoot }, options).Write(root);
            var read = AlphaFacts.Reader(provider, lawRoot, AlphaFacts.Given);
            var identity = $"Model: {model}; digest: {digest}; think value sent: {(think is false ? "false" : "omitted")}.\n";
            return await run.RunAsync(cases, provider.Name + " (alpha-01, claims given)", async (c, t, ct) =>
            { last = null; return await read(c, t, ct); }, () => last?.PromptMayBeTruncated == true, speed, identity);
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(Path.Combine(root, "harness-error.json"), JsonSerializer.Serialize(new { Type = ex.GetType().Name, ex.Message }, Json));
            progress.WriteLine("GUARD stopped; private details saved by harness.");
            return 3;
        }
    }

    public static string ResolveDigest(string model, bool? think, string? expectedDigest = null)
    {
        var pinned = (model, think) switch
        {
            ("gemma3:4b", null) => "c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291",
            ("gemma4:e4b", false) => "dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c",
            ("gemma4:e2b", false) or ("gemma3n:e2b", null) => null,
            _ => throw new ArgumentException("Measured owner runs require an approved model and its agreed thinking value.", nameof(model)),
        };
        if (pinned is not null)
        {
            if (expectedDigest is not null && !pinned.Equals(expectedDigest, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The original model digest cannot be overridden.", nameof(expectedDigest));
            }
            return pinned;
        }
        if (expectedDigest is not { Length: 64 } || !expectedDigest.All(char.IsAsciiHexDigit))
        {
            throw new ArgumentException("Phone models require their previously recorded SHA-256 digest.", nameof(expectedDigest));
        }
        return expectedDigest.ToLowerInvariant();
    }

    public async Task<int> RunAsync(IReadOnlyList<FactCase> cases, string readerName,
        Func<FactCase, CaseText, CancellationToken, Task<FactReading>> read, Func<bool> contextTruncated, SpeedLog? speed = null, string? identity = null)
    {
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(contextTruncated);
        var results = new List<FactCaseResult>();
        var loads = new List<object>();
        var started = DateTimeOffset.UtcNow;
        var clock = Stopwatch.StartNew();
        string? failure = null;
        _progress.WriteLine($"PROGRESS 0/{cases.Count}");
        try
        {
            for (var i = 0; i < cases.Count; i++)
            {
                await _guard();
                if (i % 10 == 0)
                {
                    var batch = i / 10 + 1;
                    _progress.WriteLine($"GUARD reload block {batch}");
                    await File.WriteAllTextAsync(Path.Combine(_root, "phase.txt"), "reload");
                    var loadClock = Stopwatch.StartNew();
                    await GenerateAsync(new { model = _model, keep_alive = 0 }, $"batch-{batch}-unload");
                    var unload = loadClock.Elapsed.TotalSeconds;
                    await _wait(DateTimeOffset.UtcNow);
                    await _guard();
                    loadClock.Restart();
                    var warm = await GenerateAsync(new { model = _model, keep_alive = "15m", options = new { num_ctx = 4096, num_gpu = 0 } }, $"batch-{batch}-warmup");
                    var wall = loadClock.Elapsed.TotalSeconds;
                    await _wait(DateTimeOffset.UtcNow);
                    await _guard();
                    loads.Add(new { Batch = batch, BeforeOrdinal = i + 1, UnloadWallSeconds = unload, WarmupWallSeconds = wall,
                        NativeLoadSeconds = warm.TryGetProperty("load_duration", out var native) ? native.GetDouble() / 1e9 : (double?)null });
                    await SaveAsync("loads.json", loads);
                }

                await File.WriteAllTextAsync(Path.Combine(_root, "phase.txt"), "reading");
                await _guard();
                var report = await FactsEvaluator.RunAsync([cases[i]], readerName, read, TextWriter.Null, _root);
                results.Add(report.Results.Single());
                _progress.WriteLine($"PROGRESS {results.Count}/{cases.Count}");
                await _guard();
                if (results[^1].Failure is not null || contextTruncated())
                {
                    throw new InvalidDataException("Transport/runtime/context failure; no further case attempted.");
                }
            }
        }
        catch (Exception ex)
        {
            failure = ex.GetType().Name + ": " + ex.Message;
            _progress.WriteLine("GUARD stopped; private details saved by harness.");
        }
        finally
        {
            try { await GenerateAsync(new { model = _model, keep_alive = 0 }, "final-unload"); }
            catch (Exception ex) { failure ??= ex.GetType().Name + ": " + ex.Message; _progress.WriteLine("GUARD final unload failed."); }
        }

        clock.Stop();
        var resources = await ResourcesAsync(started, DateTimeOffset.UtcNow);
        if (resources.InvalidSamples > 0)
        {
            failure ??= "Recorded power/headroom precondition failed within the timed window.";
        }

        var bill = results.Count == 0 ? "n/a" : (clock.Elapsed.TotalSeconds / results.Count).ToString("F3", CultureInfo.InvariantCulture);
        var measurement = $"\n\n## Measurement\n\nMains / Best performance required; reload every10 cases;512MiB guard; app resident.\n\n" +
            $"Full batch including loads/waits: {clock.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)} s; per attempted case: {bill} s. Loads: {loads.Count}.\n\n" +
            "Sampled model/server working-set peak: " + GiB(resources.ModelWorkingSetPeak) + " GiB; private peak: " + GiB(resources.ModelPrivatePeak) +
            " GiB; concurrent model+app working-set peak: " + GiB(resources.ModelAppPeak) + " GiB; minimum machine available: " + GiB(resources.AvailableMinimum) +
            $" GiB. Resource samples: {resources.Samples}; invalid power/headroom samples: {resources.InvalidSamples}. Sampling can miss short peaks.\n\n" +
            (failure is null ? "Run complete.\n" : "Run incomplete; see private measurement metadata.\n");
        await File.WriteAllTextAsync(Path.Combine(_root, "summary.md"), "# Alpha-01 owner run\n\n" + identity + "\n" + FactsEvaluator.Summary(new FactsReport(readerName, results), speed?.Table()) + measurement);
        await SaveAsync("measurement-resources.json", resources);
        await SaveAsync("measurement-completion.json", new { Planned = cases.Count, Attempted = results.Count, Unattempted = cases.Count - results.Count,
            Complete = failure is null && results.Count == cases.Count, Failure = failure, StartedUtc = started, FinishedUtc = DateTimeOffset.UtcNow,
            BatchSeconds = clock.Elapsed.TotalSeconds, CaseSeconds = results.Sum(r => r.Elapsed.TotalSeconds), LoadCount = loads.Count });
        _progress.WriteLine(failure is null ? "GUARD run complete; model unloaded." : "GUARD run incomplete; no retries.");
        return failure is null ? 0 : 3;
    }

    private Task SaveAsync<T>(string name, T value) => File.WriteAllTextAsync(Path.Combine(_root, name), JsonSerializer.Serialize(value, Json));

    private async Task<Resources> ResourcesAsync(DateTimeOffset started, DateTimeOffset finished)
    {
        var result = new Resources();
        var path = Path.Combine(_root, "resources.jsonl");
        if (!File.Exists(path))
        {
            return result; // Numerical/mock runs do not have a Windows resource monitor.
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var r = document.RootElement;
                var time = DateTimeOffset.Parse(r.GetProperty("utc").GetString()!, CultureInfo.InvariantCulture);
                if (time < started || time > finished)
                {
                    continue;
                }

                result.Samples++;
                var model = r.GetProperty("modelWorkingSetBytes").GetInt64();
                var app = r.GetProperty("appWorkingSetBytes").GetInt64();
                var privateBytes = r.GetProperty("modelPrivateBytes").GetInt64();
                var available = r.GetProperty("availablePhysicalBytes").GetInt64();
                result.ModelWorkingSetPeak = Math.Max(result.ModelWorkingSetPeak, model);
                result.ModelPrivatePeak = Math.Max(result.ModelPrivatePeak, privateBytes);
                result.ModelAppPeak = Math.Max(result.ModelAppPeak, model + app);
                result.AvailableMinimum = result.AvailableMinimum is null ? available : Math.Min(result.AvailableMinimum.Value, available);
                var p = r.GetProperty("power");
                const string best = "ded574b5-45a0-4f42-8737-46345c09c238";
                if (p.GetProperty("acLineStatus").GetInt32() != 1 || p.GetProperty("energySaver").GetInt32() != 0
                    || OverlayKeys.Any(k => p.GetProperty(k).GetString() != best)
                    || available < 512L * 1024 * 1024 || app <= 0 || privateBytes + app > r.GetProperty("totalPhysicalBytes").GetInt64())
                {
                    result.InvalidSamples++;
                }
            }
            catch (JsonException) { result.IncompleteSamples++; }
        }

        return result;
    }

    private static string GiB(long? bytes) => bytes is null ? "n/a" : (bytes.Value / 1073741824.0).ToString("F3", CultureInfo.InvariantCulture);

    private sealed class Resources
    {
        public int Samples { get; set; }
        public int InvalidSamples { get; set; }
        public int IncompleteSamples { get; set; }
        public long ModelWorkingSetPeak { get; set; }
        public long ModelPrivatePeak { get; set; }
        public long ModelAppPeak { get; set; }
        public long? AvailableMinimum { get; set; }
    }

    private async Task<JsonElement> GenerateAsync<T>(T body, string stem)
    {
        using var content = System.Net.Http.Json.JsonContent.Create(body);
        await File.WriteAllBytesAsync(Path.Combine(_root, stem + "-request.json"), await content.ReadAsByteArrayAsync());
        using var response = await _http.PostAsync("api/generate", content);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(Path.Combine(_root, stem + "-response.json"), bytes);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(bytes);
        if (!document.RootElement.GetProperty("done").GetBoolean() || document.RootElement.TryGetProperty("eval_count", out var count) && count.GetInt32() != 0)
        {
            throw new InvalidDataException("Empty load/unload generated output.");
        }

        return document.RootElement.Clone();
    }

    public void Dispose() => _http.Dispose();
}
