using System.Net;
using System.Text;
using DzLegal.App;
using DzLegal.App.Services.LocalModels;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.App.Tests;

public class ManagedEngineTests
{
    private sealed class FakeProcess(bool exited = false, bool owns = true) : IEngineProcess
    {
        public bool HasExited { get; set; } = exited;
        public bool Disposed { get; private set; }
        public bool OwnsListener(int port) => owns;
        public void Dispose() => Disposed = true;
    }
    private sealed class Launcher(Func<FakeProcess> create) : IEngineLauncher
    {
        public List<FakeProcess> Processes { get; } = [];
        public IEngineProcess Start(int port) { var process = create(); Processes.Add(process); return process; }
    }

    [Fact]
    public async Task ACompatibleExistingServerIsReusedAndNeverOwned()
    {
        var launcher = new Launcher(() => new FakeProcess());
        var endpoint = new Uri("http://127.0.0.1:11434");
        using var engine = new ManagedOllama(endpoint, launcher, (_, _) => Task.FromResult(true));
        var session = await engine.EnsureAsync(CancellationToken.None);
        Assert.Equal(endpoint, session.Endpoint);
        Assert.False(session.IsManaged);
        Assert.False(engine.IsManaged);
        Assert.Empty(launcher.Processes);
    }

    [Fact]
    public async Task AClashUsesAnotherLoopbackPortAndOnlyOurProcessIsDisposed()
    {
        var launcher = new Launcher(() => new FakeProcess());
        var existing = new Uri("http://127.0.0.1:11434");
        var engine = new ManagedOllama(existing, launcher, (uri, _) => Task.FromResult(uri.Port == 12345), () => 12345);
        var session = await engine.EnsureAsync(CancellationToken.None);
        Assert.Equal("127.0.0.1", session.Endpoint.Host);
        Assert.Equal(12345, session.Endpoint.Port);
        Assert.True(session.IsManaged);
        Assert.Equal(session, await engine.EnsureAsync(CancellationToken.None));
        var owned = Assert.Single(launcher.Processes);
        engine.Dispose();
        engine.Dispose();
        Assert.True(owned.Disposed);
    }

    [Fact]
    public async Task ABindRaceHasBoundedRetriesAndDisposesEveryFailedCandidate()
    {
        var launcher = new Launcher(() => new FakeProcess(exited: true));
        using var engine = new ManagedOllama(new Uri("http://127.0.0.1:11434"), launcher, (_, _) => Task.FromResult(false), () => 12345);
        await Assert.ThrowsAsync<IOException>(() => engine.EnsureAsync(CancellationToken.None));
        Assert.Equal(3, launcher.Processes.Count);
        Assert.All(launcher.Processes, p => Assert.True(p.Disposed));
    }

    [Fact]
    public async Task ReadinessFromAListenerWeDoNotOwnIsNeverAccepted()
    {
        var launcher = new Launcher(() => new FakeProcess(owns: false));
        using var engine = new ManagedOllama(new Uri("http://127.0.0.1:11434"), launcher, (uri, _) => Task.FromResult(uri.Port != 11434), () => 12345);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.EnsureAsync(cancel.Token));
        Assert.True(Assert.Single(launcher.Processes).Disposed);
        Assert.False(engine.IsManaged);
    }

    [Fact]
    public void TheChildHasFixedPrivateLocalSettingsAndNoTrayOrInheritedCloudSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), "dz-model-test");
        var info = WindowsEngineLauncher.StartInfo(Path.Combine(root, "engine", "ollama.exe"), root, 12345);
        Assert.Equal(["serve"], info.ArgumentList);
        Assert.False(info.UseShellExecute);
        Assert.True(info.CreateNoWindow);
        Assert.Equal("127.0.0.1:12345", info.Environment["OLLAMA_HOST"]);
        Assert.Equal("1", info.Environment["OLLAMA_NO_CLOUD"]);
        Assert.Equal("false", info.Environment["OLLAMA_DEBUG_LOG_REQUESTS"]);
        Assert.Equal(Path.Combine(root, "models"), info.Environment["OLLAMA_MODELS"]);
        Assert.Equal(Path.Combine(root, "engine-home"), info.Environment["USERPROFILE"]);
        Assert.DoesNotContain("OLLAMA_REMOTES", info.Environment.Keys);
    }

    private sealed class MetadataHandler(bool remote = false) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            var json = request.RequestUri.AbsolutePath == "/api/tags"
                ? "{\"models\":[{\"name\":\"gemma3:4b\"},{\"name\":\"gemma4:e4b\"}]}"
                : "{\"details\":{\"format\":\"gguf\"},\"model_info\":{\"general.architecture\":\"gemma4\"}" + (remote ? ",\"remote_host\":\"https://example.invalid\",\"remote_model\":\"alias\"}" : "}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task Gemma4WinsAndThinkingIsResolvedAfterTheChoiceWithoutPullingAnything()
    {
        using var handler = new MetadataHandler();
        var choice = new OllamaModelChoice(new OllamaOptions(), null);
        Assert.Equal(ModelState.Ready, (await choice.ProbeAsync(CancellationToken.None, handler)).State);
        Assert.Equal("gemma4:e4b", choice.Options.Model);
        Assert.Equal(false, choice.Options.Think);
        Assert.Equal(["/api/tags", "/api/show"], handler.Paths);
    }

    [Fact]
    public async Task ACloudAliasWithoutACloudSuffixIsRejectedBeforeInference()
    {
        using var handler = new MetadataHandler(remote: true);
        var choice = new OllamaModelChoice(new OllamaOptions(), null);
        Assert.Equal(ModelState.ModelMissing, (await choice.ProbeAsync(CancellationToken.None, handler)).State);
        Assert.DoesNotContain("/api/chat", handler.Paths);
    }
}