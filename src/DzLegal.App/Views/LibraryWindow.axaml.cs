using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DzLegal.Law;
using DzLegal.Presentation;
using DzLegal.Presentation.Services;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Views;

public partial class LibraryWindow : Window
{
    private readonly Func<Task> _tour;
    private readonly LibraryViewModel _model;

    public LibraryWindow()
    {
        InitializeComponent();
        _tour = static () => Task.CompletedTask;
        _model = null!;
        (Application.Current as App)?.Themes?.TrackWindow(this);
    }

    public LibraryWindow(
        LawLibrary library,
        LawSourceManifest manifest,
        Loc localization,
        IExternalLinks links,
        IUserFiles files,
        Func<Task> tour)
    {
        InitializeComponent();
        _tour = tour;
        _model = new LibraryViewModel(library, manifest, localization, links, files, OpenHelpAsync);
        DataContext = _model;
        (Application.Current as App)?.Themes?.TrackWindow(this);
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            if (_model.IsReading)
            {
                _model.BackCommand.Execute(null);
                e.Handled = true;
            }
            else
            {
                Close();
                e.Handled = true;
            }
        };
    }

    private async Task OpenHelpAsync(int page)
    {
        var window = new HelpWindow();
        using var help = new HelpViewModel(
            _model.L,
            async () => { window.Close(); Close(); await _tour(); },
            () => { window.Close(); return Task.CompletedTask; });
        help.OpenPage(page);
        window.DataContext = help;
        await window.ShowDialog(this);
    }
}
