using DzLegal.Domain;
using DzLegal.Drafting;

namespace DzLegal.App.Tests;

/// <summary>A scripted local model for the grounds analysis: a fixed reading, then a fixed choice of candidates.</summary>
internal static class Analysis
{
    public const string Reading = """
        العلاقة القانونية: عقد قرض بين شخصين طبيعيين
        الالتزام المخل به: لم يرد المدعى عليه المبلغ في الأجل
        المطلوب: إلزامه برد المبلغ
        نوع الدعوى: loan-repayment
        كلمات البحث: قرض الاستهلاك، رد المثل، الإعذار
        """;

    public const string Selection = """
        civil-code:450 | أساس | المدعى عليه اقترض المبلغ ولم يرده
        civil-code:323 | إثبات | على المدعي إثبات القرض
        civil-code:999 | أساس | مادة غير مرشحة
        """;

    public static RecordingProvider Model() => new(r => r.SectionId switch
    {
        GroundsAnalyzer.ReadingRequestId => Reading,
        GroundsAnalyzer.SelectionRequestId => Selection,
        _ => r.SourceText,
    });
}

public sealed class GroundsWorkflowTests
{
    [Fact]
    public void TheNotesAreSplitIntoFactsGroundsAndRequestsSteps()
    {
        var vm = Vm.Create();
        var ids = vm.FieldGroups.Select(g => g.Id).ToList();
        Assert.Equal(["court", "plaintiff", "defendant", "claim", "facts", "grounds", "requests", "documents", "signature"], ids);
        Assert.Equal(["notes.facts"], vm.FieldGroups.Single(g => g.Id == "facts").Fields.Select(f => f.Id));

        vm.SelectStepCommand.Execute(vm.FieldGroups.Single(g => g.Id == "grounds"));
        Assert.True(vm.IsGroundsStep);
        Assert.False(vm.IsRequestsStep);
        Assert.True(vm.CanAnalyseGrounds);
    }

    [Fact]
    public async Task TheAnalysisShowsTheReadingAndProposesArticlesButTicksNothingUntilTheLawyerConfirmsTheClaim()
    {
        var recorder = Analysis.Model();
        var vm = Vm.Create(recorder: recorder).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);

        Assert.Equal("عقد قرض بين شخصين طبيعيين", vm.ReadingRelationship);
        Assert.Equal("استرداد مبلغ قرض", vm.ReadingClaims);
        // The loan claim's required articles come first (450, 323, 454: no interest), whether or not the model chose
        // them; 999 was not a candidate.
        Assert.Equal(["450", "323", "454"], vm.AnalysedArticles.Select(a => a.Reference.Number));
        var first = vm.AnalysedArticles[0];
        Assert.Equal(vm.L["Role_basis"], first.RoleText);
        Assert.Equal(vm.L.Format("ModelReason", "المدعى عليه اقترض المبلغ ولم يرده"), first.ModelReason);
        Assert.Equal(vm.L["ProvenanceRequired"], first.ProvenanceTag);
        Assert.True(first.IsRequired);
        Assert.Equal(vm.L["FromClaimCatalog"], first.SourceText);
        Assert.True(first.IsAvailable); // the shipped library has the official text of every catalog article
        Assert.Equal(77, first.Page);
        Assert.Equal(vm.L["Role_limit"], vm.AnalysedArticles[2].RoleText);
        Assert.Equal(vm.L.Format("AnalysisDone", 3, "recording"), vm.AnalysisStatus);
        Assert.Empty(vm.SelectedArticles); // the model's guess ticks nothing
        Assert.True(vm.NeedsClaimConfirmation);

        vm.ConfirmClaimCommand.Execute(null);
        Assert.Equal(["450", "323", "454"], vm.SelectedArticles.Select(a => a.Number));
        Assert.True(vm.IsClaimConfirmed);
        Assert.False(vm.NeedsClaimConfirmation);
        Assert.False(vm.IsAnalysisStale);

