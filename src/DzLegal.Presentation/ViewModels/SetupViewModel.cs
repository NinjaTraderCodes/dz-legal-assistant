using System.ComponentModel;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.Services;
using DzLegal.Storage;

namespace DzLegal.Presentation.ViewModels;

/// <summary>Three optional setup steps, also reused by the profile editor.</summary>
public sealed partial class SetupViewModel : ObservableObject
{
    private readonly ProfileStore _profileStore;
    private readonly PreferencesStore _preferencesStore;
    private readonly ThemePreferences _themePreferences;
    private readonly WelcomePreferences _welcomePreferences;
    private readonly OnboardingProfileAssets _assets;
    private readonly Action _onFinished;
    private readonly Action _onLater;
    private readonly Action<bool>? _onThemeApplied;
    private readonly Func<string?, Task>? _saveFolderPicker;
    private readonly ISetupFiles? _setupFiles;
    private readonly bool _usesSharedLocalization;
    private LawyerProfile _savedProfile = LawyerProfile.Default;
    private ProfilePreferences _savedPreferences = ProfilePreferences.Default;
    private bool _loading;
    private bool _suggesting;
    private bool _applyingTheme;
    private bool _standingEdited;
    private Action? _pendingLeave;
    private ExportedProfile? _pendingImport;
    private string _initialLanguage = "ar";
    private string _initialTheme = "dark";

