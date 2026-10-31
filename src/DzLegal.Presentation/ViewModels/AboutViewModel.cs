using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Law;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// The About page. The version comes from the caller (the assembly's informational version), and
/// the legal-sources line is counted from the loaded library, so neither can drift from the build.
/// </summary>
public sealed partial class AboutViewModel : ObservableObject, IDisposable
{
    private readonly Func<string?> _loadNotices;
    private readonly Func<Task>? _openLibrary;
    private readonly Func<Task>? _goHome;
    private string? _notices;

    public AboutViewModel(
        Loc localization,
        string version,
        LawLibrary library,
        bool onlineEngineOffered,
        Func<string?> loadNotices,
        Func<Task>? openLibrary = null,
        Func<Task>? goHome = null)
    {
        L = localization;
        Version = version;
        ArticleCount = library.Codes.Sum(code => code.Articles.Count);
        TextCount = library.Codes.Count;
        ShowsOnlineNotice = onlineEngineOffered;
        _loadNotices = loadNotices;
        _openLibrary = openLibrary;
        _goHome = goHome;
        L.PropertyChanged += LanguageChanged;
    }

    public Loc L { get; }
    public string Version { get; }
    public int ArticleCount { get; }
    public int TextCount { get; }
    public bool ShowsOnlineNotice { get; }
    public bool IsRightToLeft => L.IsRightToLeft;
    public bool CanOpenLibrary => _openLibrary is not null;
    public bool CanGoHome => _goHome is not null;

    public string Title => IsShowingNotices ? L["AboutNotices"] : L["AboutTitle"];
    public string VersionText => L.Format("AboutVersion", Version);
    public string SourcesText => L.Format("AboutSources", ArticleCount, TextCount);
    public IReadOnlyList<string> Team => [L["AboutTeamLead"], L["AboutTeamDesign"], L["AboutTeamLegal"]];

    /// <summary>What ships with the app; THIRD-PARTY-NOTICES.txt holds the full text of each licence.</summary>
    public IReadOnlyList<AboutLicenceViewModel> Licences =>
    [
        new("Noto Naskh Arabic", "SIL Open Font License 1.1"),
        new("Inter", "SIL Open Font License 1.1"),
        new("Fluent UI System Icons, FluentIcons.Avalonia", "MIT"),
        new(L["AboutWelcomeImage"], L["AboutOwnImage"]),
        new("Avalonia", "MIT"),
        new("Avalonia.Angle.Windows.Natives (ANGLE)", "BSD 3-Clause"),
        new("CommunityToolkit.Mvvm", "MIT"),
        new("DocumentFormat.OpenXml, System.IO.Packaging", "MIT"),
        new("SkiaSharp, HarfBuzzSharp", "MIT"),
        new("MicroCom.Runtime, Tmds.DBus.Protocol", "MIT"),
        new("Scriban", "BSD 2-Clause"),
        new("QuestPDF", "QuestPDF Community License"),
    ];

    [ObservableProperty] public partial bool IsShowingNotices { get; private set; }

    public bool HasNotices => (_notices ??= _loadNotices()) is { Length: > 0 };

    /// <summary>The licence texts are English, whatever the interface language.</summary>
    public string NoticesText => HasNotices ? _notices! : L["AboutNoticesMissing"];

    [RelayCommand] private void ShowNotices() => IsShowingNotices = true;
    [RelayCommand] private void Back() => IsShowingNotices = false;

    [RelayCommand(CanExecute = nameof(CanOpenLibrary))]
    private async Task LibraryAsync()
    {
        if (_openLibrary is not null) await _openLibrary();
    }

    [RelayCommand(CanExecute = nameof(CanGoHome))]
    private async Task HomeAsync()
    {
        if (_goHome is not null) await _goHome();
    }

    partial void OnIsShowingNoticesChanged(bool value)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(NoticesText));
    }

    private void LanguageChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(Loc.Language)) return;
        foreach (var name in new[] { nameof(IsRightToLeft), nameof(Title), nameof(VersionText), nameof(SourcesText), nameof(Team), nameof(Licences), nameof(NoticesText) })
            OnPropertyChanged(name);
    }

    public void Dispose() => L.PropertyChanged -= LanguageChanged;
}

public sealed record AboutLicenceViewModel(string Name, string Licence);
