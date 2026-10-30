using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Views;

public partial class HomeView : UserControl
{
    private HomeViewModel? _observed;
    public HomeView()
    {
        InitializeComponent();
        var asset = AssetLoader.GetAssets(new Uri("avares://DzLegalAssistant/"), null)
            .Single(uri => uri.AbsolutePath.Equals("/Assets/Images/welcome-bg.jpg", StringComparison.OrdinalIgnoreCase));
        using var stream = AssetLoader.Open(asset);
        HomeBackground.Source = new Bitmap(stream);
        DataContextChanged += (_, _) => Observe();
        AttachedToVisualTree += (_, _) => Observe();
        DetachedFromVisualTree += (_, _) => { if (_observed is not null) _observed.PropertyChanged -= Changed; _observed = null; };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _observed?.IsConfirmingClear == true)
            { _observed.CancelClearCommand.Execute(null); e.Handled = true; }
        };
    }
    private void Observe()
    {
        if (_observed is not null) _observed.PropertyChanged -= Changed;
        _observed = DataContext as HomeViewModel;
        if (_observed is not null) _observed.PropertyChanged += Changed;
    }
    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HomeViewModel.IsConfirmingClear)) return;
        if (_observed?.IsConfirmingClear == true) ConfirmClear.Focus();
        else Dispatcher.UIThread.Post(() =>
        {
            if (_observed?.HasRecent == true) ClearRecent.Focus();
            else HomeArabic.Focus();
        });
    }
}
