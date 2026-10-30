using Avalonia.Controls;
using DzLegal.Presentation;
using DzLegal.Presentation.ViewModels;

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
        }, Tour, () => ShowHelpAsync(owner, model.L, Tour));
    }

    public static async Task ShowHelpAsync(Window owner, Loc localization, Func<Task> tour)
    {
        var window = new HelpWindow();
        using var help = new HelpViewModel(localization, async () => { window.Close(); await tour(); });
        window.DataContext = help;
        await window.ShowDialog(owner);
    }
}
