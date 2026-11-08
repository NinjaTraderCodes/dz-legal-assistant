using System.Security.Cryptography;
using System.Text.Json;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Harness;

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: StatusEvaluation frozenCasesRoot firstReferenceRoot statusPredictions externalOutputFile");
    return 2;
}

var output = Path.GetFullPath(args[3]);
var workspace = Path.GetFullPath(Directory.GetCurrentDirectory());
if (File.Exists(output) || output == workspace || output.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Use a new external output file; no overwrite.");
}

var bytes = File.ReadAllBytes(Path.Combine(args[0], "cases.json"));
var splitBytes = File.ReadAllBytes(Path.Combine(args[0], "split-manifest.json"));
if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != "415fc5c2f0bcec7dbb41b0e12df7dcd06ccd3d03fb3d6feb8c2dee907275ab67"
    || Convert.ToHexStringLower(SHA256.HashData(splitBytes)) != "1a9b132b481c7e3719f5f66b8f955c5aee56a2c16fc0e76b945520ef033e762f")
{
    throw new InvalidDataException("Expected the immutable v3 fitting split.");
}

using var manifest = JsonDocument.Parse(splitBytes);
var members = manifest.RootElement.GetProperty("members").EnumerateArray().Where(m => m.GetProperty("split").GetString() == "fitting")
    .ToDictionary(m => m.GetProperty("name").GetString()!, m => m.Clone(), StringComparer.Ordinal);
using var source = JsonDocument.Parse(bytes);
var cases = source.RootElement.GetProperty("cases").EnumerateArray().Where(c => members.ContainsKey(c.GetProperty("name").GetString()!))
    .Select(c => JsonSerializer.Deserialize<FactCase>(c.GetRawText(), DefinitionJson.Options)!)
    .ToDictionary(c => c.Name, StringComparer.Ordinal);
if (cases.Count != 96)
{
    throw new InvalidDataException("Only the96 fitting cases may be evaluated here.");
}

var catalog = ClaimFactReader.LoadElements(Path.Combine(workspace, "data", "law"));
var references = new Dictionary<string, Dictionary<string, JsonElement>>(StringComparer.Ordinal);
foreach (var model in new[] { "gemma3:4b", "gemma4:e4b" })
{
    var baselineBytes = File.ReadAllBytes(Path.Combine(args[1], "reference-" + model.Replace(':', '-'), "case-results.json"));
    var expected = model == "gemma3:4b" ? "0dc3123e10bcedfc77566f1c71bec35925d4e446dc327ebf9b51d221a09ff91b"
        : "67617d6569064b6f17933da5427555e4ebf0494bc9b3afdd527d2c3e32f9f7b9";
    if (Convert.ToHexStringLower(SHA256.HashData(baselineBytes)) != expected)
    {
        throw new InvalidDataException("Only the first complete semantic reference is permitted: " + model);
    }

    using var document = JsonDocument.Parse(baselineBytes);
    var rows = document.RootElement.EnumerateArray().ToDictionary(r => r.GetProperty("Name").GetString()!, r => r.Clone(), StringComparer.Ordinal);
    if (rows.Count != 96 || !rows.Keys.Order(StringComparer.Ordinal).SequenceEqual(cases.Keys.Order(StringComparer.Ordinal)))
    {
        throw new InvalidDataException("Reference members differ from fitting inputs.");
    }

    foreach (var (name, row) in rows)
    {
        var old = JsonSerializer.Deserialize<CaseFacts>(row.GetProperty("Facts").GetRawText(), DefinitionJson.Options)!;
        var errors = FactsEvaluator.Score(cases[name].Gold, old);
        var saved = JsonSerializer.Deserialize<List<FactError>>(row.GetProperty("Errors").GetRawText(), DefinitionJson.Options)!;
        if (!Sorted(errors).SequenceEqual(Sorted(saved)))
        {
            throw new InvalidDataException("Offline evaluator did not reproduce the exact first reference errors: " + name);
        }
    }

    references[model] = rows;
}

using var predictions = JsonDocument.Parse(File.ReadAllText(args[2]));
if (predictions.RootElement.GetProperty("corpusHash").GetString() != "9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5")
{
    throw new InvalidDataException("Predictions target another corpus.");
}

var results = new List<object>();
foreach (var candidate in predictions.RootElement.GetProperty("candidates").EnumerateArray())
{
    var model = candidate.GetProperty("model").GetString()!;
    var choices = candidate.GetProperty("choices");
    if (choices.EnumerateObject().Count() != 96 || !references.TryGetValue(model, out var modelRows))
    {
        throw new InvalidDataException("Incomplete candidate or undeclared model.");
    }

    var rows = new List<object>();
    foreach (var (name, c) in cases)
    {
        var selected = choices.GetProperty(name).EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32(), StringComparer.Ordinal);
        var baseline = JsonSerializer.Deserialize<CaseFacts>(modelRows[name].GetProperty("Facts").GetRawText(), DefinitionJson.Options)!;
        var facts = ScoredStatusReader.Apply(baseline, catalog[c.Claim], selected);
        var errors = FactsEvaluator.Score(c.Gold, facts);
        rows.Add(new { Name = name, Kind = members[name].GetProperty("familyKind").GetString(), Fold = members[name].GetProperty("fold").GetInt32(),
            StatusChoices = selected, Facts = facts, Errors = errors, HasCritical = errors.Any(e => e.Critical), Failure = (string?)null,
            ReachedCap = false, Answer = string.Empty, Seconds = 0.0 });
    }

    results.Add(new { Model = model, Arm = candidate.GetProperty("arm").GetString(), Order = candidate.GetProperty("order").GetString(),
        Regularization = candidate.GetProperty("regularization").Clone(), Rows = rows });
}

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("Evaluated all fitting-only status ablations offline; first reference errors reproduced exactly. No model calls.");
return 0;

static IOrderedEnumerable<FactError> Sorted(IEnumerable<FactError> errors) => errors.OrderBy(e => e.Type, StringComparer.Ordinal)
    .ThenBy(e => e.Detail, StringComparer.Ordinal).ThenBy(e => e.Critical);
