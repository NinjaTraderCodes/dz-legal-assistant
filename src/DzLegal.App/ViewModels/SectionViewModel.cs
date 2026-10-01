using CommunityToolkit.Mvvm.ComponentModel;
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
    private bool _updatingFromModel;

    public SectionViewModel(SectionDefinition definition, string text, Loc loc, Action<SectionViewModel> onEdited)
    {
        Definition = definition;
        _loc = loc;
        _onEdited = onEdited;
        SetFromModel(text);
    }

    public SectionDefinition Definition { get; }

    public string Id => Definition.Id;

    public string Title => Definition.Title.Get(_loc.Language);

    public bool IsEditable => Definition.Kind == SectionKind.Narrative;

    public bool IsReadOnly => !IsEditable;

    public string Hint => _loc[IsEditable ? "NarrativeSectionHint" : "GeneratedSectionHint"];

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
