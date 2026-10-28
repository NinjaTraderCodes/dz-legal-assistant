using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using DzLegal.Storage;

namespace DzLegal.App.Views;

/// <summary>Owns only launch navigation. Setup/start can replace the two placeholder factories later.</summary>
public sealed class OnboardingWindow : Window
{
    private readonly WelcomeViewModel _welcome;
    private readonly Func<Control> _setup;
    private readonly Func<Control> _start;

    public OnboardingWindow(WelcomePreferences preferences, Func<Window> openWorkbench,
        Func<Control>? setupStepOne = null, Func<Control>? startScreen = null)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(openWorkbench);
        Width = 1366;
        Height = 768;
        MinWidth = 800;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = ThemeVariant.Dark;
        this.Bind(BackgroundProperty, new DynamicResourceExtension("SurfaceBrush"));
        this.Bind(ForegroundProperty, new DynamicResourceExtension("TextPrimaryBrush"));
        _welcome = new WelcomeViewModel(preferences, OpenSetupStepOne);
        this.Bind(TitleProperty, new Binding("L[WelcomeTitle]") { Source = _welcome });
        _setup = setupStepOne ?? (() => Placeholder("WelcomeSetupPlaceholder", openWorkbench));
        _start = startScreen ?? (() => Placeholder("WelcomeStartPlaceholder", openWorkbench));
        Content = preferences.HasProfile ? _start() : new WelcomeView { DataContext = _welcome };
    }

    public void OpenSetupStepOne() => Content = _setup();

    private StackPanel Placeholder(string key, Func<Window> openWorkbench)
    {
        var button = new Button
        {
            Content = _welcome.L["WelcomeOpenWorkbench"],
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
            },
        };
        return panel;
    }
}
