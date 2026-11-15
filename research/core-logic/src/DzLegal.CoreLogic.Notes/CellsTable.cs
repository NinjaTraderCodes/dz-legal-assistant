using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes;

/// <summary>One cell item: an element of claim-elements.json, asked whether its "denied" polarity is still a note.</summary>
public sealed record CellItem(int Id, string Claim, string Event, string ClaimLabel, string Proposition);

/// <summary>
/// The cells table of SCHEME.md, section 8: which "denied" cells are drawn. A cell is not drawn only when both deciders
/// answer "no"; on any other outcome it is drawn (the safe side).
/// </summary>
public static class CellsTable
{
    public const int FirstId = 1001;
    public const string TableHead = "| Claim type | Event kind | `denied` drawn | Sonnet | Opus |";

    /// <summary>One item per element of claim-elements.json, in its order, with the claim type's Arabic label.</summary>
    public static IReadOnlyList<CellItem> Build(NotesFiles files)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(files.Claims, "*.json").Order(StringComparer.Ordinal))
        {
            foreach (var claim in NotesJson.Load(path)["claims"]!.AsArray())
            {
                labels[claim!["id"]!.GetValue<string>()] = claim["label"]!["ar"]!.GetValue<string>();
            }
        }

        var items = new List<CellItem>();
        foreach (var claim in NotesJson.Load(files.ClaimElements)["claims"]!.AsArray())
        {
            var id = claim!["claim"]!.GetValue<string>();
            foreach (var element in claim["elements"]!.AsArray())
            {
                items.Add(new CellItem(FirstId + items.Count, id, element!["event"]!.GetValue<string>(),
                    labels.TryGetValue(id, out var label) ? label : throw new InvalidDataException($"{id} is not in data/law/claims"),
                    element["label"]!.GetValue<string>()));
            }
        }

        return items;
    }

    public static void Save(string path, IReadOnlyList<CellItem> items)
    {
        var root = new JsonObject
        {
            ["about"] = "The cell items of SCHEME.md, section 8 (written by `notes cells --prepare` from data/law/claim-elements.json): "
                + "for each element, whether a note whose own author denies it is still a note for that claim type. No expected "
                + "answer: a cell is not drawn only when both deciders answer no.",
            ["items"] = new JsonArray([.. items.Select(item => (JsonNode)new JsonObject
            {
                ["id"] = item.Id,
                ["claim"] = item.Claim,
                ["event"] = item.Event,
                ["claimLabel"] = item.ClaimLabel,
                ["proposition"] = item.Proposition,
            })]),
        };
        NotesJson.Write(path, root.ToJsonString(NotesJson.Options) + "\n");
    }

    public static string Prepare(NotesFiles files, IReadOnlyList<CellItem> items)
    {
        var lines = items.Select(item =>
            $"- {item.Id.ToString(CultureInfo.InvariantCulture)}. The claim: {item.ClaimLabel}. The fact its own lawyer writes did not happen: {item.Proposition}");
        return PromptTemplate.Fill(PromptTemplate.Body(files.Prompts, "notes-cells"),
            new Dictionary<string, string> { ["items"] = string.Join("\n", lines) });
    }

    public static IReadOnlyDictionary<int, string> Answers(string replyPath) =>
        NotesJson.Load(replyPath)["answers"]!.AsArray()
            .ToDictionary(node => node!["id"]!.GetValue<int>(), node => node!["answer"]!.GetValue<string>());

    /// <summary>Not drawn only when both answer "no"; a missing answer counts as drawn.</summary>
    public static bool Drawn(string? sonnet, string? opus) => !(sonnet == "no" && opus == "no");

    /// <summary>Writes the table rows of SCHEME.md, section 8, in place of whatever rows the table held.</summary>
    public static string WriteTable(string scheme, IReadOnlyList<CellItem> items, IReadOnlyDictionary<int, string> sonnet,
        IReadOnlyDictionary<int, string> opus)
    {
        var text = scheme.ReplaceLineEndings("\n");
        var head = text.IndexOf(TableHead + "\n", StringComparison.Ordinal);
        if (head < 0)
        {
            throw new InvalidDataException("SCHEME.md has no cells table head");
        }

        var rowsStart = text.IndexOf('\n', text.IndexOf('\n', head) + 1) + 1;  // after the head and the |---| line
        var rowsEnd = rowsStart;
        while (rowsEnd < text.Length && text[rowsEnd] == '|')
        {
            rowsEnd = text.IndexOf('\n', rowsEnd) + 1;
        }

        var rows = new StringBuilder();
        foreach (var item in items)
        {
            var s = sonnet.TryGetValue(item.Id, out var a) ? a : null;
            var o = opus.TryGetValue(item.Id, out var b) ? b : null;
            rows.Append("| `").Append(item.Claim).Append("` | `").Append(item.Event).Append("` | ")
                .Append(Drawn(s, o) ? "yes" : "no").Append(" | ").Append(s ?? "no answer").Append(" | ").Append(o ?? "no answer").Append(" |\n");
        }

        return text[..rowsStart] + rows + text[rowsEnd..];
    }
}
