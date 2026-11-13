using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>Each kind of item, each branch (specs/rule-evaluator.md, "What each kind of item produces"), on small records written here.</summary>
public sealed class EvaluatorTests
{
    // A deadline of 10 days counted from the day after the start (excludeFirstDay): started 2026-01-06, last day 2026-01-16.
    private static readonly DateOnly LastDay = new(2026, 1, 16);

    [Fact]
    public void ADeadlineWhoseConditionIsFalseSaysNothing()
    {
        var finding = Build.Run(Build.Record() with { Deadlines = [Build.Deadline(when: Build.Is("q", "yes"))] }, Build.Case("q=no;start=2020-01-01")).Finding("d1");
        Assert.Equal(FindingEffect.None, finding.DeclaredEffect);
        Assert.Null(finding.LastDay);
    }

    [Fact]
    public void ADeadlineWhoseConditionIsUnknownAsksForTheConditionsSlots()
    {
        var outcome = Build.Run(Build.Record() with { Deadlines = [Build.Deadline(when: Build.Is("q", "yes"))] }, Build.Case("start=2020-01-01"));
        Assert.Equal(FindingEffect.Ask, outcome.Finding("d1").DeclaredEffect);
        var question = Assert.Single(outcome.Questions);
        Assert.Equal("q", question.Slot);
        Assert.Equal(["d1"], question.NeededBy);
    }

    [Fact]
    public void ADeadlineWhoseStartIsUnknownAsksForTheStart()
    {
        var outcome = Build.Run(Build.Record() with { Deadlines = [Build.Deadline(when: Build.Is("q", "yes"))] }, Build.Case("q=yes"));
        Assert.Equal(FindingEffect.Ask, outcome.Finding("d1").DeclaredEffect);
        Assert.Equal(["start"], outcome.Asked());
    }

    [Theory]
    [InlineData(-20, FindingEffect.None)] // 20 days left
    [InlineData(-16, FindingEffect.None)] // just outside the band
    [InlineData(-15, FindingEffect.Warn)] // the band's first edge
    [InlineData(0, FindingEffect.Warn)] // the last day
    [InlineData(15, FindingEffect.Warn)] // the band's other edge
    [InlineData(16, FindingEffect.BlockCleanExport)] // just outside it: expired
    public void ADeadlineWarnsInsideItsBandBeforeOrAfterAndOutsideItIsInTimeOrExpired(int daysAfterLastDay, FindingEffect expected)
    {
        var finding = Build.Run(Build.Record() with { Deadlines = [Build.Deadline()] }, Build.Case("start=2026-01-06"), LastDay.AddDays(daysAfterLastDay)).Finding("d1");
        Assert.Equal(expected, finding.DeclaredEffect);
        Assert.Equal(LastDay, finding.LastDay);
        Assert.Equal(-daysAfterLastDay, finding.DaysLeft);
    }

    [Theory]
    [InlineData(0, FindingEffect.Warn)]
    [InlineData(1, FindingEffect.Warn)] // expired, but its record declares a warning
    public void AnExpiredDeadlineDeclaresWhatItsRecordSays(int band, FindingEffect expected)
    {
        var record = Build.Record() with { Deadlines = [Build.Deadline(expired: FindingEffect.Warn, band: band)] };
        Assert.Equal(expected, Build.Run(record, Build.Case("start=2026-01-06"), LastDay.AddDays(5)).Finding("d1").DeclaredEffect);
    }

    [Fact]
    public void ABandOfZeroWarnsOnTheLastDayOnly()
    {
        var record = Build.Record() with { Deadlines = [Build.Deadline(band: 0)] };
        Assert.Equal(FindingEffect.Warn, Build.Run(record, Build.Case("start=2026-01-06"), LastDay).Finding("d1").DeclaredEffect);
        Assert.Equal(FindingEffect.BlockCleanExport, Build.Run(record, Build.Case("start=2026-01-06"), LastDay.AddDays(1)).Finding("d1").DeclaredEffect);
        Assert.Equal(FindingEffect.None, Build.Run(record, Build.Case("start=2026-01-06"), LastDay.AddDays(-1)).Finding("d1").DeclaredEffect);
    }

