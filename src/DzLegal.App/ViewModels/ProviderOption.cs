using CommunityToolkit.Mvvm.ComponentModel;
using DzLegal.Drafting;

namespace DzLegal.App.ViewModels;

/// <summary>A drafting engine the lawyer can pick. Providers are created on demand, per drafting run.</summary>
public sealed partial class ProviderOption(string key, Func<Loc, string> displayName, Func<IModelProvider> create) : ObservableObject
{
    private Loc? _loc;

    public string Key { get; } = key;

    public string DisplayName => _loc is null ? Key : displayName(_loc);

    public IModelProvider Create() => create();

    internal void Localize(Loc loc)
    {
        _loc = loc;
        OnPropertyChanged(nameof(DisplayName));
    }
}
