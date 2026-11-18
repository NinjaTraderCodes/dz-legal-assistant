using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Reader.Front;

// reader ids    --notes <notes.jsonl> --vocab <vocab.txt> --out <ids.jsonl>
// reader encode --notes <notes.jsonl> --vocab <vocab.txt> --model <encoder.int8.onnx> --cache <folder>
// reader triggers --notes <notes.jsonl> --cues <cues.json> --out <triggers.jsonl>   (where each kind's trigger words occur)
// reader cue-votes --notes <notes.jsonl> --cues <cues.json> --out <summary.json>   (the cue table's polarity votes against the gold, per class)
// reader values --notes <notes.jsonl> --out <values.jsonl>   (rung 2's dates and amounts, with their spans: the roles head's candidates)
// The notes file is the one `notes export` writes (the loader's only door): one note per line with its id and its text
// fields. ids writes, per note, the .NET pieces (the reference: training uses these ids, never Python's own).
Console.OutputEncoding = System.Text.Encoding.UTF8;
if (args.Length < 1 || args[0] is not ("ids" or "encode" or "values" or "triggers" or "cue-votes"))
{
    Console.Error.WriteLine("usage: reader ids|encode|values|triggers|cue-votes --notes <file> [--vocab <file>] [--out <file>] [--model <file> --cache <folder>]");
    return 2;
}

var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (var i = 1; i + 1 < args.Length; i += 2)
{
    options[args[i]] = args[i + 1];
}

string Need(string name) => options.TryGetValue(name, out var value) ? value : throw new ArgumentException($"reader {args[0]} needs {name}");

static NoteInput Input(JsonNode note)
{
    var text = note["text"] ?? throw new InvalidDataException("a note without its text");
    string Field(string name) => text[name]?.GetValue<string>() ?? string.Empty;
    return new NoteInput { Subject = Field("subject"), Facts = Field("facts"), Requests = Field("requests"), Documents = Field("documents") };
}

