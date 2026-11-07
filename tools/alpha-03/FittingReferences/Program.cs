using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DzLegal.Alpha03.Measurement;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Harness;
using DzLegal.ModelProviders.Ollama;

if (args.Length == 2 && args[0] == "--check-guard")
{
    try
    {
        await MeasurementGuard.CheckAsync(args[1]);
        Console.WriteLine("Measurement preconditions accepted; no model call.");
        return 0;
    }
    catch (IOException ex)
    {
        Console.WriteLine(ex.Message);
        return 3;
    }
}

if (args.Length != 5 || args[0] is not ("gemma3:4b" or "gemma4:e4b"))
{
    Console.Error.WriteLine("Usage: FittingReferences <model> <frozen cases root> <law root> <external output> <resource run root>");
    return 2;
}

var model = args[0];
var casesRoot = Path.GetFullPath(args[1]);
var lawRoot = Path.GetFullPath(args[2]);
var output = Path.GetFullPath(args[3]);
var runRoot = Path.GetFullPath(args[4]);
var workspace = Path.GetFullPath(Directory.GetCurrentDirectory());
if (output == workspace || output.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Raw measurement outputs must remain outside the repository.");
}

Directory.CreateDirectory(output);
using var frozen = JsonDocument.Parse(File.ReadAllText(Path.Combine(casesRoot, "freeze.json")));
using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(casesRoot, "split-manifest.json")));
var caseBytes = File.ReadAllBytes(Path.Combine(casesRoot, "cases.json"));
if (frozen.RootElement.GetProperty("contentHash").GetString() != "9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5"
    || Convert.ToHexStringLower(SHA256.HashData(caseBytes)) != "415fc5c2f0bcec7dbb41b0e12df7dcd06ccd3d03fb3d6feb8c2dee907275ab67")
{
    throw new InvalidDataException("The owner-adjudicated v3 freeze is required.");
}

var members = manifest.RootElement.GetProperty("members").EnumerateArray()
    .Where(m => m.GetProperty("split").GetString() == "fitting")
    .ToDictionary(m => m.GetProperty("name").GetString()!, m => m.Clone(), StringComparer.Ordinal);
using var source = JsonDocument.Parse(caseBytes);
var cases = source.RootElement.GetProperty("cases").EnumerateArray()
    .Where(c => members.ContainsKey(c.GetProperty("name").GetString()!))
    .Select(c => JsonSerializer.Deserialize<FactCase>(c.GetRawText(), DefinitionJson.Options)!)
    .ToList();
if (cases.Count != 96 || cases.Any(c => c.Partition != "development") || ClaimFactReader.MaxTokens != 300)
{
    throw new InvalidDataException("Expected 96 fitting-only cases and the unchanged 300-token reader cap.");
}

RunManifest.Create("alpha03-fitting-reference", [model],
    new Dictionary<string, string>
    {
        ["prompt"] = Path.Combine(lawRoot, ClaimFactReader.PromptFile),
        ["elements"] = Path.Combine(lawRoot, ClaimFactReader.ElementsFile),
        ["cases"] = Path.Combine(casesRoot, "cases.json"),
        ["split"] = Path.Combine(casesRoot, "split-manifest.json"),
    },
    new Dictionary<string, string>
    {
        ["reader"] = "unchanged Alpha-01, claims given", ["selection"] = "96 fitting manifest members only",
        ["endpoint"] = "http://127.0.0.1:11434", ["thinking"] = model == "gemma4:e4b" ? "false" : "non-thinking model; option omitted",
        ["residence"] = "unload/reload before cases 1,11,...,91; separate CPU/context-matched empty warm-up; no retries", ["maxTokens"] = "300",
        ["operationalStop"] = "power/app/monitor precondition, available physical below 512 MiB or model private + app working set exceeds physical; no retries",
    }).Write(output);

