using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using DzLegal.App.Views;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public sealed class HomeTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-home-");
    private HomeViewModel Create(IHomeWorkspace? workspace = null, string language = "ar") => new(
        new RecentCasesStore(_folder.FullName), new ProfileStore(_folder.FullName), () => Task.CompletedTask, workspace, language);
    private void CompleteProfile() => new ProfileStore(_folder.FullName).Save(new LawyerProfile { NameAr = "محامية افتراضية", Gender = "female" });

    [Fact]
    public void FirstUseHasEmptyRecentAndIncompleteProfileButNoRecovery()
    {
        using var home = Create();
        Assert.True(home.HasNoRecent);
        Assert.True(home.IsProfileIncomplete);
        Assert.False(home.HasRecoverableDraft);
        Assert.False(home.NewPetitionCommand.CanExecute(null));
        Assert.True(home.WorkspaceUnavailable);
        CompleteProfile(); home.Reload();
        Assert.False(home.IsProfileIncomplete);
    }
    [Fact]
    public async Task RecoveryActionsUseOnlyTheExistingWorkspaceCommands()
    {
        var workspace = new Workspace { Recovery = true };
        using var home = Create(workspace);
        Assert.True(home.HasRecoverableDraft);
        home.NotNowCommand.Execute(null);
        Assert.Equal(1, workspace.Dismissed);
        Assert.False(home.HasRecoverableDraft);
        workspace.Recovery = true;
        await home.ResumeCommand.ExecuteAsync(null);
        Assert.Equal(1, workspace.Restored);
        Assert.Equal(1, workspace.Shown);
    }
    [Fact]
    public async Task MissingRecentOffersRemovalAndClearRequiresConfirmation()
    {
        var store = new RecentCasesStore(_folder.FullName);
        var path = Path.Combine(_folder.FullName, "missing.dzdraft.json");
        store.RecordOpened(path, "موضوع افتراضي");
        using var home = Create();
        var item = Assert.Single(home.RecentCases);
        Assert.True(item.OpenCommand.CanExecute(null));
        await item.OpenCommand.ExecuteAsync(null);
        Assert.True(home.HasMissingFile);
        Assert.Contains(path, home.Message, StringComparison.Ordinal);
        home.RemoveMissingCommand.Execute(null);
        Assert.Empty(home.RecentCases);
        File.WriteAllText(path, "fixture"); store.RecordOpened(path, "fictional"); home.Reload();
        Assert.False(home.RecentCases[0].OpenCommand.CanExecute(null));
        home.ClearRecentCommand.Execute(null);
        Assert.Single(store.Load());
        home.CancelClearCommand.Execute(null);
        Assert.Single(store.Load());
        home.ClearRecentCommand.Execute(null); home.ConfirmClearCommand.Execute(null);
        Assert.Empty(store.Load()); Assert.True(File.Exists(path));
    }
    [Theory]
    [InlineData("ar", true)][InlineData("en", false)][InlineData("fr", true)]
    public void HomeLanguagesDoNotModifyFeatureOneAndAlwaysReuseOfflineStatus(string language, bool rtl)
    {
        using var home = Create(language: language);
        Assert.Equal(rtl, home.IsRightToLeft);
        Assert.Equal(home.L.Source["OfflineStatus"], home.L["OfflineStatus"]);
        Assert.DoesNotContain("[", home.L["StartNew"], StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_folder.FullName, "welcome-preferences.json")));
    }
    [Fact]
    public async Task RecentOpeningUsesTheProvidedSafeRouteAndClearPausesWorkspaceShortcuts()
    {
        var path = Path.Combine(_folder.FullName, "fictional.dzdraft.json");
        File.WriteAllText(path, "fixture");
        var store = new RecentCasesStore(_folder.FullName);
        store.RecordOpened(path, "fictional subject");
        string? opened = null;
        var workspace = new Workspace { OpenRecent = value => { opened = value; return Task.CompletedTask; } };
        using var home = Create(workspace);
        await home.RecentCases[0].OpenCommand.ExecuteAsync(null);
        Assert.Equal(path, opened); Assert.Equal(1, workspace.Shown);
        home.ClearRecentCommand.Execute(null);
        Assert.False(home.NewPetitionCommand.CanExecute(null));
        Assert.False(home.OpenCaseCommand.CanExecute(null));
        Assert.False(home.RecentCases[0].OpenCommand.CanExecute(null));
        home.CancelClearCommand.Execute(null);
        Assert.True(home.NewPetitionCommand.CanExecute(null));
        Assert.True(home.OpenCaseCommand.CanExecute(null));
        Assert.True(home.RecentCases[0].OpenCommand.CanExecute(null));
    }
    [Fact]
    public void FailedRecentWriteShowsFeedbackAndRetainsDisplayedEntry()
    {
        var store = new RecentCasesStore(_folder.FullName);
        store.RecordOpened(Path.Combine(_folder.FullName, "fictional.dzdraft.json"), "fictional");
        using var home = Create();
        File.Delete(store.FilePath); Directory.CreateDirectory(store.FilePath);
        home.RecentCases[0].RemoveCommand.Execute(null);
        Assert.Equal(home.L["HomeStorageFailed"], home.Message);
        Assert.Single(home.RecentCases);
    }
    [Theory]
    [InlineData("", false, false)][InlineData("{broken", false, false)][InlineData("null", false, false)][InlineData("[]", false, false)][InlineData("{}", false, true)]
    [InlineData("", true, true)][InlineData("{broken", true, true)][InlineData("null", true, true)][InlineData("[]", true, true)][InlineData("{}", true, true)]
    public void ProfilesAndMarkersDetermineTheRouteAfterWelcomeWithoutOverwritingThem(string contents, bool hasMarker, bool goHome)
    {
        var preferences = new WelcomePreferences(_folder.FullName);
        File.WriteAllText(preferences.ProfilePath, contents);
        if (hasMarker) preferences.MarkSetupSeen();
        Assert.Equal(goHome, HomeLaunchRouting.CanGoHome(preferences));
        Assert.Equal(contents, File.ReadAllText(preferences.ProfilePath));
        Assert.Equal(hasMarker, preferences.HasSetupSeen);
    }
    [Fact]
    public void LaterMarkerSkipsSetupWhenProfileIsAbsent()
    {
        var preferences = new WelcomePreferences(_folder.FullName);
        Assert.False(HomeLaunchRouting.CanGoHome(preferences));
        preferences.MarkSetupSeen();
        Assert.True(HomeLaunchRouting.CanGoHome(preferences));
    }
    [AvaloniaFact]
    public void FinishAndLaterOpenHomeAfterSetupAndRelaunchAlwaysShowsWelcome()
    {
        var preferences = new WelcomePreferences(_folder.FullName);
        var window = new OnboardingWindow(preferences, () => new Window());
        var welcome = Assert.IsType<WelcomeView>(window.Content);
        Assert.IsType<WelcomeViewModel>(welcome.DataContext).StartCommand.Execute(null);
        Assert.IsType<Grid>(window.Content);
        Assert.True(window.Setup.ShowLawyerStep);
        window.Setup.GoNextCommand.Execute(null);
        Assert.True(window.Setup.ShowOfficeStep);
        window.Setup.GoNextCommand.Execute(null);
        Assert.True(window.Setup.ShowPrefsStep);
        window.Setup.FinishCommand.Execute(null);
        Assert.IsType<TourOfferViewModel>(Assert.IsType<TourOfferView>(window.Content).DataContext).NotNowCommand.Execute(null);
        Assert.IsType<HomeView>(window.Content);
        window.Close();
        var saved = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(saved.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(saved.Content); saved.Close();
        File.WriteAllText(preferences.ProfilePath, "{broken");
        var broken = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(broken.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(broken.Content);
        Assert.Equal("{broken", File.ReadAllText(preferences.ProfilePath)); broken.Close();
        File.Delete(preferences.ProfilePath);
        var later = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(later.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(later.Content); later.Close();
        var root = Path.Combine(_folder.FullName, "new-user");
        var first = new OnboardingWindow(new WelcomePreferences(root), () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(first.Content).DataContext).StartCommand.Execute(null);
        first.Setup.LaterCommand.Execute(null);
        Assert.IsType<HomeView>(first.Content); Assert.False(File.Exists(Path.Combine(root, "profile.json"))); first.Close();
        var afterLater = new OnboardingWindow(new WelcomePreferences(root), () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(afterLater.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(afterLater.Content);
        Assert.False(File.Exists(Path.Combine(root, "profile.json"))); afterLater.Close();
    }
    [AvaloniaFact]
    public async Task NewFromHomePreservesTheExistingReplaceConfirmationAndBackKeepsCase()
    {
        var model = Vm.Create();
        model.Field("notes.facts").Value = "وقائع افتراضية غير محفوظة";
        var workbench = new Window { DataContext = model };
        using var workspace = new HomeWorkspace(workbench, () => { }, hasOpenCase: true);
        using var home = Create(workspace);
        await home.NewPetitionCommand.ExecuteAsync(null);
        Assert.True(model.IsConfirmingReplace);
        Assert.Equal("وقائع افتراضية غير محفوظة", model.Field("notes.facts").Value);
        model.CancelReplaceCommand.Execute(null);
        await home.OpenCaseCommand.ExecuteAsync(null);
        Assert.True(model.IsConfirmingReplace);
        model.CancelReplaceCommand.Execute(null);
        home.BackToCaseCommand.Execute(null);
        Assert.Equal("وقائع افتراضية غير محفوظة", model.Field("notes.facts").Value);
        workbench.Close(); model.Dispose();
    }
    [AvaloniaFact]
    public void HomeKeyboardShortcutsUseTheWorkspaceCommands()
    {
        var workspace = new Workspace();
        using var home = Create(workspace);
        var view = new HomeView { DataContext = home };
        var window = new Window { Content = view, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        foreach (var language in new[] { "ar", "en" })
        {
            home.SwitchLanguageCommand.Execute(language); Layout(window);
            view.FindControl<Button>("HomeNew")!.Focus();
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null); Layout(window);
            Assert.Same(view.FindControl<Button>("HomeOpen"), window.FocusManager!.GetFocusedElement());
        }
        view.FindControl<Button>("HomeNew")!.Focus();
        window.KeyPress(Key.N, RawInputModifiers.Control, PhysicalKey.N, "n");
        window.KeyRelease(Key.N, RawInputModifiers.Control, PhysicalKey.N, "n"); Layout(window);
        Assert.Equal(1, workspace.Shown);
        view.FindControl<Button>("HomeOpen")!.Focus();
        window.KeyPress(Key.O, RawInputModifiers.Control, PhysicalKey.O, "o");
        window.KeyRelease(Key.O, RawInputModifiers.Control, PhysicalKey.O, "o"); Layout(window);
        Assert.Equal(2, workspace.Shown);
        window.Close();
    }
    [AvaloniaFact]
    public void EscapeCancelsClearAndReturnsKeyboardFocusToTheList()
    {
        new RecentCasesStore(_folder.FullName).RecordOpened(Path.Combine(_folder.FullName, "fictional.dzdraft.json"), "fictional");
        using var home = Create(new Workspace());
        var view = new HomeView { DataContext = home };
        var window = new Window { Content = view, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        home.ClearRecentCommand.Execute(null); Layout(window);
        Assert.Same(view.FindControl<Button>("ConfirmClear"), window.FocusManager!.GetFocusedElement());
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Layout(window);
        Assert.False(home.IsConfirmingClear);
        Assert.Same(view.FindControl<Button>("ClearRecent"), window.FocusManager.GetFocusedElement());
        Assert.Single(home.RecentCases);
        window.Close();
    }
    [AvaloniaFact]
    public async Task AllHomeStatesRenderAtBothSizesScalesLanguagesAndThemes()
    {
        foreach (var size in new[] { (1366,768),(1920,1080) })
        foreach (var scale in new[] { 1d, 1.25 })
        foreach (var language in new[] { "ar","en" })
        foreach (var dark in new[] { false,true })
        foreach (var state in new[] { "first","recent","resume","missing","incomplete","combined" })
        {
            var root = Path.Combine(_folder.FullName, "render");Directory.CreateDirectory(root);
            var profiles = new ProfileStore(root);
            profiles.Save(state is "incomplete" or "combined" ? LawyerProfile.Default : new LawyerProfile { NameAr="اسم افتراضي",Gender="male" });
            var store = new RecentCasesStore(root);store.Clear();
            if (state is "recent" or "resume" or "missing" or "combined")
                store.RecordOpened(Path.Combine(root,new string('a',220)+".dzdraft.json"), string.Join(' ',Enumerable.Repeat("موضوع افتراضي طويل",35)));
            var workspace = new Workspace { Recovery = state is "resume" or "combined" };
            using var vm = new HomeViewModel(store,profiles,()=>Task.CompletedTask,workspace,language, exists: _ => state != "missing");
            if (state == "missing") await vm.RecentCases[0].OpenCommand.ExecuteAsync(null);
            var view = new HomeView { DataContext=vm };
            var window = new Window { Content=view,Width=size.Item1/scale,Height=size.Item2/scale,RequestedThemeVariant=dark?ThemeVariant.Dark:ThemeVariant.Light };
            window.Show();window.SetRenderScaling(scale);Layout(window);
            Assert.Equal(language=="ar"?FlowDirection.RightToLeft:FlowDirection.LeftToRight,view.FlowDirection);
            Assert.Equal(state is "resume" or "combined",view.FindControl<Border>("ResumeBanner")!.IsVisible);
            Assert.Equal(state is "incomplete" or "combined",view.FindControl<Border>("IncompleteProfileBanner")!.IsVisible);
            Assert.Null(view.FindControl<Button>("HomeFrench"));
            Assert.Null(view.FindControl<Button>("HomeProfile"));
            foreach (var names in new[] { new[] { "HomeNew", "HomeOpen", "HomeSample" }, new[] { "HomeTour", "HomeHelp", "HomeLibrary" } })
            {
                var buttons = names.Select(name => view.FindControl<Button>(name)!).ToArray();
                var first = buttons[0]; var y = first.TranslatePoint(default, window)!.Value.Y;
                foreach (var button in buttons)
                {
                    Assert.InRange(Math.Abs(button.Bounds.Width - first.Bounds.Width), 0, 1);
                    Assert.Equal(first.Bounds.Height, button.Bounds.Height);
                    Assert.Equal(y, button.TranslatePoint(default, window)!.Value.Y);
                    Assert.Equal(first.FontSize, button.FontSize);
                    Assert.Equal(first.CornerRadius, button.CornerRadius);
                    Assert.Same(first.Theme, button.Theme);
                }
            }
            foreach (var name in new[] { "HomeActions","HomeLinks","HomeOffline" })
            {
                var control=view.FindControl<Control>(name)!;var point=control.TranslatePoint(default,window)!.Value;
                var opposite=control.TranslatePoint(new Point(control.Bounds.Width,control.Bounds.Height),window)!.Value;
                // RTL mirrors the local origin; test both transformed corners.
                Assert.True(Math.Min(point.X,opposite.X)>=0 && Math.Min(point.Y,opposite.Y)>=0 && Math.Max(point.X,opposite.X)<=window.Bounds.Width+1 && Math.Max(point.Y,opposite.Y)<=window.Bounds.Height+1,$"{state}/{language}/{dark}/{size}/{scale}: {name} at {point}/{opposite}, window {window.Bounds}");
            }
            foreach (var text in view.GetVisualDescendants().OfType<TextBlock>().Where(t=>t.IsEffectivelyVisible))
                Assert.DoesNotContain("[Start",text.Text??string.Empty,StringComparison.Ordinal);
            if (scale==1 && Environment.GetEnvironmentVariable("DZLEGAL_HOME_SCREENSHOTS") is {Length:>0} output)
            {
                Directory.CreateDirectory(output);using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);
                frame.Save(Path.Combine(output,$"home-{state}-{language}-{(dark?"dark":"light")}-{size.Item1}x{size.Item2}.png"),PngBitmapEncoderOptions.Default);
            }
            window.Close();
        }
    }
    private static void Layout(Window window) { window.UpdateLayout();Dispatcher.UIThread.RunJobs();window.CaptureRenderedFrame()?.Dispose();Dispatcher.UIThread.RunJobs(); }
    private sealed class Workspace : IHomeWorkspace
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool _recovery;
        public bool Recovery { get=>_recovery;set { _recovery=value;PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(HasRecoverableDraft))); } }
        public int Dismissed { get;private set; }
        public int Restored { get;private set; }
        public int Shown { get;private set; }
        public ICommand? NewCommand { get; } = new RelayCommand(()=> { });
        public ICommand? OpenCommand { get; } = new RelayCommand(()=> { });
        public ICommand? RestoreLastSessionCommand => new RelayCommand(()=> { Restored++;Recovery=false; });
        public ICommand? DismissRecoveryCommand => new RelayCommand(()=> { Dismissed++;Recovery=false; });
        public bool HasRecoverableDraft=>Recovery;
        public bool HasOpenCase=>false;
        public Func<string,Task>? OpenRecent { get; init; }
        public void ShowCase()=>Shown++;
    }
    public void Dispose()=>_folder.Delete(true);
}
