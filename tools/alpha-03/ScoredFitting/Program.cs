using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DzLegal.Alpha03.Measurement;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Harness;
using DzLegal.ModelProviders.Ollama;

if (args.Length != 7 || args[0] is not ("capability" or "fitting") || args[1] is not ("gemma3:4b" or "gemma4:e4b"))
{
    Console.Error.WriteLine("Usage: ScoredFitting capability|fitting model casesRoot lawRoot promptPath externalOutput resourceRoot");
    return 2;
}

var mode = args[0];
var model = args[1];
var casesRoot = Path.GetFullPath(args[2]);
var promptPath = Path.GetFullPath(args[4]);
var output = Path.GetFullPath(args[5]);
var runRoot = Path.GetFullPath(args[6]);
var workspace = Path.GetFullPath(Directory.GetCurrentDirectory());
if (output == workspace || output.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
    || Directory.Exists(output))
{
    throw new ArgumentException("Use a fresh external output directory; no resume or overwrite.");
}

using var frozen = JsonDocument.Parse(File.ReadAllText(Path.Combine(casesRoot, "freeze.json")));
var caseBytes = File.ReadAllBytes(Path.Combine(casesRoot, "cases.json"));
if (frozen.RootElement.GetProperty("contentHash").GetString() != "9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5"
    || Convert.ToHexStringLower(SHA256.HashData(caseBytes)) != "415fc5c2f0bcec7dbb41b0e12df7dcd06ccd3d03fb3d6feb8c2dee907275ab67")
{
    throw new InvalidDataException("The owner-adjudicated v3 freeze is required.");
}

using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(casesRoot, "split-manifest.json")));
var members = manifest.RootElement.GetProperty("members").EnumerateArray().Where(m => m.GetProperty("split").GetString() == "fitting")
    .ToDictionary(m => m.GetProperty("name").GetString()!, m => m.Clone(), StringComparer.Ordinal);
using var source = JsonDocument.Parse(caseBytes);
// Select fitting members before reading their inputs; gold/annotations never enter the reader.
var inputs = source.RootElement.GetProperty("cases").EnumerateArray().Where(c => members.ContainsKey(c.GetProperty("name").GetString()!))
    .Select(c => new Input(c.GetProperty("name").GetString()!, c.GetProperty("claim").GetString()!,
        JsonSerializer.Deserialize<Dictionary<string, string>>(c.GetProperty("inputs").GetRawText(), DefinitionJson.Options)!)).ToArray();
if (inputs.Length != 96)
{
    throw new InvalidDataException("Expected exactly96 frozen fitting members.");
}

var selected = mode == "capability" ? inputs.Take(1).ToArray() : inputs;
var catalog = ClaimFactReader.LoadElements(args[3]);
Directory.CreateDirectory(output);
RunManifest.Create("alpha03-scored-" + mode, [model], new Dictionary<string, string>
{
    ["prompt"] = promptPath, ["elements"] = Path.Combine(args[3], ClaimFactReader.ElementsFile),
    ["cases"] = Path.Combine(casesRoot, "cases.json"), ["split"] = Path.Combine(casesRoot, "split-manifest.json"),
}, new Dictionary<string, string>
{
    ["selection"] = mode == "capability" ? "first fitting input only, forward and reverse, no semantic selection" : "all96 fitting members, forward and reverse",
    ["carrier"] = "actual literal0, four byte-exact finite pre-mask alternatives; no fallback",
    ["residence"] = "unload/reload every10 complete cases, separate empty CPU/context warm-up; no retries",
    ["power"] = "mains and Best performance; browser/heavy apps closed; app resident;512MiB guard",
}).Write(output);

