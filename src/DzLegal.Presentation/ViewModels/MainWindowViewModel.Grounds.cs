using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.Services;
using DzLegal.Domain;
using DzLegal.Drafting;
using DzLegal.Law;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// The Grounds step: on the lawyer's click, the local model reads the facts, the official library is searched, and
/// the model proposes articles from it (<see cref="GroundsAnalyzer"/>). The lawyer ticks what to rely on; ticked
/// articles are the legal basis the grounds are drafted from. The Requests and Documents steps offer the usual
/// lines of the claim types the analysis recognised.
/// </summary>
public sealed partial class MainWindowViewModel
{
    public const string FactsStepId = "facts";
    public const string GroundsStepId = "grounds";
    public const string RequestsStepId = "requests";
    public const string DocumentsStepId = "documents";

    private const string FactsField = "notes.facts";
    private const string GroundsField = "notes.grounds";
    private const string RequestsField = "notes.requests";
    private const string DocumentsField = "documents.list";

    private LegalKnowledge _knowledge = LegalKnowledge.Empty;
    private IExternalLinks _links = new NoExternalLinks();
    private readonly List<ArticleRef> _selectedArticles = [];
    private GroundsAnalysis? _analysis;

    /// <summary>Articles the model proposed in the last analysis, to tick.</summary>
    public ObservableCollection<ArticleChoiceViewModel> AnalysedArticles { get; } = [];

    /// <summary>«الأسانيد المعتمدة»: every ticked article, from the analysis or the search.</summary>
    public ObservableCollection<SelectedArticleViewModel> SelectedGrounds { get; } = [];

    public ObservableCollection<ArticleChoiceViewModel> ArticleResults { get; } = [];

    public ObservableCollection<InsertChipViewModel> RequestChips { get; } = [];

    public ObservableCollection<InsertChipViewModel> DocumentChips { get; } = [];

    public bool HasAnalysedArticles => AnalysedArticles.Count > 0;

    public bool HasRequestChips => RequestChips.Count > 0;

    public bool HasDocumentChips => DocumentChips.Count > 0;

    public bool IsGroundsStep => SelectedStep?.Id == GroundsStepId;

    public bool IsRequestsStep => SelectedStep?.Id == RequestsStepId;

    public bool IsDocumentsStep => SelectedStep?.Id == DocumentsStepId;

    /// <summary>Whether any official text has been imported; until then citations show without text.</summary>
    public bool IsLibraryImported => _knowledge.Library.Codes.Count > 0;

