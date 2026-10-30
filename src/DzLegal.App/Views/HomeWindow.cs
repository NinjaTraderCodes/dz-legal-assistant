using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Views;

/// <summary>A Home dialog keeps the existing case window and its model alive behind it.</summary>
public sealed class HomeWindow : Window
{
    public HomeWindow(Window workbench, string root, Func<string, Task>? openRecent = null)
    {
        Width = 1366; Height = 768; MinWidth = 800; MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        (Application.Current as App)?.Themes?.TrackWindow(this);
        var workspace = new HomeWorkspace(workbench, Close, hasOpenCase: true, openRecent: openRecent);
        var localization = (workbench.DataContext as MainWindowViewModel)?.L;
        if (localization is null)
        {
            localization = new Loc();
            localization.SetLanguage(new WelcomePreferences(root).LoadLanguage());
        }
        Home = new HomeViewModel(new RecentCasesStore(root), new ProfileStore(root), async () =>
            await new ProfileSettingsWindow(root, localization).ShowDialog(this), workspace, new WelcomePreferences(root).LoadLanguage(), localization: localization);
        this.Bind(TitleProperty, new Binding("L[Home]") { Source = Home });
        Content = new HomeView { DataContext = Home };
        Closed += (_, _) => { Home.Dispose(); workspace.Dispose(); };
    }
    public HomeViewModel Home { get; }
}
