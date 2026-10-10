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
}

/// <summary>A legal point the lawyer should look at, with the article it rests on.</summary>
public sealed record SubstanceIssue(SubstanceIssueKind Kind, ArticleRef Article);

/// <summary>
/// Deterministic checks of the substance, beside the formal check (which only says whether each mandatory element is
/// present). Each one names the article it comes from; none replaces the lawyer's reading. No AI.
/// </summary>
public static class SubstanceChecks
{
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
        if (facts.Trim().Length == 0)
        {
            return issues;
        }

        if (selected.Count == 0 && !NarrativeChecks.ArticleReferences(groundsNotes).Any())
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

        return issues;
    }

    private static bool Mentions(string text, IEnumerable<string> words)
    {
        var tokens = ArabicScript.Tokens(text);
        return words.Any(w => ArabicScript.Tokens(w) is [var word] && tokens.Any(t => ArabicScript.MatchesWord(t, word)));
    }
}
