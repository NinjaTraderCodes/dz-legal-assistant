using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.Drafting;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class ScoredStatusReaderTests
{
    private static JsonObject Response(int cached = 0) => JsonSerializer.SerializeToNode(new
    {
        done = true, eval_count = 1, message = new { content = "0" },
        prompt_eval_count = 100, prompt_eval_cached_count = cached, prompt_eval_duration = 10, eval_duration = 20,
        logprobs = new[] { new { token = "0", bytes = new[] { 48 }, top_logprobs = new[]
        {
            new { token = "1", bytes = new[] { 49 }, logprob = -1.0 },
            new { token = "2", bytes = new[] { 50 }, logprob = -2.0 },
            new { token = "3", bytes = new[] { 51 }, logprob = -3.0 },
            new { token = "4", bytes = new[] { 52 }, logprob = -4.0 },
        } } },
    })!.AsObject();

    private static ScoredStatusQuestion Parse(JsonObject response, bool extension = false)
    {
        using var document = JsonDocument.Parse(response.ToJsonString());
        return ScoredStatusReader.ParseResponse(document.RootElement, "notice_served", extension, .1);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("bytes")]
    [InlineData("whitespace")]
    [InlineData("null")]
    [InlineData("nonfinite")]
    public void APartialOrAmbiguousClassSetNeverBecomesAnAbsence(string fault)
    {
        var response = Response();
        var alternatives = response["logprobs"]![0]!["top_logprobs"]!.AsArray();
        switch (fault)
        {
            case "missing": alternatives.RemoveAt(3); break;
            case "duplicate": alternatives.Add(alternatives[0]!.DeepClone()); break;
            case "bytes": alternatives[3]!["bytes"]![0] = 99; break;
            case "whitespace": alternatives[3]!["token"] = " 4"; break;
            case "nonfinite": alternatives[3]!["logprob"] = JsonNode.Parse("1e999"); break;
            default: alternatives[3]!["logprob"] = null; break;
        }

        Assert.Throws<InvalidDataException>(() => Parse(response));
    }

    [Fact]
    public void ExtensionsRequireReuseAndEveryCarrierIsOneActualZeroToken()
    {
        Assert.Equal([-1d, -2d, -3d, -4d], Parse(Response(90), true).Scores);
        Assert.Throws<InvalidDataException>(() => Parse(Response(), true));
        var response = Response(90);
        response["message"]!["content"] = "1";
        Assert.Throws<InvalidDataException>(() => Parse(response, true));
        response = Response();
        response["prompt_eval_count"] = 4096;
        Assert.Throws<InvalidDataException>(() => Parse(response));
    }

    [Fact]
    public async Task HistoryExtendsExactlyWithActualZerosAndRetainsEverySourceField()
    {
        var requests = new List<JsonNode>();
        using var client = new HttpClient(new Handler(async request =>
        {
            requests.Add(JsonNode.Parse(await request.Content!.ReadAsStringAsync())!);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Response(requests.Count == 1 ? 0 : 90).ToJsonString()) };
        })) { BaseAddress = new Uri("http://127.0.0.1:11434/") };
        var reader = new ScoredStatusReader(client, "gemma4:e4b", "{{ claim }}\n{{ case }}");
        var text = CaseText.From(new Dictionary<string, string> { ["notes.facts"] = "وقائع افتراضية.", ["documents.list"] = "مستند افتراضي." });
        var claim = new ClaimElements { Claim = "test", Elements = [new() { Event = "money_lent", Label = "أقرض" }, new() { Event = "notice_served", Label = "أعذر" }] };
        var chain = await reader.ReadAsync(text, claim, false);
        Assert.Equal(2, chain.Questions.Count);
        var first = requests[0]["messages"]!.AsArray();
        var second = requests[1]["messages"]!.AsArray();
        Assert.Equal(first.ToJsonString(), new JsonArray(second.Take(first.Count).Select(n => n!.DeepClone()).ToArray()).ToJsonString());
        Assert.Equal("0", second[2]!["content"]!.GetValue<string>());
        Assert.Equal("assistant", second[2]!["role"]!.GetValue<string>());
        Assert.Contains(text.Render(), first[1]!["content"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.False(requests[0]["think"]!.GetValue<bool>());
        Assert.Equal(0, requests[0]["format"]!["enum"]![0]!.GetValue<int>());
        Assert.Equal(1, requests[0]["options"]!["num_predict"]!.GetValue<int>());
    }

    [Fact]
    public async Task AnUnavailableClassStopsBeforeTheNextQuestionWithoutRetry()
    {
        var calls = 0;
        using var client = new HttpClient(new Handler(_ =>
        {
            calls++;
            var response = Response();
            response["logprobs"]![0]!["top_logprobs"]!.AsArray().RemoveAt(3);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response.ToJsonString()) });
        })) { BaseAddress = new Uri("http://127.0.0.1:11434/") };
        var reader = new ScoredStatusReader(client, "gemma4:e4b", "{{ claim }} {{ case }}");
        var claim = new ClaimElements { Claim = "test", Elements = [new() { Event = "money_lent", Label = "أقرض" }, new() { Event = "notice_served", Label = "أعذر" }] };
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadAsync(CaseText.From(new Dictionary<string, string>()), claim, false));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void StatusAblationKeepsBindingsAndRequestsAndDoesNotInventRecoveredValues()
    {
        var value = new FactValue("100", "100", ValueCheck.InCitedSentence);
        var baseline = new CaseFacts { Relation = "loan", Position = "lender",
            Events = [new("money_lent", null, value, "plaintiff", FactPolarity.Affirmed, ["s1"])],
            Requests = [new("payment", value, ["s2"])], Rejected = ["kept"] };
        var claim = new ClaimElements { Claim = "test", Elements = [new() { Event = "money_lent", Label = "أقرض" }, new() { Event = "notice_served", Label = "أعذر", Actor = "lender" }] };
        var facts = ScoredStatusReader.Apply(baseline, claim, new Dictionary<string, int> { ["money_lent"] = 3, ["notice_served"] = 2 });
        Assert.Equal(value, facts.Events[0].Amount);
        Assert.Equal(FactPolarity.Disputed, facts.Events[0].Polarity);
        Assert.Null(facts.Events[1].Date);
        Assert.Empty(facts.Events[1].Sentences);
        Assert.Equal("plaintiff", facts.Events[1].Actor);
        Assert.Equal(baseline.Requests, facts.Requests);
        Assert.Equal(baseline.Rejected, facts.Rejected);
        Assert.Throws<ArgumentException>(() => ScoredStatusReader.Apply(baseline, claim, new Dictionary<string, int>()));
        Assert.Empty(ScoredStatusReader.Apply(baseline, claim, new Dictionary<string, int> { ["money_lent"] = 4, ["notice_served"] = 4 }).Events);
    }

    [Fact]
    public void ChoicesUseNumericTieOrderAndRejectNonfiniteScores()
    {
        Assert.Equal(1, ScoredStatusReader.Choose([0d, 0d, 0d, 0d], [0d, 0d, 0d, 0d]));
        Assert.Equal(3, ScoredStatusReader.Choose([0d, 0d, 0d, 0d], [0d, 0d, 1d, 0d]));
        Assert.Throws<ArgumentException>(() => ScoredStatusReader.Choose([0d, double.NegativeInfinity, 0d, 0d], [0d, 0d, 0d, 0d]));
    }

    [Fact]
    public void RemoteEndpointsAndUndeclaredModelsAreRefusedBeforeAnyRequest()
    {
        using var remote = new HttpClient { BaseAddress = new Uri("https://example.com/") };
        Assert.Throws<ArgumentException>(() => new ScoredStatusReader(remote, "gemma3:4b", "{{ case }} {{ claim }}"));
        using var local = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:11434/") };
        Assert.Throws<ArgumentException>(() => new ScoredStatusReader(local, "other-cloud", "{{ case }} {{ claim }}"));
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
