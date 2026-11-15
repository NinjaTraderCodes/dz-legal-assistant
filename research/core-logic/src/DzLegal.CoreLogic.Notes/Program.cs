using System.Globalization;
using DzLegal.CoreLogic.Notes;

// The notes tool (specs/notes-pipeline.md, "The commands of the notes tool"). Phase A, step 1:
//   notes guide-items --prepare --count <n> --sections <file> --out <file>
//   notes guide-items --ingest <reply> [--count <n>]
//   notes guide-test --prepare --out <file>
//   notes guide-test --compare <Sonnet's reply> <Opus's reply> [--date yyyy-mm-dd]
//   notes cells --prepare --out <file>
//   notes cells --write-table <Sonnet's reply> <Opus's reply>
//   notes export --purpose train|tune|measure --sets <a,b> --out <folder> [--size <n> --subset-seed <n>]
//                [--exclude-writer <family>] [--pack <slot>] [--allow-pilot] [--mirror-run <n>] [--work <folder>]
//   notes style-cards --write
//   notes sample --set <set> --count <n> --seed <n> [--groups <n>] [--writer codex|sonnet] [--work <folder>] [--out <file>]
//   notes write --set <set> --calls <n> --writer codex|sonnet [--work <folder>]
//   notes ingest --call <id> [--reply <file>] [--rejected] [--work <folder>]
//   notes filter-sheet --note <id> --set <set>; notes filter --set <set> --model <ollama tag> [--limit <n>]
// Every command takes --repo <folder> (the worktree); without it the tool finds the repository above it.
const string Usage = "usage: notes guide-items --prepare|--ingest … | guide-test --prepare|--compare … | cells --prepare|--write-table … [--repo <folder>]";

Console.OutputEncoding = System.Text.Encoding.UTF8;  // the items and problems are Arabic

if (args.Length < 2)
{
    Console.Error.WriteLine(Usage);
    return 2;
}

var options = new Dictionary<string, string>(StringComparer.Ordinal);
var positional = new List<string>();
for (var i = 2; i < args.Length; i++)
{
    if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
    {
        options[args[i]] = args[++i];
    }
    else
    {
        positional.Add(args[i]);
    }
}

