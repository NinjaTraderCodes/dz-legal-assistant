using DzLegal.Domain;
using DzLegal.FormEngine;
using DzLegal.ModelProviders.Stub;

namespace DzLegal.Drafting.Tests;

public class DraftingServiceTests
{
    private static readonly DocumentTypeDefinition Requete = Fixtures.Requete;

    [Fact]
    public async Task StubDraftOfCompleteInputsPassesTheFormEngine()
    {
        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, Fixtures.CompleteInputs());

        var report = ComplianceChecker.Check(Requete, draft);

        Assert.True(report.IsAdmissibleInForm, string.Join("\n", report.Missing.Select(m => m.ElementId)));
        Assert.Empty(report.Missing);
    }

    [Fact]
    public async Task GeneratedSectionsAreFilledFromTheInputs()
    {
        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, Fixtures.CompleteInputs());

        Assert.Contains("محكمة سيدي امحمد", draft.FindSection("header")!.Text, StringComparison.Ordinal);
        Assert.Contains("بن علي كريم", draft.FindSection("plaintiff")!.Text, StringComparison.Ordinal);
        Assert.Equal("إلى السيد رئيس القسم المدني بمحكمة سيدي امحمد", draft.FindSection("opening")!.Text.Split('\n')[0]);
        Assert.Equal("1. عقد القرض\n2. إنذار بالدفع", draft.FindSection("documents")!.Text);
        Assert.Equal("حرر بالجزائر في: 15/09/2026", draft.FindSection("dateplace")!.Text);
        Assert.Null(draft.FindSection("publication"));
    }

    [Fact]
    public async Task SectionsFollowDefinitionOrder()
    {
        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, Fixtures.CompleteInputs());
        var expected = Requete.Sections.Where(s => s.Id != "publication").Select(s => s.Id);
        Assert.Equal(expected, draft.Sections.Select(s => s.Id));
    }

    [Fact]
    public async Task NarrativeSectionWithoutNotesIsLeftEmptyAndTheModelIsNotCalled()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.grounds"] = "   ";
        var provider = new RecordingProvider(_ => "نص مختلق");

        var draft = await new DraftingService(provider).DraftAsync(Requete, inputs);

        Assert.Equal(string.Empty, draft.FindSection("grounds")!.Text);
        Assert.DoesNotContain(provider.Requests, r => r.SectionId == "grounds");
        Assert.Equal(ElementStatus.Missing, ComplianceChecker.Check(Requete, draft).Find("art15.5.grounds")!.Status);
    }

    [Fact]
    public async Task PromptsAreGroundedInTheLawyersNotes()
    {
        var provider = new RecordingProvider(r => "حيث إن " + r.SourceText);
        await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        Assert.Equal(["grounds"], provider.Requests.Select(r => r.SectionId)); // requests are the lawyer's lines
        var grounds = provider.Requests[0];
        Assert.Contains("المادة 106 من القانون المدني.", grounds.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("المطالبة بدين", grounds.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("لا تضف أي واقعة", grounds.SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RealPropertyClaimAddsThePublicationSection()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["claim.concernsRegisteredRealProperty"] = "true";
        inputs["claim.publicationReference"] = "إيداع رقم 1234";

        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, inputs);

        Assert.Contains("إيداع رقم 1234", draft.FindSection("publication")!.Text, StringComparison.Ordinal);
        Assert.Equal(ElementStatus.Present, ComplianceChecker.Check(Requete, draft).Find("art17.publication")!.Status);
    }

    [Fact]
    public async Task LegalEntityDefendantIsRenderedWithItsRepresentative()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["defendant.isLegalEntity"] = "true";
        inputs["defendant.entityName"] = "شركة النور";
        inputs["defendant.entityNature"] = "ش.ذ.م.م";
        inputs["defendant.headOffice"] = "باب الزوار";
        inputs["defendant.representativeCapacity"] = "المسير";

        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, inputs);

        Assert.StartsWith("ضد: شركة النور، ش.ذ.م.م، الكائن مقرها الاجتماعي بـ: باب الزوار، ممثلة من طرف المسير", draft.FindSection("defendant")!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshReRendersGeneratedSectionsAndKeepsEditedNarrative()
    {
        var service = new DraftingService(new StubModelProvider());
        var inputs = Fixtures.CompleteInputs();
        var draft = await service.DraftAsync(Requete, inputs);
        draft.FindSection("grounds")!.Text = "نص عدله المحامي";

        inputs["court.name"] = "محكمة بئر مراد رايس";
        inputs["claim.concernsRegisteredRealProperty"] = "true";
        var refreshed = service.Refresh(Requete, draft, inputs);

        Assert.Contains("محكمة بئر مراد رايس", refreshed.FindSection("header")!.Text, StringComparison.Ordinal);
        Assert.Equal("نص عدله المحامي", refreshed.FindSection("grounds")!.Text);
        Assert.NotNull(refreshed.FindSection("publication"));
        Assert.Equal("محكمة بئر مراد رايس", refreshed.Inputs["court.name"]);
    }

    [Fact]
    public async Task GenderChoicesProduceAgreedWording()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["plaintiff.gender"] = "female";
        inputs["defendant.gender"] = "male";
        inputs["lawyer.gender"] = "female";

        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, inputs);

        Assert.Equal(
            "لفائدة: السيدة بن علي كريم، القاطنة بـ: 12 شارع ديدوش مراد، الجزائر.\nالقائم في حقها الأستاذة نادية سعدي، محامية لدى مجلس قضاء الجزائر.\n(مدعية)",
            draft.FindSection("plaintiff")!.Text);
        Assert.StartsWith("ضد: السيد حمداني سمير، القاطن بـ:", draft.FindSection("defendant")!.Text, StringComparison.Ordinal);
        Assert.Contains("تتشرف المدعية بواسطة دفاعها أن تعرض", draft.FindSection("opening")!.Text, StringComparison.Ordinal);
        Assert.Equal("تلتمس المدعية من هيئة المحكمة الموقرة القضاء بما يلي:", draft.FindSection("requestsIntro")!.Text);
        Assert.Equal("عن المدعية\nالأستاذة نادية سعدي\nالتوقيع:", draft.FindSection("signature")!.Text);
    }

    [Fact]
    public async Task UnchosenGenderUsesTheMasculineGenericWithoutATitle()
    {
        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, Fixtures.CompleteInputs());
        Assert.StartsWith("لفائدة: بن علي كريم، القاطن بـ:", draft.FindSection("plaintiff")!.Text, StringComparison.Ordinal);
        Assert.Contains("يتشرف المدعي بواسطة دفاعه أن يعرض", draft.FindSection("opening")!.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("/", draft.FindSection("signature")!.Text, StringComparison.Ordinal);
    }

    /// <summary>Paragraph 1 of the grounds in the second local run (qwen2.5:3b) copied the prompt's example.</summary>
    private const string CopiedFromExample =
        "حيث إن المادة (س) من القانون المدني تلزم البائع بتسليم المبيع، وحيث إن المدعى عليه بصفته بائعا قد أخل بهذا الالتزام، فإن طلب المدعي يكون مؤسسا قانونا.";

    [Fact]
    public async Task ParagraphsCopiedFromTheExampleAreRemoved()
    {
        var provider = new RecordingProvider(r => r.SectionId == "grounds"
            ? CopiedFromExample + "\nحيث إن المادة 106 من القانون المدني تجعل العقد شريعة المتعاقدين."
            : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("حيث إن المادة 106 من القانون المدني تجعل العقد شريعة المتعاقدين.", grounds.Text);
        Assert.Equal([DraftingNoteCodes.RemovedCopiedExample], grounds.DraftingNotes);
        Assert.Single(provider.Requests, r => r.SectionId == "grounds");
    }

    [Fact]
    public async Task AnAnswerThatIsOnlyTheExampleIsRetriedThenReplacedByTheNotes()
    {
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? CopiedFromExample : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("المادة 106 من القانون المدني.", grounds.Text);
        Assert.Equal(
            [DraftingNoteCodes.RemovedCopiedExample, DraftingNoteCodes.Retried, DraftingNoteCodes.FellBackToNotes],
            grounds.DraftingNotes);
    }

    [Fact]
    public void TheModelSeesTheExampleWithoutMarkersAndTheExampleIsKeptAside()
    {
        var service = new DraftingService(new StubModelProvider());
        var request = service.BuildRequest(Requete, Requete.FindSection("grounds")!, Fixtures.CompleteInputs());

        Assert.DoesNotContain("[[", request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("المادة (س) من القانون (ص)", request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("المادة (س) من القانون (ص)", request.ExampleText, StringComparison.Ordinal);
        Assert.DoesNotContain("البائع", request.ExampleText, StringComparison.Ordinal);
        Assert.DoesNotContain("ملاحظات المحامي حول الأسانيد", request.ExampleText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PromptsStateThePartiesRolesAndTheRequestsContext()
    {
        var provider = new RecordingProvider(r => r.SourceText);
        await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = provider.Requests.Single(r => r.SectionId == "grounds").UserPrompt;
        Assert.Contains("- المدعي: بن علي كريم", grounds, StringComparison.Ordinal);
        Assert.Contains("- المدعى عليه: حمداني سمير", grounds, StringComparison.Ordinal);
        Assert.DoesNotContain("الدائن بالطلبات", grounds, StringComparison.Ordinal);
        Assert.Contains("إلزام المدعى عليه بدفع 500000 دج.", grounds, StringComparison.Ordinal);
        Assert.Contains("قرض 500000 دج بتاريخ 10/01/2025 لم يسدد.", grounds, StringComparison.Ordinal);
        Assert.Contains("لا تعكس أدوار الطرفين", provider.Requests[0].SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CorruptedAnswerIsRetriedOnce()
    {
        var answers = new Queue<string>(["حيث إن المادة 106 لم受理 الدين", "حيث إن المادة 106 من القانون المدني تجعل العقد شريعة المتعاقدين"]);
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? answers.Dequeue() : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("حيث إن المادة 106 من القانون المدني تجعل العقد شريعة المتعاقدين", grounds.Text);
        Assert.Equal([DraftingNoteCodes.Retried], grounds.DraftingNotes);
        Assert.Equal(2, provider.Requests.Count(r => r.SectionId == "grounds"));
    }

    [Fact]
    public async Task AParagraphThatKeepsWritingLawFromMemoryIsRemovedAfterTheRetry()
    {
        // Notes cite 106 by number only, so whatever the model says 106 provides must come from somewhere: it does not.
        var answer = "حيث إن المادة 106 من القانون المدني تنص على أن الحقوق الخاصة لا تدعى إلا باللجوء إلى القضاء المختص.\n"
            + "وحيث إن المدعى عليه لم يرد المبلغ، فإن طلب المدعي يكون مؤسسا طبقا للمادة 106 من القانون المدني.";
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? answer : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("وحيث إن المدعى عليه لم يرد المبلغ، فإن طلب المدعي يكون مؤسسا طبقا للمادة 106 من القانون المدني.", grounds.Text);
        Assert.Equal([DraftingNoteCodes.Retried, DraftingNoteCodes.RemovedWrongLaw], grounds.DraftingNotes);
    }

    [Fact]
    public async Task WhenEveryParagraphIsWrongLawTheDraftFallsBackAndKeepsWhatWasRemoved()
    {
        // Week 1 case a8: the only paragraph invented the content of art. 106; removing it left nothing.
        const string Wrong = "حيث إن المادة 106 من القانون المدني تنص على أن الحقوق الخاصة لا تدعى إلا باللجوء إلى القضاء المختص.";
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? Wrong : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("المادة 106 من القانون المدني.", grounds.Text);
        Assert.Contains(DraftingNoteCodes.FellBackToNotes, grounds.DraftingNotes);
        Assert.Equal(Wrong, Assert.Single(grounds.RemovedText));
    }

    [Fact]
    public async Task WithALoadedTextTheGroundsAreArguedAndClassicOnRequest()
    {
        LegalText[] texts = [new("106", "القانون المدني", "العقد شريعة المتعاقدين، فلا يجوز نقضه، ولا تعديله الا باتفاق الطرفين.") { Role = "basis", Gist = "العقد شريعة المتعاقدين" }];
        var provider = new RecordingProvider(r => r.SectionId == ArguedGrounds.RequestId ? "[1] حيث إن المادة 107 تؤكد ذلك، فإن الطلب مؤسس." : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs(), new DraftOptions { LegalTexts = texts });

        // The answer cites an article outside the plan twice: the paragraph is left for the lawyer, with its article.
        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("حيث إن (...)، عملا بالمادة 106 من القانون المدني.", grounds.Text);
        Assert.Equal([DraftingNoteCodes.Retried, DraftingNoteCodes.ArgumentLeftForLawyer], grounds.DraftingNotes);
        Assert.DoesNotContain(provider.Requests, r => r.SectionId == "grounds");

        var classic = await new DraftingService(provider) { ClassicGrounds = true }.DraftAsync(Requete, Fixtures.CompleteInputs(), new DraftOptions { LegalTexts = texts });
        Assert.Contains(provider.Requests, r => r.SectionId == "grounds");
        Assert.Equal("المادة 106 من القانون المدني.", classic.FindSection("grounds")!.Text);
    }

    [Fact]
    public async Task PersistentlyCorruptedAnswerFallsBackToTheLawyersNotes()
    {
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? "受理 受理" : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("المادة 106 من القانون المدني.", grounds.Text);
        Assert.Equal([DraftingNoteCodes.Retried, DraftingNoteCodes.FellBackToNotes], grounds.DraftingNotes);
    }

    [Fact]
    public async Task GrammarCorrectionsApplyToModelTextButNeverToTheLawyersWords()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.facts"] = "إن المادتان المذكورتان في العقد واضحتان.";
        var provider = new RecordingProvider(r => r.SectionId == "grounds"
            ? "حيث إن المادتان 106 و107 من القانون المدني تجعلان العقد شريعة المتعاقدين."
            : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, inputs);

        Assert.Equal("حيث إن المادتين 106 و107 من القانون المدني تجعلان العقد شريعة المتعاقدين.", draft.FindSection("grounds")!.Text);
        Assert.Contains("إن المادتان المذكورتان", draft.FindSection("facts")!.Text, StringComparison.Ordinal);
    }

    private const string InventedQualification =
        "حيث إن المادتين 106 و107 من القانون المدني تنفيذاً لعقد المقاولة تجعلان العقد شريعة المتعاقدين.";

    private const string FaithfulGrounds = "حيث إن المادة 106 من القانون المدني تجعل العقد شريعة المتعاقدين.";

    [Fact]
    public async Task InventedContractTypeEarnsOneRetry()
    {
        var answers = new Queue<string>([InventedQualification, FaithfulGrounds]);
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? answers.Dequeue() : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal(FaithfulGrounds, grounds.Text);
        Assert.Equal([DraftingNoteCodes.Retried], grounds.DraftingNotes);
    }

    [Fact]
    public async Task PersistentInventedTermIsKeptForReviewNotReplacedByNotes()
    {
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? InventedQualification : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal(InventedQualification, grounds.Text);
        Assert.Equal([DraftingNoteCodes.Retried], grounds.DraftingNotes);
        var issue = Assert.Single(NarrativeChecks.Check(grounds.Text, "المادة 106 من القانون المدني.", draft.Inputs, Requete.GroundedTerms),
            i => i.Kind == NarrativeIssueKind.UngroundedTerm);
        Assert.Equal("عقد المقاولة", issue.Detail);
    }

    [Fact]
    public async Task GratitudeIsCorrectedToRefusalLikeInBench4()
    {
        var provider = new RecordingProvider(r => r.SectionId == "grounds"
            ? "وحيث إن المدعى عليه قد تسبب عن طريق امتنانه بالسداد في ضرر للمدعي."
            : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        Assert.Equal("وحيث إن المدعى عليه قد تسبب عن طريق امتناعه عن السداد في ضرر للمدعي.", draft.FindSection("grounds")!.Text);
    }

    [Fact]
    public void GroundsPromptAsksForOneParagraphPerGround()
    {
        var request = new DraftingService(new StubModelProvider()).BuildRequest(Requete, Requete.FindSection("grounds")!, Fixtures.CompleteInputs());
        Assert.Contains("اكتب كل سند في فقرة مستقلة تبدأ في سطر جديد", request.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FactsAreTheLawyersSentencesWithoutAnyModelCall()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.facts"] = "بتاريخ 15 جانفي 2025، اقترض المدعى عليه من المدعي مبلغ 1,500,000 دج.\nالمدعى عليه امتنع عن السداد.\nاتفق الطرفان على أجل.";
        var provider = new RecordingProvider(r => r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, inputs);

        Assert.Equal(
            "حيث إنه بتاريخ 15 جانفي 2025، اقترض المدعى عليه من المدعي مبلغ 1,500,000 دج.\nوحيث إن المدعى عليه امتنع عن السداد.\nوحيث إنه اتفق الطرفان على أجل.",
            draft.FindSection("facts")!.Text);
        Assert.DoesNotContain(provider.Requests, r => r.SectionId == "facts");
    }

    [Fact]
    public async Task TheRequestsAreTheLawyersLinesVerbatimUnderTheFixedFormula()
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.requests"] = "في الشكل: قبول الدعوى شكلا\n- إلزام المدعى عليه بتسليم الآلات أو إرجاع الثمن المدفوع وقدره 1.500.000 دج\n2) تحميل المدعى عليه المصاريف القضائية.";
        var provider = new RecordingProvider(_ => "نص من النموذج");

        var draft = await new DraftingService(provider).DraftAsync(Requete, inputs);

        Assert.Equal(
            "في الشكل: قبول الدعوى شكلا.\nفي الموضوع:\n1. إلزام المدعى عليه بتسليم الآلات أو إرجاع الثمن المدفوع وقدره 1.500.000 دج.\n2. تحميل المدعى عليه المصاريف القضائية.",
            draft.FindSection("requests")!.Text);
        Assert.DoesNotContain(provider.Requests, r => r.SectionId == "requests");
    }

    [Fact]
    public async Task EnglishReasoningIsRejectedLikeQwen3OnTheBench()
    {
        const string qwen3 = "Okay, let's tackle this legal drafting request. The user wants me to rephrase the lawyer's notes into formal Arabic.";
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? qwen3 : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var grounds = draft.FindSection("grounds")!;
        Assert.Equal("المادة 106 من القانون المدني.", grounds.Text);
        Assert.Equal([DraftingNoteCodes.Retried, DraftingNoteCodes.FellBackToNotes], grounds.DraftingNotes);
    }

    [Theory]
    [InlineData("<think>reasoning cut off by the length cap", "")]
    [InlineData("Okay, here is the text:\nحيث إن المادة 106 تنص", "حيث إن المادة 106 تنص")]
    public void ReasoningIsStripped(string raw, string expected) =>
        Assert.Equal(expected, DraftingService.CleanModelOutput(raw, Requete.FindSection("grounds")!));

    [Theory]
    [InlineData("<think>تفكير</think>\nحيث إن", "حيث إن")]
    [InlineData("```\nحيث إن\n```", "حيث إن")]
    [InlineData("المناقشة القانونية:\nحيث إن **العارض**", "حيث إن العارض")]
    [InlineData("## المناقشة القانونية\n\nحيث إن", "حيث إن")]
    public void ModelOutputIsCleaned(string raw, string expected)
    {
        Assert.Equal(expected, DraftingService.CleanModelOutput(raw, Requete.FindSection("grounds")!));
    }

    [Fact]
    public async Task ComposedDocumentHasTitlesForNarrativeSectionsOnly()
    {
        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, Fixtures.CompleteInputs());
        var text = DocumentText.Compose(Requete, draft);

        Assert.StartsWith("الجمهورية الجزائرية الديمقراطية الشعبية", text, StringComparison.Ordinal);
        Assert.Contains("الوقائع:\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("الجهة القضائية:", text, StringComparison.Ordinal);
        Assert.EndsWith("التوقيع:", text, StringComparison.Ordinal);
    }
}
