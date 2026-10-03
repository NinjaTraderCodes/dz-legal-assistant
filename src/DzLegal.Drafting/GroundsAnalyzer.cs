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
    public const int MaxSelected = 6;
    public const int ExcerptLength = 300;
    public const int MaxReasonWords = 30;
    public const string NoneMarker = "لا شيء";

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    /// Receives (step, text) for diagnostics: each prompt and raw model answer, and the candidate list. Used by the
    /// quality harness to save what the model actually wrote; the app leaves it unset.
    /// </summary>
    public Action<string, string>? Trace { get; init; }

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
        var readingRequest = BuildReadingRequest(knowledge, subject, facts, claims);
        Trace?.Invoke("reading.prompt", readingRequest.UserPrompt);
        var readingAnswer = await _provider.GenerateAsync(readingRequest, cancellationToken);
        Trace?.Invoke("reading.answer", readingAnswer);
        var reading = ParseReading(readingAnswer, claims);

        progress?.Report(AnalysisStage.Searching);
        var candidates = Candidates(knowledge, reading, subject + "\n" + facts, claims);
        Trace?.Invoke("candidates", string.Join('\n', candidates.Select(c => $"{c.Key} — {c.Source}")));

        progress?.Report(AnalysisStage.Selecting);
        var selected = new List<AnalysedArticle>();
        if (candidates.Count > 0)
        {
            var selectionRequest = BuildSelectionRequest(knowledge, facts, reading, candidates);
            Trace?.Invoke("selection.prompt", selectionRequest.UserPrompt);
            var selectionAnswer = await _provider.GenerateAsync(selectionRequest, cancellationToken);
            Trace?.Invoke("selection.answer", selectionAnswer);
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
            .Replace("{{ claims }}", string.Join('\n', claims.Select(c => c.Description.Length > 0 ? $"{c.Id} — {c.Label.Ar}: {c.Description}" : $"{c.Id} — {c.Label.Ar}")), StringComparison.Ordinal);
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

    /// <summary>
    /// Reads the five labelled lines, tolerating what small models do: bold or bulleted labels, spelling variants
    /// («نوع الدعوي»), the value on the next line, and a claim type given by its Arabic name instead of its id. Only
    /// claim types from the given list are kept; article numbers are removed from the free lines. When the claim line
    /// is missing, the whole answer is searched for ids and names.
    /// </summary>
    public static FactsReading ParseReading(string answer, IReadOnlyList<ClaimType> allowedClaims)
    {
        ArgumentNullException.ThrowIfNull(allowedClaims);
        var lines = WithoutReasoning(answer).Split('\n').Select(l => l.Trim()).ToList();
        string? Field(string label)
        {
            var key = ArabicScript.Normalize(label);
            for (var i = 0; i < lines.Count; i++)
            {
                var line = ArabicScript.Normalize(lines[i].TrimStart('-', '*', '•', '#', ' ', '>'));
                if (!line.StartsWith(key, StringComparison.Ordinal))
                {
                    continue;
                }

                var value = ValueAfterLabel(lines[i], label);
                if (value.Length == 0 && i + 1 < lines.Count)
                {
                    value = lines[i + 1].Trim().TrimStart('-', '*', '•').Trim();
                }

                return WithoutArticleNumbers(value.Trim('*', ' '));
            }

            return null;
        }

        var claimLine = Field("نوع الدعوى");
        var ids = FindClaims(claimLine ?? string.Join('\n', lines), allowedClaims);
        var terms = (Field("كلمات البحث") ?? string.Empty)
            .Split([',', '،', '؛', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length is > 1 and < 60)
            .Distinct()
            .Take(12)
            .ToList();
        return new FactsReading(Field("العلاقة القانونية") ?? string.Empty, Field("الالتزام المخل به") ?? string.Empty, Field("المطلوب") ?? string.Empty, ids, terms);
    }

    /// <summary>Claim ids named in a text, by id or by Arabic name (full, before «:», or inside brackets), in order of mention.</summary>
    private static List<string> FindClaims(string text, IReadOnlyList<ClaimType> claims)
    {
        var lower = text.ToLowerInvariant();
        var normalized = ArabicScript.Normalize(text);
        var found = new List<(int Position, string Id)>();
        foreach (var claim in claims)
        {
            var idMatch = Regex.Match(lower, @"(?<![a-z0-9-])" + Regex.Escape(claim.Id) + @"(?![a-z0-9-])");
            var position = idMatch.Success ? idMatch.Index : -1;
            if (position < 0)
            {
                position = NameVariants(claim.Label.Ar)
                    .Select(n => normalized.IndexOf(n, StringComparison.Ordinal))
                    .Where(p => p >= 0)
                    .DefaultIfEmpty(-1)
                    .Min();
            }

            if (position >= 0)
            {
                found.Add((position, claim.Id));
            }
        }

        return found.OrderBy(f => f.Position).Select(f => f.Id).Distinct().Take(MaxClaims).ToList();
    }

    /// <summary>«تعويض عن ضرر بسبب خطأ (مسؤولية تقصيرية)» → itself, «تعويض عن ضرر بسبب خطأ», «مسؤولية تقصيرية».</summary>
    private static IEnumerable<string> NameVariants(string label)
    {
        var variants = new List<string> { label };
        var bracket = Regex.Match(label, @"^(.*?)\((.*?)\)");
        if (bracket.Success)
        {
            variants.Add(bracket.Groups[1].Value);
            variants.Add(bracket.Groups[2].Value);
        }

        if (label.Contains(':', StringComparison.Ordinal))
        {
            variants.Add(label[..label.IndexOf(':', StringComparison.Ordinal)]);
        }

        return variants.Select(v => ArabicScript.Normalize(v).Trim()).Where(v => v.Length >= 4).Distinct();
    }

    /// <summary>The text after the label: after the first colon, or else after the label's words.</summary>
    private static string ValueAfterLabel(string line, string label)
    {
        var colon = line.IndexOfAny([':', '：']);
        var value = colon >= 0
            ? line[(colon + 1)..]
            : string.Join(' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(label.Split(' ').Length));
        return value.Trim().TrimStart('-', '–', '—', ' ').Trim();
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
