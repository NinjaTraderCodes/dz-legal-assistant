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

    private static async Task<(MainWindow Window, ViewModels.MainWindowViewModel Vm)> ShowFilledWindowAsync()
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
        Assert.Equal("لا ينقص أي بيان إلزامي.", window.FindControl<TextBlock>("SummaryText")!.Text);

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
        Assert.StartsWith("DRAFT", window.FindControl<TextBlock>("DraftBanner")!.Text, StringComparison.Ordinal);
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

        Assert.Equal("ينقص 2 من البيانات الإلزامية — العريضة معرضة لعدم القبول شكلا.", window.FindControl<TextBlock>("SummaryText")!.Text);
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
