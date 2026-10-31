using Avalonia.Controls;
using DzLegal.App.Services;
using DzLegal.Law;
using DzLegal.Presentation;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Views;

internal static class GuideNavigation
{
    public static HomeGuideActions? CreateActions(Window workbench, Window owner, Action showCase, bool hasOpenCase)
    {
        if (workbench.DataContext is not MainWindowViewModel model) return null;
        Task Tour()
        {
            model.StartTour(hasOpenCase);
            showCase();
            return Task.CompletedTask;
        }
        return new HomeGuideActions(async () =>
        {
            var sample = SampleCase.LoadAll(Path.Combine(AppContext.BaseDirectory, "Samples")).Single(item => item.Id == "debt");
            showCase();
            await model.OpenSampleAsync(sample);
        }, Tour, () => ShowHelpAsync(owner, model.L, Tour, () => ShowLibraryAsync(owner, model.L, Tour),
                goHome: () => ShowHomeAsync(owner)),
            () => ShowLibraryAsync(owner, model.L, Tour));
    }

    public static async Task ShowHelpAsync(Window owner, Loc localization, Func<Task> tour,
        Func<Task>? openLibrary = null, Func<Task>? goHome = null)
    {
        var window = new HelpWindow();
        using var help = new HelpViewModel(
            localization,
            async () => { window.Close(); await tour(); },
            openLibrary is null ? null : async () => { window.Close(); await openLibrary(); },
            goHome is null ? null : async () => { window.Close(); await goHome(); });
        window.DataContext = help;
        await window.ShowDialog(owner);
    }

    public static async Task ShowHomeAsync(Window owner)
    {
        if (owner is MainWindow workbench)
        {
            await workbench.OpenHomeAsync();
            return;
        }
        owner.Activate();
        await Task.CompletedTask;
    }

    public static async Task ShowLibraryAsync(Window owner, Loc localization, Func<Task> tour)
    {
        var library = LawLibraryLoader.Load(Composition.LawRoot);
        var manifest = LawSourceManifest.Load(Composition.LawRoot);
        var window = new LibraryWindow(
            library,
            manifest,
            localization,
            new AvaloniaExternalLinks(() => TopLevel.GetTopLevel(owner)),
            new AvaloniaUserFiles(() => TopLevel.GetTopLevel(owner), AppPaths.Default),
            tour);
        await window.ShowDialog(owner);
    }
}
