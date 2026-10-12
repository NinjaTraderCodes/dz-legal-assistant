namespace DzLegal.Drafting.Tests;

public class GroundsSkeletonTests
{
    private const string Case01Notes = "المادتان 106 و107 من القانون المدني: العقد شريعة المتعاقدين ويجب تنفيذه طبقا لما اشتمل عليه وبحسن نية.\n"
        + "المادة 186 من القانون المدني: إذا كان محل الالتزام بين أفراد مبلغا من النقود وتأخر المدين في الوفاء به، فيجب عليه أن يعوض للدائن الضرر اللاحق من هذا التأخير، وقد أعذر المدعى عليه طبقا للمادة 179.";

    private static readonly LegalText[] Texts =
    [
        new("106", "القانون المدني", "العقد شريعة المتعاقدين، فلا يجوز نقضه، ولا تعديله الا باتفاق الطرفين، أو للاسباب التي يقررها القانون."),
        new("107", "القانون المدني", "يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية. غير أنه إذا طرأت حوادث استثنائية عامة لم يكن في الوسع توقعها جاز للقاضي أن يرد الالتزام المرهق إلى الحد المعقول."),
        new("186", "القانون المدني", "إذا كان محل الالتزام بين أفراد مبلغا من النقود عين مقداره وقت رفع الدعوى وتأخر المدين في الوفاء به، فيجب عليه أن يعوض للدائن الضرر اللاحق من هذا التأخير."),
    ];

    [Fact]
    public void EachLineOfTheNotesBecomesOneParagraphQuotingTheOfficialText()
    {
        var paragraphs = GroundsSkeleton.Build(Case01Notes, Texts);

        Assert.Equal(2, paragraphs.Count);
        Assert.Equal(["106 (القانون المدني)", "107 (القانون المدني)"], paragraphs[0].Articles);
        Assert.Equal(
            "المادة 106 من القانون المدني تنص على أن «العقد شريعة المتعاقدين، فلا يجوز نقضه، ولا تعديله الا باتفاق الطرفين، أو للاسباب التي يقررها القانون»، "
            + "والمادة 107 من القانون المدني تنص على أن «يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية»",
            paragraphs[0].Lead);

        // Art. 179's text is not loaded: cited by number only, never with content.
        Assert.EndsWith("، وذلك طبقا للمادة 179 من القانون المدني", paragraphs[1].Lead, StringComparison.Ordinal);
    }

    [Fact]
    public void FrenchReferencesFindTheirTexts()
    {
        var paragraphs = GroundsSkeleton.Build("Articles 106 et 107 du code civil : le contrat fait la loi des parties.", Texts[..2]);
        Assert.Equal(["106 (القانون المدني)", "107 (القانون المدني)"], Assert.Single(paragraphs).Articles);
    }

    [Fact]
    public void ArgumentsAreParsedByNumberWhateverTheirMarker()
    {
        var arguments = GroundsSkeleton.ParseArguments("إليك الحجج:\n[1] وحيث إن أ\n٢. وحيث إن ب\n[1] نسخة ثانية\n(7) زائدة");
        Assert.Equal("وحيث إن أ", arguments[1]);
        Assert.Equal("وحيث إن ب", arguments[2]);
        Assert.Equal("زائدة", arguments[7]);
        Assert.Equal(3, arguments.Count);
    }

    [Fact]
    public async Task TheCodeStatesTheLawAndAnArgumentThatCitesOrQuotesIsLeftForTheLawyer()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج ولم يرده رغم إعذاره.";
        inputs["notes.grounds"] = Case01Notes;
        var provider = new RecordingProvider(_ =>
            "[1] وحيث إن المدعى عليه امتنع عن رد مبلغ 1500000 دج، فإن طلب المدعي يكون مؤسسا.\n"
            + "[2] وحيث إن المادة 182 تنص على «التعويض الكامل»، فإن التعويض مستحق.");

        var draft = await new GroundsSkeleton(provider).DraftAsync(Fixtures.Requete, inputs, Texts);

        var lines = draft.Text.Split('\n');
        Assert.StartsWith("حيث إن المادة 106 من القانون المدني تنص على أن «العقد شريعة المتعاقدين", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("«يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية»، وحيث إن المدعى عليه امتنع عن رد مبلغ 1500000 دج، فإن طلب المدعي يكون مؤسسا.", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("وحيث إن (...).", lines[1], StringComparison.Ordinal);
        Assert.Contains("cites an article", Assert.Single(draft.Problems), StringComparison.Ordinal);
        Assert.DoesNotContain(NarrativeChecks.Check(draft.Text, Case01Notes, inputs, legalTexts: Texts), i => i.IsWrongArticleContent);
        Assert.Equal(GroundsSkeleton.RequestId, Assert.Single(provider.Requests).SectionId);
    }

    [Fact]
    public void AnArgumentWithANumberFromNowhereIsRejected()
    {
        var paragraphs = GroundsSkeleton.Build(Case01Notes, Texts);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج." };
        var draft = GroundsSkeleton.Assemble(null, paragraphs, "[1] وحيث إن المدعى عليه مدين بمبلغ 9000000 دج، فإن الطلب مؤسس.\n[2] وحيث إن المدعى عليه تأخر في الوفاء، فإن التعويض مستحق.", inputs, Texts);
        Assert.Single(draft.Problems);
        Assert.StartsWith("[1]", draft.Problems[0], StringComparison.Ordinal);
    }
}
