using System.Text.Json;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Reader.Cues;

namespace DzLegal.CoreLogic.Reader.Tests;

/// <summary>
/// Rung 2's bar for the cue table (specs/reader.md): on the guide's hard cases and on the worked sentences of its table 5.1
/// the cue table gives no wrong vote (it may give none).
/// </summary>
public sealed partial class CueTableTests(ITestOutputHelper output)
{
    private static readonly CueEngine Engine = CueEngine.Load(Path.Combine(AppContext.BaseDirectory, "cues.json"));

    private static string Name(Polarity polarity) => polarity switch
    {
        Polarity.Stated => "stated",
        Polarity.Denied => "denied",
        Polarity.Disputed => "disputed",
        _ => "notMentioned",
    };

    private static (int Votes, List<string> Wrong) Check(IEnumerable<(string Text, string Kind, string Expected, NoteField Field)> cases)
    {
        var votes = 0;
        var wrong = new List<string>();
        foreach (var (text, kind, expected, field) in cases)
        {
            var vote = Engine.Vote(text, kind, field);
            if (vote is null)
            {
                continue;
            }

            votes++;
            if (Name(vote.Polarity) != expected)
            {
                wrong.Add($"{kind} «{text}»: voted {Name(vote.Polarity)}, the guide says {expected}");
            }
        }

        return (votes, wrong);
    }

    [Fact]
    public void NoWrongVoteOnTheGuidesHardCases()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "hard-cases.json")));
        var cases = doc.RootElement.GetProperty("cases").EnumerateArray().Select(c => (
            c.GetProperty("text").GetString()!, c.GetProperty("kind").GetString()!, c.GetProperty("expected").GetString()!,
            c.TryGetProperty("field", out var f) && f.GetString() == "documents" ? NoteField.Documents : NoteField.Facts)).ToList();
        var (votes, wrong) = Check(cases);
        output.WriteLine($"hard cases: {votes} votes on {cases.Count} entries, {wrong.Count} wrong");
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void NoWrongVoteOnTheWorkedSentencesOfTable51()
    {
        var guide = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LABELLING-GUIDE.md")).ReplaceLineEndings("\n");
        var table = guide[guide.IndexOf("### 5.1", StringComparison.Ordinal)..guide.IndexOf("### 5.2", StringComparison.Ordinal)];
        string[] columns = ["stated", "denied", "disputed"];
        var cases = new List<(string, string, string, NoteField)>();
        foreach (var line in table.Split('\n').Where(l => l.StartsWith("| `", StringComparison.Ordinal)))
        {
            var cells = line.Trim('|').Split('|').Select(c => c.Trim()).ToList();
            var kind = cells[0].Trim('`').Split('`')[0];
            for (var i = 0; i < 3 && i + 1 < cells.Count; i++)
            {
                if (cells[i + 1].Contains("see note", StringComparison.Ordinal))
                {
                    continue;  // the note decides these, not the column
                }

                foreach (Match m in Quoted().Matches(cells[i + 1]))
                {
                    cases.Add((m.Groups[1].Value, kind, columns[i], NoteField.Facts));
                }
            }
        }

        var (votes, wrong) = Check(cases);
        output.WriteLine($"table 5.1: {votes} votes on {cases.Count} sentences, {wrong.Count} wrong");
        Assert.True(cases.Count >= 100, "the table's sentences were read");
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void TheSpanIsTheTriggerInTheOriginalTextEvenWithDiacritics()
    {
        const string Text = "ذكّر الموكل المدين، ثمّ أعذره بواسطة محضر";
        var vote = Engine.Vote("أعذَر المدعي المدعى عليه بواسطة محضر قضائي", "notice_served");
        Assert.NotNull(vote);
        Assert.Equal("أعذَر", "أعذَر المدعي المدعى عليه بواسطة محضر قضائي".Substring(vote!.Span.Start, vote.Span.Length));
        Assert.Null(Engine.Vote(Text, "notice_served"));  // an informal demand nearby: the kind asks for no vote
    }

    [Fact]
    public void TriggersAreListedWhereTheyStandInTheOriginalText()
    {
        const string Text = "أعذَره بتاريخ 12 مارس 2025، ثم دفع 200.000 دج";
        var found = Engine.Triggers(Text, NoteField.Facts);
        var notice = Assert.Single(found, f => f.Kind == "notice_served");
        Assert.Equal("أعذَر", Text.Substring(notice.Span.Start, notice.Span.Length));
        Assert.Contains(found, f => f.Kind == "payment_made" && Text.Substring(f.Span.Start, f.Span.Length) == "دفع");
        Assert.Equal(found.OrderBy(f => f.Span.Start).Select(f => f.Span.Start), found.Select(f => f.Span.Start));
    }

    [GeneratedRegex("«([^»]+)»")]
    private static partial Regex Quoted();
}
