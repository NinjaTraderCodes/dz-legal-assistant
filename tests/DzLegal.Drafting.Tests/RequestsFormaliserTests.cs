namespace DzLegal.Drafting.Tests;

public sealed class RequestsFormaliserTests
{
    private const string DarijaNotes = "يخلص 240000 دج تاع الكرا لي راه فاتو.\nيخلص المصاريف تاع المحكمة.";

    private static Dictionary<string, string> Inputs(string requests)
    {
        var inputs = Fixtures.CompleteInputs();
        inputs["notes.requests"] = requests;
        return inputs;
    }

    [Fact]
    public async Task AFaithfulFormalWordingIsOffered()
    {
        var provider = new RecordingProvider(_ => "إلزام المدعى عليه بأن يدفع للمدعية مبلغ 240000 دج بدلات الإيجار المستحقة.\nتحميل المدعى عليه المصاريف القضائية.");

        var proposal = await new RequestsFormaliser(provider).ProposeAsync(Fixtures.Requete, Inputs(DarijaNotes));

        Assert.True(proposal.IsOffered, string.Join("; ", proposal.Problems));
        Assert.Equal(2, proposal.Lines.Count);
        var request = Assert.Single(provider.Requests);
        Assert.Contains("يخلص 240000 دج", request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("2 سطرا", request.UserPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("إلزام المدعى عليه بدفع 204000 دج.\nتحميله المصاريف القضائية.", "numbers")] // amount changed
    [InlineData("إلزام المدعى عليه بدفع 240000 دج وتحميله المصاريف القضائية.", "lines")] // two requests merged
    [InlineData("إلزام المدعى عليه بدفع مائتين وأربعين ألف دينار (240000 دج).\nتحميله المصاريف القضائية.", "number in words")]
    [InlineData("Condamner le défendeur à payer 240000 DA.\nتحميله المصاريف القضائية.", "Latin")]
    public void AProposalThatChangesTheReliefIsWithheld(string answer, string problem)
    {
        var problems = RequestsFormaliser.Check(RequestsFormaliser.Lines(DarijaNotes), RequestsFormaliser.Lines(answer));
        Assert.Contains(problems, p => p.Contains(problem, StringComparison.Ordinal));
    }

    [Fact]
    public void AnAlternativeMustStayAnAlternative()
    {
        var original = RequestsFormaliser.Lines("يسلم 20 آلة ولا يرجع 800,000 دج.");
        Assert.NotEmpty(RequestsFormaliser.Check(original, ["إلزام المدعى عليه بتسليم 20 آلة وإرجاع الثمن 800.000 دج."]));
        Assert.Empty(RequestsFormaliser.Check(original, ["إلزام المدعى عليه بتسليم 20 آلة أو إرجاع الثمن 800.000 دج."]));
    }

    [Fact]
    public async Task AFailingAnswerIsRetriedOnceThenWithheld()
    {
        var provider = new RecordingProvider(_ => "إلزام المدعى عليه بدفع كل المستحقات.");

        var proposal = await new RequestsFormaliser(provider).ProposeAsync(Fixtures.Requete, Inputs(DarijaNotes));

        Assert.False(proposal.IsOffered);
        Assert.Equal(2, provider.Requests.Count);
    }
}
