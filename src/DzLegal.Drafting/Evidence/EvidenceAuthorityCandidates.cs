using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Drafting.Evidence;

public sealed record EvidenceAuthorityCandidate(string Code, string Article, string Version, string? Text, ImmutableArray<string> Origins, ImmutableArray<string> SentenceIds);

public sealed record EvidenceCandidateSet(ImmutableArray<EvidenceAuthorityCandidate> Candidates, ImmutableArray<string> CoverageGaps);

/// <summary>Discovery only. The whole article and its exception paragraphs stay together; scores never prove applicability.</summary>
public static partial class EvidenceAuthorityCandidates
{
    public static EvidenceCandidateSet Discover(LegalKnowledge knowledge, CaseSnapshot snapshot, IEnumerable<ClaimType> seeds, IEnumerable<AuthorityClause> dependencies)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(seeds);
        ArgumentNullException.ThrowIfNull(dependencies);
        var entries = new Dictionary<(string Code, string Article), (HashSet<string> Origins, HashSet<string> Sentences)>();
        var gaps = ImmutableArray.CreateBuilder<string>();
        void Add(string code, string article, string origin, string? sentence = null)
        {
            var key = (code, ArticleNumbers.Canonical(article));
            if (!entries.TryGetValue(key, out var entry)) { entry = (new(StringComparer.Ordinal), new(StringComparer.Ordinal)); entries.Add(key, entry); }
            entry.Origins.Add(origin);
            if (sentence is not null) { entry.Sentences.Add(sentence); }
        }

        foreach (var seed in seeds)
        {
            foreach (var reference in seed.Articles) { Add(reference.Code, reference.Number, "catalogue:" + seed.Id); }
        }

        foreach (var dependency in dependencies) { Add(dependency.Code, dependency.Article, "rule-dependency"); }
        foreach (var sentence in snapshot.Sentences.Where(s => s.FieldId != "court.section"))
        {
            foreach (var capture in ExactReference().Matches(ArabicScript.Normalize(sentence.Text)).SelectMany(m => m.Groups["number"].Captures.Cast<Capture>()))
            {
                var number = ArticleNumbers.Canonical(capture.Value);
                var codes = knowledge.Library.Codes.Where(c => c.FindArticle(number) is not null).ToArray();
                if (codes.Length == 0) { gaps.Add("unresolved exact article: " + number); }
                foreach (var code in codes) { Add(code.Id, number, codes.Length == 1 ? "exact-reference" : "ambiguous-exact-reference", sentence.Id); }
            }

            // Query the lexical index directly: dates and money in the raw text must not masquerade as article references.
            foreach (var hit in knowledge.Library.Index.Search(sentence.Text, knowledge.Library.Index.Count)) { Add(hit.Code.Id, hit.Article.Number, "raw-text-bm25", sentence.Id); }
        }

        var result = entries.OrderBy(e => e.Key.Code, StringComparer.Ordinal).ThenBy(e => e.Key.Article, StringComparer.Ordinal).Select(entry =>
        {
            var code = knowledge.Library.FindCode(entry.Key.Code);
            var article = code?.FindArticle(entry.Key.Article);
            if (article is null || article.Repealed) { gaps.Add("unavailable dependency: " + entry.Key.Code + ":" + entry.Key.Article); }
            if (code?.ConsolidatedAsOf is null) { gaps.Add("unresolved source version: " + entry.Key.Code); }
            return new EvidenceAuthorityCandidate(entry.Key.Code, entry.Key.Article, code?.ConsolidatedAsOf ?? "unknown", article is { Repealed: false } ? article.Text : null,
                [.. entry.Value.Origins.Order(StringComparer.Ordinal)], [.. entry.Value.Sentences.Order(StringComparer.Ordinal)]);
        }).ToImmutableArray();
        gaps.Add("Temporal applicability is unresolved until the relevant event, source versions and transitions are reviewed.");
        return new(result, [.. gaps.Distinct(StringComparer.Ordinal)]);
    }

    [GeneratedRegex(@"(?:الماده|المواد)\s+(?<number>\d+(?:\s*-\s*\d+)?(?:\s+مكرر(?:\s+\d+)?)?)(?:\s*(?:،|,|و)\s*(?<number>\d+(?:\s*-\s*\d+)?(?:\s+مكرر(?:\s+\d+)?)?))*")]
    private static partial Regex ExactReference();
}
