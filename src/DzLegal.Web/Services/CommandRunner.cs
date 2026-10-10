using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.Web.Services;

/// <summary>
/// How the page starts the view model's long commands (AI, exports). Blazor calls a handler directly, without the
/// enabled/disabled checks XAML buttons get, so this does them: one AI task at a time per visitor (a double click or a
/// second button cannot start a parallel run that overwrites the first), and an unexpected error becomes a message in
/// the status bar instead of ending the visitor's session.
/// </summary>
public static class CommandRunner
{
    public static async Task RunAsync(MainWindowViewModel vm, IAsyncRelayCommand command, object? argument = null)
    {
        ArgumentNullException.ThrowIfNull(vm);
        ArgumentNullException.ThrowIfNull(command);
        if (!command.CanExecute(argument) || IsAiBusy(vm))
        {
            return;
        }

        try
        {
            await command.ExecuteAsync(argument);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            vm.StatusMessage = string.Format(vm.L.Culture, vm.L["Error"], ex.Message);
        }
    }

    /// <summary>True while any AI task of this visitor runs: drafting, redrafting, analysing, or the assistant.</summary>
    public static bool IsAiBusy(MainWindowViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        return vm.GenerateCommand.IsRunning
            || vm.AnalyseGroundsCommand.IsRunning
            || vm.SendInstructionCommand.IsRunning
            || vm.UseShortcutCommand.IsRunning
            || vm.RetryProposalCommand.IsRunning
            || vm.Sections.Any(s => s.RedraftCommand.IsRunning);
    }
}
