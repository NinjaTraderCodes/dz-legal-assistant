namespace DzLegal.Drafting.Tests;

public sealed class ClaimPickerTests
{
    private const string Prompt = "الموضوع: {{ subject }}\nالوقائع: {{ facts }}\nالقائمة:\n{{ claims }}\nاكتب المعرفات فقط أو: لا شيء";

    private static readonly ClaimOption[] Options =
    [
        new("loan-repayment", "استرداد مبلغ قرض"),
        new("tort-damages", "تعويض عن ضرر"),
        new("khul", "الخلع"),
    ];

    [Fact]
    public void ThePromptListsOnlyTheAllowedClaimTypes()
    {
        var request = ClaimPicker.BuildRequest(Prompt, "المطالبة بدين", "أقرض المدعي المدعى عليه مبلغا", Options[..2]);
        Assert.Equal(ClaimPicker.RequestId, request.SectionId);
        Assert.Contains("loan-repayment — استرداد مبلغ قرض\ntort-damages — تعويض عن ضرر", request.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("khul", request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("الوقائع: أقرض المدعي", request.UserPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("tort-damages\nloan-repayment", new[] { "tort-damages", "loan-repayment" })]
    [InlineData("1. loan-repayment (الأنسب)\n2. contract-damages\n3. Loan-Repayment", new[] { "loan-repayment" })]
    [InlineData("<think>khul could fit</think>\ntort-damages", new[] { "tort-damages" })]
    [InlineData("<think>khul, tort-damages… (cut off)", new string[0])]
    [InlineData("لا شيء", new string[0])]
    [InlineData("khul\ntort-damages\nloan-repayment\nkhul", new[] { "khul", "tort-damages", "loan-repayment" })]
    public void OnlyKnownIdsAreKeptInOrder(string answer, string[] expected) =>
        Assert.Equal(expected, ClaimPicker.Parse(answer, Options));

    [Fact]
    public async Task TheModelIsNotCalledWithoutFactsOrOptions()
    {
        var provider = new RecordingProvider(_ => "khul");
        Assert.Empty(await new ClaimPicker(provider).PickAsync(Prompt, "", "  ", Options));
        Assert.Empty(await new ClaimPicker(provider).PickAsync(Prompt, "", "وقائع", []));
        Assert.Empty(provider.Requests);
    }

    [Fact]
    public async Task PicksComeFromTheModelAnswer()
    {
        var provider = new RecordingProvider(_ => "khul\ninvented-claim");
        Assert.Equal(["khul"], await new ClaimPicker(provider).PickAsync(Prompt, "", "تريد الزوجة مخالعة زوجها", Options));
    }

    [Fact]
    public async Task CancellationStopsTheRequest()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ClaimPicker(new SlowProvider()).PickAsync(Prompt, "", "وقائع", Options, cts.Token));
    }

    private sealed class SlowProvider : IModelProvider
    {
        public string Name => "slow";

        public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            return "khul";
        }
    }
}
