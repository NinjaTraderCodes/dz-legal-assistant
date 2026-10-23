using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DzLegal.App.Views;

/// <summary>A navigation entry delegates to an existing command; it never implements a case action.</summary>
public sealed record PaletteAction(string Label, ICommand Command, object? Parameter = null)
{
    public bool IsEnabled => Command.CanExecute(Parameter);
}

public sealed class CommandPaletteModel(MainWindowViewModel owner, IReadOnlyList<PaletteAction> entries) : ObservableObject
{
    private string _query = string.Empty;
    public MainWindowViewModel Owner { get; } = owner;
    public IReadOnlyList<PaletteAction> Actions => entries.Where(a => a.Label.Contains(Query.Trim(), StringComparison.OrdinalIgnoreCase))
        .OrderBy(a => a.Label.Equals(Query.Trim(), StringComparison.OrdinalIgnoreCase) ? 0
            : a.Label.StartsWith(Query.Trim(), StringComparison.OrdinalIgnoreCase) ? 1 : 2).ToList();
    public bool HasNoActions => Actions.Count == 0;

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
            {
                OnPropertyChanged(nameof(Actions));
                OnPropertyChanged(nameof(HasNoActions));
            }
        }
    }
}
