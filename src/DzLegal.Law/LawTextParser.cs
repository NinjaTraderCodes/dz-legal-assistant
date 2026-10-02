using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>What the parser found, plus everything a reviewer should look at before trusting the import.</summary>
public sealed record ParseResult(IReadOnlyList<LawArticle> Articles, IReadOnlyList<string> Warnings, bool WasReversed);

/// <summary>
/// Splits the plain text of an official Arabic code into articles on «المادة N» headings, keeping each article's
/// text exactly as published and the book / title / chapter headings above it. Deterministic; no AI.
/// </summary>
public static partial class LawTextParser
{
    /// <summary>Articles shorter than this are reported for review (often a heading mistaken for an article).</summary>
    public const int ShortArticle = 25;

    /// <summary>Articles longer than this are reported (often two articles merged because a heading was missed).</summary>
    public const int LongArticle = 6000;

    private static readonly string[] Levels = ["الكتاب", "الباب", "الفصل", "القسم", "الفرع"];

    public static ParseResult Parse(string rawText)
    {
        ArgumentNullException.ThrowIfNull(rawText);
        var text = rawText.Normalize(NormalizationForm.FormKC).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var reversed = LooksReversed(text);
        if (reversed)
        {
            text = string.Join('\n', text.Split('\n').Select(l => new string(l.Reverse().ToArray())));
        }

        var articles = new List<LawArticle>();
        var warnings = new List<string>();
        if (reversed)
        {
            warnings.Add("The text was in visual (reversed) order and was flipped line by line; check a few articles carefully.");
        }

        var path = new string?[Levels.Length];
        string? number = null;
        string[] articlePath = [];
        var body = new List<string>();
        var lines = text.Split('\n').Select(l => l.Trim()).ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var heading = ArticleHeading().Match(line);
            if (heading.Success)
            {
                Flush();
                number = ArticleNumbers.Canonical(heading.Groups["n"].Value + (heading.Groups["bis"].Success ? " " + heading.Groups["bis"].Value : string.Empty));
                articlePath = path.Where(p => p is not null).Select(p => p!).ToArray();
                var rest = line[heading.Length..].Trim();
                if (rest.Length > 0)
                {
                    body.Add(rest);
                }

                continue;
            }

            var level = Array.FindIndex(Levels, l => line.StartsWith(l + " ", StringComparison.Ordinal) && line.Split(' ').Length <= 8);
            if (level >= 0)
            {
                Flush();
                number = null;
                var title = line;
                if (line.Split(' ').Length <= 3 && i + 1 < lines.Count && IsTitleLine(lines[i + 1]))
                {
                    title += " — " + lines[++i];
                }

                path[level] = title;
                for (var deeper = level + 1; deeper < Levels.Length; deeper++)
                {
                    path[deeper] = null;
                }

                continue;
            }

            if (number is not null)
            {
                body.Add(line);
            }
        }

        Flush();
        warnings.AddRange(Review(articles));
        return new ParseResult(articles, warnings, reversed);

