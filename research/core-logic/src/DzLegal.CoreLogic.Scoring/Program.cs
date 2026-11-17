using DzLegal.CoreLogic.Scoring;

// score --gold <notes.jsonl> --answers <end-to-end readings.jsonl> --given <type-given readings.jsonl> --out <folder>
//       [--tuning] [--scorer-hash <hash>] [--set <name>] [--run <n> --runs <RUNS.md>] [--first-line <text>]
//       [--purpose tune|measure] [--no-by-writer]
// Reads the gold and the two answer files and nothing else (the runs file only for a hidden set); writes summary.md and
// summary.json. Every failure is printed as a fixed code (specs/scorer.md).
try
{
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    var flags = new HashSet<string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i++)
    {
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options[args[i]] = args[++i];
        }
        else
        {
            flags.Add(args[i]);
        }
    }

    string Need(string name) => options.TryGetValue(name, out var value) ? value : throw new ScoreException("U01", $"missing {name}");
    var goldPath = Path.GetFullPath(Need("--gold"));
    if (goldPath.StartsWith(@"C:\dz-hidden", StringComparison.OrdinalIgnoreCase))
    {
        var run = options.GetValueOrDefault("--run");
        var runs = options.GetValueOrDefault("--runs");
        if (run is null || runs is null || !File.ReadLines(runs).Any(line => line.StartsWith($"| {run} |", StringComparison.Ordinal)
            && line.Contains("started", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ScoreException("H01", "a hidden set is scored only in a run recorded as started");
        }
    }

    var gold = File.ReadLines(goldPath).Where(line => !string.IsNullOrWhiteSpace(line)).Select(GoldNote.Parse).ToList();
    var ids = gold.Select(note => note.Id).ToHashSet(StringComparer.Ordinal);
    if (ids.Count != gold.Count)
    {
        throw new ScoreException("G04", "two gold notes share an id");
    }

    // With --purpose tune a gold holding a Claude-written note is refused, and the output is marked "tune": only a tune
    // output may choose anything about the reader (specs/scorer.md, "Per-writer rows").
    var purpose = options.GetValueOrDefault("--purpose") ?? "measure";
    if (purpose is not ("tune" or "measure"))
    {
        throw new ScoreException("U02", "--purpose is tune or measure");
    }

    if (purpose == "tune" && gold.Any(note => note.Writer == "anthropic"))
    {
        throw new ScoreException("P01", "a tune score is never computed on a note written by a Claude model");
    }

    var endToEnd = Answers.Load(Need("--answers"), ids);
    var given = Answers.Load(Need("--given"), ids);
    // --evidence-rule 1.0 reproduces scorer 1.0's evidence line, to report a figure under both versions.
    var evidenceRule = options.GetValueOrDefault("--evidence-rule") ?? Scorer.Version;
    var report = Scorer.Score(gold, endToEnd, given, flags.Contains("--tuning"), evidenceRule);
    var output = Need("--out");
    Directory.CreateDirectory(output);
    var set = options.GetValueOrDefault("--set") ?? string.Join(", ", gold.Select(note => note.Set).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    var hash = (options.GetValueOrDefault("--scorer-hash") ?? "(hash not given)") + $" (scorer {Scorer.Version}, evidence rule {evidenceRule})";
    var markdown = Summary.Markdown(report, set, hash, options.GetValueOrDefault("--first-line")) + "\nPurpose: " + purpose + ".\n";

    // By writer, on by default when the gold holds a Codex half and a Sonnet half (dev, challenge).
    var families = gold.Select(note => note.Writer).ToHashSet(StringComparer.Ordinal);
    if (!flags.Contains("--no-by-writer") && families.Contains("openai") && families.Contains("anthropic"))
    {
        List<GoldNote> Half(string family) => [.. gold.Where(note => note.Writer == family)];
        markdown += Summary.ByWriter(report, Scorer.Score(Half("openai"), endToEnd, given), Scorer.Score(Half("anthropic"), endToEnd, given));
    }

    File.WriteAllText(Path.Combine(output, "summary.md"), markdown.ReplaceLineEndings("\n"));
    var json = System.Text.Json.Nodes.JsonNode.Parse(Summary.Json(report, set, hash))!.AsObject();
    json["purpose"] = purpose;
    File.WriteAllText(Path.Combine(output, "summary.json"), json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");
    Console.WriteLine($"scored {gold.Count} notes -> {output}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception is ScoreException score ? $"score: {score.Code}" : $"score: X01 ({exception.GetType().Name})");
    return 1;
}
