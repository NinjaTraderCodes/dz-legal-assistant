using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DzLegal.App.Services;
using DzLegal.App.Views;
using DzLegal.Storage;

namespace DzLegal.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static Window CreateMainWindow()
    {
        // Explicit isolated profile for reviewing the real UI without touching an existing lawyer's recovery copy.
        var reviewHome = Environment.GetEnvironmentVariable("DZLEGAL_REVIEW_HOME");
        var paths = string.IsNullOrWhiteSpace(reviewHome) ? AppPaths.Default : new AppPaths(Path.GetFullPath(reviewHome));
        var window = new MainWindow(Path.Combine(paths.Root, "settings", "workbench-layout.json"));
        try
        {
            var settings = AppSettings.Load(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
            window.DataContext = Composition.CreateMainViewModel(
                settings, new AvaloniaUserFiles(() => TopLevel.GetTopLevel(window), paths), paths, new Loc(), new AvaloniaExternalLinks(() => TopLevel.GetTopLevel(window)));
            return window;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or Domain.Serialization.DefinitionValidationException or Law.LawDataException or System.Text.Json.JsonException or UriFormatException)
        {
            // Bad settings or rule data: show the problem instead of crashing silently.
            return new Window
            {
                Title = "DzLegalAssistant — startup error",
                Width = 800,
                Height = 400,
                Content = new TextBox { Text = ex.ToString(), IsReadOnly = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            };
        }
    }
}
