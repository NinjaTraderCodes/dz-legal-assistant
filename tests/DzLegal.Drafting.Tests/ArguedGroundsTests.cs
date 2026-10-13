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
    public void ARequestInFrenchIsNotGivenToTheModelAndAFrenchApplicationIsRejected()
    {
        // Run of 13 Oct (02, a8): Gemma 4 26B ended the Arabic grounds with «فإن Condamner le défendeur à payer…».
        const string french = "Condamner le défendeur à payer au demandeur 1.500.000 DA au principal.";
        var groups = ArguedGrounds.Plan(Notes, Ticked, french);
        Assert.Null(groups[0].Request);

        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج ولم يرده.", ["notes.requests"] = french };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، فإن " + french,
            [2] = "وحيث إن الدين ثابت.",
            [3] = "وحيث إن المدعى عليه أعذر ولم يرد المبلغ.",
        }, inputs, Ticked);

        Assert.DoesNotContain("Condamner", draft.Text, StringComparison.Ordinal);
        Assert.Contains("writes in French", Assert.Single(draft.Problems), StringComparison.Ordinal);
    }

    [Fact]
    public void ALimitTheLawyerArguesInTheNotesStays()
    {
        var groups = ArguedGrounds.Plan("المادة 454 من القانون المدني: لا فوائد على القرض بين الأفراد، فالمدعي لا يطلبها.", Ticked, Requests);
        Assert.Contains(groups, g => g.Texts.Any(t => t.Number == "454"));
    }

    [Fact]
    public void TheCodeWritesTheRuleAndAnApplicationThatCitesOrMisstatesIsRejected()
    {
        var groups = ArguedGrounds.Plan(Notes, Ticked, Requests);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.", ["notes.requests"] = Requests };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، فإن طلب المدعي إلزامه بدفعه مؤسس.",
            [2] = "وحيث إن الدين ثابت بموجب اعتراف بدين.",
            // Codex's probe: a model stating the opposite of art. 179. It cites an article, so it never reaches the text.
            [3] = "وحيث إن المادة 179 من القانون المدني تعفي الدائن من إعذار المدين في جميع الأحوال، فإن التعويض مستحق.",
        }, inputs, Ticked);

        var lines = draft.Text.Split('\n');
        Assert.Equal("حيث إن المواد 106 و107 و450 من القانون المدني تقضي بأن العقد شريعة المتعاقدين، وبأنه يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية، وبأنه يلتزم المقترض برد نظير ما اقترضه عند نهاية القرض، وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، فإن طلب المدعي إلزامه بدفعه مؤسس.", lines[0]);
        Assert.Equal("وحيث إن المادة 323 من القانون المدني تقضي بأن على الدائن إثبات الالتزام وعلى المدين إثبات التخلص منه، وحيث إن الدين ثابت بموجب اعتراف بدين.", lines[1]);
        Assert.Equal("وحيث إن المادتين 179 و186 من القانون المدني تقضيان بأنه لا يستحق التعويض إلا بعد إعذار المدين، وبتعويض الدائن عن الضرر اللاحق من تأخر المدين في الوفاء بمبلغ من النقود، وحيث إن (...).", lines[2]);
        Assert.DoesNotContain("تعفي", draft.Text, StringComparison.Ordinal);
        Assert.Contains("cites article 179", Assert.Single(draft.Problems), StringComparison.Ordinal);
        Assert.Contains("تعفي الدائن", Assert.Single(draft.Rejected), StringComparison.Ordinal);
    }

    [Fact]
    public void AnAnswerWithoutNumbersIsTakenLineByLineWhenTheCountMatches()
    {
        var parsed = ArguedGrounds.Parse("وحيث إن أ، فإن ب.\nوحيث إن ج.", 2);
        Assert.Equal("وحيث إن ج.", parsed[2]);
        Assert.Empty(ArguedGrounds.Parse("وحيث إن أ.", 2));
    }

    [Theory]
    [InlineData("العقد شريعة المتعاقدين", "بأن العقد شريعة المتعاقدين")]
    [InlineData("لا يستحق التعويض إلا بعد إعذار المدين", "بأنه لا يستحق التعويض إلا بعد إعذار المدين")]
    [InlineData("تعويض الدائن عن الضرر", "بتعويض الدائن عن الضرر")]
    [InlineData("تعريف الإيجار: الانتفاع بشيء لمدة محددة", "بأن الإيجار هو الانتفاع بشيء لمدة محددة")]
    [InlineData("ما ورد في العقد الرسمي (كالاعتراف بدين الموثق) حجة", "بأن ما ورد في العقد الرسمي حجة")]
    public void EachOneLineRuleReadsAfterTheVerb(string gist, string clause) => Assert.Equal(clause, ArguedGroup.RuleClause(gist));

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
            "1- وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، فإن طلب المدعي إلزامه بدفع 1500000 دج مؤسس قانونا.\n"
            + "2- وحيث إن الدين ثابت باعتراف بدين.\n"
            + "3- وحيث إن المدعى عليه أعذر ولم يف، فإن طلب التعويض بمبلغ 150000 دج مؤسس.");

        var draft = await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.Empty(draft.Problems);
        Assert.Equal(3, draft.Text.Split('\n').Length);
        Assert.DoesNotContain("«", draft.Text, StringComparison.Ordinal);
        var request = Assert.Single(provider.Requests);
        Assert.Contains("بداية الفقرة (يكتبها البرنامج): وحيث إن المادة 323 من القانون المدني تقضي بأن على الدائن إثبات الالتزام", request.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("454", request.UserPrompt, StringComparison.Ordinal);

        // No «[1]»: behind OpenRouter, DeepInfra took an answer starting with «[» for a function call and sent no text.
        Assert.Contains("1- وحيث إن …", request.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("[1]", request.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACutOffAnswerIsAskedAgainAndThenLeftForTheLawyerWithoutFailingTheDraft()
    {
        // Gemma 3 4B on French and dialect notes (02, a4, 13 Oct) ran into the length limit: the whole draft failed.
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = Notes;
        inputs["notes.requests"] = Requests;
        var calls = 0;
        var provider = new RecordingProvider(_ => ++calls > 0 ? throw new ModelProviderException("cut off") { IsCutOff = true } : string.Empty);

        var draft = await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.Equal(2, calls);
        Assert.True(draft.Retried);
        Assert.Equal(3, draft.Problems.Count);
        Assert.All(draft.Text.Split('\n'), line => Assert.EndsWith("(...).", line, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("female", "male", "فإن طلب المدعية إلزام المدعى عليه")]
    [InlineData("male", "female", "فإن طلب المدعي إلزام المدعى عليها")]
    public async Task TheClosingTheModelIsShownAgreesWithThePartiesGender(string plaintiff, string defendant, string closing)
    {
        // Case 03 (13 Oct): a woman's petition closed with «فإن طلب المدعي».
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = Notes;
        inputs["notes.requests"] = Requests;
        inputs["plaintiff.gender"] = plaintiff;
        inputs["defendant.gender"] = defendant;
        var provider = new RecordingProvider(_ => "1- وحيث إن الدين ثابت.");

        await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.Contains(closing, provider.Requests[0].UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void AClauseRepeatingTheRuleTheCodeWroteIsDropped()
    {
        // Gemma 3 4B on case 01, paragraph 2 (13 Oct): the rule of art. 186 came back inside the application.
        var damages = ArguedGrounds.Plan(Notes, Ticked, Requests)[2];
        var application = "وحيث إن المدعى عليه امتنع عن السداد رغم إعذاره رسميا بتاريخ 10 جويلية 2025، فإن ذلك يثبت إخلاله بالتزامه، "
            + "وبتعويض الدائن عن الضرر اللاحق من تأخر المدين في الوفاء بمبلغ من النقود، فإنه يستحق تعويضا قدره 150000 دج.";

        Assert.Equal(
            "وحيث إن المدعى عليه امتنع عن السداد رغم إعذاره رسميا بتاريخ 10 جويلية 2025، فإن ذلك يثبت إخلاله بالتزامه، فإنه يستحق تعويضا قدره 150000 دج.",
            ArguedGrounds.WithoutRestatedRule(application, damages));

        const string plain = "وحيث إن المدعى عليه لم يرد المبلغ، فإن طلب المدعي يكون مؤسسا قانونا.";
        Assert.Equal(plain, ArguedGrounds.WithoutRestatedRule(plain, damages));
    }
}
