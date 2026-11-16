using System.Globalization;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Filtering;

/// <summary>
/// <c>notes accept --set &lt;set&gt;</c>: the owner's route of 16 November 2026 for training notes (HANDBOOK section 2,
/// "after the filter trial"): no local model checks every note; a note that passed the code checks at ingest is kept,
/// with the writer's labels as gold and the code checks named as its filter. The reader later flags the suspicious notes
/// itself, and a flagged note is dropped, never judged by a Claude model. Training sets only; a test set keeps the blind
/// relabel (<c>notes relabel</c>, <c>notes compare</c>).
/// </summary>
public static class AcceptCommand
{
    public const string Filter = "codeChecks";

    public static int Run(NotesFiles files, string set, IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(options);
        if (set is not ("pilot" or "train"))
        {
            throw new ArgumentException($"notes accept is for the training sets (pilot, train), not {set}");
        }

        var work = FilterCommand.Work(options);
        var written = FilterCommand.Written(work, set).ToDictionary(n => (string)n["id"]!, StringComparer.Ordinal);
        var queue = QueueFile.Load(work);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var lines = new List<string>();
        foreach (var line in queue.Lines.Where(l => (string?)l["set"] == set && (string?)l["state"] == "written" && (string?)l["writer"] == "codex"))
        {
            var note = written[(string)line["note"]!];
            var provenance = note["provenance"]!.DeepClone().AsObject();
            provenance["filter"] = new JsonObject
            {
                ["family"] = "code", ["model"] = Filter, ["version"] = "owner-2026-11-16", ["date"] = date,
            };
            lines.Add(new JsonObject
            {
                ["id"] = (string)note["id"]!, ["set"] = set, ["text"] = note["text"]!.DeepClone(),
                ["gold"] = note["provenance"]!["writerLabels"]!.DeepClone(), ["blanked"] = new JsonArray(),
                ["provenance"] = provenance, ["status"] = "kept",
            }.ToJsonString(NotesJson.Compact));
            line["state"] = "kept";
            line["checkRun"] = Filter;
        }

        Directory.CreateDirectory(Path.Combine(work, "notes"));
        File.AppendAllText(Path.Combine(work, "notes", set + ".jsonl"), string.Concat(lines.Select(l => l + "\n")));
        queue.Save();
        Console.WriteLine($"{lines.Count} notes of {set} kept on the code checks -> {Path.Combine(work, "notes", set + ".jsonl")}");
        return 0;
    }
}
