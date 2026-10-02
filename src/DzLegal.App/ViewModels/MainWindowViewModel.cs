using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.App.Services;
using DzLegal.Documents;
using DzLegal.Domain;
using DzLegal.Drafting;
using DzLegal.FormEngine;
using DzLegal.ModelProviders.Stub;
using DzLegal.Storage;

namespace DzLegal.App.ViewModels;

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
        string? autosaveFile = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(providers);
        _definition = definition;
        _files = files;
        _evaluations = evaluations;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
        _autosaveFile = autosaveFile;
        L = loc;

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

    public string DocumentTypeName => _definition.Name.Get(L.Language);

    public FlowDirection FlowDirection => L.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public IReadOnlyList<FieldGroupViewModel> FieldGroups { get; }

    public ObservableCollection<SectionViewModel> Sections { get; } = [];

    /// <summary>Every finding, most urgent first.</summary>
    public ObservableCollection<FindingViewModel> Findings { get; } = [];

    /// <summary>Missing elements and warnings: what the compliance pane shows first.</summary>
    public ObservableCollection<FindingViewModel> IssueFindings { get; } = [];

    /// <summary>Elements present: folded away in the compliance pane.</summary>
    public ObservableCollection<FindingViewModel> PresentFindings { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepPosition))]
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
    [NotifyPropertyChangedFor(nameof(RequestedTheme))]
    public partial bool IsDarkTheme { get; set; }

    public ThemeVariant RequestedTheme => IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

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
    public partial ProviderOption? SelectedProvider { get; set; }

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
    }

    public void Dispose()
    {
        _previewCts?.Cancel();
        _previewCts?.Dispose();
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        var model = DocumentLayoutBuilder.Build(_definition, _draft.DeepCopy());
        var bytes = await Task.Run(() => PdfRenderer.RenderPdf(model, new DocumentRenderOptions { DraftWatermark = DraftWatermark }));
        await ExportAsync(bytes, ".pdf", SaveKind.Pdf);
    }

    [RelayCommand]
    private async Task ExportWordAsync()
    {
        var model = DocumentLayoutBuilder.Build(_definition, _draft.DeepCopy());
        var bytes = await Task.Run(() => DocxRenderer.Render(model));
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
            // Synchronous progress: Progress<T> would post late updates over the final status.
            var progress = new InlineProgress<SectionDefinition>(s => SetStatus(L.Format("Generating", s.Title.Get(L.Language))));
            var keep = EditedNarrative();
            var draft = await drafting.DraftAsync(
                _definition,
                CurrentInputs(),
                new DraftOptions
                {
                    Progress = progress,
                    KeepNarrative = keep,
                    OnPartial = (id, partial) => Sections.FirstOrDefault(x => x.Id == id)?.SetFromModel(partial),
                },
                cancellationToken);
            foreach (var manual in _draft.Sections.Where(x => x.Overridden))
            {
                if (draft.FindSection(manual.Id) is { } target)
                {
                    target.Text = manual.Text;
                    target.Overridden = true;
                }
            }

            foreach (var section in draft.Sections.Where(x => _definition.FindSection(x.Id)?.Kind == SectionKind.Narrative && !keep.ContainsKey(x.Id)))
            {
                _modelText[section.Id] = section.Text;
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
        }
        catch (OperationCanceledException)
        {
            SetStatus(L["Cancelled"]);
        }
        catch (Exception ex) when (ex is ModelProviderException or ArgumentException or InvalidOperationException)
        {
            SetStatus(L.Format("Error", ex.Message), isError: true);
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
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

        Directory.CreateDirectory(Path.GetDirectoryName(_autosaveFile)!);
        using var stream = File.Create(_autosaveFile);
        DraftFile.SaveAsync(_draft with { Inputs = CurrentInputs() }, stream).GetAwaiter().GetResult();
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
        OnInputChanged();
        SyncSections(overwriteNarrative: true);
        SetStatus(L["Ready"]);
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
            await DraftFile.SaveAsync(_draft, t.Stream);
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
        _generatedSnapshot = null;
        HasGeneratedDraft = false;
        UpdateVisibility();
        SyncSections(overwriteNarrative: true);
        Recheck();
    }

    [RelayCommand]
    private async Task CopyTextAsync()
    {
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

        OnPropertyChanged(nameof(FlowDirection));
        OnPropertyChanged(nameof(DocumentTypeName));
        OnPropertyChanged(nameof(StepPosition));
        OnPropertyChanged(nameof(Shortcuts));
        OnPropertyChanged(nameof(AssistantTitle));
        _ = CheckModelStatusAsync();
        Recheck();
        SetStatus(L["Ready"]);
    }

    private void OnInputChanged()
    {
        UpdateVisibility();
        _draft = _templates.Refresh(_definition, _draft, CurrentInputs());
        SyncSections(overwriteNarrative: false);
        Recheck();
    }

    private void OnSectionEdited(SectionViewModel section)
    {
        if (_draft.FindSection(section.Id) is { } draftSection)
        {
            draftSection.Text = section.Text;
            draftSection.DraftingNotes.Clear(); // the lawyer's own text now
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
            if (vm.IsLocked || overwriteNarrative)
            {
                vm.SetFromModel(section?.Text ?? string.Empty);
            }
        }
    }

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

        PresentCount = Report.Findings.Count(f => f.Status == ElementStatus.Present);
        ApplicableCount = Report.Findings.Count(f => f.Status != ElementStatus.NotApplicable);
        ScoreText = L.Format("Score", PresentCount, ApplicableCount);
        PresentHeader = L.Format("PresentHeader", PresentCount);
        HasIssues = IssueFindings.Count > 0;
        UpdateStepsAndFields();

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
        var inputs = CurrentInputs();
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
                        _ => L["NoteRemovedCopiedExample"],
                    });
                }

                var notes = inputs.GetValueOrDefault(vm.Definition.SourceField ?? string.Empty, string.Empty);
                lines.AddRange(NarrativeChecks.Check(vm.Text, notes, inputs, _definition.GroundedTerms).Select(i => L.Format(i.Kind switch
                {
                    NarrativeIssueKind.ForeignScript => "IssueForeignScript",
                    NarrativeIssueKind.NotArabic => "IssueNotArabic",
                    NarrativeIssueKind.UnknownNumber => "IssueUnknownNumber",
                    NarrativeIssueKind.UnexpectedLatinWord => "IssueUnexpectedLatinWord",
                    NarrativeIssueKind.MissingNumber => "IssueMissingNumber",
                    NarrativeIssueKind.UngroundedTerm => "IssueUngroundedTerm",
                    _ => throw new InvalidOperationException($"No message for {i.Kind}."),
                }, i.Detail)));
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
