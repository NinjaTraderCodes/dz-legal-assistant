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

    [Theory]
    [InlineData("gpt-oss:120b-cloud", true)]
    [InlineData("deepseek-v3.1:671b-cloud", true)]
    [InlineData("qwen3-coder:cloud", true)]
    [InlineData("gemma3:4b", false)]
    [InlineData("qwen2.5:7b-instruct", false)]
    public void OllamaCloudModelsAreRefusedBecauseTheyForwardPromptsToOllamaCom(string model, bool cloud)
    {
        Assert.Equal(cloud, LoopbackGuard.IsCloudModel(model));
        if (cloud)
        {
            Assert.Throws<ArgumentException>(() => new OllamaModelProvider(new OllamaOptions { Model = model }));
        }
    }

    [Fact]
    public async Task ARedirectIsNeverFollowedOffTheMachine()
    {
        // A local server that answers every request with a redirect to another host: the case text must not follow it.
        using var server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        server.Start();
        var port = ((System.Net.IPEndPoint)server.LocalEndpoint).Port;
        var serving = Task.Run(async () =>
        {
            using var client = await server.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            var buffer = new byte[8192];
            _ = await stream.ReadAsync(buffer);
            var reply = "HTTP/1.1 307 Temporary Redirect\r\nLocation: http://192.0.2.1/exfil\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray();
            await stream.WriteAsync(reply);
        });
        using var provider = new OllamaModelProvider(new OllamaOptions { Endpoint = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(5) });

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(new ModelRequest("facts", "s", "SECRET-CASE-FACTS", "n"), CancellationToken.None));
        Assert.Contains("307", ex.Message, StringComparison.Ordinal);
        await serving;
    }

    [Theory]
    [InlineData("192.0.2.1")]
    [InlineData("example.com")]
    public async Task TheHandlerConnectsToLoopbackOnlyWhateverTheRequestNames(string host)
    {
        using var http = new HttpClient(LoopbackGuard.CreateHandler()) { Timeout = TimeSpan.FromSeconds(5) };
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync(new Uri($"http://{host}/")));
        Assert.Contains("loopback", ex.ToString(), StringComparison.Ordinal);
    }
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
        Assert.Equal(1500, body.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("think", out _)); // unset for non-reasoning models
    }

    [Fact]
    public async Task ThinkCanBeDisabledForReasoningModels()
    {
        var handler = new FakeHandler(_ => Json("""{ "message": { "role": "assistant", "content": "x" }, "done": true }"""));
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
        Assert.Equal("15m", body.RootElement.GetProperty("keep_alive").GetString());
        Assert.False(body.RootElement.TryGetProperty("prompt", out _)); // nothing to generate
        // The same context window as the real requests, or Ollama would load the model a second time.
        Assert.Equal(4096, body.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
        Assert.Equal(0, body.RootElement.GetProperty("options").GetProperty("num_gpu").GetInt32()); // CPU only, as every request: a mismatch would reload the model
    }

    [Fact]
    public async Task UnloadFreesTheModelsMemoryNow()
    {
        var handler = new FakeHandler(_ => Json("""{ "model": "gemma3:4b", "done": true, "done_reason": "unload" }"""));
        using var provider = new OllamaModelProvider(new OllamaOptions { Model = "gemma3:4b" }, handler);

        await provider.UnloadAsync(CancellationToken.None);

        var sent = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("http://127.0.0.1:11434/api/generate"), sent.Uri);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.Equal("gemma3:4b", body.RootElement.GetProperty("model").GetString());
        Assert.Equal(0, body.RootElement.GetProperty("keep_alive").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("prompt", out _));
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
            Assert.Equal(0, body.RootElement.GetProperty("options").GetProperty("num_gpu").GetInt32()); // no graphics chip: one tested path
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
