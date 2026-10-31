using CommunityToolkit.Mvvm.Input;
using DzLegal.Law;
using DzLegal.Presentation.Services;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// A card in the library list. It carries only the code's metadata and counts; its articles are
/// turned into view models by <see cref="LibraryViewModel"/> when the card is opened.
/// </summary>
public sealed class LibraryTextViewModel
{
    public LibraryTextViewModel(
        LawCode code,
        Loc localization,
        LawSourceManifest manifest,
        IExternalLinks links,
        Func<LibraryTextViewModel, Task> open,
        Func<int, Task> openHelp)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(openHelp);
        Code = code;
        L = localization;
        OpenCommand = new AsyncRelayCommand(() => open(this));
        OfficialSource = OfficialSourceFor(code, manifest);
        OfficialSourceCommand = new AsyncRelayCommand(
            async () => { if (OfficialSource is { } uri) await links.OpenAsync(uri); },
            () => OfficialSource is not null);
        var what = new AsyncRelayCommand(() => openHelp(7));
        CheckBadge = CheckBadgeFor(code.Check, what);
        ReviewBadge = new LibraryBadgeViewModel(
            L[code.Verified ? "LibraryReviewed" : "LibraryNotReviewed"],
            L["Help7Body3"],
            code.Verified ? "ShieldCheckmark" : "Person",
            code.Verified ? "reviewed" : "pending",
            what,
            L["LibraryWhat"]);
    }

    public LawCode Code { get; }
    public Loc L { get; }
    public string Title => Code.Title.Get(L.Language);
    public string Reference => Code.Reference;
    public bool HasReference => !string.IsNullOrWhiteSpace(Reference);
    public string Edition => Code.Edition ?? string.Empty;
    public bool HasEdition => Edition.Length > 0;
    public string AsOfText => string.IsNullOrWhiteSpace(Code.ConsolidatedAsOf)
        ? string.Empty
        : L.Format("LibraryAsOf", Code.ConsolidatedAsOf);
    public bool HasAsOf => AsOfText.Length > 0;
    public string ArticlesText => L.Format("LibraryArticles", Code.Articles.Count);
    public bool IsPartial => Code.Partial;
    public string CoverageText => L[Code.Partial ? "LibraryPartial" : "LibraryComplete"];
    public string CoverageHint => Code.Partial ? L["LibraryPartialHint"] : string.Empty;
    public Uri? OfficialSource { get; }
    public IAsyncRelayCommand OpenCommand { get; }
    public IAsyncRelayCommand OfficialSourceCommand { get; }
    public LibraryBadgeViewModel CheckBadge { get; }
    public LibraryBadgeViewModel ReviewBadge { get; }

    private LibraryBadgeViewModel CheckBadgeFor(LawCheck? check, IAsyncRelayCommand what)
    {
        var (text, hint, icon, tone) = check switch
        {
            LawCheck.OfficialJournalPage => ("LibraryCheckJournal", "LibraryCheckJournalHint", "CheckmarkCircle", "journal"),
            LawCheck.OfficialPdfText => ("LibraryCheckPdf", "LibraryCheckPdfHint", "DocumentPdf", "pdf"),
            LawCheck.PageImagesReadTwice => ("LibraryCheckImages", "LibraryCheckImagesHint", "DocumentText", "images"),
            _ => ("LibraryWhat", "Help7Body3", "Warning", "pending"),
        };
        return new LibraryBadgeViewModel(L[text], L[hint], icon, tone, what, L["LibraryWhat"]);
    }

    private static Uri? OfficialSourceFor(LawCode code, LawSourceManifest manifest)
    {
        if (OfficialLinks.IsOfficial(code.Source?.Url)
            && Uri.TryCreate(code.Source!.Url, UriKind.Absolute, out var source))
        {
            return source;
        }

        var entry = manifest.Codes.FirstOrDefault(item => item.Id == code.Id);
        if (entry is not null
            && OfficialLinks.IsOfficial(entry.OfficialSite)
            && Uri.TryCreate(entry.OfficialSite, UriKind.Absolute, out var site))
        {
            return site;
        }

        return null;
    }
}
