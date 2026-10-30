using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using DzLegal.App.Services;
using DzLegal.Presentation.Services;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Views;

/// <summary>Owns only launch navigation. Setup/start can replace the two placeholder factories later.</summary>
public sealed class OnboardingWindow : Window, IDisposable
{
    private readonly WelcomeViewModel _welcome;
    private readonly Func<Control> _setup;
    private readonly Func<Control> _start;
    private readonly Func<Window> _openWorkbench;
    private readonly Action _onFinished;
    private readonly Action _onLater;
    private readonly SetupViewModel _setupViewModel;
    private readonly string _profileRoot;
    private bool _allowClose;
    private bool _isSetupOpen;
    private HomeViewModel? _home;
    private HomeWorkspace? _homeWorkspace;
    private Window? _workbench;
    private readonly Func<Window, string, Task>? _openRecentCase;
    private readonly Func<Loc, Window>? _localizedWorkbenchFactory;

    public OnboardingWindow(WelcomePreferences preferences, Func<Window> openWorkbench,
        Func<Control>? setupStepOne = null, Func<Control>? startScreen = null,
        Func<bool>? tourHandler = null, Action? onSetupFinished = null, Action? onSetupLater = null,
        IUserFiles? files = null, Func<string?, Task>? saveFolderPicker = null,
        Func<Window, string, Task>? openRecentCase = null,
        Func<Loc, Window>? localizedWorkbenchFactory = null)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(openWorkbench);
        _openWorkbench = openWorkbench;
        _profileRoot = preferences.Root;
        _openRecentCase = openRecentCase;
        _localizedWorkbenchFactory = localizedWorkbenchFactory;
        Width = 1366;
        Height = 768;
        MinWidth = 800;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        (Application.Current as App)?.Themes?.TrackWindow(this);
        this.Bind(BackgroundProperty, new DynamicResourceExtension("SurfaceBrush"));
        this.Bind(ForegroundProperty, new DynamicResourceExtension("TextPrimaryBrush"));
        _welcome = new WelcomeViewModel(preferences, () =>
        {
            if (HomeLaunchRouting.CanGoHome(preferences)) OpenHome();
            else OpenSetupStepOne();
        });
        this.Bind(TitleProperty, new Binding("L[WelcomeTitle]") { Source = _welcome });
        _start = startScreen ?? CreateHome;
        _onFinished = onSetupFinished ?? OpenHome;
        _onLater = onSetupLater ?? OpenHome;
        _setupViewModel = new SetupViewModel(
            new ProfileStore(preferences.Root),
            new PreferencesStore(preferences.Root),
            new ThemePreferences(preferences.Root),
            preferences,
            files ?? new AvaloniaUserFiles(() => TopLevel.GetTopLevel(this), new AppPaths(preferences.Root)),
            _onFinished,
            _onLater,
            tourHandler,
            onThemeApplied: ApplyTheme,
            saveFolderPicker: saveFolderPicker,
            setupFiles: new SetupFileDialogs(() => this), localization: _welcome.L);
        _setup = setupStepOne ?? (() => new SetupView { DataContext = _setupViewModel }.CreateHost());
        Content = new WelcomeView { DataContext = _welcome };
        Closing += (_, e) =>
        {
            if (_allowClose || !_isSetupOpen || !Setup.IsDirty) return;
            e.Cancel = true;
            Setup.RequestLeave(() => { _allowClose = true; Close(); });
        };
        Closed += (_, _) => Dispose();
    }

    public SetupViewModel Setup => _setupViewModel;

    public void OpenSetupStepOne()
    {
        Setup.Reload();
        _isSetupOpen = true;
        Content = _setup();
    }

    public void OpenHome()
    {
        _isSetupOpen = false;
        Content = _start();
    }

    public void Dispose()
    {
        _home?.Dispose();
        _homeWorkspace?.Dispose();
        GC.SuppressFinalize(this);
    }

    private HomeView CreateHome()
    {
        _home?.Dispose();
        _homeWorkspace?.Dispose();
        _workbench ??= _localizedWorkbenchFactory?.Invoke(_welcome.L) ?? _openWorkbench();
        if (_workbench.DataContext is MainWindowViewModel model && model.L.Language != _welcome.L.Language)
            model.SwitchLanguageCommand.Execute(null);
        _homeWorkspace = new HomeWorkspace(_workbench, ShowWorkbench,
            openRecent: _openRecentCase is null ? null : path => _openRecentCase(_workbench, path));
        _home = new HomeViewModel(new RecentCasesStore(_profileRoot), new ProfileStore(_profileRoot),
            async () => await new ProfileSettingsWindow(_profileRoot, _welcome.L).ShowDialog(this), _homeWorkspace,
            new WelcomePreferences(_profileRoot).LoadLanguage(), localization: _welcome.L);
        return new HomeView { DataContext = _home };
    }

    private void ShowWorkbench()
    {
        if (_workbench is not { } window) return;
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = window;
        }

        if (window.DataContext is MainWindowViewModel model && model.L.Language != _welcome.L.Language)
            model.SwitchLanguageCommand.Execute(null);
        window.Show();
        _allowClose = true;
        Close();
    }

    private void ApplyTheme(bool dark) => (Application.Current as App)?.Themes?.SetDarkTheme(dark);

    private Grid Placeholder(string key, Func<Window> openWorkbench)
    {
        var button = new Button
        {
            Content = _welcome.L[key],
            MinHeight = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        button.Click += (_, _) =>
        {
            var window = openWorkbench();
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = window;
            }

            window.Show();
            Close();
        };
        var profileButton = new Button
        {
            Content = _setupViewModel.L["ProfileTitle"],
            MinHeight = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        profileButton.Click += async (_, _) => await new ProfileSettingsWindow(_profileRoot, _welcome.L).ShowDialog(this);
        var panel = new StackPanel
        {
            Spacing = 24,
            Margin = new Thickness(40),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FlowDirection = _welcome.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Children =
            {
                new TextBlock { Text = _welcome.L[key], FontSize = 24, TextWrapping = TextWrapping.Wrap, MaxWidth = 560, TextAlignment = TextAlignment.Center },
                button,
                profileButton,
            },
        };
        Grid.SetRow(panel, 1);
        return new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Margin = new Thickness(40, 28),
            FlowDirection = _welcome.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Children = { ThemeHost(), panel },
        };
    }

    private Border ThemeHost()
    {
        var host = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new ThemeToggleButton { Localization = _welcome.L },
        };
        host.Bind(Border.BackgroundProperty, new DynamicResourceExtension("AppBarBrush"));
        host.Bind(Border.CornerRadiusProperty, new DynamicResourceExtension("ControlRadius"));
        return host;
    }
}
