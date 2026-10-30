using System.ComponentModel;
using System.Windows.Input;

namespace DzLegal.Presentation.ViewModels;

/// <summary>Only existing workspace commands are offered to Home; unsupported integrations stay absent.</summary>
public interface IHomeWorkspace : INotifyPropertyChanged
{
    ICommand? NewCommand { get; }
    ICommand? OpenCommand { get; }
    ICommand? RestoreLastSessionCommand { get; }
    ICommand? DismissRecoveryCommand { get; }
    bool HasRecoverableDraft { get; }
    bool HasOpenCase { get; }
    Func<string, Task>? OpenRecent { get; }
    void ShowCase();
}

/// <summary>Optional desktop picker metadata for Hicham's successful-open/save integration.</summary>
public interface IHomeRecentFiles
{
    string? LastOpenedPath { get; }
    string? LastSavedPath { get; }
    Task<(Stream Stream, string Name)?> OpenPathAsync(string path);
}
