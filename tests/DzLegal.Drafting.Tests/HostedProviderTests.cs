using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DzLegal.ModelProviders.Hosted;

namespace DzLegal.Drafting.Tests;

public class GoogleAiProviderTests
{
    private static readonly ModelRequest Request = new("grounds", "system rules", "user prompt", "notes") { MaxTokens = 400 };

    [Fact]
    public async Task StreamsGemmaWithTheRulesOnTopOfTheUserTurnAndTheKeyInAHeader()
    {
        var handler = new FakeHandler(_ => Sse(
            """{"candidates":[{"content":{"role":"model","parts":[{"text":"حيث "}]}}]}""",
            """{"candidates":[{"content":{"role":"model","parts":[{"text":"إن"}]}}]}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "secret-key" }, handler);

        var pieces = new List<string>();
        await foreach (var piece in provider.StreamAsync(Request, CancellationToken.None))
        {
            pieces.Add(piece);
        }

        Assert.Equal(["حيث ", "إن"], pieces);
        var sent = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("https://generativelanguage.googleapis.com/v1beta/models/gemma-3-4b-it:streamGenerateContent?alt=sse"), sent.Uri);
        Assert.DoesNotContain("secret-key", sent.Uri.ToString(), StringComparison.Ordinal);
        Assert.Equal("secret-key", sent.Headers["x-goog-api-key"]);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.False(body.RootElement.TryGetProperty("systemInstruction", out _)); // Gemma refuses a system instruction
        Assert.Equal("system rules\n\nuser prompt", body.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal(400, body.RootElement.GetProperty("generationConfig").GetProperty("maxOutputTokens").GetInt32());
    }

    [Fact]
    public async Task GeminiModelsCanKeepASeparateSystemInstruction()
    {
        var handler = new FakeHandler(_ => Sse("""{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k", Model = "gemini-2.5-flash", MergeSystemPrompt = false }, handler);

        Assert.Equal("ok", await provider.GenerateAsync(Request, CancellationToken.None));
        using var body = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.Equal("system rules", body.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("user prompt", body.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public async Task ARateLimitIsTransientABadKeyIsNot(HttpStatusCode status, bool transient)
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(status) { Content = new StringContent("""{"error":{"status":"RESOURCE_EXHAUSTED"}}""") });
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal(transient, ex.IsTransient);
    }

    [Fact]
    public async Task ABlockedPromptIsAnError()
    {
        var handler = new FakeHandler(_ => Sse("""{"promptFeedback":{"blockReason":"SAFETY"}}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Contains("SAFETY", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AKeyIsRequired() =>
        Assert.Throws<ArgumentException>(() => new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = " " }));

    internal static HttpResponseMessage Sse(params string[] events) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(string.Concat(events.Select(e => $"data: {e}\n\n")) + "data: [DONE]\n\n", Encoding.UTF8, "text/event-stream"),
    };
}

public class OpenAiCompatibleProviderTests
{
    private static readonly ModelRequest Request = new("requests", "system rules", "user prompt", "notes");

    [Fact]
    public async Task StreamsChatCompletionsWithABearerKey()
    {
        var handler = new FakeHandler(_ => GoogleAiProviderTests.Sse(
            """{"choices":[{"delta":{"role":"assistant"}}]}""",
            """{"choices":[{"delta":{"content":"في "}}]}""",
            """{"choices":[{"delta":{"content":"الشكل"}}]}"""));
        using var provider = new OpenAiCompatibleModelProvider(
            new OpenAiCompatibleOptions { Label = "groq", Endpoint = new Uri("https://api.groq.com/openai/v1/"), ApiKey = "gsk", Model = "llama-3.3-70b-versatile" },
            handler);

        Assert.Equal("في الشكل", await provider.GenerateAsync(Request, CancellationToken.None));
        var sent = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("https://api.groq.com/openai/v1/chat/completions"), sent.Uri);
        Assert.Equal("Bearer gsk", sent.Headers["Authorization"]);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.True(body.RootElement.GetProperty("stream").GetBoolean());
        Assert.Equal("system", body.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("llama-3.3-70b-versatile (groq)", provider.Name);
    }
}

public class FallbackProviderTests
{
    private static readonly ModelRequest Request = new("grounds", "s", "u", "n");

    [Fact]
    public async Task ABusyEngineHandsOverToTheNextOneBeforeWritingAnything()
    {
        var skipped = new List<string>();
        using var chain = new FallbackModelProvider([new Scripted("first", busy: true), new Scripted("second")])
        {
            OnSkipped = (engine, _) => skipped.Add(engine.Name),
        };

        Assert.Equal("answer from second", await Collect(chain));
        Assert.Equal(["first"], skipped);
        Assert.Equal("second", chain.Name);
    }

    [Fact]
    public async Task AnErrorThatARetryCannotFixIsReportedAtOnce()
    {
        using var chain = new FallbackModelProvider([new Scripted("first", broken: true), new Scripted("second")]);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => Collect(chain));
        Assert.False(ex.IsTransient);
    }

    [Fact]
    public async Task AFailureAfterTheFirstWordsIsNotMaskedByAnotherEngine()
    {
        using var chain = new FallbackModelProvider([new Scripted("first", failsMidway: true), new Scripted("second")]);

        await Assert.ThrowsAsync<ModelProviderException>(() => Collect(chain));
    }

    [Fact]
    public async Task WhenEveryEngineIsBusyTheLastErrorIsReported()
    {
        using var chain = new FallbackModelProvider([new Scripted("first", busy: true), new Scripted("second", busy: true)]);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => chain.GenerateAsync(Request, CancellationToken.None));
        Assert.True(ex.IsTransient);
        Assert.Contains("second", ex.Message, StringComparison.Ordinal);
    }

    private static async Task<string> Collect(FallbackModelProvider provider)
    {
        var text = new StringBuilder();
        await foreach (var piece in provider.StreamAsync(Request, CancellationToken.None))
        {
            text.Append(piece);
        }

        return text.ToString();
    }

    private sealed class Scripted(string name, bool busy = false, bool broken = false, bool failsMidway = false) : IModelProvider
    {
        public string Name => name;

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) =>
            busy ? throw new ModelProviderException($"{name} busy") { IsTransient = true }
            : broken ? throw new ModelProviderException($"{name} bad key")
            : Task.FromResult($"answer from {name}");

        public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (busy || broken)
            {
                await GenerateAsync(request, cancellationToken);
            }

            yield return "answer ";
            if (failsMidway)
            {
                throw new ModelProviderException($"{name} dropped") { IsTransient = true };
            }

            yield return $"from {name}";
        }
    }
}

internal sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(Uri Uri, string Body, Dictionary<string, string> Headers)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        Requests.Add((request.RequestUri!, body, headers));
        return respond(request);
    }
}
