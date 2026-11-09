using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.Services;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class ModelSetupViewModel
{
    private readonly IModelPackFiles? files;
    private readonly ILocalModelPacks? packs;
    private string? pendingPack;
    private bool readyBeforeExport;
    public bool CanImport => !IsBusy && files is not null && packs is not null;
    public bool CanExport => CanImport && IsReady;
    public bool CanInstallPack => CanImport && pendingPack is not null && !IsReady;
    public string PendingFileName => pendingPack is null ? string.Empty : L.Format("ModelSetupPackSelected", Path.GetFileName(pendingPack));

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ChoosePackAsync()
    {
        try
        {
            var path = await files!.PickImportAsync();
            if (path is null) return;
            pendingPack = null;
            readyBeforeExport = false;
            IsBusy = true;
            Progress = new ModelSetupProgress(ModelSetupStage.Checking, SelectedModel.Name);
            var model = await packs!.ReadPackAsync(path, lifetime.Token);
            SelectKnownModel(model.Name);
            pendingPack = path;
            Progress = new ModelSetupProgress(ModelSetupStage.ImportSelected, model.Name);
        }
        catch (OperationCanceledException) { }
        catch (ModelSetupException ex) { Progress = new ModelSetupProgress(ex.Stage, SelectedModel.Name); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        { Progress = new ModelSetupProgress(ModelSetupStage.InvalidPack, SelectedModel.Name); }
        finally { IsBusy = false; }
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanInstallPack))]
    private async Task InstallPackAsync(CancellationToken cancellationToken)
    {
        var path = pendingPack;
        var model = SelectedModel.Name;
        if (path is null) return;
        await RunPackAsync(ModelSetupStage.Importing, (progress, ct) => packs!.ImportAsync(path, model, progress, ct), cancellationToken);
        if (Progress.Stage is ModelSetupStage.Ready or ModelSetupStage.MemoryLow or ModelSetupStage.InvalidPack)
        {
            pendingPack = null;
            NotifyState();
        }
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanExport))]
    private async Task SavePackAsync(CancellationToken cancellationToken)
    {
        try
        {
            var model = SelectedModel.Name;
            var path = await files!.PickExportAsync(model);
            if (path is null) return;
            readyBeforeExport = IsReady;
            await RunPackAsync(ModelSetupStage.Exporting, (progress, ct) => packs!.ExportAsync(model, path, progress, ct), cancellationToken);
            if (Progress.Stage is ModelSetupStage.VerificationFailed or ModelSetupStage.MemoryLow)
            {
                readyBeforeExport = false;
                NotifyState();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        { Progress = new ModelSetupProgress(ModelSetupStage.ExportFailed, SelectedModel.Name); }
    }

    private async Task RunPackAsync(ModelSetupStage stage, Func<IProgress<ModelSetupProgress>, CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        IsBusy = true;
        Progress = new ModelSetupProgress(stage, SelectedModel.Name);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        using var reports = new SetupReports(SynchronizationContext.Current, p => Progress = p);
        ModelSetupProgress? result = null;
        try { await operation(reports, linked.Token); result = reports.Latest; }
        catch (OperationCanceledException) { result = new ModelSetupProgress(ModelSetupStage.Cancelled, SelectedModel.Name); }
        catch (ModelSetupException ex) { result = new ModelSetupProgress(ex.Stage, SelectedModel.Name); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or InvalidOperationException)
        { result = new ModelSetupProgress(stage == ModelSetupStage.Exporting ? ModelSetupStage.ExportFailed : ModelSetupStage.ImportFailed, SelectedModel.Name); }
        finally
        {
            reports.Dispose();
            if (result is not null) Progress = result;
            IsBusy = false;
        }
    }
}