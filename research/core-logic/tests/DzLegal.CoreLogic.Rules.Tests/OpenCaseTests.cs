using DzLegal.CoreLogic.Contracts;
using DzLegal.Domain;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>
/// The owner's rule of 14 November 2026 for boundary cases whose outcome is "ask" or "unsettled": at least one matched
/// item gives the expected response, and no matched item contradicts it (no block; no item that applies stays silent).
/// The findings are built by hand: the rule is about how the items answer together, not about any record.
/// </summary>
public sealed class OpenCaseTests
{
    private static RuleFinding Finding(string id, FindingEffect declared, string branch) => new()
    {
        Ref = id,
        RecordId = "m",
        Kind = FindingKind.PriorStep,
        DeclaredEffect = declared,
        Effect = declared,
        Grade = "D",
        ToCheck = false,
        Message = new LocalizedText("رسالة.", "Message."),
        Basis = [],
        Trace = $"{id}: {branch} -> {RuleEvaluator.Name(declared)}; grade D -> {RuleEvaluator.Name(declared)}",
    };

    // First half: at least one matched item gives the expected response.
    [Fact]
    public void AnAskCaseIsMetWhenOneItemAsksEvenIfAnotherWarns()
    {
        var problems = BoundaryCases.OpenCaseProblems(BoundaryOutcome.Ask,
        [
            Finding("p1", FindingEffect.Ask, "notice_served no, exemptions unknown, asks x"),
            Finding("n1", FindingEffect.Warn, "when true"),
        ]);
        Assert.Empty(problems);
    }

    [Fact]
    public void AnAskCaseFailsWhenNoItemAsks()
    {
        var problems = BoundaryCases.OpenCaseProblems(BoundaryOutcome.Ask, [Finding("n1", FindingEffect.Warn, "when true")]);
        Assert.Contains(problems, text => text.StartsWith("no matched item asks", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(FindingEffect.Warn)]
    [InlineData(FindingEffect.Ask)]
    public void AnUnsettledCaseIsMetByOneItemThatWarnsOrAsks(FindingEffect effect)
    {
        Assert.Empty(BoundaryCases.OpenCaseProblems(BoundaryOutcome.Unsettled, [Finding("n1", effect, "when true")]));
    }

    [Fact]
    public void AnUnsettledCaseFailsWhenNoItemWarnsOrAsks()
    {
        var problems = BoundaryCases.OpenCaseProblems(BoundaryOutcome.Unsettled, []);
        Assert.Contains(problems, text => text.StartsWith("no matched item warns or asks", StringComparison.Ordinal));
    }

    // Second half: no matched item contradicts it.
    [Theory]
    [InlineData(BoundaryOutcome.Ask)]
    [InlineData(BoundaryOutcome.Unsettled)]
    public void ABlockContradictsEvenWhenAnotherItemResponds(BoundaryOutcome outcome)
    {
        var problems = BoundaryCases.OpenCaseProblems(outcome,
        [
            Finding("p1", FindingEffect.Ask, "notice_served unknown, asks notice_served"),
            Finding("d1", FindingEffect.BlockCleanExport, "expired"),
        ]);
        Assert.Equal(["d1"], problems.Select(text => text.Split(':')[0]));
        Assert.Contains("declared blockCleanExport", problems[0], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(BoundaryOutcome.Ask)]
    [InlineData(BoundaryOutcome.Unsettled)]
    public void AnItemThatAppliesAndStaysSilentContradicts(BoundaryOutcome outcome)
    {
        var problems = BoundaryCases.OpenCaseProblems(outcome,
        [
            Finding("n1", FindingEffect.Ask, "when unknown, asks notice_means"),
            Finding("p1", FindingEffect.None, "notice_served yes, the step was done"),
        ]);
        Assert.Equal(["p1"], problems.Select(text => text.Split(':')[0]));
        Assert.Contains("settles what the outcome", problems[0], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("requiredWhen false")]
    [InlineData("when false")]
    public void AnItemThatDoesNotApplyIsNotAContradiction(string branch)
    {
        var problems = BoundaryCases.OpenCaseProblems(BoundaryOutcome.Unsettled,
        [
            Finding("n1", FindingEffect.Warn, "when true"),
            Finding("p2", FindingEffect.None, branch),
        ]);
        Assert.Empty(problems);
        Assert.True(RuleEvaluator.IsNotApplicable(Finding("p2", FindingEffect.None, branch)));
        Assert.False(RuleEvaluator.IsNotApplicable(Finding("p2", FindingEffect.None, "notice_served no, exempt by notRequiredWhen[0]")));
    }
}
