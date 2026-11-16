using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Reader.Front;

// reader ids    --notes <notes.jsonl> --vocab <vocab.txt> --out <ids.jsonl>
// reader encode --notes <notes.jsonl> --vocab <vocab.txt> --model <encoder.int8.onnx> --cache <folder>
// reader values --notes <notes.jsonl> --out <values.jsonl>   (rung 2's dates and amounts, with their spans: the roles head's candidates)
// The notes file is the one `notes export` writes (the loader's only door): one note per line with its id and its text
// fields. ids writes, per note, the .NET pieces (the reference: training uses these ids, never Python's own).
Console.OutputEncoding = System.Text.Encoding.UTF8;
if (args.Length < 1 || args[0] is not ("ids" or "encode" or "values"))
{
    Console.Error.WriteLine("usage: reader ids|encode|values --notes <file> [--vocab <file>] [--out <file>] [--model <file> --cache <folder>]");
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
