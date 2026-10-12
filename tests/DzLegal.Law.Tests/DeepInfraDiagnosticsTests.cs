using System.Net;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class DeepInfraDiagnosticsTests
{
    private const string Models = """{"data":[{"id":"google/gemma-3-4b-it"},{"id":"google/gemma-3-12b-it"},{"id":"meta-llama/Llama-3.3-70B-Instruct"}]}""";

    private static Handler DeepInfra(HttpStatusCode chat, string body) => new(request => request.Method == HttpMethod.Get
        ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Models) }
        : new HttpResponseMessage(chat) { Content = new StringContent(body) });

    private static async Task<string> Ping(string model, Handler handler)
    {
        var output = new StringWriter();
        await OpenRouterDiagnostics.RunAsync(new Uri("https://api.deepinfra.com/v1/openai/"), "secret-deepinfra-key", model, output, "DeepInfra", handler);
        return output.ToString();
    }

    [Fact]
    public async Task ListsTheGemmaModelsAndSaysTheModelIsAvailable()
    {
        var text = await Ping("google/gemma-3-4b-it", DeepInfra(HttpStatusCode.OK, """{"choices":[{"message":{"content":"نعم"}}]}"""));

        Assert.Contains("google/gemma-3-4b-it  ← under test", text, StringComparison.Ordinal);
        Assert.Contains("google/gemma-3-12b-it", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Llama", text, StringComparison.Ordinal);
        Assert.Contains("Verdict: available. Run: --models deepinfra:google/gemma-3-4b-it", text, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-deepinfra-key", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAccountWithoutCreditSaysSo() =>
        Assert.Contains("asks for credit or a payment method",
            await Ping("google/gemma-3-4b-it", DeepInfra(HttpStatusCode.PaymentRequired, """{"error":{"message":"Insufficient balance"}}""")),
            StringComparison.Ordinal);

    [Fact]
    public async Task AWrongModelNamePointsToTheListedIds() =>
        Assert.Contains("is not served under this name",
            await Ping("google/gemma-4-e4b-it", DeepInfra(HttpStatusCode.NotFound, """{"error":{"message":"Model not found"}}""")),
            StringComparison.Ordinal);

    [Fact]
    public void TheDeepInfraSpecDefaultsToGemma3()
    {
        Assert.Equal("google/gemma-3-4b-it", HarnessModels.DeepInfraModel("deepinfra"));
        Assert.Equal("google/gemma-3-12b-it", HarnessModels.DeepInfraModel("deepinfra:google/gemma-3-12b-it"));
        Assert.True(HarnessModels.IsHosted("deepinfra"));
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
