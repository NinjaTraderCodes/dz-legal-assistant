using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Views;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public sealed class PhaseZeroWorkbenchTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-guide-workbench-");

    [AvaloniaTheory]
    [InlineData("ar", false)]
    [InlineData("en", false)]
    [InlineData("ar", true)]
    [InlineData("en", true)]
    public void HomeAndBackToCaseKeepTheSameUnsavedCaseWithoutASavePrompt(string language, bool escape)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        using var model = Vm.Create(loc: loc).Filled();
        var window = new MainWindow { DataContext = model, UserDataRoot = _folder.FullName };
        window.Show(); Layout(window);
        var inputs = model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value).ToArray();
        var sections = model.Sections.Select(section => section.Text).ToArray();
        var selectedStep = model.SelectedStep;
        var button = window.FindControl<Button>("HomeButton")!;
        button.Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout(window);
        var home = Assert.Single(window.OwnedWindows.OfType<HomeWindow>());
        Assert.True(home.Home.HasOpenCase);
        Assert.True(model.HasUnsavedChanges);
        Assert.False(model.IsConfirmingReplace);
        if (escape)
        {
            home.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        }
        else home.Home.BackToCaseCommand.Execute(null);
        Layout(window);
        Assert.DoesNotContain(home, window.OwnedWindows);
        Assert.Same(model, window.DataContext);
        Assert.Same(selectedStep, model.SelectedStep);
        Assert.Equal(inputs, model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value));
        Assert.Equal(sections, model.Sections.Select(section => section.Text));
        Assert.True(model.HasUnsavedChanges);
        Assert.False(model.IsConfirmingReplace);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task HelpHomeFromTheStartScreenClosesHelpAndLeavesTheCaseUntouched(string language)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        using var model = Vm.Create(loc: loc).Filled();
        var window = new MainWindow { DataContext = model, UserDataRoot = _folder.FullName };
        window.Show(); Layout(window);
        var inputs = model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value).ToArray();
        var sections = model.Sections.Select(section => section.Text).ToArray();
        var selectedStep = model.SelectedStep;

        window.FindControl<Button>("HomeButton")!.Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout(window);
        var home = Assert.Single(window.OwnedWindows.OfType<HomeWindow>());
        Assert.True(home.Home.HelpCommand.CanExecute(null));

        var helpTask = home.Home.HelpCommand.ExecuteAsync(null);
        Layout(home);
        var help = Assert.Single(home.OwnedWindows.OfType<HelpWindow>());
        var helpModel = Assert.IsType<HelpViewModel>(help.DataContext);
        var helpHome = help.FindControl<Button>("HelpHome")!;
        Assert.True(helpHome.IsEffectivelyVisible);
        Assert.True(helpHome.IsEnabled);
        Assert.Equal(loc["Home"], helpHome.GetVisualDescendants().OfType<TextBlock>().Single().Text);

        await helpModel.HomeCommand.ExecuteAsync(null);
        await helpTask;
        Layout(home);
        Assert.DoesNotContain(help, home.OwnedWindows);
        Assert.True(home.IsVisible);
        Assert.Same(model, window.DataContext);
        Assert.Same(selectedStep, model.SelectedStep);
        Assert.Equal(inputs, model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value));
        Assert.Equal(sections, model.Sections.Select(section => section.Text));
        Assert.True(model.HasUnsavedChanges);
        Assert.False(model.IsConfirmingReplace);

        home.Home.BackToCaseCommand.Execute(null);
        Layout(window);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task HelpHomeFromTheWorkbenchOpensTheStartScreenAndLeavesTheCaseUntouched(string language)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        using var model = Vm.Create(loc: loc).Filled();
        var window = new MainWindow { DataContext = model, UserDataRoot = _folder.FullName };
        window.Show(); Layout(window);
        var inputs = model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value).ToArray();
        var sections = model.Sections.Select(section => section.Text).ToArray();
        var selectedStep = model.SelectedStep;

        var helpButton = window.FindControl<Button>("HelpButton")!;
        helpButton.Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout(window);
        var help = Assert.Single(window.OwnedWindows.OfType<HelpWindow>());
        var helpModel = Assert.IsType<HelpViewModel>(help.DataContext);
        var helpHome = help.FindControl<Button>("HelpHome")!;
        Assert.True(helpHome.IsEffectivelyVisible);
        Assert.True(helpHome.IsEnabled);
        Assert.Equal(loc["Home"], helpHome.GetVisualDescendants().OfType<TextBlock>().Single().Text);

        var homeTask = helpModel.HomeCommand.ExecuteAsync(null);
        Layout(window);
        var home = Assert.Single(window.OwnedWindows.OfType<HomeWindow>());
        Assert.True(home.Home.HasOpenCase);
        Assert.DoesNotContain(help, window.OwnedWindows);
        Assert.Same(model, window.DataContext);
        Assert.Same(selectedStep, model.SelectedStep);
        Assert.Equal(inputs, model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value));
        Assert.Equal(sections, model.Sections.Select(section => section.Text));
        Assert.True(model.HasUnsavedChanges);
        Assert.False(model.IsConfirmingReplace);

        home.Home.BackToCaseCommand.Execute(null);
        Layout(window);
        await homeTask;
        Assert.DoesNotContain(home, window.OwnedWindows);
        window.Close();
    }

    [AvaloniaFact]
    public void ProfileOpensFromTheWorkbenchAndDoesNotChangeTheCurrentCase()
    {
        using var model = Vm.Create().Filled();
        var originalLawyer = model.Field("lawyer.name").Value;
        var window = new MainWindow { DataContext = model, UserDataRoot = _folder.FullName };
        window.Show(); Layout(window);
        window.FindControl<Button>("ProfileButton")!.Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout(window);
        var profile = Assert.Single(window.OwnedWindows.OfType<ProfileSettingsWindow>());
        Assert.True(profile.Setup.IsEditing);
        Assert.Same(model.L, profile.Setup.L.Source);
        profile.Setup.NameAr = "Fictitious profile";
        profile.Setup.SaveCommand.Execute(null);
        profile.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Layout(window);
        Assert.DoesNotContain(profile, window.OwnedWindows);
        Assert.Equal("Fictitious profile", new ProfileStore(_folder.FullName).Load().NameAr);
        Assert.Equal(originalLawyer, model.Field("lawyer.name").Value);
        Assert.True(model.HasUnsavedChanges);
        window.Close();
    }

    [AvaloniaFact]
    public void ProfileEscapeKeepsUnsavedProfileEditsBehindTheExistingConfirmation()
    {
        var profile = new ProfileSettingsWindow(_folder.FullName);
        profile.Show(); Layout(profile);
        profile.Setup.NameAr = "Fictitious unsaved profile";
        profile.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        profile.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Layout(profile);
        Assert.True(profile.IsVisible);
        Assert.True(profile.Setup.IsConfirmingLeave);
        Assert.Equal("Fictitious unsaved profile", profile.Setup.NameAr);
        Assert.False(File.Exists(Path.Combine(_folder.FullName, "profile.json")));
        profile.Setup.CancelLeaveCommand.Execute(null);
        Assert.True(profile.Setup.IsDirty);
        profile.Setup.DiscardAndLeaveCommand.Execute(null);
    }

    [AvaloniaFact]
    public async Task PhaseZeroScreensRenderWithAccessibleControlsAndNoTitleBarOverflow()
    {
        foreach (var size in new[] { (1366, 768), (1920, 1080) })
        foreach (var scale in new[] { 1d, 1.25 })
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        {
            var loc = new Loc(); loc.SetLanguage(language);
            using var model = Vm.Create(loc: loc).Filled();
            model.IsDarkTheme = dark;
            var window = new MainWindow { DataContext = model, UserDataRoot = _folder.FullName,
                Width = size.Item1 / scale, Height = size.Item2 / scale };
            window.Show(); window.SetRenderScaling(scale); Layout(window);
            Assert.InRange(window.Bounds.Width, size.Item1 / scale - 1, size.Item1 / scale + 1);
            Assert.InRange(window.Bounds.Height, size.Item2 / scale - 1, size.Item2 / scale + 1);
            var bar = window.FindControl<Border>("WorkbenchTitleBar")!;
            var buttons = bar.GetVisualDescendants().OfType<Button>().Where(button => button.IsEffectivelyVisible).ToArray();
            Assert.Contains(window.FindControl<Button>("HomeButton")!, buttons);
            Assert.Contains(window.FindControl<Button>("ProfileButton")!, buttons);
            var rectangles = buttons.Select(button => OnWindow(button, window)).ToArray();
            foreach (var rectangle in rectangles)
            {
                Assert.InRange(rectangle.Left, -1, window.Bounds.Width);
                Assert.InRange(rectangle.Right, 0, window.Bounds.Width + 1);
                Assert.InRange(rectangle.Top, -1, bar.Bounds.Height + 1);
                Assert.InRange(rectangle.Bottom, 0, bar.Bounds.Height + 1);
            }
            for (var first = 0; first < rectangles.Length; first++)
            for (var second = first + 1; second < rectangles.Length; second++)
                Assert.False(rectangles[first].Intersects(rectangles[second]), $"Overlapping title buttons: {language}/{dark}/{size}/{scale}");
            Assert.Equal(loc["Home"], AutomationProperties.GetName(window.FindControl<Button>("HomeButton")!));
            Assert.Equal(loc["ProfileButton"], AutomationProperties.GetName(window.FindControl<Button>("ProfileButton")!));
            Assert.Equal(loc["ProfileButtonHint"], ToolTip.GetTip(window.FindControl<Button>("ProfileButton")!));
            if (size.Item1 == 1366 && scale == 1) Capture(window, $"phase0-workbench-{language}-{(dark ? "dark" : "light")}-1366x768");
            window.Close();

            var store = new RecentCasesStore(_folder.FullName);
            store.Clear();
            for (var index = 1; index <= 3; index++)
            {
                var path = Path.Combine(_folder.FullName, $"fictional-case-{index}" + DraftFile.Extension);
                await File.WriteAllTextAsync(path, "{}");
                store.RecordSaved(path, language == "ar" ? "\u0645\u0648\u0636\u0648\u0639 \u0627\u0641\u062a\u0631\u0627\u0636\u064a " + index : "Fictitious case " + index);
            }
            using var homeModel = Vm.Create(loc: loc);
            var homeWorkbench = new Window { DataContext = homeModel };
            using var workspace = new HomeWorkspace(homeWorkbench, () => { }, openRecent: _ => Task.CompletedTask);
            using var home = new HomeViewModel(store, new ProfileStore(_folder.FullName), () => Task.CompletedTask,
                workspace, localization: loc, guides: new HomeGuideActions(() => Task.CompletedTask, () => Task.CompletedTask, () => Task.CompletedTask, () => Task.CompletedTask, () => Task.CompletedTask));
            Assert.False(home.WorkspaceUnavailable);
            Assert.False(home.RecentOpeningUnavailable);
            var view = new HomeView { DataContext = home };
            var homeWindow = new Window { Content = view, Width = size.Item1 / scale, Height = size.Item2 / scale,
                RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light };
            homeWindow.Show(); homeWindow.SetRenderScaling(scale); Layout(homeWindow);
            Assert.Equal(3, home.RecentCases.Count);
            foreach (var name in new[] { "HomeSample", "HomeTour", "HomeHelp", "HomeLibrary", "HomeAbout" })
                Assert.True(view.FindControl<Button>(name)!.IsEnabled);
            if (size.Item1 == 1366 && scale == 1) Capture(homeWindow, $"phase0-recents-{language}-{(dark ? "dark" : "light")}-1366x768");
            homeWindow.Close();
        }
    }

    [Fact]
    public void ProfileWordingMatchesAppendixAInBothResourceFiles()
    {
        var loc = new Loc();
        Assert.Equal("\u0627\u0644\u0645\u0644\u0641 \u0627\u0644\u0645\u0647\u0646\u064a", loc["ProfileButton"]);
        Assert.Equal("\u0627\u0644\u0645\u0644\u0641 \u0627\u0644\u0645\u0647\u0646\u064a \u0648\u0627\u0644\u062a\u0641\u0636\u064a\u0644\u0627\u062a", loc["ProfileButtonHint"]);
        loc.SetLanguage("en");
        Assert.Equal("Profile", loc["ProfileButton"]);
        Assert.Equal("Profile and settings", loc["ProfileButtonHint"]);
    }

    private static Rect OnWindow(Control control, Window window)
    {
        var first = control.TranslatePoint(default, window)!.Value;
        var last = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), window)!.Value;
        return new Rect(Math.Min(first.X, last.X), Math.Min(first.Y, last.Y), Math.Abs(first.X - last.X), Math.Abs(first.Y - last.Y));
    }

    private static void Layout(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame()?.Dispose(); Dispatcher.UIThread.RunJobs();
    }

    private static void Capture(Window window, string name)
    {
        if (Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is not { Length: > 0 } folder) return;
        Directory.CreateDirectory(folder);
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Combine(folder, name + ".png"), PngBitmapEncoderOptions.Default);
    }

    public void Dispose() => _folder.Delete(true);
}
