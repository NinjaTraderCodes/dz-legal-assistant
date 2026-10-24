using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Views;

namespace DzLegal.App.Tests;

public sealed class WorkbenchTests
{
    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        using var warmup = window.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
    }

    private static Rect Bounds(Control control, Window window) =>
        new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(window)!.Value);

    private static void Click(Window window, Control control)
    {
        var bounds = Bounds(control, window);
        window.MouseDown(bounds.Center, MouseButton.Left);
        window.MouseUp(bounds.Center, MouseButton.Left);
        Layout(window);
    }

    private static void Capture(Window window, string name)
    {
        if (Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is not { Length: > 0 } folder) return;
        Directory.CreateDirectory(folder);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(folder, name + ".png"), PngBitmapEncoderOptions.Default);
    }

    [AvaloniaFact]
    public async Task ToolbarAndCaseOutlineFitBothLanguagesThemesAndWindowSizes()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var window = new MainWindow { DataContext = vm };
        window.Show();
        foreach (var (width, height) in new[] { (1100, 640), (1366, 768), (1920, 1080) })
        foreach (var english in new[] { false, true })
        foreach (var dark in new[] { false, true })
        {
            window.Width = width; window.Height = height;
            if ((vm.L.Language == "en") != english) vm.SwitchLanguageCommand.Execute(null);
            vm.IsDarkTheme = dark; Layout(window);
            var toolbar = window.FindControl<Grid>("DraftToolbar")!;
            var toolbarBounds = Bounds(toolbar, window);
            var controls = toolbar.GetVisualDescendants().OfType<Control>()
                .Where(c => c.IsEffectivelyVisible && c is Button or CheckBox or TabItem).ToArray();
            var bounds = controls.Select(c => (Control: c, Rect: Bounds(c, window))).ToArray();
            foreach (var (control, rect) in bounds)
            {
                Assert.True(rect.Left >= toolbarBounds.Left - 1 && rect.Right <= toolbarBounds.Right + 1,
                    $"{width} {vm.L.Language}: {control.GetType().Name} outside toolbar: {rect}, toolbar={toolbarBounds}");
                Assert.True(rect.Top >= toolbarBounds.Top - 1 && rect.Bottom <= toolbarBounds.Bottom + 1,
                    $"{width} {vm.L.Language}: {control.GetType().Name} outside toolbar height: {rect}, toolbar={toolbarBounds}");
            }
            for (var i = 0; i < bounds.Length; i++)
            for (var j = i + 1; j < bounds.Length; j++)
                Assert.False(bounds[i].Rect.Intersects(bounds[j].Rect), $"{width} {vm.L.Language}: toolbar controls overlap");
            var language = Bounds(window.FindControl<Button>("LanguageButton")!, window);
            Assert.True(language.Left >= 0 && language.Right <= width);
            Assert.Equal(9, window.FindControl<ListBox>("StepsList")!.ItemCount);
            var form = Bounds(window.FindControl<Border>("FormPanel")!, window);
            var paper = Bounds(window.FindControl<Border>("DocumentPaper")!, window);
            Assert.True(english ? form.X < paper.X : form.X > paper.X);
            Assert.True(paper.Width > 350);
            Assert.DoesNotContain(window.FindControl<Border>("DocumentPaper")!.GetVisualDescendants(), c => c is Button or CheckBox);
            Assert.All(window.GetVisualDescendants().OfType<TextBox>().Where(t => t.Classes.Contains("document")), t => Assert.Equal(FlowDirection.RightToLeft, t.FlowDirection));
            Capture(window, $"workbench-{width}-{vm.L.Language}-{(dark ? "dark" : "light")}");
        }
        window.Close();
    }

    [AvaloniaFact]
    public async Task SelectingAParagraphAndUsingItsToolsPreservesOverrideAndRestore()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var window = new MainWindow { DataContext = vm, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        var section = vm.Sections.Single(s => s.Id == "header");
        var original = section.Text;
        var editor = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Classes.Contains("document") && t.DataContext == section);
        Click(window, editor);
        Assert.Same(section, window.FindControl<ContentControl>("ActiveFieldTools")!.Content);
        Assert.True(editor.IsReadOnly);
        var overrideButton = window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "OverrideSectionButton" && b.IsEffectivelyVisible);
        Click(window, overrideButton);
        Assert.True(section.IsOverridden); Assert.False(editor.IsReadOnly);
        editor.Focus(); editor.SelectAll(); window.KeyTextInput("نص عدله المحامي"); Layout(window);
        Assert.Equal("نص عدله المحامي", section.Text);
        var restore = window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "RestoreSectionButton" && b.IsEffectivelyVisible);
        Click(window, restore);
        Assert.False(section.IsOverridden); Assert.True(editor.IsReadOnly); Assert.Equal(original, section.Text);

        var steps = window.FindControl<ListBox>("StepsList")!;
        steps.SelectedItem = vm.FieldGroups.Single(g => g.Id == "grounds"); Layout(window);
        Assert.Equal("grounds", vm.SelectedStep!.Id);
        Assert.Same(vm.Sections.Single(s => s.Id == "grounds"), window.FindControl<ContentControl>("ActiveFieldTools")!.Content);
        Assert.True(steps.ContainerFromItem(steps.SelectedItem!)!.Focus());
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); Layout(window);
        Assert.Equal("requests", vm.SelectedStep.Id);
        window.Close();
    }

    [AvaloniaFact]
    public async Task PreviewStillShowsActualPdfPagesAndLongEditsAreNotCut()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var window = new MainWindow { DataContext = vm, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        var tabs = window.GetVisualDescendants().OfType<TabItem>().ToArray();
        Click(window, tabs[1]);
        Assert.Equal(1, vm.SelectedDraftTab);
        await vm.RefreshPreviewAsync(); Layout(window);
        Assert.Contains(window.FindControl<ItemsControl>("PreviewPagesList")!.GetVisualDescendants().OfType<Image>(), i => i.Source is not null);
        Click(window, tabs[0]); Assert.Equal(0, vm.SelectedDraftTab);
        var section = vm.Sections.Single(s => s.Id == "grounds");
        var editor = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Classes.Contains("document") && t.DataContext == section);
        editor.BringIntoView(); Layout(window); Assert.True(editor.Focus());
        editor.SelectAll(); var text = new string('ب', 8001); window.KeyTextInput(text); Layout(window);
        Assert.Equal(text, section.Text);
        Assert.Same(section, window.FindControl<ContentControl>("ActiveFieldTools")!.Content);
        window.Close();
    }

    [AvaloniaFact]
    public async Task ReviewCheckboxAndExportButtonsKeepTheOriginalGuardAndInvalidation()
    {
        var files = new FakeFiles();
        var vm = Vm.Create(provider: "scripted", files: files).Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var window = new MainWindow { DataContext = vm, Width = 1366, Height = 768 };
        window.Show(); Layout(window);
        var grounds = vm.Sections.Single(s => s.Id == "grounds");
        var check = window.GetVisualDescendants().OfType<CheckBox>().Single(c => c.Name == "ReviewedCheck" && c.IsEffectivelyVisible);
        Assert.True(grounds.IsAiWritten); Assert.False(grounds.IsReviewed);
        Click(window, window.FindControl<Button>("ExportPdfButton")!);
        Assert.True(vm.IsExportBlockedShown); Assert.Empty(files.SavedByKind);
        Capture(window, "workbench-export-blocked");
        Click(window, check); Assert.True(grounds.IsReviewed); Assert.False(vm.HasExportBlockers);
        Click(window, window.FindControl<Button>("ExportPdfButton")!);
        // Export is async; await the same in-flight command after the real button click.
        if (vm.ExportPdfCommand.ExecutionTask is { } pdf) await pdf;
        Assert.Contains(SaveKind.Pdf, files.SavedByKind.Keys);
        Click(window, window.FindControl<Button>("ExportWordButton")!);
        if (vm.ExportWordCommand.ExecutionTask is { } word) await word;
        Assert.Contains(SaveKind.Word, files.SavedByKind.Keys);
        grounds.Text += "\nنص جديد"; Layout(window);
        Assert.False(grounds.IsReviewed); Assert.False(check.IsChecked); Assert.True(vm.HasExportBlockers);
        vm.NewCommand.Execute(null); Layout(window);
        Assert.True(window.FindControl<Border>("ReplaceBar")!.IsVisible);
        vm.CancelReplaceCommand.Execute(null);
        Assert.Contains("نص جديد", grounds.Text, StringComparison.Ordinal);
        window.Close();
    }
}
