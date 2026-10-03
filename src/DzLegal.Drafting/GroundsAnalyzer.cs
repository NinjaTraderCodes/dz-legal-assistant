using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Drafting;

public enum AnalysisStage
{
    /// <summary>The model reads the facts: relationship, breach, remedy, claim types, search terms.</summary>
    Reading,

    /// <summary>The library is searched for candidate articles (no model).</summary>
    Searching,

    /// <summary>The model chooses among the candidates.</summary>
    Selecting,
}

/// <summary>Stage 1 output, parsed: the model's reading, catalog claim ids only, and search terms in legal vocabulary.</summary>
public sealed record FactsReading(string Relationship, string Breach, string Sought, IReadOnlyList<string> ClaimIds, IReadOnlyList<string> SearchTerms)
{
    public static FactsReading Empty { get; } = new(string.Empty, string.Empty, string.Empty, [], []);
}

/// <summary>An article offered to the model in stage 3: a stable key, its citation, an excerpt of its text and where it came from.</summary>
public sealed record CandidateArticle(string Key, ArticleRef Ref, string Citation, string Excerpt, string Source, ArticleRole? CatalogRole);

/// <summary>
/// The grounds analysis, in three stages:
/// 1. the model reads the facts and translates them into the codes' vocabulary (and recognises catalog claim types);
/// 2. the official library is searched (BM25, no model) for candidates, together with the recognised claim types'
///    catalog articles;
/// 3. the model chooses among the candidates, by key, with a role and a short reason.
/// Every proposed article therefore exists in the library or the catalog; anything else the model writes is ignored.
/// </summary>
public sealed partial class GroundsAnalyzer(IModelProvider provider)
{
    public const string ReadingRequestId = "grounds.reading";
    public const string SelectionRequestId = "grounds.selection";
    public const int MaxClaims = 2;
    public const int MaxCandidates = 25;
    public const int MaxSelected = 8;
    public const int ExcerptLength = 300;
    public const int MaxReasonWords = 30;
    public const string NoneMarker = "لا شيء";

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public async Task<GroundsAnalysis> AnalyseAsync(
        LegalKnowledge knowledge,
        string subject,
        string facts,
        string? courtSection,
        IProgress<AnalysisStage>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        if (!knowledge.Prompts.IsAvailable)
        {
            throw new InvalidOperationException("The grounds analysis prompts are missing from data/law/prompts.");
        }

        var area = LegalAreas.AreaOf(courtSection);
        var claims = knowledge.Claims.Where(c => area is null || c.Area == area).ToList();

        progress?.Report(AnalysisStage.Reading);
        var readingAnswer = await _provider.GenerateAsync(BuildReadingRequest(knowledge, subject, facts, claims), cancellationToken);
        var reading = ParseReading(readingAnswer, claims.Select(c => c.Id).ToList());

        progress?.Report(AnalysisStage.Searching);
        var candidates = Candidates(knowledge, reading, subject + "\n" + facts, claims);

        progress?.Report(AnalysisStage.Selecting);
        var selected = new List<AnalysedArticle>();
        if (candidates.Count > 0)
        {
            var selectionAnswer = await _provider.GenerateAsync(BuildSelectionRequest(knowledge, facts, reading, candidates), cancellationToken);
            selected = ParseSelection(selectionAnswer, candidates);
        }

        return new GroundsAnalysis
        {
            FactsHash = FactsHash(subject, facts, courtSection),
            Relationship = reading.Relationship,
            Breach = reading.Breach,
            Sought = reading.Sought,
            ClaimIds = [.. reading.ClaimIds],
            Articles = selected,
            Model = _provider.Name,
        };
    }