        // The local model saw the catalog's civil claims, the lawyer's section as a hint, and the candidates' keys.
        var reading = recorder.Requests.Single(r => r.SectionId == GroundsAnalyzer.ReadingRequestId).UserPrompt;
        Assert.Contains("loan-repayment — استرداد مبلغ قرض", reading, StringComparison.Ordinal);
        Assert.DoesNotContain("khul", reading, StringComparison.Ordinal);
        Assert.Contains("القسم الذي اختاره المحامي: القسم المدني", reading, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AClaimTypeOfAnotherAreaThanTheSectionGetsANeutralNoteAndAnAdministrativeSectionAMessage()
    {
        // With a hosted engine every claim type is offered and the section is only a hint: a loan filed under the
        // commercial section is still read as a loan, and the status says the section is unusual for it, without
        // blocking anything.
        var online = new ProviderOption("online", _ => "online", Analysis.Model, sendsCaseData: true);
        var vm = Vm.Create(extra: online).Filled();
        vm.Field("court.section").Value = "القسم التجاري";
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.Equal("استرداد مبلغ قرض", vm.ReadingClaims);
        Assert.Equal(vm.L.Format("AnalysisDone", 3, "recording") + " " + vm.L["AnalysisSectionNote"], vm.AnalysisStatus);
        Assert.True(vm.NeedsClaimConfirmation);

        vm.Field("court.section").Value = "القسم المدني";
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.Equal(vm.L.Format("AnalysisDone", 3, "recording"), vm.AnalysisStatus);

        var recorder = Analysis.Model();
        var administrative = Vm.Create(recorder: recorder).Filled();
        administrative.Field("court.section").Value = "المحكمة الإدارية";
        await administrative.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.Equal(administrative.L["AnalysisAdministrative"], administrative.AnalysisStatus);
        Assert.Empty(administrative.AnalysedArticles);
        Assert.Empty(recorder.Requests);
    }

    [Fact]
    public async Task TickedArticlesBecomeTheSelectedGroundsAndCanBeRemoved()
    {
        var vm = Vm.Create(recorder: Analysis.Model()).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        vm.ConfirmClaimCommand.Execute(null);

        Assert.Equal(3, vm.SelectedGrounds.Count); // the required articles are ticked once the claim is confirmed
        var chip = vm.SelectedGrounds[0];
        Assert.Equal("المادة 450 من القانون المدني", chip.Citation);

        chip.RemoveCommand.Execute(null);
        Assert.DoesNotContain(new ArticleRef("civil-code", "450"), vm.SelectedArticles);
        Assert.False(vm.AnalysedArticles[0].IsSelected);

        vm.TickBasisArticlesCommand.Execute(null);
        Assert.Contains(new ArticleRef("civil-code", "450"), vm.SelectedArticles);
    }

    [Fact]
    public async Task AnArticleTheNotesCiteWithoutTickingItGetsItsRuleLikeATickedOne()
    {
        // Screenshot of 12 Oct: 106/107/186, cited in the notes but not ticked, came out as «الدعوى تستند إلى المادة 106»
        // with no rule, although the library holds them.
        var recorder = Analysis.Model();
        var vm = Vm.Create(knowledge: Vm.FixtureKnowledge, recorder: recorder).Filled();
        vm.Field("notes.grounds").Value = "حيث إن المدعى عليه أخل بالتزامه طبقا للمادة 106 من القانون المدني.";
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        vm.ConfirmClaimCommand.Execute(null);
        foreach (var other in vm.AnalysedArticles.Where(a => a.Reference.Number != "450"))
        {
            other.IsSelected = false;
        }

        Assert.DoesNotContain(new ArticleRef("civil-code", "106"), vm.SelectedArticles);

        await vm.GenerateCommand.ExecuteAsync(null);

        var grounds = recorder.Requests.First(r => r.SectionId == DzLegal.Drafting.ArguedGrounds.RequestId);
        Assert.Contains("المادتين 106 و450 من القانون المدني تقضيان", grounds.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("سند آخر", grounds.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(new ArticleRef("civil-code", "106"), vm.SelectedArticles); // drafting uses it; the selection is unchanged
    }

    [Fact]
    public async Task TheGroundsAreDraftedFromTheTickedArticles()
    {
        var recorder = Analysis.Model();
        var vm = Vm.Create(knowledge: Vm.FixtureKnowledge, recorder: recorder).Filled();
        vm.Field("notes.grounds").Value = string.Empty; // no own arguments: the ticked articles are enough
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        vm.ConfirmClaimCommand.Execute(null);
        foreach (var other in vm.AnalysedArticles.Where(a => a.Reference.Number != "450"))
        {
            other.IsSelected = false;
        }

        Assert.True(vm.AnalysedArticles.Single(a => a.Reference.Number == "450").IsSelected);

        await vm.GenerateCommand.ExecuteAsync(null);

        // Argued grounds: the code writes each ticked article's rule from the catalog; the model only applies it.
        var grounds = recorder.Requests.First(r => r.SectionId == DzLegal.Drafting.ArguedGrounds.RequestId);
        Assert.Contains("بداية الفقرة (يكتبها البرنامج): حيث إن المادة 450 من القانون المدني تقضي", grounds.UserPrompt, StringComparison.Ordinal);
        Assert.StartsWith("حيث إن المادة 450 من القانون المدني تقضي", vm.Sections.Single(s => s.Id == "grounds").Text, StringComparison.Ordinal);
        Assert.Empty(vm.Field("notes.grounds").Value); // the lawyer's box is not touched

        // An article the lawyer neither ticked nor wrote is flagged, and so is a ticked article the text dropped.
        vm.Sections.Single(s => s.Id == "grounds").Text = "حيث إن المادة 777 من القانون المدني تلزم المدعى عليه.";
        var review = vm.Sections.Single(s => s.Id == "grounds").ReviewText;
        Assert.Contains(vm.L.Format("IssueUncitedArticle", "777"), review, StringComparison.Ordinal);
        Assert.Contains(vm.L.Format("IssueMissingNumber", "450"), review, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangedFactsMarkTheAnalysisStaleButKeepTheTicks()
    {
        var vm = Vm.Create(recorder: Analysis.Model()).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        vm.ConfirmClaimCommand.Execute(null);
        vm.AnalysedArticles[1].IsSelected = false;

        vm.Field("notes.facts").Value += "\nوأرسل له إعذارا ثانيا.";
        Assert.True(vm.IsAnalysisStale);
        Assert.Equal(2, vm.SelectedArticles.Count);

        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.False(vm.IsAnalysisStale);
        Assert.True(vm.AnalysedArticles[0].IsSelected);
    }

    [Fact]
    public async Task RejectingTheClaimWithdrawsTheProposalsAndTicksNothing()
    {
        var vm = Vm.Create(recorder: Analysis.Model()).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);

        vm.RejectClaimCommand.Execute(null);

        Assert.Empty(vm.AnalysedArticles);
        Assert.Empty(vm.SelectedArticles);
        Assert.False(vm.NeedsClaimConfirmation);
        Assert.Equal(vm.L["ClaimRejectedStatus"], vm.AnalysisStatus);
        Assert.Equal("عقد قرض بين شخصين طبيعيين", vm.ReadingRelationship); // the reading stays for reference
    }

    [Fact]
    public async Task FactsOutsideTheCatalogGetNoProposalAndSaySo()
    {
        var model = new RecordingProvider(_ => "العلاقة القانونية: قرار إداري\nالمطلوب: إلغاء القرار\nنوع الدعوى: other\nكلمات البحث: الإعذار");
        var vm = Vm.Create(recorder: model).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);

        Assert.Empty(vm.AnalysedArticles);
        Assert.Empty(vm.SelectedArticles);
        Assert.False(vm.NeedsClaimConfirmation);
        Assert.Equal(vm.L["AnalysisOutOfScope"], vm.AnalysisStatus);
        Assert.Single(model.Requests); // no second call: nothing to choose from
    }

    [Fact]
    public async Task TheAiMayProposeAFormalWordingOfTheRequestsThatTheLawyerAccepts()
    {
        var model = new RecordingProvider(r => r.SectionId == RequestsFormaliser.RequestId
            ? "إلزام المدعى عليه بأن يدفع للمدعي مبلغ 240000 دج بدلات الإيجار.\nتحميل المدعى عليه المصاريف القضائية."
            : r.SourceText);
        var vm = Vm.Create(recorder: model).Filled();
        vm.Field("notes.requests").Value = "يخلص 240000 دج تاع الكرا.\nيخلص المصاريف.";
        var before = vm.Sections.Single(s => s.Id == "requests").Text;
        Assert.Contains("يخلص 240000 دج تاع الكرا.", before, StringComparison.Ordinal); // the lawyer's lines, verbatim

        Assert.True(vm.CanProposeRequestsWording);
        await vm.ProposeRequestsWordingCommand.ExecuteAsync(null);
        Assert.True(vm.HasRequestsProposal);
        Assert.Equal(vm.L["RequestsWordingReady"], vm.RequestsWordingStatus);
        Assert.Equal(before, vm.Sections.Single(s => s.Id == "requests").Text); // nothing changes until accepted

        vm.AcceptRequestsProposalCommand.Execute(null);
        Assert.False(vm.HasRequestsProposal);
        Assert.Contains("1. إلزام المدعى عليه بأن يدفع للمدعي مبلغ 240000 دج بدلات الإيجار.", vm.Sections.Single(s => s.Id == "requests").Text, StringComparison.Ordinal);
        Assert.StartsWith("إلزام المدعى عليه", vm.Field("notes.requests").Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AWordingThatChangesAnAmountIsNeverShown()
    {
        var model = new RecordingProvider(r => r.SectionId == RequestsFormaliser.RequestId ? "إلزام المدعى عليه بدفع 24000 دج." : r.SourceText);
        var vm = Vm.Create(recorder: model).Filled();
        vm.Field("notes.requests").Value = "يخلص 240000 دج.";

        await vm.ProposeRequestsWordingCommand.ExecuteAsync(null);

        Assert.False(vm.HasRequestsProposal);
        Assert.Equal(vm.L["RequestsWordingWithheld"], vm.RequestsWordingStatus);
        Assert.Equal("يخلص 240000 دج.", vm.Field("notes.requests").Value);
        Assert.False(Vm.Create().CanProposeRequestsWording); // template mode has no wording to offer
    }

    [Fact]
    public async Task TemplateModeCannotAnalyseAndSaysSo()
    {
        var vm = Vm.Create().Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.Empty(vm.AnalysedArticles);
        Assert.False(vm.HasReading);
        Assert.Equal(vm.L.Format("AnalysisNothing", vm.L["ProviderStub"]), vm.AnalysisStatus);

        var empty = Vm.Create();
        await empty.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.Equal(empty.L["GroundsNeedFacts"], empty.AnalysisStatus);
    }

    [Fact]
    public async Task TheRequestsAndDocumentsStepsOfferTheUsualLines()
    {
        var vm = Vm.Create(recorder: Analysis.Model()).Filled();
        Assert.False(vm.HasRequestChips);
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);

        var request = vm.RequestChips[0];
        request.InsertCommand.Execute(null);
        request.InsertCommand.Execute(null);
        Assert.True(request.IsInserted);
        Assert.Single(vm.Field("notes.requests").Value.Split(request.Text).Skip(1));

        vm.DocumentChips.Single(d => d.Text == "محضر الإعذار").InsertCommand.Execute(null);
        Assert.Contains("- محضر الإعذار", vm.Field("documents.list").Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheAnalysisAndTheTicksAreSavedWithTheDraft()
    {
        var files = new FakeFiles();
        var vm = Vm.Create(files, recorder: Analysis.Model()).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        vm.ConfirmClaimCommand.Execute(null);
        vm.AnalysedArticles[0].IsSelected = false;
        vm.AnalysedArticles[2].IsSelected = false;
        await vm.SaveCommand.ExecuteAsync(null);

        var reopened = Vm.Create(files);
        await reopened.OpenCommand.ExecuteAsync(null);
        Assert.Equal("عقد قرض بين شخصين طبيعيين", reopened.ReadingRelationship);
        Assert.Equal([new ArticleRef("civil-code", "323")], reopened.SelectedArticles);
        Assert.True(reopened.AnalysedArticles[1].IsSelected);
        Assert.True(reopened.IsClaimConfirmed); // the confirmation is saved with the analysis
        Assert.False(reopened.IsAnalysisStale);

        reopened.NewCommand.Execute(null);
        Assert.Empty(reopened.SelectedArticles);
        Assert.Empty(reopened.AnalysedArticles);
        Assert.False(reopened.HasReading);
    }
}

public sealed class ArticleSearchAndProvenanceTests
{
    [Fact]
    public void SearchFindsImportedArticlesAndSharesTheTick()
    {
        var empty = Vm.Create(knowledge: DzLegal.Law.LegalKnowledge.Empty);
        empty.ArticleQuery = "450";
        Assert.Equal(empty.L["LibraryNotImported"], empty.ArticleSearchHint);

        var vm = Vm.Create(knowledge: Vm.FixtureKnowledge).Filled();
        vm.ArticleQuery = "المادة 450 ق م";
        var hit = Assert.Single(vm.ArticleResults);
        Assert.Equal(vm.L["ProvenanceTextUnverified"], hit.ProvenanceTag);
        hit.IsSelected = true;
        Assert.Equal("المادة 450 من القانون المدني", Assert.Single(vm.SelectedGrounds).Citation);

        vm.ArticleQuery = "القرض الوهمي";
        Assert.Equal("450", vm.ArticleResults[0].Reference.Number);
        Assert.True(vm.ArticleResults[0].IsSelected);

        vm.ArticleQuery = "مادة لا وجود لها 9999";
        Assert.Equal(vm.L["ArticleSearchNone"], vm.ArticleSearchHint);
    }

    [Fact]
    public async Task EachProposedArticleShowsItsOfficialReferenceAndOpensTheOfficialSiteOnClick()
    {
        var links = new FakeLinks();
        var vm = Vm.Create(recorder: Analysis.Model(), links: links).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        var article = vm.AnalysedArticles[0];

        Assert.StartsWith("الأمر رقم 75-58", article.ReferenceText, StringComparison.Ordinal);
        Assert.Equal(vm.L["CheckInOfficialJournal"], article.LinkLabel);
        await article.OpenSourceCommand.ExecuteAsync(null);
        Assert.Equal([new Uri("https://www.joradp.dz")], links.Opened);
        Assert.Equal(vm.L["LinkOpened"], vm.StatusMessage);
    }

    [Fact]
    public async Task ANonOfficialLinkIsNeverOpened()
    {
        var links = new FakeLinks();
        var vm = Vm.Create(links: links);
        await vm.OpenOfficialLinkAsync("https://example.com/phishing");
        Assert.Empty(links.Opened);
        Assert.True(vm.IsError);
    }
}
