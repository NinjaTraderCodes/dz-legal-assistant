using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using DzLegal.App.Views;

namespace DzLegal.App.Tests;

public sealed class WorkbenchThemeTests
{
    [AvaloniaFact]
    public void LightWorkbenchUsesHomeBrownAndDarkPaletteIsUnchanged()
    {
        var app = Assert.IsType<App>(Application.Current);
        var previous = app.RequestedThemeVariant;
        try
        {
            AssertColor(app, "AppBarBrush", ThemeVariant.Light, "#786039");
            AssertColor(app, "AppBarTextBrush", ThemeVariant.Light, "#FFFFFF");
            AssertColor(app, "AccentBrush", ThemeVariant.Light, "#786039");
            AssertColor(app, "AccentSoftBrush", ThemeVariant.Light, "#FFF3DC");
            AssertColor(app, "PrimaryButtonBrush", ThemeVariant.Light, "#C6A76D");
            AssertColor(app, "PrimaryButtonTextBrush", ThemeVariant.Light, "#28231D");
            AssertColor(app, "DocumentFocusTintBrush", ThemeVariant.Light, "#0A786039");
            AssertColor(app, "DocumentHoverTintBrush", ThemeVariant.Light, "#06786039");

            AssertColor(app, "AppBarBrush", ThemeVariant.Dark, "#1D1812");
            AssertColor(app, "AppBarTextBrush", ThemeVariant.Dark, "#F2EDE5");
            AssertColor(app, "AccentBrush", ThemeVariant.Dark, "#D2B981");
            AssertColor(app, "AccentSoftBrush", ThemeVariant.Dark, "#423724");
            AssertColor(app, "PrimaryButtonBrush", ThemeVariant.Dark, "#C6A76D");
            AssertColor(app, "PrimaryButtonTextBrush", ThemeVariant.Dark, "#28231D");
            AssertColor(app, "DocumentFocusTintBrush", ThemeVariant.Dark, "#0A1E5A4B");
            AssertColor(app, "DocumentHoverTintBrush", ThemeVariant.Dark, "#061E5A4B");

            var light = Palette(app, ThemeVariant.Light);
            Assert.True(Ratio(light["AppBarTextBrush"], light["AppBarBrush"]) >= 4.5);
            Assert.True(Ratio(light["PrimaryButtonTextBrush"], light["PrimaryButtonBrush"]) >= 4.5);
            Assert.True(Ratio(light["AccentBrush"], light["SurfaceBrush"]) >= 4.5);
            Assert.True(Ratio(light["AccentBrush"], light["SurfaceAltBrush"]) >= 4.5);
            Assert.True(Ratio(light["AccentBrush"], light["AccentSoftBrush"]) >= 4.5);
            Assert.True(Ratio(light["AccentBrush"], light["SurfaceBrush"]) >= 3);

            var dark = Palette(app, ThemeVariant.Dark);
            Assert.True(Ratio(dark["AppBarTextBrush"], dark["AppBarBrush"]) >= 4.5);
            Assert.True(Ratio(dark["PrimaryButtonTextBrush"], dark["PrimaryButtonBrush"]) >= 4.5);
            Assert.True(Ratio(dark["AccentBrush"], dark["SurfaceBrush"]) >= 4.5);
            Assert.True(Ratio(dark["AccentBrush"], dark["AccentSoftBrush"]) >= 4.5);
        }
        finally
        {
            app.RequestedThemeVariant = previous;
        }
    }

    private static Dictionary<string, Color> Palette(App app, ThemeVariant theme)
    {
        var keys = new[]
        {
            "AppBarBrush", "AppBarTextBrush", "AccentBrush", "AccentSoftBrush",
            "PrimaryButtonBrush", "PrimaryButtonTextBrush", "SurfaceBrush", "SurfaceAltBrush",
        };
        return keys.ToDictionary(key => key, key => ColorOf(app, key, theme));
    }

    private static void AssertColor(App app, string key, ThemeVariant theme, string expected) =>
        Assert.Equal(Color.Parse(expected), ColorOf(app, key, theme));

    private static Color ColorOf(App app, string key, ThemeVariant theme)
    {
        Assert.True(app.Resources.TryGetResource(key, theme, out var value), $"Missing theme resource: {key}/{theme}");
        return Assert.IsAssignableFrom<ISolidColorBrush>(value).Color;
    }

    private static double Ratio(Color a, Color b)
    {
        static double Channel(byte channel)
        {
            var value = channel / 255d;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        static double Luminance(Color color) =>
            0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        var first = Luminance(a);
        var second = Luminance(b);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }
}