    /// <summary>Identifies the analysed input, so the UI can say when the facts changed after the analysis.</summary>
    public static string FactsHash(string? subject, string? facts, string? courtSection)
    {
        var canonical = string.Join('\u0001', ArabicScript.Normalize(subject), ArabicScript.Normalize(facts), ArabicScript.Normalize(courtSection));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..16];
    }

    public static ModelRequest BuildReadingRequest(LegalKnowledge knowledge, string subject, string facts, IReadOnlyList<ClaimType> claims)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(claims);
        var user = knowledge.Prompts.ReadFacts
            .Replace("{{ subject }}", subject?.Trim() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{{ facts }}", facts?.Trim() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{{ claims }}", string.Join('\n', claims.Select(c => $"{c.Id} — {c.Label.Ar}")), StringComparison.Ordinal);
        return new ModelRequest(ReadingRequestId, string.Empty, TemplateRenderer.Normalize(user), facts?.Trim() ?? string.Empty);
    }

    public static ModelRequest BuildSelectionRequest(LegalKnowledge knowledge, string facts, FactsReading reading, IReadOnlyList<CandidateArticle> candidates)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(candidates);
        var readingText = string.Join('\n', new[]
        {
            Line("العلاقة القانونية", reading.Relationship),
            Line("الالتزام المخل به", reading.Breach),
            Line("المطلوب", reading.Sought),
        }.Where(l => l.Length > 0));
        var user = knowledge.Prompts.SelectArticles
            .Replace("{{ facts }}", facts?.Trim() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{{ reading }}", readingText, StringComparison.Ordinal)
            .Replace("{{ candidates }}", string.Join('\n', candidates.Select(c => $"{c.Key} — {c.Citation} — {c.Excerpt}")), StringComparison.Ordinal);
        return new ModelRequest(SelectionRequestId, string.Empty, TemplateRenderer.Normalize(user), facts?.Trim() ?? string.Empty);

        static string Line(string label, string value) => value.Length == 0 ? string.Empty : $"{label}: {value}";
    }

    /// <summary>
    /// Candidates for stage 3: the catalog articles of the recognised claim types (curated), then the library's best
    /// matches for the model's search terms, then for the raw facts at a lower weight. Deduplicated, capped. Before any
    /// code is imported only catalog articles exist, described by the catalog's reason instead of the official text.
    /// </summary>
    public static IReadOnlyList<CandidateArticle> Candidates(LegalKnowledge knowledge, FactsReading reading, string facts, IReadOnlyList<ClaimType> claims)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(claims);
        var result = new List<CandidateArticle>();
        void Add(ArticleRef reference, string citation, string excerpt, string source, ArticleRole? role)
        {
            if (result.Count < MaxCandidates && result.All(c => c.Ref != reference))
            {
                result.Add(new CandidateArticle($"{reference.Code}:{reference.Number}", reference, citation, Excerpt(excerpt), source, role));
            }
        }

        foreach (var claim in reading.ClaimIds.Select(id => claims.FirstOrDefault(c => c.Id == id)).OfType<ClaimType>())
        {
            foreach (var article in knowledge.Resolve(claim))
            {
                Add(
                    new ArticleRef(article.Reference.Code, article.Reference.Number),
                    article.Citation,
                    article.IsAvailable ? article.Article!.Text : article.Reference.Why.Ar,
                    AnalysedArticle.FromCatalog,
                    article.Reference.Role);
            }
        }

        var hits = knowledge.Library.Index.Search(
            reading.SearchTerms.Select(t => (t, 1.0)).Append((facts, 0.5)),
            MaxCandidates);
        foreach (var hit in hits)
        {
            Add(new ArticleRef(hit.Code.Id, hit.Article.Number), hit.Citation, hit.Article.Text, AnalysedArticle.FromSearch, null);
        }

        return result;
    }

    /// <summary>Reads the five labelled lines; keeps catalog ids only and drops article numbers from the free lines.</summary>
    public static FactsReading ParseReading(string answer, IReadOnlyList<string> allowedClaimIds)
    {
        ArgumentNullException.ThrowIfNull(allowedClaimIds);
        var text = WithoutReasoning(answer);
        string Field(string label)
        {
            var m = Regex.Match(text, "^[\\s*\\-•]*" + Regex.Escape(label) + "\\s*[:：]\\s*(.+)$", RegexOptions.Multiline);
            return m.Success ? WithoutArticleNumbers(m.Groups[1].Value.Trim().Trim('*').Trim()) : string.Empty;
        }

        var claimLine = Field("نوع الدعوى");
        var ids = IdToken().Matches(claimLine.ToLowerInvariant()).Select(m => m.Value)
            .Where(allowedClaimIds.Contains).Distinct().Take(MaxClaims).ToList();
        var terms = Field("كلمات البحث")
            .Split([',', '،', '؛', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length is > 1 and < 60)
            .Distinct()
            .Take(12)
            .ToList();
        return new FactsReading(Field("العلاقة القانونية"), Field("الالتزام المخل به"), Field("المطلوب"), ids, terms);
    }

    /// <summary>Reads «key | role | reason» lines; keeps candidate keys only (a bare number is accepted when unambiguous).</summary>
    public static List<AnalysedArticle> ParseSelection(string answer, IReadOnlyList<CandidateArticle> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var result = new List<AnalysedArticle>();
        foreach (var raw in WithoutReasoning(answer).Split('\n'))
        {
            var parts = raw.Split(['|', '｜'], 3);
            var keyText = ArabicScript.ToAsciiDigits(parts[0]).Trim().Trim('-', '*', '•', '.', ' ').ToLowerInvariant();
            var candidate = candidates.FirstOrDefault(c => c.Key == keyText);
            if (candidate is null && Regex.Match(keyText, @"(\d+(?:\s*مكرر(?:\s*\d+)?)?)\s*$") is { Success: true } m)
            {
                var number = ArticleNumbers.Canonical(m.Groups[1].Value);
                var matching = candidates.Where(c => c.Ref.Number == number).ToList();
                candidate = matching.Count == 1 ? matching[0] : null;
            }

            if (candidate is null || result.Any(r => r.Ref == candidate.Ref))
            {
                continue;
            }

            var role = Role(parts.Length > 1 ? parts[1] : string.Empty) ?? candidate.CatalogRole ?? ArticleRole.Basis;
            result.Add(new AnalysedArticle(
                candidate.Ref.Code,
                candidate.Ref.Number,
                role.ToString().ToLowerInvariant(),
                Reason(parts.Length > 2 ? parts[2] : string.Empty, candidate.Ref.Number),
                candidate.Source));
            if (result.Count == MaxSelected)
            {
                break;
            }
        }

        return result;
    }

    private static ArticleRole? Role(string text)
    {
        var t = ArabicScript.Normalize(text);
        return t.Contains("اساس", StringComparison.Ordinal) ? ArticleRole.Basis
            : t.Contains("اثبات", StringComparison.Ordinal) ? ArticleRole.Proof
            : t.Contains("تعويض", StringComparison.Ordinal) ? ArticleRole.Damages
            : t.Contains("اجراء", StringComparison.Ordinal) ? ArticleRole.Procedure
            : null;
    }

    /// <summary>Short, and never citing another article: a reason that names other articles is dropped.</summary>
    private static string Reason(string text, string number)
    {
        var reason = text.Trim().Trim('*').Trim();
        var others = NarrativeChecks.ArticleReferences(reason).Select(r => r.Number).Where(n => n != ArticleNumbers.Base(number)?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (others.Any())
        {
            return string.Empty;
        }

        var words = reason.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= MaxReasonWords ? reason : string.Join(' ', words.Take(MaxReasonWords)) + "…";
    }

    private static string Excerpt(string text)
    {
        var flat = text.ReplaceLineEndings(" ").Trim();
        return flat.Length <= ExcerptLength ? flat : flat[..ExcerptLength] + "…";
    }

    private static string WithoutReasoning(string? answer) =>
        UnclosedThink().Replace(ThinkBlock().Replace(answer ?? string.Empty, string.Empty), string.Empty).Replace("**", string.Empty, StringComparison.Ordinal);

    private static string WithoutArticleNumbers(string text) => ArticleMention().Replace(text, string.Empty).Trim(' ', '،', ',');

    [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline)]
    private static partial Regex ThinkBlock();

    [GeneratedRegex(@"<think>.*", RegexOptions.Singleline)]
    private static partial Regex UnclosedThink();

    [GeneratedRegex(@"[a-z0-9]+(?:-[a-z0-9]+)*")]
    private static partial Regex IdToken();

    [GeneratedRegex(@"\(?(?:ال)?(?:مادة|مادتين|مادتان|مواد)\s+[0-9٠-٩][0-9٠-٩\s،,و\-مكرر]*\)?")]
    private static partial Regex ArticleMention();
}
