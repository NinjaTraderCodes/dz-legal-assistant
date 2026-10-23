using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Views;

namespace DzLegal.App.Tests;

public sealed class PowerFeaturesTests
{
    [AvaloniaTheory]
    [InlineData("ar", false)]
    [InlineData("ar", true)]
    [InlineData("en", false)]
    [InlineData("en", true)]
    public async Task PaletteNavigatesAndUsesTheOriginalExportGuard(string language, bool dark)
    {
        var files = new FakeFiles();
        using var vm = WorkbenchRenderingMatrixTests.FillFixture(Vm.Create(files));
        vm.L.SetLanguage(language);
        vm.IsDarkTheme = dark;
        await vm.GenerateCommand.ExecuteAsync(null);
        var main = new MainWindow { DataContext = vm, Width = 1366, Height = 768 };
        main.Show();
        main.KeyPress(Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");
        main.KeyRelease(Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");
        Dispatcher.UIThread.RunJobs();
        var palette = main.OwnedWindows.OfType<CommandPaletteWindow>().Single();
        var model = (CommandPaletteModel)palette.DataContext!;
        Assert.All(vm.FieldGroups, step => Assert.Contains(model.Actions, a => a.Label == step.Label));
        var query = palette.FindControl<TextBox>("PaletteQuery")!;
        query.Focus();
        palette.KeyTextInput(vm.FieldGroups.Single(s => s.Id == "facts").Label);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(vm.FieldGroups.Single(s => s.Id == "facts").Label, model.Query);
        Assert.IsType<FieldGroupViewModel>(model.Actions[0].Parameter);
        Assert.Contains(model.Actions, a => a.Parameter is FieldGroupViewModel { Id: "facts" });
        Save(palette, $"palette-{language}-{(dark ? "dark" : "light")}");
        palette.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        main.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Assert.Equal("facts", vm.SelectedStep!.Id);

        vm.Field("notes.facts").Value += new string('ب', 8001);
        main.ShowCommandPalette();
        palette = main.OwnedWindows.OfType<CommandPaletteWindow>().Single();
        ((CommandPaletteModel)palette.DataContext!).Query = vm.L["ExportPdf"];
        Dispatcher.UIThread.RunJobs();
        palette.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        main.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.IsExportBlockedShown);
        Assert.True(vm.IsToolsOpen);
        Assert.False(files.SavedByKind.ContainsKey(SaveKind.Pdf));
        main.Close();
    }

    [AvaloniaFact]
    public async Task SeparateWindowSharesTheDraftAndPreservesTheMountedMainEditors()
    {
        var files = new FakeFiles();
        using var vm = WorkbenchRenderingMatrixTests.FillFixture(Vm.Create(files));
        await vm.GenerateCommand.ExecuteAsync(null);
        var main = new MainWindow { DataContext = vm, Width = 1366, Height = 768 };
        main.Show();
        Dispatcher.UIThread.RunJobs();
        var view = main.FindControl<DraftWorkspaceView>("DraftWorkspace")!;
        var editor = view.GetVisualDescendants().OfType<TextBox>().Single(t => t.DataContext is SectionViewModel { Id: "grounds" });
        var section = vm.Sections.Single(s => s.Id == "grounds");
        vm.ToggleDraftWindowCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var detached = main.OwnedWindows.Single();
        Assert.Same(vm, detached.DataContext);
        Assert.IsType<DraftWorkspaceView>(detached.Content);
        Assert.Contains(view, main.FindControl<Grid>("WorkbenchGrid")!.Children);
        var floatingEditor = detached.GetVisualDescendants().OfType<TextBox>().Single(t => t.DataContext is SectionViewModel { Id: "grounds" });
        Assert.Same(section, floatingEditor.DataContext);
        section.Text = vm.Field("notes.grounds").Value;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(section.Text, editor.Text);
        Assert.Equal(section.Text, floatingEditor.Text);
        vm.Field("notes.facts").Value += new string('ب', 8001);
        var export = detached.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "ExportPdfButton");
        export.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        export.Command!.Execute(export.CommandParameter);
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.IsExportBlockedShown);
        Assert.Empty(files.SavedByKind);
        vm.HideWorkbenchToolsCommand.Execute(null);
        export.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(vm.IsToolsOpen); // repeated refusal still reopens a panel the lawyer closed
        Save(detached, "detached-draft-ar-light");
        detached.Close();
        Dispatcher.UIThread.RunJobs();
        Assert.False(vm.IsDraftDetached);
        Assert.Contains(view, main.FindControl<Grid>("WorkbenchGrid")!.Children);
        Assert.Contains(editor, main.GetVisualDescendants());
        Assert.Equal(section.Text, editor.Text);
        main.Close();
    }

    private static void Save(Window window, string name)
    {
        var folder = Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(folder)) return;
        Directory.CreateDirectory(folder);
        using var frame = window.CaptureRenderedFrame();
        frame!.Save(Path.Combine(folder, name + ".png"), PngBitmapEncoderOptions.Default);
    }
}
