using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Storage;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class HomeViewModel : ObservableObject, IDisposable
{
    private readonly RecentCasesStore _recent;
    private readonly ProfileStore _profiles;
    private readonly IHomeWorkspace? _workspace;
    private readonly Func<Task> _openProfile;
    private readonly Func<string, bool> _exists;
    private readonly HomeGuideActions? _guides;
    private bool _busy;
    private RecentCaseItem? _missing;

    public HomeViewModel(RecentCasesStore recent, ProfileStore profiles, Func<Task> openProfile,
        IHomeWorkspace? workspace = null, string language = "ar", Func<string, bool>? exists = null, Loc? localization = null,
        HomeGuideActions? guides = null)
    {
        ArgumentNullException.ThrowIfNull(recent);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(openProfile);
        _recent = recent;
        _profiles = profiles;
        _openProfile = openProfile;
        _workspace = workspace;
        _exists = exists ?? File.Exists;
        _guides = guides;
        L = new HomeLocalization(localization);
        if (localization is null) L.SetLanguage(language);
        L.PropertyChanged += LanguageChanged;
        if (workspace is not null) workspace.PropertyChanged += WorkspaceChanged;
        foreach (var command in new[] { workspace?.NewCommand, workspace?.OpenCommand, workspace?.RestoreLastSessionCommand })
            if (command is not null) command.CanExecuteChanged += CommandChanged;
        Reload();
    }

    public HomeLocalization L { get; }
    public ObservableCollection<RecentCaseItem> RecentCases { get; } = [];
    public bool IsRightToLeft => L.IsRightToLeft;
    public bool IsArabic => L.Language == "ar";
    public bool IsEnglish => L.Language == "en";
    public bool HasRecent => RecentCases.Count > 0;
    public bool HasNoRecent => !HasRecent;
    public bool HasRecoverableDraft => _workspace?.HasRecoverableDraft == true;
    public bool HasOpenCase => _workspace?.HasOpenCase == true;
    public bool WorkspaceUnavailable => _workspace?.NewCommand is null || _workspace.OpenCommand is null;
    public bool RecentOpeningUnavailable => HasRecent && _workspace?.OpenRecent is null;
    public string Message => _missing is not null ? L.Format("RecentMissing", _missing.Path) : L[MessageKey];
    public bool HasMessage => _missing is not null || MessageKey.Length > 0;
    public bool HasMissingFile => _missing is not null;
    public bool CanInteract => !IsConfirmingClear && !_busy;

    [ObservableProperty] public partial bool IsProfileIncomplete { get; private set; }
    [ObservableProperty] public partial bool IsConfirmingClear { get; private set; }
    [ObservableProperty] public partial string MessageKey { get; private set; } = string.Empty;

    public void Reload()
    {
        var profile = _profiles.Load();
        IsProfileIncomplete = string.IsNullOrWhiteSpace(profile.NameAr) || profile.Gender is not ("male" or "female");
        RecentCases.Clear();
        foreach (var entry in _recent.Load()) RecentCases.Add(new RecentCaseItem(entry, this, _exists(entry.Path)));
        foreach (var property in new[] { nameof(HasRecent), nameof(HasNoRecent), nameof(RecentOpeningUnavailable), nameof(HasRecoverableDraft), nameof(HasOpenCase) }) OnPropertyChanged(property);
        NotifyCommands();
    }

    [RelayCommand] private void SwitchLanguage(string language)
    {
        L.SetLanguage(language == "en" ? "en" : "ar");
        if (!new WelcomePreferences(_profiles.Root).SaveLanguage(L.Language)) SetMessage("HomeStorageFailed");
    }

    private void LanguageChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(Loc.Language)) return;
        foreach (var entry in RecentCases) entry.Relocalize();
        OnPropertyChanged(nameof(IsRightToLeft));
        OnPropertyChanged(nameof(IsArabic));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(Message));
    }

    private bool CanNew() => CanInteract && _workspace?.NewCommand?.CanExecute(null) == true;
    private bool CanOpen() => CanInteract && _workspace?.OpenCommand?.CanExecute(null) == true;
    private bool CanResume() => CanInteract && HasRecoverableDraft && _workspace?.RestoreLastSessionCommand?.CanExecute(null) == true;
    private bool CanBack() => CanInteract && HasOpenCase;
    private bool CanGuide() => CanInteract && _guides is not null;
    private bool CanLibrary() => CanInteract && _guides?.Library is not null;
    private bool CanAbout() => CanInteract && _guides?.About is not null;

    [RelayCommand(CanExecute = nameof(CanGuide))] private async Task SampleAsync() => await RunGuideAsync(_guides!.Sample);
    [RelayCommand(CanExecute = nameof(CanGuide))] private async Task TourAsync() => await RunGuideAsync(_guides!.Tour);
    [RelayCommand(CanExecute = nameof(CanGuide))] private async Task HelpAsync() => await RunGuideAsync(_guides!.Help);
    [RelayCommand(CanExecute = nameof(CanLibrary))] private async Task LibraryAsync() => await RunGuideAsync(_guides!.Library!);
    [RelayCommand(CanExecute = nameof(CanAbout))] private async Task AboutAsync() => await RunGuideAsync(_guides!.About!);

    private async Task RunGuideAsync(Func<Task> action)
    {
        _busy = true; OnPropertyChanged(nameof(CanInteract)); NotifyCommands();
        try { await action(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException or DzLegal.Law.LawDataException)
        { SetMessage("HomeActionFailed"); }
        finally { _busy = false; Reload(); OnPropertyChanged(nameof(CanInteract)); NotifyCommands(); }
    }

    [RelayCommand(CanExecute = nameof(CanNew))] private async Task NewPetitionAsync() => await RunWorkspaceAsync(_workspace?.NewCommand);
    [RelayCommand(CanExecute = nameof(CanOpen))] private async Task OpenCaseAsync() => await RunWorkspaceAsync(_workspace?.OpenCommand);
    [RelayCommand(CanExecute = nameof(CanResume))] private async Task ResumeAsync() => await RunWorkspaceAsync(_workspace?.RestoreLastSessionCommand);
    [RelayCommand(CanExecute = nameof(CanBack))] private void BackToCase() => _workspace?.ShowCase();

    private async Task RunWorkspaceAsync(ICommand? command)
    {
        if (command?.CanExecute(null) != true || _workspace is null) return;
        _busy = true;
        OnPropertyChanged(nameof(CanInteract));
        NotifyCommands();
        try
        {
            // Expose the original workspace before invoking its own confirmation/picker flow.
            _workspace.ShowCase();
            if (command is IAsyncRelayCommand asyncCommand) await asyncCommand.ExecuteAsync(null);
            else command.Execute(null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException)
        { SetMessage("HomeActionFailed"); }
        finally { _busy = false; Reload(); OnPropertyChanged(nameof(CanInteract)); }
    }

    [RelayCommand] private void NotNow()
    {
        if (!HasRecoverableDraft || _workspace?.DismissRecoveryCommand?.CanExecute(null) != true) return;
        _workspace.DismissRecoveryCommand.Execute(null);
        Reload();
    }

    [RelayCommand] private async Task ProfileAsync()
    {
        try { await _openProfile(); Reload(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        { SetMessage("HomeActionFailed"); }
    }

    public async Task OpenRecentAsync(RecentCaseItem entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!_exists(entry.Path))
        {
            _missing = entry;
            OnPropertyChanged(nameof(Message)); OnPropertyChanged(nameof(HasMessage)); OnPropertyChanged(nameof(HasMissingFile));
            return;
        }
        if (_workspace?.OpenRecent is not { } open) return;
        try { _workspace.ShowCase(); await open(entry.Path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException)
        { SetMessage("HomeActionFailed"); }
        Reload();
    }

    public bool CanOpenRecent(RecentCaseItem entry) => CanInteract && (!entry.Exists || _workspace?.OpenRecent is not null);
    public void Remove(RecentCaseItem entry)
    {
        if (!_recent.Remove(entry.Path)) { SetMessage("HomeStorageFailed"); return; }
        _missing = null; MessageKey = string.Empty;
        OnPropertyChanged(nameof(Message)); OnPropertyChanged(nameof(HasMessage)); OnPropertyChanged(nameof(HasMissingFile)); Reload();
    }
    [RelayCommand] private void RemoveMissing() { if (_missing is { } entry) Remove(entry); }
    [RelayCommand] private void ClearRecent() { if (HasRecent) IsConfirmingClear = true; }
    [RelayCommand] private void CancelClear() => IsConfirmingClear = false;
    [RelayCommand] private void ConfirmClear()
    {
        if (!_recent.Clear()) { SetMessage("HomeStorageFailed"); IsConfirmingClear = false; return; }
        IsConfirmingClear = false; _missing = null; MessageKey = string.Empty; Reload();
        OnPropertyChanged(nameof(Message)); OnPropertyChanged(nameof(HasMessage)); OnPropertyChanged(nameof(HasMissingFile));
    }
    partial void OnIsConfirmingClearChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInteract));
        NotifyCommands();
        foreach (var entry in RecentCases) entry.OpenCommand.NotifyCanExecuteChanged();
    }
    partial void OnMessageKeyChanged(string value) { OnPropertyChanged(nameof(Message)); OnPropertyChanged(nameof(HasMessage)); }
    private void SetMessage(string key) { _missing = null; MessageKey = key; OnPropertyChanged(nameof(HasMissingFile)); OnPropertyChanged(nameof(Message)); }
    private void WorkspaceChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasRecoverableDraft)); OnPropertyChanged(nameof(HasOpenCase)); NotifyCommands();
    }
    private void CommandChanged(object? sender, EventArgs e) => NotifyCommands();
    private void NotifyCommands()
    {
        NewPetitionCommand.NotifyCanExecuteChanged(); OpenCaseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged(); BackToCaseCommand.NotifyCanExecuteChanged();
        SampleCommand.NotifyCanExecuteChanged(); TourCommand.NotifyCanExecuteChanged(); HelpCommand.NotifyCanExecuteChanged();
        LibraryCommand.NotifyCanExecuteChanged(); AboutCommand.NotifyCanExecuteChanged();
    }
    public void Dispose()
    {
        L.PropertyChanged -= LanguageChanged;
        if (_workspace is not null) _workspace.PropertyChanged -= WorkspaceChanged;
        foreach (var command in new[] { _workspace?.NewCommand, _workspace?.OpenCommand, _workspace?.RestoreLastSessionCommand })
            if (command is not null) command.CanExecuteChanged -= CommandChanged;
    }
}

public sealed class RecentCaseItem : ObservableObject
{
    private readonly HomeViewModel _home;
    private readonly RecentCase _entry;
    public RecentCaseItem(RecentCase entry, HomeViewModel home, bool exists)
    {
        _entry = entry; _home = home; Exists = exists;
        OpenCommand = new AsyncRelayCommand(() => home.OpenRecentAsync(this), () => home.CanOpenRecent(this));
        RemoveCommand = new RelayCommand(() => home.Remove(this));
    }
    public string Path => _entry.Path;
    public string FileName => _entry.FileName;
    public string Subject => _entry.Subject;
    public bool Exists { get; }
    public string OpenedText => _home.L.Format("RecentOpened", _entry.LastOpened.ToLocalTime().ToString("g", _home.L.Culture));
    public string RemoveLabel => _home.L["RecentRemove"];
    public IAsyncRelayCommand OpenCommand { get; }
    public IRelayCommand RemoveCommand { get; }
    public void Relocalize() { OnPropertyChanged(nameof(OpenedText)); OnPropertyChanged(nameof(RemoveLabel)); }
}