var speed = new SpeedLog();
ModelTimings? lastTiming = null;
using var provider = new OllamaModelProvider(new OllamaOptions { Model = model, Think = model == "gemma4:e4b" ? false : null }, new Recorder(model, output, runRoot))
{
    OnTimings = timing => { lastTiming = timing; speed.Add(timing); },
};
var read = AlphaFacts.Reader(provider, lawRoot, AlphaFacts.Given);
var results = new List<FactCaseResult>();
var observations = new List<object>();
var loads = new List<object>();
var options = new JsonSerializerOptions { WriteIndented = true };
using var reloadClient = new HttpClient(LoopbackGuard.CreateHandler()) { BaseAddress = new Uri("http://127.0.0.1:11434/"), Timeout = TimeSpan.FromMinutes(2) };
var started = DateTimeOffset.UtcNow;
var clock = System.Diagnostics.Stopwatch.StartNew();
var stop = "completed 96 fitting cases";
foreach (var c in cases)
{
    if (results.Count % 10 == 0)
    {
        var reloadClock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var batch = results.Count / 10 + 1;
            await File.WriteAllTextAsync(Path.Combine(runRoot, "phase.txt"), "reload-reference-" + model.Replace(':', '-') + ":batch-" + batch.ToString(CultureInfo.InvariantCulture));
            var prefix = "batch-" + batch.ToString("D2", CultureInfo.InvariantCulture);
            var loadStarted = DateTimeOffset.UtcNow;
            var unloadClock = System.Diagnostics.Stopwatch.StartNew();
            await GenerateAsync(new { model, keep_alive = 0 }, prefix + "-unload");
            unloadClock.Stop();
            await MeasurementGuard.WaitAfterAsync(runRoot, DateTimeOffset.UtcNow);
            await MeasurementGuard.CheckAsync(runRoot);
            var warmClock = System.Diagnostics.Stopwatch.StartNew();
            var warm = await GenerateAsync(new { model, keep_alive = "15m", options = new { num_ctx = 4096, num_gpu = 0 } }, prefix + "-warmup");
            warmClock.Stop();
            await MeasurementGuard.WaitAfterAsync(runRoot, DateTimeOffset.UtcNow);
            await MeasurementGuard.CheckAsync(runRoot);
            loads.Add(new { Batch = batch, FirstCase = c.Name, StartedUtc = loadStarted, FinishedUtc = DateTimeOffset.UtcNow,
                UnloadWallSeconds = unloadClock.Elapsed.TotalSeconds, WarmupWallSeconds = warmClock.Elapsed.TotalSeconds,
                NativeLoadSeconds = warm.TryGetProperty("load_duration", out var duration) ? duration.GetDouble() / 1e9 : (double?)null });
            await File.WriteAllTextAsync(Path.Combine(output, "loads.json"), JsonSerializer.Serialize(loads, options));
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or TaskCanceledException)
        {
            stop = "stopped at planned reload: " + ex.Message;
            await File.WriteAllTextAsync(Path.Combine(output, "reload-failure.json"), JsonSerializer.Serialize(new
            {
                BeforeCase = c.Name, Utc = DateTimeOffset.UtcNow, Seconds = reloadClock.Elapsed.TotalSeconds, Failure = ex.Message,
            }, options));
            break;
        }
    }

    var member = members[c.Name];
    var kind = member.GetProperty("familyKind").GetString();
    await File.WriteAllTextAsync(Path.Combine(runRoot, "phase.txt"), "reference-" + model.Replace(':', '-') + ":" + kind + ":" + c.Name);
    lastTiming = null;
    var report = await FactsEvaluator.RunAsync([c], provider.Name + " (unchanged Alpha-01, fitting)", read, Console.Out, output);
    var result = report.Results.Single();
    results.Add(result);
    var cap = lastTiming?.AnswerTokens >= 300 || result.Answer.Contains("[cut off at the answer length limit]", StringComparison.Ordinal)
        || result.Failure?.Contains("answer length limit", StringComparison.Ordinal) == true;
    observations.Add(new
    {
        c.Name, c.Family, Kind = kind, Fold = member.GetProperty("fold").GetInt32(),
        Batch = (results.Count - 1) / 10 + 1,
        Seconds = result.Elapsed.TotalSeconds, result.Failure, result.HasCritical, result.Errors,
        ReachedCap = cap, Timings = lastTiming, result.Facts, result.Answer,
    });
    await File.WriteAllTextAsync(Path.Combine(output, "case-results.json"), JsonSerializer.Serialize(observations, options));
    if (result.Failure is not null && !cap)
    {
        stop = "stopped at technical/measurement precondition: " + result.Failure;
        break;
    }
    if (lastTiming?.PromptMayBeTruncated == true)
    {
        stop = "stopped: unchanged reference exceeded context allowance";
        break;
    }
}