    public SetupViewModel(ProfileStore profileStore, PreferencesStore preferencesStore,
        ThemePreferences themePreferences, WelcomePreferences welcomePreferences, IUserFiles files,
        Action onFinished, Action onLater, Func<bool>? tourHandler = null,
        Action<bool>? onThemeApplied = null, Func<string?, Task>? saveFolderPicker = null,
        ISetupFiles? setupFiles = null, bool isEditing = false, Loc? localization = null)
    {
        ArgumentNullException.ThrowIfNull(profileStore);
        ArgumentNullException.ThrowIfNull(preferencesStore);
        ArgumentNullException.ThrowIfNull(themePreferences);
        ArgumentNullException.ThrowIfNull(welcomePreferences);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(onFinished);
        ArgumentNullException.ThrowIfNull(onLater);
        _profileStore = profileStore;
        _preferencesStore = preferencesStore;
        _themePreferences = themePreferences;
        _welcomePreferences = welcomePreferences;
        _assets = new OnboardingProfileAssets(profileStore.Root);
        _onFinished = onFinished;
        _onLater = onLater;
        _onThemeApplied = onThemeApplied;
        _saveFolderPicker = saveFolderPicker;
        _setupFiles = setupFiles;
        L = new SetupLocalization(localization);
        _usesSharedLocalization = localization is not null;
        L.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(Loc.Language)) return;
            _loading = true;
            try { Language = L.Language; }
            finally { _loading = false; }
            RefreshLanguage();
        };
        IsEditing = isEditing;
        GenderOptions = Choices(("", "SetupLater"), ("male", "ProfileGenderMale"), ("female", "ProfileGenderFemale"));
        LanguageOptions = Choices(("ar", "WelcomeArabic"), ("en", "WelcomeEnglish"));
        ThemeOptions = Choices(("light", "PrefsThemeLight"), ("dark", "PrefsThemeDark"));
        EngineOptions = Choices(("ollama", "PrefsEngineLocal"), ("stub", "ProviderStub"));
        FileNamingOptions = Choices(("current", "FileNamingCurrent"), ("parties-date", "FileNamingParties"));
        Reload();
    }

    public SetupLocalization L { get; }
    public const int StepCount = 3;
    public bool IsEditing { get; }
    public bool IsSetup => !IsEditing;
    public bool IsRightToLeft => L.IsRightToLeft;
    public bool ShowLawyerStep => Step == 0;
    public bool ShowOfficeStep => Step == 1;
    public bool ShowPrefsStep => Step == 2;
    public bool IsLastStep => Step == StepCount - 1;
    public bool HasNext => Step < StepCount - 1;
    public bool HasPrevious => Step > 0;
    public bool ShowEditorSave => IsEditing && HasNext;
    public string ProgressText => L.Format("SetupProgress", Step + 1, StepCount);
    public string PageTitle => L[IsEditing ? "ProfileTitle" : "SetupTitle"];
    public string FinishText => L[IsEditing ? "ProfileSave" : "SetupFinish"];
    public string AccreditationLabel => L[Gender switch
    {
        "male" => "ProfileSupremeCourtMale", "female" => "ProfileSupremeCourtFemale", _ => "ProfileSupremeCourt",
    }];
    public bool HasNameError => NameAr.Length > 120 || NameFr.Length > 120;
    public bool IsEmailValid => string.IsNullOrWhiteSpace(Email)
        || MailAddress.TryCreate(Email, out var address) && address.Address == Email && !Email.Any(char.IsWhiteSpace);
    public bool HasEmailError => !IsEmailValid;
    public bool IsValid => !HasNameError && IsEmailValid;
    public bool IsDirty => Profile() != _savedProfile || Preferences() != _savedPreferences;
    public bool CanEdit => !IsConfirmingLeave && !IsConfirmingImport;
    public bool HasLogo => LogoPreview is not null;
    public bool HasMessage => MessageKey.Length > 0;
    public string Message => L[MessageKey];
    public string ImportSummary => _pendingImport is null ? string.Empty : L.Format("ProfileImportSummary",
        _pendingImport.Profile.NameAr, _pendingImport.Profile.Council, _pendingImport.Preferences.Language);

    public IReadOnlyList<SetupChoice> GenderOptions { get; }
    public IReadOnlyList<SetupChoice> LanguageOptions { get; }
    public IReadOnlyList<SetupChoice> ThemeOptions { get; }
    public IReadOnlyList<SetupChoice> EngineOptions { get; }
    public IReadOnlyList<SetupChoice> FileNamingOptions { get; }
    private SetupChoice[] Choices(params (string Value, string Key)[] choices) =>
        choices.Select(c => new SetupChoice(c.Value, c.Key, L)).ToArray();

    [ObservableProperty] public partial int Step { get; set; }
    [ObservableProperty] public partial string NameAr { get; set; } = string.Empty;
    [ObservableProperty] public partial string NameFr { get; set; } = string.Empty;
    [ObservableProperty] public partial string Gender { get; set; } = string.Empty;
    [ObservableProperty] public partial string Council { get; set; } = string.Empty;
    [ObservableProperty] public partial string Bar { get; set; } = string.Empty;
    [ObservableProperty] public partial bool SupremeCourt { get; set; }
    [ObservableProperty] public partial string Standing { get; set; } = string.Empty;
    [ObservableProperty] public partial string Address { get; set; } = string.Empty;
    [ObservableProperty] public partial string Phone { get; set; } = string.Empty;
    [ObservableProperty] public partial string Email { get; set; } = string.Empty;
    [ObservableProperty] public partial string Firm { get; set; } = string.Empty;
    [ObservableProperty] public partial string Logo { get; private set; } = string.Empty;
    [ObservableProperty] public partial byte[]? LogoPreview { get; private set; }
    [ObservableProperty] public partial string DefaultPlace { get; set; } = string.Empty;
    [ObservableProperty] public partial string DefaultCouncil { get; set; } = string.Empty;
    [ObservableProperty] public partial string DefaultCourt { get; set; } = string.Empty;
    [ObservableProperty] public partial bool WatermarkByDefault { get; set; } = true;
    [ObservableProperty] public partial string FileNaming { get; set; } = "current";
    [ObservableProperty] public partial string Language { get; set; } = "ar";
    [ObservableProperty] public partial string Theme { get; set; } = "dark";
    [ObservableProperty] public partial string Engine { get; set; } = "ollama";
    [ObservableProperty] public partial bool AnalyseInBackground { get; set; } = true;
    [ObservableProperty] public partial string SaveFolder { get; set; } = string.Empty;
    [ObservableProperty] public partial string MessageKey { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsError { get; private set; }
    [ObservableProperty] public partial bool IsConfirmingLeave { get; private set; }
    [ObservableProperty] public partial bool IsConfirmingImport { get; private set; }
    [ObservableProperty] public partial bool CanContinueAnyway { get; private set; }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Step))
        {
            foreach (var name in new[] { nameof(ShowLawyerStep), nameof(ShowOfficeStep), nameof(ShowPrefsStep),
                nameof(HasNext), nameof(HasPrevious), nameof(IsLastStep), nameof(ProgressText), nameof(ShowEditorSave) }) OnPropertyChanged(name);
        }
        if (e.PropertyName == nameof(MessageKey))
        {
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(HasMessage));
        }
        if (e.PropertyName == nameof(LogoPreview)) OnPropertyChanged(nameof(HasLogo));
        if (e.PropertyName is nameof(IsConfirmingLeave) or nameof(IsConfirmingImport)) OnPropertyChanged(nameof(CanEdit));
        if (_loading) return;
        if (e.PropertyName is nameof(NameAr) or nameof(NameFr) or nameof(Email))
        {
            OnPropertyChanged(nameof(HasNameError));
            OnPropertyChanged(nameof(HasEmailError));
            OnPropertyChanged(nameof(IsValid));
        }
        if (e.PropertyName is nameof(Gender) or nameof(Council) or nameof(SupremeCourt))
        {
            if (!_standingEdited) SuggestStanding();
            OnPropertyChanged(nameof(AccreditationLabel));
        }
        if (e.PropertyName == nameof(Standing) && !_suggesting) _standingEdited = true;
        if (e.PropertyName == nameof(Language)) ApplyLanguage();
        if (e.PropertyName == nameof(Theme)) ApplyTheme();
        if (e.PropertyName is nameof(NameAr) or nameof(NameFr) or nameof(Gender) or nameof(Council) or nameof(Bar)
            or nameof(SupremeCourt) or nameof(Standing) or nameof(Address) or nameof(Phone) or nameof(Email)
            or nameof(Firm) or nameof(Logo) or nameof(DefaultPlace) or nameof(DefaultCouncil) or nameof(DefaultCourt)
            or nameof(WatermarkByDefault) or nameof(FileNaming) or nameof(Language) or nameof(Theme)
            or nameof(Engine) or nameof(AnalyseInBackground) or nameof(SaveFolder))
        {
            OnPropertyChanged(nameof(IsDirty));
            if (MessageKey is "ProfileSaved" or "ProfileExported") MessageKey = string.Empty;
        }
    }

    public void Reload()
    {
        var preferences = _preferencesStore.Load();
        _initialLanguage = _usesSharedLocalization ? L.Language : File.Exists(Path.Combine(_welcomePreferences.Root, "welcome-preferences.json"))
            ? _welcomePreferences.LoadLanguage() : preferences.Language;
        _initialTheme = File.Exists(_themePreferences.FilePath)
            ? _themePreferences.LoadDarkTheme() ? "dark" : "light" : preferences.Theme;
        Load(_profileStore.Load(), preferences with { Language = _initialLanguage, Theme = _initialTheme });
        _savedProfile = Profile();
        _savedPreferences = Preferences();
        MessageKey = string.Empty;
        OnPropertyChanged(nameof(IsDirty));
    }

    private void Load(LawyerProfile profile, ProfilePreferences preferences)
    {
        _loading = true;
        try
        {
            NameAr = profile.NameAr ?? string.Empty;
            NameFr = profile.NameFr ?? string.Empty;
            Gender = profile.Gender is "male" or "female" ? profile.Gender : string.Empty;
            Council = profile.Council ?? string.Empty;
            Bar = profile.Bar ?? string.Empty;
            SupremeCourt = profile.SupremeCourt;
            Standing = profile.Standing ?? string.Empty;
            _standingEdited = profile.StandingEdited || !string.IsNullOrEmpty(Standing)
                && Standing != LawyerProfile.SuggestedStanding(Council, SupremeCourt, Gender);
            Address = profile.Address ?? string.Empty;
            Phone = profile.Phone ?? string.Empty;
            Email = profile.Email ?? string.Empty;
            Firm = profile.Firm ?? string.Empty;
            Logo = profile.Logo ?? string.Empty;
            LogoPreview = _assets.Read(Logo);
            DefaultPlace = profile.DefaultPlace ?? string.Empty;
            DefaultCouncil = profile.DefaultCouncil ?? string.Empty;
            DefaultCourt = profile.DefaultCourt ?? string.Empty;
            WatermarkByDefault = profile.WatermarkByDefault;
            FileNaming = profile.FileNaming == "parties-date" ? "parties-date" : "current";
            Language = preferences.Language == "en" ? "en" : "ar";
            Theme = preferences.Theme == "light" ? "light" : "dark";
            Engine = preferences.Engine == "stub" ? "stub" : "ollama";
            AnalyseInBackground = preferences.AnalyseInBackground;
            SaveFolder = preferences.SaveFolder ?? string.Empty;
        }
        finally { _loading = false; }
        if (!_standingEdited && string.IsNullOrEmpty(Standing)) SuggestStanding();
        L.SetLanguage(Language);
        RefreshLanguage();
        OnPropertyChanged(nameof(LogoPreview));
        OnPropertyChanged(nameof(HasNameError));
        OnPropertyChanged(nameof(HasEmailError));
        OnPropertyChanged(nameof(IsDirty));
    }

    private LawyerProfile Profile() => new()
    {
        NameAr = EmptyToNull(NameAr), NameFr = EmptyToNull(NameFr), Gender = EmptyToNull(Gender),
        Council = EmptyToNull(Council), Bar = EmptyToNull(Bar), SupremeCourt = SupremeCourt,
        Standing = EmptyToNull(Standing), StandingEdited = _standingEdited,
        Address = EmptyToNull(Address), Phone = EmptyToNull(Phone), Email = EmptyToNull(Email),
        Firm = EmptyToNull(Firm), Logo = EmptyToNull(Logo), DefaultPlace = EmptyToNull(DefaultPlace),
        DefaultCouncil = EmptyToNull(DefaultCouncil), DefaultCourt = EmptyToNull(DefaultCourt),
        WatermarkByDefault = WatermarkByDefault, FileNaming = FileNaming,
    };

    private ProfilePreferences Preferences() => new()
    {
        Language = Language, Theme = Theme, Engine = Engine,
        AnalyseInBackground = AnalyseInBackground, SaveFolder = EmptyToNull(SaveFolder),
    };

    [RelayCommand] private void GoNext() { if (HasNext) Step++; }
    [RelayCommand] private void GoBack() { if (HasPrevious) Step--; }
    [RelayCommand] private void Finish() { if (SaveCore() && !IsEditing) CompleteSetup(); }
    [RelayCommand] private void Save() => SaveCore();

    private bool SaveCore()
    {
        if (!IsValid)
        {
            Step = HasNameError ? 0 : 1;
            SetMessage("ProfileValidationFailed", true);
            return false;
        }
        try
        {
            var profile = Profile();
            var preferences = Preferences();
            _profileStore.Save(profile);
            _preferencesStore.Save(preferences);
            if (!_welcomePreferences.SaveLanguage(Language) || !_themePreferences.Save(Theme == "dark"))
            {
                SetMessage("ProfileSaveFailed", true);
                return false;
            }
            _savedProfile = profile;
            _savedPreferences = preferences;
            _initialLanguage = Language;
            _initialTheme = Theme;
            SetMessage("ProfileSaved");
            OnPropertyChanged(nameof(IsDirty));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetMessage("ProfileSaveFailed", true);
            return false;
        }
    }

    private void CompleteSetup()
    {
        if (!_welcomePreferences.MarkSetupSeen(skipped: false))
        {
            CanContinueAnyway = true;
            SetMessage("SetupMarkerFailed", true);
            return;
        }
        _onFinished();
    }

    [RelayCommand] private void Later() => RequestLeave(SkipSetup);
    [RelayCommand] private void CloseEditor() => RequestLeave(_onLater);
    public void RequestLeave(Action leave)
    {
        ArgumentNullException.ThrowIfNull(leave);
        if (!IsDirty) { leave(); return; }
        _pendingLeave = leave;
        IsConfirmingLeave = true;
    }
    [RelayCommand] private void SaveAndLeave()
    {
        if (!SaveCore()) return;
        IsConfirmingLeave = false;
        var leave = _pendingLeave;
        _pendingLeave = null;
        leave?.Invoke();
    }
    [RelayCommand] private void DiscardAndLeave()
    {
        Load(_savedProfile, _savedPreferences);
        ApplyLanguage();
        ApplyTheme();
        IsConfirmingLeave = false;
        var leave = _pendingLeave;
        _pendingLeave = null;
        leave?.Invoke();
    }
    [RelayCommand] private void CancelLeave() { IsConfirmingLeave = false; _pendingLeave = null; }
    private void SkipSetup()
    {
        if (!_welcomePreferences.MarkSetupSeen())
        {
            CanContinueAnyway = true;
            SetMessage("SetupMarkerFailed", true);
            return;
        }
        _onLater();
    }
    [RelayCommand] private void ContinueAnyway() { CanContinueAnyway = false; _onLater(); }

    [RelayCommand] private void RestoreStanding()
    {
        _standingEdited = false;
        SuggestStanding();
        OnPropertyChanged(nameof(IsDirty));
    }
    private void SuggestStanding()
    {
        _suggesting = true;
        try { Standing = LawyerProfile.SuggestedStanding(Council, SupremeCourt, Gender) ?? string.Empty; }
        finally { _suggesting = false; }
    }

    [RelayCommand] private async Task ChooseLogoAsync()
    {
        if (_setupFiles is null) return;
        try
        {
            var image = await _setupFiles.ChooseImageAsync();
            if (image is null) return;
            if (!ValidateImage(image.Name, image.Bytes)) return;
            Logo = _assets.Copy(image.Name, image.Bytes);
            LogoPreview = image.Bytes;
            MessageKey = string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { SetMessage("ImageInvalid", true); }
    }
    private bool ValidateImage(string name, byte[] bytes)
    {
        if (bytes.Length > OnboardingProfileAssets.MaximumBytes) { SetMessage("ImageTooLarge", true); return false; }
        if (Path.GetExtension(name).ToLowerInvariant() is not (".png" or ".jpg" or ".jpeg")
            || !OnboardingProfileAssets.HasImageSignature(bytes) || _setupFiles?.IsReadableImage(bytes) != true)
        { SetMessage("ImageInvalid", true); return false; }
        return true;
    }
    [RelayCommand] private void RemoveLogo() { Logo = string.Empty; LogoPreview = null; }
    [RelayCommand] private async Task ChooseSaveFolderAsync()
    {
        try
        {
            if (_setupFiles is not null)
            {
                var folder = await _setupFiles.ChooseFolderAsync(SaveFolder);
                if (folder is not null) SaveFolder = folder;
            }
            else if (_saveFolderPicker is not null) await _saveFolderPicker(SaveFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { SetMessage("ProfileSaveFailed", true); }
    }
    public void SetLanguage(string language) => Language = language == "en" ? "en" : "ar";
    public void SetTheme(string theme) => Theme = theme == "light" ? "light" : "dark";
    private void ApplyLanguage()
    {
        L.SetLanguage(Language);
        RefreshLanguage();
        if (!_welcomePreferences.SaveLanguage(Language)) SetMessage("ProfilePreferenceFailed", true);
    }
    private void RefreshLanguage()
    {
        foreach (var choice in GenderOptions.Concat(LanguageOptions).Concat(ThemeOptions).Concat(EngineOptions).Concat(FileNamingOptions))
            choice.Relocalize();
        foreach (var name in new[] { nameof(IsRightToLeft), nameof(AccreditationLabel), nameof(PageTitle), nameof(FinishText),
            nameof(ProgressText), nameof(Message), nameof(ImportSummary) }) OnPropertyChanged(name);
    }
    private void ApplyTheme()
    {
        _applyingTheme = true;
        try { _onThemeApplied?.Invoke(Theme == "dark"); }
        finally { _applyingTheme = false; }
        if (!_themePreferences.Save(Theme == "dark")) SetMessage("ProfilePreferenceFailed", true);
    }

    public void ObserveTheme(bool dark)
    {
        if (_applyingTheme) return;
        _loading = true;
        try { Theme = dark ? "dark" : "light"; }
        finally { _loading = false; }
        // A title-bar change has already been saved by the shared theme controller.
        _savedPreferences = _savedPreferences with { Theme = Theme };
        OnPropertyChanged(nameof(IsDirty));
    }

    [RelayCommand] private async Task ImportAsync()
    {
        if (_setupFiles is null) return;
        try
        {
            var path = await _setupFiles.ChooseImportAsync();
            if (path is null) return;
            var imported = ProfileStore.Import(path);
            if (imported is null || imported.Profile.NameAr?.Length > 120 || imported.Profile.NameFr?.Length > 120
                || imported.Preferences.Language is not ("ar" or "en") || imported.Preferences.Theme is not ("light" or "dark")
                || imported.Preferences.Engine is not ("ollama" or "stub") || imported.Profile.Gender is not (null or "" or "male" or "female")
                || imported.Profile.FileNaming is not ("current" or "parties-date"))
            { SetMessage("ProfileImportInvalid", true); return; }
            if (imported.Logo is { } logo && (!ValidateImage(logo.FileName, Convert.FromBase64String(logo.Data)))) return;
            if (imported.Profile.Logo is { Length: > 0 } && imported.Logo is null)
            { SetMessage("ProfileImportInvalid", true); return; }
            if (!string.IsNullOrWhiteSpace(imported.Profile.Email)
                && (!MailAddress.TryCreate(imported.Profile.Email, out var email) || email.Address != imported.Profile.Email
                    || imported.Profile.Email.Any(char.IsWhiteSpace)))
            { SetMessage("ProfileImportInvalid", true); return; }
            _pendingImport = imported;
            OnPropertyChanged(nameof(ImportSummary));
            IsConfirmingImport = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or FormatException)
        { SetMessage("ProfileImportInvalid", true); }
    }
    [RelayCommand] private void ConfirmImport()
    {
        if (_pendingImport is not { } imported) return;
        try
        {
            var profile = imported.Profile with { Logo = null };
            if (imported.Logo is { } logo)
                profile = profile with { Logo = _assets.Copy(logo.FileName, Convert.FromBase64String(logo.Data)) };
            Load(profile, imported.Preferences);
            ApplyLanguage();
            ApplyTheme();
            IsConfirmingImport = false;
            _pendingImport = null;
            SetMessage("ProfileImported");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or FormatException)
        { SetMessage("ProfileImportInvalid", true); }
    }
    [RelayCommand] private void CancelImport() { IsConfirmingImport = false; _pendingImport = null; }
    [RelayCommand] private async Task ExportAsync()
    {
        if (_setupFiles is null) return;
        if (!IsValid) { SetMessage("ProfileValidationFailed", true); return; }
        try
        {
            var path = await _setupFiles.ChooseExportAsync();
            if (path is null) return;
            ExportedLogo? logo = LogoPreview is null ? null : new ExportedLogo(Path.GetFileName(Logo), Convert.ToBase64String(LogoPreview));
            ProfileStore.Export(path, Profile(), Preferences(), logo);
            SetMessage("ProfileExported");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { SetMessage("ProfileSaveFailed", true); }
    }
    private void SetMessage(string key, bool error = false) { IsError = error; MessageKey = key; }
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public sealed class SetupChoice(string value, string key, SetupLocalization localization) : ObservableObject
{
    public string Value => value;
    public string Label => localization[key];
    public void Relocalize() => OnPropertyChanged(nameof(Label));
}
