using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Domain;
using DzLegal.Drafting;

namespace DzLegal.Presentation.ViewModels;

/// <summary>One line of the assistant thread: the lawyer's request or the assistant's answer status.</summary>
public sealed record AssistantMessage(string Text, bool FromLawyer);

public sealed record DiffTokenViewModel(string Text, bool IsAdded, bool IsRemoved);

public sealed record ShortcutViewModel(string Label, string Instruction);

/// <summary>
/// The section assistant and model-related behaviour: scoped revisions with a reviewed diff, per-section
/// redraft, streaming, and the model status indicator.
/// </summary>
public sealed partial class MainWindowViewModel
{
    /// <summary>Text each narrative section had when the model last wrote it; anything else is the lawyer's edit.</summary>
    private readonly Dictionary<string, string> _modelText = new(StringComparer.Ordinal);

    public ObservableCollection<AssistantMessage> AssistantThread { get; } = [];

    public ObservableCollection<DiffTokenViewModel> ProposalDiff { get; } = [];

    public IReadOnlyList<ShortcutViewModel> Shortcuts =>
        _definition.RevisionShortcuts.Select(s => new ShortcutViewModel(s.Label.Get(L.Language), s.Instruction)).ToList();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAssistantOpen), nameof(AssistantTitle))]
    public partial SectionViewModel? AssistantSection { get; set; }

    public bool IsAssistantOpen => AssistantSection is not null;

    public string AssistantTitle => AssistantSection is null ? string.Empty : L.Format("AssistantTitle", AssistantSection.Title);

    [ObservableProperty]
    public partial string AssistantInstruction { get; set; } = string.Empty;

    /// <summary>The answer as it streams in.</summary>
    [ObservableProperty]
    public partial string AssistantPartial { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProposal), nameof(CanAcceptProposal))]
    public partial RevisionProposal? Proposal { get; set; }

    public bool HasProposal => Proposal is { IsRefusal: false };

    public bool CanAcceptProposal => Proposal is { IsRefusal: false, IsUnusable: false };

    [ObservableProperty]
    public partial string ProposalHints { get; set; } = string.Empty;

    private string _lastInstruction = string.Empty;

    [ObservableProperty]
    public partial ModelStatus ModelStatus { get; set; } = new(ModelState.Unknown);

    [ObservableProperty]
    public partial string ModelStatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsModelReady { get; set; } = true;

    partial void OnSelectedProviderChanged(ProviderOption? value) => _ = CheckModelStatusAsync();

    /// <summary>Probes the selected engine (loopback only) and updates the status pill.</summary>
    [RelayCommand]
    public async Task CheckModelStatusAsync()
    {
        if (SelectedProvider is null)
        {
            return;
        }

        var option = SelectedProvider;
        ModelStatus = await option.ProbeAsync(CancellationToken.None);
        option.RefreshName();
        IsModelReady = ModelStatus.State is ModelState.Ready or ModelState.Unknown;
        if (ModelStatus.State == ModelState.Ready && option.CanWarmUp && _warmUpStarted.Add(option.Key))
        {
            _ = WarmUpAsync(option);
        }

        ModelStatusText = ModelStatus.State switch
        {
            ModelState.Ready when _inMemory.Contains(option.Key) => L.Format("ModelInMemory", option.DisplayName),
            ModelState.Ready when _warmUpStarted.Contains(option.Key) && !_warmUpFailed.Contains(option.Key) => L.Format("ModelWarming", option.DisplayName),
            ModelState.Ready => L.Format("ModelReady", option.DisplayName),
            ModelState.NotRunning => L["ModelNotRunning"],
            ModelState.ModelMissing => L.Format("ModelMissing", ModelStatus.Detail),
            ModelState.Refused => L.Format("ModelRefusedCloud", ModelStatus.Detail),
            _ => string.Empty,
        };
    }

    /// <summary>Engines whose warm-up was started, and those now loaded in memory (each warmed once per session).</summary>
    private readonly HashSet<string> _warmUpStarted = new(StringComparer.Ordinal);
    private readonly HashSet<string> _inMemory = new(StringComparer.Ordinal);
    private readonly HashSet<string> _warmUpFailed = new(StringComparer.Ordinal);

    /// <summary>The longest the window waits, on closing, for the local model to free its memory.</summary>
    public static readonly TimeSpan ReleaseWait = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Frees the memory the local engines hold (about 3 GB for Gemma 3 4B) when the app closes, instead of leaving it
    /// taken until Ollama's keep-alive runs out. Waits at most <see cref="ReleaseWait"/>; a server that does not answer
    /// keeps the model until then, which is harmless.
    /// </summary>
    public void ReleaseModels()
    {
        var releases = Providers.Where(p => p.CanRelease).Select(p => Task.Run(async () =>
        {
            using var timeout = new CancellationTokenSource(ReleaseWait);
            try
            {
                await p.ReleaseAsync(timeout.Token);
            }
            catch (Exception ex) when (ex is ModelProviderException or OperationCanceledException or InvalidOperationException or HttpRequestException)
            {
                // Not critical: Ollama frees it when the keep-alive runs out.
            }
        })).ToArray();
        Task.WaitAll(releases, ReleaseWait + TimeSpan.FromMilliseconds(500));
    }

    /// <summary>Loads the local model in the background so the lawyer's first request does not wait for it.</summary>
    private async Task WarmUpAsync(ProviderOption option)
    {
        try
        {
            await option.WarmUpAsync(CancellationToken.None);
            _inMemory.Add(option.Key);
        }
        catch (Exception ex) when (ex is ModelProviderException or OperationCanceledException or InvalidOperationException or HttpRequestException)
        {
            // Not critical: the first request will load the model instead. No retry, so a failing server is not hammered.
            _warmUpFailed.Add(option.Key);
            if (SelectedProvider == option && ModelStatus.State == ModelState.Ready)
            {
                ModelStatusText = L.Format("ModelReady", option.DisplayName);
            }

            return;
        }

        if (SelectedProvider == option && ModelStatus.State == ModelState.Ready)
        {
            ModelStatusText = L.Format("ModelInMemory", option.DisplayName);
        }
    }

    private void WireSection(SectionViewModel section)
    {
        section.RedraftHandler = RedraftSectionAsync;
        section.ReviewChanged = OnSectionReviewChanged;
        section.AssistantHandler = s =>
        {
            AssistantSection = s;
            AssistantThread.Clear();
            ClearProposal();
        };
    }

    [RelayCommand]
    private void CloseAssistant()
    {
        AssistantSection = null;
        ClearProposal();
    }

    [RelayCommand]
    private Task UseShortcutAsync(ShortcutViewModel shortcut)
    {
        AssistantInstruction = shortcut?.Instruction ?? string.Empty;
        return SendInstructionAsync(CancellationToken.None);
    }

    /// <summary>Sends the lawyer's instruction for the open section and shows the proposal as a diff.</summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SendInstructionAsync(CancellationToken cancellationToken)
    {
        var section = AssistantSection;
        var instruction = AssistantInstruction.Trim();
        if (section is null || instruction.Length == 0 || SelectedProvider is null)
        {
            return;
        }

        _lastInstruction = instruction;
        AssistantInstruction = string.Empty;
        AssistantThread.Add(new AssistantMessage(instruction, FromLawyer: true));
        ClearProposal();

        IModelProvider? provider = null;
        try
        {
            provider = SelectedProvider.Create();
            AssistantActivity.Start([L["ActivityRevising"]], PowerOnly());
            AssistantActivity.BeginStep(0);
            var proposal = await new SectionReviser(provider).ReviseAsync(
                _definition,
                section.Id,
                section.Text,
                instruction,
                DraftingInputs(),
                partial =>
                {
                    AssistantPartial = partial;
                    AssistantActivity.OnText(partial);
                },
                LegalTextsForDrafting(),
                cancellationToken);
            AssistantActivity.Complete();
            AssistantPartial = string.Empty;
            ShowProposal(section, proposal);
            NoteAnswer(provider);
        }
        catch (OperationCanceledException)
        {
            AssistantThread.Add(new AssistantMessage(L["Cancelled"], FromLawyer: false));
        }
        catch (Exception ex) when (IsModelOrRenderFailure(ex))
        {
            AssistantThread.Add(new AssistantMessage(ErrorText(ex), FromLawyer: false));
        }
        finally
        {
            AssistantActivity.Stop();
            AssistantPartial = string.Empty;
            (provider as IDisposable)?.Dispose();
        }
    }

    private void ShowProposal(SectionViewModel section, RevisionProposal proposal)
    {
        Proposal = proposal;
        if (proposal.IsRefusal)
        {
            AssistantThread.Add(new AssistantMessage(L["AssistantScope"], FromLawyer: false));
            return;
        }

        foreach (var token in TextDiff.Words(section.Text, proposal.Text))
        {
            ProposalDiff.Add(new DiffTokenViewModel(token.Text, token.Kind == DiffKind.Added, token.Kind == DiffKind.Removed));
        }

        ProposalHints = string.Join("\n", proposal.Issues.Select(i => "⚠ " + DescribeIssue(i)));
        AssistantThread.Add(new AssistantMessage(L[proposal.IsUnusable ? "ProposalUnusable" : "ProposalReady"], FromLawyer: false));
    }

    [RelayCommand]
    private void AcceptProposal()
    {
        if (AssistantSection is not { } section || Proposal is not { IsRefusal: false, IsUnusable: false } proposal)
        {
            return;
        }

        section.PushHistory();
        section.Text = proposal.Text; // goes through OnSectionEdited: draft updated, re-checked
        MarkModelWritten(section);
        AssistantThread.Add(new AssistantMessage(L["ProposalAccepted"], FromLawyer: false));
        ClearProposal();
    }

    [RelayCommand]
    private void RejectProposal()
    {
        AssistantThread.Add(new AssistantMessage(L["ProposalRejected"], FromLawyer: false));
        ClearProposal();
    }

    [RelayCommand]
    private Task RetryProposalAsync()
    {
        AssistantInstruction = _lastInstruction;
        return SendInstructionAsync(CancellationToken.None);
    }

    private void ClearProposal()
    {
        Proposal = null;
        ProposalDiff.Clear();
        ProposalHints = string.Empty;
    }

    /// <summary>Redrafts one narrative section from the notes; the previous text stays in the section's history.</summary>
    private async Task RedraftSectionAsync(SectionViewModel section)
    {
        if (SelectedProvider is null)
        {
            return;
        }

        IModelProvider? provider = null;
        var previous = section.Text;
        var completed = false;
        try
        {
            provider = SelectedProvider.Create();
            SetStatus(L.Format("Generating", section.Title));
            GenerationActivity.Start([section.Title], PowerOnly());
            GenerationActivity.BeginStep(0);
            MarkWriting(section.Id);
            var drafted = await new DraftingService(provider).DraftSectionAsync(
                _definition,
                section.Id,
                DraftingInputs(),
                (_, partial) =>
                {
                    section.SetFromModel(partial);
                    GenerationActivity.OnText(partial);
                },
                LegalTextsForDrafting());
            GenerationActivity.Complete();
            if (previous.Trim().Length > 0)
            {
                section.SetFromModel(previous);
                section.PushHistory();
            }

            ApplyModelSection(section, drafted);
            completed = true;
            Recheck();
            ReportDrafted(provider);
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
            if (!completed)
            {
                section.SetFromModel(previous); // the unfinished answer was a preview only
            }

            GenerationActivity.Stop();
            MarkWriting(null);
            (provider as IDisposable)?.Dispose();
        }
    }

    /// <summary>Puts model-written text into a section and remembers it as "untouched by the lawyer".</summary>
    private void ApplyModelSection(SectionViewModel section, DraftSection drafted)
    {
        if (_draft.FindSection(section.Id) is { } target)
        {
            target.Text = drafted.Text;
            target.DraftingNotes.Clear();
            target.DraftingNotes.AddRange(drafted.DraftingNotes);
        }

        section.SetFromModel(drafted.Text);
        _modelText[section.Id] = drafted.Text;
        MarkModelWritten(section);
    }

    /// <summary>The section's text now comes from an AI model (not in template mode): it needs the lawyer's review again.</summary>
    private void MarkModelWritten(SectionViewModel section)
    {
        var byModel = SelectedProvider?.Key != StubProviderKey && section.Text.Trim().Length > 0;
        if (_draft.FindSection(section.Id) is { } target)
        {
            target.AiWritten = byModel || target.AiWritten;
            target.Reviewed = false;
        }

        section.SetReviewState(byModel || section.IsAiWritten, reviewed: false);
        UpdateExportBlockers();
    }

    /// <summary>Narrative sections the lawyer wrote or changed: redrafting keeps them.</summary>
    private Dictionary<string, string> EditedNarrative() =>
        Sections.Where(s => s.IsEditable && s.Text.Trim().Length > 0 && _modelText.GetValueOrDefault(s.Id) != s.Text)
            .ToDictionary(s => s.Id, s => s.Text, StringComparer.Ordinal);

    private string DescribeIssue(NarrativeIssue issue) => L.Format(issue.Kind switch
    {
        NarrativeIssueKind.ForeignScript => "IssueForeignScript",
        NarrativeIssueKind.NotArabic => "IssueNotArabic",
        NarrativeIssueKind.UnknownNumber => "IssueUnknownNumber",
        NarrativeIssueKind.UnexpectedLatinWord => "IssueUnexpectedLatinWord",
        NarrativeIssueKind.MissingNumber => "IssueMissingNumber",
        NarrativeIssueKind.UngroundedTerm => "IssueUngroundedTerm",
        NarrativeIssueKind.UncitedArticle => "IssueUncitedArticle",
        NarrativeIssueKind.MisquotedArticle => "IssueMisquotedArticle",
        NarrativeIssueKind.UnsupportedArticleContent => "IssueUnsupportedArticleContent",
        NarrativeIssueKind.UnverifiedQuoteFromNotes => "IssueUnverifiedQuoteFromNotes",
        NarrativeIssueKind.SwappedRoles => "IssueSwappedRoles",
        _ => throw new InvalidOperationException($"No message for {issue.Kind}."),
    }, issue.Detail);
}
