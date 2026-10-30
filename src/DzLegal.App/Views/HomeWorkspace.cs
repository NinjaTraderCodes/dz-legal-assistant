using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Controls;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Views;

public sealed class HomeWorkspace : IHomeWorkspace, IDisposable
{
    private readonly Window _window;
    private readonly MainWindowViewModel? _model;
    private readonly Action _showCase;
    public HomeWorkspace(Window window, Action showCase, bool hasOpenCase = false, Func<string, Task>? openRecent = null)
    {
        _window = window; _model = window.DataContext as MainWindowViewModel; _showCase = showCase;
        HasOpenCase = hasOpenCase; OpenRecent = openRecent;
        if (_model is not null) _model.PropertyChanged += Changed;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ICommand? NewCommand => _model?.NewCommand;
    public ICommand? OpenCommand => _model?.OpenCommand;
    public ICommand? RestoreLastSessionCommand => _model?.RestoreLastSessionCommand;
    public ICommand? DismissRecoveryCommand => _model?.DismissRecoveryCommand;
    public bool HasRecoverableDraft => _model?.HasRecoverableDraft == true;
    public bool HasOpenCase { get; private set; }
    public Func<string, Task>? OpenRecent { get; }
    public void ShowCase()
    {
        _showCase(); HasOpenCase = true; _window.Show(); _window.Activate();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasOpenCase)));
    }
    private void Changed(object? sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
    public void Dispose() { if (_model is not null) _model.PropertyChanged -= Changed; }
}
