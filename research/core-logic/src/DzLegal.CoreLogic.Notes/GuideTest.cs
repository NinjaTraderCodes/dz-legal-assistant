using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DzLegal.CoreLogic.Notes;

/// <summary>One item of notes/guide-test.json. A tester sees id, claim, ask and text only.</summary>
public sealed record GuideItem(int Id, string Claim, string Ask, string Text, string Expected, string Tests);

/// <summary>One tester's answer to an item (prompts/schemas/guide-test.schema.json).</summary>
public sealed record GuideAnswer(int Id, string Answer, string Rule, bool Unsure);

/// <summary>
/// The guide test of specs/notes-pipeline.md, section 1: new items from the guide (<c>guide-items</c>), the two testers'
/// prompt (<c>guide-test --prepare</c>) and the comparison of their answers with the keys (<c>guide-test --compare</c>).
/// </summary>
public static partial class GuideTest
{
    public static IReadOnlyList<GuideItem> Load(string path) =>
        [.. NotesJson.Load(path)["items"]!.AsArray().Select(node => new GuideItem(
            node!["id"]!.GetValue<int>(), Str(node, "claim"), Str(node, "ask"), Str(node, "text"), Str(node, "expected"),
            Str(node, "tests")))];

    /// <summary>The prompt of notes-guide-items.md: the whole guide, the parts that changed, how many items.</summary>
    public static string PrepareItems(NotesFiles files, int count, string sections) =>
        PromptTemplate.Fill(PromptTemplate.Body(files.Prompts, "notes-guide-items"), new Dictionary<string, string>
        {
            ["guide"] = File.ReadAllText(files.Guide).Trim(),
            ["sections"] = sections.Trim(),
            ["count"] = count.ToString(CultureInfo.InvariantCulture),
        });

    /// <summary>
    /// Appends the new items of a reply with the next free ids, keeping the file's own one-item-per-line format (the diff
    /// is additions only). Returns the new items and the reply's problems (each a defect of the guide to fix first).
    /// </summary>
    public static (IReadOnlyList<GuideItem> Added, IReadOnlyList<string> Problems) Ingest(string path, string replyPath)
    {
        var reply = NotesJson.Load(replyPath);
        var next = Load(path).Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
        var added = new List<GuideItem>();
        foreach (var node in reply["items"]!.AsArray())
        {
            var item = new GuideItem(next++, Str(node!, "claim"), Str(node!, "ask"), Str(node!, "text"), Str(node!, "expected"),
                Str(node!, "tests"));
            if (!AskForm().IsMatch(item.Ask))
            {
                throw new InvalidDataException($"a new item's ask is not event:<kind>, slot:<id>, value or role:<claim>: {item.Ask}");
            }

            added.Add(item);
        }

        var problems = reply["problems"]?.AsArray().Select(node => node!.GetValue<string>()).ToList() ?? [];
        if (added.Count > 0)
        {
            var text = File.ReadAllText(path).ReplaceLineEndings("\n");
            var end = text.LastIndexOf("\n  ]", StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidDataException($"{path}: the items list does not end with «  ]»");
            }

            var lines = string.Join(",\n", added.Select(Line));
            NotesJson.Write(path, text[..end] + ",\n" + lines + text[end..]);
        }

        return (added, problems);
    }

    /// <summary>One item as the file writes it: «    { "id": 1, "claim": "…", … }».</summary>
    public static string Line(GuideItem item) =>
        "    { \"id\": " + item.Id.ToString(CultureInfo.InvariantCulture) + ", \"claim\": " + NotesJson.Literal(item.Claim)
        + ", \"ask\": " + NotesJson.Literal(item.Ask) + ", \"text\": " + NotesJson.Literal(item.Text)
        + ", \"expected\": " + NotesJson.Literal(item.Expected) + ", \"tests\": " + NotesJson.Literal(item.Tests) + " }";

    /// <summary>The testers' prompt: the whole guide and every item without its key.</summary>
    public static string PrepareTest(NotesFiles files)
    {
        var items = Load(files.GuideTest).Select(item => new JsonObject
        {
            ["id"] = item.Id,
            ["claim"] = item.Claim,
            ["ask"] = item.Ask,
            ["text"] = item.Text,
        }.ToJsonString(NotesJson.Compact));
        return PromptTemplate.Fill(PromptTemplate.Body(files.Prompts, "notes-guide-test"), new Dictionary<string, string>
        {
            ["guide"] = File.ReadAllText(files.Guide).Trim(),
            ["items"] = string.Join("\n", items),
        });
    }

    public static IReadOnlyDictionary<int, GuideAnswer> Answers(string replyPath) =>
        NotesJson.Load(replyPath)["answers"]!.AsArray().Select(node => new GuideAnswer(
            node!["id"]!.GetValue<int>(), Str(node, "answer"), Str(node, "rule"), node["unsure"]!.GetValue<bool>()))
        .ToDictionary(answer => answer.Id);

    /// <summary>Two answers are the same when they differ only by case, spaces, underscores or hyphens.</summary>
    public static bool Same(string? a, string? b) => a is not null && b is not null && Norm(a) == Norm(b);

