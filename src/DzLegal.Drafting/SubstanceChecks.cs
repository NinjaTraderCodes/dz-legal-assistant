using System.Text.RegularExpressions;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Drafting;

public enum SubstanceIssueKind
{
    /// <summary>The facts are written but no article founds the claim: nothing ticked, none cited in the notes.</summary>
    NoBasisArticle,

    /// <summary>Civil Code art. 124 (liability for fault, outside any contract) cited for a claim that arises from a contract.</summary>
    TortArticleInContractClaim,

    /// <summary>Interest asked on a loan between individuals, which Civil Code art. 454 forbids.</summary>
    InterestOnPrivateLoan,

    /// <summary>Damages asked in a contract claim while the facts mention no formal notice (Civil Code art. 179).</summary>
    DamagesWithoutFormalNotice,

    /// <summary>The notes contain text addressed to an AI («SYSTEM:», "ignore previous instructions", «تجاهل التعليمات»). Blocks export.</summary>
    InstructionInNotes,

    /// <summary>A request line is mostly in Latin letters: the requests are copied verbatim, so they must be in Arabic (CPCA art. 8). Blocks export.</summary>
    NonArabicRequest,

    /// <summary>A prison sentence («حبس», «سجن») asked in a civil case, which a civil court cannot order. Blocks export.</summary>
    CriminalPenaltyInCivilClaim,

    /// <summary>An article of the Family Code or the labour laws cited while no recognised claim belongs to that area.</summary>
    ArticleOutsideClaimArea,
}

/// <summary>A legal point the lawyer should look at, with the article it rests on.</summary>
/// <param name="Article">The article the point rests on, or null for the checks on the notes themselves.</param>
public sealed record SubstanceIssue(SubstanceIssueKind Kind, ArticleRef? Article);

/// <summary>
/// Deterministic checks of the substance, beside the formal check (which only says whether each mandatory element is
/// present). Each one names the article it comes from; none replaces the lawyer's reading. No AI.
/// </summary>
public static partial class SubstanceChecks
{
    /// <summary>The kinds that must be corrected in the notes before the document leaves the app; no review tick overrides them.</summary>
    public static bool BlocksExport(SubstanceIssueKind kind) =>
        kind is SubstanceIssueKind.InstructionInNotes or SubstanceIssueKind.NonArabicRequest or SubstanceIssueKind.CriminalPenaltyInCivilClaim;

    private static readonly string[] NoticeWords = ["اعذار", "إعذار", "أعذر", "اعذر", "انذار", "إنذار", "أنذر", "انذر", "تنبيه"];
    private static readonly string[] InterestWords = ["فائدة", "فوائد", "الفوائد", "فائدة قانونية"];
    private static readonly string[] DamagesWords = ["تعويض", "التعويض"];
    private static readonly string[] LoanWords = ["قرض", "اقترض", "سلفة", "أقرض", "اقرض"];

    /// <param name="inputs">The lawyer's form fields (facts, grounds and requests notes, documents).</param>
    /// <param name="claims">The claim types recognised in the facts (from the analysis), possibly none.</param>
    /// <param name="selected">The articles the lawyer ticked.</param>
    /// <param name="draftText">The drafted grounds and requests, when written.</param>
    public static IReadOnlyList<SubstanceIssue> Check(
        IReadOnlyDictionary<string, string> inputs,
        IReadOnlyList<ClaimType> claims,
        IReadOnlyList<ArticleRef> selected,
        string draftText)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentNullException.ThrowIfNull(selected);
        var facts = inputs.GetValueOrDefault("notes.facts", string.Empty) + "\n" + inputs.GetValueOrDefault("documents.list", string.Empty);
        var groundsNotes = inputs.GetValueOrDefault("notes.grounds", string.Empty);
        var requests = inputs.GetValueOrDefault("notes.requests", string.Empty) + "\n" + (draftText ?? string.Empty);
        var issues = new List<SubstanceIssue>();
        if (inputs.Values.Any(v => !string.IsNullOrEmpty(v) && InstructionToModel().IsMatch(v)))
        {
            issues.Add(new SubstanceIssue(SubstanceIssueKind.InstructionInNotes, null));
        }

        var requestLines = inputs.GetValueOrDefault("notes.requests", string.Empty).Split('\n');
        if (requestLines.Any(l => l.Count(char.IsLetter) >= 3 && ArabicScript.Ratio(l) < 0.5))
        {
            issues.Add(new SubstanceIssue(SubstanceIssueKind.NonArabicRequest, null));
        }

