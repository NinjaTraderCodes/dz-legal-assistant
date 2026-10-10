using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Views;

namespace DzLegal.App.Tests;

public class RenderingTests
{
    /// <summary>Screenshots go to $DZLEGAL_SCREENSHOTS when set, for visual review of the RTL layout.</summary>
    private static void SaveScreenshot(Window window, string name)
    {
        var folder = Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS");
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        using var frame = window.CaptureRenderedFrame();
#pragma warning disable CS0618 // PNG default encoder options are fine for review screenshots.
        frame?.Save(Path.Combine(folder, name + ".png"));
#pragma warning restore CS0618
    }

    private static async Task<(MainWindow Window, MainWindowViewModel Vm)> ShowFilledWindowAsync()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, vm);
    }

    [AvaloniaFact]
    public async Task MainWindowIsRightToLeftInArabic()
    {
        var (window, _) = await ShowFilledWindowAsync();

        Assert.Equal(FlowDirection.RightToLeft, window.FlowDirection);
        var banner = window.FindControl<TextBlock>("DraftBanner")!;
        Assert.StartsWith("مسودة", banner.Text, StringComparison.Ordinal);
        Assert.Equal("جميع البيانات الإلزامية متوفرة.", window.FindControl<TextBlock>("SummaryText")!.Text);

        // In RTL the case form (first column) sits on the right-hand side of the window.
        var formPanel = window.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("panel"));
        var origin = formPanel.TranslatePoint(new Point(0, 0), window)!.Value;
        Assert.True(origin.X > window.Bounds.Width / 2, $"form panel at x={origin.X}");

        SaveScreenshot(window, "main-window-ar");
    }

    [AvaloniaFact]
    public async Task LanguageButtonSwitchesTheWindowToEnglishLeftToRight()
    {
        var (window, vm) = await ShowFilledWindowAsync();

        vm.SwitchLanguageCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(FlowDirection.LeftToRight, window.FlowDirection);
        Assert.StartsWith("Draft", window.FindControl<TextBlock>("DraftBanner")!.Text, StringComparison.Ordinal);
        Assert.Equal("العربية", (string?)window.FindControl<Button>("LanguageButton")!.Content);

        // The document and the case inputs stay Arabic and right-to-left.
        var editor = window.GetVisualDescendants().OfType<TextBox>().First(t => t.Classes.Contains("document"));
        Assert.Equal(FlowDirection.RightToLeft, editor.FlowDirection);
        var input = window.GetVisualDescendants().OfType<TextBox>().First(t => t.Classes.Contains("input"));
        Assert.Equal(FlowDirection.RightToLeft, input.FlowDirection);

        SaveScreenshot(window, "main-window-en");
    }

    [AvaloniaFact]
    public async Task MissingElementsAreShownInTheReport()
    {
        var (window, vm) = await ShowFilledWindowAsync();

        vm.Field("defendant.domicile").Value = string.Empty;
        vm.Sections.Single(s => s.Id == "grounds").Text = string.Empty;
        Dispatcher.UIThread.RunJobs();
        Assert.StartsWith("حيث إن العارض", vm.Sections.Single(s => s.Id == "facts").Text, StringComparison.Ordinal);

        Assert.Equal("ينقص 2 من البيانات الإلزامية: ترفض المحكمة العريضة شكلا.", window.FindControl<TextBlock>("SummaryText")!.Text);
        SaveScreenshot(window, "main-window-missing");
    }

    [AvaloniaFact]
    public async Task ReviewHintIsShownUnderANarrativeSection()
    {
        var (window, vm) = await ShowFilledWindowAsync();
        vm.Field("plaintiff.gender").Value = "male";
        vm.Sections.Single(s => s.Id == "grounds").Text += "\nوحيث إن المبلغ أصبح 999 دج.";
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),
            t => t.Classes.Contains("review") && t.IsVisible && t.Text!.Contains("999", StringComparison.Ordinal));
        SaveScreenshot(window, "main-window-review");
    }

    [AvaloniaFact]
    public async Task PreviewTabShowsPaperPages()
    {
        var (window, vm) = await ShowFilledWindowAsync();
        vm.Field("plaintiff.gender").Value = "male";
        vm.Field("defendant.gender").Value = "male";
        vm.Field("lawyer.gender").Value = "female";

        vm.SelectedDraftTab = 1;
        await vm.RefreshPreviewAsync();
        Dispatcher.UIThread.RunJobs();

        var pages = window.GetVisualDescendants().OfType<Image>().Where(i => i.IsVisible && i.Source is not null).ToList();
        Assert.NotEmpty(pages);
        SaveScreenshot(window, "main-window-preview");
    }

    [AvaloniaFact]
    public async Task DarkThemeRenders()
    {
        var (window, vm) = await ShowFilledWindowAsync();
        vm.ToggleThemeCommand.Execute(null);
        vm.SelectedStep = vm.FieldGroups.Single(g => g.Id == "plaintiff");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Avalonia.Styling.ThemeVariant.Dark, window.ActualThemeVariant);
        SaveScreenshot(window, "main-window-dark");
    }

    [AvaloniaFact]
    public async Task GroundsStepShowsTheAnalysis()
    {
        var vm = Vm.Create(recorder: Analysis.Model()).Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.SelectStepCommand.Execute(vm.FieldGroups.Single(g => g.Id == "grounds"));
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        vm.AnalysedArticles[0].IsSelected = true;
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.FindControl<StackPanel>("GroundsPanel")!.IsVisible);
        Assert.True(window.FindControl<Button>("AnalyseButton")!.IsEnabled);
        Assert.False(window.FindControl<Border>("StaleBanner")!.IsVisible);
        SaveScreenshot(window, "main-window-grounds");

        window.FindControl<ItemsControl>("SelectedGroundsList")!.BringIntoView();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window, "main-window-grounds-selected");
    }

    [AvaloniaFact]
    public async Task TheActivityPanelShowsTheModelReadingTheFactsLive()
    {
        var model = new PausingProvider(r => r.SectionId switch
        {
            Drafting.GroundsAnalyzer.ReadingRequestId => Analysis.Reading,
            Drafting.GroundsAnalyzer.SelectionRequestId => Analysis.Selection,
            _ => r.SourceText,
        });
        var vm = Vm.Create(extra: new ProviderOption("paused", _ => "paused", () => model)).Filled();
        vm.SelectStepCommand.Execute(vm.FieldGroups.Single(g => g.Id == "grounds"));
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();

        var running = vm.AnalyseGroundsCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();

        var panel = window.FindControl<ContentControl>("AnalysisActivityView")!;
        Assert.True(vm.AnalysisActivity.IsActive);
        Assert.True(vm.AnalysisActivity.Steps[0].IsActive);
        Assert.Contains("العلاقة القانونية", vm.AnalysisActivity.LiveText, StringComparison.Ordinal);
        Assert.True(panel.IsVisible);
        SaveScreenshot(window, "main-window-activity");

        model.Release();
        await running;
        Dispatcher.UIThread.RunJobs();
        Assert.False(vm.AnalysisActivity.IsActive);
    }

    [AvaloniaFact]
    public async Task TheSectionBeingWrittenIsMarkedWhileTheModelDrafts()
    {
        var model = new PausingProvider(r => r.SourceText);
        var vm = Vm.Create(extra: new ProviderOption("paused", _ => "paused", () => model)).Filled();
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();

        var running = vm.GenerateCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.GenerationActivity.IsActive);
        Assert.Equal(["grounds"], vm.Sections.Where(s => s.IsWriting).Select(s => s.Id));
        Assert.True(window.FindControl<ContentControl>("GenerationActivityView")!.IsVisible);
        SaveScreenshot(window, "main-window-writing");

        model.Release();
        await running;
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(vm.Sections, s => s.IsWriting);
    }

    /// <summary>Writes the first part of each answer, then waits until released: a model caught mid-sentence.</summary>
    private sealed class PausingProvider(Func<Drafting.ModelRequest, string> reply) : Drafting.IModelProvider
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Name => "paused";

        public void Release() => _gate.TrySetResult();

        public Task<string> GenerateAsync(Drafting.ModelRequest request, CancellationToken cancellationToken) => Task.FromResult(reply(request));

        public async IAsyncEnumerable<string> StreamAsync(Drafting.ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var text = reply(request);
            var half = text.Length * 2 / 3;
            yield return text[..half];
            await _gate.Task;
            yield return text[half..];
        }
    }

    [AvaloniaFact]
    public async Task RequestsStepOffersTheUsualRequests()
    {
        var vm = Vm.Create(recorder: Analysis.Model()).Filled();
        await vm.AnalyseGroundsCommand.ExecuteAsync(null);
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.SelectStepCommand.Execute(vm.FieldGroups.Single(g => g.Id == "requests"));
        vm.RequestChips[0].InsertCommand.Execute(null);
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.FindControl<StackPanel>("RequestsHelpPanel")!.IsVisible);
        Assert.False(window.FindControl<StackPanel>("GroundsPanel")!.IsVisible);
        SaveScreenshot(window, "main-window-requests");
    }

    [AvaloniaFact]
    public async Task AssistantPanelShowsTheProposalDiff()
    {
        var vm = Vm.Create(provider: "scripted").Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        vm.Sections.Single(s => s.Id == "grounds").OpenAssistantCommand.Execute(null);
        vm.AssistantInstruction = "أكثر رسمية";
        await vm.SendInstructionCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.FindControl<Border>("AssistantPanel")!.IsVisible);
        Assert.True(window.FindControl<Button>("AcceptButton")!.IsEnabled);
        SaveScreenshot(window, "main-window-assistant");
    }

    [AvaloniaFact]
    public void MixedArabicLatinAndDigitsLayOutInBidiOrder()
    {
        // Arabic paragraph with an embedded French term, a Latin name and a number.
        const string text = "حيث إن Maître Benali حرر 500000 دج";
        var typeface = new Typeface(new FontFamily("fonts:Naskh#Noto Naskh Arabic, fonts:Inter#Inter"));
        using var layout = new TextLayout(text, typeface, 16, Brushes.Black, flowDirection: FlowDirection.RightToLeft);

        double X(int index) => layout.HitTestTextPosition(index).X;
        var firstArabic = X(0);                         // ح
        var latinStart = X(text.IndexOf('M', StringComparison.Ordinal));
        var latinNext = X(text.IndexOf('a', StringComparison.Ordinal));
        var digitStart = X(text.IndexOf('5', StringComparison.Ordinal));
        var digitNext = X(text.IndexOf('5', StringComparison.Ordinal) + 1);
        var lastArabic = X(text.Length - 1);            // ج

        Assert.True(firstArabic > latinStart, "Arabic text starts on the right");
        Assert.True(latinStart < latinNext, "Latin run keeps left-to-right order inside the RTL line");
        Assert.True(digitStart < digitNext, "digits keep left-to-right order");
        Assert.True(lastArabic < digitStart, "the paragraph ends on the left");
    }
}
