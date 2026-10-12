using DzLegal.Domain;

namespace DzLegal.Drafting.Tests;

public sealed class LegalTextTests
{
    private static readonly LegalText[] Selected = [new("124", "القانون التجريبي", "كل فعل وهمي يرتكبه الشخص بخطئه ويسبب ضررا للغير يلزم من كان سببا في حدوثه بالتعويض.")];

    [Fact]
    public void SelectedTextsAreInTheGroundsPromptOnlyWhenThereAreAny()
    {
        var service = new DraftingService(new RecordingProvider(_ => string.Empty));
        var grounds = Fixtures.Requete.FindSection("grounds")!;

        var with = service.BuildRequest(Fixtures.Requete, grounds, Fixtures.CompleteInputs(), Selected).UserPrompt;
        Assert.Contains("اختارها المحامي من النص الرسمي", with, StringComparison.Ordinal);
        Assert.Contains("- المادة 124 من القانون التجريبي: «كل فعل وهمي", with, StringComparison.Ordinal);

        var without = service.BuildRequest(Fixtures.Requete, grounds, Fixtures.CompleteInputs()).UserPrompt;
        Assert.DoesNotContain("اختارها المحامي من النص الرسمي", without, StringComparison.Ordinal);
    }

    [Fact]
    public void ALongArticleIsShortenedInThePromptOnly()
    {
        var longText = string.Join(' ', Enumerable.Repeat("يلتزم المدين الوهمي بالوفاء", 80));
        var block = LegalText.Block([new LegalText("99", "القانون التجريبي", longText)])!;
        Assert.True(block.Length < LegalText.MaxPromptLength + 60, $"{block.Length} characters");
        Assert.EndsWith("…»", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// A slow local model spends most of its time reading the prompt: these budgets stop prompts from growing back.
    /// Raise them only on purpose, after measuring the speed.
    /// </summary>
    [Theory]
    [InlineData("grounds", 6000)]
    public void DraftingPromptsStayWithinTheirBudget(string sectionId, int budget)
    {
        var request = new DraftingService(new RecordingProvider(_ => string.Empty))
            .BuildRequest(Fixtures.Requete, Fixtures.Requete.FindSection(sectionId)!, Fixtures.CompleteInputs());
        var size = request.SystemPrompt.Length + request.UserPrompt.Length;
        Assert.True(size <= budget, $"{sectionId}: {size} characters, budget {budget}");
    }

    [Fact]
    public void EachModelCallCarriesItsOwnAnswerCap()
    {
        var service = new DraftingService(new RecordingProvider(_ => string.Empty));
        Assert.Equal(900, service.BuildRequest(Fixtures.Requete, Fixtures.Requete.FindSection("grounds")!, Fixtures.CompleteInputs()).MaxTokens);
        Assert.Equal(900, new SectionReviser(new RecordingProvider(_ => string.Empty))
            .BuildRequest(Fixtures.Requete, "grounds", "نص", "أوجز", Fixtures.CompleteInputs()).MaxTokens);
    }

    [Fact]
    public void TheRevisionPromptCarriesTheSelectedTextsToo()
    {
        var request = new SectionReviser(new RecordingProvider(_ => string.Empty))
            .BuildRequest(Fixtures.Requete, "grounds", "حيث إن", "أوجز", Fixtures.CompleteInputs(), Selected);
        Assert.Contains("- المادة 124 من القانون التجريبي:", request.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnArticleNobodySelectedEarnsOneMoreTry()
    {
        var answers = new Queue<string>(["حيث إن المادة 777 من القانون المدني تلزم المدعى عليه.", "حيث إن المادة 124 من القانون التجريبي تلزم المدعى عليه بالتعويض."]);
        var provider = new RecordingProvider(r => r.SectionId == "grounds" ? answers.Dequeue() : "إلزام المدعى عليه بأداء 500000 دج.");

        var draft = await new DraftingService(provider) { ClassicGrounds = true }.DraftAsync(Fixtures.Requete, Fixtures.CompleteInputs(), new DraftOptions { LegalTexts = Selected });

        var grounds = draft.FindSection("grounds")!;
        Assert.Contains("المادة 124", grounds.Text, StringComparison.Ordinal);
        Assert.Contains(DraftingNoteCodes.Retried, grounds.DraftingNotes);
    }
}
