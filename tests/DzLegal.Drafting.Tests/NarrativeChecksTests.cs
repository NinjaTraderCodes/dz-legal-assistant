namespace DzLegal.Drafting.Tests;

public class NarrativeChecksTests
{
    private const string Notes = "اقترض المدعى عليه من المدعي مبلغ 1,500,000 دج بتاريخ 15 جانفي 2025.";
    private static readonly Dictionary<string, string> Inputs = new()
    {
        ["notes.facts"] = Notes,
        ["document.date"] = "01/10/2026",
    };

    private static IReadOnlyList<NarrativeIssue> Check(string text) => NarrativeChecks.Check(text, Notes, Inputs);

    [Fact]
    public void FaithfulTextHasNoIssues() =>
        Assert.Empty(Check("حيث إن المدعى عليه اقترض من المدعي مبلغ 1.500.000 دج بتاريخ 15 جانفي 2025."));

    [Fact]
    public void ChineseCharactersFromTheFirstLocalRunAreAHardIssue()
    {
        var issue = Assert.Single(Check("حيث إن الدعوى لم受理 قبول 1500000 15 2025"));
        Assert.Equal(NarrativeIssueKind.ForeignScript, issue.Kind);
        Assert.Equal("受理", issue.Detail);
        Assert.True(issue.IsHard);
    }

    [Fact]
    public void FrenchTermsAndAccentsAreAllowed() =>
        Assert.Empty(Check("حيث إن reconnaissance de dette notariée تثبت مبلغ 1500000 دج في 15 جانفي 2025، أي créance certaine."));

    [Fact]
    public void InventedArticleNumberIsFlagged()
    {
        var issue = Assert.Single(Check("حيث إن المادة 999 تلزم بدفع 1500000 دج منذ 15 جانفي 2025."));
        Assert.Equal(NarrativeIssueKind.UnknownNumber, issue.Kind);
        Assert.Equal("999", issue.Detail);
        Assert.False(issue.IsHard);
    }

    [Fact]
    public void ChangedAmountIsFlaggedBothWays()
    {
        var issues = Check("حيث إن المدعى عليه اقترض مبلغ 150,000 دج بتاريخ 15 جانفي 2025.");
        Assert.Contains(issues, i => i.Kind == NarrativeIssueKind.UnknownNumber && i.Detail == "150000");
        Assert.Contains(issues, i => i.Kind == NarrativeIssueKind.MissingNumber && i.Detail == "1500000");
    }

    [Fact]
    public void ArabicIndicDigitsAndListMarkersAreNormalized() =>
        Assert.Empty(Check("1. حيث إن المبلغ ١٬٥٠٠٬٠٠٠ دج\n2. بتاريخ ١٥ جانفي ٢٠٢٥"));

    [Fact]
    public void NumbersFromOtherInputsAreKnown() =>
        Assert.DoesNotContain(Check("حرر في 01/10/2026، مبلغ 1500000 دج، 15 جانفي 2025"), i => i.Kind == NarrativeIssueKind.UnknownNumber);

    [Fact]
    public void EmptyTextHasNoIssues() => Assert.Empty(Check("  "));
}
