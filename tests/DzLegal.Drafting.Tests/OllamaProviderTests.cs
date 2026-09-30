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
