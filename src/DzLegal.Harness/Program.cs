using System.Globalization;
using DzLegal.Domain.Serialization;
using DzLegal.Harness;

// Usage:
//   dzlegal-harness [--data <document-types folder>] [--corpus <corpus folder>]
//       Form Engine accuracy over the corpus. Exit code 1 on any mismatch.
//   Models: a local Ollama model (gemma3:4b, gemma4:e4b), stub, openrouter[:<model>] (OPENROUTER_API_KEY) or
//   google[:<model>] (GOOGLE_API_KEY; default gemma-4-e4b-it). Hosted engines receive the fictitious test cases (docs/ONLINE-TESTING.md).
//   dzlegal-harness ping [--models openrouter[:<model>] | google[:<model>]]
//       OpenRouter: whose limit stops it (key status, then a tiny and a drafting-sized request). Google: which Gemma
//       models the key can call, then a tiny request to the model (default gemma-4-e4b-it). Keys are never shown.
//   Batch options for draft and grounds: --runs N (repeat each case), --case <name> (one case), --delay-seconds S,
//   --rate-limit-wait-seconds S (hosted: wait out "too many requests", default 120 per call),
//   --max-consecutive-failures N (stop a model's batch after N failed cases in a row, default 3; 0 = never).
//   dzlegal-harness draft --models <m1,m2,...|stub> [--out bench] [--cases <folder>] [--endpoint http://127.0.0.1:11434] [--timeout-minutes 15]
//       Drafting bench: runs the drafting cases through each model and writes outputs + summary.md.
//   dzlegal-harness grounds --models <gemma3:4b,...|stub> [--out bench-grounds] [--law-root <data/law>] [--cases <grounds-cases>] [--endpoint ...]
//       Runs the grounds analysis (read facts → search library → choose articles) on fictitious fact patterns and
//       reports claim recognition, expected-article recall (final and by the model alone), time per case, and with
//       --runs, how stable the answers are. Reported only (no gate).
//   dzlegal-harness law import <code-id> (--url <official url> | --file <file>) [--law-root data/law] [--as-of yyyy-mm-dd]
//       Maintainer tool: imports an official text into the law library and writes a review report.
var dataRoot = Path.Combine(AppContext.BaseDirectory, "data", "document-types");
string Option(string name, string fallback)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}

dataRoot = Option("--data", dataRoot);

if (args.Length > 1 && args[0] == "law" && args[1] == "import" && args.Length > 2)
{
    return await LawImport.RunAsync(
        args[2],
        args.Contains("--url") ? Option("--url", string.Empty) : null,
        args.Contains("--file") ? Option("--file", string.Empty) : null,
        Option("--law-root", Path.Combine("data", "law")),
        args.Contains("--as-of") ? Option("--as-of", string.Empty) : null,
        Console.Out);
}

