using System.Net;
using System.Text;
using System.Text.Json;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.Drafting.Tests;

public class LoopbackGuardTests
{
    [Theory]
    [InlineData("http://127.0.0.1:11434")]
    [InlineData("http://127.5.6.7:8080")]
    [InlineData("http://localhost:11434")]
    [InlineData("http://[::1]:11434")]
    [InlineData("https://127.0.0.1")]
    public void AcceptsOnDeviceEndpoints(string url) => Assert.True(LoopbackGuard.IsLoopback(new Uri(url)));

    [Theory]
    [InlineData("http://10.0.0.5:11434")]
    [InlineData("http://192.168.1.10:11434")]
    [InlineData("http://0.0.0.0:11434")]
    [InlineData("https://api.example.com")]
    [InlineData("http://localhost.example.com")]
    [InlineData("http://127.0.0.1.nip.io")]
    [InlineData("http://user:pass@127.0.0.1")]
    [InlineData("ftp://127.0.0.1")]
    public void RefusesAnythingElse(string url) => Assert.False(LoopbackGuard.IsLoopback(new Uri(url)));

    [Fact]
    public void ProviderCannotBeConstructedWithARemoteEndpoint() =>
        Assert.Throws<ArgumentException>(() => new OllamaModelProvider(new OllamaOptions { Endpoint = new Uri("https://ollama.example.com") }));
}

public class OllamaProviderTests
{
    private static readonly ModelRequest Request = new("facts", "system rules", "user prompt", "notes");

    [Fact]
    public async Task SendsAChatRequestAndReturnsTheContent()
    {
        var handler = new FakeHandler(_ => Json("""{ "message": { "role": "assistant", "content": "حيث إن" }, "done": true }"""));
        using var provider = new OllamaModelProvider(new OllamaOptions { Model = "qwen2.5:7b-instruct" }, handler);

        var result = await provider.GenerateAsync(Request, CancellationToken.None);

        Assert.Equal("حيث إن", result);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("http://127.0.0.1:11434/api/chat"), sent.Uri);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal("qwen2.5:7b-instruct", body.RootElement.GetProperty("model").GetString());
        Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
        var messages = body.RootElement.GetProperty("messages");
        Assert.Equal("system rules", messages[0].GetProperty("content").GetString());
        Assert.Equal("user prompt", messages[1].GetProperty("content").GetString());
        Assert.Equal(1000, body.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("think", out _)); // unset for non-reasoning models
    }

    [Fact]
    public async Task ThinkCanBeDisabledForReasoningModels()
    {
        var handler = new FakeHandler(_ => Json("""{ "message": { "role": "assistant", "content": "x" } }"""));
        using var provider = new OllamaModelProvider(new OllamaOptions { Model = "qwen3:4b", Think = false }, handler);

        await provider.GenerateAsync(Request, CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.False(body.RootElement.GetProperty("think").GetBoolean());
    }

    [Fact]
    public async Task MissingModelGivesAPullHint()
    {
        using var provider = new OllamaModelProvider(new OllamaOptions(), new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Contains("ollama pull", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnreachableServerGivesAClearError()
    {
        using var provider = new OllamaModelProvider(new OllamaOptions(), new FakeHandler(_ => throw new HttpRequestException("refused")));
        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Contains("not reachable", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WarmUpLoadsTheModelWithoutGeneratingAnything()
    {
        var handler = new FakeHandler(_ => Json("""{ "model": "gemma3:4b", "done": true }"""));
        using var provider = new OllamaModelProvider(new OllamaOptions { Model = "gemma3:4b" }, handler);

        await provider.WarmUpAsync(CancellationToken.None);

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("http://127.0.0.1:11434/api/generate"), sent.Uri);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal("gemma3:4b", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("60m", body.RootElement.GetProperty("keep_alive").GetString());
        Assert.False(body.RootElement.TryGetProperty("prompt", out _)); // nothing to generate
        // The same context window as the real requests, or Ollama would load the model a second time.
        Assert.Equal(4096, body.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
    }

    [Fact]
    public async Task EveryRequestKeepsTheModelInMemoryAndHonoursItsOwnAnswerCap()
    {
        var handler = new FakeHandler(_ => Json("""{ "message": { "role": "assistant", "content": "x" }, "done": true }"""));
        using var provider = new OllamaModelProvider(new OllamaOptions { KeepAlive = "2h", MaxTokens = 1000 }, handler);

        await provider.GenerateAsync(Request with { MaxTokens = 300 }, CancellationToken.None);
        await provider.GenerateAsync(Request with { MaxTokens = 5000 }, CancellationToken.None);
        await provider.GenerateAsync(Request, CancellationToken.None);

        var caps = handler.Requests.Select(r =>
        {
            using var body = JsonDocument.Parse(r.Body);
            Assert.Equal("2h", body.RootElement.GetProperty("keep_alive").GetString());
            return body.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32();
        }).ToList();
        Assert.Equal([300, 1000, 1000], caps); // never above the configured maximum
    }

    [Fact]
    public async Task TheServersTimingsAreReportedAfterAStreamedAnswer()
    {
        var handler = new FakeHandler(_ => Json(
            """{ "message": { "role": "assistant", "content": "حيث" }, "done": false }""" + "\n" +
            """{ "message": { "role": "assistant", "content": " إن" }, "done": true, "load_duration": 2000000000, "prompt_eval_count": 1200, "prompt_eval_duration": 24000000000, "eval_count": 60, "eval_duration": 10000000000 }"""));
        using var provider = new OllamaModelProvider(new OllamaOptions(), handler);
        ModelTimings? timings = null;
        provider.OnTimings = t => timings = t;

        var text = string.Concat(await provider.StreamAsync(Request with { MaxTokens = 300 }, CancellationToken.None).ToListAsync());

        Assert.Equal("حيث إن", text);
        Assert.NotNull(timings);
        Assert.Equal(TimeSpan.FromSeconds(2), timings.Load);
        Assert.Equal(1200, timings.PromptTokens);
        Assert.Equal(50, timings.PromptTokensPerSecond, 3);
        Assert.Equal(6, timings.AnswerTokensPerSecond, 3);
        Assert.False(timings.PromptMayBeTruncated); // 1200 + 300 fits in 4096
    }

    [Fact]
    public async Task APromptThatFillsTheContextWindowIsFlagged()
    {
        var handler = new FakeHandler(_ => Json("""{ "message": { "role": "assistant", "content": "x" }, "done": true, "prompt_eval_count": 3900, "eval_count": 1 }"""));
        using var provider = new OllamaModelProvider(new OllamaOptions(), handler);
        ModelTimings? timings = null;
        provider.OnTimings = t => timings = t;

        await provider.GenerateAsync(Request with { MaxTokens = 400 }, CancellationToken.None);

        Assert.True(timings!.PromptMayBeTruncated);
    }

    [Fact]
    public async Task AWarmUpWithTheServerDownIsAReadableError()
    {
        using var provider = new OllamaModelProvider(new OllamaOptions(), new FakeHandler(_ => throw new HttpRequestException("refused")));
        await Assert.ThrowsAsync<ModelProviderException>(() => provider.WarmUpAsync(CancellationToken.None));
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(Uri Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.RequestUri!, body));
            return respond(request);
        }
    }
}
