using System.Globalization;
using DzLegal.Domain.Serialization;
using DzLegal.Harness;

// Usage:
//   dzlegal-harness [--data <document-types folder>] [--corpus <corpus folder>]
//       Form Engine accuracy over the corpus. Exit code 1 on any mismatch.
//   dzlegal-harness draft --models <m1,m2,...|stub> [--out bench] [--cases <folder>] [--endpoint http://127.0.0.1:11434] [--timeout-minutes 15]
//       Drafting bench: runs the drafting cases through each local model and writes outputs + summary.md.
var dataRoot = Path.Combine(AppContext.BaseDirectory, "data", "document-types");
string Option(string name, string fallback)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}

dataRoot = Option("--data", dataRoot);

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
