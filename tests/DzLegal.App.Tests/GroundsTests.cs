using DzLegal.Domain;
using DzLegal.Drafting;

namespace DzLegal.App.Tests;

public sealed class GroundsTests
{
    [Fact]
    public void TheNotesStepSuggestsTheLoanClaimFromTheFacts()
    {
        var vm = Vm.Create().Filled();
        vm.SelectStepCommand.Execute(vm.FieldGroups.Single(g => g.Id == "notes"));

        Assert.True(vm.IsNotesStep);
        var first = vm.GroundsSuggestions[0];
        Assert.Equal("loan-repayment", first.Claim.Id);
        Assert.True(first.IsExpanded);
        Assert.Contains("أقرض", first.MatchedText, StringComparison.Ordinal);
        Assert.Empty(vm.GroundsHint);

        // No official text imported in the shipped library: citations show, texts are marked unavailable.
        var article = first.Articles.First(a => a.Reference.Number == "450");
        Assert.Equal("المادة 450 من القانون المدني", article.Citation);
        Assert.False(article.IsAvailable);
        Assert.Equal(vm.L["ArticleTextUnavailable"], article.Text);
    }

    [Fact]
    public void HintsExplainAnEmptyList()
    {
        var vm = Vm.Create();
        Assert.Equal(vm.L["GroundsNeedFacts"], vm.GroundsHint);
        vm.Field("notes.facts").Value = "وقع خطأ في تاريخ ميلاد المدعي في سجلات الحالة المدنية";
        Assert.Empty(vm.GroundsSuggestions);
        Assert.Equal(vm.L["GroundsNone"], vm.GroundsHint);
    }

    [Fact]
    public void TickedArticlesAreAddedToTheGroundsNotesOnce()
    {
        var vm = Vm.Create().Filled();
        Assert.False(vm.AddSelectedToGroundsCommand.CanExecute(null));
        var loan = vm.GroundsSuggestions[0];
        loan.Articles.First(a => a.Reference.Number == "450").IsSelected = true;
        loan.Articles.First(a => a.Reference.Number == "106").IsSelected = true; // already cited in the notes

        Assert.Equal(2, vm.SelectedArticleCount);
        vm.AddSelectedToGroundsCommand.Execute(null);
        vm.AddSelectedToGroundsCommand.Execute(null);

        var grounds = vm.Field("notes.grounds").Value;
        Assert.EndsWith("\n- المادة 450 من القانون المدني", grounds, StringComparison.Ordinal);
        Assert.Single(grounds.Split("المادة 106").Skip(1));
    }

