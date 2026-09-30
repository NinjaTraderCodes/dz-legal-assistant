using Avalonia;
using Avalonia.Headless;
using DzLegal.App.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace DzLegal.App.Tests;

public static class TestApp
{
    /// <summary>Headless platform with real Skia rendering, so text layout and screenshots are genuine.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .WithInterFont()
            .WithArabicFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
