using System.Runtime.InteropServices;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using DzLegal.App.Views;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public sealed class WelcomeTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-welcome-");

    private WelcomePreferences Preferences => new(_folder.FullName);

    [AvaloniaFact]
    public void MissingProfileShowsWelcomeAndStartCallsOnlySetupHook()
    {
        var setup = new Border();
        var calls = 0;
        var window = new OnboardingWindow(Preferences, () => throw new InvalidOperationException(),
            () => { calls++; return setup; });
        var view = Assert.IsType<WelcomeView>(window.Content);
        var vm = Assert.IsType<WelcomeViewModel>(view.DataContext);
        vm.StartCommand.Execute(null);
        Assert.Same(setup, window.Content);
        Assert.Equal(1, calls);
        Assert.False(Preferences.HasProfile);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("{broken")]
    [InlineData("{}")]
    public void AnyPresentProfileGoesDirectlyToStartWithoutParsingOrOverwritingIt(string contents)
    {
        File.WriteAllText(Preferences.ProfilePath, contents);
        var start = new Border();
        var window = new OnboardingWindow(Preferences, () => throw new InvalidOperationException(), startScreen: () => start);
        Assert.Same(start, window.Content);
        Assert.Equal(contents, File.ReadAllText(Preferences.ProfilePath));
        window.Close();
    }

    [Fact]
    public void LanguageSwitchUsesExistingLocalizationAndPersistsWithoutCreatingProfile()
    {
        var vm = new WelcomeViewModel(Preferences, () => { });
        Assert.Equal("ar", vm.L.Language);
        Assert.True(vm.IsRightToLeft);
        vm.SwitchLanguageCommand.Execute("en");
        Assert.Equal("Get started", vm.L["WelcomeStart"]);
        Assert.False(vm.IsRightToLeft);
        Assert.False(vm.LanguageSaveFailed);
        var reopened = new WelcomeViewModel(Preferences, () => { });
        Assert.Equal("en", reopened.L.Language);
        reopened.SwitchLanguageCommand.Execute("ar");
        Assert.Equal("ابدأ", reopened.L["WelcomeStart"]);
        Assert.Equal("ar", Preferences.LoadLanguage());
        Assert.False(Preferences.HasProfile);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void BrokenPreferencesFallBackToArabic(string contents)
    {
        File.WriteAllText(Path.Combine(_folder.FullName, "welcome-preferences.json"), contents);
        Assert.Equal("ar", new WelcomeViewModel(Preferences, () => { }).L.Language);
    }

    [Fact]
    public void EveryWelcomeKeyHasAnExplicitTranslationInBothResourceFiles()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DzLegal.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var resourceRoot = Path.Combine(root.FullName, "src", "DzLegal.Presentation", "Resources");
        var arabic = Keys(Path.Combine(resourceRoot, "Strings.resx"));
        var english = Keys(Path.Combine(resourceRoot, "Strings.en.resx"));
        Assert.NotEmpty(arabic);
        Assert.Equal(arabic.Keys.Order(StringComparer.Ordinal), english.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("مساعد صياغة العرائض", arabic["WelcomeTitle"]);
        Assert.Equal("عريضة افتتاح الدعوى، بدقة ودون أن تغادر بياناتك حاسوبك.", arabic["WelcomeTagline"]);
        Assert.All(arabic.Values.Concat(english.Values), text => Assert.False(string.IsNullOrWhiteSpace(text)));
    }

    private static Dictionary<string, string> Keys(string file) => XDocument.Load(file).Root!.Elements("data")
        .Where(e => ((string?)e.Attribute("name"))?.StartsWith("Welcome", StringComparison.Ordinal) == true)
        .ToDictionary(e => (string)e.Attribute("name")!, e => (string)e.Element("value")!, StringComparer.Ordinal);

    [AvaloniaTheory]
    [InlineData("ar")]
    [InlineData("en")]
    public void KeyboardVisitsLanguagesThenStartAndEnterOpensSetup(string language)
    {
        var starts = 0;
        var vm = new WelcomeViewModel(Preferences, () => starts++);
        vm.SwitchLanguageCommand.Execute(language);
        var view = new WelcomeView(false) { DataContext = vm };
        var window = new Window { Content = view, Width = 1366, Height = 768 };
        window.Show();
        Layout(window);
        var arabic = view.FindControl<Button>("ArabicLanguage")!;
        var english = view.FindControl<Button>("EnglishLanguage")!;
        var start = view.FindControl<Button>("WelcomeStart")!;
        arabic.Focus();
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Assert.Same(english, window.FocusManager!.GetFocusedElement());
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Assert.Same(start, window.FocusManager.GetFocusedElement());
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Assert.Equal(1, starts);
        window.Close();
    }

    [AvaloniaFact]
    public void WelcomeRendersWithoutClippingAndMeetsContrastAtBothSizesAndScales()
    {
        var contrastFailures = new List<string>();
        foreach (var scale in new[] { 1d, 1.25 })
        foreach (var size in new[] { (1366, 768), (1920, 1080) })
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        {
            var vm = new WelcomeViewModel(Preferences, () => { });
            vm.SwitchLanguageCommand.Execute(language);
            var view = new WelcomeView(false) { DataContext = vm };
            var window = new Window
            {
                Content = view, Width = size.Item1 / scale, Height = size.Item2 / scale,
                RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light,
            };
            window.Show();
            window.SetRenderScaling(scale);
            Layout(window);
            Assert.Equal(language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, view.FlowDirection);
            var controls = new[] { "LanguageSwitch", "WelcomeLogo", "WelcomeTitle", "WelcomeDivider", "WelcomeTagline", "WelcomeStart", "WelcomeFooter" }
                .Select(name => view.FindControl<Control>(name)!).ToArray();
            foreach (var control in controls)
            {
                var rect = Bounds(control, window);
                Assert.True(rect.Left >= 0 && rect.Top >= 0 && rect.Right <= window.Bounds.Width + 1 && rect.Bottom <= window.Bounds.Height + 1,
                    $"{language}/{dark}/{size}/{scale}: {control.Name} clips: {rect}");
            }
            for (var i = 0; i < controls.Length; i++)
            for (var j = i + 1; j < controls.Length; j++)
                Assert.False(Bounds(controls[i], window).Intersects(Bounds(controls[j], window)), $"{controls[i].Name} overlaps {controls[j].Name}");
            Assert.True(view.FindControl<TextBlock>("WelcomeTitle")!.FontSize >= 36);
            Assert.True(view.FindControl<TextBlock>("WelcomeTagline")!.FontSize >= 16);
            Assert.True(view.FindControl<Button>("WelcomeStart")!.Bounds.Height >= 44);
            Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(view.FindControl<Button>("WelcomeStart")!)));
            using (var frame = window.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                if (scale == 1 && Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is { Length: > 0 } folder)
                {
                    Directory.CreateDirectory(folder);
                    frame.Save(Path.Combine(folder, $"welcome-{language}-{(dark ? "dark" : "light")}-{size.Item1}x{size.Item2}.png"), PngBitmapEncoderOptions.Default);
                }
            }

            try { CheckContrast(window, view, controls, language, dark, size, scale); }
            catch (Xunit.Sdk.XunitException ex) { contrastFailures.Add(ex.Message); }
            window.Close();
        }
        Assert.True(contrastFailures.Count == 0, string.Join('\n', contrastFailures));
    }

    private static void CheckContrast(Window window, WelcomeView view, Control[] controls, string language, bool dark, (int, int) size, double scale)
    {
        foreach (var control in controls) control.Opacity = 0;
        Layout(window);
        using var background = window.CaptureRenderedFrame();
        Assert.NotNull(background);
        var stride = background.PixelSize.Width * 4;
        var bytes = new byte[stride * background.PixelSize.Height];
        var memory = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            background.CopyPixels(new PixelRect(background.PixelSize), memory, bytes.Length, stride);
            Marshal.Copy(memory, bytes, 0, bytes.Length);
        }
        finally { Marshal.FreeHGlobal(memory); }

        var measurements = new List<string>();
        foreach (var name in new[] { "WelcomeTitle", "WelcomeTagline", "WelcomeFooter", "WelcomeLogo", "WelcomeDivider", "WelcomeStart", "LanguageSwitch" })
        {
            var control = view.FindControl<Control>(name)!;
            var rectangle = Bounds(control, window);
            var brush = name switch
            {
                "WelcomeTitle" => Brush(view, "WelcomeTextBrush"),
                "WelcomeTagline" or "WelcomeFooter" => Brush(view, "WelcomeSecondaryBrush"),
                "WelcomeLogo" => Brush(view, "WelcomeLogoBrush"),
                "WelcomeDivider" => Brush(view, "WelcomeMarkBrush"),
                "WelcomeStart" => Brush(view, "WelcomeButtonBorderBrush"),
                _ => Brush(view, "WelcomeEdgeBrush"),
            };
            var colors = brush is SolidColorBrush solid ? new[] { solid.Color } : ((LinearGradientBrush)brush).GradientStops.Select(g => g.Color).ToArray();
            var minimum = double.MaxValue;
            for (var y = Math.Max(0, (int)(rectangle.Top * scale)); y < Math.Min(background.PixelSize.Height, (int)(rectangle.Bottom * scale)); y++)
            for (var x = Math.Max(0, (int)(rectangle.Left * scale)); x < Math.Min(background.PixelSize.Width, (int)(rectangle.Right * scale)); x++)
            {
                var offset = y * stride + x * 4;
                var pixel = Color.FromRgb(bytes[offset + 2], bytes[offset + 1], bytes[offset]);
                foreach (var color in colors) minimum = Math.Min(minimum, Ratio(color, pixel));
            }
            var required = name is "WelcomeTitle" or "WelcomeTagline" or "WelcomeFooter" ? 4.5 : 3;
            Assert.True(minimum >= required, $"{language}/{dark}/{size}/{scale}: {name} contrast {minimum:F2}, required {required}");
            measurements.Add($"{name}: {minimum:F2}:1");
        }
        var gold = ((SolidColorBrush)Brush(view, "WelcomeGoldBrush")).Color;
        var ink = ((SolidColorBrush)Brush(view, "WelcomeButtonTextBrush")).Color;
        Assert.True(Ratio(gold, ink) >= 4.5);
        var focus = ((SolidColorBrush)Brush(view, "WelcomeFocusBrush")).Color;
        var surface = ((SolidColorBrush)Brush(view, "WelcomeControlBrush")).Color;
        Assert.True(Ratio(focus, surface) >= 3);
        var languageInk = ((SolidColorBrush)Brush(view, "WelcomeTextBrush")).Color;
        Assert.True(Ratio(languageInk, surface) >= 4.5);
        measurements.Add($"Button text: {Ratio(gold, ink):F2}:1");
        measurements.Add($"Language text: {Ratio(languageInk, surface):F2}:1");
        measurements.Add($"Focus / language surface: {Ratio(focus, surface):F2}:1");
        if (scale == 1 && Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is { Length: > 0 } folder)
            File.WriteAllLines(Path.Combine(folder, $"contrast-{language}-{(dark ? "dark" : "light")}-{size.Item1}x{size.Item2}.txt"), measurements);
    }

    private static IBrush Brush(WelcomeView view, string key)
    {
        Assert.True(view.Styles.TryGetResource(key, view.ActualThemeVariant, out var value), $"Missing welcome resource: {key}");
        return Assert.IsAssignableFrom<IBrush>(value);
    }

    private static double Ratio(Color a, Color b)
    {
        static double Channel(byte channel) { var value = channel / 255d; return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4); }
        static double Luminance(Color color) => 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        var first = Luminance(a); var second = Luminance(b);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    private static Rect Bounds(Control control, Window window) => new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(window)!.Value);

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
    }

    public void Dispose() => _folder.Delete(recursive: true);
}
