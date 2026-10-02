using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Domain;
using DzLegal.Drafting;
using DzLegal.Law;

namespace DzLegal.App.ViewModels;

/// <summary>
/// Grounds suggestions: claim types matched deterministically from the facts, their articles from the offline law
/// library, and article search. The lawyer ticks what to rely on; the official texts of the ticked articles are
/// what the model may cite. Nothing here is decided by a model.
/// </summary>
public sealed partial class MainWindowViewModel
{
    /// <summary>The form step whose fields feed the narrative sections; the suggestions card shows there.</summary>
    public const string NotesStepId = "notes";

    private const string FactsField = "notes.facts";
    private const string GroundsField = "notes.grounds";
    private const string RequestsField = "notes.requests";
    private const string DocumentsField = "documents.list";

    private LegalKnowledge _knowledge = LegalKnowledge.Empty;
    private GroundsSuggester _suggester = new(LegalKnowledge.Empty);
    private readonly List<ArticleRef> _selectedArticles = [];
    private string _suggestionKey = string.Empty;

    public ObservableCollection<ClaimSuggestionViewModel> GroundsSuggestions { get; } = [];

    public ObservableCollection<ArticleChoiceViewModel> ArticleResults { get; } = [];

    public bool IsNotesStep => SelectedStep?.Id == NotesStepId;

    /// <summary>Whether any official text has been imported; until then citations show without text.</summary>
    public bool IsLibraryImported => _knowledge.Library.Codes.Count > 0;

