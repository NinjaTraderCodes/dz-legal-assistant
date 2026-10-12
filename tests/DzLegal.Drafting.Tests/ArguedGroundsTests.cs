namespace DzLegal.Drafting.Tests;

public class ArguedGroundsTests
{
    private const string Civil = "القانون المدني";

    private static readonly LegalText[] Ticked =
    [
        new("450", Civil, "قرض الاستهلاك هو عقد يلتزم به المقرض أن ينقل إلى المقترض ملكية مبلغ من النقود.") { Role = "basis", Gist = "يلتزم المقترض برد نظير ما اقترضه عند نهاية القرض" },
        new("106", Civil, "العقد شريعة المتعاقدين.") { Role = "basis", Gist = "العقد شريعة المتعاقدين" },
        new("107", Civil, "يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية.") { Role = "basis", Gist = "يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية" },
        new("323", Civil, "على الدائن اثبات الالتزام وعلى المدين اثبات التخلص منه.") { Role = "proof", Gist = "على الدائن إثبات الالتزام وعلى المدين إثبات التخلص منه" },
        new("454", Civil, "القرض بين الافراد يكون دائما بدون أجر.") { Role = "limit", Gist = "القرض بين الأفراد يكون دائما بدون أجر: لا تطلب فوائد" },
        new("179", Civil, "لا يستحق التعويض إلا بعد اعذار المدين.") { Role = "prerequisite", Gist = "لا يستحق التعويض إلا بعد إعذار المدين" },
        new("186", Civil, "إذا كان محل الالتزام بين أفراد مبلغا من النقود وتأخر المدين في الوفاء به، فيجب عليه أن يعوض للدائن الضرر.") { Role = "damages", Gist = "تعويض الدائن عن الضرر اللاحق من تأخر المدين في الوفاء بمبلغ من النقود" },
    ];

    private const string Notes = "- المادة 450 من القانون المدني\n- المادة 106 من القانون المدني\n- المادة 107 من القانون المدني\n- المادة 323 من القانون المدني\n- المادة 454 من القانون المدني\n- المادة 179 من القانون المدني\n- المادة 186 من القانون المدني";
    private const string Requests = "إلزام المدعى عليه بأن يدفع للمدعي 1500000 دج.\nإلزامه بأن يدفع 150000 دج تعويضا عن التأخير.";

    [Fact]
    public void TheArticlesAreGroupedByTheirPartInTheClaimAndALimitIsLeftOut()
    {
        var groups = ArguedGrounds.Plan(Notes, Ticked, Requests);

        Assert.Equal(3, groups.Count);
        Assert.Equal("المواد 106 و107 و450 من القانون المدني", groups[0].Citation);
        Assert.Equal("إلزام المدعى عليه بأن يدفع للمدعي 1500000 دج.", groups[0].Request);
        Assert.Equal("المادة 323 من القانون المدني", groups[1].Citation);
        Assert.Null(groups[1].Request);
        Assert.Equal("المادتين 179 و186 من القانون المدني", groups[2].Citation);
        Assert.Contains("150000", groups[2].Request, StringComparison.Ordinal);
        Assert.DoesNotContain(groups, g => g.Texts.Any(t => t.Number == "454"));
    }

    [Fact]
    public void ALimitTheLawyerArguesInTheNotesStays()
    {
        var groups = ArguedGrounds.Plan("المادة 454 من القانون المدني: لا فوائد على القرض بين الأفراد، فالمدعي لا يطلبها.", Ticked, Requests);
        Assert.Contains(groups, g => g.Texts.Any(t => t.Number == "454"));
    }

    [Fact]
    public void AMissingCitationIsAddedAndAnArticleOutsideThePlanIsRejected()
    {
        var groups = ArguedGrounds.Plan(Notes, Ticked, Requests);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.", ["notes.requests"] = Requests };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "حيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، عملا بالمادتين 106 و107 من القانون المدني، مما يجعل طلب المدعي مؤسسا قانونا.",
            [2] = "وحيث إن الدين ثابت بموجب اعتراف بدين، وهو ما تقضي به المادة 323 من القانون المدني.",
            [3] = "وحيث إن المادة 182 من القانون المدني تجيز التعويض، فإن طلب التعويض مؤسس.",
        }, inputs, Ticked);

        var lines = draft.Text.Split('\n');
        Assert.StartsWith("حيث إن المدعى عليه اقترض", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("(عملا بالمادة 450 من القانون المدني).", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("وحيث إن الدين ثابت", lines[1], StringComparison.Ordinal);
        Assert.Equal("وحيث إن (...)، عملا بالمادتين 179 و186 من القانون المدني.", lines[2]);
        Assert.Contains("182", Assert.Single(draft.Problems), StringComparison.Ordinal);
    }

    [Fact]
    public void TheCatalogsOneLineRuleIsAcceptedWordingButOtherContentIsNot()
    {
        LegalText[] texts = [new("323", Civil, "نص رسمي مختلف الصياغة كليا لأغراض هذا الاختبار وحده.") { Gist = "على الدائن إثبات الالتزام وعلى المدين إثبات التخلص منه" }];
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغا." };

        Assert.DoesNotContain(
            NarrativeChecks.Check("وحيث إن المادة 323 من القانون المدني تنص على أن على الدائن إثبات الالتزام وعلى المدين إثبات التخلص منه، فإن الدين ثابت.", string.Empty, inputs, legalTexts: texts),
            i => i.IsWrongArticleContent);
        Assert.Contains(
            NarrativeChecks.Check("وحيث إن المادة 323 من القانون المدني تنص على أن المدين يعفى من الدين بمرور سنة كاملة دون مطالبة.", string.Empty, inputs, legalTexts: texts),
            i => i.IsWrongArticleContent);
    }

    [Fact]
    public async Task OneModelCallWritesEveryParagraph()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = Notes;
        inputs["notes.requests"] = Requests;
        inputs["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.";
        var provider = new RecordingProvider(_ =>
            "[1] حيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، عملا بالمواد 450 و106 و107 من القانون المدني، مما يجعل طلب المدعي إلزامه بدفع 1500000 دج مؤسسا قانونا.\n"
            + "[2] وحيث إن الدين ثابت باعتراف بدين، طبقا للمادة 323 من القانون المدني.\n"
            + "[3] وحيث إن المدعى عليه أعذر ولم يف، فإن طلب التعويض بمبلغ 150000 دج مؤسس عملا بالمادتين 179 و186 من القانون المدني.");

        var draft = await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.Empty(draft.Problems);
        Assert.Equal(3, draft.Text.Split('\n').Length);
        Assert.DoesNotContain("«", draft.Text, StringComparison.Ordinal);
        var request = Assert.Single(provider.Requests);
        Assert.Contains("مضمون المادة 323: على الدائن إثبات الالتزام", request.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("454", request.UserPrompt, StringComparison.Ordinal);
    }
}
