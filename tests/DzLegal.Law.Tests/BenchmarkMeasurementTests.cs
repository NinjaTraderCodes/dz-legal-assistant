using System.Net;
using System.Text.Json;
using DzLegal.Drafting;
using DzLegal.Harness;
using DzLegal.Law;
using DzLegal.ModelProviders.Stub;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.Law.Tests;

public sealed class BenchmarkMeasurementTests
{
    [Fact]
    public async Task GroundsCaseBoundariesReloadAtOneElevenAndTwentyOneWithoutChangingScores()
    {
        using var folder = new FixtureFolder();
        using var handler = new Handler();
        await using (var measurement = new BenchmarkMeasurement("fixture", new Uri("http://127.0.0.1:9"), folder.Path,
            TextWriter.Null, handler, () => Task.CompletedTask, _ => Task.CompletedTask))
        {
            var cases = Enumerable.Range(1, 21).Select(i => new GroundsCase { Name = "fixture-" + i, Facts = "ملاحظات خيالية" }).ToList();
            var knowledge = LegalKnowledgeLoader.Load(System.IO.Path.Combine(AppContext.BaseDirectory, "data", "law"));
            var plain = await GroundsEvaluator.EvaluateAsync(knowledge, cases, new StubModelProvider(), TextWriter.Null);
            var guarded = await GroundsEvaluator.EvaluateAsync(knowledge, cases, measurement.Guard(new StubModelProvider()), TextWriter.Null, measurement: measurement);
            Assert.Equal(plain.ClaimExactRate, guarded.ClaimExactRate);
            Assert.Equal(plain.ArticleRecall, guarded.ArticleRecall);
            Assert.Equal(plain.OutOfScopeAbstention, guarded.OutOfScopeAbstention);
            Assert.Equal(21, guarded.Results.Count);
            measurement.Complete();
        }

        Assert.Equal(7, handler.Calls.Count);
        using var loads = JsonDocument.Parse(await File.ReadAllTextAsync(System.IO.Path.Combine(folder.Path, "measurement-loads.json")));
        Assert.Equal([1, 11, 21], loads.RootElement.EnumerateArray().Select(r => r.GetProperty("BeforeOrdinal").GetInt32()));
        using var completion = JsonDocument.Parse(await File.ReadAllTextAsync(System.IO.Path.Combine(folder.Path, "measurement-completion.json")));
        Assert.True(completion.RootElement.GetProperty("Complete").GetBoolean());
    }

    [Fact]
    public async Task GuardFailureBetweenCallsIsPermanentAndDoesNotReloadOrRetry()
    {
        using var folder = new FixtureFolder();
        using var handler = new Handler();
        var provider = new Counter();
        await using var measurement = new BenchmarkMeasurement("fixture", new Uri("http://127.0.0.1:9"), folder.Path,
            TextWriter.Null, handler, () => provider.Calls == 1 ? Task.FromException(new IOException("512 MiB guard")) : Task.CompletedTask, _ => Task.CompletedTask);
        var guarded = measurement.Guard(provider);
        await measurement.BeforeCaseAsync("fixture");
        await Assert.ThrowsAsync<IOException>(() => guarded.GenerateAsync(Request, CancellationToken.None));
        await Assert.ThrowsAsync<IOException>(() => guarded.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal(1, provider.Calls);
        Assert.Equal(2, handler.Calls.Count); // initial unload/warm only; cleanup happens at scope exit
    }

    [Fact]
    public async Task CutoffStillUsesTheExistingAppOutcomeAndStreamPiecesAreUnchanged()
    {
        using var folder = new FixtureFolder();
        using var handler = new Handler();
        await using var measurement = new BenchmarkMeasurement("fixture", new Uri("http://127.0.0.1:9"), folder.Path,
            TextWriter.Null, handler, () => Task.CompletedTask, _ => Task.CompletedTask);
        await measurement.BeforeCaseAsync("fixture");
        var guarded = measurement.Guard(new Counter());
        var pieces = new List<string>();
        await foreach (var piece in guarded.StreamAsync(Request, CancellationToken.None)) { pieces.Add(piece); }
        Assert.Equal(["first", "second"], pieces);
        var cutoff = measurement.Guard(new Cutoff());
        var error = await Assert.ThrowsAsync<ModelProviderException>(() => cutoff.GenerateAsync(Request, CancellationToken.None));
        Assert.True(error.IsCutOff);
        await measurement.AfterCaseAsync("fixture", TimeSpan.Zero, null, new { AppUsedItsExistingFallback = true });
        measurement.Complete();
    }

    private static readonly ModelRequest Request = new("fixture", string.Empty, "fixture", "fixture");

    [Fact]
    public async Task NativeContextWarningStopsBeforeAnotherRequest()
    {
        using var folder = new FixtureFolder();
        using var handler = new Handler();
        using var chat = new ContextHandler();
        using var local = new OllamaModelProvider(new OllamaOptions { Endpoint = new Uri("http://127.0.0.1:9"), Model = "fixture" }, chat);
        await using var measurement = new BenchmarkMeasurement("fixture", new Uri("http://127.0.0.1:9"), folder.Path,
            TextWriter.Null, handler, () => Task.CompletedTask, _ => Task.CompletedTask);
        await measurement.BeforeCaseAsync("fixture");
        var guarded = measurement.Guard(local);
        await Assert.ThrowsAsync<IOException>(() => guarded.GenerateAsync(Request, CancellationToken.None));
        await Assert.ThrowsAsync<IOException>(() => guarded.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal(1, chat.Calls);
    }

    private sealed class ContextHandler : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("""{"message":{"content":"fixture"},"done":true,"prompt_eval_count":4090,"eval_count":1}""") });
        }
    }

    private sealed class Counter : IModelProvider
    {
        internal int Calls { get; private set; }
        public string Name => "fixture";
        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) { Calls++; return Task.FromResult("fixture"); }
        public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        { Calls++; await Task.CompletedTask; yield return "first"; yield return "second"; }
    }

    private sealed class Cutoff : IModelProvider
    {
        public string Name => "fixture";
        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) =>
            throw new ModelProviderException("fixture cap") { IsCutOff = true };
    }

    private sealed class Handler : HttpMessageHandler
    {
        internal List<string> Calls { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"done":true,"eval_count":0}""") };
        }
    }

    private sealed class FixtureFolder : IDisposable
    {
        internal string Path { get; } = Directory.CreateTempSubdirectory("dz-alpha03-app-measure-unit-").FullName;
        public void Dispose()
        {
            Assert.StartsWith(System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dz-alpha03-app-measure-unit-")), System.IO.Path.GetFullPath(Path), StringComparison.OrdinalIgnoreCase);
            Directory.Delete(Path, recursive: true);
        }
    }
}
