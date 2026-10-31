using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class HelpViewModel : ObservableObject, IDisposable
{
    private readonly Func<Task> _startTour;
    private readonly Func<Task>? _openLibrary;

    public HelpViewModel(Loc localization, Func<Task> startTour, Func<Task>? openLibrary = null)
    {
        L = localization;
        _startTour = startTour;
        _openLibrary = openLibrary;
        Pages = Enumerable.Range(1, 7).Select(number => new HelpPageViewModel(number, L, Select)).ToArray();
        L.PropertyChanged += LanguageChanged;
    }

    public Loc L { get; }
    public IReadOnlyList<HelpPageViewModel> Pages { get; }
    public bool IsRightToLeft => L.IsRightToLeft;
    public bool IsIndex => SelectedPage is null;
    public bool IsLibraryPage => SelectedPage?.Number == 7;
    public bool CanOpenLibrary => _openLibrary is not null;
    public bool ShowLibraryPageLink => CanOpenLibrary && IsLibraryPage;
    public string Title => SelectedPage?.Title ?? L["HelpTitle"];
    public IReadOnlyList<HelpParagraphViewModel> Paragraphs => SelectedPage?.Paragraphs ?? [];
    [ObservableProperty] public partial HelpPageViewModel? SelectedPage { get; private set; }

    public void OpenPage(int number) => Select(Pages.Single(page => page.Number == number));
    private void Select(HelpPageViewModel page) => SelectedPage = page;
    [RelayCommand] private void Back() => SelectedPage = null;
    [RelayCommand] private async Task TourAsync() => await _startTour();
    [RelayCommand(CanExecute = nameof(CanOpenLibrary))]
    private async Task LibraryAsync()
    {
        if (_openLibrary is not null) await _openLibrary();
    }
    partial void OnSelectedPageChanged(HelpPageViewModel? value) => Refresh();

    private void LanguageChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(Loc.Language)) return;
        foreach (var page in Pages) page.Relocalize();
        Refresh();
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(IsIndex)); OnPropertyChanged(nameof(IsLibraryPage)); OnPropertyChanged(nameof(ShowLibraryPageLink));
        OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(Paragraphs)); OnPropertyChanged(nameof(IsRightToLeft));
    }

    public void Dispose() => L.PropertyChanged -= LanguageChanged;
}

public sealed class HelpPageViewModel : ObservableObject
{
    private static readonly int[] Counts = [3, 5, 3, 1, 4, 6, 4];
    private readonly Loc _localization;
    public HelpPageViewModel(int number, Loc localization, Action<HelpPageViewModel> open)
    {
        Number = number;
        _localization = localization;
        OpenCommand = new RelayCommand(() => open(this));
    }
    public int Number { get; }
    public string Title => _localization[$"Help{Number}Title"];
    public IReadOnlyList<HelpParagraphViewModel> Paragraphs => Enumerable.Range(1, Counts[Number - 1])
        .Select(index => new HelpParagraphViewModel(_localization[$"Help{Number}Body{index}"])).ToArray();
    public IRelayCommand OpenCommand { get; }
    public void Relocalize() { OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(Paragraphs)); }
}

public sealed record HelpParagraphViewModel(string Text);
