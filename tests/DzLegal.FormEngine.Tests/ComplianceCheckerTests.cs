using DzLegal.Domain;
using static DzLegal.FormEngine.Tests.TestData;

namespace DzLegal.FormEngine.Tests;

public class ComplianceCheckerTests
{
    private static Finding CheckOne(RuleCheck check, DraftDocument draft, params Condition[] appliesWhen) =>
        ComplianceChecker.Check(Tiny(Element(check, appliesWhen)), draft).Findings.Single();

    [Fact]
    public void FieldsPresentPassesWhenAllFieldsHaveValues()
    {
        var finding = CheckOne(new FieldsPresentCheck(["a", "b"]), Draft(new() { ["a"] = "x", ["b"] = "y" }));
        Assert.Equal(ElementStatus.Present, finding.Status);
    }

    [Fact]
    public void FieldsPresentReportsEachBlankOrAbsentField()
    {
        var finding = CheckOne(new FieldsPresentCheck(["a", "b"]), Draft(new() { ["a"] = "  " }));
        Assert.Equal(ElementStatus.Missing, finding.Status);
        Assert.Equal(["a", "b"], finding.MissingParts);
    }

    [Theory]
    [InlineData("نص", ElementStatus.Present)]
    [InlineData("", ElementStatus.Missing)]
    [InlineData(" \n\t ", ElementStatus.Missing)]
    public void SectionNotEmpty(string text, ElementStatus expected) =>
        Assert.Equal(expected, CheckOne(new SectionNotEmptyCheck("s1"), Draft(null, ("s1", text))).Status);

    [Fact]
    public void SectionNotEmptyFailsWhenSectionIsAbsent() =>
        Assert.Equal(ElementStatus.Missing, CheckOne(new SectionNotEmptyCheck("s1"), Draft()).Status);

    [Theory]
    [InlineData("في: 15/09/2026", ElementStatus.Present)]
    [InlineData("في: ........", ElementStatus.Missing)]
    public void SectionMatches(string text, ElementStatus expected) =>
        Assert.Equal(expected, CheckOne(new SectionMatchesCheck("s1", @"\d{2}/\d{2}/\d{4}"), Draft(null, ("s1", text))).Status);

    [Fact]
    public void SectionOrderPassesInOrderAndIgnoresAbsentSections()
    {
        var finding = CheckOne(new SectionOrderCheck(["s1", "s2", "s3"]), Draft(null, ("s1", "x"), ("s3", "z")));
        Assert.Equal(ElementStatus.Present, finding.Status);
    }

    [Fact]
    public void SectionOrderReportsTheInvertedPair()
    {
        var finding = CheckOne(new SectionOrderCheck(["s1", "s2", "s3"]), Draft(null, ("s2", "y"), ("s1", "x"), ("s3", "z")));
        Assert.Equal(ElementStatus.Missing, finding.Status);
        Assert.Equal(["s1<s2"], finding.MissingParts);
    }

    [Theory]
    [InlineData("حيث إن المدعى عليه لم يسدد الدين", ElementStatus.Present)]
    [InlineData("حيث إن العارض أقرض المدعى عليه مبلغا ماليا بموجب reconnaissance de dette محررة لدى الموثق", ElementStatus.Present)]
    [InlineData("Attendu que le défendeur n'a pas payé", ElementStatus.Missing)]
    [InlineData("", ElementStatus.Present)]
    [InlineData("500000 - 10/01/2025", ElementStatus.Present)]
    public void PredominantlyArabic(string text, ElementStatus expected) =>
        Assert.Equal(expected, CheckOne(new PredominantlyArabicCheck(["s1"]), Draft(null, ("s1", text))).Status);

    [Fact]
    public void AnyOfPassesWhenOneAlternativeHolds()
    {
        var check = new AnyOfCheck([new FieldsPresentCheck(["a"]), new FieldsPresentCheck(["b"])]);
        Assert.Equal(ElementStatus.Present, CheckOne(check, Draft(new() { ["b"] = "y" })).Status);
    }

    [Fact]
    public void AnyOfReportsAlternativesTogether()
    {
        var check = new AnyOfCheck([new FieldsPresentCheck(["a"]), new FieldsPresentCheck(["b"])]);
        var finding = CheckOne(check, Draft());
        Assert.Equal(["a|b"], finding.MissingParts);
    }

    [Fact]
    public void AllOfCollectsEveryMissingPart()
    {
        var check = new AllOfCheck([new FieldsPresentCheck(["a"]), new SectionNotEmptyCheck("s1")]);
        var finding = CheckOne(check, Draft(new() { ["a"] = "" }));
        Assert.Equal(["a", "s1"], finding.MissingParts);
    }

    [Theory]
    [InlineData("true", ElementStatus.Missing)]
    [InlineData("false", ElementStatus.NotApplicable)]
    [InlineData("", ElementStatus.NotApplicable)]
    public void AppliesWhenControlsApplicability(string flag, ElementStatus expected)
    {
        var finding = CheckOne(new FieldsPresentCheck(["a"]), Draft(new() { ["flag"] = flag }), new Condition { Field = "flag", Is = "true" });
        Assert.Equal(expected, finding.Status);
    }

    [Fact]
    public void ReportIsAdmissibleWhenOnlyWarningsAreMissing()
    {
        var warning = Element(new FieldsPresentCheck(["a"])) with { Severity = Severity.Warning };
        var report = ComplianceChecker.Check(Tiny(warning), Draft());
        Assert.Single(report.Missing);
        Assert.True(report.IsAdmissibleInForm);
    }

    [Fact]
    public void ReportIsInadmissibleWhenAMandatoryElementIsMissing()
    {
        var report = ComplianceChecker.Check(Tiny(Element(new FieldsPresentCheck(["a"]))), Draft());
        Assert.False(report.IsAdmissibleInForm);
    }

    [Fact]
    public void CheckingIsDeterministic()
    {
        var definition = TestData.Requete;
        var draft = Harness.CorpusLoader.Load(Path.Combine(CorpusRoot, "requete-introductive", "empty-document.case.json")).Draft;
        var first = ComplianceChecker.Check(definition, draft);
        var second = ComplianceChecker.Check(definition, draft);
        Assert.Equal(
            first.Findings.Select(f => (f.ElementId, f.Status, string.Join(',', f.MissingParts))),
            second.Findings.Select(f => (f.ElementId, f.Status, string.Join(',', f.MissingParts))));
    }

    [Fact]
    public void RejectsDraftOfAnotherDocumentType()
    {
        var draft = Draft() with { DocumentTypeId = "other" };
        Assert.Throws<ArgumentException>(() => ComplianceChecker.Check(Tiny(), draft));
    }
}
