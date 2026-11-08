using System.Net;
using System.Text.Json;
using DzLegal.Drafting;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class FactRunMeasurementTests
{
    [Fact]
    public async Task ReloadsAtOneElevenAndTwentyOneAndNeverPrintsPrivateLines()
    {
        using var folder = new FixtureFolder();
        using var handler = new Handler();
        using var output = new StringWriter();
        var reads = 0;
        using var run = new FactRunMeasurement("gemma4:e4b", new Uri("http://127.0.0.1:9/"), folder.Path, output,
            handler, () => Task.CompletedTask, _ => Task.CompletedTask);
        var code = await run.RunAsync(Cases(21), "fixture", (_, _, _) =>
        { reads++; return Task.FromResult(new FactReading(new CaseFacts(), "private-answer")); }, () => false);
        Assert.Equal(0, code);
        Assert.Equal(21, reads);
        Assert.Equal(7, handler.Calls.Count); // three unload/warm pairs, then final unload
        using var loads = JsonDocument.Parse(await File.ReadAllTextAsync(System.IO.Path.Combine(folder.Path, "loads.json")));
        Assert.Equal([1, 11, 21], loads.RootElement.EnumerateArray().Select(r => r.GetProperty("BeforeOrdinal").GetInt32()));
        Assert.Contains("PROGRESS 21/21", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("private-", output.ToString(), StringComparison.Ordinal);
        Assert.All(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries), line => Assert.True(line.StartsWith("PROGRESS ", StringComparison.Ordinal) || line.StartsWith("GUARD ", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task MemoryFailureDoesNotRescueReloadOrStartAnotherCase()
    {
        using var folder = new FixtureFolder();
        using var handler = new Handler();
        using var output = new StringWriter();
        var reads = 0;
        using var run = new FactRunMeasurement("gemma3:4b", new Uri("http://127.0.0.1:9/"), folder.Path, output,
            handler, () => reads == 10 ? Task.FromException(new IOException("private-case below 512 MiB")) : Task.CompletedTask, _ => Task.CompletedTask);
        var code = await run.RunAsync(Cases(12), "fixture", (_, _, _) =>
        { reads++; return Task.FromResult(new FactReading(new CaseFacts(), "private-answer")); }, () => false);
        Assert.Equal(3, code);
        Assert.Equal(10, reads);
        Assert.Equal(3, handler.Calls.Count); // first pair and final cleanup only
        Assert.DoesNotContain("private-", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TransportFailureStopsOnceAndKeepsItsMessageOffTheConsole()
    {
        using var folder = new FixtureFolder();
        using var handler = new Handler();
        using var output = new StringWriter();
        var reads = 0;
        using var run = new FactRunMeasurement("gemma4:e4b", new Uri("http://127.0.0.1:9/"), folder.Path, output,
            handler, () => Task.CompletedTask, _ => Task.CompletedTask);
        var code = await run.RunAsync(Cases(2), "fixture", (_, _, _) =>
        { reads++; throw new ModelProviderException("private-answer"); }, () => false);
        Assert.Equal(3, code);
        Assert.Equal(1, reads);
        Assert.DoesNotContain("private-", output.ToString(), StringComparison.Ordinal);
    }

    private static List<FactCase> Cases(int count) => Enumerable.Range(1, count).Select(i => new FactCase
    { Name = "private-case-" + i, Claim = "loan-repayment", Inputs = new Dictionary<string, string> { ["notes.facts"] = "private-notes" }, Gold = new FactGold() }).ToList();

    private sealed class Handler : HttpMessageHandler
    {
        internal List<string> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"done":true,"eval_count":0,"response":""}""") };
        }
    }

    private sealed class FixtureFolder : IDisposable
    {
        internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dz-alpha03-unit-" + Guid.NewGuid().ToString("N"));
        internal FixtureFolder() => Directory.CreateDirectory(Path);
        public void Dispose()
        {
            var expected = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dz-alpha03-unit-"));
            if (!System.IO.Path.GetFullPath(Path).StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unexpected fixture directory.");
            }

            Directory.Delete(Path, recursive: true);
        }
    }
}
