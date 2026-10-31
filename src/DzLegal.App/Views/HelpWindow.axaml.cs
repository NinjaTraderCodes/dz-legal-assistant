using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Views;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        (Application.Current as App)?.Themes?.TrackWindow(this);
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
    }
}
