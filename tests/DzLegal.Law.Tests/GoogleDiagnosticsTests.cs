using System.Net;
using System.Text;
using DzLegal.Harness;
using DzLegal.ModelProviders.Hosted;

namespace DzLegal.Law.Tests;

public sealed class GoogleDiagnosticsTests
{
    private const string Models = """
        {"models":[
          {"name":"models/gemini-2.5-flash","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemma-4-26b-a4b-it","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemma-4-e4b-it","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemma-4-e2b-it","supportedGenerationMethods":["generateContent"]}
        ]}
        """;

    private static GoogleAiOptions Options(string model) => new() { ApiKey = "secret-google-key", Model = model };

    private static Handler Google(HttpStatusCode generateStatus) => new(request => request.Method == HttpMethod.Get
        ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Models) }
        : generateStatus == HttpStatusCode.OK
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"نعم\"}]}}]}\n\n", Encoding.UTF8, "text/event-stream") }
            : new HttpResponseMessage(generateStatus) { Content = new StringContent("""{"error":{"message":"models/gemma-3-4b-it is not found for API version v1beta"}}""") });

    [Fact]
    public async Task ListsTheGemmaModelsAndConfirmsTheModelAnswers()
    {
        var output = new StringWriter();
        await GoogleDiagnostics.RunAsync(Options("gemma-4-e4b-it"), output, Google(HttpStatusCode.OK));

        var text = output.ToString();
        Assert.Contains("gemma-4-e4b-it  ← under test", text, StringComparison.Ordinal);
        Assert.Contains("gemma-4-e2b-it  (laptop size)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("gemini", text, StringComparison.Ordinal);
        Assert.Contains("Verdict: available. Run: --models google:gemma-4-e4b-it", text, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-google-key", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AModelTheKeyCannotUseSaysSoAndNamesTheAlternatives()
    {
        var output = new StringWriter();
        await GoogleDiagnostics.RunAsync(Options("gemma-3-4b-it"), output, Google(HttpStatusCode.NotFound));

        Assert.Contains("Verdict: gemma-3-4b-it is not served to this key. Gemma models it can use: gemma-4-26b-a4b-it, gemma-4-e2b-it, gemma-4-e4b-it.", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheGoogleSpecDefaultsToGemma4E4B()
    {
        Assert.Equal("gemma-4-e4b-it", HarnessModels.GoogleModel("google"));
        Assert.Equal("gemma-4-26b-a4b-it", HarnessModels.GoogleModel("google:gemma-4-26b-a4b-it"));
        Assert.True(HarnessModels.IsHosted("google"));
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
