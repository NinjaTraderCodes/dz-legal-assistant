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
    public async Task CaptureRefinements()
    {
        var output = Environment.GetEnvironmentVariable("DZLEGAL_STUDY_OUTPUT")
            ?? throw new InvalidOperationException("Set DZLEGAL_STUDY_OUTPUT to this study's renders directory.");
        Directory.CreateDirectory(output);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "drafting-cases", "01-debt-arabic.json")));
        var inputs = json.RootElement.GetProperty("inputs").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);
        var scenes = new List<(int Width, int Height, string Language, bool Blocked, bool Glass, bool Manual)>();
        foreach (var (w, h) in new[] { (1366, 768), (1920, 1080) })
        foreach (var language in new[] { "ar", "en" })
        foreach (var blocked in new[] { false, true })
        foreach (var glass in new[] { false, true }) scenes.Add((w, h, language, blocked, glass, false));
        scenes.Add((1920, 1080, "ar", false, false, true));
        scenes.Add((1920, 1080, "en", false, false, true));
        if (Environment.GetEnvironmentVariable("DZLEGAL_STUDY_SAMPLE") == "1")
            scenes = scenes.Where(s => s.Language == "en" && !s.Blocked && !s.Manual).ToList();
        var observations = new List<object>();
        foreach (var (width, height, language, blocked, glass, manual) in scenes)
        {
            var files = new FakeFiles();
            using var vm = Vm.Create(files);
            vm.L.SetLanguage(language); vm.IsDarkTheme = true;
            var draft = new DraftDocument
            {
                DocumentTypeId = "requete-introductive", DefinitionVersion = "0.9.0",
                Inputs = new Dictionary<string, string>(inputs, StringComparer.Ordinal),
                CreatedAt = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
                Sections = [new DraftSection { Id = "grounds", Text = inputs["notes.grounds"], AiWritten = true, Reviewed = !blocked }],
                SelectedArticles = [new("civil-code", "106"), new("civil-code", "107"), new("civil-code", "179"), new("civil-code", "186")],
            };
            var fixture = (await files.SaveAsync("design-study.dzdraft.json", SaveKind.Draft))!.Value;
            await fixture.Stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(draft, DefinitionJson.Options));
            await fixture.Stream.DisposeAsync(); await vm.OpenCommand.ExecuteAsync(null); files.SavedByKind.Clear();
            vm.SelectedStep = vm.FieldGroups.Single(g => g.Id == "facts");
            if (manual) vm.Sections.First(s => s.Id == "header").OverrideCommand.Execute(null);
            if (blocked)
            {
                await vm.ExportPdfCommand.ExecuteAsync(null);
                Assert.True(vm.IsExportBlockedShown); Assert.Empty(files.SavedByKind);
            }
            else Assert.False(vm.HasExportBlockers);
            var window = new MainWindow { DataContext = vm, Width = width, Height = height };
            Treatment.Prepare(window); window.Show(); Dispatcher.UIThread.RunJobs();
            using (var warmup = window.CaptureRenderedFrame()) Assert.NotNull(warmup);
            Treatment.Finish(window, width); window.UpdateLayout();
            var composition = Refinement.Apply(window, width, glass, manual);
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            using (var warmup = window.CaptureRenderedFrame()) Assert.NotNull(warmup);
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();

            var formBounds = Bounds(window.FindControl<Border>("FormPanel")!, window);
            var paperBounds = Bounds(composition.Paper, window);
            Assert.True(language == "ar" ? formBounds.X > paperBounds.X : formBounds.X < paperBounds.X);
            Assert.Equal(inputs["notes.facts"], vm.Field("notes.facts").Value);
            Assert.Equal(vm.Sections.Where(s => !string.IsNullOrWhiteSpace(s.Text)).SelectMany(s => s.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')), composition.Paragraphs);
            Assert.Equal(9, composition.Outline.ItemCount);
            Assert.Equal(vm.SelectedStep, composition.Outline.SelectedItem);
            Assert.Equal(!blocked, composition.ReviewCheck.IsChecked);
            Assert.Equal(manual, vm.Sections.First(s => s.Id == "header").IsOverridden);
            Assert.All(composition.Paper.GetVisualDescendants().OfType<TextBlock>().Where(t => t.FontSize >= 19),
                t => Assert.Equal(FlowDirection.RightToLeft, t.FlowDirection));
            Assert.DoesNotContain(composition.Paper.GetVisualDescendants(), c => c is Button or CheckBox);
            var toolbarBounds = Bounds(composition.Toolbar, window);
            var controls = composition.Toolbar.GetVisualDescendants().OfType<Control>()
                .Where(c => c.IsEffectivelyVisible && c is Button or CheckBox or TabItem).ToArray();
            var buttonBounds = controls.Select(c => (Control: c, Bounds: Bounds(c, window))).ToArray();
            foreach (var (control, bounds) in buttonBounds)
            {
                Assert.True(bounds.X >= toolbarBounds.X - 1 && bounds.Right <= toolbarBounds.Right + 1,
                    $"{language} {width}: {control.GetType().Name} outside toolbar: {bounds}, toolbar={toolbarBounds}");
                Assert.True(bounds.Y >= toolbarBounds.Y - 1 && bounds.Bottom <= toolbarBounds.Bottom + 1,
                    $"{language} {width}: {control.GetType().Name} outside toolbar height: {bounds}, toolbar={toolbarBounds}");
            }
            for (var i = 0; i < buttonBounds.Length; i++)
            for (var j = i + 1; j < buttonBounds.Length; j++)
                Assert.False(buttonBounds[i].Bounds.Intersects(buttonBounds[j].Bounds),
                    $"{language} {width}: toolbar controls overlap: {buttonBounds[i].Control.GetType().Name}, {buttonBounds[j].Control.GetType().Name}");
            Assert.True(toolbarBounds.Height < 53, $"Toolbar wrapped: {toolbarBounds.Height}");
            Assert.True(composition.PageCount >= 2);
            var finish = glass ? "glass" : "solid";
            var state = manual ? "manual" : blocked ? "blocked" : "editing";
            var filename = $"{finish}-{language}-{state}-{width}x{height}.png";
            using var frame = window.CaptureRenderedFrame(); Assert.NotNull(frame);
            frame.Save(Path.Combine(output, filename), PngBitmapEncoderOptions.Default);
            if (width == 1920 && language == "en" && !blocked && !glass && !manual)
            {
                using var proof = new RenderTargetBitmap(new PixelSize(794, 1123), new Vector(96, 96));
                proof.Render(composition.Paper);
                proof.Save(Path.Combine(output, "page-proof.png"), PngBitmapEncoderOptions.Default);
            }
            observations.Add(new { filename, width, height, language, blocked, finish, manual,
                formBounds, paperBounds, toolbarBounds, composition.PageCount, composition.Zoom,
                noRealAiCalled = true, identicalParagraphs = true, oneRowToolbar = true,
                metadataOutsidePaper = true, originalPaneOrder = true, fakeFixture = "01-debt-arabic.json" });
            window.Close(); Dispatcher.UIThread.RunJobs();
        }
        File.WriteAllText(Path.Combine(output, "observations.json"), JsonSerializer.Serialize(observations, JsonOptions));
    }

    private static Rect Bounds(Control control, Window window) =>
        new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(window)!.Value);
}
