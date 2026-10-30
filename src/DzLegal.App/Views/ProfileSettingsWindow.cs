using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using DzLegal.App.Services;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Views;

/// <summary>The profile editor can be opened by the future workbench entry point.</summary>
public sealed class ProfileSettingsWindow : Window
{
    private bool _allowClose;

    public ProfileSettingsWindow(string root, Loc? localization = null)
    {
        Width = 1100;
        Height = 800;
        MinWidth = 800;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        (Application.Current as App)?.Themes?.TrackWindow(this);
        Setup = new SetupViewModel(new ProfileStore(root), new PreferencesStore(root), new ThemePreferences(root),
            new WelcomePreferences(root), new AvaloniaUserFiles(() => this, new AppPaths(root)), CloseEditor, CloseEditor,
            onThemeApplied: dark => (Application.Current as App)?.Themes?.SetDarkTheme(dark),
            setupFiles: new SetupFileDialogs(() => this), isEditing: true, localization: localization);
        this.Bind(TitleProperty, new Binding("L[ProfileTitle]") { Source = Setup });
        Content = new SetupView { DataContext = Setup }.CreateHost();
        Closing += (_, e) =>
        {
            if (_allowClose || !Setup.IsDirty) return;
            e.Cancel = true;
            Setup.RequestLeave(CloseEditor);
        };
    }

    public SetupViewModel Setup { get; }

    private void CloseEditor() { _allowClose = true; Close(); }
}
