using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>The effect table of rules/GRADES.md, every cell, and the origin rule.</summary>
public sealed class GradeTests
{
    [Theory]
    [InlineData(FindingEffect.BlockCleanExport, "A", FindingEffect.BlockCleanExport, false)]
    [InlineData(FindingEffect.BlockCleanExport, "B", FindingEffect.Warn, false)]
    [InlineData(FindingEffect.BlockCleanExport, "C", FindingEffect.Warn, false)]
    [InlineData(FindingEffect.BlockCleanExport, "D", FindingEffect.Warn, true)]
    [InlineData(FindingEffect.BlockCleanExport, null, FindingEffect.Warn, true)]
    [InlineData(FindingEffect.Warn, "A", FindingEffect.Warn, false)]
    [InlineData(FindingEffect.Warn, "B", FindingEffect.Warn, false)]
    [InlineData(FindingEffect.Warn, "C", FindingEffect.Warn, false)]
    [InlineData(FindingEffect.Warn, "D", FindingEffect.Warn, true)]
    [InlineData(FindingEffect.Warn, null, FindingEffect.Warn, true)]
    [InlineData(FindingEffect.Ask, "A", FindingEffect.Ask, false)]
    [InlineData(FindingEffect.Ask, "B", FindingEffect.Ask, false)]
    [InlineData(FindingEffect.Ask, "C", FindingEffect.Ask, false)]
    [InlineData(FindingEffect.Ask, "D", FindingEffect.Ask, true)]
    [InlineData(FindingEffect.Ask, null, FindingEffect.Ask, true)]
    [InlineData(FindingEffect.None, "A", FindingEffect.None, false)]
    [InlineData(FindingEffect.None, "B", FindingEffect.None, false)]
    [InlineData(FindingEffect.None, "C", FindingEffect.None, false)]
    [InlineData(FindingEffect.None, "D", FindingEffect.None, true)]
    [InlineData(FindingEffect.None, null, FindingEffect.None, true)]
    public void EveryCellOfTheGradeTable(FindingEffect declared, string? grade, FindingEffect shown, bool toCheck)
    {
        var grading = grade is null ? null : Build.Grade(grade);
        Assert.Equal((shown, toCheck), GradeTable.Apply(declared, GradeTable.GradeOf(grading), usesProposedSlot: false));
    }

    [Theory]
    [InlineData(FindingEffect.BlockCleanExport, FindingEffect.Warn)]
    [InlineData(FindingEffect.Warn, FindingEffect.Warn)]
    [InlineData(FindingEffect.Ask, FindingEffect.Ask)]
    [InlineData(FindingEffect.None, FindingEffect.None)]
    public void AProposedValueNeverBlocksAndChangesNothingElse(FindingEffect declared, FindingEffect shown) =>
        Assert.Equal((shown, false), GradeTable.Apply(declared, Grades.A, usesProposedSlot: true));

    [Theory]
    [InlineData("start=2026-01-06", FindingEffect.BlockCleanExport)]
    [InlineData("start~2026-01-06", FindingEffect.Warn)]
    public void AGradeADeadlineBlocksOnlyOnConfirmedValues(string slots, FindingEffect shown)
    {
        var finding = Build.Run(Build.Record() with { Deadlines = [Build.Deadline()] }, Build.Case(slots), new DateOnly(2026, 6, 1)).Finding("d1");
        Assert.Equal(FindingEffect.BlockCleanExport, finding.DeclaredEffect);
        Assert.Equal(shown, finding.Effect);
        Assert.Equal(Grades.A, finding.Grade);
    }

    [Fact]
    public void AProposedSlotInTheConditionAlsoKeepsTheBlockAway()
    {
        var record = Build.Record() with { Deadlines = [Build.Deadline(when: Build.Is("q", "yes"))] };
        var finding = Build.Run(record, Build.Case("q~yes;start=2026-01-06"), new DateOnly(2026, 6, 1)).Finding("d1");
        Assert.Equal(FindingEffect.Warn, finding.Effect);
        Assert.EndsWith("grade A -> blockCleanExport; a proposed slot -> warn", finding.Trace, StringComparison.Ordinal);
    }

    [Fact]
    public void AGradeDBlockIsAMarkedWarningNeverWeakerAndNeverAbsent()
    {
        var finding = Build.Run(Build.Record() with { Deadlines = [Build.Deadline(grade: Grades.D)] }, Build.Case("start=2026-01-06"), new DateOnly(2026, 6, 1)).Finding("d1");
        Assert.Equal(FindingEffect.BlockCleanExport, finding.DeclaredEffect);
        Assert.Equal(FindingEffect.Warn, finding.Effect);
        Assert.True(finding.ToCheck);
        Assert.Equal(Grades.D, finding.Grade);
    }

    [Fact]
    public void AGradeDQuestionKeepsItsEffectAndCarriesTheMark()
    {
        var finding = Build.Run(Build.Record() with { PriorSteps = [Build.Step(grade: Grades.D)] }, Build.Case()).Finding("p1");
        Assert.Equal(FindingEffect.Ask, finding.Effect);
        Assert.True(finding.ToCheck);
    }

    [Fact]
    public void AnItemWithoutGradingCountsAsGradeD()
    {
        var finding = Build.Run(Build.Record() with { PriorSteps = [Build.Step() with { Grading = null }] }, Build.Case("done=no")).Finding("p1");
        Assert.Equal(FindingEffect.BlockCleanExport, finding.DeclaredEffect);
        Assert.Equal(FindingEffect.Warn, finding.Effect);
        Assert.Equal(Grades.D, finding.Grade);
        Assert.True(finding.ToCheck);
    }

    [Theory]
    [InlineData(Grades.B)]
    [InlineData(Grades.C)]
    public void BelowGradeABlockIsAnUnmarkedWarning(string grade)
    {
        var finding = Build.Run(Build.Record() with { PriorSteps = [Build.Step(grade: grade)] }, Build.Case("done=no")).Finding("p1");
        Assert.Equal(FindingEffect.Warn, finding.Effect);
        Assert.False(finding.ToCheck);
    }
}
