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

        var draft = await new DraftingService(provider).DraftAsync(Fixtures.Requete, Fixtures.CompleteInputs(), new DraftOptions { LegalTexts = Selected });

        var grounds = draft.FindSection("grounds")!;
        Assert.Contains("المادة 124", grounds.Text, StringComparison.Ordinal);
        Assert.Contains(DraftingNoteCodes.Retried, grounds.DraftingNotes);
    }
}
