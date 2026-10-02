using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Domain;

namespace DzLegal.App.ViewModels;

/// <summary>
/// One section of the draft in the editor. Narrative sections are edited freely; generated sections
/// are read-only here and change through the form, so they always match the checked inputs.
/// </summary>
public sealed partial class SectionViewModel : ObservableObject
{
    private readonly Loc _loc;
    private readonly Action<SectionViewModel> _onEdited;
    private readonly Action<SectionViewModel, bool> _onOverride;
    private bool _updatingFromModel;

    public SectionViewModel(SectionDefinition definition, string text, Loc loc, Action<SectionViewModel> onEdited, Action<SectionViewModel, bool>? onOverride = null)
    {
        Definition = definition;
        _loc = loc;
        _onEdited = onEdited;
        _onOverride = onOverride ?? ((_, _) => { });
        SetFromModel(text);
    }

    /// <summary>A generated section the lawyer took over: editable, no longer follows the form.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnly), nameof(IsLocked), nameof(CanOverride), nameof(Hint))]
    public partial bool IsOverridden { get; set; }

    public bool IsGenerated => Definition.Kind == SectionKind.Generated;

    /// <summary>Generated and following the form.</summary>
    public bool IsLocked => IsGenerated && !IsOverridden;

    public bool CanOverride => IsLocked;

    [RelayCommand]
    private void Override() => _onOverride(this, true);

    [RelayCommand]
    private void Restore() => _onOverride(this, false);

    public SectionDefinition Definition { get; }

    public string Id => Definition.Id;

    public string Title => Definition.Title.Get(_loc.Language);

    public bool IsEditable => Definition.Kind == SectionKind.Narrative;

    public bool IsReadOnly => !IsEditable && !IsOverridden;

    public string Hint => _loc[IsOverridden ? "OverriddenSectionHint" : IsEditable ? "NarrativeSectionHint" : "GeneratedSectionHint"];

    public string Watermark => IsEditable ? _loc["EmptyNarrativeHint"] : string.Empty;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>Review hints for model-written text (drafting notes, changed or invented numbers).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReview))]
    public partial string ReviewText { get; set; } = string.Empty;

    public bool HasReview => ReviewText.Length > 0;

    internal void SetFromModel(string text)
    {
        _updatingFromModel = true;
        Text = text;
        _updatingFromModel = false;
    }

    partial void OnTextChanged(string value)
    {
        if (!_updatingFromModel)
        {
            _onEdited(this);
        }
    }

    internal void Relocalize()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(Watermark));
    }
}
