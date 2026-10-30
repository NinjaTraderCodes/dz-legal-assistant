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

public sealed class PhaseOneGuideTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-phase1-");
    private static SampleCase Debt => SampleCase.LoadAll(Path.Combine(AppContext.BaseDirectory, "Samples")).Single(item => item.Id == "debt");

    [AvaloniaTheory]
    [InlineData("ar")]
    [InlineData("en")]
    public void TourFindsAllSixAnchorsAndRestoresCaseAndFocusWithoutStartingAi(string language)
    {
        var loc = new Loc(); loc.SetLanguage(language);
        var recorder = Analysis.Model();
        using var model = Vm.Create(loc: loc, recorder: recorder, analyseInBackground: true).Filled();
        model.SelectedStep = model.FieldGroups.Single(step => step.Id == "facts");
        var window = new MainWindow { DataContext = model, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        var focus = window.FindControl<Button>("HelpButton")!;
        focus.Focus();
        var inputs = model.CurrentInputs().ToArray();
        var sections = model.Sections.Select(section => section.Text).ToArray();
        var provider = model.SelectedProvider;
        var mark = model.DraftWatermark;
        var step = model.SelectedStep;
        var requests = recorder.Requests.Count;
        var overlay = window.FindControl<TourOverlay>("GuideOverlay")!;
        model.StartTour(); Layout(window);
        for (var index = 0; index < 6; index++)
        {
            Assert.Equal(index, model.Tour.Index);
            Assert.NotNull(overlay.HighlightBounds);
            var outline = Assert.IsType<Border>(overlay.FindControl<Canvas>("Shade")!.Children.Last());
            var targetPoints = model.Tour.AnchorNames.SelectMany(name =>
            {
                var target = window.FindControl<Control>(name)!;
                return new[] { target.TranslatePoint(default, window)!.Value, target.TranslatePoint(new Point(target.Bounds.Width, target.Bounds.Height), window)!.Value };
            }).ToArray();
            var outlineStart = outline.TranslatePoint(default, window)!.Value;
            var outlineEnd = outline.TranslatePoint(new Point(outline.Bounds.Width, outline.Bounds.Height), window)!.Value;
            Assert.True(Math.Abs(Math.Min(outlineStart.X, outlineEnd.X) - targetPoints.Min(point => point.X)) <= 1,
                $"Stop {index}: target={string.Join(';', targetPoints)}, local={overlay.HighlightBounds}, overlay={overlay.Bounds}, shade={overlay.FindControl<Canvas>("Shade")!.Bounds}, outline={outline.Bounds}, rendered={outlineStart}/{outlineEnd}");
            Assert.InRange(Math.Min(outlineStart.X, outlineEnd.X), targetPoints.Min(point => point.X) - 1, targetPoints.Min(point => point.X) + 1);
            Assert.InRange(Math.Max(outlineStart.X, outlineEnd.X), targetPoints.Max(point => point.X) - 1, targetPoints.Max(point => point.X) + 1);
            Assert.Contains(model.Tour.Title, AutomationProperties.GetName(overlay.FindControl<Border>("TourCard")!), StringComparison.Ordinal);
            Assert.Same(overlay.FindControl<Border>("TourCard"), window.FocusManager!.GetFocusedElement());
            window.KeyPress(language == "ar" ? Key.Left : Key.Right, RawInputModifiers.None, language == "ar" ? PhysicalKey.ArrowLeft : PhysicalKey.ArrowRight, null);
            Layout(window);
        }
        Assert.False(model.Tour.IsOpen);
        Assert.Same(step, model.SelectedStep);
        Assert.Same(provider, model.SelectedProvider);
        Assert.Equal(mark, model.DraftWatermark);
        Assert.Equal(inputs, model.CurrentInputs().ToArray());
        Assert.Equal(sections, model.Sections.Select(section => section.Text));
        Assert.Equal(requests, recorder.Requests.Count);
        Assert.Same(focus, window.FocusManager!.GetFocusedElement());
        window.Close();
    }

    [AvaloniaFact]
    public void HiddenAnchorCentresTheCardAndEscapeRestoresTheStep()
    {
        using var model = Vm.Create();
        var window = new MainWindow { DataContext = model, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        var step = model.SelectedStep;
        model.StartTour();
        for (var index = 0; index < 4; index++) model.Tour.NextCommand.Execute(null);
        window.FindControl<Border>("CheckPanel")!.IsVisible = false;
        Layout(window);
        var overlay = window.FindControl<TourOverlay>("GuideOverlay")!;
        overlay.PositionCard(); Layout(window);
        Assert.Null(overlay.HighlightBounds);
        var card = overlay.FindControl<Border>("TourCard")!;
        Assert.InRange(Canvas.GetLeft(card) + card.Bounds.Width / 2, overlay.Bounds.Width / 2 - 1, overlay.Bounds.Width / 2 + 1);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Layout(window);
        Assert.False(model.Tour.IsOpen);
        Assert.Same(step, model.SelectedStep);
        window.Close();
    }

    [Fact]
    public async Task TourWithoutACaseUsesNewAndCancelPreservesUnsavedWork()
    {
        using var model = Vm.Create().Filled();
        var before = model.CurrentInputs().ToArray();
        model.StartTour(hasOpenCase: false);
        Assert.True(model.IsConfirmingReplace);
        Assert.False(model.Tour.IsOpen);
        model.CancelReplaceCommand.Execute(null);
        Assert.Equal(before, model.CurrentInputs().ToArray());
        model.StartTour(hasOpenCase: false);
        await model.ReplaceWithoutSavingCommand.ExecuteAsync(null);
        Assert.True(model.Tour.IsOpen);
        Assert.Empty(model.Field("notes.facts").Value);
        Assert.False(model.HasUnsavedChanges);
        model.Tour.Close();
    }

    [AvaloniaFact]
    public void FirstSetupOffersTourOnceAndYesUsesAnEmptyCase()
    {
        using var model = Vm.Create(noLocalModel: true);
        var workbench = new MainWindow { DataContext = model };
        var preferences = new WelcomePreferences(_folder.FullName);
        var window = new OnboardingWindow(preferences, () => workbench);
        window.Show();
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(window.Content).DataContext).StartCommand.Execute(null);
        window.Setup.GoNextCommand.Execute(null); window.Setup.GoNextCommand.Execute(null);
        window.Setup.FinishCommand.Execute(null);
        var offer = Assert.IsType<TourOfferViewModel>(Assert.IsType<TourOfferView>(window.Content).DataContext);
        Assert.True(new GuidePreferences(_folder.FullName).TourOffered);
        offer.YesCommand.Execute(null); Layout(workbench);
        Assert.True(model.Tour.IsOpen);
        Assert.Empty(model.Field("notes.facts").Value);
        model.Tour.Close(); workbench.Close();
        var next = new OnboardingWindow(preferences, () => new Window());
        Assert.IsType<WelcomeViewModel>(Assert.IsType<WelcomeView>(next.Content).DataContext).StartCommand.Execute(null);
        Assert.IsType<HomeView>(next.Content);
        next.OpenSetupStepOne(); next.Setup.GoNextCommand.Execute(null); next.Setup.GoNextCommand.Execute(null); next.Setup.FinishCommand.Execute(null);
        Assert.IsType<HomeView>(next.Content);
        next.Close();
    }

    [Fact]
    public async Task SampleTasksFollowRealAnalysisConfirmationReviewAndSuccessfulExport()
    {
        var files = new FakeFiles();
        using var model = Vm.Create(files, recorder: Analysis.Model());
        await model.OpenSampleAsync(Debt);
        var guide = Assert.IsType<SampleGuideViewModel>(model.SampleGuide);
        Assert.All(guide.Tasks.Where(task => task.Number != 5), task => Assert.False(task.IsComplete));
        Assert.True(guide.Tasks[4].IsComplete);
        var inputs = model.CurrentInputs().ToArray();
        foreach (var task in guide.Tasks) task.ShowMeCommand.Execute(null);
        Assert.Equal(inputs, model.CurrentInputs().ToArray());
        Assert.Null(model.Analysis);
        Assert.False(model.HasGeneratedDraft);
        Assert.True(guide.Tasks[0].IsComplete);
        await model.AnalyseGroundsCommand.ExecuteAsync(null);
        Assert.True(guide.Tasks[1].IsComplete);
        Assert.False(guide.Tasks[2].IsComplete);
        model.ConfirmClaimCommand.Execute(null);
        Assert.True(guide.Tasks[2].IsComplete);
        await model.GenerateCommand.ExecuteAsync(null);
        Assert.True(guide.Tasks[3].IsComplete);
        Assert.True(model.HasUnreviewedAiSections);
        Assert.False(guide.Tasks[4].IsComplete);
        await model.ExportPdfCommand.ExecuteAsync(null);
        Assert.False(guide.Tasks[5].IsComplete);
        foreach (var section in model.Sections.Where(section => section.NeedsReview)) section.IsReviewed = true;
        Assert.True(guide.Tasks[4].IsComplete);
        model.Field("notes.facts").Value += " وقائع إضافية للاختبار.";
        Assert.True(model.IsAnalysisStale);
        Assert.False(guide.Tasks[1].IsComplete);
    }

    [Fact]
    public async Task SampleReplacementAsksFirstAndWatermarkDefaultReturnsOnlyWhenLeaving()
    {
        using var model = Vm.Create(noLocalModel: true, profileDefaults: new UnmarkedProfile()).Filled();
        model.DraftWatermark = false;
        var inputs = model.CurrentInputs().ToArray();
        await model.OpenSampleAsync(Debt);
        Assert.False(model.IsSampleCase);
        Assert.True(model.IsConfirmingReplace);
        model.CancelReplaceCommand.Execute(null);
        Assert.Equal(inputs, model.CurrentInputs().ToArray());
        await model.OpenSampleAsync(Debt); await model.ReplaceWithoutSavingCommand.ExecuteAsync(null);
        Assert.True(model.IsSampleCase);
        model.DraftWatermark = false;
        Assert.True(model.DraftWatermark);
        model.Field("notes.facts").Value += " changed";
        model.NewCommand.Execute(null);
        Assert.True(model.IsSampleCase);
        model.CancelReplaceCommand.Execute(null);
        Assert.True(model.IsSampleCase);
        model.NewCommand.Execute(null); await model.ReplaceWithoutSavingCommand.ExecuteAsync(null);
        Assert.False(model.IsSampleCase);
        Assert.Null(model.SampleGuide);
        Assert.False(model.DraftWatermark);
    }

    [Fact]
    public void HelpHasSevenOfflinePagesAndAllDynamicStringsInBothLanguages()
    {
        foreach (var language in new[] { "ar", "en" })
        {
            var loc = new Loc(); loc.SetLanguage(language);
            using var help = new HelpViewModel(loc, () => Task.CompletedTask);
            Assert.Equal(7, help.Pages.Count);
            foreach (var page in help.Pages)
            {
                page.OpenCommand.Execute(null);
                Assert.False(help.IsIndex);
                Assert.Equal(page.Title, help.Title);
                Assert.All(help.Paragraphs, paragraph => { Assert.False(string.IsNullOrWhiteSpace(paragraph.Text)); Assert.DoesNotContain("[Help", paragraph.Text, StringComparison.Ordinal); });
                help.BackCommand.Execute(null); Assert.True(help.IsIndex);
            }
            foreach (var prefix in new[] { "Tour", "SampleTask" })
            for (var index = 1; index <= 6; index++)
            foreach (var suffix in prefix == "Tour" ? new[] { "Title", "Text" } : new[] { "", "Hint" })
                Assert.DoesNotContain("[", loc[prefix + index + suffix], StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task SampleRecordsOnlyAnActualSaveAndFailedOrCancelledExportsNeverFinishIt()
    {
        var store = new RecentCasesStore(_folder.FullName);
        var files = new GuideFiles(Path.Combine(_folder.FullName, "sample" + DraftFile.Extension));
        using var model = Vm.Create(files, noLocalModel: true, recentCases: store);
        await model.OpenSampleAsync(Debt);
        Assert.Empty(store.Load());
        await model.GenerateCommand.ExecuteAsync(null);
        CompletePlaceholder(model);
        Assert.False(model.HasExportBlockers, model.ExportBlockers);
        files.Cancel = true;
        await model.ExportPdfCommand.ExecuteAsync(null);
        Assert.False(model.SampleExportSucceeded);
        files.Cancel = false; files.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => model.ExportWordCommand.ExecuteAsync(null));
        Assert.False(model.SampleExportSucceeded);
        Assert.Empty(store.Load());
        files.Fail = false;
        await model.ExportWordCommand.ExecuteAsync(null);
        Assert.True(model.SampleExportSucceeded);
        Assert.Empty(store.Load());
        await model.SaveCommand.ExecuteAsync(null);
        Assert.StartsWith(model.L["SampleFilePrefix"], files.SuggestedName, StringComparison.Ordinal);
        Assert.Equal(files.LastSavedPath, Assert.Single(store.Load()).Path);
        Assert.True(model.IsSampleCase);
        await model.OpenRecentCaseAsync(files.LastSavedPath!);
        Assert.False(model.IsSampleCase);
        Assert.Null(model.SampleGuide);
        Assert.True(File.Exists(files.LastSavedPath));
    }

    [Fact]
    public void DesktopDebtCopyIsByteForByteTheWebSampleAndSharedLoaderRetainsAllFour()
    {
        var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DzLegal.Web", "Samples"));
        Assert.Equal(File.ReadAllBytes(Path.Combine(source, "debt.json")), File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Samples", "debt.json")));
        var samples = SampleCase.LoadAll(source);
        Assert.Equal(new[] { "debt", "delivery", "land", "rent" }, samples.Select(sample => sample.Id).Order(StringComparer.Ordinal));
        foreach (var sample in samples)
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(source, sample.Id + ".json")));
            var inputs = document.RootElement.GetProperty("inputs").EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString()!);
            Assert.Equal(inputs.OrderBy(pair => pair.Key, StringComparer.Ordinal), sample.Inputs.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void EveryGuideResourceExistsDirectlyInBothResourceFiles()
    {
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DzLegal.Presentation", "Resources"));
        string[] Keys(string name)
        {
            var data = System.Xml.Linq.XDocument.Load(Path.Combine(folder, name)).Root!.Elements("data")
                .Where(item => new[] { "Tour", "Sample", "Help", "SetupOfferTour" }.Any(prefix => item.Attribute("name")!.Value.StartsWith(prefix, StringComparison.Ordinal))).ToArray();
            Assert.All(data, item => Assert.False(string.IsNullOrWhiteSpace(item.Element("value")?.Value)));
            return data.Select(item => item.Attribute("name")!.Value).Order(StringComparer.Ordinal).ToArray();
        }
        var arabic = Keys("Strings.resx");
        Assert.Equal(77, arabic.Length);
        Assert.Equal(arabic, Keys("Strings.en.resx"));
    }

    [Fact]
    public async Task AnExportFromAnEarlierSampleCannotCompleteTheNewSample()
    {
        var files = new GuideFiles(Path.Combine(_folder.FullName, "fictitious" + DraftFile.Extension)) { PauseExport = true };
        using var model = Vm.Create(files, noLocalModel: true);
        await model.OpenSampleAsync(Debt);
        await model.GenerateCommand.ExecuteAsync(null); CompletePlaceholder(model);
        var exporting = model.ExportPdfCommand.ExecuteAsync(null);
        await files.PickerOpened.Task.WaitAsync(TimeSpan.FromSeconds(10));
        model.NewCommand.Execute(null); await model.ReplaceWithoutSavingCommand.ExecuteAsync(null);
        await model.OpenSampleAsync(Debt);
        files.ContinueExport.SetResult();
        await exporting;
        Assert.True(model.IsSampleCase);
        Assert.False(model.SampleExportSucceeded);
        Assert.False(model.SampleGuide!.Tasks[5].IsComplete);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{broken")]
    [InlineData("[]")]
    public void MissingAndBrokenTourPreferencesFallBackSafely(string content)
    {
        var preferences = new GuidePreferences(_folder.FullName);
        if (content.Length > 0) File.WriteAllText(preferences.FilePath, content);
        Assert.False(preferences.TourOffered);
        Assert.True(preferences.MarkTourOffered());
        Assert.True(new GuidePreferences(_folder.FullName).TourOffered);
    }

    [AvaloniaFact]
    public async Task PhaseOneScreensRenderInBothLanguagesAndThemes()
    {
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        foreach (var size in new[] { (1366, 768), (1920, 1080) })
        foreach (var scale in new[] { 1d, 1.25 })
        {
            var loc = new Loc(); loc.SetLanguage(language);
            var files = new FakeFiles();
            using var model = Vm.Create(files, loc: loc, recorder: Analysis.Model());
            model.IsDarkTheme = dark;
            var window = new MainWindow { DataContext = model, Width = size.Item1 / scale, Height = size.Item2 / scale };
            window.Show(); window.SetRenderScaling(scale); Layout(window);
            var suffix = $"{language}-{(dark ? "dark" : "light")}-1366x768";
            model.StartTour(); Layout(window);
            if (size.Item1 == 1366 && scale == 1) Capture(window, "phase1-tour1-" + suffix);
            for (var index = 0; index < 3; index++) model.Tour.NextCommand.Execute(null);
            Layout(window);
            if (size.Item1 == 1366 && scale == 1) Capture(window, "phase1-tour4-" + suffix);
            model.Tour.Close();
            await model.OpenSampleAsync(Debt);
            model.SampleGuide!.Tasks[0].ShowMeCommand.Execute(null);
            await model.AnalyseGroundsCommand.ExecuteAsync(null);
            model.SampleGuide.Tasks[2].ShowMeCommand.Execute(null);
            Layout(window);
            Assert.True(window.FindControl<CheckBox>("DraftWatermarkCheck")!.IsChecked);
            Assert.False(window.FindControl<CheckBox>("DraftWatermarkCheck")!.IsEnabled);
            Assert.Equal(loc["SampleWatermarkForced"], ToolTip.GetTip(window.FindControl<CheckBox>("DraftWatermarkCheck")!));
            Assert.True(window.FindControl<SampleGuideView>("SampleGuideView")?.IsEffectivelyVisible ?? window.GetVisualDescendants().OfType<SampleGuideView>().Single().IsEffectivelyVisible);
            if (size.Item1 == 1366 && scale == 1) Capture(window, "phase1-sample-task3-" + suffix);
            model.ConfirmClaimCommand.Execute(null);
            model.SelectedProvider = model.Providers.Single(provider => provider.Key == "stub");
            await model.GenerateCommand.ExecuteAsync(null);
            CompletePlaceholder(model);
            foreach (var section in model.Sections.Where(section => section.NeedsReview)) section.IsReviewed = true;
            await model.ExportPdfCommand.ExecuteAsync(null);
            Assert.True(model.SampleGuide.IsDone, model.ExportBlockers);
            Layout(window);
            if (size.Item1 == 1366 && scale == 1) Capture(window, "phase1-sample-finished-" + suffix);
            using var help = new HelpViewModel(loc, () => Task.CompletedTask);
            var helpWindow = new HelpWindow { DataContext = help, Width = size.Item1 / scale, Height = size.Item2 / scale, RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light };
            helpWindow.Show(); helpWindow.SetRenderScaling(scale); Layout(helpWindow);
            if (size.Item1 == 1366 && scale == 1) Capture(helpWindow, "phase1-help-index-" + suffix);
            help.OpenPage(4); Layout(helpWindow);
            if (size.Item1 == 1366 && scale == 1) Capture(helpWindow, "phase1-help-page-" + suffix);
            helpWindow.Close();
            model.NewCommand.Execute(null); await model.ReplaceWithoutSavingCommand.ExecuteAsync(null); Layout(window);
            Assert.False(model.IsSampleCase);
            Assert.True(window.FindControl<CheckBox>("DraftWatermarkCheck")!.IsEnabled);
            window.Close();
        }
    }

    private static void CompletePlaceholder(MainWindowViewModel model)
    {
        var section = model.Sections.Single(section => section.Id == "grounds");
        section.Text = section.Text.Replace("(...)", "وحيث إن المدعى عليه لم ينفذ التزامه برد مبلغ القرض رغم حلول الأجل والإعذار.", StringComparison.Ordinal);
    }
    private sealed class GuideFiles(string path) : IUserFiles, IHomeRecentFiles
    {
        public bool Cancel { get; set; }
        public bool Fail { get; set; }
        public bool PauseExport { get; set; }
        public TaskCompletionSource PickerOpened { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ContinueExport { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? LastOpenedPath { get; private set; }
        public string? LastSavedPath { get; private set; }
        public string SuggestedName { get; private set; } = string.Empty;
        public async Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind)
        {
            SuggestedName = suggestedName;
            LastSavedPath = null;
            if (Cancel) return null;
            if (Fail) throw new IOException("Fictitious write failure");
            if (PauseExport && kind != SaveKind.Draft) { PickerOpened.SetResult(); await ContinueExport.Task; }
            if (kind == SaveKind.Draft) LastSavedPath = path;
            return (kind == SaveKind.Draft ? File.Create(path) : new MemoryStream(), suggestedName);
        }
        public Task<(Stream Stream, string Name)?> OpenAsync() => OpenPathAsync(path);
        public Task<(Stream Stream, string Name)?> OpenPathAsync(string file)
        {
            LastOpenedPath = file;
            return Task.FromResult<(Stream, string)?>((File.OpenRead(file), Path.GetFileName(file)));
        }
        public Task CopyTextAsync(string text) => Task.CompletedTask;
    }
    private sealed class UnmarkedProfile : IProfileDefaults
    {
        public IReadOnlyDictionary<string, string> Values => new Dictionary<string, string>();
        public bool WatermarkByDefault => false;
    }
    private static void Layout(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.CaptureRenderedFrame()?.Dispose(); Dispatcher.UIThread.RunJobs();
    }
    private static void Capture(Window window, string name)
    {
        if (Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is not { Length: > 0 } folder) return;
        Directory.CreateDirectory(folder);
        using var bitmap = window.CaptureRenderedFrame(); Assert.NotNull(bitmap);
        bitmap.Save(Path.Combine(folder, name + ".png"), PngBitmapEncoderOptions.Default);
    }
    public void Dispose() => _folder.Delete(recursive: true);
}
