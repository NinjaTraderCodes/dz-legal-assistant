using System.Globalization;
using System.Text.Json;
using DzLegal.Drafting;
using DzLegal.Harness;
using DzLegal.ModelProviders.Ollama;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: ModelCheck <original fact-cases folder> <law root> <external output folder>");
    return 2;
}

var casesRoot = Path.GetFullPath(args[0]);
var lawRoot = Path.GetFullPath(args[1]);
var output = Path.GetFullPath(args[2]);
var workspace = Path.GetFullPath(Directory.GetCurrentDirectory()) + Path.DirectorySeparatorChar;
if (output.StartsWith(workspace, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Measurement outputs must be outside the repository.");
}

Directory.CreateDirectory(output);
var elements = ClaimFactReader.LoadElements(lawRoot);
var cases = FactsEvaluator.LoadCases(casesRoot)
    .Where(c => c.Partition == "development" && elements.ContainsKey(c.Claim))
    .ToList();
if (cases.Count != 40 || ClaimFactReader.MaxTokens != 300)
{
    throw new InvalidDataException("Expected the original 40 in-catalogue development cases and the unchanged 300-token cap.");
}

RunManifest.Create("alpha03-gemma4-think-off", ["gemma4:e4b"],
    new Dictionary<string, string>
    {
        ["prompt"] = Path.Combine(lawRoot, ClaimFactReader.PromptFile),
        ["elements"] = Path.Combine(lawRoot, ClaimFactReader.ElementsFile),
        ["cases"] = casesRoot,
    },
    new Dictionary<string, string>
    {
        ["reader"] = "unchanged alpha-01, claims given",
        ["partition"] = "development, in-catalogue only",
        ["endpoint"] = "http://127.0.0.1:11434",
        ["think"] = "false",
        ["maxTokens"] = "300",
        ["stop"] = "first cap or technical failure; no higher cap",
    }).Write(output);

var options = new OllamaOptions
{
    Model = "gemma4:e4b",
    Endpoint = new Uri("http://127.0.0.1:11434"),
    Timeout = TimeSpan.FromMinutes(10),
    Think = false,
};
var speed = new SpeedLog();
ModelTimings? lastTiming = null;
using var provider = new OllamaModelProvider(options, new RequestRecorder(output))
{
    OnTimings = timing => { speed.Add(timing); lastTiming = timing; },
};
var read = AlphaFacts.Reader(provider, lawRoot, AlphaFacts.Given);
var name = provider.Name + " (alpha-01, claims given, think false)";
var results = new List<FactCaseResult>();
var observations = new List<object>();
var json = new JsonSerializerOptions { WriteIndented = true };
var stopReason = "completed all forty cases";
var reachedCap = false;
var batchClock = System.Diagnostics.Stopwatch.StartNew();
foreach (var c in cases)
{
    lastTiming = null;
    var one = await FactsEvaluator.RunAsync([c], name, read, Console.Out, output);
    var result = one.Results.Single();
    results.Add(result);
    var cap = lastTiming?.AnswerTokens >= ClaimFactReader.MaxTokens
        || result.Answer.Contains("[cut off at the answer length limit]", StringComparison.Ordinal)
        || result.Failure?.Contains("answer length limit", StringComparison.Ordinal) == true;
    observations.Add(new
    {
        c.Name, c.Claim, Seconds = result.Elapsed.TotalSeconds, result.Failure,
        result.HasCritical, result.Errors, ReachedCap = cap, Timings = lastTiming,
    });
    await File.WriteAllTextAsync(Path.Combine(output, "case-results.json"), JsonSerializer.Serialize(observations, json));
    if (cap || result.Failure is not null)
    {
        reachedCap = cap;
        stopReason = cap ? "stopped at the unchanged 300-token cap" : "stopped at a technical failure";
        break;
    }
}

batchClock.Stop();
var report = new FactsReport(name, results);
var summary = "# Alpha-03 thinking-off model check\n\n"
    + FactsEvaluator.Summary(report, speed.Table())
    + $"\nPlanned: 40 cases. Attempted: {results.Count}. Unattempted: {40 - results.Count}. Stop: {stopReason}.\n";
Console.WriteLine(summary);
await File.WriteAllTextAsync(Path.Combine(output, "summary.md"), summary);
await File.WriteAllTextAsync(Path.Combine(output, "completion.json"), JsonSerializer.Serialize(new
{
    PlannedCases = 40, AttemptedCases = results.Count, UnattemptedCases = 40 - results.Count,
    StopReason = stopReason, ReachedCap = reachedCap, Think = false, ReaderCap = 300,
    BatchSeconds = batchClock.Elapsed.TotalSeconds,
}, json));
return 0;

internal sealed class RequestRecorder(string folder) : DelegatingHandler(LoopbackGuard.CreateHandler())
{
    private int _calls;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await (request.Content ?? throw new InvalidDataException("Missing request body."))
            .ReadAsByteArrayAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var options = root.GetProperty("options");
        if (root.GetProperty("model").GetString() != "gemma4:e4b"
            || root.GetProperty("think").ValueKind != JsonValueKind.False
            || !root.GetProperty("stream").GetBoolean()
            || options.GetProperty("num_predict").GetInt32() != 300
            || options.GetProperty("num_ctx").GetInt32() != 4096
            || options.GetProperty("num_gpu").GetInt32() != 0)
        {
            throw new InvalidDataException("The wire request differs from the authorised thinking-off shape.");
        }

        var ordinal = Interlocked.Increment(ref _calls).ToString("D3", CultureInfo.InvariantCulture);
        await File.WriteAllBytesAsync(Path.Combine(folder, "call-" + ordinal + "-request.json"), body, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
