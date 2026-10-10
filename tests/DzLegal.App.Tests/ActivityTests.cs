using DzLegal.Drafting;

namespace DzLegal.App.Tests;

public sealed class ModelActivityTests
{
    private readonly Loc _loc = new();
    private DateTimeOffset _now = new(2026, 10, 7, 9, 0, 0, TimeSpan.Zero);

    private ModelActivityViewModel Create(bool showLiveText = true) => new(_loc, showLiveText, () => _now, useTimer: false);

    [Fact]
    public void AStepWaitsForTheModelUntilTheFirstWordsArrive()
    {
        var activity = Create();
        activity.Start(["قراءة", "اختيار"]);
        activity.BeginStep(0);

        Assert.True(activity.IsActive);
        Assert.True(activity.IsWaiting);
        Assert.True(activity.Steps[0].IsActive);
        Assert.True(activity.Steps[1].IsPending);
        Assert.Equal(_loc["ActivityWaking"], activity.WaitingText);
        Assert.Equal("0:00", activity.Elapsed);

        activity.OnText("حيث إن المدعي");
        Assert.False(activity.IsWaiting);
        Assert.Equal("حيث إن المدعي", activity.LiveText);
        Assert.True(activity.HasLiveText);
        Assert.Equal(_loc.Format("ActivityWords", 3), activity.WordsText);
    }

    [Fact]
    public void ALongWaitSaysTheModelIsLoadingAndTheTimerRuns()
    {
        var activity = Create();
        activity.Start(["قراءة"]);
        activity.BeginStep(0);

        _now += TimeSpan.FromSeconds(75);
        activity.Tick();

        Assert.Equal(_loc["ActivityLoading"], activity.WaitingText);
        Assert.Equal("1:15", activity.Elapsed);
    }

    [Fact]
    public void TheNextStepMarksTheEarlierOnesDoneAndClearsTheLiveText()
    {
        var activity = Create();
        activity.Start(["قراءة", "بحث", "اختيار"]);
        activity.BeginStep(0);
        activity.OnText("نص");
        activity.BeginStep(1, waitsForModel: false);
        activity.SetDetail(1, "18");

        Assert.True(activity.Steps[0].IsDone);
        Assert.True(activity.Steps[1].IsActive);
        Assert.Equal("18", activity.Steps[1].Detail);
        Assert.False(activity.IsWaiting); // a search step calls no model
        Assert.Empty(activity.LiveText);
    }

    [Fact]
    public void TheLiveBoxKeepsOnlyTheEndOfALongAnswer()
    {
        var activity = Create();
        activity.Start(["كتابة"]);
        activity.BeginStep(0);
        var answer = string.Join(' ', Enumerable.Repeat("وحيث إن المدعى عليه امتنع عن السداد", 30)) + " النهاية";
        activity.OnText(answer);

        Assert.StartsWith("…", activity.LiveText, StringComparison.Ordinal);
        Assert.EndsWith("النهاية", activity.LiveText, StringComparison.Ordinal);
        Assert.True(activity.LiveText.Length <= ModelActivityViewModel.LiveTextLength + 1);
    }

    [Fact]
    public void LiveTextCanBeHiddenWhereTheTextAlreadyAppearsElsewhere()
    {
        var activity = Create(showLiveText: false);
        activity.Start(["كتابة"]);
        activity.BeginStep(0);
        activity.OnText("نص");
        Assert.False(activity.HasLiveText);
        Assert.False(activity.IsWaiting);
    }

    [Fact]
    public void CompleteMarksEveryStepDoneAndHidesThePanel()
    {
        var activity = Create();
        activity.Start(["أ", "ب"]);
        activity.BeginStep(0);
        activity.Complete();
        Assert.All(activity.Steps, s => Assert.True(s.IsDone));
        Assert.False(activity.IsActive);
        Assert.False(activity.IsWaiting);
    }
}

public sealed class ModelActivityWorkflowTests
{
    [Fact]
    public async Task TheAnalysisShowsEachStepWhileTheModelWorksThenHides()
    {
        MainWindowViewModel? vm = null;
        var seen = new List<(string Request, bool Waiting, string Step)>();
        var recorder = new RecordingProvider(r =>
        {
            var a = vm!.AnalysisActivity;
            seen.Add((r.SectionId, a.IsWaiting, a.Steps.Single(s => s.IsActive).Label));
            return r.SectionId == GroundsAnalyzer.ReadingRequestId ? Analysis.Reading : Analysis.Selection;
        });
        vm = Vm.Create(recorder: recorder).Filled();

        await vm.AnalyseGroundsCommand.ExecuteAsync(null);

        Assert.Equal(
            [
                (GroundsAnalyzer.ReadingRequestId, true, vm.L["AnalysisStepReading"]),
                (GroundsAnalyzer.SelectionRequestId, true, vm.L["AnalysisStepSelecting"]),
            ],
            seen);
        var activity = vm.AnalysisActivity;
        Assert.False(activity.IsActive);
        Assert.All(activity.Steps, s => Assert.True(s.IsDone));
        Assert.Matches(@"^\d+", activity.Steps[1].Detail); // "n candidate articles"
        Assert.NotEmpty(vm.AnalysedArticles);
    }

    [Fact]
    public async Task GeneratingMarksTheSectionBeingWrittenAndClearsItAfterwards()
    {
        MainWindowViewModel? vm = null;
        var writing = new List<(string Request, string[] Marked, bool Waiting)>();
        var recorder = new RecordingProvider(r =>
        {
            writing.Add((r.SectionId, vm!.Sections.Where(s => s.IsWriting).Select(s => s.Id).ToArray(), vm.GenerationActivity.IsWaiting));
            return r.SourceText;
        });
        vm = Vm.Create(recorder: recorder).Filled();

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.NotEmpty(writing);
        Assert.All(writing, w =>
        {
            Assert.Equal([w.Request], w.Marked);
            Assert.True(w.Waiting);
        });
        Assert.DoesNotContain(vm.Sections, s => s.IsWriting);
        Assert.False(vm.GenerationActivity.IsActive);
    }

    [Fact]
    public async Task TheModelIsWarmedUpOnceWhenItIsReady()
    {
        var calls = 0;
        var option = new ProviderOption(
            "local",
            _ => "local",
            () => new RecordingProvider(),
            _ => Task.FromResult(new ModelStatus(ModelState.Ready, "m")),
            _ =>
            {
                calls++;
                return Task.CompletedTask;
            });
        var vm = Vm.Create(extra: option);

        await vm.CheckModelStatusAsync();
        await vm.CheckModelStatusAsync();

        Assert.Equal(1, calls);
        Assert.Equal(vm.L.Format("ModelInMemory", "local"), vm.ModelStatusText);
    }

    [Fact]
    public async Task AFailedWarmUpIsNotRetriedAndTheModelStillShowsReady()
    {
        var calls = 0;
        var option = new ProviderOption(
            "local",
            _ => "local",
            () => new RecordingProvider(),
            _ => Task.FromResult(new ModelStatus(ModelState.Ready, "m")),
            _ =>
            {
                calls++;
                throw new ModelProviderException("out of memory");
            });
        var vm = Vm.Create(extra: option);

        await vm.CheckModelStatusAsync();

        Assert.Equal(1, calls);
        Assert.Equal(vm.L.Format("ModelReady", "local"), vm.ModelStatusText);
    }

    [Fact]
    public void TemplateModeHasNothingToWarmUp() =>
        Assert.False(Vm.Create().Providers.Single(p => p.Key == "stub").CanWarmUp);
}
