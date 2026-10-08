using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DzLegal.App.ViewModels;

public enum ActivityStepState
{
    Pending,
    Active,
    Done,
}

/// <summary>One step of a long model task (e.g. "Reading the facts"), with an optional detail ("18 found").</summary>
public sealed partial class ActivityStepViewModel(string label) : ObservableObject
{
    public string Label { get; } = label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending), nameof(IsActive), nameof(IsDone))]
    public partial ActivityStepState State { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDetail))]
    public partial string Detail { get; set; } = string.Empty;

    public bool IsPending => State == ActivityStepState.Pending;

    public bool IsActive => State == ActivityStepState.Active;

    public bool IsDone => State == ActivityStepState.Done;

    public bool HasDetail => Detail.Length > 0;
}

/// <summary>
/// What the local model is doing right now, so the lawyer never waits in silence: the steps of the task, a running
/// timer, a "waking up the model" state until the first words arrive, then the words as they are written.
/// </summary>
public sealed partial class ModelActivityViewModel : ObservableObject, IDisposable
{
    /// <summary>The live box keeps the end of the text: enough to see the model writing, short enough to stay calm.</summary>
    public const int LiveTextLength = 240;

    /// <summary>Waiting longer than this for the first words usually means the model is being loaded into memory.</summary>
    public static readonly TimeSpan LoadingHintAfter = TimeSpan.FromSeconds(15);

    private readonly Loc _loc;
    private readonly Func<DateTimeOffset> _now;
    private readonly bool _useTimer;
    private readonly bool _showLiveText;
    private CancellationTokenSource? _timer;
    private DateTimeOffset _startedAt;
    private DateTimeOffset _waitingSince;

    /// <param name="showLiveText">False where the text already appears elsewhere as it is written (the draft, the assistant).</param>
    public ModelActivityViewModel(Loc loc, bool showLiveText = true, Func<DateTimeOffset>? now = null, bool useTimer = true)
    {
        _loc = loc;
        _showLiveText = showLiveText;
        _now = now ?? (() => DateTimeOffset.Now);
        _useTimer = useTimer;
    }

    public ObservableCollection<ActivityStepViewModel> Steps { get; } = [];

    [ObservableProperty]
    public partial bool IsActive { get; private set; }

    /// <summary>A model request is on its way and no word has come back yet.</summary>
    [ObservableProperty]
    public partial bool IsWaiting { get; private set; }

    [ObservableProperty]
    public partial string WaitingText { get; private set; } = string.Empty;

    /// <summary>Time since the task started, as m:ss.</summary>
    [ObservableProperty]
    public partial string Elapsed { get; private set; } = "0:00";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveText))]
    public partial string LiveText { get; private set; } = string.Empty;

    public bool HasLiveText => _showLiveText && LiveText.Length > 0;

    [ObservableProperty]
    public partial string WordsText { get; private set; } = string.Empty;

    public void Start(IEnumerable<string> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Steps.Clear();
        foreach (var label in steps)
        {
            Steps.Add(new ActivityStepViewModel(label));
        }

        _startedAt = _now();
        LiveText = string.Empty;
        WordsText = string.Empty;
        IsWaiting = false;
        IsActive = true;
        Tick();
        if (_useTimer)
        {
            StopTimer();
            _timer = new CancellationTokenSource();
            _ = RunTimerAsync(_timer.Token);
        }
    }

    /// <summary>Makes step <paramref name="index"/> the current one; earlier steps are done.</summary>
    /// <param name="waitsForModel">The step sends a request to the model (shows the waiting state until words arrive).</param>
    public void BeginStep(int index, bool waitsForModel = true)
    {
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].State = i < index ? ActivityStepState.Done : i == index ? ActivityStepState.Active : ActivityStepState.Pending;
        }

        LiveText = string.Empty;
        WordsText = string.Empty;
        IsWaiting = waitsForModel;
        _waitingSince = _now();
        Tick();
    }

    public void SetDetail(int index, string detail)
    {
        if (index >= 0 && index < Steps.Count)
        {
            Steps[index].Detail = detail;
        }
    }

    /// <summary>The model's answer so far for the current step.</summary>
    public void OnText(string textSoFar)
    {
        textSoFar ??= string.Empty;
        IsWaiting = false;
        var trimmed = textSoFar.TrimEnd();
        LiveText = trimmed.Length <= LiveTextLength ? trimmed : "…" + trimmed[^LiveTextLength..].TrimStart();
        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        WordsText = words == 0 ? string.Empty : _loc.Format("ActivityWords", words);
    }

    /// <summary>The task finished: every step is done and the panel hides.</summary>
    public void Complete()
    {
        foreach (var step in Steps)
        {
            step.State = ActivityStepState.Done;
        }

        Stop();
    }

    /// <summary>The task ended early (cancelled or failed): the panel hides.</summary>
    public void Stop()
    {
        StopTimer();
        IsWaiting = false;
        IsActive = false;
    }

    /// <summary>Updates the timer and the waiting hint (every second while active).</summary>
    internal void Tick()
    {
        var elapsed = _now() - _startedAt;
        Elapsed = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}";
        WaitingText = _loc[_now() - _waitingSince >= LoadingHintAfter ? "ActivityLoading" : "ActivityWaking"];
    }

    internal void Relocalize() => Tick();

    public void Dispose() => StopTimer();

    /// <summary>
    /// Ticks every second on the caller's context (the UI thread in the app). No Avalonia dispatcher is touched, so a
    /// view model used off the UI thread (tests) never claims it.
    /// </summary>
    private async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                Tick();
            }
        }
        catch (OperationCanceledException)
        {
            // stopped
        }
    }

    private void StopTimer()
    {
        _timer?.Cancel();
        _timer?.Dispose();
        _timer = null;
    }
}
