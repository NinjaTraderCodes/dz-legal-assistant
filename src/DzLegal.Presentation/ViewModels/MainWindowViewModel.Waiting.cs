using CommunityToolkit.Mvvm.ComponentModel;
using DzLegal.Drafting;
using DzLegal.Presentation.Services;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// Making the local model's minutes easier to wait through: how long is left (measured on this computer), a note on
/// battery or power saver, and the analysis started while the lawyer fills the next steps.
/// </summary>
public sealed partial class MainWindowViewModel
{
    public const string AnalysisTask = "analysis";
    public const string DraftTask = "draft";

    /// <summary>Durations measured on this computer; in memory unless the app gives a file.</summary>
    public ITaskTimings Timings { get; init; } = new TaskTimings();

    public IPowerStatus Power { get; init; } = new NoPowerStatus();

    /// <summary>
    /// Start the grounds analysis when the lawyer leaves the facts step, so it runs while they fill the next steps. Only
    /// with a local engine that is ready: nothing leaves the computer without the lawyer's click.
    /// </summary>
    public bool AnalyseInBackground { get; init; }

    /// <summary>The running analysis was started on leaving the facts step, not by the lawyer's click.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsBackgroundAnalysis))]
    public partial bool IsAnalysisAutomatic { get; set; }

    /// <summary>The automatic analysis runs while the lawyer is on another step: a small line under the steps says so.</summary>
    public bool ShowsBackgroundAnalysis => IsAnalysisAutomatic && !IsGroundsStep;

    private bool IsLocalModel => SelectedProvider is { Key: not StubProviderKey, SendsCaseData: false };

    /// <summary>What the activity panel can promise for <paramref name="task"/> with this engine.</summary>
    /// <param name="usual">False for a shorter run than usual (a draft keeping edited sections): no estimate then.</param>
    private ActivityExpectation Expect(string task, IModelProvider provider, bool usual = true)
    {
        if (SelectedProvider?.Key == StubProviderKey)
        {
            return new ActivityExpectation();
        }

        var local = IsLocalModel;
        return new ActivityExpectation(
            usual ? Timings.Typical(task, provider.Name) : null,
            usual && local ? task == AnalysisTask ? "AnalysisFirstRun" : "DraftFirstRun" : null,
            local && Power.IsSavingPower());
    }

    /// <summary>Short tasks (one section, a proposal): no estimate, only the power note.</summary>
    private ActivityExpectation PowerOnly() => new(SavingPower: IsLocalModel && Power.IsSavingPower());

    /// <summary>Keeps the duration of a task that ran to the end with a model (never the template mode).</summary>
    private void RecordDuration(string task, IModelProvider provider, ModelActivityViewModel activity)
    {
        if (SelectedProvider?.Key != StubProviderKey)
        {
            Timings.Record(task, provider.Name, activity.ElapsedTime);
        }
    }

    partial void OnSelectedStepChanged(FieldGroupViewModel? oldValue, FieldGroupViewModel? newValue)
    {
        if (oldValue?.Id == FactsStepId && newValue?.Id != FactsStepId && ShouldAnalyseInBackground())
        {
            IsAnalysisAutomatic = true;
            AnalyseGroundsCommand.Execute(null);
        }
    }

    private bool ShouldAnalyseInBackground() =>
        AnalyseInBackground
        && IsLocalModel
        && ModelStatus.State == ModelState.Ready
        && CanAnalyseGrounds
        && _analysis is null
        && !AnalyseGroundsCommand.IsRunning
        && CurrentInputs().GetValueOrDefault(FactsField, string.Empty).Trim().Length > 0;
}
