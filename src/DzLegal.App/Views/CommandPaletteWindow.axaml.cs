using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace DzLegal.App.Views;

public partial class CommandPaletteWindow : Window
{
    private void QueryLoaded(object? sender, RoutedEventArgs e) => this.FindControl<TextBox>("PaletteQuery")!.Focus();

    public CommandPaletteWindow()
    {
        InitializeComponent();
        Opened += (_, _) => this.FindControl<TextBox>("PaletteQuery")!.Focus();
        AddHandler(KeyDownEvent, (_, e) =>
        {
            var list = this.FindControl<ListBox>("PaletteActions")!;
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ExecuteSelection();
                e.Handled = true;
            }
            else if (e.Key is Key.Down or Key.Up && list.ItemCount > 0)
            {
                list.SelectedIndex = Math.Clamp(list.SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, list.ItemCount - 1);
                list.ScrollIntoView(list.SelectedIndex);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        this.FindControl<TextBox>("PaletteQuery")!.TextChanged += (_, _) =>
            Dispatcher.UIThread.Post(() => this.FindControl<ListBox>("PaletteActions")!.SelectedIndex = 0);
    }

    private void ActivateEntry(object? sender, TappedEventArgs e) => ExecuteSelection();

    private void ExecuteSelection()
    {
        if (this.FindControl<ListBox>("PaletteActions")!.SelectedItem is PaletteAction { IsEnabled: true } entry)
        {
            Close();
            entry.Command.Execute(entry.Parameter);
        }
    }
}
