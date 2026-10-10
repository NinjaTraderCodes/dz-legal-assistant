using DzLegal.Domain;
using DzLegal.Drafting;
using DzLegal.Presentation.Services;
using DzLegal.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DzLegal.Web.Tests;

public class ModelGateTests
{
    [Fact]
    public async Task CallsBeyondTheCapacityWaitTheirTurn()
    {
        using var gate = new ModelGate(1);
        var first = await gate.EnterAsync(CancellationToken.None);
        var second = gate.EnterAsync(CancellationToken.None);

        await Task.Delay(50);
        Assert.False(second.IsCompleted);
        Assert.Equal(1, gate.Waiting);

        first.Dispose();
        using var slot = await second;
        Assert.Equal(0, gate.Waiting);
    }

    [Fact]
    public async Task AGatedProviderHoldsItsSlotUntilTheAnswerEnds()
    {
        using var gate = new ModelGate(1);
        using var provider = new GatedModelProvider(new Echo(), gate);

        var pieces = new List<string>();
        await foreach (var piece in provider.StreamAsync(new ModelRequest("s", "", "hello", ""), CancellationToken.None))
        {
            pieces.Add(piece);
            var other = gate.EnterAsync(CancellationToken.None);
            await Task.Delay(10);
            Assert.False(other.IsCompleted); // still writing: the slot is taken
            _ = other.ContinueWith(t => t.Result.Dispose(), TaskScheduler.Default);
        }

        Assert.Equal(["hello"], pieces);
    }

    private sealed class Echo : IModelProvider
    {
        public string Name => "echo";

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) => Task.FromResult(request.UserPrompt);
    }
}

public class SessionQuotaTests
{
    [Fact]
    public void ASessionGetsItsRunsPerHourThenWaits()
    {
        var now = DateTimeOffset.Parse("2026-11-02T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var quota = new SessionQuota(2, () => now);

        Assert.True(quota.TryTake());
        Assert.True(quota.TryTake());
        Assert.False(quota.TryTake());

        now = now.AddMinutes(61);
        Assert.True(quota.TryTake());
    }

    [Fact]
    public async Task OnceSpentTheEngineExplainsTheLimit()
    {
        var provider = new QuotaReachedModelProvider("Gemma 3 4B", 30);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(new ModelRequest("s", "", "u", ""), CancellationToken.None));
        Assert.Contains("30 AI runs", ex.Message, StringComparison.Ordinal);
        Assert.Contains("rule check", ex.Message, StringComparison.Ordinal);
    }
}

public class EngineSettingsTests
{
    [Fact]
    public void KeysComeFromTheEnvironmentAndModelsFromSettings()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engines:GoogleModels:0"] = "gemma-3-12b-it",
            ["Engines:MaxConcurrentCalls"] = "0",
            ["GOOGLE_API_KEY"] = "  g-key  ",
            ["GROQ_API_KEY"] = "",
        }).Build();

        var settings = EngineSettings.From(configuration);

        Assert.Equal(["gemma-3-12b-it"], settings.GoogleModels);
        Assert.Equal("g-key", settings.GoogleApiKey);
        Assert.Null(settings.GroqApiKey);
        Assert.Equal(1, settings.MaxConcurrentCalls); // never zero: that would block every call
        Assert.True(settings.HasAnyEngine);
    }

    [Fact]
    public void WithoutAnyKeyThereIsNoEngine() =>
        Assert.False(EngineSettings.From(new ConfigurationBuilder().Build()).HasAnyEngine);

    [Theory]
    [InlineData("gemma-3-4b-it", "Gemma 3 4B")]
    [InlineData("gemma-3-27b-it", "Gemma 3 27B")]
    [InlineData("gemma-4-26b-a4b-it", "Gemma 4 26B A4B")]
    [InlineData("google/gemma-3-4b-it", "Gemma 3 4B")]
    [InlineData("google/gemma-3-4b-it:free", "Gemma 3 4B")]
    [InlineData("meta-llama/llama-3.3-70b-instruct", "meta-llama/llama-3.3-70b-instruct")]
    [InlineData("llama-3.3-70b-versatile", "llama-3.3-70b-versatile")]
    public void ModelNamesAreShownReadably(string model, string shown) => Assert.Equal(shown, WebEngine.Pretty(model));
}

public class WebEngineTests
{
    private static WebEngine Create(EngineSettings settings) => new(settings, AppContext.BaseDirectory, NullLogger<WebEngine>.Instance);

    [Fact]
    public void WithoutAKeyOnlyTemplateModeIsOffered()
    {
        using var engine = Create(new EngineSettings());
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");

        Assert.Equal(["stub"], vm.Providers.Select(p => p.Key));
        Assert.Empty(engine.EngineNames);
    }

    [Fact]
    public void WithAKeyTheHostedEngineComesFirst()
    {
        using var engine = Create(new EngineSettings { GoogleApiKey = "k" });
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");

        Assert.Equal(["hosted", "stub"], vm.Providers.Select(p => p.Key));
        Assert.Equal("hosted", vm.SelectedProvider?.Key);
        Assert.Equal("Gemma 4 26B A4B (demo server)", vm.SelectedProvider?.DisplayName);
        Assert.Equal("gemma-4-26b-a4b-it (Google AI Studio)", engine.EngineNames[0]);
    }