clock.Stop();
var summary = "# Frozen fitting references\n\n" + FactsEvaluator.Summary(new FactsReport(provider.Name, results), speed.Table());
foreach (var kind in new[] { "polarity", "binding" })
{
    summary += "\n# Family kind: " + kind + "\n\n" + FactsEvaluator.Summary(new FactsReport(provider.Name, results.Where(r => members[r.Case.Name].GetProperty("familyKind").GetString() == kind).ToList()));
}
await File.WriteAllTextAsync(Path.Combine(output, "summary.md"), summary);
await File.WriteAllTextAsync(Path.Combine(output, "completion.json"), JsonSerializer.Serialize(new
{
    Model = model, Planned = 96, Attempted = results.Count, Unattempted = 96 - results.Count,
    StopReason = stop, StartedUtc = started, FinishedUtc = DateTimeOffset.UtcNow, BatchSeconds = clock.Elapsed.TotalSeconds,
    LoadCount = loads.Count, CaseSeconds = results.Sum(r => r.Elapsed.TotalSeconds),
    Complete = results.Count == 96 && stop == "completed 96 fitting cases",
}, options));
Console.WriteLine(stop);
return stop == "completed 96 fitting cases" ? 0 : 3;

async Task<JsonElement> GenerateAsync<T>(T body, string name)
{
    var bytes = JsonSerializer.SerializeToUtf8Bytes(body);
    await File.WriteAllBytesAsync(Path.Combine(output, name + "-request.json"), bytes);
    using var content = new StringContent(System.Text.Encoding.UTF8.GetString(bytes), System.Text.Encoding.UTF8, "application/json");
    using var response = await reloadClient.PostAsync("api/generate", content);
    var raw = await response.Content.ReadAsByteArrayAsync();
    await File.WriteAllBytesAsync(Path.Combine(output, name + "-response.json"), raw);
    response.EnsureSuccessStatusCode();
    using var document = JsonDocument.Parse(raw);
    if (!document.RootElement.GetProperty("done").GetBoolean()
        || document.RootElement.TryGetProperty("eval_count", out var count) && count.GetInt32() != 0)
    {
        throw new IOException("MEASUREMENT PRECONDITION: empty load request unexpectedly generated tokens");
    }

    return document.RootElement.Clone();
}

internal sealed class Recorder(string model, string output, string runRoot) : DelegatingHandler(LoopbackGuard.CreateHandler())
{
    private int _calls;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await MeasurementGuard.CheckAsync(runRoot, cancellationToken);
        var body = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var opts = root.GetProperty("options");
        if (root.GetProperty("model").GetString() != model || !root.GetProperty("stream").GetBoolean()
            || opts.GetProperty("num_predict").GetInt32() != 300 || opts.GetProperty("num_ctx").GetInt32() != 4096 || opts.GetProperty("num_gpu").GetInt32() != 0
            || (model == "gemma4:e4b" ? root.GetProperty("think").ValueKind != JsonValueKind.False : root.TryGetProperty("think", out _)))
        {
            throw new IOException("MEASUREMENT PRECONDITION: reference wire shape changed");
        }
        var ordinal = Interlocked.Increment(ref _calls).ToString("D3", CultureInfo.InvariantCulture);
        await File.WriteAllBytesAsync(Path.Combine(output, "call-" + ordinal + "-request.json"), body, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