    [Fact]
    public void ThePriorStepIsNotRequiredWhenItsConditionIsFalse() =>
        Assert.Equal(FindingEffect.None, Build.Run(Build.Record() with { PriorSteps = [Build.Step(requiredWhen: Build.Is("q", "yes"))] }, Build.Case("q=no;done=no")).Finding("p1").DeclaredEffect);

    [Fact]
    public void APriorStepWhoseConditionIsUnknownAsksForItAndNotYetForTheStep()
    {
        var outcome = Build.Run(Build.Record() with { PriorSteps = [Build.Step(requiredWhen: Build.Is("q", "yes"))] }, Build.Case());
        Assert.Equal(FindingEffect.Ask, outcome.Finding("p1").DeclaredEffect);
        Assert.Equal(["q"], outcome.Asked());
    }

    [Fact]
    public void AStepThatWasDoneSaysNothing() =>
        Assert.Equal(FindingEffect.None, Build.Run(Build.Record() with { PriorSteps = [Build.Step()] }, Build.Case("done=yes")).Finding("p1").DeclaredEffect);

    [Theory]
    [InlineData(FindingEffect.Ask)]
    [InlineData(FindingEffect.Warn)]
    public void AStepNobodyMentionedTakesItsEffectWhenUnknownAndAlwaysAsksForTheStepAlone(FindingEffect whenUnknown)
    {
        var record = Build.Record() with { PriorSteps = [Build.Step(exemptions: [Build.Is("abroad", "yes")], unknown: whenUnknown)] };
        var outcome = Build.Run(record, Build.Case());
        Assert.Equal(whenUnknown, outcome.Finding("p1").DeclaredEffect);
        Assert.Equal(["done"], outcome.Asked()); // the exemptions wait until the lawyer says the step was not done
    }

    [Fact]
    public void AStepNotDoneIsExemptWhenAnyExemptionHolds() =>
        Assert.Equal(FindingEffect.None, Build.Run(
            Build.Record() with { PriorSteps = [Build.Step(exemptions: [Build.Is("abroad", "yes"), Build.Is("bankrupt", "yes")])] },
            Build.Case("done=no;abroad=unknown;bankrupt=yes")).Finding("p1").DeclaredEffect);

    [Fact]
    public void AStepNotDoneAsksForTheExemptionsStillUnknownOnly()
    {
        var record = Build.Record() with { PriorSteps = [Build.Step(exemptions: [Build.Is("abroad", "yes"), Build.Is("bankrupt", "yes")])] };
        var outcome = Build.Run(record, Build.Case("done=no;abroad=no"));
        Assert.Equal(FindingEffect.Ask, outcome.Finding("p1").DeclaredEffect);
        Assert.Equal(["bankrupt"], outcome.Asked());
    }

    [Theory]
    [InlineData("done=no;abroad=no;bankrupt=no")]
    [InlineData("done=no")]
    public void AStepNotDoneWithNoExemptionTakesItsEffectWhenMissing(string slots)
    {
        var exemptions = slots.Contains("abroad", StringComparison.Ordinal) ? new[] { Build.Is("abroad", "yes"), Build.Is("bankrupt", "yes") } : [];
        var outcome = Build.Run(Build.Record() with { PriorSteps = [Build.Step(exemptions: exemptions, missing: FindingEffect.Warn)] }, Build.Case(slots));
        Assert.Equal(FindingEffect.Warn, outcome.Finding("p1").DeclaredEffect);
        Assert.Empty(outcome.Questions);
    }

    [Fact]
    public void AStepSlotThatIsNeitherYesNorNoIsRefused() =>
        Assert.Throws<ArgumentException>(() => Build.Run(Build.Record() with { PriorSteps = [Build.Step()] }, Build.Case("done=maybe")));

    [Fact]
    public void AStepWithoutItsOwnSlotIsRefused() =>
        Assert.Throws<RuleRecordException>(() => Build.Run(Build.Record() with { PriorSteps = [Build.Step() with { Slots = [] }] }, Build.Case()));

