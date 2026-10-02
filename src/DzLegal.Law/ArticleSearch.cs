using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Law;

public sealed record ArticleHit(LawCode Code, LawArticle Article)
{
    public string Citation => $"المادة {Article.Number} من {Code.CitationName}";
}

/// <summary>
/// Finds articles in the imported library, for the lawyer to add by hand: by number («المادة 124 ق م», «124
/// مكرر») or by words («التعويض عن الضرر»). Repealed articles are never returned.
/// </summary>
public static partial class ArticleSearch
{
    private static readonly (string Code, string[] Hints)[] CodeHints =
    [
        ("civil-code", ["ق م", "مدني", "المدني"]),
        ("cpca", ["ق ا م ا", "اجراءات", "الاجراءات"]),
        ("commercial-code", ["ق ت", "تجاري", "التجاري"]),
        ("family-code", ["اسره", "الاسره"]),
        ("labour-relations", ["90-11", "علاقات العمل"]),
        ("labour-disputes", ["90-04", "النزاعات الفرديه"]),
    ];

    public static IReadOnlyList<ArticleHit> Find(LawLibrary library, string query, int max = 10)
    {
        ArgumentNullException.ThrowIfNull(library);
        var normalized = ArabicScript.Normalize(query);
        if (normalized.Length == 0)
        {
            return [];
        }

        var number = Number().Match(normalized);
        if (number.Success)
        {
            var canonical = ArticleNumbers.Canonical(number.Groups[1].Value);
            var hinted = CodeHints.Where(h => h.Hints.Any(hint => normalized.Contains(hint, StringComparison.Ordinal))).Select(h => h.Code).ToHashSet();
            return library.Codes
                .Where(c => hinted.Count == 0 || hinted.Contains(c.Id))
                .Select(c => (Code: c, Article: c.FindArticle(canonical)))
                .Where(x => x.Article is { Repealed: false })
                .Select(x => new ArticleHit(x.Code, x.Article!))
                .Take(max)
                .ToList();
        }

        var words = ArabicScript.Tokens(query).Where(t => t.Length >= 3).Distinct().ToList();
        if (words.Count == 0)
        {
            return [];
        }

        var needed = (words.Count + 1) / 2;
        return library.Codes
            .SelectMany(c => c.Articles.Where(a => !a.Repealed).Select(a => (Code: c, Article: a)))
            .Select(x =>
            {
                var tokens = ArabicScript.Tokens(x.Article.Text);
                return (x.Code, x.Article, Score: words.Count(w => tokens.Any(t => ArabicScript.MatchesWord(t, w))));
            })
            .Where(x => x.Score >= needed)
            .OrderByDescending(x => x.Score)
            .Take(max)
            .Select(x => new ArticleHit(x.Code, x.Article))
            .ToList();
    }

    [GeneratedRegex(@"(?:^|\s|ماده\s*)(\d+(?:\s*-\s*\d+)?(?:\s*مكرر(?:\s*\d+)?)?)(?:\s|$)")]
    private static partial Regex Number();
}
