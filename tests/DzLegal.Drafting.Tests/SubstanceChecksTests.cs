using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Drafting.Tests;

public class SubstanceChecksTests
{
    private static readonly ClaimType Loan = new() { Id = "loan-repayment", Label = new LocalizedText("قرض", "Loan"), Contractual = true };

    private static Dictionary<string, string> Inputs(string grounds = "", string requests = "", string facts = "اقترض المدعى عليه مبلغ 1500000 دج ولم يرده رغم إعذاره بتاريخ 10 جويلية 2025.") => new()
    {
        ["notes.facts"] = facts,
        ["notes.grounds"] = grounds,
        ["notes.requests"] = requests,
    };

    private static readonly ArticleRef Art450 = new("civil-code", "450");

    [Fact]
    public void ACleanLoanClaimRaisesNothing() =>
        Assert.Empty(SubstanceChecks.Check(Inputs(requests: "إلزام المدعى عليه برد 1500000 دج مع التعويض."), [Loan], [Art450], string.Empty));

    [Fact]
    public void TortLiabilityCitedForAContractIsFlagged()
    {
        // The debt sample as it used to be: the lawyer's notes ground contractual damages on art. 124.
        var issues = SubstanceChecks.Check(Inputs(grounds: "المادة 124 من القانون المدني: كل فعل أيا كان يرتكبه الشخص بخطئه."), [Loan], [Art450], string.Empty);
        Assert.Equal(SubstanceIssueKind.TortArticleInContractClaim, Assert.Single(issues).Kind);
    }

    [Fact]
    public void InterestOnALoanBetweenIndividualsIsFlagged() =>
        Assert.Contains(
            SubstanceChecks.Check(Inputs(requests: "إلزامه برد المبلغ مع الفوائد القانونية."), [Loan], [Art450], string.Empty),
            i => i.Kind == SubstanceIssueKind.InterestOnPrivateLoan && i.Article == new ArticleRef("civil-code", "454"));

    [Fact]
    public void DamagesWithoutFormalNoticeAreFlagged() =>
        Assert.Contains(
            SubstanceChecks.Check(Inputs(requests: "إلزامه بالتعويض عن التأخير.", facts: "اقترض المدعى عليه مبلغا ولم يرده."), [Loan], [Art450], string.Empty),
            i => i.Kind == SubstanceIssueKind.DamagesWithoutFormalNotice);

    [Fact]
    public void NoGroundsAtAllIsFlaggedButAGroundInTheNotesIsEnough()
    {
        Assert.Contains(SubstanceChecks.Check(Inputs(), [], [], string.Empty), i => i.Kind == SubstanceIssueKind.NoBasisArticle);
        Assert.DoesNotContain(SubstanceChecks.Check(Inputs(grounds: "المادة 450 من القانون المدني."), [], [], string.Empty), i => i.Kind == SubstanceIssueKind.NoBasisArticle);
    }
}