try
{
    var files = NotesFiles.Find(options.GetValueOrDefault("--repo"));
    string Need(string name) => options.TryGetValue(name, out var value) ? value : throw new ArgumentException($"{args[0]} {args[1]} needs {name}");

    switch (args[0], args[1])
    {
        case ("guide-items", "--prepare"):
            File.WriteAllText(Need("--out"), GuideTest.PrepareItems(files, int.Parse(Need("--count"), CultureInfo.InvariantCulture),
                File.ReadAllText(Need("--sections"))));
            Console.WriteLine(Need("--out"));
            return 0;
        case ("guide-items", "--ingest"):
        {
            var reply = options.GetValueOrDefault("--ingest") ?? positional.FirstOrDefault() ?? throw new ArgumentException("--ingest <reply>");
            var (added, problems) = GuideTest.Ingest(files.GuideTest, reply);
            Console.WriteLine($"added {added.Count} items" + (added.Count > 0 ? $": ids {added[0].Id} to {added[^1].Id}" : ""));
            if (options.TryGetValue("--count", out var asked) && added.Count < int.Parse(asked, CultureInfo.InvariantCulture))
            {
                Console.WriteLine($"FEWER than asked ({asked}): a fresh run for the missing number");
            }

            foreach (var problem in problems)
            {
                Console.WriteLine($"PROBLEM (a defect of the guide: fix it, bump its version, before the guide test): {problem}");
            }

            return problems.Count > 0 ? 3 : 0;
        }

        case ("guide-test", "--prepare"):
            File.WriteAllText(Need("--out"), GuideTest.PrepareTest(files));
            Console.WriteLine(Need("--out"));
            return 0;
        case ("guide-test", "--compare"):
        {
            var replies = options.TryGetValue("--compare", out var first) ? [first, .. positional] : positional;
            if (replies.Count != 2)
            {
                throw new ArgumentException("--compare <Sonnet's reply> <Opus's reply>");
            }

            var section = GuideTest.Compare(GuideTest.Load(files.GuideTest), GuideTest.Answers(replies[0]), GuideTest.Answers(replies[1]),
                options.GetValueOrDefault("--date") ?? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            File.AppendAllText(files.GuideTestResults, section);
            Console.Write(section);
            return 0;
        }

        case ("cells", "--prepare"):
        {
            var items = CellsTable.Build(files);
            CellsTable.Save(files.CellsTest, items);
            File.WriteAllText(Need("--out"), CellsTable.Prepare(files, items));
            Console.WriteLine($"{items.Count} cell items -> {files.CellsTest}; prompt -> {Need("--out")}");
            return 0;
        }

        case ("cells", "--write-table"):
        {
            var replies = options.TryGetValue("--write-table", out var first) ? [first, .. positional] : positional;
            if (replies.Count != 2)
            {
                throw new ArgumentException("--write-table <Sonnet's reply> <Opus's reply>");
            }

            var items = CellsTable.Build(files);
            var sonnet = CellsTable.Answers(replies[0]);
            var opus = CellsTable.Answers(replies[1]);
            NotesJson.Write(files.Scheme, CellsTable.WriteTable(File.ReadAllText(files.Scheme), items, sonnet, opus));
            var notDrawn = items.Count(item => !CellsTable.Drawn(sonnet.GetValueOrDefault(item.Id), opus.GetValueOrDefault(item.Id)));
            Console.WriteLine($"{items.Count} rows; {notDrawn} denied cells not drawn");
            return 0;
        }

        case ("export", "--purpose"):
        {
            // The loader's only door for the training code: notes export --purpose <p> --sets a,b --out <folder>
            var purpose = Enum.Parse<LoadPurpose>(positional.FirstOrDefault() ?? throw new ArgumentException("--purpose <train|tune|measure>"), ignoreCase: true);
            var work = options.GetValueOrDefault("--work") ?? @"C:\Users\TheShadow\dz-core-research\notes-work";
            var loader = new NotesLoader(Path.Combine(work, "notes"), Path.Combine(files.Notes, "TEST-IDS.txt"),
                Path.Combine(files.CoreLogic, "results", "mirror", "RUNS.md"));
            var result = loader.Load(new LoadRequest
            {
                Purpose = purpose,
                Sets = Need("--sets").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                ExcludeWriter = options.GetValueOrDefault("--exclude-writer"),
                Pack = options.GetValueOrDefault("--pack"),
                AllowPilot = positional.Contains("--allow-pilot"),
                MirrorRun = options.TryGetValue("--mirror-run", out var run) ? int.Parse(run, CultureInfo.InvariantCulture) : null,
                Size = options.TryGetValue("--size", out var size) ? int.Parse(size, CultureInfo.InvariantCulture) : null,
                SubsetSeed = options.TryGetValue("--subset-seed", out var seed) ? int.Parse(seed, CultureInfo.InvariantCulture) : 7,
            });
            var output = Need("--out");
            NotesLoader.Guard(output);
            Directory.CreateDirectory(output);
            NotesJson.Write(Path.Combine(output, "notes.jsonl"), string.Concat(result.Notes.Select(note => note.ToJsonString(NotesJson.Compact) + "\n")));
            NotesJson.Write(Path.Combine(output, "manifest.json"), result.Manifest().ToJsonString(NotesJson.Options) + "\n");
            Console.WriteLine($"{result.Notes.Count} notes for {purpose.ToString().ToLowerInvariant()} -> {output}");
            return 0;
        }

        case ("style-cards", "--write"):
        {
            var path = Path.Combine(files.Notes, "sampler", "style-cards.json");
            NotesJson.Write(path, DzLegal.CoreLogic.Notes.Sampler.StyleCards.ToJson(DzLegal.CoreLogic.Notes.Sampler.StyleCards.Build()));
            Console.WriteLine(path);
            return 0;
        }

        case ("write", "--set"):
            return DzLegal.CoreLogic.Notes.Writing.WriteCommand.Run(files,
                positional.FirstOrDefault() ?? throw new ArgumentException("--set <name>"), options);

        case ("ingest", "--call"):
            return DzLegal.CoreLogic.Notes.Writing.IngestCommand.Run(files,
                positional.FirstOrDefault(p => !p.StartsWith("--", StringComparison.Ordinal)) ?? throw new ArgumentException("--call <id>"),
                options, positional.Where(p => p.StartsWith("--", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal));

        case ("filter-sheet", "--note"):
            return DzLegal.CoreLogic.Notes.Filtering.FilterCommand.RunSheet(files,
                positional.FirstOrDefault() ?? throw new ArgumentException("--note <id>"), options);

        case ("filter-trial", "--notes"):
            options["--notes"] = positional.FirstOrDefault() ?? throw new ArgumentException("--notes <n>");
            return DzLegal.CoreLogic.Notes.Filtering.FilterTrial.Run(files, options);

        case ("filter", "--set"):
            return DzLegal.CoreLogic.Notes.Filtering.FilterCommand.Run(files,
                positional.FirstOrDefault() ?? throw new ArgumentException("--set <name>"), options);

        case ("sample", "--set"):
            return DzLegal.CoreLogic.Notes.Sampler.SampleCommand.Run(files,
                positional.FirstOrDefault() ?? throw new ArgumentException("--set <name>"), options);

        default:
            Console.Error.WriteLine(Usage);
            return 2;
    }
}
catch (LoaderRefusalException refusal)
{
    Console.Error.WriteLine($"notes {args[0]}: REFUSED {refusal.Message}");
    return 4;
}
catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or KeyNotFoundException
                                       or UnauthorizedAccessException or System.Text.Json.JsonException or FormatException)
{
    Console.Error.WriteLine($"notes {args[0]} {args[1]}: {exception.Message}");
    return 1;
}
