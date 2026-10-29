using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using DzLegal.App.Views;
using DzLegal.Storage;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace DzLegal.App.Tests;

public sealed class GlobalThemeTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-theme-");

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{broken")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void InvalidPreferenceFallsBackWithoutCrashing(string content)
    {
        var preferences = new ThemePreferences(_folder.FullName);
        File.WriteAllText(preferences.FilePath, content);
        Assert.True(preferences.LoadDarkTheme());
        Assert.True(preferences.Save(false));
        Assert.False(new ThemePreferences(_folder.FullName).LoadDarkTheme());
    }

    [AvaloniaFact]
    public void SharedButtonAndExistingCommandUpdateEveryHostAndSurviveNavigation()
    {
        var app = Assert.IsType<App>(Application.Current);
        var previous = app.RequestedThemeVariant;
        var preferences = new ThemePreferences(_folder.FullName);
        Assert.True(preferences.Save(false));
        var theme = app.InitializeTheme(preferences);
        var vm = Vm.Create();
        var workbench = new MainWindow { DataContext = vm, Width = 1920, Height = 1080 };
        var onboarding = new OnboardingWindow(new WelcomePreferences(_folder.FullName), () => throw new InvalidOperationException());
        var dialog = new Window { Width = 320, Height = 160, Content = new TextBox() };
        try
        {
            workbench.Show(); onboarding.Show(); dialog.Show(workbench);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);
            Assert.All(new Window[] { workbench, onboarding, dialog }, w => Assert.Equal(ThemeVariant.Light, w.ActualThemeVariant));
            var welcome = Assert.IsType<WelcomeView>(onboarding.Content);
            var button = welcome.FindControl<ThemeToggleButton>("ThemeButton")!;
            Assert.NotNull(button.Command);
            button.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(theme.IsDark);
            Assert.True(vm.IsDarkTheme);
            Assert.All(new Window[] { workbench, onboarding, dialog }, w => Assert.Equal(ThemeVariant.Dark, w.ActualThemeVariant));
            Assert.Equal(Symbol.WeatherSunny, Assert.IsType<SymbolIcon>(button.Content).Symbol);
            Assert.Equal(((WelcomeViewModel)welcome.DataContext!).L["ThemeSwitchToLight"], ToolTip.GetTip(button));

            ((WelcomeViewModel)welcome.DataContext!).StartCommand.Execute(null);
            var placeholder = Assert.IsType<Grid>(onboarding.Content);
            var host = Assert.IsType<Border>(placeholder.Children[0]);
            var navigationButton = Assert.IsType<ThemeToggleButton>(host.Child);
            Assert.Same(theme.ToggleCommand, navigationButton.Command);
            Assert.Equal(ThemeVariant.Dark, onboarding.ActualThemeVariant);
            vm.SelectedStep = vm.FieldGroups[^1];
            vm.SelectedDraftTab = 1;
            Assert.True(theme.IsDark);

            vm.ToggleThemeCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.False(theme.IsDark);
            Assert.False(new ThemePreferences(_folder.FullName).LoadDarkTheme());
            Assert.All(new Window[] { workbench, onboarding, dialog }, w => Assert.Equal(ThemeVariant.Light, w.ActualThemeVariant));
            vm.SelectedStep = vm.FieldGroups[0];
            vm.SelectedDraftTab = 0;
            Assert.Equal(Symbol.WeatherMoon, Assert.IsType<SymbolIcon>(navigationButton.Content).Symbol);
            Assert.False(theme.PreferenceSaveFailed);
        }
        finally
        {
            dialog.Close(); onboarding.Close(); workbench.Close();
            app.ClearTheme(); app.RequestedThemeVariant = previous;
        }
    }

    [AvaloniaFact]
    public void SavedThemeIsAppliedBeforeFirstWindowAndSharedControlMatchesExistingBarButton()
    {
        var app = Assert.IsType<App>(Application.Current);
        var previous = app.RequestedThemeVariant;
        var preferences = new ThemePreferences(_folder.FullName);
        Assert.True(preferences.Save(true));
        app.InitializeTheme(preferences);
        var window = new MainWindow { DataContext = Vm.Create(), Width = 1920, Height = 1080 };
        try
        {
            Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
            var shared = window.FindControl<ThemeToggleButton>("ThemeButton")!;
            var toolbar = Assert.IsType<StackPanel>(shared.Parent);
            var original = new Button { Content = new SymbolIcon { Symbol = Symbol.WeatherMoon } };
            original.Classes.Add("bar");
            toolbar.Children.Add(original);
            window.Show(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            Assert.Equal(original.Bounds.Size, shared.Bounds.Size);
            Assert.Equal(original.Padding, shared.Padding);
            Assert.Equal(original.CornerRadius, shared.CornerRadius);
            Assert.Equal(original.Foreground, shared.Foreground);
            shared.Command!.Execute(null);
            Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);
            window.Close();
            app.InitializeTheme(new ThemePreferences(_folder.FullName));
            Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);
            Assert.False(new ThemePreferences(_folder.FullName).LoadDarkTheme());
        }
        finally
        {
            window.Close(); app.ClearTheme(); app.RequestedThemeVariant = previous;
        }
    }

    public void Dispose() => _folder.Delete(recursive: true);
}
