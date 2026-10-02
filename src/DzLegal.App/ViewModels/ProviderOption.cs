using CommunityToolkit.Mvvm.ComponentModel;
using DzLegal.Drafting;

namespace DzLegal.App.ViewModels;

/// <summary>A drafting engine the lawyer can pick. Providers are created on demand, per drafting run.</summary>
public enum ModelState
{
    Unknown,
    Ready,
    NotRunning,
    ModelMissing,
}

/// <summary>Whether the engine can draft right now, and why not.</summary>
public sealed record ModelStatus(ModelState State, string Detail = "");

public sealed partial class ProviderOption(
    string key,
    Func<Loc, string> displayName,
    Func<IModelProvider> create,
    Func<CancellationToken, Task<ModelStatus>>? probe = null) : ObservableObject
{
    private Loc? _loc;

    public string Key { get; } = key;

    public string DisplayName => _loc is null ? Key : displayName(_loc);

    public IModelProvider Create() => create();

    /// <summary>Checks the engine without drafting anything (for the status indicator).</summary>
    public Task<ModelStatus> ProbeAsync(CancellationToken cancellationToken) =>
        probe?.Invoke(cancellationToken) ?? Task.FromResult(new ModelStatus(ModelState.Ready));

    internal void Localize(Loc loc)
    {
        _loc = loc;
        OnPropertyChanged(nameof(DisplayName));
    }
}
