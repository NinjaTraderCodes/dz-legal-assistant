using System.Globalization;
using DzLegal.Domain.Serialization;
using DzLegal.Harness;

// Usage: dzlegal-harness [--data <document-types folder>] [--corpus <corpus folder>]
// Exit code 0 when every element of every case matches its expected outcome, 1 otherwise.
var dataRoot = Path.Combine(AppContext.BaseDirectory, "data", "document-types");
var corpusRoot = Path.Combine(AppContext.BaseDirectory, "corpus");
for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--data": dataRoot = args[++i]; break;
        case "--corpus": corpusRoot = args[++i]; break;
    }
}

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
