using DzLegal.Drafting;

namespace DzLegal.App.Tests;

/// <summary>A power state set by the test.</summary>
internal sealed class FakePower(bool saving) : IPowerStatus
{
    public bool IsSavingPower() => saving;
}

/// <summary>Holds every request until the test releases it, to look at the window while the model works.</summary>
internal sealed class GatedProvider(Func<ModelRequest, string> reply) : IModelProvider
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public List<ModelRequest> Requests { get; } = [];

    public string Name => "gated";

    public void Release() => _gate.TrySetResult();

    public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        await _gate.Task.WaitAsync(cancellationToken);
        return reply(request);
    }
}

public sealed class EstimateTests
{
    private readonly Loc _loc = new();
    private DateTimeOffset _now = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private ModelActivityViewModel Create() => new(_loc, now: () => _now, useTimer: false);

    [Fact]
    public void TheUsualTimeOnThisComputerGivesTheTimeLeftRoundedUpTo10Seconds()
    {
        var activity = Create();
        activity.Start(["قراءة"], new ActivityExpectation(TimeSpan.FromSeconds(120)));
        Assert.Equal(_loc.Format("ActivityRemaining", "2:00"), activity.EstimateText);

        _now += TimeSpan.FromSeconds(43);
        activity.Tick();
        Assert.Equal(_loc.Format("ActivityRemaining", "1:20"), activity.EstimateText); // 77 s left

        _now += TimeSpan.FromSeconds(72);
        activity.Tick();
        Assert.Equal(_loc["ActivityAlmostDone"], activity.EstimateText);

        _now += TimeSpan.FromSeconds(10);
        activity.Tick();
        Assert.Equal(_loc["ActivityLongerThanUsual"], activity.EstimateText);
        Assert.Equal("2:05", activity.Elapsed);
    }

    [Fact]
    public void BeforeTheFirstRunAHintIsShownAndWithoutOneNothing()
    {
        var activity = Create();
        activity.Start(["قراءة"], new ActivityExpectation(FirstRunHintKey: "AnalysisFirstRun"));
        Assert.Equal(_loc["AnalysisFirstRun"], activity.EstimateText);
        Assert.True(activity.HasEstimate);

        activity.Start(["قراءة"]);
        Assert.False(activity.HasEstimate);
        Assert.False(activity.HasPowerNote);
    }

    [Fact]
    public void OnBatteryOrPowerSaverTheLawyerIsTold()
    {
        var activity = Create();
        activity.Start(["كتابة"], new ActivityExpectation(SavingPower: true));
        Assert.Equal(_loc["ActivityPowerSaving"], activity.PowerNote);
        Assert.True(activity.HasPowerNote);
    }

    [Fact]
    public void TheUsualTimeIsTheMedianOfTheLastFiveRunsPerEngine()
    {
        var timings = new TaskTimings();
        Assert.Null(timings.Typical("analysis", "ollama"));

        // A cold start (300 s) and an early slow run fall out of the median, then out of the last five.
        foreach (var seconds in new[] { 300, 200, 120, 110, 130, 125, 118 })
        {
            timings.Record("analysis", "ollama", TimeSpan.FromSeconds(seconds));
        }

        Assert.Equal(TimeSpan.FromSeconds(120), timings.Typical("analysis", "ollama"));
        Assert.Null(timings.Typical("analysis", "other"));
        Assert.Null(timings.Typical("draft", "ollama"));
    }

    [Fact]
    public void TheTimesAreKeptInALocalFileAndADamagedFileOnlyLosesThem()
    {
        var file = Path.Combine(Path.GetTempPath(), "dzlegal-timings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            new TaskTimings(file).Record("draft", "ollama", TimeSpan.FromSeconds(80));
            Assert.Equal(TimeSpan.FromSeconds(80), new TaskTimings(file).Typical("draft", "ollama"));

            File.WriteAllText(file, "{ not json");
            var damaged = new TaskTimings(file);
            Assert.Null(damaged.Typical("draft", "ollama"));
            damaged.Record("draft", "ollama", TimeSpan.FromSeconds(70));
            Assert.Equal(TimeSpan.FromSeconds(70), new TaskTimings(file).Typical("draft", "ollama"));
        }
        finally
        {
            File.Delete(file);
        }
    }
}

