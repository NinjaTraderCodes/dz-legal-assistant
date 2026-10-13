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
    [InlineData(HttpStatusCode.NotFound, true)] // a retired model: the next engine may still be served
    [InlineData(HttpStatusCode.Unauthorized, true)] // this service's key; the next service has its own
    [InlineData(HttpStatusCode.PaymentRequired, true)] // credit used up
    [InlineData(HttpStatusCode.BadRequest, false)]
    public async Task ARateLimitIsTransientABadKeyIsNot(HttpStatusCode status, bool transient)
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(status) { Content = new StringContent("""{"error":{"status":"RESOURCE_EXHAUSTED"}}""") });
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal(transient, ex.IsTransient);
    }

    [Fact]
    public async Task Gemma4IsAskedNotToThinkFirstOtherModelsGetNoThinkingSetting()
    {
        var handler = new FakeHandler(_ => Sse("""{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}"""));
        using (var gemma4 = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k", Model = "gemma-4-26b-a4b-it" }, handler))
        {
            await gemma4.GenerateAsync(Request, CancellationToken.None);
        }

        using (var gemma3 = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler))
        {
            await gemma3.GenerateAsync(Request, CancellationToken.None);
        }

        using var first = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.Equal("minimal", first.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString());
        using var second = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.False(second.RootElement.GetProperty("generationConfig").TryGetProperty("thinkingConfig", out _));
    }

    [Fact]
    public async Task AModelThatRefusesTheThinkingSettingIsAskedAgainWithoutIt()
    {
        var handler = new FakeHandler(r => r.Content!.ReadAsStringAsync().Result.Contains("thinkingConfig", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("""{"error":{"message":"Thinking level is not supported for this model."}}""") }
            : Sse("""{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k", Model = "gemma-4-31b-it" }, handler);

        Assert.Equal("ok", await provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task AnAnswerCutOffWhileThinkingSaysSo()
    {
        var handler = new FakeHandler(_ => Sse("""{"candidates":[{"content":{"parts":[{"text":"…","thought":true}]},"finishReason":"MAX_TOKENS"}]}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k", Model = "gemma-4-31b-it" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Contains("length limit", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnEmptyAnswerIsAnError()
    {
        var handler = new FakeHandler(_ => Sse("""{"candidates":[{"content":{"parts":[{"text":" "}]},"finishReason":"STOP"}]}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(async () => await provider.StreamAsync(Request, CancellationToken.None).ToListAsync());
        Assert.Contains("empty answer (finish reason: STOP)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAnswerCutOffAfterSomeTextIsNotTakenAsComplete()
    {
        // Audit A-03: plausible but unfinished legal text used to pass as a full answer.
        var handler = new FakeHandler(_ => Sse("""{"candidates":[{"content":{"parts":[{"text":"حيث إن المدعى عليه"}]},"finishReason":"MAX_TOKENS"}]}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Contains("cut off", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAnswerThatGoesSilentIsReportedAndCanBeHandedOver()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallingStream()) });
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k", IdleTimeout = TimeSpan.FromMilliseconds(200) }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.True(ex.IsTransient);
        Assert.Contains("silent", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADroppedConnectionIsAModelErrorNotACrash()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new BreakingStream()) });
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.True(ex.IsTransient);
    }

    [Fact]
    public async Task AnUnreadableEventIsAModelError()
    {
        var handler = new FakeHandler(_ => Sse("{not json"));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
    }

    [Fact]
    public async Task ThoughtsAreNotPartOfTheAnswer()
    {
        var handler = new FakeHandler(_ => Sse("""{"candidates":[{"content":{"parts":[{"text":"planning…","thought":true},{"text":"حيث إن"}]}}]}"""));
        using var provider = new GoogleAiModelProvider(new GoogleAiOptions { ApiKey = "k" }, handler);

        Assert.Equal("حيث إن", await provider.GenerateAsync(Request, CancellationToken.None));
    }

    [Fact]
    public async Task ListsTheModelsTheKeyCanGenerateWithAcrossPages()
    {
        var handler = new FakeHandler(r => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(r.RequestUri!.Query.Contains("pageToken=p2", StringComparison.Ordinal)
                ? """{"models":[{"name":"models/gemma-4-31b-it","supportedGenerationMethods":["generateContent","countTokens"]}]}"""
                : """{"models":[{"name":"models/gemma-4-26b-a4b-it","supportedGenerationMethods":["generateContent"]},{"name":"models/text-embedding-004","supportedGenerationMethods":["embedContent"]}],"nextPageToken":"p2"}"""),
        });

        var models = await GoogleAiModelProvider.ListModelsAsync(new GoogleAiOptions { ApiKey = "secret-key" }, handler, CancellationToken.None);

        Assert.Equal(["gemma-4-26b-a4b-it", "gemma-4-31b-it"], models);
        Assert.All(handler.Requests, r => Assert.Equal("secret-key", r.Headers["x-goog-api-key"]));
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
    public async Task AnAnswerCutOffAtTheLengthLimitIsNotTakenAsComplete()
    {
        var handler = new FakeHandler(_ => GoogleAiProviderTests.Sse("""{"choices":[{"delta":{"content":"حيث إن"},"finish_reason":"length"}]}"""));
        using var provider = new OpenAiCompatibleModelProvider(
            new OpenAiCompatibleOptions { Label = "OpenRouter", Endpoint = new Uri("https://openrouter.ai/api/v1/"), ApiKey = "k", Model = "google/gemma-3-4b-it" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(new ModelRequest("grounds", "s", "u", "n"), CancellationToken.None));
        Assert.Contains("cut off", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnEmptyAnswerIsAnErrorThatSaysWhyTheModelStopped()
    {
        // Gemma 3 4B via OpenRouter answered the argued grounds with nothing at all, which read as «(...)» everywhere.
        var handler = new FakeHandler(_ => GoogleAiProviderTests.Sse(
            """{"provider":"DeepInfra","choices":[{"delta":{"role":"assistant","content":""}}]}""",
            """{"provider":"DeepInfra","choices":[{"delta":{"content":"\n"},"finish_reason":"stop"}],"usage":{"completion_tokens":1}}"""));
        using var provider = new OpenAiCompatibleModelProvider(
            new OpenAiCompatibleOptions { Label = "OpenRouter", Endpoint = new Uri("https://openrouter.ai/api/v1/"), ApiKey = "k", Model = "google/gemma-3-4b-it" }, handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(async () => await provider.StreamAsync(new ModelRequest("grounds-argued", "s", "u", "n"), CancellationToken.None).ToListAsync());
        Assert.Contains("empty answer (finish reason: stop, answer tokens: 1, served by DeepInfra)", ex.Message, StringComparison.Ordinal);
    }

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

    [Fact]
    public async Task AnErrorInsideASuccessfulStreamLetsTheNextEngineTry()
    {
        // OpenRouter answers 200, then reports the error (e.g. credit used up) as an event.
        var handler = new FakeHandler(_ => GoogleAiProviderTests.Sse("""{"error":{"code":402,"message":"Insufficient credits"}}"""));
        using var provider = new OpenAiCompatibleModelProvider(
            new OpenAiCompatibleOptions { Label = "OpenRouter", Endpoint = new Uri("https://openrouter.ai/api/v1/"), ApiKey = "k", Model = "google/gemma-3-4b-it" },
            handler);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.True(ex.IsTransient);
        Assert.Contains("Insufficient credits", ex.Message, StringComparison.Ordinal);
    }
}

public class RateLimitTests
{
    private static readonly ModelRequest Request = new("requests", "system rules", "user prompt", "notes");

    private const string UpstreamLimited =
        """{"error":{"message":"Provider returned error","code":429,"metadata":{"raw":"google/gemma-3-4b-it is temporarily rate-limited upstream.","provider_name":"DeepInfra"}}}""";

    private static OpenAiCompatibleModelProvider Provider(FakeHandler handler, RateLimitPolicy policy) =>
        new(new OpenAiCompatibleOptions { Label = "OpenRouter", Endpoint = new Uri("https://openrouter.ai/api/v1/"), ApiKey = "k", Model = "google/gemma-3-4b-it", RateLimit = policy }, handler);

    private static HttpResponseMessage Limited(TimeSpan? retryAfter = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent(UpstreamLimited) };
        if (retryAfter is { } delay)
        {
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(delay);
        }

        return response;
    }

    [Fact]
    public async Task AProviderPreferenceIsSentOnlyWhenSet()
    {
        var handler = new FakeHandler(_ => GoogleAiProviderTests.Sse("""{"choices":[{"delta":{"content":"ok"}}]}"""));
        using (var preferring = new OpenAiCompatibleModelProvider(new OpenAiCompatibleOptions { Label = "OpenRouter", Endpoint = new Uri("https://openrouter.ai/api/v1/"), ApiKey = "k", Model = "google/gemma-3-4b-it", ProviderSort = "throughput" }, handler))
        {
            await preferring.GenerateAsync(Request, CancellationToken.None);
        }

        using (var plain = Provider(handler, RateLimitPolicy.None))
        {
            await plain.GenerateAsync(Request, CancellationToken.None);
        }

        using var first = System.Text.Json.JsonDocument.Parse(handler.Requests[0].Body);
        Assert.Equal("throughput", first.RootElement.GetProperty("provider").GetProperty("sort").GetString());
        Assert.True(first.RootElement.GetProperty("provider").GetProperty("allow_fallbacks").GetBoolean());
        using var second = System.Text.Json.JsonDocument.Parse(handler.Requests[1].Body);
        Assert.False(second.RootElement.TryGetProperty("provider", out _)); // the web demo's request is unchanged
    }

    [Fact]
    public async Task ARateLimitIsWaitedOutAndTheRequestSentAgain()
    {
        var calls = 0;
        var waits = new List<(TimeSpan Delay, int Attempt, string Reason)>();
        var handler = new FakeHandler(_ => ++calls == 1 ? Limited(TimeSpan.Zero) : GoogleAiProviderTests.Sse("""{"choices":[{"delta":{"content":"ok"}}]}"""));
        using var provider = Provider(handler, new RateLimitPolicy(TimeSpan.FromMinutes(1), (d, a, r) => waits.Add((d, a, r))));

        Assert.Equal("ok", await provider.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(handler.Requests[0].Body, handler.Requests[1].Body); // the same request, rebuilt
        Assert.Equal([(TimeSpan.Zero, 2, "google/gemma-3-4b-it is temporarily rate-limited upstream.")], waits);
    }

    [Fact]
    public async Task BeyondTheBudgetItSaysRateLimitedInPlainWordsWithoutTheRawJson()
    {
        var handler = new FakeHandler(_ => Limited());
        using var provider = Provider(handler, new RateLimitPolicy(TimeSpan.FromMilliseconds(30)));

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.True(ex.IsRateLimited);
        Assert.True(ex.IsTransient);
        Assert.Equal(2, handler.Requests.Count); // waited the remaining 30 ms once, then gave up
        Assert.Contains("rate-limited by its provider", ex.Message, StringComparison.Ordinal);
        Assert.Contains("temporarily rate-limited upstream", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("{", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutAPolicyARateLimitFailsAtOnceSoTheWebFallbackMovesOn()
    {
        var handler = new FakeHandler(_ => Limited(TimeSpan.Zero));
        using var provider = Provider(handler, RateLimitPolicy.None);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.True(ex.IsRateLimited);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ARefusedRequestIsNeverRetriedAndShowsTheServicesMessage()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("""{"error":{"message":"max_tokens is too large"}}""") });
        using var provider = Provider(handler, new RateLimitPolicy(TimeSpan.FromMinutes(1)));

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.False(ex.IsRateLimited);
        Assert.Single(handler.Requests);
        Assert.EndsWith("error 400: max_tokens is too large", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellingDuringTheWaitStopsAtOnce()
    {
        var handler = new FakeHandler(_ => Limited(TimeSpan.FromMinutes(1)));
        using var provider = Provider(handler, new RateLimitPolicy(TimeSpan.FromMinutes(5)));
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var clock = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GenerateAsync(Request, cancel.Token));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task ARateLimitReportedInsideTheStreamIsRecognised()
    {
        var handler = new FakeHandler(_ => GoogleAiProviderTests.Sse(UpstreamLimited));
        using var provider = Provider(handler, RateLimitPolicy.None);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));
        Assert.True(ex.IsRateLimited);
        Assert.Contains("temporarily rate-limited upstream", ex.Message, StringComparison.Ordinal);
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

/// <summary>Never sends anything after the headers.</summary>
internal sealed class StallingStream : MemoryStream
{
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return 0;
    }
}

/// <summary>The connection drops after the headers.</summary>
internal sealed class BreakingStream : MemoryStream
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        ValueTask.FromException<int>(new IOException("connection reset by peer"));
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