    /// <summary>
    /// The comparison: for each item the key and both testers' answers; every item where the three are not equal is listed,
    /// and every item marked unsure. Returns the section appended to GUIDE-TEST-RESULTS.md (the executor writes, below it,
    /// how each listed item is explained: guide unclear, key wrong, or a tester misread a clear rule).
    /// </summary>
    public static string Compare(IReadOnlyList<GuideItem> items, IReadOnlyDictionary<int, GuideAnswer> sonnet,
        IReadOnlyDictionary<int, GuideAnswer> opus, string date)
    {
        string Count(IReadOnlyDictionary<int, GuideAnswer> answers) =>
            items.Count(item => answers.TryGetValue(item.Id, out var a) && Same(a.Answer, item.Expected)).ToString(CultureInfo.InvariantCulture);

        string Unsure(IReadOnlyDictionary<int, GuideAnswer> answers)
        {
            var ids = items.Where(item => answers.TryGetValue(item.Id, out var a) && a.Unsure).Select(item => item.Id.ToString(CultureInfo.InvariantCulture)).ToList();
            return ids.Count == 0 ? "0" : $"{ids.Count} (items {string.Join(", ", ids)})";
        }

        var total = items.Count.ToString(CultureInfo.InvariantCulture);
        var agree = items.Count(item => sonnet.TryGetValue(item.Id, out var a) && opus.TryGetValue(item.Id, out var b) && Same(a.Answer, b.Answer));
        var builder = new StringBuilder();
        builder.Append("\n## The guide test: ").Append(date).Append(", ").Append(total).Append(" items (script `notes guide-test --compare`)\n\n")
            .Append("Both testers are Claude models (Sonnet 5.5 and Opus 5.5): what they share is not tested here.\n\n")
            .Append("| | Sonnet 5.5 | Opus 5.5 |\n|---|---:|---:|\n")
            .Append("| Expected answer given | ").Append(Count(sonnet)).Append(" of ").Append(total).Append(" | ").Append(Count(opus))
            .Append(" of ").Append(total).Append(" |\n")
            .Append("| Marked \"unsure\" | ").Append(Unsure(sonnet)).Append(" | ").Append(Unsure(opus)).Append(" |\n\n")
            .Append("The two testers agreed with each other on ").Append(agree.ToString(CultureInfo.InvariantCulture))
            .Append(" of ").Append(total).Append(" items.\n\n");

        var listed = items.Where(item => !(sonnet.TryGetValue(item.Id, out var a) && opus.TryGetValue(item.Id, out var b)
            && Same(a.Answer, item.Expected) && Same(b.Answer, item.Expected))).ToList();
        builder.Append("### Items where the three answers are not equal (").Append(listed.Count.ToString(CultureInfo.InvariantCulture)).Append(")\n\n");
        if (listed.Count == 0)
        {
            builder.Append("None.\n\n");
        }
        else
        {
            builder.Append("| Item | Claim, ask | Expected (tests) | Sonnet (rule) | Opus (rule) |\n|---|---|---|---|---|\n");
            foreach (var item in listed)
            {
                builder.Append(Row(item, sonnet, opus));
            }

            builder.Append('\n');
        }

        var unsure = items.Where(item => !listed.Contains(item) && ((sonnet.TryGetValue(item.Id, out var a) && a.Unsure)
            || (opus.TryGetValue(item.Id, out var b) && b.Unsure))).ToList();
        builder.Append("### Items answered as expected but marked unsure (").Append(unsure.Count.ToString(CultureInfo.InvariantCulture)).Append(")\n\n");
        if (unsure.Count == 0)
        {
            builder.Append("None.\n");
        }
        else
        {
            builder.Append("| Item | Claim, ask | Expected (tests) | Sonnet (rule) | Opus (rule) |\n|---|---|---|---|---|\n");
            foreach (var item in unsure)
            {
                builder.Append(Row(item, sonnet, opus));
            }
        }

        return builder.ToString();
    }

    private static string Row(GuideItem item, IReadOnlyDictionary<int, GuideAnswer> sonnet, IReadOnlyDictionary<int, GuideAnswer> opus)
    {
        static string Cell(IReadOnlyDictionary<int, GuideAnswer> answers, int id) =>
            answers.TryGetValue(id, out var a) ? $"{Escape(a.Answer)}{(a.Unsure ? " (unsure)" : "")}: {Escape(a.Rule)}" : "no answer";

        return $"| {item.Id.ToString(CultureInfo.InvariantCulture)} | `{item.Claim}`, `{item.Ask}` | {Escape(item.Expected)} ({Escape(item.Tests)}) | "
            + $"{Cell(sonnet, item.Id)} | {Cell(opus, item.Id)} |\n";
    }

    private static string Escape(string text) => text.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");

    private static string Norm(string text) =>
        new([.. text.Trim().ToUpperInvariant().Where(c => c is not (' ' or '_' or '-'))]);

    private static string Str(JsonNode node, string name) =>
        node[name]?.GetValue<string>() ?? throw new InvalidDataException($"an item without \"{name}\"");

    [GeneratedRegex(@"^(event:[a-z_]+|slot:[a-z_]+|value|role:[a-z-]+)$")]
    private static partial Regex AskForm();
}