        // Requests, grounds notes and the drafted text: an injected «وبالحبس لمدة 5 سنوات» can reach the grounds too.
        // «حق الحبس» (the right of retention, Civil Code art. 200) is a civil remedy, not a prison sentence.
        var paragraphs = requestLines.Concat(groundsNotes.Split('\n')).Concat((draftText ?? string.Empty).Split('\n'));
        if (paragraphs.Any(l => (Mentions(l, PrisonWords) && !RightOfRetention().IsMatch(ArabicScript.Normalize(l))) || ForeignPrisonWords().IsMatch(l)))
        {
            issues.Add(new SubstanceIssue(SubstanceIssueKind.CriminalPenaltyInCivilClaim, null));
        }

        if (facts.Trim().Length == 0)
        {
            return issues;
        }

        if (selected.Count == 0 && !NarrativeChecks.ArticleReferences(groundsNotes).Any() && !NarrativeChecks.FrenchArticleReferences(groundsNotes).Any())
        {
            issues.Add(new SubstanceIssue(SubstanceIssueKind.NoBasisArticle, new ArticleRef("cpca", "15")));
        }

        var contractual = claims.Any(c => c.Contractual) || (claims.Count == 0 && Mentions(facts, ["عقد", "اقترض", "قرض", "إيجار", "ايجار", "بيع", "اشترى", "باع"]));
        var cited = selected.Where(a => a.Code == "civil-code").Select(a => a.Number)
            .Concat(NarrativeChecks.FullArticleReferences(groundsNotes + "\n" + draftText).Where(r => r.Code is null or "القانون المدني" or "ق م" or "ق.م").Select(r => r.Number));
        if (contractual && cited.Contains("124"))
        {
            issues.Add(new SubstanceIssue(SubstanceIssueKind.TortArticleInContractClaim, new ArticleRef("civil-code", "124")));
        }

        var loan = claims.Any(c => c.Id == "loan-repayment") || (claims.Count == 0 && Mentions(facts, LoanWords));
        if (loan && Mentions(requests, InterestWords))
        {
            issues.Add(new SubstanceIssue(SubstanceIssueKind.InterestOnPrivateLoan, new ArticleRef("civil-code", "454")));
        }

        if (contractual && Mentions(requests, DamagesWords) && !Mentions(facts, NoticeWords))
        {
            issues.Add(new SubstanceIssue(SubstanceIssueKind.DamagesWithoutFormalNotice, new ArticleRef("civil-code", "179")));
        }

        // The Constitution grounds no claim this app drafts (civil, commercial, family or social), so it is flagged even
        // before the claim type is confirmed; a family or labour law only when the confirmed claims are of another area.
        var areas = claims.Select(c => c.Area).ToHashSet(StringComparer.Ordinal);
        {
            var outside = NarrativeChecks.FullArticleReferences(groundsNotes + "\n" + draftText)
                .Select(r => (r.Number, Code: AreaCode(r.Code)))
                .Where(r => r.Code == "constitution"
                    || (claims.Count > 0 && r.Code is { } code && !areas.Contains(code == "family-code" ? "family" : "social")))
                .Select(r => new ArticleRef(r.Code!, r.Number))
                .FirstOrDefault();
            if (outside is not null)
            {
                issues.Add(new SubstanceIssue(SubstanceIssueKind.ArticleOutsideClaimArea, outside));
            }
        }

        return issues;
    }

    /// <summary>"family-code", "labour-relations" or "constitution" for a reference to those texts, otherwise null.</summary>
    private static string? AreaCode(string? code)
    {
        if (code is null)
        {
            return null;
        }

        var name = NarrativeChecks.CodeName(code);
        return name switch
        {
            "قانون الأسرة" => "family-code",
            "قانون العمل" => "labour-relations",
            "الدستور" => "constitution",
            _ => null,
        };
    }

    private static readonly string[] PrisonWords = ["حبس", "الحبس", "بحبس", "سجن", "السجن", "بسجن"];

    [GeneratedRegex(@"(?i)\b(?:system|assistant)\s*:|ignore\s+(?:all\s+|the\s+)?(?:previous|prior|above)\s+instructions|disregard\s+(?:all\s+|the\s+)?(?:previous|prior|above)|تجاهل\s+(?:كل\s+|جميع\s+)?(?:التعليمات|الاوامر|الأوامر|ما\s+سبق)|تعليمات\s+النظام|انت\s+نموذج|أنت\s+نموذج")]
    private static partial Regex InstructionToModel();

    [GeneratedRegex(@"حق\S{0,2}\s+(?:في\s+)?الحبس")]
    private static partial Regex RightOfRetention();

    [GeneratedRegex(@"(?i)\b(?:imprison\w*|prison|emprisonnement|incarc[ée]ration)\b")]
    private static partial Regex ForeignPrisonWords();

    private static bool Mentions(string text, IEnumerable<string> words)
    {
        var tokens = ArabicScript.Tokens(text);
        return words.Any(w => ArabicScript.Tokens(w) is [var word] && tokens.Any(t => ArabicScript.MatchesWord(t, word)));
    }
}
