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
    public async Task TheAnalysisShowsTheReadingAndTheProposedArticlesNothingTicked()
    {
        var recorder = Analysis.Model();
        var vm = Vm.Create(recorder: recorder).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);

        Assert.Equal("عقد قرض بين شخصين طبيعيين", vm.ReadingRelationship);
        Assert.Equal("استرداد مبلغ قرض", vm.ReadingClaims);
        Assert.Equal(["450", "323"], vm.AnalysedArticles.Select(a => a.Reference.Number)); // 999 was not a candidate
        var first = vm.AnalysedArticles[0];
        Assert.Equal(vm.L["Role_basis"], first.RoleText);
        Assert.Equal(vm.L.Format("ModelReason", "المدعى عليه اقترض المبلغ ولم يرده"), first.ModelReason);
        Assert.Equal(vm.L["ProvenanceModel"], first.ProvenanceTag);
        Assert.Equal(vm.L["FromClaimCatalog"], first.SourceText);
        Assert.True(first.IsAvailable); // the shipped library has the official text of every catalog article
        Assert.Empty(vm.SelectedArticles);
        Assert.Equal(vm.L.Format("AnalysisDone", 2, "recording"), vm.AnalysisStatus);
        Assert.False(vm.IsAnalysisStale);

        // The model only saw the catalog's civil claims and the candidates' keys.
        var reading = recorder.Requests.Single(r => r.SectionId == GroundsAnalyzer.ReadingRequestId).UserPrompt;
        Assert.Contains("loan-repayment — استرداد مبلغ قرض", reading, StringComparison.Ordinal);
        Assert.DoesNotContain("khul", reading, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TickedArticlesBecomeTheSelectedGroundsAndCanBeRemoved()
    {
        var vm = Vm.Create(recorder: Analysis.Model()).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);

        vm.TickBasisArticlesCommand.Execute(null);
        Assert.Equal([new ArticleRef("civil-code", "450")], vm.SelectedArticles); // 323 is a proof article
        var chip = Assert.Single(vm.SelectedGrounds);
        Assert.Equal("المادة 450 من القانون المدني", chip.Citation);

        chip.RemoveCommand.Execute(null);
        Assert.Empty(vm.SelectedArticles);
        Assert.False(vm.AnalysedArticles[0].IsSelected);
    }

    [Fact]
    public async Task TheGroundsAreDraftedFromTheTickedArticles()
    {
        var recorder = Analysis.Model();
        var vm = Vm.Create(knowledge: Vm.FixtureKnowledge, recorder: recorder).Filled();
        vm.Field("notes.grounds").Value = string.Empty; // no own arguments: the ticked articles are enough
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        vm.AnalysedArticles.Single(a => a.Reference.Number == "450").IsSelected = true;

        await vm.GenerateCommand.ExecuteAsync(null);

        var grounds = recorder.Requests.Single(r => r.SectionId == "grounds");
        Assert.Equal("- المادة 450 من القانون المدني", grounds.SourceText);
        Assert.Contains("- المادة 450 من القانون المدني: «نص وهمي للاختبار: القرض الوهمي", grounds.UserPrompt, StringComparison.Ordinal);
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
        vm.AnalysedArticles[0].IsSelected = true;

        vm.Field("notes.facts").Value += "\nوأرسل له إعذارا ثانيا.";
        Assert.True(vm.IsAnalysisStale);
        Assert.Single(vm.SelectedArticles);

        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.False(vm.IsAnalysisStale);
        Assert.True(vm.AnalysedArticles[0].IsSelected);
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
        vm.AnalysedArticles[1].IsSelected = true;
        await vm.SaveCommand.ExecuteAsync(null);

        var reopened = Vm.Create(files);
        await reopened.OpenCommand.ExecuteAsync(null);
        Assert.Equal("عقد قرض بين شخصين طبيعيين", reopened.ReadingRelationship);
        Assert.Equal([new ArticleRef("civil-code", "323")], reopened.SelectedArticles);
        Assert.True(reopened.AnalysedArticles[1].IsSelected);
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