using var recorder = new ScoreRecorder(output, runRoot);
using var client = new HttpClient(recorder) { BaseAddress = new Uri("http://127.0.0.1:11434/"), Timeout = TimeSpan.FromMinutes(5) };
using var reloadClient = new HttpClient(LoopbackGuard.CreateHandler()) { BaseAddress = client.BaseAddress, Timeout = TimeSpan.FromMinutes(2) };
var reader = new ScoredStatusReader(client, model, File.ReadAllText(promptPath));
var rows = new List<object>();
var loads = new List<object>();
var options = new JsonSerializerOptions { WriteIndented = true };
var started = DateTimeOffset.UtcNow;
var batchClock = Stopwatch.StartNew();
string? failure = null;
string? activeCase = null;
bool? activeReverse = null;
try
{
    foreach (var input in selected)
    {
        activeCase = input.Name;
        activeReverse = null;
        if (rows.Count % 10 == 0)
        {
            var batch = rows.Count / 10 + 1;
            await PhaseAsync("reload-score-" + model.Replace(':', '-') + ":batch-" + batch.ToString(CultureInfo.InvariantCulture));
            await MeasurementGuard.CheckAsync(runRoot);
            var loadStarted = DateTimeOffset.UtcNow;
            var clock = Stopwatch.StartNew();
            await GenerateAsync(new { model, keep_alive = 0 }, "batch-" + batch + "-unload");
            var unload = clock.Elapsed.TotalSeconds;
            await MeasurementGuard.WaitAfterAsync(runRoot, DateTimeOffset.UtcNow);
            await MeasurementGuard.CheckAsync(runRoot);
            clock.Restart();
            var warm = await GenerateAsync(new { model, keep_alive = "15m", options = new { num_ctx = 4096, num_gpu = 0 } }, "batch-" + batch + "-warmup");
            var wall = clock.Elapsed.TotalSeconds;
            await MeasurementGuard.WaitAfterAsync(runRoot, DateTimeOffset.UtcNow);
            await MeasurementGuard.CheckAsync(runRoot);
            loads.Add(new { Batch = batch, FirstCase = input.Name, StartedUtc = loadStarted, FinishedUtc = DateTimeOffset.UtcNow,
                UnloadWallSeconds = unload, WarmupWallSeconds = wall,
                NativeLoadSeconds = warm.TryGetProperty("load_duration", out var native) ? native.GetDouble() / 1e9 : (double?)null });
            await SaveAsync("loads.json", loads);
        }

        var text = CaseText.From(input.Inputs);
        var chains = new List<ScoredStatusChain>();
        var caseStarted = DateTimeOffset.UtcNow;
        var firstCall = recorder.Calls + 1;
        var caseClock = Stopwatch.StartNew();
        foreach (var reverse in new[] { false, true })
        {
            activeReverse = reverse;
            await PhaseAsync("score-" + mode + "-" + model.Replace(':', '-') + ":" + input.Name + ":" + (reverse ? "reverse" : "forward"));
            chains.Add(await reader.ReadAsync(text, catalog[input.Claim], reverse));
        }

        rows.Add(new { input.Name, input.Claim, Kind = members[input.Name].GetProperty("familyKind").GetString(),
            Fold = members[input.Name].GetProperty("fold").GetInt32(), StartedUtc = caseStarted, FinishedUtc = DateTimeOffset.UtcNow,
            FirstCall = firstCall, LastCall = recorder.Calls, Seconds = caseClock.Elapsed.TotalSeconds, Chains = chains });
        await SaveAsync("case-results.json", rows);
        Console.WriteLine(input.Name + ": both chains complete, " + recorder.Calls + " total four-score positions");
    }
}
catch (Exception ex) when (ex is IOException or InvalidDataException or HttpRequestException or TaskCanceledException
    or JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
{
    failure = ex.GetType().Name + ": " + ex.Message;
    await SaveAsync("failure.json", new { Failure = failure, Case = activeCase, Reverse = activeReverse,
        LastNativeCall = recorder.Calls, Utc = DateTimeOffset.UtcNow });
    Console.WriteLine("Stopped on capability/runtime precondition: " + failure);
}

await SaveAsync("completion.json", new { Model = model, Mode = mode, Planned = selected.Length, Completed = rows.Count,
    Complete = failure is null && rows.Count == selected.Length, Failure = failure, StartedUtc = started,
    FinishedUtc = DateTimeOffset.UtcNow, BatchSeconds = batchClock.Elapsed.TotalSeconds, Calls = recorder.Calls, LoadCount = loads.Count,
    StoppedCase = failure is null ? null : activeCase, StoppedReverse = failure is null ? null : activeReverse });
return failure is null ? 0 : 3;

Task PhaseAsync(string phase) => File.WriteAllTextAsync(Path.Combine(runRoot, "phase.txt"), phase);
Task SaveAsync<T>(string name, T value) => File.WriteAllTextAsync(Path.Combine(output, name), JsonSerializer.Serialize(value, options));
async Task<JsonElement> GenerateAsync<T>(T body, string stem)
{
    var bytes = JsonSerializer.SerializeToUtf8Bytes(body);
    await File.WriteAllBytesAsync(Path.Combine(output, stem + "-request.json"), bytes);
    using var content = new ByteArrayContent(bytes);
    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    using var response = await reloadClient.PostAsync("api/generate", content);
    var raw = await response.Content.ReadAsByteArrayAsync();
    await File.WriteAllBytesAsync(Path.Combine(output, stem + "-response.json"), raw);
    response.EnsureSuccessStatusCode();
    using var document = JsonDocument.Parse(raw);
    if (!document.RootElement.GetProperty("done").GetBoolean()
        || document.RootElement.TryGetProperty("eval_count", out var count) && count.GetInt32() != 0)
    {
        throw new IOException("SCORE PRECONDITION: empty load generated tokens.");
    }

    return document.RootElement.Clone();
}

internal sealed record Input(string Name, string Claim, Dictionary<string, string> Inputs);

internal sealed class ScoreRecorder(string output, string runRoot) : DelegatingHandler(LoopbackGuard.CreateHandler())
{
    internal int Calls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await MeasurementGuard.CheckAsync(runRoot, cancellationToken);
        var stem = "call-" + (++Calls).ToString("D4", CultureInfo.InvariantCulture);
        await File.WriteAllBytesAsync(Path.Combine(output, stem + "-request.json"), await request.Content!.ReadAsByteArrayAsync(cancellationToken), cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(output, stem + "-response.json"), await response.Content.ReadAsByteArrayAsync(cancellationToken), cancellationToken);
        return response;
    }
}
