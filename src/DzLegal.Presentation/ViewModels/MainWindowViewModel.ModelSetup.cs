using CommunityToolkit.Mvvm.ComponentModel;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasModelSetup))]
    public partial ModelSetupViewModel? ModelSetup { get; set; }
    public bool HasModelSetup => ModelSetup is not null;
}