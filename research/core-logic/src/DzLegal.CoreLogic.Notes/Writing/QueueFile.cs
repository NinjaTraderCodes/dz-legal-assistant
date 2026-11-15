using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes.Writing;

/// <summary>
/// The notes queue (STATE-FORMAT.md, "The notes queue"): <c>&lt;work&gt;\queue.jsonl</c>, one line per scenario, rewritten
/// whole as notes move on, so that a stop loses nothing and repeats nothing. Only the notes tool writes it.
/// </summary>
public sealed class QueueFile
{
    private readonly string path;

    private QueueFile(string path, List<JsonObject> lines)
    {
        this.path = path;
        Lines = lines;
    }

    public List<JsonObject> Lines { get; }

    public static QueueFile Load(string work)
    {
        var path = Path.Combine(work, "queue.jsonl");
        var lines = File.Exists(path)
            ? File.ReadLines(path).Where(l => l.Trim().Length > 0).Select(l => JsonNode.Parse(l)!.AsObject()).ToList()
            : [];
        return new QueueFile(path, lines);
    }

    public JsonObject Line(string scenario) =>
        Lines.FirstOrDefault(l => (string?)l["scenario"] == scenario) ?? throw new KeyNotFoundException($"{scenario} is not in the queue");

    /// <summary>Writes the queue to a temporary file and moves it over the old one, so that a stop never leaves half a queue.</summary>
    public void Save()
    {
        var temporary = path + ".tmp";
        NotesJson.Write(temporary, string.Concat(Lines.Select(l => l.ToJsonString(NotesJson.Compact) + "\n")));
        File.Move(temporary, path, overwrite: true);
    }
}
