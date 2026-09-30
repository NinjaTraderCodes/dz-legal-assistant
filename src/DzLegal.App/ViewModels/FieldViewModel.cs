using CommunityToolkit.Mvvm.ComponentModel;
using DzLegal.Domain;

namespace DzLegal.App.ViewModels;

public sealed partial class FieldViewModel : ObservableObject
{
    private readonly Loc _loc;
    private readonly Action _onChanged;

    public FieldViewModel(FieldDefinition definition, string value, Loc loc, Action onChanged)
    {
        Definition = definition;
        _loc = loc;
        _onChanged = onChanged;
        Silent = true;
        Value = value;
        Silent = false;
    }

    public FieldDefinition Definition { get; }

    public string Id => Definition.Id;

    public string Label => Definition.Label.Get(_loc.Language);

    public string? Hint => Definition.Hint?.Get(_loc.Language);

    public bool IsSingleLine => Definition.Kind is FieldKind.Text or FieldKind.Date;

    public bool IsMultiline => Definition.Kind == FieldKind.MultilineText;

    public bool IsBoolean => Definition.Kind == FieldKind.Boolean;

    [ObservableProperty]
    public partial string Value { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    /// <summary>Set while loading values, so the form does not re-check once per field.</summary>
    internal bool Silent { get; set; }

    public bool BoolValue
    {
        get => string.Equals(Value, "true", StringComparison.OrdinalIgnoreCase);
        set => Value = value ? "true" : "false";
    }

    partial void OnValueChanged(string value)
    {
        OnPropertyChanged(nameof(BoolValue));
        if (!Silent)
        {
            _onChanged();
        }
    }

    internal void Relocalize()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Hint));
    }
}

public sealed class FieldGroupViewModel(FieldGroupDefinition? definition, IReadOnlyList<FieldViewModel> fields, Loc loc) : ObservableObject
{
    public string Label => definition?.Label.Get(loc.Language) ?? string.Empty;

    public IReadOnlyList<FieldViewModel> Fields { get; } = fields;

    internal void Relocalize()
    {
        OnPropertyChanged(nameof(Label));
        foreach (var f in Fields)
        {
            f.Relocalize();
        }
    }
}
