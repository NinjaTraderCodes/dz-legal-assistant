using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using DzLegal.App.Views;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;
using FluentIcons.Avalonia;

namespace DzLegal.App.Tests;

public sealed class GuideVisualTests
{
    private sealed record Display(string Language, bool Dark, int Width, int Height, double Scale)
    {
        public string Suffix => $"{Language}-{(Dark ? "dark" : "light")}-{Width}x{Height}";
        public override string ToString() => $"{Suffix}/{Scale}";
    }

    // Every layout, clipping and keyboard check runs at both sizes and both scales.
    private static IEnumerable<Display> Displays()
    {
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        foreach (var size in new[] { (1366, 768), (1920, 1080) })
        foreach (var scale in new[] { 1d, 1.25 })
            yield return new Display(language, dark, size.Item1, size.Item2, scale);
    }

    // Colour contrast depends on the theme, not on the window size or scale. Rendering every
    // pixel of sixteen full-window frames exhausts the headless Skia surface; the four
    // language/theme combinations cover every palette the screens can show.
    private static IEnumerable<Display> ContrastDisplays() => Displays().Where(display => display.Width == 1366 && display.Scale == 1);
    private static bool IsContrast(Display display) => display.Width == 1366 && display.Scale == 1;

    private static Loc Localization(Display display) { var loc = new Loc(); loc.SetLanguage(display.Language); return loc; }
    private static void Show(Window window, Display display)
    {
        window.Width = display.Width / display.Scale; window.Height = display.Height / display.Scale;
        window.RequestedThemeVariant = display.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        window.Show(); window.SetRenderScaling(display.Scale); Layout(window);
        Assert.InRange(window.Bounds.Width, display.Width / display.Scale - 1, display.Width / display.Scale + 1);
        Assert.InRange(window.Bounds.Height, display.Height / display.Scale - 1, display.Height / display.Scale + 1);
    }

    [AvaloniaFact]
    public void HelpIndexAndAllSevenPagesFitAndMeetRenderedContrastInEveryDisplayMode()
    {
        foreach (var display in Displays())
        {
            using var model = new HelpViewModel(Localization(display), () => Task.CompletedTask,
                goHome: () => Task.CompletedTask);
            var window = new HelpWindow { DataContext = model };
            Show(window, display);
            Assert.Equal(display.Language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, window.FlowDirection);
            var content = window.FindControl<Grid>("HelpContent")!;
            var card = window.FindControl<Border>("HelpCard")!;
            var heading = window.FindControl<Border>("HelpHeadingPanel")!;
            AssertInside(window, card); AssertInside(window, heading);
            Assert.False(Bounds(card, window).Intersects(Bounds(heading, window)));
            var rows = window.FindControl<ItemsControl>("HelpPages")!.GetVisualDescendants().OfType<Button>().ToArray();
            Assert.Equal(7, rows.Length);
            Assert.Equal(10, content.GetVisualDescendants().OfType<SymbolIcon>().Count());
            var home = window.FindControl<Button>("HelpHome")!;
            Assert.True(home.IsEffectivelyVisible);
            Assert.True(home.IsEnabled);
            Assert.Equal(Localization(display)["Home"], home.GetVisualDescendants().OfType<TextBlock>().Single().Text);
            Capture(window, display, "phase1-help-index");
            CheckTypography(content);
            CheckBorders(content, "SetupSurfaceBrush");
            if (IsContrast(display)) CheckContrast(window, content, display, "help-index");
            foreach (var row in rows.Append(window.FindControl<Button>("HelpTour")!))
            {
                row.BringIntoView(); Layout(window); AssertInside(window, row);
                Assert.True(row.Bounds.Height >= 44);
                Assert.True(row.IsEnabled);
            }
            AssertInside(window, home);
            CheckKeyboardOrder(window, new[] { home }.Concat(rows).Append(window.FindControl<Button>("HelpTour")!).ToArray());
            CheckFocus(window, home, display, "SetupSurfaceBrush", IsContrast(display));
            CheckFocus(window, window.FindControl<Button>("HelpTour")!, display, "SetupSurfaceBrush", IsContrast(display));
            foreach (var page in model.Pages)
            {
                page.OpenCommand.Execute(null); Layout(window);
                var scroll = window.FindControl<ScrollViewer>("HelpScroll")!;
                scroll.Offset = default; Layout(window);
                var paragraphs = window.FindControl<ItemsControl>("HelpParagraphs")!.GetVisualDescendants().OfType<TextBlock>().ToArray();
                Assert.Equal(page.Paragraphs.Select(paragraph => paragraph.Text), paragraphs.Select(paragraph => paragraph.Text));
                foreach (var paragraph in paragraphs)
                {
                    Assert.Equal(TextWrapping.Wrap, paragraph.TextWrapping);
                    Assert.Equal(TextTrimming.None, paragraph.TextTrimming);
                    Assert.Equal(0, paragraph.MaxLines);
                    Assert.True(paragraph.FontSize >= 14);
                    Assert.True(paragraph.TextLayout.Height <= paragraph.Bounds.Height + 1);
                }
                var back = window.FindControl<Button>("HelpBack")!;
                AssertInside(window, back);
                Assert.True(home.IsEffectivelyVisible);
                Assert.Equal(Localization(display)["Home"], home.GetVisualDescendants().OfType<TextBlock>().Single().Text);
                if (page.Number == 4) Capture(window, display, "phase1-help-page");
                CheckTypography(content);
                if (IsContrast(display))
                {
                    CheckContrast(window, content, display, "help-page-" + page.Number);
                    scroll.Offset = new Vector(0, scroll.Extent.Height); Layout(window);
                    CheckContrast(window, content, display, "help-page-" + page.Number + "-end");
                }
                CheckFocus(window, back, display, "SetupSurfaceBrush", IsContrast(display));
                back.Command!.Execute(null); Layout(window);
                Assert.True(model.IsIndex);
            }
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Assert.False(window.IsVisible);
        }
    }

