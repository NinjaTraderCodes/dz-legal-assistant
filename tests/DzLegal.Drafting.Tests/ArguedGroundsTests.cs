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

        // Gemma 4 26B, 14 Oct: only the tail of the French request («… DA au principal») was copied.
        var tail = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، فإن طلب المدعي إلزام المدعى عليه بدفع مبلغ 1500000 DA au principal يكون مؤسسا قانونا.",
            [2] = "وحيث إن الدين ثابت.",
            [3] = "وحيث إن المدعى عليه أعذر ولم يرد المبلغ.",
        }, inputs, Ticked);
        Assert.DoesNotContain("principal", tail.Text, StringComparison.Ordinal);
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
        Assert.Equal("حيث إن المواد 106 و107 و450 من القانون المدني تقضي بأن العقد شريعة المتعاقدين، وبأنه يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية، وبأنه يلتزم المقترض برد نظير ما اقترضه عند نهاية القرض، وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، فإن طلب المدعي إلزام المدعى عليه بأن يدفع للمدعي 1500000 دج يكون مؤسسا قانونا.", lines[0]);
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

        Assert.Equal(4, calls); // the whole plan, then each paragraph on its own
        Assert.True(draft.Retried);
        Assert.Equal(3, draft.Problems.Count);
        Assert.All(draft.Text.Split('\n'), line => Assert.EndsWith("(...).", line, StringComparison.Ordinal));
    }

    [Fact]
    public void TheAnswerIsCutWhereALineBeyondThePlanStarts()
    {
        // LOCAL-MODEL-REPORT: one answer wrote 548 tokens for a single planned line.
        const string answer = "1- وحيث إن الدين ثابت.\n2- وحيث إن المدعى عليه لم يرد.\n3- وحيث إن";
        var end = ArguedGrounds.PastThePlan(answer, 1);
        Assert.Equal("1- وحيث إن الدين ثابت.\n", answer[..end!.Value]);

        Assert.Null(ArguedGrounds.PastThePlan("1- وحيث إن الدين ثابت.\n3- وحيث إن", 2)); // line 2 is missing: nothing is cut
        Assert.Null(ArguedGrounds.PastThePlan("1- وحيث إن الدين ثابت.\n2- وحيث إن", 2));
        Assert.Null(ArguedGrounds.PastThePlan("1- وحيث إن المبلغ 2- مليون", 1)); // a number inside a line is not a line
        const string arabicIndic = "١- وحيث إن الدين ثابت.\n٢- وحيث إن";
        Assert.Equal(arabicIndic.IndexOf('٢', StringComparison.Ordinal), ArguedGrounds.PastThePlan(arabicIndic, 1));
    }

    [Fact]
    public void AnArticleOfATextThisAppDoesNotCoverIsNotCitedByTheCode()
    {
        // Hostile case a1: the notes named «المادة 1 من الدستور», and the code wrote it into the grounds.
        var groups = ArguedGrounds.Plan(Notes + "\nالمادة 1 من الدستور تضمن الحق في التقاضي.\nالمادة 1000 من القانون المدني.", Ticked, Requests);
        Assert.DoesNotContain(groups, g => g.Citation.Contains("الدستور", StringComparison.Ordinal));
        Assert.Contains(groups, g => g.Citation.Contains("1000", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ACompanyIsNamedInTheFeminine()
    {
        // Hostile case 04: a company defendant was «المدعى عليه» in the closing the model copies.
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = Notes;
        inputs["notes.requests"] = Requests;
        inputs["defendant.isLegalEntity"] = "true";
        inputs["defendant.gender"] = "male";
        var provider = new RecordingProvider(_ => "1- وحيث إن الدين ثابت.");

        await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.Contains("المدعي والمدعى عليها،", provider.Requests[0].UserPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("female", "false", "فإن طلب المدعية إلزام المدعى عليه بأن يدفع للمدعي 1500000 دج يكون مؤسسا قانونا.")]
    [InlineData("male", "true", "فإن طلب المدعية إلزام المدعى عليه بأن يدفع للمدعي 1500000 دج يكون مؤسسا قانونا.")]
    [InlineData("male", "false", "فإن طلب المدعي إلزام المدعى عليه بأن يدفع للمدعي 1500000 دج يكون مؤسسا قانونا.")]
    public async Task TheCodeClosesTheParagraphWithTheLawyersRequestInThePlaintiffsGender(string gender, string entity, string closing)
    {
        // Case 03 (13 Oct): a woman's petition closed with «فإن طلب المدعي»; the closing is now the code's.
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = Notes;
        inputs["notes.requests"] = Requests;
        inputs["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج ولم يرده.";
        inputs["plaintiff.gender"] = gender;
        inputs["plaintiff.isLegalEntity"] = entity;
        var provider = new RecordingProvider(_ => "1- وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده.");

        var draft = await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.EndsWith("ولم يرده، " + closing, draft.Text.Split('\n')[0], StringComparison.Ordinal);
        Assert.DoesNotContain("مؤسسا قانونا", provider.Requests[0].UserPrompt.Split("التعليمات:")[1].Replace("ولا عبارة يكون مؤسسا قانونا", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("إلزام الشركة المدعى عليها بتسليم 20 آلة خياطة تحت غرامة تهديدية قدرها 5,000 دج عن كل يوم تأخير، أو إرجاع الثمن 800,000 دج.", true)] // case 04
    [InlineData("إبطال عقد البيع المؤرخ في 12/02/2026.", true)] // case 05
    [InlineData("الحكم بتطليق المدعية من المدعى عليه.", true)]
    [InlineData("1- إلزام المدعى عليه بأن يدفع للمدعي أجور ثلاثة أشهر.", true)]
    [InlineData("وإلزامه بأن يدفع 150000 دج تعويضا.", true)]
    [InlineData("فسخ عقد الإيجار وطرد المدعى عليه من المحل.", true)]
    [InlineData("يخلص 240000 دج تاع الكرا لي راه فاتو.", false)] // case 03, dialect
    [InlineData("Condamner le défendeur à payer 1.500.000 DA.", false)] // case 02, French
    [InlineData("نلتمس من المحكمة الموقرة إلزام المدعى عليه بأن يدفع للمدعي مبلغ 1,500,000 دج.", true)] // the frame is dropped
    [InlineData("قبول الدعوى شكلا.", false)] // does not read after «طلب المدعي»
    [InlineData("خلاص 150 مليون سنتيم لي عندو.", false)] // hostile a4
    public void OnlyAFormalRequestLineIsRestatedInTheClosing(string request, bool formal) =>
        Assert.Equal(formal, ArguedGrounds.IsFormalRequest(request));

    [Fact]
    public void ARequestInDialectGetsTheGeneralClosingOnceAndTheModelsOwnClosingIsReplaced()
    {
        // Hostile a4 and case 03 (16 Oct): the model copied the dialect request into the closing, six times in 89.
        const string dialect = "يخلص 240000 دج تاع الكرا لي راه فاتو.\nيخلص تعويض 50000 دج لي فاتو.";
        var groups = ArguedGrounds.Plan(Notes, Ticked, dialect);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.", ["notes.requests"] = dialect };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده، فإن طلب المدعي يخلص 240000 دج يكون مؤسسا قانونا.",
            [2] = "وحيث إن الدين ثابت بموجب اعتراف بدين، وثبت بذلك.",
            [3] = "وحيث إن المدعى عليه أعذر ولم يرد المبلغ.",
        }, inputs, Ticked);

        Assert.Empty(draft.Problems);
        Assert.DoesNotContain("يخلص", draft.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("وثبت بذلك", draft.Text, StringComparison.Ordinal);
        var lines = draft.Text.Split('\n');
        Assert.EndsWith("ولم يرده، فإن طلبات المدعي تكون مؤسسة قانونا.", lines[0], StringComparison.Ordinal);
        Assert.Equal(1, draft.Text.Split("تكون مؤسسة قانونا").Length - 1);
        Assert.EndsWith("ثابت بموجب اعتراف بدين.", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void TheFrameAskingTheCourtIsNotRepeatedInTheClosing()
    {
        const string requests = "نلتمس من المحكمة الموقرة إلزام المدعى عليه بأن يدفع للمدعي مبلغ 1500000 دج.";
        var groups = ArguedGrounds.Plan(Notes, Ticked, requests);
        var closings = ArguedGrounds.Closings(groups, new Dictionary<string, string> { ["notes.requests"] = requests }, groups.Select(_ => true).ToList());
        Assert.Equal("فإن طلب المدعي إلزام المدعى عليه بأن يدفع للمدعي مبلغ 1500000 دج يكون مؤسسا قانونا", closings[0]);
    }

    [Fact]
    public void TheModelReadsOnlyTheFormalRequestsAndASentenceCopyingADialectRequestIsRejected()
    {
        // Gemma 3 4B, case 03 (16 Oct): «… دون جدوى، يخلص 240000 دج تاع الكرا لي راه فاتو، فإن طلبات المدعية …».
        const string requests = "يخلص 240000 دج تاع الكرا لي راه فاتو.\nتحميل المدعى عليه المصاريف القضائية.";
        var shown = ArguedGrounds.RequestsForModel(requests);
        Assert.DoesNotContain("يخلص", shown, StringComparison.Ordinal);
        Assert.Contains("تحميل المدعى عليه المصاريف القضائية.", shown, StringComparison.Ordinal);

        Assert.True(ArguedGrounds.CopiesInformalRequest("وحيث إن المدعى عليه أعذر دون جدوى، يخلص 240000 دج تاع الكرا لي راه فاتو", requests));
        Assert.False(ArguedGrounds.CopiesInformalRequest("وحيث إن المدعى عليه امتنع عن دفع 240000 دج من بدلات الإيجار", requests));
    }

    [Fact]
    public void ANumberWrittenInWordsIsRejected()
    {
        // Gemma 3 4B, hostile a4 (16 Oct): 1,500,000 became «مليون وخمائة وخمسين ألف», 15/01/2025 «خمس عشرة جانفي ألف وخمائة…».
        var groups = ArguedGrounds.Plan(Notes, Ticked, Requests);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.", ["notes.requests"] = Requests };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه اقترض مبلغا قدره مليون وخمائة وخمسين ألف دينار جزائري ولم يرده.",
            [2] = "وحيث إن الدين ثابت بموجب اعتراف بدين.",
            [3] = "وحيث إن المدعى عليه أعذر لمدة ستة أشهر ولم يرد المبلغ.",
        }, inputs, Ticked);

        Assert.Contains("writes a number in words", Assert.Single(draft.Problems), StringComparison.Ordinal);
        Assert.Contains("ستة أشهر", draft.Text, StringComparison.Ordinal); // a single number word is not a spelled-out amount
    }

    [Fact]
    public void WithOnlyAnUnloadedArticleTheFirstParagraphStillClosesOnTheRequest()
    {
        // Case 06 (16 Oct): Family Code art. 53 is not loaded; the old path wrote «فإن طلب المدعي بتطليق المدعية».
        const string notes = "المادة 53 من قانون الأسرة: يجوز للزوجة أن تطلب التطليق لعدم الإنفاق بعد صدور الحكم بوجوبه.";
        const string requests = "الحكم بتطليق المدعية من المدعى عليه.\nتحميل المدعى عليه المصاريف القضائية.";
        Assert.True(ArguedGrounds.CitesCoveredArticle(notes));
        var groups = ArguedGrounds.Plan(notes, [], requests);
        var group = Assert.Single(groups);
        Assert.Equal("الحكم بتطليق المدعية من المدعى عليه.", group.Request);

        var inputs = new Dictionary<string, string> { ["notes.facts"] = "امتنع المدعى عليه عن الإنفاق على المدعية رغم صدور حكم بالنفقة.", ["notes.requests"] = requests, ["plaintiff.gender"] = "female" };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string> { [1] = "وحيث إن المدعى عليه امتنع عن الإنفاق على المدعية رغم صدور حكم بالنفقة." }, inputs, []);
        Assert.Equal("حيث إن الدعوى تستند إلى المادة 53 من قانون الأسرة، وحيث إن المدعى عليه امتنع عن الإنفاق على المدعية رغم صدور حكم بالنفقة، فإن طلب المدعية الحكم بتطليق المدعية من المدعى عليه يكون مؤسسا قانونا.", draft.Text);
    }

    [Fact]
    public void AnUnloadedArticleKeepsTheLawTheLawyerNamed()
    {
        // Case 07 (16 Oct): «المادة 80 من القانون المتعلق بعلاقات العمل» came out «المادة 80 من القانون المدني».
        var group = Assert.Single(ArguedGrounds.Plan("المادة 80 من القانون المتعلق بعلاقات العمل: للعامل الحق في أجر مقابل العمل المؤدى.", [], "إلزام الشركة المدعى عليها بأن تدفع للمدعي 180,000 دج."));
        Assert.Equal("المادة 80 من القانون رقم 90-11 المتعلق بعلاقات العمل", Assert.Single(group.Unloaded)); // the library's citation name
        Assert.Equal("المادة 12 من القانون رقم 12-34 المتعلق بشيء آخر", Assert.Single(Assert.Single(ArguedGrounds.Plan("المادة 12 من القانون رقم 12-34 المتعلق بشيء آخر: نص.", [], string.Empty)).Unloaded));
        Assert.Equal("المادة 179 من القانون المدني", Assert.Single(Assert.Single(ArguedGrounds.Plan("المادة 179: الإعذار.", [], string.Empty)).Unloaded));
    }

    [Fact]
    public void RequestsInFrenchLeaveOneGeneralClosingOnTheLastParagraph()
    {
        const string french = "Condamner le défendeur à payer au demandeur 1.500.000 DA au principal.";
        var groups = ArguedGrounds.Plan(Notes, Ticked, french);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.", ["notes.requests"] = french };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده.",
            [2] = "وحيث إن الدين ثابت بموجب اعتراف بدين.",
            [3] = "وحيث إن المدعى عليه أعذر ولم يرد المبلغ.",
        }, inputs, Ticked);

        var lines = draft.Text.Split('\n');
        Assert.EndsWith("ولم يرده.", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("ولم يرد المبلغ، فإن طلبات المدعي تكون مؤسسة قانونا.", lines[2], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("وحيث إن المدعى عليه لم يرد المبلغ، وعليه فإن طلب المدعي إلزامه بالدفع يكون مؤسسا قانونا.", "وحيث إن المدعى عليه لم يرد المبلغ")]
    [InlineData("وحيث إن المدعى عليه لم يرد المبلغ مما يجعل طلب المدعي مؤسسا.", "وحيث إن المدعى عليه لم يرد المبلغ")]
    [InlineData("وحيث إن المدعى عليه لم يرد المبلغ، وثبت بذلك.", "وحيث إن المدعى عليه لم يرد المبلغ")]
    [InlineData("وحيث إن المدعى عليه لم يرد المبلغ.", "وحيث إن المدعى عليه لم يرد المبلغ.")]
    public void TheModelsOwnClosingIsCutBeforeTheCodeWritesIt(string application, string kept) =>
        Assert.Equal(kept, ArguedGrounds.WithoutModelClosing(application));

    [Fact]
    public void ALineSplitOverTwoPhysicalLinesIsNotCut()
    {
        // HARNESS-RESEARCH-REPORT risk: the stop must never cut planned line N when it runs on to a second line.
        const string answer = "1- وحيث إن المدعى عليه اقترض المبلغ\nولم يرده رغم إعذاره.\n2- وحيث إن الدين ثابت.";
        Assert.Null(ArguedGrounds.PastThePlan(answer, 2));
        Assert.Null(ArguedGrounds.PastThePlan("1- وحيث إن المدعى عليه اقترض المبلغ\nولم يرده.", 1));
    }

    [Fact]
    public void AReferenceToAnArticleAfterTheFactsIsDropped()
    {
        // Gemma 3 4B, 14 Oct, case 03: a sound application lost to «كما تنص عليه المادة 498 من القانون المدني».
        var groups = ArguedGrounds.Plan(Notes, Ticked, Requests);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج ولم يرده رغم إعذاره.", ["notes.requests"] = Requests };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه ملزم برد القرض، كما تنص عليه المادة 450 من القانون المدني، وقد امتنع عن رد مبلغ 1500000 دج.",
            [2] = "وحيث إن الدين ثابت طبقا للمادة 323 من القانون المدني.",
            [3] = "وحيث إن المدعى عليه أعذر ولم يف، وفقا للمادة 182 من القانون المدني.",
        }, inputs, Ticked);

        var lines = draft.Text.Split('\n');
        Assert.EndsWith("وحيث إن المدعى عليه ملزم برد القرض، وقد امتنع عن رد مبلغ 1500000 دج، فإن طلب المدعي إلزام المدعى عليه بأن يدفع للمدعي 1500000 دج يكون مؤسسا قانونا.", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("وحيث إن الدين ثابت.", lines[1], StringComparison.Ordinal);
        // 182 is not this paragraph's article: the clause naming it goes, the facts before it stay.
        Assert.Contains("وحيث إن المدعى عليه أعذر ولم يف", lines[2], StringComparison.Ordinal);
        Assert.DoesNotContain("182", draft.Text, StringComparison.Ordinal);
        Assert.Empty(draft.Problems);
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

    [Fact]
    public void AClauseThatBringsInAnArticleIsCutAndTheFactsBeforeItAreKept()
    {
        // Gemma 3 4B, local, 18 Oct: both paragraphs were left as «(...)» and the whole grounds asked again.
        var groups = ArguedGrounds.Plan(Notes, Ticked, Requests);
        var inputs = new Dictionary<string, string>
        {
            ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين، وأعذره المدعي بمحضر قضائي بتاريخ 10 جويلية 2025، فامتنع عن السداد دون أي مبرر قانوني.",
            ["notes.requests"] = Requests,
        };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده.",
            [2] = "وحيث إن المدعى عليه يمتنع عن السداد بدون أي مبرر قانوني، وبموجب المادة 450 من القانون المدني الذي يلزم المقترض برد نظير ما اقترضه عند نهاية القرض.",
            [3] = "وحيث إن المدعي قد أرسل محضر إعذار رسمي بتاريخ 10 جويلية 2025، وبموجب المادة 106 من القانون المدني الذي يلزم المقترض برد نظير ما اقترضه عند نهاية القرض.",
        }, inputs, Ticked);

        Assert.Empty(draft.Problems);
        Assert.Empty(draft.Rejected);
        var lines = draft.Text.Split('\n');
        Assert.EndsWith("وحيث إن المدعى عليه يمتنع عن السداد بدون أي مبرر قانوني.", lines[1], StringComparison.Ordinal);
        Assert.Contains("وحيث إن المدعي قد أرسل محضر إعذار رسمي بتاريخ 10 جويلية 2025", lines[2], StringComparison.Ordinal);
        Assert.DoesNotContain("المادة 106 من القانون المدني الذي", draft.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("وحيث إن المدعى عليه لم يرد المبلغ، طبقا لأحكام المادة 450 من القانون المدني", "وحيث إن المدعى عليه لم يرد المبلغ")]
    [InlineData("وحيث إن المدعى عليه لم يرد المبلغ، والمادتان 106 و107 تلزمانه بذلك، رغم إعذاره", "وحيث إن المدعى عليه لم يرد المبلغ، رغم إعذاره")]
    [InlineData("وحيث إن المادة 450 تلزم المقترض بالرد، وقد امتنع", "وحيث إن المادة 450 تلزم المقترض بالرد، وقد امتنع")] // the opening clause stays, and is rejected
    [InlineData("وحيث إن الدين ثابت بموجب اعتراف بدين موثق، وبموجب محضر الإعذار", "وحيث إن الدين ثابت بموجب اعتراف بدين موثق، وبموجب محضر الإعذار")]
    public void OnlyAClauseNamingAnArticleIsCut(string application, string expected) =>
        Assert.Equal(expected, ArguedGrounds.WithoutCitingClauses(application));

    [Fact]
    public async Task EachParagraphTheFirstAnswerLeftOutIsAskedOnItsOwn()
    {
        // Local run, 18 Oct: Gemma 3 4B wrote only the first of 4 numbered lines, twice; asking the whole plan again did not help.
        const string own = "تهرب المدعى عليه من الوفاء رغم المساعي الودية المتكررة.";
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = Notes + "\n" + own;
        inputs["notes.requests"] = Requests;
        inputs["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره، والدعوى مرفوعة أمام القسم المدني.";
        var provider = new RecordingProvider(r =>
            r.UserPrompt.EndsWith("ودون ذكر أي مادة أو قانون.", StringComparison.Ordinal)
                ? r.UserPrompt.Contains("اكتب الآن مكان «…» في الفقرة 2", StringComparison.Ordinal)
                    // copies the opening the code writes, article included: only what follows is kept
                    ? "2- وحيث إن المادة 323 من القانون المدني تقضي بأن على الدائن إثبات الالتزام وعلى المدين إثبات التخلص منه، وحيث إن الدين ثابت باعتراف بدين.\nوحيث إن شيئا آخر."
                    : r.UserPrompt.Contains("اكتب الآن مكان «…» في الفقرة 3", StringComparison.Ordinal)
                        ? "وحيث إن المدعى عليه أعذر ولم يف."
                        : "وحيث إن المدعى عليه تهرب من الوفاء رغم المساعي الودية."
                : "1- وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده.");

        var draft = await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.Equal(4, provider.Requests.Count);
        Assert.Empty(draft.Problems);
        var lines = draft.Text.Split('\n');
        Assert.EndsWith("وحيث إن الدين ثابت باعتراف بدين.", lines[1], StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(lines[1], "323")); // the code's citation only
        Assert.Contains("وحيث إن المدعى عليه أعذر ولم يف", lines[2], StringComparison.Ordinal);
        Assert.Equal("وحيث إن المدعى عليه تهرب من الوفاء رغم المساعي الودية.", lines[3]);
        Assert.DoesNotContain("شيئا آخر", draft.Text, StringComparison.Ordinal); // one line only
        // The single question comes after the very same prompt, so the server reuses what it already read.
        Assert.All(provider.Requests.Skip(1), r => Assert.StartsWith(provider.Requests[0].UserPrompt, r.UserPrompt, StringComparison.Ordinal));
        Assert.Contains(own.TrimEnd('.'), provider.Requests[3].UserPrompt[provider.Requests[0].UserPrompt.Length..], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ALineOfTheLawyersOwnStillUnwrittenIsNamedForTheLawyer()
    {
        const string own = "تهرب المدعى عليه من الوفاء رغم المساعي الودية المتكررة.";
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = Notes + "\n" + own;
        inputs["notes.requests"] = Requests;
        inputs["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.";
        var provider = new RecordingProvider(r => r.UserPrompt.EndsWith("ودون ذكر أي مادة أو قانون.", StringComparison.Ordinal)
            ? string.Empty
            : "1- وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده.\n2- وحيث إن الدين ثابت باعتراف بدين.\n3- وحيث إن المدعى عليه أعذر ولم يف.");

        var draft = await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        Assert.Equal(2, provider.Requests.Count); // the plan, then paragraph 4 alone
        Assert.True(draft.Retried);
        Assert.Equal("وحيث إن (...).", draft.Text.Split('\n')[3]);
        Assert.Equal(own, Assert.Single(draft.Unwritten));
        Assert.Empty(draft.Rejected);
    }

    [Theory]
    [InlineData("3- وحيث إن المدعى عليه لم يف.", "وحيث إن المدعى عليه لم يف.")]
    [InlineData("\n\n(2) وحيث إن الدين ثابت.\n3- وحيث إن …", "وحيث إن الدين ثابت.")]
    [InlineData("", null)]
    [InlineData("4-", null)]
    public void AOneParagraphAnswerIsItsFirstLineWithoutANumber(string answer, string? expected) =>
        Assert.Equal(expected, ArguedGrounds.OneParagraph(answer, new ArguedGroup("حجة أوردها المحامي", [], [], "ملاحظة", null)));

    [Fact]
    public void ThePartiesAreNamedByTheirStanding()
    {
        var inputs = new Dictionary<string, string>
        {
            ["plaintiff.lastName"] = "بن علي", ["plaintiff.firstName"] = "كريم",
            ["defendant.lastName"] = "حمداني", ["defendant.firstName"] = "سمير", ["defendant.gender"] = "female",
        };

        // Gemma 3 4B, harness case 12 (18 Oct).
        Assert.Equal(
            "وحيث إن المدعى عليها اقترضت من المدعي مبلغ 1,500,000 دج، وإن المدعي أعذر المدعى عليها",
            ArguedGrounds.WithRolesForNames("وحيث إن المدعى عليه حمداني سمير اقترضت من المدعي (بن علي كريم) مبلغ 1,500,000 دج، وإن كريم بن علي أعذر حمداني سمير", inputs));
        Assert.Equal("وحيث إن المدعي ملتزم بتنفيذ العقد", ArguedGrounds.WithRolesForNames("وحيث إن بن علي كريم ملتزم بتنفيذ العقد", inputs));
        // A surname alone is left: it may be a word of the facts.
        Assert.Equal("وحيث إن حمداني امتنع", ArguedGrounds.WithRolesForNames("وحيث إن حمداني امتنع", inputs));
    }

    [Theory]
    [InlineData("- المادة 450 من القانون المدني", false)]
    [InlineData("- المادة 324 مكرر 1 من القانون المدني", false)]
    [InlineData("- المادة 73 من القانون رقم 90-11 المتعلق بعلاقات العمل", false)]
    [InlineData("- المادة 124 من القانون المدني الجزائري (الالتزام بالتعويض عن الضرر الناتج عن عدم الوفاء أو التأخير).", true)]
    [InlineData("- المادتان 106 و107 من القانون المدني: العقد شريعة المتعاقدين", true)]
    [InlineData("المادة 186 من القانون المدني: إذا كان محل الالتزام…", true)]
    public void OnlyABareCitationIsTakenForATickedArticle(string line, bool own) => Assert.Equal(own, ArguedGrounds.IsOwnLine(line));

    [Fact]
    public async Task ALineOnAdmissibilityIsWrittenByTheCodeFirstAndNotAskedOfTheModel()
    {
        // Gemma 3 4B, harness case 12 (18 Oct): «وحيث إن أحكام قانون الإجراءات المدنية والإدارية المتعلقة باختصاص القسم
        // المدني ومقبولية الدعوى شكلا، امتنع المدعى عليه عن السداد…».
        var inputs = Fixtures.CompleteInputs();
        inputs["court.section"] = "القسم المدني";
        inputs["notes.grounds"] = Notes + "\n- أحكام قانون الإجراءات المدنية والإدارية المتعلقة باختصاص القسم المدني ومقبولية الدعوى شكلا.";
        inputs["notes.requests"] = Requests;
        inputs["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.";
        var provider = new RecordingProvider(_ =>
            "1- وحيث إن المدعى عليه اقترض مبلغ 1500000 دج ولم يرده.\n2- وحيث إن الدين ثابت باعتراف بدين.\n3- وحيث إن المدعى عليه أعذر ولم يف.");

        var draft = await new ArguedGrounds(provider).DraftAsync(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, inputs, Ticked, "prompts/grounds-argued.ar.txt");

        var lines = draft.Text.Split('\n');
        Assert.Equal("حيث إن الدعوى رفعت أمام القسم المدني وفق الأشكال المقررة في قانون الإجراءات المدنية والإدارية، فهي مقبولة شكلا.", lines[0]);
        Assert.StartsWith("وحيث إن المواد 106 و107 و450", lines[1], StringComparison.Ordinal);
        Assert.Equal(4, lines.Length);
        Assert.Single(provider.Requests);
        Assert.DoesNotContain("مقبولية", provider.Requests[0].UserPrompt, StringComparison.Ordinal);
        Assert.Empty(draft.Problems);
    }

    [Theory]
    [InlineData("- أحكام قانون الإجراءات المدنية والإدارية المتعلقة باختصاص القسم المدني ومقبولية الدعوى شكلا.", true)]
    [InlineData("الدعوى مقبولة شكلا", true)]
    [InlineData("- المادة 124 من القانون المدني الجزائري (الالتزام بالتعويض عن الضرر الناتج عن عدم الوفاء أو التأخير).", false)]
    [InlineData("المادة 15 من قانون الإجراءات المدنية والإدارية: الشروط الشكلية للعريضة", false)] // cites an article: argued as usual
    [InlineData("المدعى عليه امتنع عن السداد رغم إعذاره", false)]
    public void OnlyALineOnJurisdictionOrAdmissibilityWithoutAnArticleIsTakenOut(string line, bool taken) =>
        Assert.Equal(taken, ArguedGrounds.WithoutAdmissibilityLines(line).Admissibility);

    [Theory]
    [InlineData("وحيث إن المدعى عليه قد ارتكب خطأً بإعراضه عن سداد الدين، مما أدى إلى ضرر للمدعي بن علي كريم", "وحيث إن المدعى عليه قد ارتكب خطأً بإعراضه عن سداد الدين، مما أدى إلى ضرر للمدعي")] // was «للالمدعي» (case 12, run 1)
    [InlineData("مما أدى إلى ضرر لبن علي كريم", "مما أدى إلى ضرر للمدعي")]
    [InlineData("وحيث إن حمداني سمير اقترض من بن علي كريم", "وحيث إن المدعى عليه اقترض من المدعي")]
    [InlineData("وحيث إن المدعى عليه (حمداني سمير) أعذر بطلب بن علي كريم", "وحيث إن المدعى عليه أعذر بطلب المدعي")]
    public void ANameAfterAJoinedLetterBecomesTheStandingJoinedCorrectly(string application, string expected)
    {
        var inputs = new Dictionary<string, string>
        {
            ["plaintiff.lastName"] = "بن علي", ["plaintiff.firstName"] = "كريم",
            ["defendant.lastName"] = "حمداني", ["defendant.firstName"] = "سمير",
        };
        Assert.Equal(expected, ArguedGrounds.WithRolesForNames(application, inputs));
    }

    private const string EvictionRequests = "فسخ عقد الإيجار المؤرخ في 01/03/2024 وطرد المدعى عليه من الشقة هو ومن يقوم مقامه.\nإلزام المدعى عليه بأن يدفع للمدعي 240,000 دج.";

    [Theory]
    // Gemma 3 4B, case 08 (landlord's eviction claim), runs 2 and 3 of 18 Oct.
    [InlineData("وحيث إن المدعي قد التزم بتسجيل عقد إيجار بتاريخ 01 مارس 2024، وأقر المدعي بدفع أجرة شهرية قدرها 40,000 دج", true)]
    [InlineData("وحيث إن المدعي قد التزم بتسجيل عقد إيجار، وأقر المدعي بأنه ملزم بدفع أجرة شهرية قدرها 40,000 دج", true)]
    [InlineData("وحيث إن المدعي اقترض من المدعى عليه مبلغ 1,500,000 دج", true)]
    [InlineData("وحيث إن المدعي امتنع عن السداد رغم إعذاره", true)]
    [InlineData("وحيث إن المدعى عليه أقرض المدعي مبلغ 1,500,000 دج", true)]
    // The same runs, roles right: kept.
    [InlineData("وحيث إن المدعي قد التزم بتسليم الشقة للمدعى عليه بموجب عقد الإيجار المؤرخ في 01 مارس 2024، وإخلال المدعى عليه بعدم دفع الأجرة الشهرية", false)]
    [InlineData("وحيث إن المدعى عليه قد اقترض من المدعي مبلغ 1,500,000 دج، وامتنع عن السداد رغم حلول الأجل", false)]
    [InlineData("وحيث إن المدعى عليه أخل بالتزامه بعد إعذار المدعي رسميا بتاريخ 10 جويلية 2025", false)]
    [InlineData("وحيث إن المدعية دفعت الثمن كاملا ولم يسلمها المدعى عليه البضاعة", false)]
    [InlineData("وحيث إن المدعى عليه لم يدفع للمدعي أجور أشهر جوان وجويلية", false)]
    public void TheDebtorsPartGivenToThePlaintiffIsRefused(string application, bool swapped) =>
        Assert.Equal(swapped, ArguedGrounds.SwapsRoles(application, EvictionRequests) is not null);

    [Fact]
    public void ASwappedApplicationIsLeftForTheLawyer()
    {
        var groups = ArguedGrounds.Plan(Notes, Ticked, Requests);
        var inputs = new Dictionary<string, string> { ["notes.facts"] = "اقترض المدعى عليه مبلغ 1500000 دج بموجب اعتراف بدين ولم يرده رغم إعذاره.", ["notes.requests"] = Requests };
        var draft = ArguedGrounds.Assemble(null, groups, new Dictionary<int, string>
        {
            [1] = "وحيث إن المدعي اقترض من المدعى عليه مبلغ 1500000 دج ولم يرده.",
            [2] = "وحيث إن الدين ثابت باعتراف بدين.",
            [3] = "وحيث إن المدعى عليه أعذر ولم يف.",
        }, inputs, Ticked);

        Assert.Contains("swaps the parties' roles", Assert.Single(draft.Problems), StringComparison.Ordinal);
        Assert.Contains("(...)", draft.Text.Split('\n')[0], StringComparison.Ordinal);
    }

    [Theory]
    // Gemma 3 4B, case 07 (the worker's wages), 18 Oct: «فإنه» after «المدعي» made the worker the one who pays.
    [InlineData("وحيث إن المدعي يعمل لدى الشركة المدعى عليها بأجر شهري قدره 60,000 دج، فإنه التزم بدفع أجر مقابل العمل المؤدى",
        "وحيث إن المدعي يعمل لدى الشركة المدعى عليها بأجر شهري قدره 60,000 دج")]
    [InlineData("وحيث إن المدعي يعمل بأجر شهري قدره 60,000 دج، مما يعني أن الشركة المدعى عليها التزمت بدفع أجر حتى تاريخ 02 جانفي 2025",
        "وحيث إن المدعي يعمل بأجر شهري قدره 60,000 دج")]
    [InlineData("وحيث إن المدعى عليه ارتكب خطأ، مما أدى إلى ضرر للمدعي، وبالتالي يلزم بالتعويض", "وحيث إن المدعى عليه ارتكب خطأ، مما أدى إلى ضرر للمدعي")]
    // A fact after «مما أدى» stays.
    [InlineData("وحيث إن المدعى عليه لم يدفع الأجرة، مما أدى إلى تراكم مبلغ 240,000 دج", "وحيث إن المدعى عليه لم يدفع الأجرة، مما أدى إلى تراكم مبلغ 240,000 دج")]
    public void AConclusionTheModelAddsAfterTheFactsIsCut(string application, string expected) =>
        Assert.Equal(expected, ArguedGrounds.WithoutConclusions(application));

    [Theory]
    [InlineData("التزمت بدفع أجر حتى تاريخ 02 جانفي 2025", "02 جانفي 2025")] // case 07, run 1: day of 2022, year of 2025
    [InlineData("بموجب اعتراف بدين مؤرخ في 15 يناير 2025", null)] // the notes' «15 جانفي 2025», in the eastern month name
    [InlineData("بموجب محضر الإعذار المؤرخ في 10/07/2025", null)]
    [InlineData("بموجب محضر مؤرخ في 11 جويلية 2025", "11 جويلية 2025")]
    [InlineData("لم يدفع أجور أشهر جوان وجويلية وأوت 2025", null)] // months without a day are not dates
    public void AFullDateMustBeOneOfTheNotes(string application, string? invented) =>
        Assert.Equal(invented, ArguedGrounds.InventedDate(application, "يعمل منذ 02 جانفي 2022.\nاقترض بتاريخ 15 جانفي 2025، وأعذر بتاريخ 10 جويلية 2025.\nلم يدفع أجور 2025."));
}
