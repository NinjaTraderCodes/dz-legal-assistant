using System.Text;
using System.Text.Json;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Rules.Checks;

namespace DzLegal.CoreLogic.Rules.Tool;

/// <summary>
/// rules check | eval | examples. Exit code 0: everything passed; 1: a record failed a check or an example differed;
/// 2: the command could not run (usage, a missing file, a file that does not load).
/// </summary>
internal static class Program
{
    private const string Usage = """
        rules check    [--quoted] [--store <articles.json>] [--modules <dir>] <record.json>...
        rules eval     --records <record.json>... --case <case.json>
        rules examples <record.json>... [--against <other-record.json> --idmap <unit.idmap.json>] [--boundary <cases.json>] [--json]

        Every command also takes --root <worktree>: where slots.json, the schema, data/law and rules/final are read
        (default: the worktree the tool was built in). --modules replaces rules/final for check.
        """;

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        try
        {
            var options = Options.Parse(args.Skip(1));
            return args[0] switch
            {
                "check" => Check(options),
                "eval" => Eval(options),
                "examples" => Examples(options),
                _ => throw new ArgumentException($"Unknown command {args[0]}."),
            };
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException
            or FormatException or JsonException or RuleRecordException or KeyNotFoundException or InvalidOperationException)
        {
            Console.Error.WriteLine($"rules {args[0]}: {exception.Message}");
            Console.Error.WriteLine(Usage);
            return 2;
        }
    }

    private static int Check(Options options)
    {
        if (options.Positional.Count == 0)
        {
            throw new ArgumentException("No record to check.");
        }

        var context = Worktree.Find(options.Value("--root")).CheckContext(options.Value("--modules"), options.Value("--store"), options.Has("--quoted"));
        var failed = 0;
        foreach (var path in options.Positional)
        {
            var result = RecordChecks.Check(File.ReadAllText(path), context);
            if (result.Passed)
            {
                Console.WriteLine($"OK    {path}");
                continue;
            }

            failed++;
            Console.WriteLine($"FAIL  {path} ({result.Problems.Count} problems)");
            foreach (var problem in result.Problems)
            {
                Console.WriteLine($"      {problem}");
            }
        }

        Console.WriteLine($"rules check: {options.Positional.Count} records, {failed} failed");
        return failed == 0 ? 0 : 1;
    }

    private static int Eval(Options options)
    {
        var records = options.Values("--records").Select(Records.Load).ToList();
        var casePath = options.Value("--case") ?? throw new ArgumentException("--case is required.");
        if (records.Count == 0)
        {
            throw new ArgumentException("--records names no record.");
        }

        var file = JsonSerializer.Deserialize<CaseFile>(File.ReadAllText(casePath), ContractJson.Options)
            ?? throw new FormatException($"{casePath} holds null.");
        var slots = WorkedExamples.CaseSlotsOf(file.Slots, file.ClaimIds, file.Section);
        var outcome = new RuleEvaluator().Evaluate(records, slots, Dates.Parse(file.PetitionDate, "petitionDate"));
        Console.WriteLine(JsonSerializer.Serialize(outcome, ContractJson.Options));
        return 0;
    }

    private static int Examples(Options options)
    {
        var records = options.Positional.Select(Records.Load).ToList();
        if (records.Count == 0)
        {
            throw new ArgumentException("No record to run.");
        }

        var evaluator = new RuleEvaluator();
        var results = records.SelectMany(record => WorkedExamples.Run(evaluator, record)).ToList();

        if (options.Value("--against") is { } against)
        {
            var map = IdMap.Parse(File.ReadAllText(options.Value("--idmap") ?? throw new ArgumentException("--against needs --idmap.")));
            var other = Records.Load(against);
            results.AddRange(records.SelectMany(record => WorkedExamples.RunAgainst(evaluator, record, other, map)));
        }

        if (options.Value("--boundary") is { } boundary)
        {
            results.AddRange(BoundaryCases.Run(evaluator, records, BoundaryCases.Parse(File.ReadAllText(boundary))));
        }

        var failed = results.Count(result => !result.Passed);
        if (options.Has("--json"))
        {
            Console.WriteLine(JsonSerializer.Serialize(results, ContractJson.Options));
        }
        else
        {
            foreach (var result in results)
            {
                Console.WriteLine($"{(result.Passed ? "PASS" : "FAIL")}  {result.RecordId} {result.CaseId}: {result.Title}");
                foreach (var difference in result.Differences)
                {
                    Console.WriteLine($"      {difference}");
                }
            }

            Console.WriteLine($"rules examples: {results.Count} cases, {failed} with differences");
        }

        return failed == 0 ? 0 : 1;
    }

    /// <summary>The case file of <c>rules eval</c>: <c>{ "claimIds", "petitionDate", "slots", "section" }</c>; claimIds and section are optional.</summary>
    private sealed record CaseFile
    {
        public IReadOnlyList<string> ClaimIds { get; init; } = [];
        public required string PetitionDate { get; init; }
        public required IReadOnlyDictionary<string, JsonElement> Slots { get; init; }
        public CourtSection? Section { get; init; }
    }

    private sealed class Options
    {
        private static readonly HashSet<string> Flags = ["--quoted", "--json"];
        private static readonly HashSet<string> Single = ["--store", "--modules", "--root", "--case", "--against", "--idmap", "--boundary"];
        private static readonly HashSet<string> Lists = ["--records"];

        private readonly Dictionary<string, List<string>> named = new(StringComparer.Ordinal);

        public List<string> Positional { get; } = [];

        public static Options Parse(IEnumerable<string> args)
        {
            var options = new Options();
            string? list = null;
            using var items = args.GetEnumerator();
            while (items.MoveNext())
            {
                var arg = items.Current;
                if (Flags.Contains(arg))
                {
                    options.named[arg] = [];
                    list = null;
                }
                else if (Single.Contains(arg))
                {
                    options.named[arg] = [items.MoveNext() ? items.Current : throw new ArgumentException($"{arg} needs a value.")];
                    list = null;
                }
                else if (Lists.Contains(arg))
                {
                    options.named[arg] = [];
                    list = arg;
                }
                else if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Unknown option {arg}.");
                }
                else if (list is not null)
                {
                    options.named[list].Add(arg);
                }
                else
                {
                    options.Positional.Add(arg);
                }
            }

            return options;
        }

        public bool Has(string name) => named.ContainsKey(name);

        public string? Value(string name) => named.TryGetValue(name, out var values) ? values.FirstOrDefault() : null;

        public List<string> Values(string name) => named.TryGetValue(name, out var values) ? values : [];
    }
}