public sealed class WaitingWorkflowTests
{
    private static FieldGroupViewModel Step(MainWindowViewModel vm, string id) => vm.FieldGroups.Single(g => g.Id == id);

    private static async Task LeaveTheFactsStep(MainWindowViewModel vm)
    {
        vm.SelectStepCommand.Execute(Step(vm, "facts"));
        vm.SelectStepCommand.Execute(Step(vm, "requests"));
        if (vm.AnalyseGroundsCommand.ExecutionTask is { } running)
        {
            await running;
        }
    }

    [Fact]
    public async Task LeavingTheFactsStepStartsTheAnalysisWithALocalModelButTicksNothing()
    {
        var recorder = Analysis.Model();
        var vm = Vm.Create(recorder: recorder, analyseInBackground: true).Filled();
        await LeaveTheFactsStep(vm);

        Assert.Equal(2, recorder.Requests.Count);
        Assert.NotNull(vm.Analysis);
        Assert.Equal(["450", "323", "454"], vm.AnalysedArticles.Select(a => a.Reference.Number));
        Assert.Empty(vm.SelectedArticles); // the lawyer still confirms the claim type
        Assert.True(vm.NeedsClaimConfirmation);
        Assert.False(vm.IsAnalysisAutomatic);

        // Once analysed, leaving the facts step again does not run it again; changed facts show as stale instead.
        vm.Field("notes.facts").Value += " وأنذره بالدفع.";
        await LeaveTheFactsStep(vm);
        Assert.Equal(2, recorder.Requests.Count);
        Assert.True(vm.IsAnalysisStale);
    }

    [Fact]
    public async Task WhileItRunsTheWindowSaysItStartedOnItsOwn()
    {
        var gated = new GatedProvider(r => r.SectionId == GroundsAnalyzer.ReadingRequestId ? Analysis.Reading : Analysis.Selection);
        var vm = Vm.Create(extra: new ProviderOption("gated", _ => "gated", () => gated), analyseInBackground: true).Filled();
        vm.SelectStepCommand.Execute(Step(vm, "facts"));
        vm.SelectStepCommand.Execute(Step(vm, "requests"));

        Assert.True(vm.AnalyseGroundsCommand.IsRunning);
        Assert.True(vm.IsAnalysisAutomatic);
        Assert.True(vm.ShowsBackgroundAnalysis);
        vm.SelectStepCommand.Execute(Step(vm, "grounds"));
        Assert.False(vm.ShowsBackgroundAnalysis); // the grounds step shows the full activity panel instead

        gated.Release();
        await vm.AnalyseGroundsCommand.ExecutionTask!;
        Assert.False(vm.IsAnalysisAutomatic);
        Assert.NotNull(vm.Analysis);
    }

    [Fact]
    public async Task NothingStartsOnItsOwnWhenSwitchedOffWithoutFactsInTemplateModeOrWithAnOnlineEngine()
    {
        var off = Analysis.Model();
        await LeaveTheFactsStep(Vm.Create(recorder: off).Filled());
        Assert.Empty(off.Requests);

        var noFacts = Analysis.Model();
        await LeaveTheFactsStep(Vm.Create(recorder: noFacts, analyseInBackground: true));
        Assert.Empty(noFacts.Requests);

        var stub = Vm.Create(analyseInBackground: true).Filled();
        await LeaveTheFactsStep(stub);
        Assert.Null(stub.Analysis);

        // The case notes never leave the computer without the lawyer's click.
        var online = Analysis.Model();
        var vm = Vm.Create(extra: new ProviderOption("online", _ => "online", () => online, sendsCaseData: true), analyseInBackground: true).Filled();
        await LeaveTheFactsStep(vm);
        Assert.Empty(online.Requests);
    }