try
{
    var notes = File.ReadLines(Need("--notes")).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => JsonNode.Parse(line)!).ToList();
    if (args[0] == "triggers")
    {
        var cues = DzLegal.CoreLogic.Reader.Cues.CueEngine.Load(Need("--cues"));
        using var writer = new StreamWriter(Need("--out"), append: false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var note in notes)
        {
            var input = Input(note);
            var found = new List<object>();
            foreach (var (field, text) in new[] { (NoteField.Subject, input.Subject), (NoteField.Facts, input.Facts), (NoteField.Requests, input.Requests), (NoteField.Documents, input.Documents) })
            {
                found.AddRange(cues.Triggers(text, field).Select(t => (object)new
                {
                    kind = t.Kind, field = t.Span.Field.ToString().ToLowerInvariant(), start = t.Span.Start, length = t.Span.Length,
                }));
            }

            writer.WriteLine(JsonSerializer.Serialize(new { id = note["id"]!.GetValue<string>(), triggers = found }));
        }

        Console.WriteLine($"{notes.Count.ToString(CultureInfo.InvariantCulture)} notes -> {Need("--out")}");
        return 0;
    }

    if (args[0] == "cue-votes")
    {
        // The cue table's own measure (rung 2, specs/reader.md): for every gold event of every note, the table's vote over
        // the four fields (no vote when the fields disagree), against the gold polarity. Counted per gold class: events,
        // votes, right votes, wrong votes. A vote on a notMentioned event is wrong.
        var cues = DzLegal.CoreLogic.Reader.Cues.CueEngine.Load(Need("--cues"));
        var table = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
        var wrongVotes = new List<object>();
        foreach (var note in notes)
        {
            var input = Input(note);
            foreach (var e in note["gold"]?["events"]?.AsArray() ?? [])
            {
                var kind = e!["kind"]!.GetValue<string>();
                var gold = e["polarity"]!.GetValue<string>();
                var votes = new[] { (NoteField.Subject, input.Subject), (NoteField.Facts, input.Facts), (NoteField.Requests, input.Requests), (NoteField.Documents, input.Documents) }
                    .Select(f => cues.Vote(f.Item2, kind, f.Item1)).OfType<DzLegal.CoreLogic.Reader.Cues.CueVote>().Select(v => v.Polarity).Distinct().ToList();
                var row = table.TryGetValue(gold, out var r) ? r : table[gold] = new int[4];
                row[0]++;
                if (votes.Count == 1)
                {
                    row[1]++;
                    var said = votes[0] switch { Polarity.Stated => "stated", Polarity.Denied => "denied", Polarity.Disputed => "disputed", _ => "notMentioned" };
                    row[said == gold ? 2 : 3]++;
                    if (said != gold)
                    {
                        wrongVotes.Add(new { id = note["id"]!.GetValue<string>(), kind, gold, vote = said });
                    }
                }
            }
        }

        var summary = table.ToDictionary(kv => kv.Key, kv => new { events = kv.Value[0], votes = kv.Value[1], right = kv.Value[2], wrong = kv.Value[3] });
        File.WriteAllText(Need("--out"), JsonSerializer.Serialize(new { notes = notes.Count, classes = summary, wrong = wrongVotes }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var (gold, row) in table)
        {
            Console.WriteLine($"{gold,-13} events {row[0],4}  votes {row[1],4}  right {row[2],4}  wrong {row[3],4}");
        }

        return 0;
    }

    if (args[0] == "values")
    {
        using var writer = new StreamWriter(Need("--out"), append: false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var note in notes)
        {
            var input = Input(note);
            var found = new List<ValueReading>();
            foreach (var (field, text) in new[] { (NoteField.Subject, input.Subject), (NoteField.Facts, input.Facts), (NoteField.Requests, input.Requests), (NoteField.Documents, input.Documents) })
            {
                found.AddRange(DzLegal.CoreLogic.Reader.Values.DateParser.Read(text, field));
                found.AddRange(DzLegal.CoreLogic.Reader.Values.AmountParser.Read(text, field));
            }

            writer.WriteLine(JsonSerializer.Serialize(new
            {
                id = note["id"]!.GetValue<string>(),
                values = found.Select(v => new
                {
                    kind = v.Kind.ToString().ToLowerInvariant(), normalized = v.Normalized,
                    field = v.Span.Field.ToString().ToLowerInvariant(), start = v.Span.Start, length = v.Span.Length,
                }),
            }));
        }

        Console.WriteLine($"{notes.Count.ToString(CultureInfo.InvariantCulture)} notes -> {Need("--out")}");
        return 0;
    }

    var sequence = NoteSequence.FromVocabulary(Need("--vocab"));
    if (args[0] == "ids")
    {
        using var writer = new StreamWriter(Need("--out"), append: false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var note in notes)
        {
            var pieces = sequence.Pieces(Input(note));
            writer.WriteLine(JsonSerializer.Serialize(new
            {
                id = note["id"]!.GetValue<string>(),
                ids = new[] { sequence.ClsId }.Concat(pieces.Select(p => p.Id)),
                pieces = pieces.Select(p => new { field = p.Field?.ToString().ToLowerInvariant(), p.Start, p.Length }),
            }));
        }

        Console.WriteLine($"{notes.Count.ToString(CultureInfo.InvariantCulture)} notes -> {Need("--out")}");
        return 0;
    }

    using var encoder = new NoteEncoder(Need("--model"), sequence);
    var folder = EncoderCache.Folder(Need("--cache"), encoder.ModelSha256);
    int windows = 0, cut = 0;
    foreach (var note in notes)
    {
        var encoded = encoder.Encode(Input(note));
        EncoderCache.Write(folder, note["id"]!.GetValue<string>(), encoded);
        windows += encoded.Windows;
        cut += encoded.CutOff > 0 ? 1 : 0;
    }

    Console.WriteLine($"{notes.Count.ToString(CultureInfo.InvariantCulture)} notes, {windows.ToString(CultureInfo.InvariantCulture)} windows, "
        + $"{cut.ToString(CultureInfo.InvariantCulture)} cut after three windows -> {folder}");
    return 0;
}
catch (Exception exception) when (exception is IOException or ArgumentException or InvalidDataException or JsonException
                                       or UnauthorizedAccessException or Microsoft.ML.OnnxRuntime.OnnxRuntimeException)
{
    Console.Error.WriteLine($"reader {args[0]}: {exception.Message}");
    return 1;
}
