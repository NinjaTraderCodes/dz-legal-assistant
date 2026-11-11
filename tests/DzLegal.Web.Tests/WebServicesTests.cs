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
    public async Task AProtectedProviderHoldsItsSlotUntilTheAnswerEnds()
    {
        using var gate = new ModelGate(1);
        using var provider = new ProtectedModelProvider(new Echo(), gate, []);

        var pieces = new List<string>();
        await foreach (var piece in provider.StreamAsync(Request, CancellationToken.None))
        {
            pieces.Add(piece);
            var other = gate.EnterAsync(CancellationToken.None);
            await Task.Delay(10);
            Assert.False(other.IsCompleted); // still writing: the slot is taken
            _ = other.ContinueWith(t => t.Result.Dispose(), TaskScheduler.Default);
        }

        Assert.Equal(["hello"], pieces);
    }

    [Fact]
    public async Task AFailingServiceShowsAPlainMessageLogsTheDetailAndFreesTheSlot()
    {
        using var gate = new ModelGate(1);
        var logged = new List<string>();
        using var provider = new ProtectedModelProvider(new Failing(new IOException("connection reset by peer")), gate, [], logged.Add);

        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(Request, CancellationToken.None));

        Assert.Equal(ProtectedModelProvider.Unavailable, ex.Message);
        Assert.Equal(["connection reset by peer"], logged);
        using var slot = await gate.EnterAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task EveryModelCallCountsAgainstEveryLimit()
    {
        using var gate = new ModelGate(1);
        var visitor = new CallQuota(2, "visitor limit");
        var network = new CallQuota(3, "network limit");
        using var first = new ProtectedModelProvider(new Echo(), gate, [visitor, network]);
        using var second = new ProtectedModelProvider(new Echo(), gate, [new CallQuota(10, "other visitor"), network]);

        await first.GenerateAsync(Request, CancellationToken.None);
        await first.GenerateAsync(Request, CancellationToken.None);
        var ex = await Assert.ThrowsAsync<ModelProviderException>(() => first.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal("visitor limit", ex.Message);

        // A reload is a new session, but the same network: its limit is shared.
        await second.GenerateAsync(Request, CancellationToken.None);
        ex = await Assert.ThrowsAsync<ModelProviderException>(() => second.GenerateAsync(Request, CancellationToken.None));
        Assert.Equal("network limit", ex.Message);
    }

    private static readonly ModelRequest Request = new("s", "", "hello", "");

    private sealed class Echo : IModelProvider
    {
        public string Name => "echo";

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) => Task.FromResult(request.UserPrompt);
    }

    private sealed class Failing(Exception error) : IModelProvider
    {
        public string Name => "failing";

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) => Task.FromException<string>(error);
    }
}

