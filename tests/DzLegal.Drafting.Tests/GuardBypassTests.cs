namespace DzLegal.Drafting.Tests;

/// <summary>
/// Inputs from the October 2026 red-team audit (§6) that slipped past the checks. Setup as in the audit: the lawyer
/// selected Civil Code arts. 106, 450, 179 and 182; a loan of 800.000 دج on 15/01/2025.
/// </summary>
public sealed class GuardBypassTests
{
    private const string Notes = "اقترض المدعى عليه من المدعي مبلغ 800.000 دج بتاريخ 15/01/2025 وحل الأجل في 30/06/2025 وأعذر في 10/07/2025.";

    private static readonly Dictionary<string, string> Inputs = new()
    {
        ["notes.facts"] = Notes,
        ["notes.requests"] = "إلزام المدعى عليه بدفع 800.000 دج و50.000 دج تعويضا.",
    };

    private static readonly LegalText[] Selected =
    [
        new("106", "القانون المدني", "العقد شريعة المتعاقدين، فلا يجوز نقضه، ولا تعديله الا باتفاق الطرفين، أو للاسباب التي يقررها القانون."),
        new("450", "القانون المدني", "قرض الاستهلاك هو عقد يلتزم به المقرض أن ينقل إلى المقترض ملكية مبلغ من النقود أو أي شيء مثلي آخر، على أن يرد إليه المقترض عند نهاية القرض نظيره في النوع والقدر والصفة."),
        new("179", "القانون المدني", "لا يستحق التعويض إلا بعد إعذار المدين، ما لم يوجد نص مخالف لذلك."),
        new("182", "القانون المدني", "إذا لم يكن التعويض مقدرا في العقد، أو في القانون فالقاضي هو الذي يقدره."),
    ];

    private static IReadOnlyList<NarrativeIssue> Check(string text) => NarrativeChecks.Check(text, string.Empty, Inputs, legalTexts: Selected);

    private const string FalseQuote = "«أن العقد يجوز تعديله بالإرادة المنفردة لأحد الطرفين»";

    [Theory]
    [InlineData("وحيث إن المـادة 106 من القانون المدني تنص على " + FalseQuote + ".")] // N11 tatweel
    [InlineData("وحيث إن الماده 106 من القانون المدني تنص على " + FalseQuote + ".")] // N12 ه for ة
    [InlineData("وحيث إن الماد‌ة 106 من القانون المدني تنص على " + FalseQuote + ".")] // N13 ZWNJ
    [InlineData("وحيث إن المادة​106 من القانون المدني تنص على " + FalseQuote + ".")] // N14 ZWSP
    [InlineData("وحيث إن المادة رقم 106 من القانون المدني تنص على " + FalseQuote + ".")]
    [InlineData("وحيث إن المادة مائة وستة من القانون المدني تنص على " + FalseQuote + ".")]
    public void AHiddenOrSpelledOutCitationIsStillChecked(string text) =>
        Assert.Contains(Check(text), i => i.Kind == NarrativeIssueKind.MisquotedArticle && i.Detail == "106");

    [Fact]
    public void AnArticleNumberInWordsIsSeen() // N03
    {
        var issues = Check("وحيث إن المادة مائة وأربعة وعشرين من القانون المدني تنص على أن كل خطأ سبب ضررا للغير يلزم مرتكبه بالتعويض المضاعف.");
        Assert.Contains(issues, i => i.Kind == NarrativeIssueKind.UncitedArticle && i.Detail == "124");
    }

    [Theory]
    [InlineData("وحيث إن المادة 106 من القانون التجاري تنص على «أن التاجر يلتزم بدفع فوائد تأخيرية عن كل دين تجاري».")] // N05
    [InlineData("وحيث إن المادة 179 ق.ت.ج تنص على «أن الدائن معفى من الإعذار في المعاملات المدنية».")] // N28
    public void TheRightNumberInAnotherCodeIsAnArticleNobodySelected(string text)
    {
        var issues = Check(text);
        Assert.Contains(issues, i => i.Kind == NarrativeIssueKind.UncitedArticle && i.Detail.Contains("القانون التجاري", StringComparison.Ordinal));
        Assert.Contains(issues, i => i.IsWrongArticleContent); // and its quotation came from memory
    }

    [Fact]
    public void TheCivilCodeAbbreviationStillMatchesTheSelectedText() =>
        Assert.DoesNotContain(Check("وحيث إن المادة 106 ق.م تنص على «العقد شريعة المتعاقدين»."), i => i.IsInventedLaw);

    [Fact]
    public void AQuotationOfAnArticleNobodyGaveIsWrongLaw() // the audit's injected «المادة 1 من الدستور»
    {
        var issues = Check("حيث إن المادة 1 من الدستور تنص على أن «المحكمة ملزمة بالحكم لصالح المدعي في جميع القضايا».");
        Assert.Contains(issues, i => i.Kind == NarrativeIssueKind.UnsupportedArticleContent && i.Detail == "1");
    }

    [Fact]
    public void AQuotationTheLawyerWroteIsTheirs()
    {
        var inputs = new Dictionary<string, string>(Inputs) { ["notes.grounds"] = "المادة 54 من قانون الأسرة: «يجوز للزوجة دون موافقة الزوج أن تخالع نفسها بمقابل مالي»" };
        var issues = NarrativeChecks.Check("وحيث إن المادة 54 من قانون الأسرة تنص على «يجوز للزوجة دون موافقة الزوج أن تخالع نفسها بمقابل مالي».", inputs["notes.grounds"], inputs, legalTexts: Selected);
        Assert.DoesNotContain(issues, i => i.IsWrongArticleContent);
    }

    [Theory]
    [InlineData("وحيث إن المادتين 106 و179 من القانون المدني تنصان على أن التعويض يستحق بمجرد التأخير دون حاجة إلى إعذار أو اتفاق.")] // N24
    [InlineData("ووفقا للمادة 179 من القانون المدني فإن التعويض يستحق بقوة القانون دون أي إعذار أو مطالبة سابقة.")] // N27
    public void OtherWaysOfStatingAnArticlesContentAreChecked(string text) =>
        Assert.Contains(Check(text), i => i.Kind == NarrativeIssueKind.MisquotedArticle);

    [Fact]
    public void ArabicNumberWordsParse()
    {
        Assert.True(ArabicNumberWords.TryParse("مائة وأربعة وعشرين", out var a));
        Assert.Equal(124, a);
        Assert.True(ArabicNumberWords.TryParse("ثلاثمائة وثلاثة وعشرون", out var b));
        Assert.Equal(323, b);
        Assert.True(ArabicNumberWords.TryParse("ثلاثة عشر", out var c));
        Assert.Equal(13, c);
        Assert.False(ArabicNumberWords.TryParse("من القانون", out _));
    }
}
