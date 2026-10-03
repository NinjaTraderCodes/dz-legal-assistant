using DzLegal.Domain;

namespace DzLegal.Law;

public sealed record ArticleHit(LawCode Code, LawArticle Article, double Score = 0)
{
    public string Citation => $"المادة {Article.Number} من {Code.CitationName}";
}

/// <summary>
/// Full-text search over the official articles (BM25), built once from the library. Article text and the book /
/// title / chapter headings above it are indexed — the headings count double, because they name the subject
/// («في القرض»، «في الإيجار»). No hand-written keywords: the index grows with every imported code. Repealed
/// articles are not indexed.
/// </summary>
public sealed class ArticleIndex
{
    private const double K1 = 1.2;
    private const double B = 0.75;

    private readonly List<(LawCode Code, LawArticle Article, Dictionary<string, int> Terms, int Length)> _docs = [];
    private readonly Dictionary<string, int> _documentFrequency = new(StringComparer.Ordinal);
    private readonly double _averageLength;

    public ArticleIndex(IEnumerable<LawCode> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        foreach (var code in codes)
        {
            foreach (var article in code.Articles.Where(a => !a.Repealed))
            {
                var keys = Keys(article.Text).Concat(article.Path.SelectMany(Keys)).Concat(article.Path.SelectMany(Keys)).ToList();
                var terms = keys.GroupBy(k => k, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
                _docs.Add((code, article, terms, keys.Count));
                foreach (var term in terms.Keys)
                {
                    _documentFrequency[term] = _documentFrequency.GetValueOrDefault(term) + 1;
                }
            }
        }

        _averageLength = _docs.Count == 0 ? 1 : _docs.Average(d => d.Length);
    }

    public int Count => _docs.Count;

    /// <summary>Search keys of a text: tokens with the article, clitics and common endings removed; very short words skipped.</summary>
    public static IEnumerable<string> Keys(string? text) =>
        ArabicScript.Tokens(text).Where(t => t.Length >= 3).Select(ArabicScript.SearchKey);

    /// <summary>
    /// The best articles for weighted queries (e.g. the model's legal terms at weight 1, the raw facts at 0.5).
    /// Each query's terms are scored with BM25; the weighted scores add up.
    /// </summary>
    public IReadOnlyList<ArticleHit> Search(IEnumerable<(string Text, double Weight)> queries, int max = 15)
    {
        ArgumentNullException.ThrowIfNull(queries);
        var weights = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (text, weight) in queries)
        {
            foreach (var key in Keys(text).Distinct(StringComparer.Ordinal))
            {
                weights[key] = Math.Max(weights.GetValueOrDefault(key), weight);
            }
        }

        if (weights.Count == 0 || _docs.Count == 0)
        {
            return [];
        }

        return _docs
            .Select(d => (Doc: d, Score: weights.Sum(w => w.Value * Bm25(d.Terms, d.Length, w.Key))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(max)
            .Select(x => new ArticleHit(x.Doc.Code, x.Doc.Article, x.Score))
            .ToList();
    }

    public IReadOnlyList<ArticleHit> Search(string query, int max = 15) => Search([(query, 1.0)], max);

    private double Bm25(Dictionary<string, int> terms, int length, string term)
    {
        if (!terms.TryGetValue(term, out var tf))
        {
            return 0;
        }

        var df = _documentFrequency[term];
        var idf = Math.Log(1 + ((_docs.Count - df + 0.5) / (df + 0.5)));
        return idf * (tf * (K1 + 1)) / (tf + (K1 * (1 - B + (B * length / _averageLength))));
    }
}