    [AvaloniaFact]
    public void TourOfferUsesTheFamilyCardAndButtonsWithoutClippingOrLowContrast()
    {
        foreach (var display in Displays())
        {
            var yes = 0; var no = 0;
            var view = new TourOfferView { DataContext = new TourOfferViewModel(Localization(display), () => yes++, () => no++) };
            var window = new Window { Content = view };
            Show(window, display);
            var card = view.FindControl<Border>("TourOfferCard")!;
            AssertInside(window, card);
            Assert.Equal(ColorOf(ResourceBrush(card, "SetupSurfaceBrush")), ColorOf(card.Background!));
            Assert.IsType<BlurEffect>(view.FindControl<Border>("TourOfferBackground")!.Effect);
            var buttons = new[] { view.FindControl<Button>("AcceptTour")!, view.FindControl<Button>("DeclineTour")! };
            foreach (var control in buttons.Cast<Control>().Append(view.FindControl<TextBlock>("TourOfferTitle")!)) AssertInside(window, control);
            Assert.False(Bounds(buttons[0], window).Intersects(Bounds(buttons[1], window)));
            Assert.Contains("home", buttons[0].Classes); Assert.Contains("primary", buttons[0].Classes);
            Assert.Contains("home", buttons[1].Classes); Assert.DoesNotContain("primary", buttons[1].Classes);
            Assert.Contains("home-title", view.FindControl<TextBlock>("TourOfferTitle")!.Classes);
            Capture(window, display, "phase1-tour-offer");
            CheckTypography(card);
            CheckBorders(card, "SetupSurfaceBrush");
            if (IsContrast(display)) CheckContrast(window, card, display, "tour-offer");
            CheckKeyboardOrder(window, buttons);
            for (var index = 0; index < buttons.Length; index++)
                CheckFocus(window, buttons[index], display, "SetupSurfaceBrush", IsContrast(display) && index == 0);
            Assert.Equal(0, yes); Assert.Equal(0, no);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EveryTourStopFitsAndMeetsContrastWithGoldHighlightAndStableBackdrop()
    {
        foreach (var display in Displays())
        {
            using var model = Vm.Create(loc: Localization(display), noLocalModel: true);
            model.IsDarkTheme = display.Dark;
            var window = new MainWindow { DataContext = model };
            Show(window, display);
            var priorStep = model.SelectedStep;
            model.StartTour(); Layout(window);
            var overlay = window.FindControl<TourOverlay>("GuideOverlay")!;
            var backdrop = Assert.IsType<LinearGradientBrush>(overlay.FindResource("OnboardingTourBackdropBrush"));
            Assert.All(backdrop.GradientStops, stop => Assert.True(Luminance(stop.Color) < 0.25, $"{display}: tour backdrop is not dark"));
            for (var stop = 0; stop < 6; stop++)
            {
                var card = overlay.FindControl<Border>("TourCard")!;
                AssertInside(window, card);
                Assert.Equal(ColorOf(ResourceBrush(card, "SetupSurfaceBrush")), ColorOf(card.Background!));
                Assert.Contains("home-note", overlay.FindControl<TextBlock>("TourCounter")!.Classes);
                Assert.Contains("home-title", overlay.FindControl<TextBlock>("TourTitle")!.Classes);
                Assert.Contains("home-text", overlay.FindControl<TextBlock>("TourText")!.Classes);
                foreach (var name in new[] { "TourCounter", "TourTitle", "TourText", "TourNext", "TourBack", "TourSkip" })
                    AssertInside(window, overlay.FindControl<Control>(name)!);
                var buttons = new[] { overlay.FindControl<Button>("TourNext")!, overlay.FindControl<Button>("TourBack")!, overlay.FindControl<Button>("TourSkip")! };
                Assert.Contains("home", buttons[0].Classes); Assert.Contains("primary", buttons[0].Classes);
                Assert.Contains("home", buttons[1].Classes); Assert.DoesNotContain("primary", buttons[1].Classes);
                Assert.Contains("home", buttons[2].Classes); Assert.DoesNotContain("primary", buttons[2].Classes);
                Assert.Equal(model.Tour.NextText, overlay.FindControl<TextBlock>("TourNextLabel")!.Text);
                Assert.Equal(stop > 0, buttons[1].IsEffectivelyEnabled);
                for (var first = 0; first < buttons.Length; first++)
                for (var second = first + 1; second < buttons.Length; second++)
                    Assert.False(Bounds(buttons[first], window).Intersects(Bounds(buttons[second], window)));
                if (overlay.HighlightBounds is { } highlight)
                {
                    var cardRect = new Rect(Canvas.GetLeft(card), Canvas.GetTop(card), card.Bounds.Width, card.Bounds.Height);
                    Assert.False(cardRect.Intersects(highlight), $"{display}/stop {stop + 1}: card {cardRect} covers anchor {highlight}");
                }
                if (stop is 0 or 3 or 5) Capture(window, display, "phase1-tour" + (stop + 1));
                CheckTypography(card);
                CheckBorders(card, "SetupSurfaceBrush");
                if (IsContrast(display)) CheckContrast(window, card, display, "tour-" + (stop + 1));
                var shade = overlay.FindControl<Canvas>("Shade")!;
                var outline = Assert.IsType<Border>(shade.Children.Last());
                var gold = ColorOf(ResourceBrush(overlay, "OnboardingHighlightBrush"));
                Assert.Equal(gold, ColorOf(outline.BorderBrush!));
                Assert.Equal(((Thickness)overlay.FindResource("OnboardingFocusThickness")!).Left, outline.BorderThickness.Left);
                Assert.Equal(((CornerRadius)overlay.FindResource("ControlRadius")!).TopLeft, outline.CornerRadius.TopLeft);
                Assert.All(shade.Children.OfType<Border>().Where(border => !ReferenceEquals(border, outline)), dim => Assert.Same(backdrop, dim.Background));
                var stable = shade.Children.ToArray();
                Layout(window); overlay.PositionCard(); Layout(window);
                Assert.Equal(stable, shade.Children.ToArray());
                if (IsContrast(display))
                {
                    using var pixels = PixelFrame.Capture(window);
                    CheckExteriorContrast(pixels, Bounds(outline, window), gold, display, "highlight");
                }
                var enabled = buttons.Where(button => button.IsEffectivelyEnabled).ToArray();
                for (var index = 0; index < enabled.Length; index++)
                    CheckFocus(window, enabled[index], display, "SetupSurfaceBrush", IsContrast(display) && stop == 0 && index == 0);
                if (stop == 0) CheckKeyboardOrder(window, [buttons[0], buttons[2]]);
                window.KeyPress(display.Language == "ar" ? Key.Left : Key.Right, RawInputModifiers.None,
                    display.Language == "ar" ? PhysicalKey.ArrowLeft : PhysicalKey.ArrowRight, null);
                Layout(window);
            }
            Assert.False(model.Tour.IsOpen);
            Assert.Same(priorStep, model.SelectedStep);
            model.StartTour(); Layout(window);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Layout(window);
            Assert.False(model.Tour.IsOpen);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HomeAndTourUseTheSameFamilySurfaceAndButtons()
    {
        foreach (var display in ContrastDisplays())
        {
            var folder = Directory.CreateTempSubdirectory("dzlegal-guide-home-");
            try
            {
                new ProfileStore(folder.FullName).Save(new LawyerProfile { NameAr = "محامية افتراضية", Gender = "female" });
                using var model = new HomeViewModel(new RecentCasesStore(folder.FullName), new ProfileStore(folder.FullName),
                    () => Task.CompletedTask, new VisualWorkspace(), display.Language);
                var view = new HomeView { DataContext = model };
                var window = new Window { Content = view };
                Show(window, display);
                var panel = view.GetVisualDescendants().OfType<Border>().First(border => border.Classes.Contains("home-panel"));
                Assert.Equal(ColorOf(ResourceBrush(panel, "SetupSurfaceBrush")), ColorOf(panel.Background!));
                var newButton = view.FindControl<Button>("HomeNew")!;
                Assert.Equal(ColorOf(ResourceBrush(newButton, "WelcomeGoldBrush")), ColorOf(newButton.Background!));
                var tourButton = view.FindControl<Button>("HomeTour")!;
                Assert.Equal(ColorOf(ResourceBrush(tourButton, "WelcomeControlBrush")), ColorOf(tourButton.Background!));
                Assert.Contains(view.GetVisualDescendants().OfType<TextBlock>(), text => text.Classes.Contains("home-title"));
                Capture(window, display, "phase1-home");
                window.Close();
            }
            finally { folder.Delete(true); }
        }
    }

    private sealed class VisualWorkspace : IHomeWorkspace
    {
        public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public ICommand? NewCommand { get; } = new RelayCommand(() => { });
        public ICommand? OpenCommand { get; } = new RelayCommand(() => { });
        public ICommand? RestoreLastSessionCommand => null;
        public ICommand? DismissRecoveryCommand => null;
        public bool HasRecoverableDraft => false;
        public bool HasOpenCase => false;
        public Func<string, Task>? OpenRecent => null;
        public void ShowCase() { }
    }

    private static void CheckKeyboardOrder(Window window, Button[] buttons)
    {
        buttons[0].Focus(NavigationMethod.Tab);
        for (var index = 1; index < buttons.Length; index++)
        {
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Layout(window);
            Assert.Same(buttons[index], window.FocusManager!.GetFocusedElement());
        }
    }
    private static void CheckFocus(Window window, Button button, Display display, string surfaceKey, bool pixel)
    {
        button.Focus(NavigationMethod.Tab); Layout(window);
        Assert.True(ReferenceEquals(button, window.FocusManager!.GetFocusedElement()), $"{display}: focus did not reach {button.Name}; enabled={button.IsEffectivelyEnabled}, visible={button.IsEffectivelyVisible}");
        var focus = ColorOf(ResourceBrush(button, "WelcomeFocusBrush"));
        Assert.Equal(focus, ColorOf(button.BorderBrush!));
        Assert.True(button.BorderThickness.Left >= 3);
        if (!pixel)
        {
            var ratio = Ratio(focus, ColorOf(ResourceBrush(button, surfaceKey)));
            Assert.True(ratio >= 3, $"{display}: focus token contrast {ratio:F2}:1");
            return;
        }
        using var pixels = PixelFrame.Capture(window);
        var rect = Bounds(button, window);
        var drawn = pixels.At(rect.Left + button.BorderThickness.Left / 2, rect.Center.Y, display.Scale);
        Assert.True(button.IsFocused);
        // Fluent's focus adorner paints over the token-based button border. Measure the visible ring itself.
        CheckExteriorContrast(pixels, rect, drawn, display, "focus");
    }
    private static void CheckTypography(Control root)
    {
        var text = root.GetVisualDescendants().OfType<TextBlock>().Where(control => control.IsEffectivelyVisible && !control.GetVisualAncestors().OfType<SymbolIcon>().Any());
        foreach (var label in text)
        {
            var secondary = label.Classes.Contains("onboarding-secondary") || label.Classes.Contains("home-note");
            Assert.True(label.FontSize >= (secondary ? 12.5 : 14));
        }
    }
    private static void CheckBorders(Control root, string surfaceKey)
    {
        foreach (var button in root.GetVisualDescendants().OfType<Button>().Where(button => button.IsEffectivelyVisible && button.IsEffectivelyEnabled && button.BorderThickness.Left > 0))
        {
            var ratio = Ratio(ColorOf(button.BorderBrush!), ColorOf(ResourceBrush(button, surfaceKey)));
            Assert.True(ratio >= 3, $"{button.Name ?? button.GetType().Name}: border contrast {ratio:F2}:1");
        }
    }
    private static void CheckExteriorContrast(PixelFrame pixels, Rect rect, Color ink, Display display, string label)
    {
        foreach (var point in new[] { new Point(rect.Left - 2, rect.Center.Y), new Point(rect.Right + 2, rect.Center.Y), new Point(rect.Center.X, rect.Top - 2), new Point(rect.Center.X, rect.Bottom + 2) })
        {
            if (point.X < 0 || point.Y < 0 || point.X >= pixels.Width / display.Scale || point.Y >= pixels.Height / display.Scale) continue;
            var background = pixels.At(point.X, point.Y, display.Scale);
            var ratio = Ratio(ink, background);
            Assert.True(ratio >= 3, $"{display}: {label} contrast {ratio:F2} at {point}, ink {ink}, background {background}, rect {rect}");
        }
    }
    private static void CheckContrast(Window window, Control root, Display display, string screen)
    {
        var text = root.GetVisualDescendants().OfType<TextBlock>().Where(control => control.IsEffectivelyVisible && !control.GetVisualAncestors().OfType<SymbolIcon>().Any()).ToArray();
        var icons = root.GetVisualDescendants().OfType<SymbolIcon>().Where(control => control.IsEffectivelyVisible).ToArray();
        var elements = text.Cast<Control>().Concat(icons).Select(control =>
        {
            var rect = Bounds(control, window);
            foreach (var scroll in control.GetVisualAncestors().OfType<ScrollViewer>()) rect = rect.Intersect(Bounds(scroll, window));
            var brush = control is TextBlock label ? label.Foreground! : ((SymbolIcon)control).Foreground!;
            var opacity = control.Opacity * control.GetVisualAncestors().Aggregate(1d, (product, visual) => product * visual.Opacity) * brush.Opacity;
            return (Control: control, Rect: rect, Ink: ColorOf(brush), Required: control is TextBlock ? 4.5 : 3, Opacity: opacity, Original: control.Opacity);
        }).Where(item => item.Rect.Width > 0 && item.Rect.Height > 0).ToArray();
        foreach (var item in elements) item.Control.Opacity = 0;
        Layout(window);
        try
        {
            using var pixels = PixelFrame.Capture(window);
            var measurements = new List<string>();
            foreach (var button in root.GetVisualDescendants().OfType<Button>().Where(button => button.IsEffectivelyVisible && button.IsEffectivelyEnabled && button.BorderThickness.Left > 0))
            {
                var rect = Bounds(button, window);
                if (button.GetVisualAncestors().OfType<ScrollViewer>().Any(scroll => !Bounds(scroll, window).Contains(rect))) continue;
                CheckExteriorContrast(pixels, rect, ColorOf(button.BorderBrush!), display, screen + "/border");
            }
            foreach (var item in elements)
            {
                var minimum = double.MaxValue;
                for (var y = Math.Max(0, (int)(item.Rect.Top * display.Scale)); y < Math.Min(pixels.Height, (int)(item.Rect.Bottom * display.Scale)); y++)
                for (var x = Math.Max(0, (int)(item.Rect.Left * display.Scale)); x < Math.Min(pixels.Width, (int)(item.Rect.Right * display.Scale)); x++)
                    minimum = Math.Min(minimum, Ratio(item.Ink, pixels.Pixel(x, y), item.Opacity));
                var owner = item.Control.GetVisualAncestors().OfType<Button>().FirstOrDefault()?.Name;
                var centre = pixels.At(item.Rect.Center.X, item.Rect.Center.Y, display.Scale);
                Assert.True(minimum >= item.Required, $"{display}/{screen}: {item.Control.Name ?? item.Control.GetType().Name} ({owner}) contrast {minimum:F2}, required {item.Required}, ink {item.Ink}, centre {centre}");
                measurements.Add($"{item.Control.Name ?? item.Control.GetType().Name}: {minimum:F2}:1");
            }
            if (Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is { Length: > 0 } folder)
                File.WriteAllLines(Path.Combine(folder, $"contrast-{screen}-{display.Suffix}-{display.Scale}.txt"), measurements);
        }
        finally { foreach (var item in elements) item.Control.Opacity = item.Original; Layout(window); }
    }
    private static IBrush ResourceBrush(Control control, string key)
    {
        foreach (var host in control.GetVisualAncestors().OfType<Control>().Prepend(control))
            if (host.Styles.TryGetResource(key, control.ActualThemeVariant, out var value)) return Assert.IsAssignableFrom<IBrush>(value);
        Assert.Fail("Missing guide resource: " + key);
        return null!;
    }
    private static Color ColorOf(IBrush brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    private static readonly double[] Linear = Enumerable.Range(0, 256).Select(channel => channel / 255d is var value && value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4)).ToArray();
    private static double Luminance(Color color) => .2126 * Linear[color.R] + .7152 * Linear[color.G] + .0722 * Linear[color.B];
    private static double Ratio(Color ink, Color background, double opacity = 1)
    {
        var alpha = ink.A / 255d * opacity;
        var foreground = alpha >= 1 ? ink : Color.FromRgb((byte)(ink.R * alpha + background.R * (1 - alpha)), (byte)(ink.G * alpha + background.G * (1 - alpha)), (byte)(ink.B * alpha + background.B * (1 - alpha)));
        var first = Luminance(foreground); var second = Luminance(background);
        return (Math.Max(first, second) + .05) / (Math.Min(first, second) + .05);
    }
    private sealed class PixelFrame(byte[] pixels, int width, int height) : IDisposable
    {
        public int Width => width;
        public int Height => height;
        public Color Pixel(int x, int y)
        {
            var offset = (y * width + x) * 4;
            return Color.FromRgb(pixels[offset + 2], pixels[offset + 1], pixels[offset]);
        }
        public Color At(double x, double y, double scale) => Pixel(Math.Clamp((int)(x * scale), 0, width - 1), Math.Clamp((int)(y * scale), 0, height - 1));
        public static PixelFrame Capture(Window window)
        {
            using var bitmap = window.CaptureRenderedFrame(); Assert.NotNull(bitmap);
            var stride = bitmap.PixelSize.Width * 4;
            var data = new byte[stride * bitmap.PixelSize.Height];
            var memory = Marshal.AllocHGlobal(data.Length);
            try { bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), memory, data.Length, stride); Marshal.Copy(memory, data, 0, data.Length); }
            finally { Marshal.FreeHGlobal(memory); }
            return new PixelFrame(data, bitmap.PixelSize.Width, bitmap.PixelSize.Height);
        }
        public void Dispose() { }
    }
    private static Rect Bounds(Control control, Window window) => new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(window)!.Value);
    private static void AssertInside(Window window, Control control)
    {
        var rect = Bounds(control, window);
        Assert.True(rect.Width > 0 && rect.Height > 0 && rect.Left >= -1 && rect.Top >= -1 && rect.Right <= window.Bounds.Width + 1 && rect.Bottom <= window.Bounds.Height + 1,
            $"{control.Name ?? control.GetType().Name} clips: {rect} in {window.Bounds.Size}");
    }
    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); window.CaptureRenderedFrame()?.Dispose(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
    }
    private static void Capture(Window window, Display display, string screen)
    {
        if (display.Width != 1366 || display.Scale != 1 || Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is not { Length: > 0 } folder) return;
        Directory.CreateDirectory(folder);
        using var bitmap = window.CaptureRenderedFrame(); Assert.NotNull(bitmap);
        bitmap.Save(Path.Combine(folder, $"{screen}-{display.Suffix}.png"), PngBitmapEncoderOptions.Default);
    }
}
