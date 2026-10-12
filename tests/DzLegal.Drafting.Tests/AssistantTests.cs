using System.Net;
using System.Text;
using DzLegal.Domain;
using DzLegal.ModelProviders.Ollama;
using DzLegal.ModelProviders.Stub;

namespace DzLegal.Drafting.Tests;

public class TextDiffTests
{
    [Fact]
    public void WordsShowAddedAndRemoved()
    {
        var diff = TextDiff.Words("حيث إن المدعى عليه امتنع", "حيث إن المدعى عليه رفض السداد");

        Assert.Equal(["حيث", "إن", "المدعى", "عليه"], diff.Where(d => d.Kind == DiffKind.Same).Select(d => d.Text));
        Assert.Equal(["امتنع"], diff.Where(d => d.Kind == DiffKind.Removed).Select(d => d.Text));
        Assert.Equal(["رفض", "السداد"], diff.Where(d => d.Kind == DiffKind.Added).Select(d => d.Text));
    }
}

public class SectionReviserTests
{
    private static readonly DocumentTypeDefinition Requete = Fixtures.Requete;
    private const string Current = "حيث إن المادة 106 من القانون المدني تجعل العقد شريعة المتعاقدين.";

    [Fact]
    public void PromptCarriesTheTextTheInstructionAndTheScopeRule()
    {
        var request = new SectionReviser(new StubModelProvider()).BuildRequest(Requete, "grounds", Current, "أوجز", Fixtures.CompleteInputs());

        Assert.Contains(Current, request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("أوجز", request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("المناقشة القانونية", request.UserPrompt, StringComparison.Ordinal);
        Assert.Contains(SectionReviser.RefusalMarker, request.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProposalIsCheckedAndStreamed()
    {
        var provider = new RecordingProvider(_ => "حيث إن المادتان 106 و999 من القانون المدني توجبان تنفيذ العقد.");
        var partials = new List<string>();

        var proposal = await new SectionReviser(provider).ReviseAsync(Requete, "grounds", Current, "أكثر رسمية", Fixtures.CompleteInputs(), partials.Add);

        Assert.False(proposal.IsRefusal);
        Assert.False(proposal.IsUnusable);
        Assert.StartsWith("حيث إن المادتين", proposal.Text, StringComparison.Ordinal); // corrections applied
        Assert.Contains(proposal.Issues, i => i.Kind == NarrativeIssueKind.UncitedArticle && i.Detail == "999");
        Assert.NotEmpty(partials);
    }

    [Fact]
    public async Task NumbersTheLawyerAskedForAreNotFlagged()
    {
        var provider = new RecordingProvider(_ => "حيث إن المادة 106 والمادة 107 من القانون المدني تجعلان العقد شريعة المتعاقدين.");
        var proposal = await new SectionReviser(provider).ReviseAsync(Requete, "grounds", Current, "أضف المادة 107", Fixtures.CompleteInputs());
        Assert.DoesNotContain(proposal.Issues, i => i.Detail == "107");
    }

    [Fact]
    public async Task LegalQuestionsAreRefused()
    {
        var provider = new RecordingProvider(_ => SectionReviser.RefusalMarker);
        var proposal = await new SectionReviser(provider).ReviseAsync(Requete, "grounds", Current, "ما هو أجل التقادم في الديون؟", Fixtures.CompleteInputs());
        Assert.True(proposal.IsRefusal);
    }

    [Fact]
    public async Task CorruptedAnswersAreMarkedUnusableAfterOneRetry()
    {
        var provider = new RecordingProvider(_ => "受理 受理");
        var proposal = await new SectionReviser(provider).ReviseAsync(Requete, "grounds", Current, "أوجز", Fixtures.CompleteInputs());
        Assert.True(proposal.IsUnusable);
        Assert.Equal(2, provider.Requests.Count);
    }
}

public class KeepAndRedraftTests
{
    [Fact]
    public async Task EditedSectionsAreKeptAndNotSentToTheModel()
    {
        var provider = new RecordingProvider(r => r.SourceText);
        var draft = await new DraftingService(provider).DraftAsync(
            Fixtures.Requete, Fixtures.CompleteInputs(), new DraftOptions { KeepNarrative = new Dictionary<string, string> { ["grounds"] = "نص المحامي" } });

        Assert.Equal("نص المحامي", draft.FindSection("grounds")!.Text);
        Assert.Empty(provider.Requests); // grounds kept; requests are never sent to the model
    }

    [Fact]
    public async Task OneSectionCanBeRedrafted()
    {
        var provider = new RecordingProvider(r => "حيث إن " + r.SourceText);
        var section = await new DraftingService(provider).DraftSectionAsync(Fixtures.Requete, "grounds", Fixtures.CompleteInputs());
        Assert.Equal("حيث إن المادة 106 من القانون المدني.", section.Text);
        Assert.Single(provider.Requests);
    }
}

public class OllamaStreamingTests
{
    [Fact]
    public async Task StreamsNdjsonPieces()
    {
        var ndjson = string.Join("\n",
            """{"message":{"role":"assistant","content":"حيث "},"done":false}""",
            """{"message":{"role":"assistant","content":"إن"},"done":false}""",
            """{"message":{"role":"assistant","content":""},"done":true}""");
        using var provider = new OllamaModelProvider(new OllamaOptions(), new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ndjson, Encoding.UTF8) }));

        var pieces = new List<string>();
        await foreach (var piece in provider.StreamAsync(new ModelRequest("grounds", "s", "u", "n"), CancellationToken.None))
        {
            pieces.Add(piece);
        }

        Assert.Equal(["حيث ", "إن"], pieces);
    }

    [Theory]
    [InlineData("""{"message":{"role":"assistant","content":"حيث إن"},"done":true,"done_reason":"length"}""")]
    [InlineData("""{"message":{"role":"assistant","content":"حيث إن"},"done":false}""")] // the stream ends without its final record
    public async Task AnAnswerCutOffOrUnfinishedIsNotTakenAsComplete(string ndjson)
    {
        using var provider = new OllamaModelProvider(new OllamaOptions(), new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ndjson, Encoding.UTF8) }));

        await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(new ModelRequest("grounds", "s", "u", "n"), CancellationToken.None));
    }

    [Fact]
    public async Task ListsInstalledModels()
    {
        using var provider = new OllamaModelProvider(new OllamaOptions(), new Handler(r => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(r.RequestUri!.AbsolutePath.EndsWith("/api/tags", StringComparison.Ordinal) ? """{"models":[{"name":"gemma3:4b"}]}""" : "{}"),
        }));

        Assert.Equal(["gemma3:4b"], await provider.GetInstalledModelsAsync(CancellationToken.None));
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
