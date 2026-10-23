using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DzLegal.Presentation.ViewModels;

/// <summary>Presentation-only workbench navigation. All case and legal commands remain unchanged.</summary>
public sealed partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOutlineExpanded))]
    public partial bool IsOutlineCompact { get; set; }

    public bool IsOutlineExpanded => !IsOutlineCompact;

    [ObservableProperty]
    public partial bool IsOutlineVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsDraftMaximized { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDraftDocked))]
    public partial bool IsDraftDetached { get; set; }

    public bool IsDraftDocked => !IsDraftDetached;

    [ObservableProperty]
    public partial bool IsToolsOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReviewTool), nameof(IsSourceTool), nameof(IsAssistantTool), nameof(IsActivityTool))]
    public partial string WorkbenchTool { get; set; } = "review";

    public bool IsReviewTool => WorkbenchTool == "review";
    public bool IsSourceTool => WorkbenchTool == "source";
    public bool IsAssistantTool => WorkbenchTool == "assistant";
    public bool IsActivityTool => WorkbenchTool == "activity";

    public string CaseTitle => Field("claim.subject").Value is { Length: > 0 } subject ? subject : L["Document"];

    public int ExportBlockerCount => ExportBlockers.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

    public ModelActivityViewModel WorkbenchActivity => AnalysisActivity.IsActive ? AnalysisActivity
        : AssistantActivity.IsActive ? AssistantActivity : GenerationActivity;

    public bool HasWorkbenchActivity => AnalysisActivity.IsActive || GenerationActivity.IsActive || AssistantActivity.IsActive;

    private bool _watchWorkbenchActivity;

    private void InitializeWorkbench()
    {
        PropertyChanged += WorkbenchChanged;
        Field("claim.subject").PropertyChanged += WorkbenchSubjectChanged;
        AnalysisActivity.PropertyChanged += WorkbenchActivityChanged;
        GenerationActivity.PropertyChanged += WorkbenchActivityChanged;
        AssistantActivity.PropertyChanged += WorkbenchActivityChanged;
    }

    private void DisposeWorkbench()
    {
        PropertyChanged -= WorkbenchChanged;
        Field("claim.subject").PropertyChanged -= WorkbenchSubjectChanged;
        AnalysisActivity.PropertyChanged -= WorkbenchActivityChanged;
        GenerationActivity.PropertyChanged -= WorkbenchActivityChanged;
        AssistantActivity.PropertyChanged -= WorkbenchActivityChanged;
    }

    private void WorkbenchSubjectChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FieldViewModel.Value))
        {
            OnPropertyChanged(nameof(CaseTitle));
        }
    }

    private void WorkbenchChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExportBlockers))
        {
            OnPropertyChanged(nameof(ExportBlockerCount));
        }
        else if (e.PropertyName == nameof(IsExportBlockedShown) && IsExportBlockedShown)
        {
            ShowWorkbenchTool("review");
        }
        else if ((e.PropertyName == nameof(IsAssistantOpen) && IsAssistantOpen)
            || (e.PropertyName == nameof(HasProposal) && HasProposal))
        {
            ShowWorkbenchTool("assistant");
        }
        else if (e.PropertyName == nameof(IsRightToLeft))
        {
            OnPropertyChanged(nameof(CaseTitle));
        }
    }

    private void WorkbenchActivityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ModelActivityViewModel.IsActive))
        {
            return;
        }

        OnPropertyChanged(nameof(WorkbenchActivity));
        OnPropertyChanged(nameof(HasWorkbenchActivity));
        if (HasWorkbenchActivity && _watchWorkbenchActivity)
        {
            ShowWorkbenchTool("activity");
        }
    }

    [RelayCommand]
    private void ShowWorkbenchTool(string? tool)
    {
        if (tool is not ("review" or "source" or "assistant" or "activity"))
        {
            return;
        }

        WorkbenchTool = tool;
        IsToolsOpen = true;
        IsDraftMaximized = false;
        if (tool == "activity")
        {
            _watchWorkbenchActivity = true;
        }
    }

    [RelayCommand]
    private void HideWorkbenchTools()
    {
        IsToolsOpen = false;
        _watchWorkbenchActivity = false;
    }

    [RelayCommand]
    private void ToggleOutline() => IsOutlineVisible = !IsOutlineVisible;

    [RelayCommand]
    private void ToggleOutlineCompact() => IsOutlineCompact = !IsOutlineCompact;

    [RelayCommand]
    private void ToggleDraftFocus() => IsDraftMaximized = !IsDraftMaximized;

    [RelayCommand]
    private void ToggleDraftWindow() => IsDraftDetached = !IsDraftDetached;
}