    [Theory]
    [InlineData("q=yes", FindingEffect.Warn)]
    [InlineData("q=no", FindingEffect.None)]
    [InlineData("", FindingEffect.Ask)]
    public void AnExitWarnsWhenItsConditionHolds(string slots, FindingEffect expected)
    {
        var outcome = Build.Run(Build.Record() with { Court = new CourtRules { Exits = [Build.Exit("x1", Build.Is("q", "yes"))] } }, Build.Case(slots));
        Assert.Equal(expected, outcome.Finding("x1").DeclaredEffect);
        Assert.Equal(FindingKind.Exit, outcome.Finding("x1").Kind);
        Assert.Equal(expected == FindingEffect.Ask ? new[] { "q" } : [], outcome.Asked());
    }

    [Theory]
    [InlineData("q=yes", FindingEffect.Warn)]
    [InlineData("q=no", FindingEffect.None)]
    [InlineData("", FindingEffect.Ask)]
    public void ABoundaryWithAConditionWarnsWhenItHolds(string slots, FindingEffect expected) =>
        Assert.Equal(expected, Build.Run(Build.Record() with { Boundaries = [Build.Boundary("n1", Build.Is("q", "yes"))] }, Build.Case(slots)).Finding("n1").DeclaredEffect);

    [Fact]
    public void ABoundaryWithoutAConditionIsDocumentationAndGivesNoFinding() =>
        Assert.Empty(Build.Run(Build.Record() with { Boundaries = [Build.Boundary("n1", null)] }, Build.Case()).Findings);

    [Fact]
    public void TheSectionIsProposedAndAFindingAppearsOnlyWhenTheWrittenSectionDiffers()
    {
        var record = Build.Record() with
        {
            Court = new CourtRules { Section = new SectionRule { Value = CourtSection.Social, Basis = ["s1"], Message = Build.Text("section"), Grading = Build.Grade(Grades.B) } },
        };

        var none = Build.Run(record, Build.Case());
        Assert.Empty(none.Findings);
        Assert.Equal(CourtSection.Social, none.Section!.Section);
        Assert.Equal(Grades.B, none.Section.Grade);
        Assert.Equal("cpca", Assert.Single(none.Section.Basis).Code);

        Assert.Empty(Build.Run(record, Build.Case(section: CourtSection.Social)).Findings);

        var finding = Build.Run(record, Build.Case(section: CourtSection.Civil)).Finding(RuleEvaluator.SectionRef);
        Assert.Equal(FindingKind.CourtSection, finding.Kind);
        Assert.Equal(FindingEffect.Warn, finding.DeclaredEffect);
        Assert.Equal("section", finding.Message.En);
    }

    [Fact]
    public void TheFirstTrueAlternativeGivesTheSectionAndUnknownOnesBeforeItAreAsked()
    {
        var record = Build.Record() with
        {
            Basis = [Build.Statement("s1"), Build.Statement("s2", article: "2")],
            Court = new CourtRules
            {
                Section = new SectionRule
                {
                    Value = CourtSection.Civil,
                    Basis = ["s1"],
                    Message = Build.Text("section"),
                    Alternatives =
                    [
                        new SectionAlternative { When = Build.Is("a", "yes"), Value = CourtSection.Commercial, Basis = ["s1"] },
                        new SectionAlternative { When = Build.Is("b", "yes"), Value = CourtSection.RealEstate, Basis = ["s2"] },
                        new SectionAlternative { When = Build.Is("c", "yes"), Value = CourtSection.Family, Basis = ["s1"] },
                    ],
                },
            },
        };

        var outcome = Build.Run(record, Build.Case("b=yes"));
        Assert.Equal(CourtSection.RealEstate, outcome.Section!.Section);
        Assert.Equal("2", Assert.Single(outcome.Section.Basis).Article);
        var question = Assert.Single(outcome.Questions);
        Assert.Equal("a", question.Slot);
        Assert.Equal([RuleEvaluator.SectionRef], question.NeededBy);

        Assert.Equal(CourtSection.Civil, Build.Run(record, Build.Case("a=no;b=no;c=no")).Section!.Section);
    }

    [Fact]
    public void ARecordWithoutASectionRuleProposesNone() => Assert.Null(Build.Run(Build.Record(), Build.Case()).Section);

