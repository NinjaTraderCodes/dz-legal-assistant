using DzLegal.Presentation.ViewModels;
using DzLegal.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DzLegal.Web.Components.Pages;

/// <summary>
/// The demo's guide: one step at a time, in a card docked at the bottom of the window. It never clicks for the visitor:
/// it lights up the one field or button to use next (dzlegal.point) and moves on once the visitor has done it, reading
/// the same view-model state the desktop's sample guide reads. The step is remembered for the visit (session storage).
/// </summary>
public sealed partial class Home
{
    internal const int GuideSteps = 5;
    private const string GuideKey = "dzlegal.guide";
    internal const string DomicileId = "defendant.domicile";
    internal const string DefendantStepId = "defendant";
    internal const string SampleId = "debt";

    [SupplyParameterFromQuery(Name = "guide")] private string? GuideQuery { get; set; }

    /// <summary>The step shown, 1 to 5.</summary>
    private int guideStep = 1;

    // What the visitor has done in the current step that the view model does not keep: opening the sample from the
    // picker, clearing the domicile, a successful export. Each is forgotten when the step is entered again.
    private bool _guideSampleOpened;
    private bool _domicileCleared;
    private bool _guideExported;

    /// <summary>The step was already done when it was entered (the visitor came back to it): it says so and waits for Next.</summary>
    private bool _stepDoneAtEntry;

    /// <summary>The furthest step reached. Next is locked until the step is done; a step done before can be passed again.</summary>
    private int _furthest = 1;

    /// <summary>The furthest guide step already counted in this session, so going back and forth counts each step once.</summary>
    private int _counted;

    /// <summary>The target last sent (empty: the guide is open with nothing to point at; null: it is closed), so the
    /// browser is called only when it changes, not on every render.</summary>
    private string? _pointed;

    /// <summary>What the guide points at and what its card says.</summary>
    private sealed record GuidePointer(string? Target, string Hint, bool Done = false);

    private string Quote(string text) => Lang == "ar" ? $"«{text}»" : $"“{text}”";

    private string Format(string key, params object[] args) => string.Format(L.Culture, T(key), args);

    private string StepLabel(string id) => Vm!.FieldGroups.FirstOrDefault(g => g.Id == id)?.Label ?? id;

    private static string StepTarget(string id) => $".steps .step[data-step=\"{id}\"]";

    private const string ExportPdfTarget = ".toolbar button.export[data-export=\"pdf\"]";

    private bool DomicileEmpty => Vm!.Field(DomicileId).Value.Trim().Length == 0;

    /// <summary>The current step's pointer, from the view model's state: the next thing to use, or that the step is done.</summary>
    private GuidePointer Pointer()
    {
        var vm = Vm!;
        if (_stepDoneAtEntry)
        {
            return new(null, Format("GuideDoneStep", Quote(guideStep == GuideSteps ? T("GuideFinish") : L["Next"])), Done: true);
        }

        switch (guideStep)
        {
            case 1:
                var sample = Engine.Samples.FirstOrDefault(s => s.Id == SampleId);
                return new("#sample-picker", Format("Guide1Pick", Quote(sample?.Title.Get(Lang) ?? SampleId), Quote(T("Samples"))));
            case 2:
                if (DomicileEmpty)
                {
                    // The score and the red card it raised, lit together.
                    return new(".score-card, .finding.mandatory", Format("Guide2Look", vm.ScoreText));
                }

                return vm.SelectedStep?.Id == DefendantStepId
                    ? new("#f-defendant-domicile", T("Guide2Clear"))
                    : new(StepTarget(DefendantStepId), Format("GuideStep", Quote(StepLabel(DefendantStepId))));
            case 3:
                if (vm.AnalyseGroundsCommand.IsRunning)
                {
                    return new(".grounds .activity", T("Guide3Wait"));
                }

                if (!vm.IsGroundsStep)
                {
                    return new(StepTarget(MainWindowViewModel.GroundsStepId), Format("GuideStep", Quote(StepLabel(MainWindowViewModel.GroundsStepId))));
                }

                // The model can fail or be cancelled (the panel's own status says why, under the button): the guide says
                // so rather than repeating the first instruction.
                if (vm.AnalysisStatus.Length > 0 && (vm.Analysis is null || vm.IsAnalysisStale))
                {
                    return new(".grounds button.ai", Format("GuideRetry", Quote(L["AnalyseGrounds"])));
                }

                if (vm.NeedsClaimConfirmation)
                {
                    return new(".confirm-claim button.primary", Format("Guide3Confirm", Quote(L["ConfirmClaim"])));
                }

                // An analysis that recognised no claim type has nothing to confirm: the lawyer ticks the articles.
                return vm.Analysis is not null && !vm.IsAnalysisStale
                    ? new(".grounds", Format("Guide3Articles", Quote(L["Next"])))
                    : new(".grounds button.ai", Format("Guide3Analyse", Quote(L["AnalyseGrounds"])));
            case 4:
                if (vm.GenerateCommand.IsRunning)
                {
                    return new(".draft-panel .activity", T("Guide4Wait"));
                }

                // A failed draft is reported in the status bar as an error.
                return new(".engine-row button.ai", Format(vm.IsError ? "GuideRetry" : "Guide4Generate", Quote(L["Generate"])));
            default:
                if (_guideExported)
                {
                    return new(null, Format("Guide5Done", Quote(T("GuideFinish"))), Done: true);
                }

                if (vm.HasUnreviewedAiSections)
                {
                    return vm.IsExportBlockedShown
                        ? new(".review-list .reviewed:has(input:not(:checked))", Format("Guide5Tick", Quote(L["ReviewedCheck"])))
                        : new(ExportPdfTarget, Format("Guide5Early", Quote(L["ExportPdf"])));
                }

                return new(ExportPdfTarget, Format(vm.IsExportBlockedShown ? "Guide5Again" : "Guide5Export", Quote(L["ExportPdf"]), Quote(L["ExportWord"])));
        }
    }

