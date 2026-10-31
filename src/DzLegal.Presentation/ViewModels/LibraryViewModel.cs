using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Law;
using DzLegal.Presentation.Services;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// The read-only law library: one card per loaded text, and the articles of the text the lawyer
/// opens. Article view models are created only on opening, never for every code at once.
/// </summary>
public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IUserFiles _files;
    private IReadOnlyList<LibraryArticleViewModel> _allArticles = [];
    private LawLibrary? _openedLibrary;

    public LibraryViewModel(
        LawLibrary library,
        LawSourceManifest manifest,
        Loc localization,
        IExternalLinks links,
        IUserFiles files,
        Func<int, Task> openHelp)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(openHelp);
        L = localization;
        _files = files;
        Texts = library.Codes
            .OrderByDescending(code => !code.Partial)
            .ThenByDescending(code => code.Articles.Count)
            .ThenBy(code => code.Id, StringComparer.Ordinal)
            .Select(code => new LibraryTextViewModel(code, L, manifest, links, OpenTextAsync, openHelp))
            .ToArray();
        Summary = L.Format(
            "LibrarySummary",
            library.Codes.Sum(code => code.Articles.Count),
            library.Codes.Count,
            library.Codes.Count(code => !code.Partial));
    }

    public Loc L { get; }
    public IReadOnlyList<LibraryTextViewModel> Texts { get; }
    public string Summary { get; }
    public string Note => L["LibraryNote"];
    public string Title => OpenedText?.Title ?? L["LibraryTitle"];
    public bool IsReading => OpenedText is not null;
    public bool IsList => !IsReading;
    public bool IsRightToLeft => L.IsRightToLeft;
    public bool HasNoResult => IsReading && SearchText.Trim().Length > 0 && Articles.Count == 0;
    public bool HasStatus => StatusMessage.Length > 0;

    [ObservableProperty] public partial LibraryTextViewModel? OpenedText { get; private set; }
    [ObservableProperty] public partial IReadOnlyList<LibraryArticleViewModel> Articles { get; private set; } = [];
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial string StatusMessage { get; private set; } = string.Empty;

    [RelayCommand] private void Back()
    {
        OpenedText = null;
        _openedLibrary = null;
        _allArticles = [];
        Articles = [];
        SearchText = string.Empty;
        StatusMessage = string.Empty;
    }

    private Task OpenTextAsync(LibraryTextViewModel text)
    {
        _openedLibrary = new LawLibrary([text.Code]);
        _allArticles = text.Code.Articles
            .Select(article => new LibraryArticleViewModel(article, text.Code, L, CopyAsync))
            .ToArray();
        OpenedText = text;
        SearchText = string.Empty;
        StatusMessage = string.Empty;
        Articles = _allArticles;
        return Task.CompletedTask;
    }

    private async Task CopyAsync(LibraryArticleViewModel article)
    {
        var text = L.Format(
            "LibraryCopyFormat",
            article.Article.Number,
            article.Code.CitationName,
            article.Article.Text);
        await _files.CopyTextAsync(text);
        StatusMessage = L["LibraryCopied"];
    }

    partial void OnOpenedTextChanged(LibraryTextViewModel? value)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsReading));
        OnPropertyChanged(nameof(IsList));
        OnPropertyChanged(nameof(HasNoResult));
    }

    partial void OnSearchTextChanged(string value) => RefreshArticles();

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    private void RefreshArticles()
    {
        if (OpenedText is null || _openedLibrary is null)
        {
            Articles = [];
            OnPropertyChanged(nameof(HasNoResult));
            return;
        }

        var query = SearchText.Trim();
        if (query.Length == 0)
        {
            Articles = _allArticles;
            OnPropertyChanged(nameof(HasNoResult));
            return;
        }

        var code = _openedLibrary.Codes[0];
        var hits = ArticleSearch.Find(_openedLibrary, query, Math.Max(1, code.Articles.Count));
        Articles = hits
            .Select(hit => new LibraryArticleViewModel(hit.Article, code, L, CopyAsync))
            .ToArray();
        OnPropertyChanged(nameof(HasNoResult));
    }
}