    [Theory]
    [InlineData(null, true)]
    [InlineData("q=yes", true)]
    [InlineData("q=no", false)]
    [InlineData("", true)]
    public void ADocumentIsListedUnlessItsConditionIsFalseAndAsksNothing(string? slots, bool listed)
    {
        var document = new RequiredDocument
        {
            Id = "doc1",
            Name = Build.Text("the record"),
            Why = Build.Text("why"),
            RequiredWhen = slots is null ? null : Build.Is("q", "yes"),
            Grading = Build.Grade(Grades.D),
        };
        var outcome = Build.Run(Build.Record() with { Documents = [document] }, Build.Case(slots ?? string.Empty));
        Assert.Empty(outcome.Questions);
        if (!listed)
        {
            Assert.Empty(outcome.Findings);
            return;
        }

        var finding = outcome.Finding("doc1");
        Assert.Equal(FindingKind.Document, finding.Kind);
        Assert.Equal(FindingEffect.None, finding.Effect);
        Assert.Equal("the record", finding.Message.En);
        Assert.True(finding.ToCheck);
    }

    [Fact]
    public void QuestionsComeOnceInTheOrderRecordsThenExitsSectionStepsDeadlinesBoundaries()
    {
        var first = Build.Record("first") with
        {
            Boundaries = [Build.Boundary("n1", Build.Is("q5", "yes"))],
            Deadlines = [Build.Deadline(when: Build.Is("q4", "yes"))],
            PriorSteps = [Build.Step(requiredWhen: Build.Is("q3", "yes"))],
            Court = new CourtRules
            {
                Exits = [Build.Exit("x1", Build.Is("q1", "yes")), Build.Exit("x2", Build.Is("q4", "yes"))],
                Section = new SectionRule
                {
                    Value = CourtSection.Civil,
                    Basis = ["s1"],
                    Message = Build.Text("section"),
                    Alternatives = [new SectionAlternative { When = Build.Is("q2", "yes"), Value = CourtSection.Family, Basis = ["s1"] }],
                },
            },
        };
        var second = Build.Record("second") with { Court = new CourtRules { Exits = [Build.Exit("x1", Build.Is("q0", "yes"))] } };

        var outcome = new RuleEvaluator().Evaluate([first, second], Build.Case(), Build.Petition);
        Assert.Equal(["q1", "q4", "q2", "q3", "q5", "q0"], outcome.Asked());
        Assert.Equal(["x2", "d1"], outcome.Questions.Single(question => question.Slot == "q4").NeededBy);
        Assert.Equal(["x1"], outcome.Questions.Single(question => question.Slot == "q0").NeededBy);
        Assert.Equal(2, outcome.Findings.Count(finding => finding.Ref == "x1"));
    }

    [Fact]
    public void AFindingCarriesItsRecordItsTextsItsSlotsAndATrace()
    {
        var record = Build.Record() with { Deadlines = [Build.Deadline(when: Build.Is("q", "yes"))] };
        var finding = Build.Run(record, Build.Case("q=yes;start=2026-01-06"), new DateOnly(2026, 3, 1)).Finding("d1");
        Assert.Equal("unit", finding.RecordId);
        Assert.Equal(FindingKind.Deadline, finding.Kind);
        Assert.Equal("1", Assert.Single(finding.Basis).Article);
        Assert.Equal(["q", "start"], finding.SlotsUsed);
        Assert.Equal("d1: start 2026-01-06, last day 2026-01-16, petition 2026-03-01, 44 days late, outside band 15 -> blockCleanExport; grade A -> blockCleanExport", finding.Trace);
    }

    [Fact]
    public void TheEvaluatorRunsEveryRecordItIsGivenWhateverTheClaimIdsSay()
    {
        var slots = Build.Case("q=yes") with { ClaimIds = ["something-else"] };
        var outcome = new RuleEvaluator().Evaluate(
            [Build.Record("a") with { Court = new CourtRules { Exits = [Build.Exit("x1", Build.Is("q", "yes"))] } }, Build.Record("b") with { Court = new CourtRules { Exits = [Build.Exit("x9", Build.Is("q", "yes"))] } }],
            slots,
            Build.Petition);
        Assert.Equal(["a", "b"], outcome.Findings.Select(finding => finding.RecordId));
    }
}
