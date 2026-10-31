using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        (Application.Current as App)?.Themes?.TrackWindow(this);
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            // Escape steps back from the notices first, as the library does from a text.
            if (DataContext is AboutViewModel { IsShowingNotices: true } about) about.BackCommand.Execute(null);
            else Close();
            e.Handled = true;
        };
    }
}
