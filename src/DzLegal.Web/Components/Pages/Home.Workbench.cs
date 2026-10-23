using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.ViewModels;
using Microsoft.JSInterop;

namespace DzLegal.Web.Components.Pages;

public sealed partial class Home
{
    private DotNetObjectReference<Home>? _workbenchBridge;
    private bool commandOpen;
    private string commandQuery = string.Empty;
    private bool _isNarrow;
    private bool _lastAssistantOpen;
    private sealed record WebCommand(string Label, Func<Task> Execute);

    private static string StepIcon(string id) => id switch
    {
        "court" => "building", "plaintiff" or "defendant" => "person", "claim" => "briefcase",
        "facts" => "text_description", "grounds" => "library", "requests" => "checkmark_circle",
        "documents" => "document", _ => "edit",
    };

    private void SelectMobileTab(int tab)
    {
        mobileTab = tab;
        if (tab == 2 && !Vm!.IsToolsOpen) Vm.ShowWorkbenchToolCommand.Execute("review");
    }

    private void ShowTool(string tool)
    {
        Vm!.ShowWorkbenchToolCommand.Execute(tool);
        if (_isNarrow) mobileTab = 2;
    }

    public sealed record BrowserWorkbenchLayout(bool Compact, bool OutlineVisible, bool ToolsOpen, string Tool);

    [JSInvokable]
    public void RestoreWorkbenchLayout(BrowserWorkbenchLayout layout)
    {
        if (Vm is null) return;
        Vm.IsOutlineCompact = layout.Compact;
        Vm.IsOutlineVisible = layout.OutlineVisible;
        Vm.WorkbenchTool = layout.Tool is "review" or "source" or "assistant" or "activity" ? layout.Tool : "review";
        Vm.IsToolsOpen = layout.ToolsOpen;
        StateHasChanged();
    }

    [JSInvokable]
    public Task ShowCommands() => InvokeAsync(() =>
    {
        commandQuery = string.Empty;
        commandOpen = true;
        StateHasChanged();
    });

    [JSInvokable]
    public Task CloseCommands() => InvokeAsync(HideCommands);

    private void HideCommands()
    {
        commandOpen = false;
        StateHasChanged();
    }

    private IEnumerable<WebCommand> FilteredCommands => WebCommands
        .Where(c => c.Label.Contains(commandQuery.Trim(), StringComparison.OrdinalIgnoreCase))
        .OrderBy(c => c.Label.Equals(commandQuery.Trim(), StringComparison.OrdinalIgnoreCase) ? 0
            : c.Label.StartsWith(commandQuery.Trim(), StringComparison.OrdinalIgnoreCase) ? 1 : 2);

    private async Task ExecuteWebCommand(WebCommand command)
    {
        commandOpen = false;
        await command.Execute();
    }

    private List<WebCommand> WebCommands
    {
        get
        {
            var vm = Vm!;
            var commands = new List<WebCommand>();
            void Add(string label, Action action) => commands.Add(new WebCommand(L[label], () => { action(); return Task.CompletedTask; }));
            void Async(string label, IAsyncRelayCommand command) => commands.Add(new WebCommand(L[label], () => Run(command)));
            Add("New", () => vm.NewCommand.Execute(null));
            commands.Add(new WebCommand(L["Open"], () => Js.InvokeVoidAsync("dzlegal.openDraftPicker").AsTask()));
            Async("Save", vm.SaveCommand); Async("Generate", vm.GenerateCommand); Async("AnalyseGrounds", vm.AnalyseGroundsCommand);
            Async("ExportPdf", vm.ExportPdfCommand); Async("ExportWord", vm.ExportWordCommand); Async("CopyText", vm.CopyTextCommand);
            Add("Edit", () => vm.SelectedDraftTab = 0); Add("Preview", () => vm.SelectedDraftTab = 1);
            Add("ToggleTheme", () => vm.ToggleThemeCommand.Execute(null)); Add("SwitchLanguage", () => vm.SwitchLanguageCommand.Execute(null));
            Add("WB.ToggleOutline", () => vm.ToggleOutlineCommand.Execute(null)); Add("WB.CompactOutline", () => vm.ToggleOutlineCompactCommand.Execute(null));
            Add("WB.FocusDraft", () => vm.ToggleDraftFocusCommand.Execute(null));
            Async("WB.CheckEngine", vm.CheckModelStatusCommand);
            Add("WB.HideTools", () => vm.HideWorkbenchToolsCommand.Execute(null));
            foreach (var tool in new[] { ("WB.BeforeExport", "review"), ("WB.Source", "source"), ("WB.Assistant", "assistant"), ("WB.Activity", "activity") })
                Add(tool.Item1, () => ShowTool(tool.Item2));
            foreach (var step in vm.FieldGroups) commands.Add(new WebCommand(step.Label, () => { SelectStep(step); return Task.CompletedTask; }));
            if (vm.IsConfirmingReplace)
            {
                Async("SaveFirst", vm.SaveThenReplaceCommand); Async("ContinueWithoutSaving", vm.ReplaceWithoutSavingCommand);
                Add("Cancel", () => vm.CancelReplaceCommand.Execute(null));
            }
            if (vm.IsRequestsStep) Async("ProposeRequestsWording", vm.ProposeRequestsWordingCommand);
            if (vm.HasRequestsProposal) Add("RejectRequestsWording", () => vm.RejectRequestsProposalCommand.Execute(null));
            foreach (var section in vm.Sections)
            {
                void SectionCommand(string key, System.Windows.Input.ICommand command) => commands.Add(new WebCommand(L[key] + " · " + section.Title,
                    () => command is IAsyncRelayCommand asyncCommand ? Run(asyncCommand) : ExecuteSync(command)));
                if (section.CanOverride) SectionCommand("EditManually", section.OverrideCommand);
                if (section.IsOverridden) SectionCommand("RestoreFromForm", section.RestoreCommand);
                if (section.CanUndo) SectionCommand("Undo", section.UndoCommand);
                if (section.IsEditable) { SectionCommand("Redraft", section.RedraftCommand); SectionCommand("Assistant", section.OpenAssistantCommand); }
            }
            if (vm.HasProposal)
            {
                Add("Accept", () => vm.AcceptProposalCommand.Execute(null)); Add("Reject", () => vm.RejectProposalCommand.Execute(null));
                Async("TryAgain", vm.RetryProposalCommand);
            }
            if (vm.HasRecognisedClaims)
            {
                Add("ConfirmClaim", () => vm.ConfirmClaimCommand.Execute(null)); Add("RejectClaim", () => vm.RejectClaimCommand.Execute(null)); Add("TickBasisArticles", () => vm.TickBasisArticlesCommand.Execute(null));
            }
            if (vm.GenerateCommand.IsRunning) Add("Cancel", () => vm.GenerateCancelCommand.Execute(null));
            if (vm.AnalyseGroundsCommand.IsRunning) Add("Cancel", () => vm.AnalyseGroundsCancelCommand.Execute(null));
            if (vm.SendInstructionCommand.IsRunning) Add("Cancel", () => vm.SendInstructionCancelCommand.Execute(null));
            return commands;
        }
    }

    private static Task ExecuteSync(System.Windows.Input.ICommand command)
    {
        if (command.CanExecute(null)) command.Execute(null);
        return Task.CompletedTask;
    }
}
