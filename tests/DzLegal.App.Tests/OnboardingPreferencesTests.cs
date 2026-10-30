using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using DzLegal.App.Views;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public sealed class OnboardingPreferencesTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-onboarding-choice-");

    [AvaloniaTheory]
    [InlineData("ar", true)]
    [InlineData("en", false)]
    public void WelcomeChoicesShareLocalizationAndSurviveSetupFinishAndRelaunch(string language, bool dark)
    {
        var app = (DzLegal.App.App)Application.Current!;
        var preferences = new WelcomePreferences(_folder.FullName);
        new ThemePreferences(_folder.FullName).Save(!dark);
        var theme = app.InitializeTheme(new ThemePreferences(_folder.FullName));
        try
        {
            var window = new OnboardingWindow(preferences, () => new Window());
            window.Show(); Layout(window);
            var welcome = Assert.IsType<WelcomeView>(window.Content);
            var model = Assert.IsType<WelcomeViewModel>(welcome.DataContext);
            model.SwitchLanguageCommand.Execute(language);
            welcome.FindControl<ThemeToggleButton>("ThemeButton")!.Command!.Execute(null);
            Assert.Equal(dark, theme.IsDark);
            model.StartCommand.Execute(null); Layout(window);
            Assert.Same(model.L, window.Setup.L.Source);
            Assert.Equal(language, window.Setup.Language);
            Assert.Equal(dark ? "dark" : "light", window.Setup.Theme);
            Assert.Equal(language == "ar", window.Setup.IsRightToLeft);
            window.Setup.GoNextCommand.Execute(null); window.Setup.GoNextCommand.Execute(null);
            window.Setup.FinishCommand.Execute(null); Layout(window);
            var offer = Assert.IsType<TourOfferViewModel>(Assert.IsType<TourOfferView>(window.Content).DataContext);
            Assert.Same(model.L, offer.L);
            offer.NotNowCommand.Execute(null); Layout(window);
            var home = Assert.IsType<HomeViewModel>(Assert.IsType<HomeView>(window.Content).DataContext);
            Assert.Same(model.L, home.L.Source);
            Assert.Equal(language, home.L.Language);
            Assert.Equal(dark ? ThemeVariant.Dark : ThemeVariant.Light, window.ActualThemeVariant);
            window.Close();
            app.ClearTheme();
            theme = app.InitializeTheme(new ThemePreferences(_folder.FullName));
            var relaunched = new OnboardingWindow(preferences, () => new Window());
            relaunched.Show(); Layout(relaunched);
            var savedWelcome = Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(relaunched.Content).DataContext);
            Assert.Equal(language, savedWelcome.L.Language);
            Assert.Equal(dark, theme.IsDark);
            savedWelcome.StartCommand.Execute(null); Layout(relaunched);
            var savedHome = Assert.IsType<HomeViewModel>(Assert.IsType<HomeView>(relaunched.Content).DataContext);
            Assert.Same(savedWelcome.L, savedHome.L.Source);
            Assert.Equal(language, savedHome.L.Language);
            Assert.Equal(dark, theme.IsDark);
            Assert.Equal(dark ? ThemeVariant.Dark : ThemeVariant.Light, relaunched.ActualThemeVariant);
            relaunched.Close();
        }
        finally { app.ClearTheme(); }
    }

    [AvaloniaFact]
    public void ArabicEnglishAndBackUpdateActualBindingsDirectionSelectionAndSavedLanguage()
    {
        var source = new Loc();
        using var home = new HomeViewModel(new RecentCasesStore(_folder.FullName), new ProfileStore(_folder.FullName),
            () => Task.CompletedTask, localization: source);
        var view = new HomeView { DataContext = home };
        var window = new Window { Content = view, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        foreach (var language in new[] { "en", "ar", "en", "ar" })
        {
            view.FindControl<Button>(language == "ar" ? "HomeArabic" : "HomeEnglish")!.Command!.Execute(language);
            Layout(window);
            Assert.Equal(language, source.Language);
            Assert.Equal(home.L["StartNew"], view.FindControl<Button>("HomeNew")!.Content);
            Assert.Equal(language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, view.FlowDirection);
            Assert.Equal(language == "ar", view.FindControl<Button>("HomeArabic")!.Classes.Contains("selected"));
            Assert.Equal(language == "en", view.FindControl<Button>("HomeEnglish")!.Classes.Contains("selected"));
            Assert.Equal(language, new WelcomePreferences(_folder.FullName).LoadLanguage());
            Assert.DoesNotContain("[", (string)view.FindControl<Button>("HomeNew")!.Content!, StringComparison.Ordinal);
        }
        Assert.Null(view.FindControl<Button>("HomeFrench"));
        Assert.Null(view.FindControl<Button>("HomeProfile"));
        view.FindControl<Button>("HomeNew")!.Focus();
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null); Layout(window);
        window.Close();
    }

    [AvaloniaFact]
    public void SavedUnsupportedLanguageFallsBackToArabicAcrossTheFlow()
    {
        File.WriteAllText(Path.Combine(_folder.FullName, "welcome-preferences.json"), "{\"language\":\"fr\"}");
        new PreferencesStore(_folder.FullName).Save(new ProfilePreferences { Language = "fr" });
        var window = new OnboardingWindow(new WelcomePreferences(_folder.FullName), () => new Window());
        var welcome = Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(window.Content).DataContext);
        Assert.Equal("ar", welcome.L.Language);
        welcome.StartCommand.Execute(null);
        Assert.Equal("ar", window.Setup.Language);
        Assert.Equal(new[] { "ar", "en" }, window.Setup.LanguageOptions.Select(c => c.Value));
        window.Setup.LaterCommand.Execute(null);
        var home = Assert.IsType<HomeViewModel>(Assert.IsType<HomeView>(window.Content).DataContext);
        Assert.Equal("ar", home.L.Language);
        Assert.True(home.IsRightToLeft);
        window.Close();
    }

    [AvaloniaFact]
    public void LaterHasNoProfileAndItsMarkerSkipsSetupOnlyAfterWelcome()
    {
        var preferences = new WelcomePreferences(_folder.FullName);
        var window = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(window.Content).DataContext).StartCommand.Execute(null);
        window.Setup.LaterCommand.Execute(null);
        Assert.IsType<HomeView>(window.Content);
        Assert.True(preferences.HasSetupSeen);
        Assert.True(preferences.WasSetupSkipped);
        Assert.False(preferences.HasProfile);
        Assert.Null(new ProfileStore(_folder.FullName).Load().NameAr);
        window.Close();
        var saved = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(saved.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(saved.Content);
        Assert.False(preferences.HasProfile); saved.Close();
        File.WriteAllText(preferences.ProfilePath, "{broken");
        var broken = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(broken.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(broken.Content);
        Assert.Equal("{broken", File.ReadAllText(preferences.ProfilePath)); broken.Close();
        preferences.MarkSetupSeen(skipped: false);
        File.Delete(preferences.ProfilePath);
        var missing = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(missing.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(missing.Content);
        Assert.False(preferences.HasProfile); missing.Close();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{broken")]
    [InlineData("{\"skipped\":true}")]
    [InlineData("{\"skipped\":false}")]
    [InlineData("{\"skipped\":\"true\"}")]
    [InlineData("[]")]
    [InlineData("")]
    public void AnySetupSeenMarkerSkipsSetupAfterWelcomeWithoutRewritingIt(string marker)
    {
        var preferences = new WelcomePreferences(_folder.FullName);
        File.WriteAllText(preferences.SetupSeenPath, marker);
        Assert.True(HomeLaunchRouting.CanGoHome(preferences));
        Assert.Equal(marker, File.ReadAllText(preferences.SetupSeenPath));
        Assert.False(preferences.HasProfile);
    }

    [AvaloniaFact]
    public void OpeningAScreenDoesNotOverwriteTheActiveSharedLanguageWithAnOlderSavedChoice()
    {
        var preferences = new WelcomePreferences(_folder.FullName);
        preferences.SaveLanguage("ar");
        var model = Vm.Create();
        model.SwitchLanguageCommand.Execute(null);
        Assert.Equal("en", model.L.Language);
        var welcome = new WelcomeViewModel(preferences, () => { }, model.L);
        Assert.Same(model.L, welcome.L);
        Assert.Equal("en", welcome.L.Language);
        var workbench = new Window { DataContext = model };
        var home = new HomeWindow(workbench, _folder.FullName);
        Assert.Same(model.L, home.Home.L.Source);
        Assert.Equal("en", home.Home.L.Language);
        var editor = new ProfileSettingsWindow(_folder.FullName, model.L);
        Assert.Same(model.L, editor.Setup.L.Source);
        Assert.Equal("en", editor.Setup.Language);
        editor.Setup.Reload();
        Assert.Equal("en", model.L.Language);
        Assert.Equal("ar", preferences.LoadLanguage());
        editor.Close(); home.Close(); workbench.Close(); model.Dispose();
    }

    private static void Layout(Window window) { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.CaptureRenderedFrame()?.Dispose(); Dispatcher.UIThread.RunJobs(); }
    public void Dispose() => _folder.Delete(true);
}