    /// <summary>Why the list is empty, or empty when there are suggestions.</summary>
    [ObservableProperty]
    public partial string GroundsHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ArticleQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ArticleSearchHint { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedArticles))]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedToGroundsCommand))]
    public partial int SelectedArticleCount { get; set; }

    public bool HasSelectedArticles => SelectedArticleCount > 0;

    public string AddSelectedText => L.Format("GroundsAddSelected", SelectedArticleCount);

    public IReadOnlyList<ArticleRef> SelectedArticles => _selectedArticles;

    private void UseKnowledge(LegalKnowledge? knowledge)
    {
        _knowledge = knowledge ?? LegalKnowledge.Empty;
        _suggester = new GroundsSuggester(_knowledge);
    }

    /// <summary>Re-ranks claim types when the facts, subject or court section changed (cheap: no model).</summary>
    private void RefreshSuggestions(bool force = false)
    {
        var inputs = CurrentInputs();
        var facts = inputs.GetValueOrDefault(FactsField, string.Empty);
        var text = inputs.GetValueOrDefault("claim.subject", string.Empty) + "\n" + facts;
        var section = inputs.GetValueOrDefault("court.section", string.Empty);
        var key = text + "\u0001" + section + "\u0001" + L.Language;
        if (!force && key == _suggestionKey)
        {
            return;
        }

        _suggestionKey = key;
        var expanded = GroundsSuggestions.Where(s => s.IsExpanded).Select(s => s.Claim.Id).ToHashSet();
        GroundsSuggestions.Clear();
        foreach (var suggestion in _suggester.Suggest(text, GroundsSuggester.AreaOf(section)))
        {
            var articles = suggestion.Articles
                .Select(a => CreateChoice(new ArticleRef(a.Reference.Code, a.Reference.Number), a.Citation, a.Reference.Why.Get(L.Language), a.Article))
                .ToList();
            GroundsSuggestions.Add(new ClaimSuggestionViewModel(suggestion, articles, L, InsertRequests, InsertDocuments)
            {
                IsExpanded = expanded.Contains(suggestion.Claim.Id) || (expanded.Count == 0 && GroundsSuggestions.Count == 0),
            });
        }

        GroundsHint = facts.Trim().Length == 0 ? L["GroundsNeedFacts"]
            : GroundsSuggestions.Count == 0 ? L["GroundsNone"]
            : string.Empty;
        SearchArticles();
    }

    partial void OnArticleQueryChanged(string value) => SearchArticles();

    private void SearchArticles()
    {
        ArticleResults.Clear();
        if (ArticleQuery.Trim().Length == 0)
        {
            ArticleSearchHint = string.Empty;
            return;
        }

        if (!IsLibraryImported)
        {
            ArticleSearchHint = L["LibraryNotImported"];
            return;
        }

        foreach (var hit in ArticleSearch.Find(_knowledge.Library, ArticleQuery))
        {
            ArticleResults.Add(CreateChoice(new ArticleRef(hit.Code.Id, hit.Article.Number), hit.Citation, string.Empty, hit.Article));
        }

        ArticleSearchHint = ArticleResults.Count == 0 ? L["ArticleSearchNone"] : string.Empty;
    }

    private ArticleChoiceViewModel CreateChoice(ArticleRef reference, string citation, string why, LawArticle? article) =>
        new(reference, citation, why, article, _selectedArticles.Contains(reference), L, OnArticleToggled);

    private void OnArticleToggled(ArticleChoiceViewModel choice)
    {
        if (choice.IsSelected && !_selectedArticles.Contains(choice.Reference))
        {
            _selectedArticles.Add(choice.Reference);
        }
        else if (!choice.IsSelected)
        {
            _selectedArticles.Remove(choice.Reference);
        }

        // The same article can appear under two claims and in the search results.
        foreach (var other in GroundsSuggestions.SelectMany(s => s.Articles).Concat(ArticleResults).Where(c => c != choice && c.Reference == choice.Reference))
        {
            other.Sync(choice.IsSelected);
        }

        SelectedArticleCount = _selectedArticles.Count;
        OnPropertyChanged(nameof(AddSelectedText));
        UpdateReviewHints();
    }

    private void SetSelectedArticles(IEnumerable<ArticleRef> articles)
    {
        _selectedArticles.Clear();
        _selectedArticles.AddRange(articles.Distinct());
        SelectedArticleCount = _selectedArticles.Count;
        OnPropertyChanged(nameof(AddSelectedText));
        RefreshSuggestions(force: true);
    }

    /// <summary>Adds «- المادة N من …» lines for the ticked articles that the grounds notes do not cite yet.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedArticles))]
    private void AddSelectedToGrounds()
    {
        var field = Field(GroundsField);
        var present = ArabicScript.Normalize(field.Value);
        var lines = _selectedArticles
            .Select(a => $"المادة {a.Number} من {_knowledge.CitationName(a.Code)}")
            .Where(c => !present.Contains(ArabicScript.Normalize(c), StringComparison.Ordinal))
            .Select(c => "- " + c)
            .ToList();
        if (lines.Count > 0)
        {
            Append(field, lines);
        }

        SetStatus(L.Format("GroundsAdded", lines.Count));
    }

    private void InsertRequests(ClaimSuggestionViewModel claim) => InsertLines(RequestsField, claim.Claim.SuggestedRequests);

    private void InsertDocuments(ClaimSuggestionViewModel claim) => InsertLines(DocumentsField, claim.Claim.RequiredDocuments);

    private void InsertLines(string fieldId, IReadOnlyList<string> items)
    {
        var field = Field(fieldId);
        var present = ArabicScript.Normalize(field.Value);
        var lines = items.Where(i => !present.Contains(ArabicScript.Normalize(i), StringComparison.Ordinal)).Select(i => "- " + i).ToList();
        if (lines.Count > 0)
        {
            Append(field, lines);
        }

        SetStatus(L.Format("GroundsInserted", lines.Count, field.Label));
    }

    private static void Append(FieldViewModel field, List<string> lines)
    {
        var current = field.Value.TrimEnd();
        field.Value = (current.Length > 0 ? current + "\n" : string.Empty) + string.Join("\n", lines);
    }

    /// <summary>Official texts of the ticked articles that are in the library: what the model may cite and quote.</summary>
    internal IReadOnlyList<LegalText> SelectedLegalTexts() =>
        _selectedArticles
            .Select(a => (Ref: a, Article: _knowledge.Library.FindArticle(a.Code, a.Number)))
            .Where(x => x.Article is { Repealed: false })
            .Select(x => new LegalText(x.Article!.Number, _knowledge.CitationName(x.Ref.Code), x.Article.Text))
            .ToList();
}
