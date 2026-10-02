using Avalonia.Controls;
using Avalonia.Threading;
using DzLegal.App.ViewModels;

namespace DzLegal.App.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _autosave = new() { Interval = TimeSpan.FromSeconds(30) };

    public MainWindow()
    {
        InitializeComponent();

        // Autosave every 30 s and when the window closes, so a crash or an accidental close loses nothing.
        _autosave.Tick += (_, _) => (DataContext as MainWindowViewModel)?.AutosaveNow();
        Opened += (_, _) => _autosave.Start();
        Closing += (_, _) =>
        {
            _autosave.Stop();
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AutosaveNow();
                vm.Dispose();
            }
        };
    }
}
