using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Services;
using DzLegal.App.Views;

namespace DzLegal.App.Tests;

public sealed class WorkbenchTests
{
    [AvaloniaFact]
    public void EngineSettingsReceiveKeyboardFocusAndEscapeReturnsToTheWorkbench()
    {
        using var vm = Vm.Create();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        var button = window.FindControl<Button>("EngineButton")!;
        button.Focus();
        button.Flyout!.ShowAt(button);
        using var frame = window.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs();
        var picker = window.FindControl<ComboBox>("EnginePicker")!;
        Assert.True(picker.IsFocused, $"open={button.Flyout.IsOpen}, visible={picker.IsEffectivelyVisible}, enabled={picker.IsEffectivelyEnabled}, focusable={picker.Focusable}, root={TopLevel.GetTopLevel(picker)}");
        var popup = TopLevel.GetTopLevel(picker)!;
        popup.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        popup.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        popup.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        popup.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Assert.Equal("scripted", vm.SelectedProvider!.Key);
        popup.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        popup.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Assert.False(button.Flyout.IsOpen);
        window.Close();
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{\"OutlineWidth\":-3}")]
    [InlineData("{\"EditorWidth\":99999}")]
    [InlineData("{\"Tool\":\"unknown\"}")]
    public void InvalidPreferencesFallBackWithoutAffectingCaseFiles(string json)
    {
        var folder = Path.Combine(Path.GetTempPath(), "r3-layout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var file = Path.Combine(folder, "layout.json");
            File.WriteAllText(file, json);
            Assert.Equal(new WorkbenchLayout(), new WorkbenchLayoutStore(file).Load());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void PreferencesRoundTripSeparatelyAndUnavailableStorageIsOptional()
    {
        var folder = Path.Combine(Path.GetTempPath(), "r3-layout-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(folder, "settings", "workbench-layout.json");
        var store = new WorkbenchLayoutStore(file);
        Assert.Equal(new WorkbenchLayout(), store.Load());
        try
        {
            var layout = new WorkbenchLayout { OutlineWidth = 280, EditorWidth = 500, ToolsHeight = 190, OutlineCompact = true, Tool = "source", ToolsOpen = true };
            store.Save(layout);
            Assert.Equal(layout, store.Load());
            Assert.DoesNotContain("IsValid", File.ReadAllText(file), StringComparison.Ordinal);
            // A file cannot also be a directory. This is an actual failing write, not a mocked exception.
            var unavailable = new WorkbenchLayoutStore(Path.Combine(file, "preferences.json"));
            unavailable.Save(layout);
            Assert.Equal(new WorkbenchLayout(), unavailable.Load());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task PanesStayPhysicalWhileFocusThemeAndLanguageKeepTheSameEditors()
    {
        using var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var window = new MainWindow { DataContext = vm, Width = 1366, Height = 768 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var draft = window.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "DraftPane");
        var form = window.FindControl<Border>("FormPanel")!;
        var editor = window.GetVisualDescendants().OfType<TextBox>().First(t => t.Classes.Contains("document"));
        var originalText = editor.Text;
        var originalSections = vm.Sections.ToArray();
        Assert.True(vm.IsOutlineCompact);
        Assert.False(vm.IsToolsOpen);
        Assert.True(window.FindControl<Grid>("ToolsStrip")!.IsEffectivelyVisible);
        AssertPhysicalOrder();
        vm.ToggleDraftFocusCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(form.IsEffectivelyVisible);
        Assert.True(draft.IsEffectivelyVisible);
        vm.ToggleDraftFocusCommand.Execute(null);
        vm.SwitchLanguageCommand.Execute(null);
        vm.ToggleThemeCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        AssertPhysicalOrder();
        Assert.Equal(originalText, editor.Text);
        Assert.Equal(originalSections, vm.Sections);
        Assert.Contains(editor, window.GetVisualDescendants());
        Assert.Equal(FlowDirection.RightToLeft, editor.FlowDirection);
        window.Close();

        void AssertPhysicalOrder()
        {
            var formOrigin = form.TranslatePoint(default, window)!.Value;
            var draftOrigin = draft.TranslatePoint(default, window)!.Value;
            Assert.True(draftOrigin.X >= formOrigin.X + form.Bounds.Width);
            Assert.True(form.Bounds.Width >= 320);
            Assert.True(draft.Bounds.Width >= 440);
        }
    }

    [AvaloniaFact]
    public async Task BlockedExportOpensReviewWithoutMovingKeyboardFocusOrDirtyingTheCase()
    {
        var files = new FakeFiles();
        using var vm = Vm.Create(files).Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        await vm.SaveCommand.ExecuteAsync(null);
        vm.Field("notes.facts").Value = new string('ب', 8001);
        var dirty = vm.HasUnsavedChanges;
        var window = new MainWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var input = window.GetVisualDescendants().OfType<TextBox>().First(t => t.Classes.Contains("input"));
        input.Focus();
        await vm.ExportPdfCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.IsToolsOpen);
        Assert.True(window.FindControl<Border>("ReviewPanel")!.IsEffectivelyVisible);
        Assert.Same(input, window.FocusManager!.GetFocusedElement());
        Assert.False(files.SavedByKind.ContainsKey(SaveKind.Pdf));
        Assert.Equal(dirty, vm.HasUnsavedChanges);
        Assert.NotEmpty(vm.ExportBlockers);
        window.Close();
    }

    [AvaloniaFact]
    public void SplittersUsePreviewAndKeyboardResizingAndRestoreSavedLayout()
    {
        var folder = Path.Combine(Path.GetTempPath(), "r3-window-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(folder, "layout.json");
        try
        {
            new WorkbenchLayoutStore(file).Save(new WorkbenchLayout { OutlineCompact = false, OutlineWidth = 240, EditorWidth = 500, ToolsOpen = true, Tool = "source", ToolsHeight = 160 });
            using var vm = Vm.Create();
            var window = new MainWindow(file) { Width = 1920, Height = 1080, DataContext = vm };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.False(vm.IsOutlineCompact);
            Assert.True(vm.IsSourceTool);
            foreach (var splitter in window.GetVisualDescendants().OfType<GridSplitter>())
            {
                Assert.True(splitter.ShowsPreview);
                Assert.True(splitter.Focusable);
                Assert.NotEmpty(Avalonia.Automation.AutomationProperties.GetName(splitter)!);
            }

            var grid = window.FindControl<Grid>("WorkbenchGrid")!;
            var before = grid.ColumnDefinitions[2].ActualWidth;
            window.FindControl<GridSplitter>("DraftSplitter")!.Focus();
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(grid.ColumnDefinitions[2].ActualWidth > before);
            Assert.False(vm.HasUnsavedChanges);
            var expected = grid.ColumnDefinitions[2].ActualWidth;
            window.Close();
            var restored = new WorkbenchLayoutStore(file).Load();
            Assert.Equal(expected, restored.EditorWidth, precision: 1);
            Assert.Equal("source", restored.Tool);
            Assert.True(restored.ToolsOpen);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
