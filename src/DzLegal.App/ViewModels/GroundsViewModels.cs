using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.App.ViewModels;

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
        bool claimVerified,
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
        IsVerified = claimVerified && provenance.IsTextVerified;
        ProvenanceTag = loc[IsVerified ? "ProvenanceVerified" : "ProvenanceCatalog"];
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

    /// <summary>Both the claim's article list and the imported text were checked by a lawyer.</summary>
    public bool IsVerified { get; }

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

/// <summary>A suggested claim type, with its articles to tick and its usual requests and documents to insert.</summary>
public sealed partial class ClaimSuggestionViewModel : ObservableObject
{
    private readonly Action<ClaimSuggestionViewModel> _insertRequests;
    private readonly Action<ClaimSuggestionViewModel> _insertDocuments;

    public ClaimSuggestionViewModel(
        ClaimSuggestion suggestion,
        IReadOnlyList<ArticleChoiceViewModel> articles,
        Loc loc,
        Action<ClaimSuggestionViewModel> insertRequests,
        Action<ClaimSuggestionViewModel> insertDocuments,
        bool pickedByModel = false)
    {
        ArgumentNullException.ThrowIfNull(suggestion);
        ArgumentNullException.ThrowIfNull(loc);
        Claim = suggestion.Claim;
        Label = Claim.Label.Get(loc.Language);
        IsPickedByModel = pickedByModel;
        MatchedText = pickedByModel ? loc["PickedByModel"] : loc.Format("GroundsMatched", string.Join("، ", suggestion.MatchedTerms));
        Articles = articles;
        Prerequisites = string.Join("\n", Claim.Prerequisites.Select(p => "⚠ " + p.Get(loc.Language)));
        FactsToState = string.Join("\n", Claim.FactsToState.Select(f => "• " + f));
        Requests = string.Join("\n", Claim.SuggestedRequests.Select(r => "• " + r));
        Documents = string.Join("\n", Claim.RequiredDocuments.Select(d => "• " + d));
        _insertRequests = insertRequests;
        _insertDocuments = insertDocuments;
    }

    public ClaimType Claim { get; }

    public string Label { get; }

    /// <summary>Chosen by the local model from the catalog (not by keyword matching).</summary>
    public bool IsPickedByModel { get; }

    /// <summary>Which of the lawyer's words led to the suggestion, or that the model chose it from the catalog.</summary>
    public string MatchedText { get; }

    public IReadOnlyList<ArticleChoiceViewModel> Articles { get; }

    public string Prerequisites { get; }

    public bool HasPrerequisites => Prerequisites.Length > 0;

    public string FactsToState { get; }

    public string Requests { get; }

    public string Documents { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [RelayCommand]
    private void InsertRequests() => _insertRequests(this);

    [RelayCommand]
    private void InsertDocuments() => _insertDocuments(this);
}
