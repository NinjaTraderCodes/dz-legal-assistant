using System.Text.Json;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Reader.Values;

namespace DzLegal.CoreLogic.Reader.Tests;

/// <summary>
/// Rung 2's bar (specs/reader.md): the parsers read 98 % of the code-made value corpus exactly, value and span. The
/// corpus is written by scripts/value_corpus.py from the guide's section 8 and the span rule, never from the parsers.
/// </summary>
public sealed class ValueCorpusTests(ITestOutputHelper output)
{
    private sealed record Item(string Text, string Kind, string Normalized, int Start, int Length);

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    [Theory]
    [InlineData("date")]
    [InlineData("amount")]
    public void TheParsersReadAtLeast98PercentOfTheCorpusExactly(string kind)
    {
        var items = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "value-corpus.jsonl"))
            .Select(line => JsonSerializer.Deserialize<Item>(line, Options)!).Where(item => item.Kind == kind).ToList();
        var misses = new List<string>();
        foreach (var item in items)
        {
            var found = kind == "date" ? DateParser.Read(item.Text, NoteField.Facts) : AmountParser.Read(item.Text, NoteField.Facts);
            if (!found.Any(v => v.Normalized == item.Normalized && v.Span.Start == item.Start && v.Span.Length == item.Length))
            {
                misses.Add($"{item.Text} | expected {item.Normalized} at {item.Start}+{item.Length} | got "
                    + string.Join(", ", found.Select(v => $"{v.Normalized} at {v.Span.Start}+{v.Span.Length}")));
            }
        }

        var rate = 1.0 - (double)misses.Count / items.Count;
        output.WriteLine($"{kind}: {items.Count - misses.Count} of {items.Count} exact ({rate:P2})");
        foreach (var miss in misses.Take(15))
        {
            output.WriteLine(miss);
        }

        Assert.True(items.Count >= 2000, "the corpus holds 2,000 of each kind");
        Assert.True(rate >= 0.98, $"{kind}: {misses.Count} misses of {items.Count}; first: {misses.FirstOrDefault()}");
    }
}