    /// <summary>Whether the visitor has done what the current step asks.</summary>
    private bool StepDone() => guideStep switch
    {
        1 => _guideSampleOpened,
        2 => _domicileCleared && !DomicileEmpty,
        3 => Vm!.IsClaimConfirmed,
        4 => Vm!.HasGeneratedDraft && !Vm.GenerateCommand.IsRunning,
        _ => _guideExported,
    };

    /// <summary>
    /// Next moves on only past a step the visitor has done: one done before, one already done on arrival, or, when the
    /// model recognised no claim type, once the visitor has ticked articles by hand. Otherwise the guide moves on by itself.
    /// </summary>
    private bool CanGoNext() =>
        guideStep < _furthest || _stepDoneAtEntry || (guideStep == GuideSteps && _guideExported)
        || (guideStep == 3 && Vm!.Analysis is not null && !Vm.IsAnalysisStale && !Vm.NeedsClaimConfirmation && Vm.HasSelectedArticles);

    private void EnterStep(int step)
    {
        guideStep = Math.Clamp(step, 1, GuideSteps);
        _furthest = Math.Max(_furthest, guideStep);
        if (guideStep > _counted)
        {
            _counted = guideStep;
            Note(VisitKinds.Guide, guideStep.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        _guideSampleOpened = false;
        _domicileCleared = false;
        _guideExported = false;
        _stepDoneAtEntry = StepDone();
    }

    private async Task ToggleGuide()
    {
        if (showGuide)
        {
            await CloseGuide();
        }
        else
        {
            OpenGuide();
        }
    }

    private void OpenGuide()
    {
        showGuide = true;
        _focusGuide = true;
        EnterStep(guideStep);
    }

    private async Task CloseGuide()
    {
        showGuide = false;
        await RememberGuideAsync();
    }

    private async Task MoveGuide(int direction)
    {
        if (direction > 0 && !CanGoNext())
        {
            return;
        }

        if (direction > 0 && guideStep == GuideSteps)
        {
            // Finished: the next opening starts from the beginning.
            Note(VisitKinds.Guide, "finished");
            guideStep = 1;
            _furthest = 1;
            await CloseGuide();
            return;
        }

        EnterStep(guideStep + direction);
        await RememberGuideAsync();
    }

    private async Task OnGuideKey(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            await CloseGuide();
        }
    }

    /// <summary>The visitor chose a sample: picking the guide's own sample is step 1 done.</summary>
    private void NoteSampleOpened(string id)
    {
        if (showGuide && guideStep == 1 && id == SampleId)
        {
            _guideSampleOpened = true;
        }
    }

    /// <summary>An export the visitor pressed: the guide's last step is done when it went through.</summary>
    private async Task ExportAsync(CommunityToolkit.Mvvm.Input.IAsyncRelayCommand command)
    {
        await Run(command);
        Note(VisitKinds.Export, (command == Vm!.ExportPdfCommand ? "pdf" : "word") + (Vm.HasExportBlockers ? " refused" : string.Empty));
        if (showGuide && guideStep == GuideSteps && !Vm!.HasExportBlockers && !Vm.IsError)
        {
            _guideExported = true;
        }
    }

    /// <summary>On the first render: the guide comes back open at its step if it was open, or opens on ?guide=1.</summary>
    private async Task RestoreGuideAsync()
    {
        var stored = await Js.InvokeAsync<string?>("dzlegal.sessionGet", GuideKey);
        var parts = stored?.Split(':');
        var open = parts?[0] == "open" || GuideQuery == "1";
        if (parts is { Length: 2 } && int.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var step))
        {
            guideStep = Math.Clamp(step, 1, GuideSteps);
        }

        if (open)
        {
            showGuide = true;
            EnterStep(guideStep);
            StateHasChanged();
        }
    }

    private async Task RememberGuideAsync()
    {
        try
        {
            await Js.InvokeVoidAsync("dzlegal.sessionSet", GuideKey, $"{(showGuide ? "open" : "closed")}:{guideStep}");
        }
        catch (JSException)
        {
            // Storage unavailable: the guide starts again from its first step on the next page load.
        }
    }

    /// <summary>
    /// After each render: a step the visitor has just completed moves the guide on, and the light follows the target.
    /// The browser is called only when the target changes.
    /// </summary>
    private async Task UpdateGuideAsync()
    {
        if (Vm is null)
        {
            return;
        }

        if (showGuide)
        {
            if (guideStep == 2 && DomicileEmpty)
            {
                _domicileCleared = true;
            }

            if (!_stepDoneAtEntry && guideStep < GuideSteps && StepDone())
            {
                EnterStep(guideStep + 1);
                await RememberGuideAsync();
                StateHasChanged();
                return;
            }
        }

        // The key tells a closed guide (null) from an open one with nothing to point at (empty).
        var target = showGuide ? Pointer().Target ?? string.Empty : null;
        if (target == _pointed)
        {
            return;
        }

        _pointed = target;
        try
        {
            await Js.InvokeVoidAsync("dzlegal.point", target, target is not null);
        }
        catch (JSException)
        {
            // The light is a help only; the card still says what to do.
        }
    }
}