    [Fact]
    public void OpenRouterComesFirstWhenItsKeyIsSetThenGoogle()
    {
        using var engine = Create(new EngineSettings { GoogleApiKey = "g", OpenRouterApiKey = "o", GoogleModels = ["gemma-4-26b-a4b-it"] });
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");

        Assert.Equal("Gemma 3 4B (demo server)", vm.SelectedProvider?.DisplayName);
        Assert.Equal(["google/gemma-3-4b-it (OpenRouter)", "gemma-4-26b-a4b-it (Google AI Studio)"], engine.EngineNames);
    }

    [Fact]
    public void TheOrderCanPutGoogleFirst()
    {
        using var engine = Create(new EngineSettings { GoogleApiKey = "g", OpenRouterApiKey = "o", GoogleModels = ["gemma-4-26b-a4b-it"], Order = ["google", "openrouter"] });

        Assert.Equal(["gemma-4-26b-a4b-it (Google AI Studio)", "google/gemma-3-4b-it (OpenRouter)"], engine.EngineNames);
    }

    [Fact]
    public async Task ModelsGoogleNoLongerServesAreDropped()
    {
        using var engine = Create(new EngineSettings { GoogleApiKey = "k", GoogleModels = ["gemma-3-4b-it", "gemma-4-31b-it", "gemma-4-26b-a4b-it"] });
        await engine.ResolveModelsAsync(new ModelListHandler("gemma-4-26b-a4b-it", "gemma-4-31b-it", "gemini-3.8-flash"), CancellationToken.None);

        Assert.Equal(["gemma-4-31b-it", "gemma-4-26b-a4b-it"], engine.GoogleModels);
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");
        Assert.Equal("Gemma 4 31B (demo server)", vm.SelectedProvider?.DisplayName);
    }

    [Fact]
    public async Task WhenNoConfiguredModelIsLeftAnyGemmaTheKeyCanUseTakesOver()
    {
        using var engine = Create(new EngineSettings { GoogleApiKey = "k", GoogleModels = ["gemma-3-4b-it"] });
        await engine.ResolveModelsAsync(new ModelListHandler("gemini-3.8-flash", "gemma-5-9b-it"), CancellationToken.None);

        Assert.Equal(["gemma-5-9b-it"], engine.GoogleModels);
    }

    [Fact]
    public async Task IfGoogleCannotBeAskedTheConfiguredModelsStay()
    {
        using var engine = Create(new EngineSettings { GoogleApiKey = "k", GoogleModels = ["gemma-3-4b-it"] });
        await engine.ResolveModelsAsync(new ModelListHandler(fail: true), CancellationToken.None);

        Assert.Equal(["gemma-3-4b-it"], engine.GoogleModels);
    }

    private sealed class ModelListHandler(params string[] models) : HttpMessageHandler
    {
        public ModelListHandler(bool fail)
            : this() => Fail = fail;

        private bool Fail { get; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("k", request.Headers.GetValues("x-goog-api-key").Single());
            var body = "{\"models\":[" + string.Join(',', models.Select(m => $$"""{"name":"models/{{m}}","supportedGenerationMethods":["generateContent"]}""")) + "]}";
            return Task.FromResult(Fail
                ? new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden) { Content = new StringContent("denied") }
                : new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    [Fact]
    public void EverySampleCaseOpensWithOnlyTheAiSectionsLeftToWrite()
    {
        using var engine = Create(new EngineSettings());
        Assert.Equal(4, engine.Samples.Count);

        foreach (var sample in engine.Samples)
        {
            using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");
            vm.OpenCase(sample.Inputs);

            // Everything the lawyer fills in is there; the legal grounds and the requests wait for the model.
            var missing = vm.Report!.Missing.Where(f => f.Element.Severity == Severity.Inadmissible).SelectMany(f => f.MissingParts).ToList();
            Assert.Equal(["grounds", "requests"], missing.Order(StringComparer.Ordinal));
            Assert.False(string.IsNullOrWhiteSpace(sample.Summary.Get("en")));
            Assert.False(string.IsNullOrWhiteSpace(sample.Summary.Get("ar")));
        }
    }

    [Fact]
    public async Task TemplateModeCompletesASampleWithoutAnyModel()
    {
        using var engine = Create(new EngineSettings());
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");
        vm.OpenCase(engine.Samples[0].Inputs);

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.True(vm.IsAdmissible);
        Assert.Equal(vm.ApplicableCount, vm.PresentCount);
    }

    [Fact]
    public void TheWatcherRefreshesThePageWhenAFieldOrTheDraftChanges()
    {
        using var engine = Create(new EngineSettings());
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");
        var changes = 0;
        using var watcher = new ViewModelWatcher(vm, () => changes++);

        vm.Field("defendant.domicile").Value = "حي النصر، الجزائر";
        Assert.True(changes > 0);

        changes = 0;
        vm.Sections[0].SetFromModel("نص جديد");
        Assert.True(changes > 0);
    }

    private sealed class NoFiles : IUserFiles
    {
        public Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind) => Task.FromResult<(Stream, string)?>(null);

        public Task<(Stream Stream, string Name)?> OpenAsync() => Task.FromResult<(Stream, string)?>(null);

        public Task CopyTextAsync(string text) => Task.CompletedTask;
    }
}
