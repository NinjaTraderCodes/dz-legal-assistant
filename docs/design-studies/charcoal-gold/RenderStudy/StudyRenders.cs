using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Tests;
using DzLegal.App.Views;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Storage;

namespace DzLegal.DesignStudy;

public sealed class StudyRenders
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    [AvaloniaFact]
    public async Task CaptureComparison()
    {
        var output = Environment.GetEnvironmentVariable("DZLEGAL_STUDY_OUTPUT")
            ?? throw new InvalidOperationException("Set DZLEGAL_STUDY_OUTPUT to the study's renders directory.");
        Directory.CreateDirectory(output);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "drafting-cases", "01-debt-arabic.json")));
        var inputs = json.RootElement.GetProperty("inputs").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);
        var observations = new List<object>();
        foreach (var (width, height) in new[] { (1366, 768), (1920, 1080) })
        foreach (var language in new[] { "ar", "en" })
        foreach (var blocked in new[] { false, true })
        foreach (var proposed in new[] { false, true })
        {
            var files = new FakeFiles();
            using var vm = Vm.Create(files);
            vm.L.SetLanguage(language);
            vm.IsDarkTheme = true;
            var draft = new DraftDocument
            {
                DocumentTypeId = "requete-introductive", DefinitionVersion = "0.9.0",
                Inputs = new Dictionary<string, string>(inputs, StringComparer.Ordinal),
                CreatedAt = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
                Sections = [new DraftSection { Id = "grounds", Text = inputs["notes.grounds"], AiWritten = true, Reviewed = !blocked }],
                SelectedArticles = [new("civil-code", "106"), new("civil-code", "107"), new("civil-code", "179"), new("civil-code", "186")],
            };
            var fixtureFile = (await files.SaveAsync("design-study.dzdraft.json", SaveKind.Draft))!.Value;
            await fixtureFile.Stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(draft, DefinitionJson.Options));
            await fixtureFile.Stream.DisposeAsync();
            await vm.OpenCommand.ExecuteAsync(null);
            files.SavedByKind.Clear();
            vm.SelectedStep = vm.FieldGroups.Single(g => g.Id == "facts");
            if (blocked)
            {
                await vm.ExportPdfCommand.ExecuteAsync(null);
                Assert.True(vm.IsExportBlockedShown);
                Assert.Empty(files.SavedByKind);
            }
            else
            {
                Assert.False(vm.HasExportBlockers);
            }
            var window = new MainWindow { DataContext = vm, Width = width, Height = height };
            if (proposed) Treatment.Prepare(window);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            using (var warmup = window.CaptureRenderedFrame()) { Assert.NotNull(warmup); }
            if (proposed) Treatment.Finish(window, width);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            var panels = window.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("panel")).ToArray();
            var form = window.FindControl<Border>("FormPanel")!;
            var formBounds = Bounds(form, window);
            var paper = window.FindControl<ItemsControl>("SectionsList")!.GetVisualAncestors().OfType<Border>()
                .First(b => b.MaxWidth == 860);
            var paperBounds = Bounds(paper, window);
            Assert.True(language == "ar" ? formBounds.X > paperBounds.X : formBounds.X < paperBounds.X);
            Assert.True(paperBounds.Width > 350);
            Assert.All(window.GetVisualDescendants().OfType<TextBox>().Where(t => t.IsEffectivelyVisible && (t.Classes.Contains("input") || t.Classes.Contains("document"))),
                t => Assert.Equal(FlowDirection.RightToLeft, t.FlowDirection));
            Assert.Equal(inputs["notes.facts"], vm.Field("notes.facts").Value);
            Assert.All(panels.Where(p => p.IsEffectivelyVisible), p => Assert.True(p.Bounds.Width >= 300));
            if (proposed)
            {
                var languageButton = Bounds(window.FindControl<Button>("LanguageButton")!, window);
                Assert.InRange(languageButton.X, 0, width);
                Assert.InRange(languageButton.Right, 0, width);
                var draftPanel = paper.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("panel"));
                var panelBounds = Bounds(draftPanel, window);
                foreach (var name in new[] { "GenerateButton", "ExportPdfButton" })
                {
                    var button = Bounds(window.FindControl<Button>(name)!, window);
                    Assert.True(button.X >= panelBounds.X && button.Right <= panelBounds.Right,
                        $"{name} escaped its pane: {language} {width}, button={button}, pane={panelBounds}");
                }
            }
            var file = $"{(proposed ? "proposed" : "original")}-{language}-{(blocked ? "blocked" : "editing")}-{width}x{height}.png";
            frame.Save(Path.Combine(output, file), PngBitmapEncoderOptions.Default);
            observations.Add(new { file, width, height, language, blocked, proposed, formBounds, paperBounds,
                present = vm.PresentCount, applicable = vm.ApplicableCount, blockers = vm.ExportBlockers,
                noRealAiCalled = true, sameFixture = "01-debt-arabic.json", originalPaneOrder = true });
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
        File.WriteAllText(Path.Combine(output, "observations.json"), JsonSerializer.Serialize(observations, JsonOptions));
    }

    private static Rect Bounds(Control control, Window window)
    {
        return new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(window)!.Value);
    }
}
