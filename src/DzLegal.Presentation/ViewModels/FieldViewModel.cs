using CommunityToolkit.Mvvm.ComponentModel;
using DzLegal.Domain;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class FieldViewModel : ObservableObject
{
    private readonly Loc _loc;
    private readonly Action _onChanged;

    public FieldViewModel(FieldDefinition definition, string value, Loc loc, Action onChanged)
    {
        Definition = definition;
        _loc = loc;
        _onChanged = onChanged;
        Options = definition.Options.Select(o => new ChoiceOptionViewModel(o, loc)).ToList();
        Silent = true;
        Value = value;
        Silent = false;
    }

    public FieldDefinition Definition { get; }

    /// <summary>
    /// Longest text the field takes. Bounds what reaches the model (a laptop model reads about 4,000 tokens in all) and
    /// what a web page sends to its server in one message: a 25,000-letter paste used to disconnect the web demo.
    /// </summary>
    public int MaxLength => Id switch
    {
        "notes.facts" => 8000,
        "notes.grounds" => 4000,
        "notes.requests" or "documents.list" => 2000,
        _ => 500,
    };

    public string Id => Definition.Id;

    public string Label => Definition.Label.Get(_loc.Language);

    public string? Hint => Definition.Hint?.Get(_loc.Language);

    public bool IsSingleLine => Definition.Kind is FieldKind.Text or FieldKind.Date;

    public bool IsMultiline => Definition.Kind == FieldKind.MultilineText;

    public bool IsBoolean => Definition.Kind == FieldKind.Boolean;

    public bool IsChoice => Definition.Kind == FieldKind.Choice;

    public IReadOnlyList<ChoiceOptionViewModel> Options { get; }

    public ChoiceOptionViewModel? SelectedOption
    {
        get => Options.FirstOrDefault(o => o.Value == Value);
        set => Value = value?.Value ?? string.Empty;
    }

    [ObservableProperty]
    public partial string Value { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    /// <summary>A mandatory element is missing because this field is empty; <see cref="MissingNote"/> says which.</summary>
    [ObservableProperty]
    public partial bool IsMissing { get; set; }

    [ObservableProperty]
    public partial string MissingNote { get; set; } = string.Empty;

    /// <summary>Set while loading values, so the form does not re-check once per field.</summary>
    internal bool Silent { get; set; }

    public bool BoolValue
    {
        get => string.Equals(Value, "true", StringComparison.OrdinalIgnoreCase);
        set => Value = value ? "true" : "false";
    }

    /// <summary>
    /// The text is longer than the field takes. It is kept whole (design review, 22 Oct: a paste used to be cut silently,
    /// which could drop a fact) and the lawyer is told; the AI steps and the export wait until it is shortened.
    /// </summary>
    public bool IsTooLong => Value.Length > MaxLength;

    public string TooLongNote => IsTooLong ? _loc.Format("FieldTooLong", Value.Length, MaxLength) : string.Empty;

    /// <summary>
    /// A narrative field (facts, grounds, requests) reads like Darija or mostly French. Formal Arabic is the supported
    /// input: the lawyer is told quietly, never stopped or asked anything.
    /// </summary>
    public bool IsInformal => IsMultiline && Drafting.WritingStyle.LooksInformal(Value);

    public string InformalNote => IsInformal ? _loc["InformalWriting"] : string.Empty;

    partial void OnValueChanged(string value)
    {
        OnPropertyChanged(nameof(IsTooLong));
        OnPropertyChanged(nameof(TooLongNote));
        OnPropertyChanged(nameof(IsInformal));
        OnPropertyChanged(nameof(InformalNote));
        OnPropertyChanged(nameof(BoolValue));
        OnPropertyChanged(nameof(SelectedOption));
        if (!Silent)
        {
            _onChanged();
        }
    }

    internal void Relocalize()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(TooLongNote));
        OnPropertyChanged(nameof(InformalNote));
        foreach (var o in Options)
        {
            o.Relocalize();
        }
    }
}

public sealed class ChoiceOptionViewModel(FieldOption option, Loc loc) : ObservableObject
{
    public string Value => option.Value;

    public string Label => option.Label.Get(loc.Language);

    internal void Relocalize() => OnPropertyChanged(nameof(Label));
}

public enum StepStatus
{
    /// <summary>Nothing entered yet.</summary>
    Empty,

    /// <summary>Entered and nothing missing.</summary>
    Done,

    /// <summary>A recommended ("where applicable") element is missing.</summary>
    Warning,

    /// <summary>A mandatory element is missing.</summary>
    Missing,
}

/// <summary>One step of the case form (a field group), with its completion status from the compliance report.</summary>
public sealed partial class FieldGroupViewModel(FieldGroupDefinition? definition, IReadOnlyList<FieldViewModel> fields, Loc loc, int number) : ObservableObject
{
    public string Id => definition?.Id ?? string.Empty;

    public int Number { get; } = number;

    public string Label => definition?.Label.Get(loc.Language) ?? string.Empty;

    public IReadOnlyList<FieldViewModel> Fields { get; } = fields;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDone), nameof(IsMissing), nameof(IsWarning), nameof(IsEmpty))]
    public partial StepStatus Status { get; set; }

    public bool IsDone => Status == StepStatus.Done;

    public bool IsMissing => Status == StepStatus.Missing;

    public bool IsWarning => Status == StepStatus.Warning;

    public bool IsEmpty => Status == StepStatus.Empty;

    internal void Relocalize()
    {
        OnPropertyChanged(nameof(Label));
        foreach (var f in Fields)
        {
            f.Relocalize();
        }
    }
}
