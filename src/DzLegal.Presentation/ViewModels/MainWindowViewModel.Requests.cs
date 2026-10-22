using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Drafting;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// The Requests step. The document's requests are the lawyer's own lines. On request, the model proposes a formal
/// wording of them (notes in Darija or French); the proposal is shown only if checks prove it changed no relief, and it
/// replaces the lawyer's lines only when the lawyer accepts it.
/// </summary>
public sealed partial class MainWindowViewModel
{
    /// <summary>The proposed lines, one per request, while the lawyer decides; empty otherwise.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRequestsProposal))]
    public partial string RequestsProposal { get; set; } = string.Empty;

    public bool HasRequestsProposal => RequestsProposal.Length > 0;

    /// <summary>Outcome of the last proposal («لم يحافظ الاقتراح على المبالغ…», «اعتُمدت الصياغة»).</summary>
    [ObservableProperty]
    public partial string RequestsWordingStatus { get; set; } = string.Empty;

    /// <summary>Only an AI engine can propose a wording; template mode has nothing to offer.</summary>
    public bool CanProposeRequestsWording => SelectedProvider is { Key: not "stub" } && _definition.FormaliseRequestsPrompt is not null;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ProposeRequestsWordingAsync(CancellationToken cancellationToken)
    {
        RequestsProposal = string.Empty;
        if (RefuseWhileTooLong())
        {
            return;
        }

        var inputs = CurrentInputs();
        if (!CanProposeRequestsWording || RequestsFormaliser.Lines(inputs.GetValueOrDefault(RequestsField)).Count == 0)
        {
            RequestsWordingStatus = L["RequestsWordingNeedsNotes"];
            return;
        }

        IModelProvider? provider = null;
        try
        {
            provider = SelectedProvider!.Create();
            RequestsWordingStatus = L["RequestsWordingRunning"];
            var proposal = await new RequestsFormaliser(provider).ProposeAsync(_definition, inputs, cancellationToken);
            NoteAnswer(provider);
            if (proposal.IsOffered)
            {
                RequestsProposal = string.Join("\n", proposal.Lines);
                RequestsWordingStatus = L["RequestsWordingReady"];
            }
            else
            {
                RequestsWordingStatus = L["RequestsWordingWithheld"];
            }
        }
        catch (OperationCanceledException)
        {
            RequestsWordingStatus = L["Cancelled"];
        }
        catch (Exception ex) when (IsModelOrRenderFailure(ex))
        {
            RequestsWordingStatus = ErrorText(ex);
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    /// <summary>The lawyer accepts the wording: it becomes their requests lines, so the document shows it.</summary>
    [RelayCommand]
    private void AcceptRequestsProposal()
    {
        if (!HasRequestsProposal)
        {
            return;
        }

        Field(RequestsField).Value = RequestsProposal;
        RequestsProposal = string.Empty;
        RequestsWordingStatus = L["RequestsWordingAccepted"];
    }

    [RelayCommand]
    private void RejectRequestsProposal()
    {
        RequestsProposal = string.Empty;
        RequestsWordingStatus = string.Empty;
    }
}
