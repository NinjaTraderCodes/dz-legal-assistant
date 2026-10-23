using Avalonia.Controls;
namespace DzLegal.App.Views;
public partial class DraftWorkspaceView : UserControl
{
    public DraftWorkspaceView() => InitializeComponent();

    private void BeforeExportClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { HasExportBlockers: true } vm)
            vm.ShowWorkbenchToolCommand.Execute("review");
    }
}