    public bool CanAnalyseGrounds => _knowledge.Prompts.IsAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReading))]
    public partial string ReadingRelationship { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReadingBreach { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReadingSought { get; set; } = string.Empty;

    /// <summary>The catalog claim types the analysis recognised, by label.</summary>
    [ObservableProperty]
    public partial string ReadingClaims { get; set; } = string.Empty;

    public bool HasReading => ReadingRelationship.Length + ReadingBreach.Length + ReadingSought.Length + ReadingClaims.Length > 0;

    /// <summary>The analysis recognised a catalog claim type (otherwise it proposes nothing).</summary>
    public bool HasRecognisedClaims => _analysis?.ClaimIds.Count > 0;

    /// <summary>
    /// The lawyer confirmed the claim type the analysis recognised. Nothing is ticked before that: the model's guess
    /// never decides which law the draft rests on.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsClaimConfirmation))]
    public partial bool IsClaimConfirmed { get; set; }

    public bool NeedsClaimConfirmation => HasRecognisedClaims && !IsClaimConfirmed;

    /// <summary>The facts, subject or court section changed after the last analysis.</summary>
    [ObservableProperty]
    public partial bool IsAnalysisStale { get; set; }

    /// <summary>Progress while the analysis runs, or its outcome («اقترح النموذج 5 مواد…»).</summary>
    [ObservableProperty]
    public partial string AnalysisStatus { get; set; } = string.Empty;

    /// <summary>Prerequisites of the recognised claim types (e.g. labour conciliation), for the Requests and Documents steps.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrerequisites))]
    public partial string Prerequisites { get; set; } = string.Empty;

    public bool HasPrerequisites => Prerequisites.Length > 0;

    [ObservableProperty]
    public partial string ArticleQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ArticleSearchHint { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedArticles))]
    public partial int SelectedArticleCount { get; set; }

    public bool HasSelectedArticles => SelectedArticleCount > 0;

    public IReadOnlyList<ArticleRef> SelectedArticles => _selectedArticles;

    public GroundsAnalysis? Analysis => _analysis;

    private void UseKnowledge(LegalKnowledge? knowledge, IExternalLinks? links)
    {
        _links = links ?? new NoExternalLinks();
        _knowledge = knowledge ?? LegalKnowledge.Empty;
    }

    /// <summary>
    /// Runs the grounds analysis with the selected local model (about 1–2 minutes on a CPU-only laptop). The lawyer's
    /// ticks are kept; the proposals are replaced.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AnalyseGroundsAsync(CancellationToken cancellationToken)
    {
        var inputs = CurrentInputs();
        var facts = inputs.GetValueOrDefault(FactsField, string.Empty);
        if (RefuseWhileTooLong())
        {
            return;
        }

        if (SelectedProvider is null || !CanAnalyseGrounds || facts.Trim().Length == 0)
        {
            AnalysisStatus = L["GroundsNeedFacts"];
            return;
        }

        IModelProvider? provider = null;
        try
        {
            provider = SelectedProvider.Create();
            AnalysisStatus = string.Empty;
            AnalysisActivity.Start([L["AnalysisStepReading"], L["AnalysisStepSearching"], L["AnalysisStepSelecting"]], Expect(AnalysisTask, provider));
            var progress = new InlineProgress<AnalysisStage>(stage =>
                AnalysisActivity.BeginStep((int)stage, waitsForModel: stage != AnalysisStage.Searching));
            var analyzer = new GroundsAnalyzer(provider)
            {
                OnPartial = (_, text) => AnalysisActivity.OnText(text),
                OnCandidates = found => AnalysisActivity.SetDetail((int)AnalysisStage.Searching, L.Format("AnalysisCandidates", found.Count)),
            };
            var analysis = await analyzer.AnalyseAsync(
                _knowledge,
                inputs.GetValueOrDefault("claim.subject", string.Empty),
                facts,
                inputs.GetValueOrDefault("court.section", string.Empty),
                progress,
                cancellationToken);
            RecordDuration(AnalysisTask, provider, AnalysisActivity);
            AnalysisActivity.Complete();
            ShowAnalysis(analysis);

            // Nothing is ticked yet: the lawyer first confirms the claim type (ConfirmClaim), or searches by hand.
            NoteAnswer(provider);
            AnalysisStatus = analysis.ClaimIds.Count > 0 ? L.Format("AnalysisDone", analysis.Articles.Count, provider.Name) // the engine that answered
                : HasReading ? L["AnalysisOutOfScope"]
                : L.Format("AnalysisNothing", SelectedProvider.DisplayName);
        }
        catch (OperationCanceledException)
        {
            AnalysisStatus = L["Cancelled"];
        }
        catch (Exception ex) when (IsModelOrRenderFailure(ex))
        {
            AnalysisStatus = ErrorText(ex);
        }
        finally
        {
            IsAnalysisAutomatic = false;
            AnalysisActivity.Stop();
            (provider as IDisposable)?.Dispose();
        }
    }

    /// <summary>The lawyer confirms the recognised claim type: the articles that type rests on («لازمة») are ticked.</summary>
    [RelayCommand]
    private void ConfirmClaim()
    {
        if (_analysis is null || _analysis.ClaimIds.Count == 0)
        {
            return;
        }

        _analysis = _analysis with { ClaimConfirmed = true };
        IsClaimConfirmed = true;
        foreach (var choice in AnalysedArticles.Where(c => _analysis.Articles.Any(a => a.Ref == c.Reference && a.Source == AnalysedArticle.Required)))
        {
            choice.IsSelected = true;
        }

        AnalysisStatus = L["ClaimConfirmedStatus"];
    }

    /// <summary>The lawyer rejects the recognised claim type: its proposals are withdrawn; the reading stays for reference.</summary>
    [RelayCommand]
    private void RejectClaim()
    {
        if (_analysis is null)
        {
            return;
        }

        ShowAnalysis(_analysis with { ClaimIds = [], Articles = [], ClaimConfirmed = false });
        AnalysisStatus = L["ClaimRejectedStatus"];
    }

    /// <summary>Ticks every article the model proposed as a basis («أساس»).</summary>
    [RelayCommand]
    private void TickBasisArticles()
    {
        foreach (var choice in AnalysedArticles.Where(a => _analysis?.Articles.Any(x => x.Ref == a.Reference && x.Role == "basis") == true))
        {
            choice.IsSelected = true;
        }
    }

    private void ShowAnalysis(GroundsAnalysis? analysis)
    {
        _analysis = analysis;
        IsClaimConfirmed = analysis?.ClaimConfirmed == true;
        OnPropertyChanged(nameof(HasRecognisedClaims));
        OnPropertyChanged(nameof(NeedsClaimConfirmation));
        AnalysedArticles.Clear();
        ReadingRelationship = analysis?.Relationship ?? string.Empty;
        ReadingBreach = analysis?.Breach ?? string.Empty;
        ReadingSought = analysis?.Sought ?? string.Empty;
        var claims = (analysis?.ClaimIds ?? []).Select(id => _knowledge.Claims.FirstOrDefault(c => c.Id == id)).OfType<ClaimType>().ToList();
        ReadingClaims = string.Join("، ", claims.Select(c => c.Label.Get(L.Language)));
        foreach (var a in analysis?.Articles ?? [])
        {
            var article = _knowledge.Library.FindArticle(a.Code, a.Number);
            var catalogWhy = claims.SelectMany(c => c.Articles).FirstOrDefault(x => x.Code == a.Code && x.Number == ArticleNumbers.Canonical(a.Number))?.Why.Get(L.Language);
            AnalysedArticles.Add(new ArticleChoiceViewModel(
                a.Ref,
                $"المادة {a.Number} من {_knowledge.CitationName(a.Code)}",
                a.Reason.Length == 0 ? catalogWhy ?? string.Empty : string.Empty,
                article,
                _knowledge.ProvenanceOf(a.Code, article),
                L[a.Source == AnalysedArticle.Required ? "ProvenanceRequired" : "ProvenanceModel"],
                isVerified: false,
                _selectedArticles.Contains(a.Ref),
                L,
                OnArticleToggled,
                OpenOfficialLinkAsync)
            {
                RoleText = L["Role_" + (CatalogEntry(a.Ref)?.Role.ToString().ToLowerInvariant() ?? a.Role)],
                ModelReason = a.Reason.Length > 0 ? L.Format("ModelReason", a.Reason) : string.Empty,
                SourceText = L[a.Source == AnalysedArticle.FromSearch ? "FromLibrarySearch" : "FromClaimCatalog"],
                IsRequired = a.Source == AnalysedArticle.Required,
            });
        }

        RequestChips.Clear();
        foreach (var line in claims.SelectMany(c => c.SuggestedRequests).Distinct())
        {
            RequestChips.Add(new InsertChipViewModel(line, chip => InsertChip(RequestsField, chip)));
        }

        DocumentChips.Clear();
        foreach (var line in claims.SelectMany(c => c.RequiredDocuments).Distinct())
        {
            DocumentChips.Add(new InsertChipViewModel(line, chip => InsertChip(DocumentsField, chip)));
        }

        Prerequisites = string.Join("\n", claims.SelectMany(c => c.Prerequisites).Select(p => "⚠ " + p.Get(L.Language)).Distinct());
        OnPropertyChanged(nameof(HasAnalysedArticles));
        OnPropertyChanged(nameof(HasRequestChips));
        OnPropertyChanged(nameof(HasDocumentChips));
        UpdateAnalysisStaleness();
    }

    /// <summary>Called on every input change: the analysis is stale once the analysed facts no longer match.</summary>
    private void UpdateAnalysisStaleness()
    {
        if (_analysis is null)
        {
            IsAnalysisStale = false;
            return;
        }

        var inputs = CurrentInputs();
        IsAnalysisStale = _analysis.FactsHash != GroundsAnalyzer.FactsHash(
            inputs.GetValueOrDefault("claim.subject", string.Empty),
            inputs.GetValueOrDefault(FactsField, string.Empty),
            inputs.GetValueOrDefault("court.section", string.Empty));
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
            var reference = new ArticleRef(hit.Code.Id, hit.Article.Number);
            ArticleResults.Add(new ArticleChoiceViewModel(
                reference,
                hit.Citation,
                string.Empty,
                hit.Article,
                _knowledge.ProvenanceOf(hit.Code.Id, hit.Article),
                L[hit.Code.Verified ? "ProvenanceVerified" : "ProvenanceTextUnverified"],
                hit.Code.Verified,
                _selectedArticles.Contains(reference),
                L,
                OnArticleToggled,
                OpenOfficialLinkAsync));
        }

        ArticleSearchHint = ArticleResults.Count == 0 ? L["ArticleSearchNone"] : string.Empty;
    }

    private void OnArticleToggled(ArticleChoiceViewModel choice)
    {
        SetSelected(choice.Reference, choice.IsSelected);
    }

    private void SetSelected(ArticleRef reference, bool selected)
    {
        if (selected && !_selectedArticles.Contains(reference))
        {
            _selectedArticles.Add(reference);
        }
        else if (!selected)
        {
            _selectedArticles.Remove(reference);
        }

        // The same article can appear among the proposals and in the search results.
        foreach (var other in AnalysedArticles.Concat(ArticleResults).Where(c => c.Reference == reference))
        {
            other.Sync(selected);
        }

        RefreshSelectedGrounds();
        WithdrawReviewsIfCaseChanged();
        UpdateReviewHints();
        UpdateSubstanceIssues();
    }

    private void RefreshSelectedGrounds()
    {
        SelectedGrounds.Clear();
        foreach (var a in _selectedArticles)
        {
            SelectedGrounds.Add(new SelectedArticleViewModel(a, Citation(a), r => SetSelected(r, false)));
        }

        SelectedArticleCount = _selectedArticles.Count;
    }

    private void LoadGroundsState(DraftDocument draft)
    {
        _selectedArticles.Clear();
        _selectedArticles.AddRange(draft.SelectedArticles.Distinct());
        ShowAnalysis(draft.GroundsAnalysis);
        AnalysisStatus = string.Empty;
        ArticleQuery = string.Empty;
        RefreshSelectedGrounds();
    }

    private string Citation(ArticleRef a) => $"المادة {a.Number} من {_knowledge.CitationName(a.Code)}";

    private void InsertChip(string fieldId, InsertChipViewModel chip)
    {
        var field = Field(fieldId);
        if (!ArabicScript.Normalize(field.Value).Contains(ArabicScript.Normalize(chip.Text), StringComparison.Ordinal))
        {
            var current = field.Value.TrimEnd();
            field.Value = (current.Length > 0 ? current + "\n" : string.Empty) + "- " + chip.Text;
        }

        chip.IsInserted = true;
    }

    /// <summary>
    /// The inputs the model drafts from: the grounds notes carry the ticked articles' citations (those not already
    /// written there), so the grounds are drafted from the selection even when the lawyer adds no argument.
    /// </summary>
    internal Dictionary<string, string> DraftingInputs()
    {
        var inputs = CurrentInputs();
        var notes = inputs.GetValueOrDefault(GroundsField, string.Empty);
        var present = ArabicScript.Normalize(notes);
        var lines = _selectedArticles.Select(Citation)
            .Where(c => !present.Contains(ArabicScript.Normalize(c), StringComparison.Ordinal))
            .Select(c => "- " + c)
            .ToList();
        if (lines.Count > 0)
        {
            inputs[GroundsField] = string.Join("\n", lines) + (notes.Trim().Length > 0 ? "\n" + notes : string.Empty);
        }

        return inputs;
    }

    /// <summary>Opens an official source in the browser, on the lawyer's click only, after re-checking the host.</summary>
    internal async Task OpenOfficialLinkAsync(string url)
    {
        if (!OfficialLinks.IsOfficial(url))
        {
            SetStatus(L["LinkRefused"], isError: true);
            return;
        }

        var opened = await _links.OpenAsync(new Uri(url));
        SetStatus(opened ? L["LinkOpened"] : L.Format("LinkNotOpened", url), isError: !opened);
    }

    /// <summary>
    /// Official texts of the ticked articles that are in the library: what the model may cite. Each carries its part in
    /// the claim and its one-line rule from the catalog, taken from the confirmed claim type first, so the grounds are
    /// argued in a lawyer's order (obligation, proof, compensation) and state rules only in the catalog's wording.
    /// </summary>
    /// <summary>
    /// What the grounds are drafted and checked against: the ticked articles, then the articles the lawyer's own
    /// notes cite without ticking them. A cited article the library holds gets its text, role and rule like a ticked
    /// one, rather than a bare citation.
    /// </summary>
    internal IReadOnlyList<LegalText> LegalTextsForDrafting() =>
        LegalTextsOf(_selectedArticles.Concat(CitedInNotes().Where(a => !_selectedArticles.Contains(a))).ToList());

    private IEnumerable<ArticleRef> CitedInNotes()
    {
        foreach (var (number, codeName) in ArguedGrounds.CitedInOwnLines(CurrentInputs().GetValueOrDefault(GroundsField, string.Empty)))
        {
            var code = codeName is null
                ? "civil-code"
                : _knowledge.Library.Codes.Select(c => c.Id).FirstOrDefault(id =>
                    ArabicScript.Normalize(_knowledge.CitationName(id)).Contains(ArabicScript.Normalize(codeName), StringComparison.Ordinal));
            if (code is not null && _knowledge.Library.FindArticle(code, number) is { } article)
            {
                yield return new ArticleRef(code, article.Number);
            }
        }
    }

    private List<LegalText> LegalTextsOf(IReadOnlyList<ArticleRef> articles) => articles
        .Select(a => (Ref: a, Article: _knowledge.Library.FindArticle(a.Code, a.Number)))
        .Where(x => x.Article is { Repealed: false })
        .Select(x => (x.Ref, x.Article, Entry: CatalogEntry(x.Ref), Part: PartInConfirmedClaim(x.Ref)))
        .Select(x => new LegalText(x.Article!.Number, _knowledge.CitationName(x.Ref.Code), x.Article.Text)
        {
            Role = x.Part?.Role.ToString().ToLowerInvariant(),
            Gist = x.Entry?.Why.Ar,
        })
        .ToList();

    /// <summary>
    /// The article's part in the claim the grounds argue: from the confirmed claim type only, once there is one. Art. 124
    /// is the basis of a tort claim, not of a loan claim: taken from any claim, it joined the loan's basis paragraph
    /// (harness, case 12, 18 Oct). An article outside the confirmed claim gets its own paragraph.
    /// </summary>
    private ClaimArticle? PartInConfirmedClaim(ArticleRef a)
    {
        var confirmed = (_analysis?.ClaimIds ?? []).Select(id => _knowledge.Claims.FirstOrDefault(c => c.Id == id)).OfType<ClaimType>().ToList();
        return (confirmed.Count > 0 ? confirmed : _knowledge.Claims).SelectMany(c => c.Articles)
            .FirstOrDefault(c => c.Code == a.Code && c.Number == ArticleNumbers.Canonical(a.Number));
    }

    /// <summary>
    /// The catalog's entry for an article, from the confirmed claims first: the same role is shown on the article's
    /// card and used to place it in the grounds.
    /// </summary>
    private ClaimArticle? CatalogEntry(ArticleRef a)
    {
        var confirmed = (_analysis?.ClaimIds ?? []).Select(id => _knowledge.Claims.FirstOrDefault(c => c.Id == id)).OfType<ClaimType>();
        return confirmed.Concat(_knowledge.Claims).SelectMany(c => c.Articles)
            .FirstOrDefault(c => c.Code == a.Code && c.Number == ArticleNumbers.Canonical(a.Number));
    }
}
