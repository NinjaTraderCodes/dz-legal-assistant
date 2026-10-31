using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Drafting;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class MainWindowViewModel
{
    private TourViewModel? _tour;
    private SampleCase? _pendingSample;
    private Action? _pendingAfterNew;
    private CancellationTokenSource? _sampleProbe;
    private bool _guideNavigating;

    public TourViewModel Tour => _tour ??= new TourViewModel(this);
    public bool HasUnreviewedAiSections => Sections.Any(NeedsAiReview);
    private static bool NeedsAiReview(SectionViewModel section) => section.NeedsReview;

    [ObservableProperty] public partial bool IsTourActive { get; internal set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSampleGuideVisible), nameof(IsCheckPanelVisible), nameof(WatermarkHint))]
    public partial bool IsSampleCase { get; private set; }
    [ObservableProperty] public partial SampleGuideViewModel? SampleGuide { get; private set; }
    [ObservableProperty] public partial bool SampleFactsOpened { get; private set; }
    [ObservableProperty] public partial bool SampleExportSucceeded { get; private set; }
    [ObservableProperty] public partial bool SampleLocalModelAvailable { get; private set; }
    [ObservableProperty] public partial bool IsCheckingSampleModel { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSampleGuideVisible), nameof(IsCheckPanelVisible))]
    public partial bool ShowSampleChecks { get; private set; }

    public bool IsSampleGuideVisible => IsSampleCase && !ShowSampleChecks && !IsAssistantOpen;
    public bool IsCheckPanelVisible => !IsAssistantOpen && !IsSampleGuideVisible;
    public string WatermarkHint => L[IsSampleCase ? "SampleWatermarkForced" : "Watermark"];
    [RelayCommand] private void ShowSampleGuide() => ShowSampleChecks = false;
    [RelayCommand] private void ShowSampleCheck() => ShowSampleChecks = true;

    public void StartTour(bool hasOpenCase = true)
    {
        if (hasOpenCase) Tour.Start();
        else RequestNew(Tour.Start);
    }

    public async Task OpenSampleAsync(SampleCase sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (AskBeforeReplacing(ReplaceAction.Sample)) _pendingSample = sample;
        else await OpenSampleCoreAsync(sample);
    }

    private async Task OpenSampleCoreAsync(SampleCase sample)
    {
        OpenCase(sample.Inputs);
        SampleFactsOpened = false;
        SampleExportSucceeded = false;
        SampleLocalModelAvailable = false;
        IsCheckingSampleModel = true;
        ShowSampleChecks = false;
        IsSampleCase = true;
        DraftWatermark = true;
        SampleGuide = new SampleGuideViewModel(this);
        await ProbeSampleModelAsync();
    }

    internal void NavigateGuideStep(string id)
    {
        _guideNavigating = true;
        try { SelectedStep = FieldGroups.First(group => group.Id == id); }
        finally { _guideNavigating = false; }
    }

    private async Task ProbeSampleModelAsync()
    {
        if (!IsSampleCase || _sampleProbe is not null) return;
        var guide = SampleGuide;
        var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        _sampleProbe = timeout;
        var token = timeout.Token;
        var available = false;
        try
        {
            foreach (var option in Providers.Where(option => option.Key != StubProviderKey && !option.SendsCaseData))
            {
                if ((await option.ProbeAsync(token)).State != ModelState.Ready) continue;
                available = true;
                break;
            }
        }
        catch (Exception ex) when (ex is ModelProviderException or HttpRequestException or OperationCanceledException or IOException)
        { available = false; }
        finally
        {
            if (ReferenceEquals(_sampleProbe, timeout)) { _sampleProbe = null; timeout.Dispose(); }
            if (IsSampleCase && ReferenceEquals(guide, SampleGuide))
            {
                SampleLocalModelAvailable = available;
                IsCheckingSampleModel = false;
            }
        }
    }

    private void EndSample()
    {
        if (!IsSampleCase) return;
        _sampleProbe?.Cancel(); _sampleProbe?.Dispose(); _sampleProbe = null;
        SampleGuide?.Dispose(); SampleGuide = null;
        IsSampleCase = false;
        SampleFactsOpened = false;
        SampleExportSucceeded = false;
        IsCheckingSampleModel = false;
        DraftWatermark = _profileDefaults?.WatermarkByDefault ?? true;
    }
}
