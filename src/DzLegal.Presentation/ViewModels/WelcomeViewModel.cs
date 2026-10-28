using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Storage;

namespace DzLegal.Presentation.ViewModels;

/// <summary>A separate screen; no case or legal-engine state is created during Welcome.</summary>
public sealed partial class WelcomeViewModel : ObservableObject
{
    private readonly WelcomePreferences _preferences;
    private readonly Action _openSetup;

    public WelcomeViewModel(WelcomePreferences preferences, Action openSetup)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(openSetup);
        _preferences = preferences;
        _openSetup = openSetup;
        L.SetLanguage(preferences.LoadLanguage());
    }

    public Loc L { get; } = new();

    public bool IsRightToLeft => L.IsRightToLeft;

    public bool IsArabic => L.Language == "ar";

    public bool IsEnglish => L.Language == "en";

    public double TitleLetterSpacing => IsEnglish ? 0.6 : 0;

    [ObservableProperty]
    public partial bool LanguageSaveFailed { get; private set; }

    [RelayCommand]
    private void SwitchLanguage(string language)
    {
        if (language is not ("ar" or "en") || L.Language == language)
        {
            return;
        }

        // Loc.SetLanguage is the existing workbench's localization mechanism.
        L.SetLanguage(language);
        LanguageSaveFailed = !_preferences.SaveLanguage(language);
        OnPropertyChanged(nameof(IsRightToLeft));
        OnPropertyChanged(nameof(IsArabic));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(TitleLetterSpacing));
    }

    [RelayCommand]
    private void Start() => _openSetup();
}
