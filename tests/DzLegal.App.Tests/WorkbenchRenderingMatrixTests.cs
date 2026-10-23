using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Views;
using DzLegal.Drafting;

namespace DzLegal.App.Tests;

/// <summary>Real Skia frames at native logical size and render scaling; never browser approximations of XAML.</summary>
public sealed class WorkbenchRenderingMatrixTests
{
    public static TheoryData<int, int, double, string, bool> Configurations
    {
        get
        {
            var data = new TheoryData<int, int, double, string, bool>();
            foreach (var size in new[] { (1366, 768), (1920, 1080) })
            foreach (var scale in new[] { 1.0, 1.25 })
            foreach (var language in new[] { "ar", "en" })
            foreach (var dark in new[] { false, true })
                data.Add(size.Item1, size.Item2, scale, language, dark);
            return data;
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Configurations))]
    public async Task MainStatesPreserveGeometryAndVisibleSafety(int pixelsWide, int pixelsHigh, double scale, string language, bool dark)
    {
        string[] states = ["empty", "filled", "analysing", "analysed", "drafting", "blockers", "review", "export-blocked", "export-done", "unsaved", "recovery", "error", "model-unavailable", "without-ai", "source", "assistant"];
        foreach (var state in states)
        {
            var folder = Path.Combine(Path.GetTempPath(), "r3-matrix-" + Guid.NewGuid().ToString("N"));
            var recovery = Path.Combine(folder, "autosave.dzlegal");
            var files = new FakeFiles();
            var gate = new MatrixProvider();
            var provider = state is "analysing" or "drafting" ? new ProviderOption("matrix-local", _ => "Local engine", () => gate) : null;
            if (state == "recovery")
            {
                using var previous = FillFixture(Vm.Create(autosaveFile: recovery));
                previous.AutosaveNow();
            }

            using var vm = Vm.Create(files, provider: state is "error" or "model-unavailable" ? "failing" : state is "blockers" or "assistant" ? "scripted" : "stub",
                autosaveFile: state == "recovery" ? recovery : null,
                recorder: state is "analysed" or "source" ? Analysis.Model() : state == "assistant" ? new RecordingProvider(request => request.SourceText) : null, extra: provider);
            if (language == "en") vm.SwitchLanguageCommand.Execute(null);
            vm.IsDarkTheme = dark;
            if (state is not ("empty" or "recovery")) FillFixture(vm);
            var window = new MainWindow { DataContext = vm, Width = pixelsWide / scale, Height = pixelsHigh / scale };
            window.SetRenderScaling(scale);
            Task? pending = null;
            try
            {
                window.Show();
                if (state == "analysing")
                {
                    vm.SelectedStep = vm.FieldGroups.Single(g => g.Id == "grounds");
                    pending = vm.AnalyseGroundsCommand.ExecuteAsync(null);
                }
                else if (state == "drafting")
                {
                    vm.SelectedStep = vm.FieldGroups.Single(g => g.Id == "facts");
                    pending = vm.GenerateCommand.ExecuteAsync(null);
                }
                else if (state is "analysed" or "source")
                {
                    vm.SelectedStep = vm.FieldGroups.Single(g => g.Id == "grounds");
                    await vm.AnalyseGroundsCommand.ExecuteAsync(null);
                    if (state == "source")
                    {
                        vm.ArticleQuery = "106";
                        vm.ShowWorkbenchToolCommand.Execute("source");
                    }
                }
                else if (state is not ("empty" or "recovery" or "model-unavailable"))
                {
                    vm.SelectedStep = vm.FieldGroups.Single(g => g.Id == "facts");
                    if (state != "filled") await vm.GenerateCommand.ExecuteAsync(null);
                    switch (state)
                    {
                        case "review":
                            vm.ShowWorkbenchToolCommand.Execute("review");
                            break;
                        case "export-blocked":
                            vm.Field("notes.facts").Value += new string('ب', 8001);
                            await vm.ExportPdfCommand.ExecuteAsync(null);
                            break;
                        case "export-done":
                            // The template leaves the unconfirmed legal-ground references as a placeholder.
                            // A lawyer completes that editable section with the original fixture notes before export.
                            vm.Sections.Single(s => s.Id == "grounds").Text = vm.Field("notes.grounds").Value;
                            Assert.False(vm.HasExportBlockers, vm.ExportBlockers);
                            await vm.ExportPdfCommand.ExecuteAsync(null);
                            await vm.ExportWordCommand.ExecuteAsync(null);
                            break;
                        case "unsaved":
                            vm.NewCommand.Execute(null);
                            break;
                        case "assistant":
                            vm.Sections.Single(s => s.Id == "grounds").OpenAssistantCommand.Execute(null);
                            vm.AssistantInstruction = "أكثر رسمية";
                            await vm.SendInstructionCommand.ExecuteAsync(null);
                            break;
                    }
                }
                if (state == "model-unavailable") await vm.CheckModelStatusAsync();
                Dispatcher.UIThread.RunJobs();

                var draft = window.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "DraftPane");
                var form = window.FindControl<Border>("FormPanel")!;
                var draftOrigin = draft.TranslatePoint(default, window)!.Value;
                var formOrigin = form.TranslatePoint(default, window)!.Value;
                Assert.True(draftOrigin.X >= formOrigin.X + form.Bounds.Width - 1, state);
                Assert.True(draft.Bounds.Width >= 439, state);
                Assert.True(form.Bounds.Width >= 319, state);
                Assert.True(vm.IsOutlineCompact == pixelsWide / scale < 1500, state);
                Assert.All(window.GetVisualDescendants().OfType<TextBox>().Where(t => t.Classes.Contains("input") || t.Classes.Contains("document")), t => Assert.Equal(FlowDirection.RightToLeft, t.FlowDirection));
                Assert.All(window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible), t => Assert.True(t.FontSize >= 12.5, $"{state}: {t.Text} = {t.FontSize}"));
                Assert.True(window.FindControl<Grid>("ToolsStrip")!.IsEffectivelyVisible, state);
                if (state is "filled" or "review" or "assistant")
                {
                    var facts = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.DataContext is FieldViewModel { Id: "notes.facts" } && t.IsVisible);
                    var viewport = window.FindControl<ScrollViewer>("FormScroll")!;
                    var origin = facts.TranslatePoint(default, viewport)!.Value;
                    Assert.True(origin.Y >= 0 && origin.Y + facts.Bounds.Height <= viewport.Bounds.Height + 1,
                        $"{state}: facts editor y={origin.Y}, height={facts.Bounds.Height}, viewport={viewport.Bounds.Height}");
                }
                switch (state)
                {
                    case "analysing": Assert.True(vm.AnalysisActivity.IsActive); Assert.NotEmpty(vm.AnalysisActivity.LiveText); break;
                    case "drafting": Assert.Contains(vm.Sections, s => s.IsWriting); break;
                    case "blockers": Assert.True(vm.ShowsExportBlockers); Assert.Contains(vm.Sections, s => s.IsAiWritten && !s.IsReviewed); break;
                    case "review": Assert.True(window.FindControl<Border>("ReviewPanel")!.IsEffectivelyVisible); break;
                    case "export-blocked": Assert.True(vm.IsToolsOpen); Assert.True(vm.IsExportBlockedShown); Assert.Empty(files.SavedByKind); break;
                    case "export-done": Assert.True(files.SavedByKind[SaveKind.Pdf].Length > 1000); Assert.True(files.SavedByKind[SaveKind.Word].Length > 1000); break;
                    case "unsaved": Assert.True(window.FindControl<Border>("ReplaceBar")!.IsEffectivelyVisible); break;
                    case "recovery": Assert.True(window.FindControl<Border>("RecoveryBar")!.IsEffectivelyVisible); break;
                    case "error": Assert.True(window.FindControl<Border>("ErrorBar")!.IsEffectivelyVisible); break;
                    case "model-unavailable": Assert.False(vm.IsModelReady); Assert.NotEmpty(vm.ModelStatusText); break;
                    case "without-ai": Assert.Equal("stub", vm.SelectedProvider!.Key); Assert.DoesNotContain(vm.Sections, s => s.IsAiWritten); break;
                    case "source": Assert.True(window.FindControl<ScrollViewer>("SourcePanel")!.IsEffectivelyVisible); break;
                    case "assistant": Assert.True(vm.ProposalDiff.Count > 0, $"Provider={vm.SelectedProvider?.Key}; proposal={vm.Proposal}; status={vm.StatusMessage}; thread={string.Join(" | ", vm.AssistantThread.Select(m => m.Text))}"); Assert.True(vm.IsToolsOpen); break;
                }
                if (vm.ShowsExportBlockers)
                {
                    Assert.True(window.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "ExportBlockersPanel").IsEffectivelyVisible);
                }
                Save(window, $"{state}-{pixelsWide}x{pixelsHigh}-{scale * 100:0}-{language}-{(dark ? "dark" : "light")}", pixelsWide, pixelsHigh);
            }
            finally
            {
                gate.Release();
                if (pending is not null) await pending;
                window.Close();
                if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            }
        }
    }

    internal static MainWindowViewModel FillFixture(MainWindowViewModel vm)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "tests", "drafting-cases", "01-debt-arabic.json"))) directory = directory.Parent;
        var file = Path.Combine(directory!.FullName, "tests", "drafting-cases", "01-debt-arabic.json");
        using var fixture = JsonDocument.Parse(File.ReadAllText(file));
        foreach (var field in fixture.RootElement.GetProperty("inputs").EnumerateObject()) vm.Field(field.Name).Value = field.Value.GetString()!;
        return vm;
    }

    private static void Save(Window window, string name, int width, int height)
    {
        var folder = Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS");
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.InRange(frame.PixelSize.Width, width - 1, width + 1);
        Assert.InRange(frame.PixelSize.Height, height - 1, height + 1);
        if (string.IsNullOrWhiteSpace(folder)) return;
        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, name + ".png"), PngBitmapEncoderOptions.Default);
    }

    private sealed class MatrixProvider : IModelProvider
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string Name => "Local engine";
        public void Release() => _gate.TrySetResult();
        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) => Task.FromResult(Reply(request));
        public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return Reply(request);
            await _gate.Task.WaitAsync(cancellationToken);
        }
        private static string Reply(ModelRequest request) => request.SectionId switch
        {
            GroundsAnalyzer.ReadingRequestId => Analysis.Reading,
            GroundsAnalyzer.SelectionRequestId => Analysis.Selection,
            _ => request.SourceText,
        };
    }
}
