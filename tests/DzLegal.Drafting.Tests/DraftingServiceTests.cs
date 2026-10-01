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

        Assert.Equal(["facts", "grounds", "requests"], provider.Requests.Select(r => r.SectionId));
        var facts = provider.Requests[0];
        Assert.Contains("قرض 500000 دج بتاريخ 10/01/2025 لم يسدد.", facts.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("المطالبة بدين", facts.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("لا تضف أي واقعة", facts.SystemPrompt, StringComparison.Ordinal);
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
        draft.FindSection("facts")!.Text = "نص عدله المحامي";

        inputs["court.name"] = "محكمة بئر مراد رايس";
        inputs["claim.concernsRegisteredRealProperty"] = "true";
        var refreshed = service.Refresh(Requete, draft, inputs);

        Assert.Contains("محكمة بئر مراد رايس", refreshed.FindSection("header")!.Text, StringComparison.Ordinal);
        Assert.Equal("نص عدله المحامي", refreshed.FindSection("facts")!.Text);
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
    public async Task UnchosenGenderShowsBothFormsNeverAGuess()
    {
        var draft = await new DraftingService(new StubModelProvider()).DraftAsync(Requete, Fixtures.CompleteInputs());
        Assert.StartsWith("لفائدة: السيد/السيدة بن علي كريم، القاطن/القاطنة بـ:", draft.FindSection("plaintiff")!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PromptsStateThePartiesRolesAndTheRequestsContext()
    {
        var provider = new RecordingProvider(r => r.SourceText);
        await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var facts = provider.Requests.Single(r => r.SectionId == "facts").UserPrompt;
        Assert.Contains("المدعي (صاحب الطلب، الدائن بالطلبات): بن علي كريم", facts, StringComparison.Ordinal);
        Assert.Contains("المدعى عليه (المطلوب منه، الملزم بالطلبات): حمداني سمير", facts, StringComparison.Ordinal);
        Assert.Contains("إلزام المدعى عليه بدفع 500000 دج.", facts, StringComparison.Ordinal);
        var grounds = provider.Requests.Single(r => r.SectionId == "grounds").UserPrompt;
        Assert.Contains("قرض 500000 دج بتاريخ 10/01/2025 لم يسدد.", grounds, StringComparison.Ordinal);
        Assert.Contains("لا تعكس أدوار الطرفين", provider.Requests[0].SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CorruptedAnswerIsRetriedOnce()
    {
        var answers = new Queue<string>(["حيث إن المدعى عليه لم受理 الدين", "حيث إن المدعى عليه اقترض مبلغ 500000 دج"]);
        var provider = new RecordingProvider(r => r.SectionId == "facts" ? answers.Dequeue() : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var facts = draft.FindSection("facts")!;
        Assert.Equal("حيث إن المدعى عليه اقترض مبلغ 500000 دج", facts.Text);
        Assert.Equal(DraftingNotes.Retried, facts.DraftingNote);
        Assert.Equal(2, provider.Requests.Count(r => r.SectionId == "facts"));
    }

    [Fact]
    public async Task PersistentlyCorruptedAnswerFallsBackToTheLawyersNotes()
    {
        var provider = new RecordingProvider(r => r.SectionId == "facts" ? "受理 受理" : r.SourceText);

        var draft = await new DraftingService(provider).DraftAsync(Requete, Fixtures.CompleteInputs());

        var facts = draft.FindSection("facts")!;
        Assert.Equal("قرض 500000 دج بتاريخ 10/01/2025 لم يسدد.", facts.Text);
        Assert.Equal(DraftingNotes.FellBackToNotes, facts.DraftingNote);
    }

    [Theory]
    [InlineData("<think>تفكير</think>\nحيث إن", "حيث إن")]
    [InlineData("```\nحيث إن\n```", "حيث إن")]
    [InlineData("الوقائع:\nحيث إن **العارض**", "حيث إن العارض")]
    [InlineData("## الوقائع\n\nحيث إن", "حيث إن")]
    public void ModelOutputIsCleaned(string raw, string expected)
    {
        var facts = Requete.FindSection("facts")!;
        Assert.Equal(expected, DraftingService.CleanModelOutput(raw, facts));
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
