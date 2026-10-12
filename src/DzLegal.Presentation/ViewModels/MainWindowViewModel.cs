using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.Services;
using DzLegal.Documents;
using DzLegal.Domain;
using DzLegal.Drafting;
using DzLegal.FormEngine;
using DzLegal.ModelProviders.Stub;
using DzLegal.Storage;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// The core loop: case form → draft → compliance report → edit → re-check.
/// The draft is kept live: generated sections follow the form as the lawyer types, narrative sections
/// are written by the selected engine on demand and can be edited freely, and the Form Engine re-checks
/// after every change.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly DocumentTypeDefinition _definition;
    private readonly IUserFiles _files;
    private readonly EvaluationStore _evaluations;
    private readonly Func<DateOnly> _today;

    // Refresh re-renders templates only; it never calls a model.
    private readonly DraftingService _templates = new(new StubModelProvider());

    private readonly List<FieldViewModel> _fields = [];
    private readonly string? _autosaveFile;
    private DraftDocument _draft;
    private DraftDocument? _generatedSnapshot;

    public MainWindowViewModel(
        DocumentTypeDefinition definition,
        IReadOnlyList<ProviderOption> providers,
        IUserFiles files,
        EvaluationStore evaluations,
        Loc loc,
        string defaultProvider = "ollama",
        Func<DateOnly>? today = null,
        string? autosaveFile = null,
        DzLegal.Law.LegalKnowledge? knowledge = null,
        IExternalLinks? links = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(providers);
        UseKnowledge(knowledge, links);
        _definition = definition;
        _files = files;
        _evaluations = evaluations;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
        _autosaveFile = autosaveFile;
        L = loc;
        AnalysisActivity = new ModelActivityViewModel(loc);
        GenerationActivity = new ModelActivityViewModel(loc, showLiveText: false);
        AssistantActivity = new ModelActivityViewModel(loc, showLiveText: false);

        Providers = providers;
        foreach (var p in providers)
        {
            p.Localize(loc);
        }

        SelectedProvider = providers.FirstOrDefault(p => p.Key == defaultProvider) ?? (providers.Count > 0 ? providers[0] : null);

        var defaults = CaseInputs.CreateDefaults(definition, _today());
        foreach (var field in definition.Fields)
        {
            _fields.Add(new FieldViewModel(field, defaults[field.Id], loc, OnInputChanged));
        }

        FieldGroups = definition.Fields
            .GroupBy(f => f.Group)
            .Select((g, i) => new FieldGroupViewModel(
                definition.FieldGroups.FirstOrDefault(d => d.Id == g.Key),
                _fields.Where(f => f.Definition.Group == g.Key).ToList(),
                loc,
                i + 1))
            .ToList();
        SelectedStep = FieldGroups[0];
        HasRecoverableDraft = _autosaveFile is not null && File.Exists(_autosaveFile);

        Verdicts = Enum.GetValues<DraftVerdict>().Select(v => new VerdictOption(v, loc)).ToList();
        SelectedVerdict = Verdicts[1];

        _draft = new DraftDocument { DocumentTypeId = definition.Id, DefinitionVersion = definition.Version };
        StatusMessage = loc["Ready"];
        OnInputChanged();
    }

    public Loc L { get; }

    /// <summary>What the model is doing during the grounds analysis.</summary>
    public ModelActivityViewModel AnalysisActivity { get; }

    /// <summary>What the model is doing while it drafts the requête (or redrafts one section).</summary>
    public ModelActivityViewModel GenerationActivity { get; }

    /// <summary>What the model is doing while the section assistant prepares a proposal.</summary>
    public ModelActivityViewModel AssistantActivity { get; }

    public string DocumentTypeName => _definition.Name.Get(L.Language);

    /// <summary>The interface reads right to left (Arabic). The document itself is always Arabic and right to left.</summary>
    public bool IsRightToLeft => L.IsRightToLeft;

    public IReadOnlyList<FieldGroupViewModel> FieldGroups { get; }

    public ObservableCollection<SectionViewModel> Sections { get; } = [];

    /// <summary>Every finding, most urgent first.</summary>
    public ObservableCollection<FindingViewModel> Findings { get; } = [];

    /// <summary>Missing elements and warnings: what the compliance pane shows first.</summary>
    public ObservableCollection<FindingViewModel> IssueFindings { get; } = [];

    /// <summary>Elements present: folded away in the compliance pane.</summary>
    public ObservableCollection<FindingViewModel> PresentFindings { get; } = [];

    /// <summary>
    /// Legal points to look at (wrong basis, interest on a private loan, damages without formal notice…), each with the
    /// article it comes from. Shown beside the formal check, which only says whether each element is present.
    /// </summary>
    public ObservableCollection<string> SubstanceIssues { get; } = [];

    [ObservableProperty]
    public partial bool HasSubstanceIssues { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepPosition), nameof(IsGroundsStep), nameof(IsRequestsStep), nameof(IsDocumentsStep))]
    public partial FieldGroupViewModel? SelectedStep { get; set; }

    public string StepPosition => SelectedStep is null ? string.Empty : L.Format("StepPosition", SelectedStep.Number, FieldGroups.Count);

    [ObservableProperty]
    public partial int PresentCount { get; set; }

    [ObservableProperty]
    public partial int ApplicableCount { get; set; }

    [ObservableProperty]
    public partial string ScoreText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PresentHeader { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasIssues { get; set; }

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    /// <summary>An autosaved draft from an earlier session can be restored.</summary>
    [ObservableProperty]
    public partial bool HasRecoverableDraft { get; set; }

    [ObservableProperty]
    public partial bool IsEvaluationOpen { get; set; }

    public IReadOnlyList<ProviderOption> Providers { get; }

    public IReadOnlyList<VerdictOption> Verdicts { get; }

    /// <summary>The latest compliance report for the current draft.</summary>
    public ComplianceReport? Report { get; private set; }

    /// <summary>The current draft, as the Form Engine sees it.</summary>
    public DraftDocument Draft => _draft;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOfflineEngine))]
    [NotifyPropertyChangedFor(nameof(CanProposeRequestsWording))]
    public partial ProviderOption? SelectedProvider { get; set; }

    /// <summary>False while an engine that sends the case notes to another server is selected.</summary>
    public bool IsOfflineEngine => SelectedProvider?.SendsCaseData != true;

    /// <summary>Added to the window title by a testing build, so its screenshots are never taken for the product.</summary>
    public string BuildLabel { get; init; } = string.Empty;

    public string WindowTitle => BuildLabel.Length == 0 ? L["AppTitle"] : $"{L["AppTitle"]} ({BuildLabel})";

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WarningSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAdmissible { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsError { get; set; }

    /// <summary>
    /// The engine that actually wrote the last AI answer («أجاب: gemma-4-26b-a4b-it (Google AI Studio)»). A chain of
    /// engines (the web demo) may answer with another model than the one selected when the first is busy.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAnsweredBy))]
    public partial string AnsweredBy { get; set; } = string.Empty;

    public bool HasAnsweredBy => AnsweredBy.Length > 0;

    private void NoteAnswer(IModelProvider provider) => AnsweredBy = L.Format("AnsweredBy", provider.Name);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEvaluationCommand))]
    public partial bool HasGeneratedDraft { get; set; }

    [ObservableProperty]
    public partial VerdictOption SelectedVerdict { get; set; }

    /// <summary>0 = edit the sections, 1 = preview the printed document.</summary>
    [ObservableProperty]
    public partial int SelectedDraftTab { get; set; }

    /// <summary>Print «مسودة» diagonally on preview and exported PDF pages.</summary>
    [ObservableProperty]
    public partial bool DraftWatermark { get; set; } = true;

    /// <summary>PNG images of the PDF pages, exactly as they will print.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<byte[]> PreviewPages { get; set; } = [];

    [ObservableProperty]
    public partial bool IsRenderingPreview { get; set; }

    private CancellationTokenSource? _previewCts;

    partial void OnSelectedDraftTabChanged(int value) => SchedulePreview(TimeSpan.Zero);

    partial void OnDraftWatermarkChanged(bool value) => SchedulePreview(TimeSpan.Zero);

    /// <summary>Renders the preview now (used by tests and when the tab opens).</summary>
    public async Task RefreshPreviewAsync(CancellationToken cancellationToken = default)
    {
        var model = DocumentLayoutBuilder.Build(_definition, _draft.DeepCopy());
        var options = new DocumentRenderOptions { DraftWatermark = DraftWatermark };
        IsRenderingPreview = true;
        try
        {
            PreviewPages = await Task.Run(() => PdfRenderer.RenderPageImages(model, options), cancellationToken);
        }
        finally
        {
            IsRenderingPreview = false;
        }
    }

    /// <summary>Re-renders the preview shortly after the last change, only while the preview tab is open.</summary>
    private async void SchedulePreview(TimeSpan delay)
    {
        if (SelectedDraftTab != 1)
        {
            return;
        }

        _previewCts?.Cancel();
        _previewCts?.Dispose();
        var cts = _previewCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(delay, cts.Token);
            await RefreshPreviewAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer change
        }
        catch (Exception ex) when (IsModelOrRenderFailure(ex))
        {
            // A failed preview must never take the window (or a web visitor's session) down with it.
            SetStatus(ErrorText(ex), isError: true);
        }
    }

    /// <summary>
    /// Failures an AI command or a preview reports to the user instead of crashing: the engine's own errors, a lost
    /// connection, a malformed answer, a timeout, or a document the renderer refuses.
    /// </summary>
    internal static bool IsModelOrRenderFailure(Exception ex) =>
        ex is ModelProviderException or InvalidOperationException or ArgumentException or HttpRequestException
            or IOException or System.Text.Json.JsonException or TimeoutException or FormatException;

    /// <summary>
    /// The message for a failed AI command. A busy online service (rate limit) gets a plain, translated explanation of
    /// what to do instead of the service's own technical text.
    /// </summary>
    internal string ErrorText(Exception ex) =>
        ex is ModelProviderException { IsRateLimited: true } ? L["EngineRateLimited"] : L.Format("Error", ex.Message);

    public void Dispose()
    {
        // A closed window or browser tab stops its running AI work: no tokens spent and no queue slot held for nobody.
        GenerateCommand.Cancel();
        AnalyseGroundsCommand.Cancel();
        SendInstructionCommand.Cancel();
        _previewCts?.Cancel();
        _previewCts?.Dispose();
        AnalysisActivity.Dispose();
        GenerationActivity.Dispose();
        AssistantActivity.Dispose();
    }

    /// <summary>What must be done before the document leaves the app: AI sections to review, placeholders to fill.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExportBlockers))]
    public partial string ExportBlockers { get; set; } = string.Empty;

    public bool HasExportBlockers => ExportBlockers.Length > 0;

    /// <summary>The blockers panel is shown after an export attempt was refused, until nothing blocks any more.</summary>
    [ObservableProperty]
    public partial bool IsExportBlockedShown { get; set; }

    private const string StubProviderKey = "stub";

    private void UpdateExportBlockers()
    {
        var lines = new List<string>();
        foreach (var section in Sections)
        {
            if (section.NeedsReview)
            {
                lines.Add("• " + L.Format("BlockerUnreviewed", section.Title));
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(section.Text, @"\(\s*(\.\.\.|…)\s*\)"))
            {
                lines.Add("• " + L.Format("BlockerPlaceholder", section.Title));
            }
        }

        // Problems in the notes themselves (an instruction to the AI, a request not in Arabic, a prison sentence) are
        // fixed in the notes: no review tick lifts them.
        lines.AddRange(CurrentSubstanceIssues().Where(i => SubstanceChecks.BlocksExport(i.Kind)).Select(i => "• " + DescribeSubstance(i)));
        ExportBlockers = string.Join("\n", lines);
        if (!HasExportBlockers)
        {
            IsExportBlockedShown = false;
        }
    }

    /// <summary>False, with the reasons shown, while something must still be done before the document leaves the app.</summary>
    private bool MayExport()
    {
        UpdateExportBlockers();
        if (!HasExportBlockers)
        {
            return true;
        }

        IsExportBlockedShown = true;
        SetStatus(L["ExportBlocked"], isError: true);
        return false;
    }

    private bool HasAiText => _draft.Sections.Any(s => s.AiWritten);

    private void OnSectionReviewChanged(SectionViewModel section)
    {
        if (_draft.FindSection(section.Id) is { } draftSection)
        {
            draftSection.Reviewed = section.IsReviewed;
            if (section.IsReviewed)
            {
                draftSection.DraftingNotes.Clear(); // the lawyer has read the AI notes and takes the text on
            }
        }

        UpdateReviewHints();
        UpdateExportBlockers();
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (!MayExport())
        {
            return;
        }

        var model = DocumentLayoutBuilder.Build(_definition, _draft.DeepCopy());
        var bytes = await Task.Run(() => PdfRenderer.RenderPdf(model, new DocumentRenderOptions { DraftWatermark = DraftWatermark, AiAssisted = HasAiText }));
        await ExportAsync(bytes, ".pdf", SaveKind.Pdf);
    }

    [RelayCommand]
    private async Task ExportWordAsync()
    {
        if (!MayExport())
        {
            return;
        }

        var model = DocumentLayoutBuilder.Build(_definition, _draft.DeepCopy());
        var bytes = await Task.Run(() => DocxRenderer.Render(model, new DocumentRenderOptions { DraftWatermark = DraftWatermark, AiAssisted = HasAiText }));
        await ExportAsync(bytes, ".docx", SaveKind.Word);
    }

    private async Task ExportAsync(byte[] bytes, string extension, SaveKind kind)
    {
        var target = await _files.SaveAsync(SuggestedFileName() + extension, kind);
        if (target is not { } t)
        {
            return;
        }

        await using (t.Stream)
        {
            await t.Stream.WriteAsync(bytes);
        }

        SetStatus(L.Format("Exported", t.Name));
        IsEvaluationOpen = HasGeneratedDraft; // invite a quick verdict once a real draft is out
    }

    [ObservableProperty]
    public partial string MinutesSaved { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EvaluationComments { get; set; } = string.Empty;

    public FieldViewModel Field(string id) => _fields.Single(f => f.Id == id);

    /// <summary>The draft as it should be saved: current inputs and the articles the lawyer ticked.</summary>
    /// <summary>The whole case as saved to a file: inputs, sections, selected articles and the analysis.</summary>
    public DraftDocument SnapshotDraft() => CurrentDraft();

    /// <summary>Puts a saved case back, as opening a draft file does (the web demo restores from the browser's storage).</summary>
    public void RestoreDraft(DraftDocument draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        LoadDraft(draft);
        SetStatus(L["Restored"]);
    }

    private DraftDocument CurrentDraft() => _draft with { Inputs = CurrentInputs(), SelectedArticles = [.. _selectedArticles], GroundsAnalysis = _analysis };

    public Dictionary<string, string> CurrentInputs() =>
        _fields.ToDictionary(f => f.Id, f => f.Value, StringComparer.Ordinal);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        if (SelectedProvider is null)
        {
            return;
        }

        IModelProvider? provider = null;
        try
        {
            provider = SelectedProvider.Create();
            var drafting = new DraftingService(provider);
            var keep = EditedNarrative();
            var inputs = DraftingInputs();
            var narrative = _definition.Sections.Where(x => x.Kind == SectionKind.Narrative).ToList();
            GenerationActivity.Start(narrative.Select(x => x.Title.Get(L.Language)));

            // Synchronous progress: Progress<T> would post late updates over the final status.
            var progress = new InlineProgress<SectionDefinition>(s =>
            {
                SetStatus(L.Format("Generating", s.Title.Get(L.Language)));
                var step = narrative.IndexOf(s);
                if (step >= 0)
                {
                    var callsModel = !keep.ContainsKey(s.Id) && inputs.GetValueOrDefault(s.SourceField ?? string.Empty, string.Empty).Trim().Length > 0;
                    GenerationActivity.BeginStep(step, waitsForModel: callsModel);
                    MarkWriting(callsModel ? s.Id : null);
                }
            });
            var draft = await drafting.DraftAsync(
                _definition,
                inputs,
                new DraftOptions
                {
                    Progress = progress,
                    KeepNarrative = keep,
                    OnPartial = (id, partial) =>
                    {
                        Sections.FirstOrDefault(x => x.Id == id)?.SetFromModel(partial);
                        GenerationActivity.OnText(partial);
                    },
                    LegalTexts = SelectedLegalTexts(),
                },
                cancellationToken);
            GenerationActivity.Complete();
            foreach (var manual in _draft.Sections.Where(x => x.Overridden))
            {
                if (draft.FindSection(manual.Id) is { } target)
                {
                    target.Text = manual.Text;
                    target.Overridden = true;
                }
            }

            var byModel = SelectedProvider.Key != StubProviderKey;
            foreach (var section in draft.Sections.Where(x => _definition.FindSection(x.Id)?.Kind == SectionKind.Narrative && !keep.ContainsKey(x.Id)))
            {
                _modelText[section.Id] = section.Text;
                section.AiWritten = byModel && section.Text.Trim().Length > 0;
                section.Reviewed = false;
            }

            foreach (var kept in keep.Keys)
            {
                if (draft.FindSection(kept) is { } target && _draft.FindSection(kept) is { } before)
                {
                    target.AiWritten = before.AiWritten;
                    target.Reviewed = before.Reviewed;
                }
            }

            _draft = draft;
            _generatedSnapshot = draft.DeepCopy();
            HasGeneratedDraft = true;
            SyncSections(overwriteNarrative: true);
            Recheck();
            if (keep.Count > 0)
            {
                SetStatus(L.Format("KeptEdited", provider.Name, string.Join("، ", keep.Keys.Select(k => _definition.FindSection(k)!.Title.Get(L.Language)))));
                return;
            }

            SetStatus(L.Format("Generated", provider.Name));
            NoteAnswer(provider);
        }
        catch (OperationCanceledException)
        {
            SetStatus(L["Cancelled"]);
        }
        catch (Exception ex) when (IsModelOrRenderFailure(ex))
        {
            SetStatus(ErrorText(ex), isError: true);
        }
        finally
        {
            GenerationActivity.Stop();
            MarkWriting(null);
            (provider as IDisposable)?.Dispose();
        }
    }

    /// <summary>Shows the "being written" marker on one section (or on none).</summary>
    private void MarkWriting(string? sectionId)
    {
        foreach (var section in Sections)
        {
            section.IsWriting = section.Id == sectionId;
        }
    }

    [RelayCommand]
    private void SelectStep(FieldGroupViewModel step) => SelectedStep = step;

    [RelayCommand]
    private void NextStep() => MoveStep(+1);

    [RelayCommand]
    private void PreviousStep() => MoveStep(-1);

    private void MoveStep(int delta)
    {
        var index = SelectedStep is null ? 0 : FieldGroups.ToList().IndexOf(SelectedStep);
        SelectedStep = FieldGroups[Math.Clamp(index + delta, 0, FieldGroups.Count - 1)];
    }

    /// <summary>Opens the form step where the lawyer can fix a finding.</summary>
    [RelayCommand]
    private void GoToFinding(FindingViewModel finding)
    {
        if (FieldGroups.FirstOrDefault(g => g.Id == finding?.TargetGroupId) is { } step)
        {
            SelectedStep = step;
        }
    }

    [RelayCommand]
    private void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    /// <summary>Writes the current work to the local autosave file (called on a timer and when closing).</summary>
    public void AutosaveNow()
    {
        if (_autosaveFile is null)
        {
            return;
        }

        // Synchronous on purpose: blocking the UI thread on async I/O deadlocks once the draft outgrows the buffers.
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_autosaveFile)!);
            DraftFile.Save(CurrentDraft(), _autosaveFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus(L.Format("AutosaveFailed", ex.Message), isError: true);
        }
    }

    [RelayCommand]
    private async Task RestoreLastSessionAsync()
    {
        if (_autosaveFile is null || !File.Exists(_autosaveFile))
        {
            HasRecoverableDraft = false;
            return;
        }

        await using (var stream = File.OpenRead(_autosaveFile))
        {
            LoadDraft(await DraftFile.LoadAsync(stream));
        }

        HasRecoverableDraft = false;
        SetStatus(L["Restored"]);
    }

    [RelayCommand]
    private void DismissRecovery()
    {
        HasRecoverableDraft = false;
        if (_autosaveFile is not null && File.Exists(_autosaveFile))
        {
            File.Delete(_autosaveFile);
        }
    }

    /// <summary>Takes a generated section over for manual editing, or gives it back to the form.</summary>
    private void OnSectionOverride(SectionViewModel section, bool overridden)
    {
        if (_draft.FindSection(section.Id) is not { } draftSection)
        {
            return;
        }

        draftSection.Overridden = overridden;
        section.IsOverridden = overridden;
        if (!overridden)
        {
            _draft = _templates.Refresh(_definition, _draft, CurrentInputs());
            SyncSections(overwriteNarrative: false);
        }

        Recheck();
    }

    [RelayCommand]
    private void New()
    {
        LoadInputs(CaseInputs.CreateDefaults(_definition, _today()));
        _draft = new DraftDocument { DocumentTypeId = _definition.Id, DefinitionVersion = _definition.Version };
        _generatedSnapshot = null;
        _modelText.Clear();
        HasGeneratedDraft = false;
        LoadGroundsState(_draft);
        OnInputChanged();
        SyncSections(overwriteNarrative: true);
        SetStatus(L["Ready"]);
    }

    /// <summary>
    /// Starts a new requête from case details only (the web demo's sample cases): no draft, no analysis yet, so the
    /// lawyer, or a judge, runs every step.
    /// </summary>
    public void OpenCase(IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        New();
        LoadDraft(new DraftDocument
        {
            DocumentTypeId = _definition.Id,
            DefinitionVersion = _definition.Version,
            Inputs = new Dictionary<string, string>(inputs, StringComparer.Ordinal),
        });
        SelectedStep = FieldGroups[0];
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var target = await _files.SaveAsync(SuggestedFileName() + DraftFile.Extension, SaveKind.Draft);
        if (target is not { } t)
        {
            return;
        }

        await using (t.Stream)
        {
            await DraftFile.SaveAsync(CurrentDraft(), t.Stream);
        }

        SetStatus(L.Format("Saved", t.Name));
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var source = await _files.OpenAsync();
        if (source is not { } s)
        {
            return;
        }

        DraftDocument loaded;
        await using (s.Stream)
        {
            loaded = await DraftFile.LoadAsync(s.Stream);
        }

        if (loaded.DocumentTypeId != _definition.Id)
        {
            SetStatus(L["WrongDocumentType"], isError: true);
            return;
        }

        LoadDraft(loaded);
        SetStatus(L.Format("Opened", s.Name));
    }

    private void LoadDraft(DraftDocument loaded)
    {
        var inputs = CaseInputs.CreateDefaults(_definition, _today());
        foreach (var (key, value) in loaded.Inputs)
        {
            inputs[key] = value;
        }

        LoadInputs(inputs);
        _draft = _templates.Refresh(_definition, loaded, inputs);
        LoadGroundsState(loaded);
        _generatedSnapshot = null;
        HasGeneratedDraft = false;
        UpdateVisibility();
        SyncSections(overwriteNarrative: true);
        Recheck();
    }

    [RelayCommand]
    private async Task CopyTextAsync()
    {
        if (!MayExport())
        {
            return;
        }

        await _files.CopyTextAsync(DocumentText.Compose(_definition, _draft));
        SetStatus(L["Copied"]);
    }

    [RelayCommand(CanExecute = nameof(HasGeneratedDraft))]
    private async Task SaveEvaluationAsync()
    {
        if (_generatedSnapshot is null)
        {
            SetStatus(L["EvaluationNeedsDraft"], isError: true);
            return;
        }

        var record = new EvaluationRecord
        {
            DocumentTypeId = _definition.Id,
            DefinitionVersion = _definition.Version,
            GeneratedBy = _generatedSnapshot.GeneratedBy,
            GeneratedDraft = _generatedSnapshot,
            FinalDraft = _draft.DeepCopy(),
            Verdict = SelectedVerdict.Value,
            MinutesSaved = int.TryParse(MinutesSaved, out var minutes) ? minutes : null,
            Comments = string.IsNullOrWhiteSpace(EvaluationComments) ? null : EvaluationComments.Trim(),
        };
        var path = await _evaluations.SaveAsync(record);
        SetStatus(L.Format("EvaluationSaved", path));
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        L.SetLanguage(L.Language == "ar" ? "en" : "ar");
        foreach (var g in FieldGroups)
        {
            g.Relocalize();
        }

        foreach (var s in Sections)
        {
            s.Relocalize();
        }

        foreach (var p in Providers)
        {
            p.Localize(L);
        }

        foreach (var v in Verdicts)
        {
            v.Relocalize();
        }

        OnPropertyChanged(nameof(IsRightToLeft));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTypeName));
        OnPropertyChanged(nameof(StepPosition));
        OnPropertyChanged(nameof(Shortcuts));
        OnPropertyChanged(nameof(AssistantTitle));
        AnalysisActivity.Relocalize();
        GenerationActivity.Relocalize();
        AssistantActivity.Relocalize();
        _ = CheckModelStatusAsync();
        ShowAnalysis(_analysis);
        Recheck();
        SetStatus(L["Ready"]);
    }

    private void OnInputChanged()
    {
        UpdateVisibility();
        _draft = _templates.Refresh(_definition, _draft, CurrentInputs());
        SyncSections(overwriteNarrative: false);
        UpdateAnalysisStaleness();
        Recheck();
    }

    private void OnSectionEdited(SectionViewModel section)
    {
        if (_draft.FindSection(section.Id) is { } draftSection)
        {
            // The AI notes and the review state stay: editing an AI text does not make it reviewed.
            draftSection.Text = section.Text;
        }

        Recheck();
    }

    private void UpdateVisibility()
    {
        var inputs = CurrentInputs();
        foreach (var f in _fields)
        {
            f.IsVisible = Condition.All(f.Definition.VisibleWhen, inputs);
        }
    }

    private void LoadInputs(IReadOnlyDictionary<string, string> inputs)
    {
        foreach (var f in _fields)
        {
            f.Silent = true;
            f.Value = inputs.GetValueOrDefault(f.Id, string.Empty);
            f.Silent = false;
        }
    }

    private SectionViewModel CreateSection(DraftSection section)
    {
        var vm = new SectionViewModel(_definition.FindSection(section.Id)!, section.Text, L, OnSectionEdited, OnSectionOverride);
        WireSection(vm);
        return vm;
    }

    /// <summary>Mirrors the draft's sections in the editor, rebuilding only when the section list changed.</summary>
    private void SyncSections(bool overwriteNarrative)
    {
        var ids = _draft.Sections.Select(s => s.Id).ToList();
        if (!ids.SequenceEqual(Sections.Select(s => s.Id)))
        {
            var existing = Sections.ToDictionary(s => s.Id);
            Sections.Clear();
            foreach (var section in _draft.Sections)
            {
                Sections.Add(existing.TryGetValue(section.Id, out var vm)
                    ? vm
                    : CreateSection(section));
            }
        }

        foreach (var vm in Sections)
        {
            var section = _draft.FindSection(vm.Id);
            vm.IsOverridden = section?.Overridden ?? false;
            vm.SetReviewState(section?.AiWritten ?? false, section?.Reviewed ?? false);
            if (vm.IsLocked || overwriteNarrative)
            {
                vm.SetFromModel(section?.Text ?? string.Empty);
            }
        }
    }

    /// <summary>Re-runs the substance checks on the form, the ticked articles and the drafted grounds and requests.</summary>
    private void UpdateSubstanceIssues()
    {
        SubstanceIssues.Clear();
        foreach (var issue in CurrentSubstanceIssues())
        {
            SubstanceIssues.Add(DescribeSubstance(issue));
        }

        HasSubstanceIssues = SubstanceIssues.Count > 0;
    }

    private IReadOnlyList<SubstanceIssue> CurrentSubstanceIssues()
    {
        var claims = (_analysis?.ClaimIds ?? []).Select(id => _knowledge.Claims.FirstOrDefault(c => c.Id == id)).OfType<DzLegal.Law.ClaimType>().ToList();
        var drafted = (_draft.FindSection("grounds")?.Text ?? string.Empty) + "\n" + (_draft.FindSection("requests")?.Text ?? string.Empty);
        return SubstanceChecks.Check(CurrentInputs(), claims, _selectedArticles, drafted);
    }

    private string DescribeSubstance(SubstanceIssue issue) =>
        issue.Article is { } article ? L.Format("Substance_" + issue.Kind, article.Number) : L["Substance_" + issue.Kind];

    private void Recheck()
    {
        Report = ComplianceChecker.Check(_definition, _draft);
        var ordered = Report.Findings
            .OrderBy(f => f.Status switch
            {
                ElementStatus.Missing when f.Element.Severity == Severity.Inadmissible => 0,
                ElementStatus.Missing => 1,
                ElementStatus.Present => 2,
                _ => 3,
            })
            .ToList();

        Findings.Clear();
        IssueFindings.Clear();
        PresentFindings.Clear();
        foreach (var f in ordered)
        {
            var vm = new FindingViewModel(f, _definition, L);
            Findings.Add(vm);
            (vm.IsMissing ? IssueFindings : vm.IsPresent ? PresentFindings : null)?.Add(vm);
        }

        // The score counts mandatory elements only: recommendations and practice points are listed but never inflate it.
        var mandatory = Report.Findings.Where(f => f.Element.Severity == Severity.Inadmissible).ToList();
        PresentCount = mandatory.Count(f => f.Status == ElementStatus.Present);
        ApplicableCount = mandatory.Count(f => f.Status != ElementStatus.NotApplicable);
        ScoreText = L.Format("Score", PresentCount, ApplicableCount);
        PresentHeader = L.Format("PresentHeader", PresentCount);
        HasIssues = IssueFindings.Count > 0;
        UpdateStepsAndFields();
        UpdateSubstanceIssues();
        UpdateExportBlockers();

        UpdateReviewHints();
        SchedulePreview(TimeSpan.FromMilliseconds(400));

        var mandatoryMissing = Report.Missing.Count(f => f.Element.Severity == Severity.Inadmissible);
        var warnings = Report.Missing.Count(f => f.Element.Severity == Severity.Warning);
        IsAdmissible = Report.IsAdmissibleInForm;
        Summary = mandatoryMissing == 0 ? L["SummaryOk"] : L.Format("SummaryMissing", mandatoryMissing);
        WarningSummary = warnings == 0 ? string.Empty : L.Format("SummaryWarnings", warnings);
        OnPropertyChanged(nameof(Report));
    }

    /// <summary>Marks missing fields and computes each step's status from the compliance report.</summary>
    private void UpdateStepsAndFields()
    {
        foreach (var f in _fields)
        {
            f.IsMissing = false;
            f.MissingNote = string.Empty;
        }

        var missingGroups = new HashSet<string>();
        var warningGroups = new HashSet<string>();
        foreach (var finding in Report!.Missing)
        {
            var mandatory = finding.Element.Severity == Severity.Inadmissible;
            foreach (var id in finding.MissingParts.SelectMany(p => p.Split('|', '<')))
            {
                var field = _fields.FirstOrDefault(f => f.Id == id);
                var group = field?.Definition.Group ?? _definition.FindSection(id)?.FormGroup;
                if (group is not null)
                {
                    (mandatory ? missingGroups : warningGroups).Add(group);
                }

                if (field is not null && !field.IsMissing)
                {
                    field.IsMissing = true;
                    field.MissingNote = L.Format(mandatory ? "FieldRequired" : "FieldRecommended", finding.Element.Article);
                }
            }
        }

        foreach (var step in FieldGroups)
        {
            step.Status = missingGroups.Contains(step.Id) ? StepStatus.Missing
                : warningGroups.Contains(step.Id) ? StepStatus.Warning
                : step.Fields.Any(f => f.IsVisible && f.Definition.Kind is FieldKind.Text or FieldKind.MultilineText && !string.IsNullOrWhiteSpace(f.Value))
                    ? StepStatus.Done
                    : StepStatus.Empty;
        }
    }

    /// <summary>
    /// Review hints under each section: the definition's own hints (e.g. gender not chosen), what happened while
    /// the model drafted, and deterministic checks of narrative text. They disappear once the cause is fixed.
    /// </summary>
    private void UpdateReviewHints()
    {
        var inputs = DraftingInputs();
        foreach (var vm in Sections)
        {
            var lines = vm.IsOverridden
                ? [L["OverriddenHint"]]
                : vm.Definition.Hints
                    .Where(h => Condition.All(h.When, inputs))
                    .Select(h => h.Message.Get(L.Language))
                    .ToList();

            if (vm.IsEditable)
            {
                foreach (var code in _draft.FindSection(vm.Id)?.DraftingNotes ?? [])
                {
                    lines.Add(code switch
                    {
                        DraftingNoteCodes.Retried => L["NoteRetried"],
                        DraftingNoteCodes.FellBackToNotes => L["NoteFellBack"],
                        DraftingNoteCodes.RemovedWrongLaw => L["NoteRemovedWrongLaw"],
                        _ => L["NoteRemovedCopiedExample"],
                    });
                }

                foreach (var removed in _draft.FindSection(vm.Id)?.RemovedText ?? [])
                {
                    lines.Add(L.Format("NoteRemovedParagraph", removed.Length > 200 ? removed[..200] + "…" : removed));
                }

                var notes = inputs.GetValueOrDefault(vm.Definition.SourceField ?? string.Empty, string.Empty);
                lines.AddRange(NarrativeChecks.Check(vm.Text, notes, inputs, _definition.GroundedTerms, SelectedLegalTexts()).Select(DescribeIssue));
            }

            vm.ReviewText = string.Join("\n", lines.Select(l => "⚠ " + l));
        }
    }

    private string SuggestedFileName()
    {
        var inputs = CurrentInputs();
        var who = inputs.GetValueOrDefault("plaintiff.lastName") is { Length: > 0 } name
            ? name
            : inputs.GetValueOrDefault("plaintiff.entityName") ?? string.Empty;
        var stem = string.Join('-', new[] { _definition.Id, who }.Where(s => s.Length > 0));
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            stem = stem.Replace(c, '_');
        }

        return stem;
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsError = isError;
    }
}

internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}

public sealed class VerdictOption(DraftVerdict value, Loc loc) : ObservableObject
{
    public DraftVerdict Value { get; } = value;

    public string DisplayName => loc["Verdict" + Value];

    internal void Relocalize() => OnPropertyChanged(nameof(DisplayName));
}
