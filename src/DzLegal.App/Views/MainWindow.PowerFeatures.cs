using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;

namespace DzLegal.App.Views;

public partial class MainWindow
{
    private CommandPaletteWindow? _palette;
    private Window? _draftWindow;

    private void OpenCommandPalette(object? sender, RoutedEventArgs e) => ShowCommandPalette();

    public void ShowCommandPalette()
    {
        if (_palette is not null)
        {
            _palette.Activate();
            return;
        }
        if (DataContext is not MainWindowViewModel vm) return;
        _palette = new CommandPaletteWindow { DataContext = new CommandPaletteModel(vm, PaletteEntries(vm)) };
        _palette.Closed += (_, _) => _palette = null;
        _ = _palette.ShowDialog(this);
    }

    private static List<PaletteAction> PaletteEntries(MainWindowViewModel vm)
    {
        var list = new List<PaletteAction>();
        void Add(string key, ICommand command, object? argument = null) => list.Add(new PaletteAction(vm.L[key], command, argument));
        Add("New", vm.NewCommand); Add("Open", vm.OpenCommand); Add("Save", vm.SaveCommand);
        Add("Generate", vm.GenerateCommand); Add("AnalyseGrounds", vm.AnalyseGroundsCommand);
        if (vm.HasRecognisedClaims)
        {
            Add("ConfirmClaim", vm.ConfirmClaimCommand);
            Add("RejectClaim", vm.RejectClaimCommand);
            Add("TickBasisArticles", vm.TickBasisArticlesCommand);
        }
        if (vm.IsRequestsStep) Add("ProposeRequestsWording", vm.ProposeRequestsWordingCommand);
        if (vm.HasRequestsProposal) Add("RejectRequestsWording", vm.RejectRequestsProposalCommand);
        if (vm.GenerateCommand.IsRunning) Add("Cancel", vm.GenerateCancelCommand);
        if (vm.AnalyseGroundsCommand.IsRunning) Add("Cancel", vm.AnalyseGroundsCancelCommand);
        if (vm.SendInstructionCommand.IsRunning) Add("Cancel", vm.SendInstructionCancelCommand);
        void AddExport(string key, ICommand command) => Add(key, new RelayCommand(() =>
        {
            if (vm.HasExportBlockers) vm.ShowWorkbenchToolCommand.Execute("review");
            command.Execute(null);
        }, () => command.CanExecute(null)));
        AddExport("ExportPdf", vm.ExportPdfCommand); AddExport("ExportWord", vm.ExportWordCommand); AddExport("CopyText", vm.CopyTextCommand);
        Add("Preview", new RelayCommand(() => vm.SelectedDraftTab = 1));
        Add("Edit", new RelayCommand(() => vm.SelectedDraftTab = 0));
        Add("ToggleTheme", vm.ToggleThemeCommand); Add("SwitchLanguage", vm.SwitchLanguageCommand);
        Add("WB.CheckEngine", vm.CheckModelStatusCommand);
        Add("WB.ToggleOutline", vm.ToggleOutlineCommand); Add("WB.CompactOutline", vm.ToggleOutlineCompactCommand);
        Add("WB.FocusDraft", vm.ToggleDraftFocusCommand);
        Add(vm.IsDraftDetached ? "WB.DockDraft" : "WB.DetachDraft", vm.ToggleDraftWindowCommand);
        foreach (var tool in new[] { ("WB.BeforeExport", "review"), ("WB.Source", "source"), ("WB.Assistant", "assistant"), ("WB.Activity", "activity") }) Add(tool.Item1, vm.ShowWorkbenchToolCommand, tool.Item2);
        Add("WB.HideTools", vm.HideWorkbenchToolsCommand);
        if (vm.IsConfirmingReplace)
        {
            Add("SaveFirst", vm.SaveThenReplaceCommand); Add("ContinueWithoutSaving", vm.ReplaceWithoutSavingCommand); Add("Cancel", vm.CancelReplaceCommand);
        }
        if (vm.HasRecoverableDraft) { Add("Restore", vm.RestoreLastSessionCommand); Add("Dismiss", vm.DismissRecoveryCommand); }
        foreach (var step in vm.FieldGroups) list.Add(new PaletteAction(step.Label, vm.SelectStepCommand, step));
        foreach (var section in vm.Sections)
        {
            if (section.CanOverride) list.Add(new PaletteAction(vm.L["EditManually"] + " · " + section.Title, section.OverrideCommand));
            if (section.IsOverridden) list.Add(new PaletteAction(vm.L["RestoreFromForm"] + " · " + section.Title, section.RestoreCommand));
            if (section.CanUndo) list.Add(new PaletteAction(vm.L["Undo"] + " · " + section.Title, section.UndoCommand));
            if (section.IsEditable)
            {
                list.Add(new PaletteAction(vm.L["Redraft"] + " · " + section.Title, section.RedraftCommand));
                list.Add(new PaletteAction(vm.L["Assistant"] + " · " + section.Title, section.OpenAssistantCommand));
            }
        }
        if (vm.HasProposal) { Add("Accept", vm.AcceptProposalCommand); Add("Reject", vm.RejectProposalCommand); Add("TryAgain", vm.RetryProposalCommand); }
        return list;
    }

    private void ApplyDraftWindow()
    {
        if (_workbenchVm is not { } vm) return;
        if (!vm.IsDraftDetached)
        {
            _draftWindow?.Close();
            return;
        }
        if (_draftWindow is not null) return;
        // Keep the main editors mounted with their scroll/caret state. Both presentations bind the same sections.
        var view = new DraftWorkspaceView { DataContext = vm };
        var window = new Window
        {
            DataContext = vm, Content = view, Width = Metric("DetachedDraftWidth"), Height = Metric("DetachedDraftHeight"),
            MinWidth = Metric("DraftMinWidth"), MinHeight = Metric("WindowMinHeight"),
        };
        window.Bind(TitleProperty, new Binding(nameof(MainWindowViewModel.CaseTitle)) { Source = vm });
        window.Bind(RequestedThemeVariantProperty, new Binding(nameof(MainWindowViewModel.IsDarkTheme)) { Source = vm, Converter = Converters.DarkTheme });
        window.Bind(FlowDirectionProperty, new Binding(nameof(MainWindowViewModel.IsRightToLeft)) { Source = vm, Converter = Converters.RightToLeft });
        window.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.K && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { ShowCommandPalette(); e.Handled = true; }
            else if (e.Key == Key.P && e.KeyModifiers.HasFlag(KeyModifiers.Control) && vm.HasExportBlockers) vm.ShowWorkbenchToolCommand.Execute("review");
        }, RoutingStrategies.Tunnel);
        window.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.S, KeyModifiers.Control), Command = vm.SaveCommand });
        window.KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.P, KeyModifiers.Control), Command = vm.ExportPdfCommand });
        window.Closed += (_, _) =>
        {
            window.Content = null;
            _draftWindow = null;
            vm.IsDraftDetached = false;
        };
        _draftWindow = window;
        window.Show(this);
    }
}
