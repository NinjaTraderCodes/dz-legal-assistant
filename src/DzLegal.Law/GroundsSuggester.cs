using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>An article of a suggested claim, with its official text when that code has been imported.</summary>
public sealed record ArticleSuggestion(ClaimArticle Reference, string CitationName, LawArticle? Article, ArticleProvenance Provenance)
{
    public bool IsAvailable => Article is { Repealed: false };

    /// <summary>«المادة 106 من القانون المدني».</summary>
    public string Citation => $"المادة {Reference.Number} من {CitationName}";
}

public sealed record ClaimSuggestion(ClaimType Claim, int Score, IReadOnlyList<string> MatchedTerms, IReadOnlyList<ArticleSuggestion> Articles);

/// <summary>
/// Suggests claim types (and so their articles) from the lawyer's facts, deterministically: Arabic-normalised
/// keyword groups, no model. It only ranks; the lawyer decides what to cite.
/// </summary>
public sealed class GroundsSuggester
{
    /// <summary>Below this score a claim is not suggested (one weak keyword is not enough).</summary>
    public const int MinScore = 3;

    private readonly LegalKnowledge _knowledge;
    private readonly List<(ClaimType Claim, List<(TriggerGroup Group, List<(string Term, string[] Tokens)> Terms)> Groups, List<string[]> Exclude)> _compiled;

    public GroundsSuggester(LegalKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        _knowledge = knowledge;
        _compiled = knowledge.Claims
            .Select(c => (
                c,
                c.Triggers.Select(g => (g, g.Any.Select(t => (t, ArabicScript.Tokens(t).ToArray())).ToList())).ToList(),
                c.Exclude.Select(t => ArabicScript.Tokens(t).ToArray()).ToList()))
            .ToList();
    }

    /// <summary>
    /// The best claim types for the text, highest score first. A group adds its weight once when any of its terms
    /// appears; a required group must appear; any exclusion word rules the claim out. When the area is known
    /// (from the court section) only that area's claims are considered.
    /// </summary>
    public IReadOnlyList<ClaimSuggestion> Suggest(string text, string? area = null, int max = 3)
    {
        var tokens = ArabicScript.Tokens(text);
        var results = new List<ClaimSuggestion>();
        foreach (var (claim, groups, exclude) in _compiled)
        {
            if ((area is not null && claim.Area != area) || exclude.Any(e => Contains(tokens, e)))
            {
                continue;
            }

            var score = 0;
            var matched = new List<string>();
            var requiredMissing = false;
            foreach (var (group, terms) in groups)
            {
                var hit = terms.FirstOrDefault(t => Contains(tokens, t.Tokens));
                if (hit.Term is not null)
                {
                    score += group.Weight;
                    matched.Add(hit.Term);
                }
                else if (group.Required)
                {
                    requiredMissing = true;
                }
            }

            if (!requiredMissing && score >= MinScore)
            {
                results.Add(new ClaimSuggestion(claim, score, matched, Resolve(claim)));
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.MatchedTerms.Count)
            .Take(max)
            .ToList();
    }

    public IReadOnlyList<ArticleSuggestion> Resolve(ClaimType claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return claim.Articles
            .Select(a =>
            {
                var article = _knowledge.Library.FindArticle(a.Code, a.Number);
                return new ArticleSuggestion(a, _knowledge.CitationName(a.Code), article, _knowledge.ProvenanceOf(a.Code, article));
            })
            .ToList();
    }

    /// <summary>The litigation area named by a court section («القسم التجاري» → commercial), or null when unclear.</summary>
    public static string? AreaOf(string? courtSection)
    {
        var tokens = ArabicScript.Tokens(courtSection);
        bool Has(string word) => tokens.Any(t => ArabicScript.MatchesWord(t, ArabicScript.Tokens(word)[0]));
        return Has("الاجتماعي") || Has("العمل") ? "social"
            : Has("الأسرة") || Has("شؤون") ? "family"
            : Has("التجاري") || Has("البحري") ? "commercial"
            : Has("المدني") || Has("العقاري") ? "civil"
            : null;
    }

    /// <summary>Whether the token sequence appears contiguously (each word allowing one leading clitic).</summary>
    internal static bool Contains(IReadOnlyList<string> tokens, string[] phrase)
    {
        if (phrase.Length == 0)
        {
            return false;
        }

        for (var i = 0; i + phrase.Length <= tokens.Count; i++)
        {
            var all = true;
            for (var j = 0; j < phrase.Length && all; j++)
            {
                all = ArabicScript.MatchesWord(tokens[i + j], phrase[j]);
            }

            if (all)
            {
                return true;
            }
        }

        return false;
    }
}