        void Flush()
        {
            if (number is null)
            {
                body.Clear();
                return;
            }

            var articleText = JoinLines(body);
            if (articles.Any(a => a.Number == number))
            {
                warnings.Add($"Article {number} appears again; the first occurrence was kept.");
            }
            else
            {
                articles.Add(new LawArticle
                {
                    Number = number,
                    Path = articlePath,
                    Text = articleText,
                    Repealed = IsRepealedMarker(articleText),
                });
            }

            body.Clear();
        }
    }

    /// <summary>Gaps in the numbering, and articles too short or too long to be plausible.</summary>
    public static IEnumerable<string> Review(IReadOnlyList<LawArticle> articles)
    {
        ArgumentNullException.ThrowIfNull(articles);
        var numbers = articles.Where(a => !ArticleNumbers.IsBis(a.Number)).Select(a => ArticleNumbers.Base(a.Number)).OfType<int>().ToHashSet();
        if (numbers.Count > 0)
        {
            var missing = Enumerable.Range(1, numbers.Max()).Where(n => !numbers.Contains(n)).ToList();
            if (missing.Count > 0)
            {
                yield return $"{missing.Count} article number(s) missing: {Ranges(missing)}.";
            }
        }

        foreach (var a in articles.Where(a => !a.Repealed && a.Text.Length < ShortArticle))
        {
            yield return $"Article {a.Number} is very short ({a.Text.Length} characters): «{a.Text}».";
        }

        foreach (var a in articles.Where(a => a.Text.Length > LongArticle))
        {
            yield return $"Article {a.Number} is very long ({a.Text.Length} characters); a heading may have been missed.";
        }
    }

    /// <summary>
    /// PDF extractions sometimes give Arabic in visual order («ةداملا» for «المادة»). Detected by counting both
    /// spellings of the article keyword.
    /// </summary>
    private static bool LooksReversed(string text) =>
        Count(text, "ةداملا") > Count(text, "المادة");

    private static int Count(string text, string word)
    {
        var count = 0;
        for (var i = text.IndexOf(word, StringComparison.Ordinal); i >= 0; i = text.IndexOf(word, i + word.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static bool IsTitleLine(string line) =>
        line.Length is > 0 and <= 80 && !ArticleHeading().IsMatch(line) && !Levels.Any(l => line.StartsWith(l + " ", StringComparison.Ordinal));

    /// <summary>A repealed article's whole text is a marker such as «(ملغاة)» or «ألغيت».</summary>
    private static bool IsRepealedMarker(string text)
    {
        var plain = ArabicScript.Normalize(text).Trim('(', ')', '.', ' ', '-', '«', '»');
        return plain.Length < 60 && (plain.StartsWith("ملغاه", StringComparison.Ordinal)
            || plain.StartsWith("ملغي", StringComparison.Ordinal)
            || plain.StartsWith("الغيت", StringComparison.Ordinal));
    }

    /// <summary>
    /// Re-joins lines wrapped by the PDF layout: a line ending a sentence («.» «:» «؛») ends a paragraph, any other
    /// line continues on the next one. Numbered items («1-», «-») start a new line.
    /// </summary>
    private static string JoinLines(List<string> lines)
    {
        var sb = new StringBuilder();
        foreach (var line in lines.Where(l => l.Length > 0))
        {
            if (sb.Length > 0)
            {
                var last = sb[^1];
                sb.Append(last is '.' or ':' or '؛' or ';' || ItemStart().IsMatch(line) ? '\n' : ' ');
            }

            sb.Append(line);
        }

        return sb.ToString().Trim();
    }

    private static string Ranges(List<int> numbers)
    {
        var parts = new List<string>();
        for (var i = 0; i < numbers.Count; i++)
        {
            var start = numbers[i];
            while (i + 1 < numbers.Count && numbers[i + 1] == numbers[i] + 1)
            {
                i++;
            }

            parts.Add(start == numbers[i] ? $"{start}" : $"{start}-{numbers[i]}");
        }

        return parts.Count > 20 ? string.Join(", ", parts.Take(20)) + ", …" : string.Join(", ", parts);
    }

    /// <summary>«المادة 106 :», «المادة ١٠٦ -», «المادة 124 مكرر 1.», «المادة 73-4:», «المادة الأولى:» at the start of a line.</summary>
    [GeneratedRegex(@"^(?:ال)?م[اَ]?ادة\s+(?<n>[0-9٠-٩]+(?:\s*-\s*[0-9٠-٩]+(?=\s*[:：.\-–—]|\s*$))?|الأولى|الاولى)\s*(?<bis>مكرر(?:\s*[0-9٠-٩]+)?)?\s*[:：.\-–—]?\s*")]
    private static partial Regex ArticleHeading();

    [GeneratedRegex(@"^(?:[-–•]|\(?[0-9٠-٩]{1,2}\s*[-–).])\s*")]
    private static partial Regex ItemStart();
}
