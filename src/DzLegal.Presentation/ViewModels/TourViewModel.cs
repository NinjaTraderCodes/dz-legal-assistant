using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class TourViewModel : ObservableObject, IDisposable
{
    private static readonly string[][] Anchors =
    [
        ["StepsList"], ["FormPanel"], ["DraftPanel"], ["GroundsPanel"],
        ["CheckPanel"], ["ExportPdfButton", "ExportWordButton"],
    ];
    private readonly MainWindowViewModel _owner;
    private FieldGroupViewModel? _originalStep;

    public TourViewModel(MainWindowViewModel owner)
    {
        _owner = owner;
        L.PropertyChanged += LanguageChanged;
    }

    public Loc L => _owner.L;
    public bool IsRightToLeft => L.IsRightToLeft;
    public string Title => L[$"Tour{Index + 1}Title"];
    public string Text => L[$"Tour{Index + 1}Text"];
    public string Counter => L.Format("TourCounter", Index + 1, Anchors.Length);
    public string Announcement => Title + ". " + Text + " " + Counter;
    public string NextText => L[IsLast ? "TourFinish" : "TourNext"];
    public bool IsLast => Index == Anchors.Length - 1;
    public IReadOnlyList<string> AnchorNames => Anchors[Index];
    public event EventHandler? Opening;
    public event EventHandler? Closed;

    [ObservableProperty] public partial bool IsOpen { get; private set; }
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    public partial int Index { get; private set; }

    public void Start()
    {
        if (IsOpen) return;
        Opening?.Invoke(this, EventArgs.Empty);
        _originalStep = _owner.SelectedStep;
        _owner.IsTourActive = true;
        Index = 0;
        IsOpen = true;
        Refresh();
    }

    private bool CanBack() => Index > 0;
    [RelayCommand(CanExecute = nameof(CanBack))] private void Back() => Index--;
    [RelayCommand] private void Next() { if (IsLast) Close(); else Index++; }
    [RelayCommand] private void Skip() => Close();

    public void Close()
    {
        if (!IsOpen) return;
        _owner.SelectedStep = _originalStep;
        IsOpen = false;
        _owner.IsTourActive = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIndexChanged(int value)
    {
        if (IsOpen && value == 3)
            _owner.SelectedStep = _owner.FieldGroups.First(group => group.Id == MainWindowViewModel.GroundsStepId);
        Refresh();
    }

    private void LanguageChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Loc.Language)) Refresh();
    }

    private void Refresh()
    {
        foreach (var name in new[] { nameof(Title), nameof(Text), nameof(Counter), nameof(Announcement), nameof(NextText), nameof(IsLast), nameof(AnchorNames), nameof(IsRightToLeft) })
            OnPropertyChanged(name);
    }

    public void Dispose() { Close(); L.PropertyChanged -= LanguageChanged; }
}
