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
        var issue = Assert.Single(Check("حيث إن الدعوى لم受理 قبول 1500000 بتاريخ 15 جانفي 2025"));
        Assert.Equal(NarrativeIssueKind.ForeignScript, issue.Kind);
        Assert.Equal("受理", issue.Detail);
        Assert.True(issue.IsHard);
    }

    [Fact]
    public void FrenchTermsAndAccentsFromTheInputsAreAllowed()
    {
        var inputs = new Dictionary<string, string>(Inputs) { ["notes.grounds"] = "reconnaissance de dette notariée ; créance certaine" };
        Assert.Empty(NarrativeChecks.Check("حيث إن المدعى عليه اقترض من المدعي مبلغ 1500000 دج بتاريخ 15 جانفي 2025 بموجب reconnaissance de dette notariée، وهو ما يجعل الدين ثابتا ومستحق الأداء بصفته créance certaine في ذمة المدعى عليه.", Notes, inputs));
    }

    [Fact]
    public void InventedArticleNumberIsFlagged()
    {
        var issue = Assert.Single(Check("حيث إن المادة 999 تلزم بدفع 1500000 دج منذ 15 جانفي 2025."));
        Assert.Equal(NarrativeIssueKind.UncitedArticle, issue.Kind);
        Assert.Equal("999", issue.Detail);
        Assert.False(issue.IsHard);
        Assert.True(issue.IsInventedLaw);
    }

    private static readonly LegalText[] Selected = [new("124", "القانون التجريبي", "كل فعل وهمي يرتكبه الشخص بخطئه ويسبب ضررا للغير يلزم من كان سببا في حدوثه بالتعويض.")];

    [Fact]
    public void SelectedArticlesCountAsKnownAndOtherArticlesAreFlagged()
    {
        var issues = NarrativeChecks.Check("حيث إن المادتين 124 و 131 توجبان التعويض عن مبلغ 1500000 دج منذ 15 جانفي 2025.", Notes, Inputs, legalTexts: Selected);
        var issue = Assert.Single(issues);
        Assert.Equal(NarrativeIssueKind.UncitedArticle, issue.Kind);
        Assert.Equal("131", issue.Detail);
    }

    [Fact]
    public void QuotationsMustMatchTheSelectedArticleText()
    {
        Assert.Empty(NarrativeChecks.Check(
            "حيث إن المادة 124 تنص على أن «كل فعلٍ وهمي يرتكبه الشخص بخطئه، ويسبب ضررا للغير» فإن المدعى عليه ملزم بأداء 1500000 دج منذ 15 جانفي 2025.",
            Notes, Inputs, legalTexts: Selected));

        var issue = Assert.Single(NarrativeChecks.Check(
            "حيث إن المادة 124 تنص على أن «كل من أخل بالتزامه يلزم بتعويض مضاعف» فإن المدعى عليه ملزم بأداء 1500000 دج منذ 15 جانفي 2025.",
            Notes, Inputs, legalTexts: Selected));
        Assert.Equal(NarrativeIssueKind.MisquotedArticle, issue.Kind);
        Assert.Equal("124", issue.Detail);
    }

    [Fact]
    public void QuotationsInPlainQuotesAreCheckedToo()
    {
        var issue = Assert.Single(NarrativeChecks.Check(
            "حيث إن المادة 124 تنص على \"كل من أخل بالتزامه يلزم بتعويض مضاعف\" فإن المدعى عليه ملزم بأداء 1500000 دج منذ 15 جانفي 2025.",
            Notes, Inputs, legalTexts: Selected));
        Assert.Equal(NarrativeIssueKind.MisquotedArticle, issue.Kind);
    }

    [Fact]
    public void WhatAnArticleIsSaidToProvideMustComeFromItsTextOrTheLawyersNotes()
    {
        // Close to the official text, in other words: accepted.
        Assert.Empty(NarrativeChecks.Check(
            "حيث إن المادة 124 تنص على أن كل فعل يرتكبه الشخص بخطئه ويسبب ضررا للغير يلزم صاحبه بالتعويض، فإن المدعى عليه ملزم بأداء 1500000 دج منذ 15 جانفي 2025.",
            Notes, Inputs, legalTexts: Selected));

        // Content the official text does not have: a misquotation.
        var misquoted = Assert.Single(NarrativeChecks.Check(
            "حيث إن المادة 124 تنص على أن الحقوق الخاصة لا تدعى إلا باللجوء إلى القضاء المختص، فإن المدعى عليه ملزم بأداء 1500000 دج منذ 15 جانفي 2025.",
            Notes, Inputs, legalTexts: Selected));
        Assert.Equal(NarrativeIssueKind.MisquotedArticle, misquoted.Kind);
    }

    [Fact]
    public void ContentOfAnArticleWithoutItsTextIsWrittenFromMemoryUnlessTheLawyerStatedIt()
    {
        var inputs = new Dictionary<string, string>(Inputs) { ["notes.grounds"] = "المادة 323 من القانون المدني: على الدائن اثبات الالتزام وعلى المدين اثبات التخلص منه." };
        var fromMemory = Assert.Single(NarrativeChecks.Check(
            "حيث إن المادة 323 من القانون المدني تنص على أن الحقوق الخاصة لا تدعى إلا باللجوء إلى القضاء المختص.",
            string.Empty, inputs),
            i => i.Kind == NarrativeIssueKind.UnsupportedArticleContent);
        Assert.Equal("323", fromMemory.Detail);
        Assert.True(fromMemory.IsWrongArticleContent);

        Assert.DoesNotContain(
            NarrativeChecks.Check("حيث إن المادة 323 من القانون المدني تنص على أن على الدائن اثبات الالتزام وعلى المدين اثبات التخلص منه.", string.Empty, inputs),
            i => i.IsWrongArticleContent);
    }

    [Fact]
    public void ArticlesWithTheSameBaseNumberAreToldApart()
    {
        LegalText[] both = [new("182", "القانون التجريبي", "يشمل التعويض الوهمي ما لحق الدائن من خسارة وما فاته من كسب."), new("182 مكرر", "القانون التجريبي", "يشمل التعويض الوهمي عن الضرر المعنوي كل مساس بالحرية أو الشرف.")];
        Assert.DoesNotContain(
            NarrativeChecks.Check("حيث إن المادة 182 مكرر تنص على «يشمل التعويض الوهمي عن الضرر المعنوي كل مساس بالحرية أو الشرف» فإن المدعى عليه ملزم بأداء 1500000 دج منذ 15 جانفي 2025.", Notes, Inputs, legalTexts: both),
            i => i.Kind == NarrativeIssueKind.MisquotedArticle);
        Assert.Contains(
            NarrativeChecks.Check("حيث إن المادة 182 تنص على «يشمل التعويض الوهمي عن الضرر المعنوي كل مساس بالحرية أو الشرف» فإن المدعى عليه ملزم بأداء 1500000 دج منذ 15 جانفي 2025.", Notes, Inputs, legalTexts: both),
            i => i.Kind == NarrativeIssueKind.MisquotedArticle);
    }

    [Fact]
    public void ShortQuotedTermsAndQuotesOfUnselectedArticlesAreNotJudged() =>
        Assert.Empty(NarrativeChecks.Check(
            "حيث إن «الخطأ» ثابت، وحيث إن المادة 124 من القانون التجريبي تلزم بأداء 1500000 دج منذ 15 جانفي 2025.",
            Notes, Inputs, legalTexts: Selected));

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
    public void StrayLatinWordFromTheSecondLocalRunIsFlagged()
    {
        var issue = Assert.Single(Check("حيث إنه في هذا caso اقترض المدعى عليه 1,500,000 دج في 15 جانفي 2025."));
        Assert.Equal(NarrativeIssueKind.UnexpectedLatinWord, issue.Kind);
        Assert.Equal("caso", issue.Detail);
        Assert.False(issue.IsHard);
    }

    [Fact]
    public void LatinWordsFromTheLawyersInputsAreAllowed()
    {
        var inputs = new Dictionary<string, string>(Inputs) { ["notes.grounds"] = "Reconnaissance de dette notariée" };
        Assert.Empty(NarrativeChecks.Check("حيث إن المدعى عليه اقترض من المدعي مبلغ 1500000 دج بموجب reconnaissance de dette موثقة بتاريخ 15 جانفي 2025", Notes, inputs));
    }

    [Fact]
    public void CopiedExampleParagraphIsDetectedEvenWhenReworded()
    {
        const string example = "حيث إن المادة (س) من القانون المدني تلزم البائع بتسليم المبيع، وحيث إن المدعى عليه بصفته بائعا قد أخل بهذا الالتزام، فإن طلب المدعي يكون مؤسسا قانونا.";
        const string reworded = "وحيث إن المادة (س) توجب على البائع التسليم، والمدعى عليه أخل بذلك.";
        const string legitimate = "حيث إن المادة 106 من القانون المدني تنص على أن العقد شريعة المتعاقدين، فإن طلب المدعي يكون مؤسسا قانونا.";

        var (text, removed) = NarrativeChecks.RemoveCopiedExample($"{reworded}\n{legitimate}", example, "المادة 106 من القانون المدني");

        Assert.Equal(1, removed);
        Assert.Equal(legitimate, text);
    }

    [Fact]
    public void WordsSharedWithTheNotesDoNotCountAsCopying()
    {
        const string example = "حيث إن المدعى عليه باع للمدعي سيارة ولم يسلمها إليه.";
        const string notes = "باع المدعى عليه للمدعي سيارة ولم يسلمها";
        var (text, removed) = NarrativeChecks.RemoveCopiedExample("حيث إن المدعى عليه باع للمدعي سيارة ولم يسلمها إليه.", example, notes);
        Assert.Equal(0, removed);
        Assert.NotEmpty(text);
    }

    [Fact]
    public void MonthNamesMatchNumericDates()
    {
        var inputs = new Dictionary<string, string> { ["notes.grounds"] = "le 15/01/2025, mise en demeure le 10/07/2025" };
        var issues = NarrativeChecks.Check("حيث إنه بتاريخ 15 جانفي 2025 ثم 10 يوليو 2025 وقع الإعذار.", "le 15/01/2025, mise en demeure le 10/07/2025", inputs);
        Assert.DoesNotContain(issues, i => i.Kind is NarrativeIssueKind.UnknownNumber or NarrativeIssueKind.MissingNumber);
    }

    [Fact]
    public void MostlyEnglishTextIsAHardIssue()
    {
        var issues = Check("Okay, let's tackle this legal drafting task: 1,500,000 DA on 15 جانفي 2025.");
        Assert.Contains(issues, i => i.Kind == NarrativeIssueKind.NotArabic && i.IsHard);
    }

    private static readonly string[] Terms = ["عقد المقاولة", "عقد القرض"];

    [Fact]
    public void QualificationFromBench3IsFlaggedEvenWithTashkeel()
    {
        var issues = NarrativeChecks.Check("حيث إن المادتين تنفيذاً لِعَقْدِ المُقَاوَلَة توجبان الوفاء بمبلغ 1,500,000 دج منذ 15 جانفي 2025.", Notes, Inputs, Terms);
        var issue = Assert.Single(issues);
        Assert.Equal(NarrativeIssueKind.UngroundedTerm, issue.Kind);
        Assert.Equal("عقد المقاولة", issue.Detail);
        Assert.False(issue.IsHard);
    }

    [Fact]
    public void QualificationTheLawyerUsedIsAllowed()
    {
        var inputs = new Dictionary<string, string>(Inputs) { ["notes.grounds"] = "عقد القرض ملزم للطرفين" };
        Assert.Empty(NarrativeChecks.Check("حيث إن عقد القرض المبرم بمبلغ 1,500,000 دج في 15 جانفي 2025 ملزم.", Notes, inputs, Terms));
    }

    [Fact]
    public void EmptyTextHasNoIssues() => Assert.Empty(Check("  "));
}
