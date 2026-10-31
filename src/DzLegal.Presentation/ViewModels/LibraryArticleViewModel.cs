using CommunityToolkit.Mvvm.Input;
using DzLegal.Law;

namespace DzLegal.Presentation.ViewModels;

/// <summary>One article in the opened text; only the Copy action changes anything (the clipboard).</summary>
public sealed class LibraryArticleViewModel
{
    public LibraryArticleViewModel(
        LawArticle article,
        LawCode code,
        Loc localization,
        Func<LibraryArticleViewModel, Task> copy)
    {
        ArgumentNullException.ThrowIfNull(article);
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(copy);
        Article = article;
        Code = code;
        L = localization;
        CopyCommand = new AsyncRelayCommand(() => copy(this));
        PathText = string.Join(" / ", article.Path
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim()));
    }

    public LawArticle Article { get; }
    public LawCode Code { get; }
    public Loc L { get; }
    public string NumberText => L.Format("LibraryArticle", Article.Number);
    public string Text => Article.Text;
    public string PathText { get; }
    public bool HasPath => PathText.Length > 0;
    public string AmendedByText => string.IsNullOrWhiteSpace(Article.AmendedBy)
        ? string.Empty
        : L.Format("LibraryAmendedBy", Article.AmendedBy);
    public bool HasAmendedBy => AmendedByText.Length > 0;
    public IAsyncRelayCommand CopyCommand { get; }
}