public class CallQuotaTests
{
    [Fact]
    public void CallsAreCountedInASlidingHour()
    {
        var now = DateTimeOffset.Parse("2026-11-02T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var quota = new CallQuota(2, "limit", () => now);

        Assert.True(quota.TryTake());
        Assert.True(quota.TryTake());
        Assert.False(quota.TryTake());
        Assert.False(quota.IsIdle);

        now = now.AddMinutes(61);
        Assert.True(quota.IsIdle);
        Assert.True(quota.TryTake());
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
    [InlineData("nvidia/nemotron-3-ultra-550b-a55b", "Nemotron 3 Ultra")]
    [InlineData("nvidia/nemotron-3-ultra-550b-a55b:free", "Nemotron 3 Ultra")]
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
    public void NemotronOnOpenRouterComesFirstByDefaultThenGoogleGemma4()
    {
        // 31 Oct 2026: Nemotron 3 Ultra matched or beat Gemma 4 26B on every set, with no failed call; Google failed up to a
        // third of its calls the same day, so Gemma 4 26B is the fallback.
        using var engine = Create(new EngineSettings { GoogleApiKey = "g", OpenRouterApiKey = "o", GoogleModels = ["gemma-4-26b-a4b-it"] });
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");

        Assert.Equal("Nemotron 3 Ultra (demo server)", vm.SelectedProvider?.DisplayName);
        Assert.Equal(["nvidia/nemotron-3-ultra-550b-a55b (OpenRouter)", "gemma-4-26b-a4b-it (Google AI Studio)"], engine.EngineNames);
    }

    [Fact]
    public void TheOrderCanPutGoogleFirst()
    {
        using var engine = Create(new EngineSettings { GoogleApiKey = "g", OpenRouterApiKey = "o", GoogleModels = ["gemma-4-26b-a4b-it"], Order = ["google", "openrouter"] });
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");

        Assert.Equal("Gemma 4 26B A4B (demo server)", vm.SelectedProvider?.DisplayName);
        Assert.Equal(["gemma-4-26b-a4b-it (Google AI Studio)", "nvidia/nemotron-3-ultra-550b-a55b (OpenRouter)"], engine.EngineNames);
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

            // Everything the lawyer fills in is there, the requests included (they are the lawyer's lines); only the
            // legal grounds wait for the model.
            var missing = vm.Report!.Missing.Where(f => f.Element.Severity == Severity.Inadmissible).SelectMany(f => f.MissingParts).ToList();
            Assert.Equal(["grounds"], missing);
            Assert.StartsWith("في الشكل: قبول الدعوى شكلا.", vm.Sections.Single(x => x.Id == "requests").Text, StringComparison.Ordinal);
            Assert.DoesNotContain(vm.FieldGroups.SelectMany(g => g.Fields), f => f.IsInformal); // the demo shows the supported input: formal Arabic
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

    [Theory]
    [InlineData("en")]
    [InlineData("ar")]
    public void TheGroundsNextStepNamesTheAppsOwnButtonsInBothLanguages(string language)
    {
        // The hint under the red grounds finding quotes the buttons by their on-screen names: two before the analysis, one after.
        Assert.Equal(2, CountPlaceholders(WebText.Get("NextAnalyse", language)));
        Assert.Equal(1, CountPlaceholders(WebText.Get("NextDraft", language)));
        static int CountPlaceholders(string text) => System.Text.RegularExpressions.Regex.Count(text, @"\{[01]\}");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ar")]
    public void TheGuideHasATitleAndItsHintsForEveryStepInBothLanguages(string language)
    {
        string[] keys = ["GuideTitle", "GuideCount", "GuidePoints", "GuideFinish", "GuideLocked", "GuideRetry", "GuideDoneStep", "GuideStep", "GuideHide", "GuideShow", "GuideOpen", "GuideInvite",
            "Guide1Pick", "Guide2Clear", "Guide2Look", "Guide3Analyse", "Guide3Wait", "Guide3Confirm", "Guide3Articles", "Guide4Generate", "Guide4Wait",
            "Guide5Early", "Guide5Tick", "Guide5Again", "Guide5Export", "Guide5Done"];
        foreach (var key in Enumerable.Range(1, Components.Pages.Home.GuideSteps).Select(n => $"Guide{n}Title").Concat(keys))
        {
            Assert.False(WebText.Get(key, language).StartsWith('['), $"{key} has no {language} text");
        }
    }

    [Fact]
    public void TheGuidesSampleHasTheStepsAndTheFieldItPointsAt()
    {
        using var engine = Create(new EngineSettings());
        var sample = Assert.Single(engine.Samples, s => s.Id == Components.Pages.Home.SampleId);
        using var vm = engine.CreateSession(new NoFiles(), new NoExternalLinks(), "en");
        vm.OpenCase(sample.Inputs);

        // Step 2 clears the defendant's domicile and watches the score drop; step 3 opens the grounds.
        Assert.Contains(vm.FieldGroups, g => g.Id == Components.Pages.Home.DefendantStepId);
        Assert.Contains(vm.FieldGroups, g => g.Id == Presentation.ViewModels.MainWindowViewModel.GroundsStepId);
        var before = vm.PresentCount;
        vm.Field(Components.Pages.Home.DomicileId).Value = "";
        Assert.Equal(before - 1, vm.PresentCount);
    }

    private sealed class NoFiles : IUserFiles
    {
        public Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind) => Task.FromResult<(Stream, string)?>(null);

        public Task<(Stream Stream, string Name)?> OpenAsync() => Task.FromResult<(Stream, string)?>(null);

        public Task CopyTextAsync(string text) => Task.CompletedTask;
    }
}