    [Fact]
    public void UsualRequestsAndDocumentsCanBeInserted()
    {
        var vm = Vm.Create().Filled();
        var loan = vm.GroundsSuggestions[0];
        loan.InsertRequestsCommand.Execute(null);
        loan.InsertDocumentsCommand.Execute(null);

        Assert.Contains("- إلزام المدعى عليه بأن يؤدي للمدعي مبلغ القرض", vm.Field("notes.requests").Value, StringComparison.Ordinal);
        Assert.Contains("- محضر الإعذار", vm.Field("documents.list").Value, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportedTextsGroundTheModelAndTheChecks()
    {
        var recorder = new RecordingProvider();
        var vm = Vm.Create(knowledge: Vm.FixtureKnowledge, recorder: recorder).Filled();
        var article = vm.GroundsSuggestions[0].Articles.First(a => a.Reference.Number == "450");
        Assert.True(article.IsAvailable);
        Assert.StartsWith("نص وهمي للاختبار", article.Text, StringComparison.Ordinal);
        article.IsSelected = true;

        vm.GenerateCommand.Execute(null);

        var prompt = recorder.Requests.Single(r => r.SectionId == "grounds").UserPrompt;
        Assert.Contains("- المادة 450 من القانون المدني: «نص وهمي للاختبار: القرض الوهمي", prompt, StringComparison.Ordinal);
        Assert.Equal(["450"], vm.SelectedLegalTexts().Select(t => t.Number));

        // An article the lawyer neither noted nor ticked is flagged under the section.
        vm.Sections.Single(s => s.Id == "grounds").Text = "حيث إن المادة 777 من القانون المدني تلزم المدعى عليه.";
        Assert.Contains("777", vm.Sections.Single(s => s.Id == "grounds").ReviewText, StringComparison.Ordinal);
    }

    [Fact]
    public void ArticleSearchFindsImportedArticlesAndSharesTheTick()
    {
        var empty = Vm.Create();
        empty.ArticleQuery = "450";
        Assert.Equal(empty.L["LibraryNotImported"], empty.ArticleSearchHint);

        var vm = Vm.Create(knowledge: Vm.FixtureKnowledge).Filled();
        vm.ArticleQuery = "المادة 450 ق م";
        var hit = Assert.Single(vm.ArticleResults);
        hit.IsSelected = true;
        Assert.True(vm.GroundsSuggestions[0].Articles.First(a => a.Reference.Number == "450").IsSelected);

        vm.ArticleQuery = "مادة لا وجود لها 9999";
        Assert.Equal(vm.L["ArticleSearchNone"], vm.ArticleSearchHint);
    }

    [Fact]
    public async Task TheSelectionIsSavedWithTheDraft()
    {
        var files = new FakeFiles();
        var vm = Vm.Create(files).Filled();
        vm.GroundsSuggestions[0].Articles.First(a => a.Reference.Number == "323").IsSelected = true;
        await vm.SaveCommand.ExecuteAsync(null);

        var reopened = Vm.Create(files);
        await reopened.OpenCommand.ExecuteAsync(null);
        Assert.Equal([new ArticleRef("civil-code", "323")], reopened.SelectedArticles);
        Assert.True(reopened.GroundsSuggestions[0].Articles.First(a => a.Reference.Number == "323").IsSelected);

        reopened.NewCommand.Execute(null);
        Assert.Empty(reopened.SelectedArticles);
    }
}

public sealed class GroundsProvenanceTests
{
    [Fact]
    public async Task EachArticleShowsItsOfficialReferenceAndOpensTheOfficialSiteOnClick()
    {
        var links = new FakeLinks();
        var vm = Vm.Create(links: links).Filled();
        var article = vm.GroundsSuggestions[0].Articles.First(a => a.Reference.Number == "107");

        Assert.StartsWith("الأمر رقم 75-58", article.ReferenceText, StringComparison.Ordinal);
        Assert.Equal(vm.L["CheckInOfficialJournal"], article.LinkLabel);
        Assert.Equal(vm.L["ProvenanceCatalog"], article.ProvenanceTag);
        Assert.False(article.IsVerified);

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

public sealed class ModelFallbackTests
{
    /// <summary>Facts no keyword group matches (the catalog has no civil-status claim).</summary>
    private const string Unmatched = "وقع خطأ في تاريخ ميلاد المدعي في سجلات الحالة المدنية";

    [Fact]
    public async Task TheModelChoosesOnlyFromTheCatalogAndItsPicksAreTagged()
    {
        var recorder = new RecordingProvider(r => r.SectionId == ClaimPicker.RequestId ? "liability-for-things\ninvented-claim\n<think>loan-repayment</think>" : r.SourceText);
        var vm = Vm.Create(recorder: recorder).Filled();
        vm.Field("claim.subject").Value = "دعوى";
        vm.Field("notes.facts").Value = Unmatched;
        Assert.Empty(vm.GroundsSuggestions);
        Assert.True(vm.CanAskModelForClaims);

        await vm.AskModelForClaimsCommand.ExecuteAsync(null);

        var prompt = recorder.Requests.Single(r => r.SectionId == ClaimPicker.RequestId).UserPrompt;
        Assert.Contains("liability-for-things — مسؤولية حارس الشيء", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("khul", prompt, StringComparison.Ordinal); // civil section: civil claims only

        var picked = Assert.Single(vm.GroundsSuggestions);
        Assert.Equal("liability-for-things", picked.Claim.Id);
        Assert.True(picked.IsPickedByModel);
        Assert.Equal(vm.L["PickedByModel"], picked.MatchedText);
        Assert.Contains(picked.Articles, a => a.Reference.Number == "138"); // the catalog's own articles
        Assert.Equal(vm.L.Format("ModelPicked", 1), vm.ModelPickHint);

        // New facts: the old picks no longer apply.
        vm.Field("notes.facts").Value = Unmatched + " ليلا";
        Assert.Empty(vm.GroundsSuggestions);
        Assert.Empty(vm.ModelPickHint);
    }

    [Fact]
    public async Task PicksAlreadySuggestedByKeywordsAreNotRepeated()
    {
        var recorder = new RecordingProvider(r => r.SectionId == ClaimPicker.RequestId ? "loan-repayment" : r.SourceText);
        var vm = Vm.Create(recorder: recorder).Filled();
        await vm.AskModelForClaimsCommand.ExecuteAsync(null);

        Assert.Single(vm.GroundsSuggestions, g => g.Claim.Id == "loan-repayment");
        Assert.DoesNotContain(vm.GroundsSuggestions, g => g.IsPickedByModel);
        Assert.Equal(vm.L["ModelPickedSame"], vm.ModelPickHint);
    }

    [Fact]
    public async Task TemplateModeFindsNothingAndSaysSo()
    {
        var vm = Vm.Create().Filled();
        vm.Field("claim.subject").Value = "دعوى";
        vm.Field("notes.facts").Value = Unmatched;
        await vm.AskModelForClaimsCommand.ExecuteAsync(null);
        Assert.Empty(vm.GroundsSuggestions);
        Assert.Equal(vm.L["ModelPickedNothing"], vm.ModelPickHint);
    }
}
