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