    [Fact]
    public async Task NothingStartsOnItsOwnWhenTheModelIsNotReady()
    {
        var recorder = Analysis.Model();
        var vm = Vm.Create(
            extra: new ProviderOption("down", _ => "down", () => recorder, _ => Task.FromResult(new ModelStatus(ModelState.NotRunning))),
            analyseInBackground: true).Filled();
        await LeaveTheFactsStep(vm);
        Assert.Empty(recorder.Requests);
    }

    [Fact]
    public async Task TheAnalysisAndAFullDraftAreTimedPerEngineAndTheNextRunShowsTheTimeLeft()
    {
        var timings = new TaskTimings();
        var vm = Vm.Create(recorder: Analysis.Model(), timings: timings).Filled();

        Assert.Equal(vm.L["AnalysisFirstRun"], StartedWith(vm.AnalysisActivity, () => vm.AnalyseGroundsCommand.ExecuteAsync(null)));
        Assert.NotNull(timings.Typical(MainWindowViewModel.AnalysisTask, "recording"));

        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.NotNull(timings.Typical(MainWindowViewModel.DraftTask, "recording"));
        Assert.Null(timings.Typical(MainWindowViewModel.DraftTask, "stub"));

        timings.Record(MainWindowViewModel.DraftTask, "recording", TimeSpan.FromSeconds(90));
        timings.Record(MainWindowViewModel.DraftTask, "recording", TimeSpan.FromSeconds(90));
        Assert.Equal(vm.L.Format("ActivityRemaining", "1:30"), StartedWith(vm.GenerationActivity, () => vm.GenerateCommand.ExecuteAsync(null)));
    }

    [Fact]
    public async Task TheTemplateModeNeverPromisesATimeNorWarnsAboutPower()
    {
        var timings = new TaskTimings();
        var vm = Vm.Create(timings: timings, power: new FakePower(saving: true)).Filled();
        string? power = null;
        vm.GenerationActivity.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ModelActivityViewModel.IsActive) && vm.GenerationActivity.IsActive)
            {
                power = vm.GenerationActivity.PowerNote;
                Assert.False(vm.GenerationActivity.HasEstimate);
            }
        };
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Equal(string.Empty, power);
        Assert.Null(timings.Typical(MainWindowViewModel.DraftTask, "stub (template mode, no AI)"));
    }

    [Fact]
    public async Task OnBatteryTheLocalModelsPanelSaysWhy()
    {
        var vm = Vm.Create(recorder: Analysis.Model(), power: new FakePower(saving: true)).Filled();
        string? power = null;
        vm.AnalysisActivity.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ModelActivityViewModel.IsActive) && vm.AnalysisActivity.IsActive)
            {
                power = vm.AnalysisActivity.PowerNote;
            }
        };
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.Equal(vm.L["ActivityPowerSaving"], power);
    }

    /// <summary>The estimate the panel showed when <paramref name="run"/> started it.</summary>
    private static string? StartedWith(ModelActivityViewModel activity, Func<Task> run)
    {
        string? estimate = null;
        void Capture(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModelActivityViewModel.IsActive) && activity.IsActive)
            {
                estimate ??= activity.EstimateText;
            }
        }

        activity.PropertyChanged += Capture;
        try
        {
            run().GetAwaiter().GetResult();
        }
        finally
        {
            activity.PropertyChanged -= Capture;
        }

        return estimate;
    }

    [Fact]
    public void ClosingTheAppFreesTheLocalModelsMemoryAndAServerThatHangsDoesNotHoldItUp()
    {
        var released = 0;
        var local = new ProviderOption("local", _ => "local", () => Analysis.Model(), release: async ct =>
        {
            Interlocked.Increment(ref released);
            await Task.Delay(Timeout.Infinite, ct); // a server that never answers
        });
        var vm = Vm.Create(extra: local);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        vm.ReleaseModels();

        Assert.Equal(1, released);
        Assert.True(watch.Elapsed < MainWindowViewModel.ReleaseWait + TimeSpan.FromSeconds(2));
    }
}
