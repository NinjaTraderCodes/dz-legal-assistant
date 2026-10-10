using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Presentation.ViewModels;

/// <summary>An article the lawyer can tick: citation, why it fits, and its official text when imported.</summary>
public sealed partial class ArticleChoiceViewModel : ObservableObject
{
    private readonly Action<ArticleChoiceViewModel> _onToggled;

    public ArticleChoiceViewModel(
        ArticleRef reference,
        string citation,
        string why,
        LawArticle? article,
        ArticleProvenance provenance,
        string provenanceTag,
        bool isVerified,
        bool selected,
        Loc loc,
        Action<ArticleChoiceViewModel> onToggled,
        Func<string, Task>? openLink = null)
    {
        ArgumentNullException.ThrowIfNull(loc);
        ArgumentNullException.ThrowIfNull(provenance);
        Reference = reference;
        Citation = citation;
        Why = why;
        IsAvailable = article is { Repealed: false };
        Text = IsAvailable ? article!.Text : loc["ArticleTextUnavailable"];
        ReferenceText = provenance.Location.Length > 0 ? provenance.Reference + "\n" + provenance.Location : provenance.Reference;
        SourceUrl = provenance.SourceUrl;
        LinkLabel = loc[provenance.SourceIsExactText ? "OfficialSource" : "CheckInOfficialJournal"];
        LinkTooltip = SourceUrl is null ? string.Empty
            : provenance.RetrievedOn.Length > 0 ? loc.Format("OfficialSourceTooltip", SourceUrl, provenance.RetrievedOn) : SourceUrl;
        IsVerified = isVerified || provenance.IsTextVerified;
        ProvenanceTag = provenanceTag;
        SourceFile = provenance.SourceFile;
        Page = provenance.Page;
        Edition = provenance.Edition ?? string.Empty;
        _isSelected = selected;
        _onToggled = onToggled;
        _openLink = openLink;
    }

    private readonly Func<string, Task>? _openLink;

    /// <summary>The official reference of the text («الأمر رقم 75-58 …») and, once imported, where the article sits.</summary>
    public string ReferenceText { get; }

    /// <summary>The exact official document it was imported from, or the official site to check it on.</summary>
    public string? SourceUrl { get; }

    public bool HasSource => SourceUrl is not null;

    public string LinkLabel { get; }

    public string LinkTooltip { get; }

    /// <summary>The imported text was checked by a lawyer (and, for a proposal, nothing else is pending).</summary>
    public bool IsVerified { get; }

    /// <summary>Always proposed and ticked for the recognised claim type (not the model's choice).</summary>
    public bool IsRequired { get; init; }

    /// <summary>The official PDF the text was taken from (repository path), and the page it is printed on.</summary>
    public string? SourceFile { get; }

    public int? Page { get; }

    /// <summary>Which publication the text comes from.</summary>
    public string Edition { get; }

    /// <summary>For an article the model proposed: its role («أساس»، «إثبات»…), empty otherwise.</summary>
    public string RoleText { get; init; } = string.Empty;

    public bool HasRole => RoleText.Length > 0;

    /// <summary>For an article the model proposed: «تعليل النموذج: …».</summary>
    public string ModelReason { get; init; } = string.Empty;

    public bool HasModelReason => ModelReason.Length > 0;

    /// <summary>Where the candidate came from: the claim catalog or the search in the official texts.</summary>
    public string SourceText { get; init; } = string.Empty;

    public string ProvenanceTag { get; }

    [RelayCommand]
    private async Task OpenSource()
    {
        if (SourceUrl is not null && _openLink is not null)
        {
            await _openLink(SourceUrl);
        }
    }

    public ArticleRef Reference { get; }

    /// <summary>«المادة 106 من القانون المدني».</summary>
    public string Citation { get; }

    public string Why { get; }

    public bool HasWhy => Why.Length > 0;

    /// <summary>The official text, or a note that the code has not been imported yet.</summary>
    public string Text { get; }

    public bool IsAvailable { get; }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _onToggled(this);
            }
        }
    }

    /// <summary>Updates the tick without reporting it back (another list ticked the same article).</summary>
    internal void Sync(bool selected) => SetProperty(ref _isSelected, selected, nameof(IsSelected));
}

/// <summary>An article the lawyer relies on, shown as a removable chip in «الأسانيد المعتمدة».</summary>
public sealed partial class SelectedArticleViewModel(ArticleRef reference, string citation, Action<ArticleRef> remove)
{
    public ArticleRef Reference { get; } = reference;

    public string Citation { get; } = citation;

    [RelayCommand]
    private void Remove() => remove(Reference);
}

/// <summary>A line the lawyer can insert with one click (a usual request or document of the recognised claim types).</summary>
public sealed partial class InsertChipViewModel(string text, Action<InsertChipViewModel> insert) : ObservableObject
{
    public string Text { get; } = text;

    [ObservableProperty]
    public partial bool IsInserted { get; set; }

    [RelayCommand]
    private void Insert() => insert(this);
}
