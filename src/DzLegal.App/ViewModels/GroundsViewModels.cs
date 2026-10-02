using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.App.ViewModels;

/// <summary>An article the lawyer can tick: citation, why it fits, and its official text when imported.</summary>
public sealed partial class ArticleChoiceViewModel : ObservableObject
{
    private readonly Action<ArticleChoiceViewModel> _onToggled;

    public ArticleChoiceViewModel(ArticleRef reference, string citation, string why, LawArticle? article, bool selected, Loc loc, Action<ArticleChoiceViewModel> onToggled)
    {
        ArgumentNullException.ThrowIfNull(loc);
        Reference = reference;
        Citation = citation;
        Why = why;
        IsAvailable = article is { Repealed: false };
        Text = IsAvailable ? article!.Text : loc["ArticleTextUnavailable"];
        _isSelected = selected;
        _onToggled = onToggled;
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
        Action<ClaimSuggestionViewModel> insertDocuments)
    {
        ArgumentNullException.ThrowIfNull(suggestion);
        ArgumentNullException.ThrowIfNull(loc);
        Claim = suggestion.Claim;
        Label = Claim.Label.Get(loc.Language);
        MatchedText = loc.Format("GroundsMatched", string.Join("، ", suggestion.MatchedTerms));
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

    /// <summary>Which of the lawyer's words led to the suggestion, so it can be judged at a glance.</summary>
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