if (args.Length > 0 && args[0] == "ping")
{
    var spec = Option("--models", "openrouter");
    if (spec == "google" || spec.StartsWith("google:", StringComparison.Ordinal))
    {
        if (Environment.GetEnvironmentVariable("GOOGLE_API_KEY") is not { Length: > 0 } googleKey)
        {
            Console.Error.WriteLine("ping: set the GOOGLE_API_KEY environment variable first.");
            return 2;
        }

        return await GoogleDiagnostics.RunAsync(
            new DzLegal.ModelProviders.Hosted.GoogleAiOptions { ApiKey = googleKey.Trim(), Model = HarnessModels.GoogleModel(spec), Endpoint = HarnessModels.GoogleEndpoint },
            Console.Out);
    }

    if (spec != "openrouter" && !spec.StartsWith("openrouter:", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("ping: --models openrouter[:<model>] or google[:<model>].");
        return 2;
    }

    if (Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") is not { Length: > 0 } key)
    {
        Console.Error.WriteLine("ping: set the OPENROUTER_API_KEY environment variable first.");
        return 2;
    }

    var endpoint = Environment.GetEnvironmentVariable("OPENROUTER_ENDPOINT") is { Length: > 0 } e ? new Uri(e) : new Uri("https://openrouter.ai/api/v1/");
    var model = spec.Length > "openrouter:".Length ? spec["openrouter:".Length..] : HarnessModels.DefaultOpenRouterModel;
    return await OpenRouterDiagnostics.RunAsync(endpoint, key.Trim(), model, Console.Out);
}

if (args.Length > 0 && args[0] == "grounds")
{
    var models = Option("--models", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (models.Length == 0)
    {
        Console.Error.WriteLine("grounds: --models is required, e.g. --models gemma3:4b, openrouter, google:gemma-4-26b-a4b-it (or 'stub').");
        return 2;
    }

    var runs = Math.Max(1, int.Parse(Option("--runs", "1"), CultureInfo.InvariantCulture));
    // Unset: 3 s between calls for a hosted engine (its shared capacity), none for a local one.
    var delaySeconds = args.Contains("--delay-seconds") ? double.Parse(Option("--delay-seconds", "0"), CultureInfo.InvariantCulture) : -1;
    var rateLimitWait = TimeSpan.FromSeconds(double.Parse(Option("--rate-limit-wait-seconds", "120"), CultureInfo.InvariantCulture));
    var maxFailures = int.Parse(Option("--max-consecutive-failures", "3"), CultureInfo.InvariantCulture);
    var knowledge = DzLegal.Law.LegalKnowledgeLoader.Load(Option("--law-root", Path.Combine(AppContext.BaseDirectory, "data", "law")));
    var outRoot = args.Contains("--out") ? Option("--out", "bench-grounds") : null;
    if (outRoot is not null)
    {
        Directory.CreateDirectory(outRoot);
        File.WriteAllText(Path.Combine(outRoot, "summary.md"), $"# Grounds analysis bench{Environment.NewLine}{Environment.NewLine}");
    }

    var groundsCases = GroundsEvaluator.LoadCases(Option("--cases", Path.Combine(AppContext.BaseDirectory, "grounds-cases")));
    if (args.Contains("--case"))
    {
        groundsCases = groundsCases.Where(c => c.Name == Option("--case", string.Empty)).ToList();
        if (groundsCases.Count == 0)
        {
            Console.Error.WriteLine($"grounds: no case named '{Option("--case", string.Empty)}'.");
            return 2;
        }
    }

    foreach (var model in models)
    {
        Console.WriteLine($"Grounds analysis with {model}: {groundsCases.Count} case(s) × {runs} run(s), about {groundsCases.Count * runs * 2} model calls");
        if (HarnessModels.IsHosted(model))
        {
            Console.WriteLine("  Hosted engine: the (fictitious) test cases are sent to that service.");
        }

        var speed = new SpeedLog();
        var reports = new List<GroundsReport>();
        for (var run = 1; run <= runs; run++)
        {
            DzLegal.Drafting.IModelProvider provider;
            try
            {
                provider = HarnessModels.Create(model, new Uri(Option("--endpoint", "http://127.0.0.1:11434")), TimeSpan.FromMinutes(10), speed, HarnessModels.Waiting(rateLimitWait, Console.Out));
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"grounds: {ex.Message}");
                return 2;
            }

            using var paced = new PacedModelProvider(provider, HarnessModels.Pace(model, delaySeconds));
            if (runs > 1)
            {
                Console.WriteLine($" run {run}/{runs}");
            }

            var runFolder = outRoot is null ? null : runs > 1 ? Path.Combine(outRoot, $"run-{run}") : outRoot;
            var report = await GroundsEvaluator.EvaluateAsync(knowledge, groundsCases, paced, Console.Out, runFolder, maxFailures);
            GroundsEvaluator.Print(report, knowledge, Console.Out);
            reports.Add(report);
            if (report.StoppedEarly)
            {
                Console.WriteLine($"  {model}: remaining runs skipped; try again later or with --delay-seconds.");
                break;
            }
        }

        if (reports.Count > 1)
        {
            var table = GroundsEvaluator.StabilityTable(model, GroundsEvaluator.Stability(reports));
            Console.WriteLine(table);
            if (outRoot is not null)
            {
                File.AppendAllText(Path.Combine(outRoot, "summary.md"), table);
            }
        }

        Console.WriteLine(speed.Table());
        if (outRoot is not null)
        {
            File.AppendAllText(
                Path.Combine(outRoot, "summary.md"),
                $"### Where the time goes ({model}){Environment.NewLine}{Environment.NewLine}{speed.Table()}{Environment.NewLine}");
        }

        Console.WriteLine();
    }

    return 0;
}

if (args.Length > 0 && args[0] == "draft")
{
    var models = Option("--models", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (models.Length == 0)
    {
        Console.Error.WriteLine("draft: --models is required, e.g. --models gemma3:4b,qwen2.5:3b-instruct (or 'stub').");
        return 2;
    }

    await DraftBench.RunAsync(
        new BenchOptions
        {
            Models = models,
            DataRoot = dataRoot,
            CasesFolder = Option("--cases", Path.Combine(AppContext.BaseDirectory, "drafting-cases")),
            OutFolder = Option("--out", "bench"),
            Endpoint = new Uri(Option("--endpoint", "http://127.0.0.1:11434")),
            Timeout = TimeSpan.FromMinutes(double.Parse(Option("--timeout-minutes", "15"), CultureInfo.InvariantCulture)),
            Runs = Math.Max(1, int.Parse(Option("--runs", "1"), CultureInfo.InvariantCulture)),
            Case = args.Contains("--case") ? Option("--case", string.Empty) : null,
            DelaySeconds = args.Contains("--delay-seconds") ? double.Parse(Option("--delay-seconds", "0"), CultureInfo.InvariantCulture) : -1,
            LawRoot = Option("--law-root", Path.Combine(AppContext.BaseDirectory, "data", "law")),
            RateLimitWait = TimeSpan.FromSeconds(double.Parse(Option("--rate-limit-wait-seconds", "120"), CultureInfo.InvariantCulture)),
            MaxConsecutiveFailures = int.Parse(Option("--max-consecutive-failures", "3"), CultureInfo.InvariantCulture),
        },
        Console.Out);
    return 0;
}

var corpusRoot = Option("--corpus", Path.Combine(AppContext.BaseDirectory, "corpus"));
var definitions = DefinitionLoader.LoadAll(dataRoot);
var cases = CorpusLoader.LoadAll(corpusRoot);
var allPerfect = true;
var ci = CultureInfo.InvariantCulture;

foreach (var definition in definitions)
{
    var result = CorpusEvaluator.Evaluate(definition, cases);
    Console.WriteLine($"Document type: {definition.Id} (rules v{definition.Version}) — {result.CaseCount} cases");
    Console.WriteLine($"{"element",-22} {"cases",5} {"acc",7} {"prec",7} {"recall",7}");
    foreach (var s in result.Scores)
    {
        Console.WriteLine(string.Format(ci, "{0,-22} {1,5} {2,7:P0} {3,7:P0} {4,7:P0}", s.ElementId, s.Cases, s.Accuracy, s.Precision, s.Recall));
    }

    Console.WriteLine(string.Format(ci, "Overall: {0}/{1} element checks correct ({2:P1})", result.Scores.Sum(s => s.Correct), result.Checks, result.OverallAccuracy));
    foreach (var m in result.Mismatches)
    {
        Console.WriteLine($"  MISMATCH [{m.CaseName}] {m.ElementId}: expected {m.Expected}, got {m.Actual}");
    }

    Console.WriteLine();
    allPerfect &= result.Perfect;
}

return allPerfect ? 0 : 1;
