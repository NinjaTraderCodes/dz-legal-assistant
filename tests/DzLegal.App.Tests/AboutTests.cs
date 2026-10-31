using System.Reflection;
using System.Text.RegularExpressions;
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
using DzLegal.App.Views;
using DzLegal.Law;
using DzLegal.Presentation;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

/// <summary>Brief 2, Phase 3: the About page, its notices file, and the two ways to reach it.</summary>
public sealed class AboutTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-about-");

    private static LawLibrary Library => Vm.ShippedKnowledge.Library;

    private static AboutViewModel Create(Loc loc, bool online = false, Func<string?>? notices = null,
        Func<Task>? library = null, Func<Task>? home = null) =>
        new(loc, GuideNavigation.AppVersion, Library, online, notices ?? GuideNavigation.LoadNotices, library, home);

    [Fact]
    public void TheVersionIsTheOneTheBuildStamped()
    {
        var stamped = typeof(GuideNavigation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Equal(stamped, GuideNavigation.AppVersion);
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+$"), GuideNavigation.AppVersion);
        var loc = new Loc();
        using var about = Create(loc);
        Assert.Equal("الإصدار " + stamped, about.VersionText);
        loc.SetLanguage("en");
        Assert.Equal("Version " + stamped, about.VersionText);
    }

    [Fact]
    public void TheSourcesLineIsCountedFromTheLoadedLibrary()
    {
        var loc = new Loc();
        using var about = Create(loc);
        var articles = Library.Codes.Sum(code => code.Articles.Count);
        Assert.True(articles > 0);
        Assert.Equal(articles, about.ArticleCount);
        Assert.Equal(Library.Codes.Count, about.TextCount);
        Assert.StartsWith($"{articles} مادة من {Library.Codes.Count} نصوص قانونية", about.SourcesText, StringComparison.Ordinal);
        loc.SetLanguage("en");
        Assert.StartsWith($"{articles} articles from {Library.Codes.Count} legal texts", about.SourcesText, StringComparison.Ordinal);

        var fictitious = new LawLibrary([Library.Codes[0]]);
        using var small = new AboutViewModel(loc, "1.2.3", fictitious, false, () => null);
        Assert.Equal(Library.Codes[0].Articles.Count, small.ArticleCount);
        Assert.Equal(1, small.TextCount);
    }

    [Fact]
    public void TheWordingMatchesAppendixAAndTheTeamComesInOrder()
    {
        var loc = new Loc();
        using var about = Create(loc);
        Assert.Equal("حول التطبيق", about.Title);
        Assert.Equal("مسودة تراجعها، لا استشارة قانونية", loc["AboutDraftTitle"]);
        Assert.Equal("يعمل دون اتصال، وبياناتك لا تغادر حاسوبك", loc["AboutPrivacyTitle"]);
        Assert.Equal(
            ["هشام بن كيحول — قيادة المشروع وتطويره", "Djihane Ben Djeddou — تصميم الواجهة وتجربة الاستخدام", "خلود عثمان شريف — المراجعة القانونية"],
            about.Team);
        Assert.Equal("طُوِّر هذا المشروع في إطار ThirdUni 2026.", loc["AboutThirdUni"]);
        loc.SetLanguage("en");
        Assert.Equal("About", about.Title);
        Assert.Equal(
            ["Hicham Benkaihoul — project lead and development", "Djihane Ben Djeddou — interface and experience design", "Khouloud Otmane Cherif — legal review"],
            about.Team);
        Assert.Equal("Built as part of ThirdUni 2026.", loc["AboutThirdUni"]);
    }

    [Fact]
    public void EveryLicenceTheBriefNamesIsListed()
    {
        var loc = new Loc();
        using var about = Create(loc);
        string LicenceOf(string name) => about.Licences.Single(item => item.Name.Contains(name, StringComparison.Ordinal)).Licence;
        Assert.Equal("SIL Open Font License 1.1", LicenceOf("Noto Naskh Arabic"));
        Assert.Equal("SIL Open Font License 1.1", LicenceOf("Inter"));
        Assert.Equal("MIT", LicenceOf("Fluent UI System Icons"));
        Assert.Equal("MIT", LicenceOf("CommunityToolkit.Mvvm"));
        Assert.Equal("MIT", LicenceOf("DocumentFormat.OpenXml"));
        Assert.Equal("BSD 2-Clause", LicenceOf("Scriban"));
        Assert.Equal("QuestPDF Community License", LicenceOf("QuestPDF"));
        Assert.Equal("MIT", about.Licences.Single(item => item.Name == "Avalonia").Licence);
        Assert.Equal(loc["AboutOwnImage"], LicenceOf(loc["AboutWelcomeImage"]));
        loc.SetLanguage("en");
        Assert.Equal("The project's own image", LicenceOf("Welcome background image"));
    }

    [Fact]
    public void TheNoticesFileShipsWithTheAppAndHoldsEveryListedLicence()
    {
        Assert.True(File.Exists(GuideNavigation.NoticesPath), GuideNavigation.NoticesPath);
        var text = GuideNavigation.LoadNotices()!;
        using var about = Create(new Loc());
        foreach (var item in about.Licences.Where(item => item.Licence != about.L["AboutOwnImage"]))
        foreach (var name in item.Name.Split(", "))
            Assert.Contains(name.Split(" (")[0], text, StringComparison.Ordinal);
        Assert.Contains("welcome-bg.jpg", text, StringComparison.Ordinal);
        Assert.Contains("SIL OPEN FONT LICENSE Version 1.1", text, StringComparison.Ordinal);
        Assert.Contains("Copyright 2022 The Noto Project Authors", text, StringComparison.Ordinal);
        Assert.Contains("Copyright (c) 2016 The Inter Project Authors", text, StringComparison.Ordinal);
        Assert.Contains("Permission is hereby granted, free of charge", text, StringComparison.Ordinal);
        Assert.Contains("Redistribution and use in source and binary forms", text, StringComparison.Ordinal);
        Assert.Contains("Alexandre Mutel", text, StringComparison.Ordinal);
        Assert.Contains("QuestPDF Community License", text, StringComparison.Ordinal);
        Assert.Contains("Ollama", text, StringComparison.Ordinal);
        Assert.Equal(text, about.NoticesText);
    }

    [Fact]
    public void AMissingNoticesFileShowsAMessageInsteadOfFailing()
    {
        var loc = new Loc();
        using var about = Create(loc, notices: () => null);
        about.ShowNoticesCommand.Execute(null);
        Assert.False(about.HasNotices);
        Assert.Equal(loc["AboutNoticesMissing"], about.NoticesText);
        Assert.Equal(loc["AboutNotices"], about.Title);
        about.BackCommand.Execute(null);
        Assert.Equal(loc["AboutTitle"], about.Title);
    }

    [AvaloniaFact]
    public void TheOnlineNoticeAppearsOnlyWhenAnOnlineEngineIsOffered()
    {
        var loc = new Loc();
        using var offline = Create(loc);
        using var online = Create(loc, online: true);
        Assert.False(offline.ShowsOnlineNotice);
        Assert.True(online.ShowsOnlineNotice);

        var window = new AboutWindow { DataContext = online };
        window.Show(); Layout(window);
        var notice = window.FindControl<TextBlock>("AboutOnlineNotice")!;
        Assert.True(notice.IsEffectivelyVisible);
        Assert.Equal(loc["OnlineEngineStatus"], notice.Text);
        window.DataContext = offline; Layout(window);
        Assert.False(notice.IsEffectivelyVisible);
        window.Close();
    }

    public static TheoryData<string, bool, int, int> Displays()
    {
        var data = new TheoryData<string, bool, int, int>();
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        foreach (var size in new[] { (1366, 768), (1920, 1080) })
            data.Add(language, dark, size.Item1, size.Item2);
        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Displays))]
    public async Task ThePageReadsInOrderAndFitsEveryDisplay(string language, bool dark, int width, int height)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        var opened = 0;
        using var about = Create(loc, library: () => { opened++; return Task.CompletedTask; }, home: () => Task.CompletedTask);
        var window = new AboutWindow
        {
            DataContext = about, Width = width, Height = height,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light,
        };
        window.Show(); Layout(window);
        Assert.Equal(language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, window.FlowDirection);

        // The seven parts, in the brief's order, top to bottom.
        string[] order = ["AboutProduct", "AboutPurpose", "AboutDraft", "AboutPrivacy", "AboutModelNote", "AboutSources", "AboutTeam", "AboutThirdUni", "AboutLicences"];
        var tops = order.Select(name => window.FindControl<Control>(name)!.TranslatePoint(default, window)!.Value.Y).ToArray();
        Assert.Equal(tops.Order().ToArray(), tops);
        Assert.Equal(loc["AppTitle"], window.FindControl<TextBlock>("AboutProduct")!.Text);
        Assert.Equal(about.VersionText, window.FindControl<TextBlock>("AboutVersion")!.Text);
        Assert.Equal(about.SourcesText, window.FindControl<TextBlock>("AboutSources")!.Text);

        var scroll = window.FindControl<ScrollViewer>("AboutScroll")!;
        Assert.True(scroll.Extent.Width <= scroll.Viewport.Width + 0.5, $"{scroll.Extent.Width} > {scroll.Viewport.Width}");
        foreach (var name in new[] { "AboutHome", "AboutNotices" })
        {
            var control = window.FindControl<Control>(name)!;
            Assert.True(control.IsEffectivelyVisible, name);
            var rect = OnWindow(control, window);
            Assert.True(rect.X >= 0 && rect.Right <= window.Bounds.Width + 0.5 && rect.Bottom <= window.Bounds.Height + 0.5, $"{name} {rect}");
        }
        Capture(window, $"phase3-about-{language}-{(dark ? "dark" : "light")}-{width}x{height}");

        scroll.Offset = new Vector(0, scroll.Extent.Height); Layout(window);
        Assert.True(window.FindControl<Control>("AboutLibrary")!.IsEffectivelyVisible);
        await about.LibraryCommand.ExecuteAsync(null);
        Assert.Equal(1, opened);
        Capture(window, $"phase3-about-end-{language}-{(dark ? "dark" : "light")}-{width}x{height}");

        window.FindControl<Button>("AboutNotices")!.Command!.Execute(null); Layout(window);
        var notices = window.FindControl<SelectableTextBlock>("AboutNoticesText")!;
        Assert.True(notices.IsEffectivelyVisible);
        Assert.False(scroll.IsEffectivelyVisible);
        Assert.Equal(FlowDirection.LeftToRight, notices.FlowDirection);
        Assert.StartsWith("DZ Legal Assistant: third-party notices", notices.Text, StringComparison.Ordinal);
        Assert.Equal(loc["AboutNotices"], window.FindControl<TextBlock>("AboutTitle")!.Text);
        Capture(window, $"phase3-about-notices-{language}-{(dark ? "dark" : "light")}-{width}x{height}");

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Layout(window);
        Assert.False(about.IsShowingNotices);
        Assert.True(window.IsVisible);
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Dispatcher.UIThread.RunJobs();
        Assert.True(closed);
    }

    [AvaloniaTheory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task TheStartScreenOpensAboutAndItsHomeButtonReturnsThere(string language)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        using var model = Vm.Create(loc: loc).Filled();
        var window = new MainWindow { DataContext = model, UserDataRoot = _folder.FullName };
        window.Show(); Layout(window);
        var inputs = model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value).ToArray();

        window.FindControl<Button>("HomeButton")!.Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout(window);
        var home = Assert.Single(window.OwnedWindows.OfType<HomeWindow>());
        var button = home.GetVisualDescendants().OfType<Button>().Single(item => item.Name == "HomeAbout");
        Assert.True(button.IsEffectivelyVisible);
        Assert.True(button.IsEnabled);
        Assert.Equal(loc["AboutTitle"], button.Content);

        var aboutTask = home.Home.AboutCommand.ExecuteAsync(null);
        Layout(home);
        var about = Assert.Single(home.OwnedWindows.OfType<AboutWindow>());
        var aboutModel = Assert.IsType<AboutViewModel>(about.DataContext);
        Assert.Equal(GuideNavigation.AppVersion, aboutModel.Version);
        Assert.True(aboutModel.CanOpenLibrary);
        Assert.False(aboutModel.ShowsOnlineNotice);
        await aboutModel.HomeCommand.ExecuteAsync(null);
        await aboutTask;
        Layout(home);
        Assert.DoesNotContain(about, home.OwnedWindows);
        Assert.True(home.IsVisible);
        Assert.Equal(inputs, model.FieldGroups.SelectMany(group => group.Fields).Select(field => field.Value));
        Assert.True(model.HasUnsavedChanges);

        home.Home.BackToCaseCommand.Execute(null);
        Layout(window);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task HelpFromTheWorkbenchOpensAbout(string language)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        using var model = Vm.Create(loc: loc).Filled();
        var window = new MainWindow { DataContext = model, UserDataRoot = _folder.FullName };
        window.Show(); Layout(window);

        window.FindControl<Button>("HelpButton")!.Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout(window);
        var help = Assert.Single(window.OwnedWindows.OfType<HelpWindow>());
        var helpModel = Assert.IsType<HelpViewModel>(help.DataContext);
        var link = help.FindControl<Button>("HelpAbout")!;
        Assert.True(link.IsEffectivelyVisible);
        Assert.True(link.IsEnabled);

        var aboutTask = helpModel.AboutCommand.ExecuteAsync(null);
        Layout(window);
        Assert.DoesNotContain(help, window.OwnedWindows);
        var about = Assert.Single(window.OwnedWindows.OfType<AboutWindow>());
        about.Close();
        await aboutTask;
        Layout(window);
        Assert.Same(model, window.DataContext);
        window.Close();
    }

    [AvaloniaFact]
    public void HelpHidesTheAboutLinkWhenNoPageIsWired()
    {
        using var help = new HelpViewModel(new Loc(), () => Task.CompletedTask);
        var window = new HelpWindow { DataContext = help };
        window.Show(); Layout(window);
        Assert.False(help.CanOpenAbout);
        Assert.False(window.FindControl<Button>("HelpAbout")!.IsEffectivelyVisible);
        window.Close();
    }

    [Fact]
    public void TheStartScreenAboutWaitsForItsAction()
    {
        using var home = new HomeViewModel(new RecentCasesStore(_folder.FullName), new ProfileStore(_folder.FullName), () => Task.CompletedTask,
            guides: new HomeGuideActions(() => Task.CompletedTask, () => Task.CompletedTask, () => Task.CompletedTask));
        Assert.False(home.AboutCommand.CanExecute(null));
    }

    [AvaloniaTheory]
    [InlineData("ar", 1366, 768)]
    [InlineData("en", 1366, 768)]
    [InlineData("ar", 1920, 1080)]
    [InlineData("en", 1920, 1080)]
    public void TheStartScreenKeepsTheOfflineNoteCentredBesideAbout(string language, int width, int height)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        using var home = new HomeViewModel(new RecentCasesStore(_folder.FullName), new ProfileStore(_folder.FullName), () => Task.CompletedTask,
            localization: loc, guides: new HomeGuideActions(() => Task.CompletedTask, () => Task.CompletedTask, () => Task.CompletedTask,
                () => Task.CompletedTask, () => Task.CompletedTask));
        var view = new HomeView { DataContext = home };
        var window = new Window { Content = view, Width = width, Height = height };
        window.Show(); Layout(window);
        var row = OnWindow(view.FindControl<Grid>("HomeNoteRow")!, window);
        var note = view.FindControl<TextBlock>("HomeOffline")!;
        var about = view.FindControl<Button>("HomeAbout")!;
        Assert.True(about.IsEffectivelyEnabled);
        var noteRect = OnWindow(note, window);
        var aboutRect = OnWindow(about, window);
        Assert.InRange(noteRect.Center.X, row.Center.X - 1, row.Center.X + 1);
        Assert.False(noteRect.Intersects(aboutRect), $"note {noteRect} meets About {aboutRect}");
        // About sits at the row's end: the right in English, the left in Arabic.
        var gapToEnd = language == "ar" ? aboutRect.Left - row.Left : row.Right - aboutRect.Right;
        Assert.InRange(gapToEnd, -1, 1);
        Capture(window, $"fixes-home-about-{language}-{width}x{height}");
        window.Close();
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
