using System.ComponentModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DzLegal.Presentation.ViewModels;

public sealed class SampleGuideViewModel : ObservableObject, IDisposable
{
    private readonly MainWindowViewModel _owner;
    private readonly HashSet<SectionViewModel> _observed = [];
    private int _currentTask = -2;

    public SampleGuideViewModel(MainWindowViewModel owner)
    {
        _owner = owner;
        Tasks = Enumerable.Range(1, 6).Select(number => new SampleTaskViewModel(number, L, Navigate)).ToArray();
        owner.PropertyChanged += Changed;
        owner.Sections.CollectionChanged += SectionsChanged;
        L.PropertyChanged += Changed;
        ObserveSections();
        Refresh();
    }

    public Loc L => _owner.L;
    public IReadOnlyList<SampleTaskViewModel> Tasks { get; }
    public bool IsRightToLeft => L.IsRightToLeft;
    public bool NeedsLocalModel => !_owner.IsCheckingSampleModel && !_owner.SampleLocalModelAvailable && !(_owner.Analysis is not null && !_owner.IsAnalysisStale);
    public bool NeedsManualCompletion => NeedsLocalModel && _owner.HasGeneratedDraft && _owner.HasExportBlockers;
    public bool IsDone => Tasks.Where(task => !task.NeedsModel).All(task => task.IsComplete);
    public string Counter => L.Format("TourCounter", Tasks.Count(task => !task.NeedsModel && task.IsComplete), Tasks.Count(task => !task.NeedsModel));

    private void Navigate(int number) => _owner.NavigateGuideStep(number == 1 ? MainWindowViewModel.FactsStepId : MainWindowViewModel.GroundsStepId);
    private void Changed(object? sender, PropertyChangedEventArgs args) => Refresh();
    private void SectionsChanged(object? sender, NotifyCollectionChangedEventArgs args) { ObserveSections(); Refresh(); }
    private void ObserveSections()
    {
        foreach (var section in _observed) section.PropertyChanged -= Changed;
        _observed.Clear();
        foreach (var section in _owner.Sections) { _observed.Add(section); section.PropertyChanged += Changed; }
    }

    private void Refresh()
    {
        var complete = new[]
        {
            _owner.SampleFactsOpened, _owner.Analysis is not null && !_owner.IsAnalysisStale,
            _owner.IsClaimConfirmed, _owner.HasGeneratedDraft,
            !_owner.HasUnreviewedAiSections, _owner.SampleExportSucceeded,
        };
        for (var index = 0; index < Tasks.Count; index++)
        {
            Tasks[index].Update(complete[index], index is 1 or 2 && NeedsLocalModel);
            Tasks[index].Relocalize();
        }
        var current = Enumerable.Range(0, Tasks.Count).FirstOrDefault(index => !Tasks[index].IsComplete && !Tasks[index].NeedsModel, -1);
        if (current != _currentTask)
        {
            _currentTask = current;
            for (var index = 0; index < Tasks.Count; index++) Tasks[index].IsExpanded = index == current;
        }
        foreach (var name in new[] { nameof(IsRightToLeft), nameof(NeedsLocalModel), nameof(NeedsManualCompletion), nameof(IsDone), nameof(Counter) })
            OnPropertyChanged(name);
    }

    public void Dispose()
    {
        _owner.PropertyChanged -= Changed;
        _owner.Sections.CollectionChanged -= SectionsChanged;
        L.PropertyChanged -= Changed;
        foreach (var section in _observed) section.PropertyChanged -= Changed;
        _observed.Clear();
    }
}

public sealed partial class SampleTaskViewModel : ObservableObject
{
    private readonly Loc _localization;
    public SampleTaskViewModel(int number, Loc localization, Action<int> show)
    {
        Number = number; _localization = localization;
        ShowMeCommand = new RelayCommand(() => show(number));
    }
    public int Number { get; }
    public string Title => _localization[$"SampleTask{Number}"];
    public string Hint => _localization[$"SampleTask{Number}Hint"];
    public string ShowMe => _localization["SampleShowMe"];
    public string ModelNote => _localization["SampleNeedsModel"];
    public IRelayCommand ShowMeCommand { get; }
    [ObservableProperty] public partial bool IsComplete { get; private set; }
    [ObservableProperty] public partial bool NeedsModel { get; private set; }
    [ObservableProperty] public partial bool IsExpanded { get; set; }
    internal void Update(bool complete, bool needsModel) { IsComplete = complete; NeedsModel = needsModel && !complete; }
    internal void Relocalize()
    {
        foreach (var name in new[] { nameof(Title), nameof(Hint), nameof(ShowMe), nameof(ModelNote) }) OnPropertyChanged(name);
    }
}
