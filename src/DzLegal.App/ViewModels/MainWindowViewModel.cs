using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.App.Services;
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
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly DocumentTypeDefinition _definition;
    private readonly IUserFiles _files;
    private readonly EvaluationStore _evaluations;
    private readonly Func<DateOnly> _today;

    // Refresh re-renders templates only; it never calls a model.
    private readonly DraftingService _templates = new(new StubModelProvider());

    private readonly List<FieldViewModel> _fields = [];
    private DraftDocument _draft;
    private DraftDocument? _generatedSnapshot;

    public MainWindowViewModel(
        DocumentTypeDefinition definition,
        IReadOnlyList<ProviderOption> providers,
        IUserFiles files,
        EvaluationStore evaluations,
        Loc loc,
        string defaultProvider = "ollama",
        Func<DateOnly>? today = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(providers);
        _definition = definition;
        _files = files;
        _evaluations = evaluations;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
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
            .Select(g => new FieldGroupViewModel(
                definition.FieldGroups.FirstOrDefault(d => d.Id == g.Key),
                _fields.Where(f => f.Definition.Group == g.Key).ToList(),
                loc))
            .ToList();

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

    public ObservableCollection<FindingViewModel> Findings { get; } = [];

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
            var draft = await drafting.DraftAsync(_definition, CurrentInputs(), progress, cancellationToken);

            _draft = draft;
            _generatedSnapshot = draft.DeepCopy();
            HasGeneratedDraft = true;
            SyncSections(overwriteNarrative: true);
            Recheck();
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
    private void New()
    {
        LoadInputs(CaseInputs.CreateDefaults(_definition, _today()));
        _draft = new DraftDocument { DocumentTypeId = _definition.Id, DefinitionVersion = _definition.Version };
        _generatedSnapshot = null;
        HasGeneratedDraft = false;
        OnInputChanged();
        SyncSections(overwriteNarrative: true);
        SetStatus(L["Ready"]);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var target = await _files.SaveAsync(SuggestedFileName());
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
        SetStatus(L.Format("Opened", s.Name));
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
            draftSection.DraftingNote = null; // the lawyer's own text now
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
                    : new SectionViewModel(_definition.FindSection(section.Id)!, section.Text, L, OnSectionEdited));
            }
        }

        foreach (var vm in Sections)
        {
            if (!vm.IsEditable || overwriteNarrative)
            {
                vm.SetFromModel(_draft.FindSection(vm.Id)?.Text ?? string.Empty);
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
        foreach (var f in ordered)
        {
            Findings.Add(new FindingViewModel(f, _definition, L));
        }

        UpdateReviewHints();

        var mandatoryMissing = Report.Missing.Count(f => f.Element.Severity == Severity.Inadmissible);
        var warnings = Report.Missing.Count(f => f.Element.Severity == Severity.Warning);
        IsAdmissible = Report.IsAdmissibleInForm;
        Summary = mandatoryMissing == 0 ? L["SummaryOk"] : L.Format("SummaryMissing", mandatoryMissing);
        WarningSummary = warnings == 0 ? string.Empty : L.Format("SummaryWarnings", warnings);
        OnPropertyChanged(nameof(Report));
    }

    /// <summary>Deterministic hints under each narrative section; they disappear once the text is fixed.</summary>
    private void UpdateReviewHints()
    {
        var inputs = CurrentInputs();
        foreach (var vm in Sections.Where(s => s.IsEditable))
        {
            var section = _draft.FindSection(vm.Id);
            var lines = new List<string>();
            if (section?.DraftingNote == DraftingNotes.Retried)
            {
                lines.Add(L["NoteRetried"]);
            }
            else if (section?.DraftingNote == DraftingNotes.FellBackToNotes)
            {
                lines.Add(L["NoteFellBack"]);
            }

            var notes = inputs.GetValueOrDefault(vm.Definition.SourceField ?? string.Empty, string.Empty);
            lines.AddRange(NarrativeChecks.Check(vm.Text, notes, inputs).Select(i => L.Format(i.Kind switch
            {
                NarrativeIssueKind.ForeignScript => "IssueForeignScript",
                NarrativeIssueKind.UnknownNumber => "IssueUnknownNumber",
                _ => "IssueMissingNumber",
            }, i.Detail)));
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

        return stem + DraftFile.Extension;
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
