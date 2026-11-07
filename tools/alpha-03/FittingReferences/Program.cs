using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Harness;
using DzLegal.ModelProviders.Ollama;

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
if (frozen.RootElement.GetProperty("contentHash").GetString() != "86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642"
    || Convert.ToHexStringLower(SHA256.HashData(caseBytes)) != "bd2ebc0c649c92375100f18f8eb533f03d0e4f3de102652c9084a1ab2f191204")
{
    throw new InvalidDataException("The reviewed second freeze is required.");
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
        ["residence"] = "fresh batch start, resident across cases; no per-case reset", ["maxTokens"] = "300",
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
var options = new JsonSerializerOptions { WriteIndented = true };
var started = DateTimeOffset.UtcNow;
var clock = System.Diagnostics.Stopwatch.StartNew();
var stop = "completed 96 fitting cases";
foreach (var c in cases)
{
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
    Complete = results.Count == 96 && stop == "completed 96 fitting cases",
}, options));
Console.WriteLine(stop);
return stop == "completed 96 fitting cases" ? 0 : 3;

internal sealed class Recorder(string model, string output, string runRoot) : DelegatingHandler(LoopbackGuard.CreateHandler())
{
    private int _calls;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var resourcePath = Path.Combine(runRoot, "resources.jsonl");
        using (var stream = new FileStream(resourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            stream.Seek(-Math.Min(stream.Length, 65536), SeekOrigin.End);
            using var reader = new StreamReader(stream);
            var lines = (await reader.ReadToEndAsync(cancellationToken)).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            JsonDocument? resource = null;
            foreach (var line in lines.Reverse())
            {
                try { resource = JsonDocument.Parse(line); break; }
                catch (JsonException) { }
            }
            using var sample = resource ?? throw new IOException("MEASUREMENT PRECONDITION: no resource sample");
            var r = sample.RootElement;
            var p = r.GetProperty("power");
            const string best = "ded574b5-45a0-4f42-8737-46345c09c238";
            if ((DateTimeOffset.UtcNow - DateTimeOffset.Parse(r.GetProperty("utc").GetString()!, CultureInfo.InvariantCulture)).TotalSeconds > 10
                || p.GetProperty("acLineStatus").GetInt32() != 1 || p.GetProperty("actualOverlayGuid").GetString() != best
                || p.GetProperty("effectiveOverlayGuid").GetString() != best || p.GetProperty("configuredAcGuid").GetString() != best
                || r.GetProperty("appWorkingSetBytes").GetInt64() <= 0 || r.GetProperty("availablePhysicalBytes").GetInt64() < 512L * 1024 * 1024
                || r.GetProperty("modelPrivateBytes").GetInt64() + r.GetProperty("appWorkingSetBytes").GetInt64() > r.GetProperty("totalPhysicalBytes").GetInt64())
            {
                throw new IOException("MEASUREMENT PRECONDITION: power, app, monitor or physical-memory headroom failed; see resource log");
            }
        }
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
