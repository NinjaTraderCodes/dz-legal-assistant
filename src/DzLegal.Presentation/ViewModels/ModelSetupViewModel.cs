using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Presentation.Services;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class ModelSetupViewModel : ObservableObject, IDisposable
{
    private readonly ILocalModelSetup service;
    private readonly Action templates;
    private readonly Func<Task> useLocal;
    private readonly CancellationTokenSource lifetime = new();
    public ModelSetupViewModel(Loc loc, ILocalModelSetup service, Action templates, Func<Task> useLocal)
    {
        L = loc;
        this.service = service;
        this.templates = templates;
        this.useLocal = useLocal;
        SelectedModel = Models[0];
        L.PropertyChanged += Localized;
    }
    public Loc L { get; }
    public IReadOnlyList<SetupModel> Models => service.Models;
    [ObservableProperty]
    public partial SetupModel SelectedModel { get; set; }
    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;
    [ObservableProperty]
    public partial bool IsBusy { get; set; }
    [ObservableProperty]
    public partial ModelSetupProgress Progress { get; set; } = new(ModelSetupStage.Checking, "gemma3:4b");

    public bool IsRightToLeft => L.IsRightToLeft;
    public bool IsReady => Progress.Stage == ModelSetupStage.Ready;
    public bool CanDownload => !IsBusy && !IsReady && Progress.Stage != ModelSetupStage.MemoryLow;
    public bool CanUseLocal => !IsBusy && IsReady;
    public bool HasProgress => IsBusy;
    public bool HasTransferProgress => IsBusy && Progress.Stage == ModelSetupStage.Downloading;
    public bool ShowDownload => !IsBusy && !IsReady && Progress.Stage != ModelSetupStage.MemoryLow;
    public bool ShowRefresh => !IsBusy;
    public bool CanCancel => IsBusy && Progress.Stage is ModelSetupStage.Preparing or ModelSetupStage.Downloading or ModelSetupStage.Verifying or ModelSetupStage.Testing;
    public string CompactStatus => Progress.Stage == ModelSetupStage.Downloading && Progress.Total > 0
        ? L.Format("ModelSetupCompactProgress", StatusText, Percent.ToString("F0", L.Culture)) : StatusText;
    public bool IsIndeterminate => Progress.Stage != ModelSetupStage.Downloading || Progress.Total <= 0;
    public double Percent => Progress.Total > 0 ? Math.Clamp(100d * Progress.Completed / Progress.Total, 0, 100) : 0;
    public string Recommendation => IsReady ? L.Format("ModelSetupInstalled", SelectedModel.Name)
        : L.Format("ModelSetupRecommendation", SelectedModel.Name, Bytes(SelectedModel.Bytes));
    public string TermsText => SelectedModel.Terms;
    public string TermsLabel => L.Format("ModelSetupTerms", SelectedModel.Name);
    public string ProgressText => Progress.Total > 0 ? L.Format("ModelSetupBytes", Percent.ToString("F0", L.Culture), Bytes(Progress.Completed), Bytes(Progress.Total)) : L["ModelSetupEstimating"];
    public string SpeedText => Progress.BytesPerSecond > 0 ? L.Format("ModelSetupSpeed", Bytes((long)Progress.BytesPerSecond), Math.Ceiling(Math.Max(0, Progress.Total - Progress.Completed) / Progress.BytesPerSecond / 60).ToString("F0", L.Culture)) : L["ModelSetupEstimating"];
    public string DiskText => !IsReady && Progress.RequiredDiskBytes > 0 ? L.Format("ModelSetupDisk", Bytes(Progress.FreeDiskBytes), Bytes(Progress.RequiredDiskBytes)) : string.Empty;
    public string StatusText => L["ModelSetup" + Progress.Stage];
    public string DownloadLabel => Progress.Stage is ModelSetupStage.Cancelled or ModelSetupStage.ConnectionFailed or ModelSetupStage.VerificationFailed
        ? L["ModelSetupRetry"] : L.Format("ModelSetupDownload", Bytes(SelectedModel.Bytes));

    private string Bytes(long bytes) => bytes >= 1024L * 1024 * 1024
        ? L.Format("ModelSetupGiB", (bytes / (1024d * 1024 * 1024)).ToString("F1", L.Culture))
        : L.Format("ModelSetupMiB", (bytes / (1024d * 1024)).ToString("F1", L.Culture));

    partial void OnSelectedModelChanged(SetupModel value)
    {
        if (Progress.Model != value.Name) Progress = new ModelSetupProgress(ModelSetupStage.Available, value.Name);
        NotifyState();
    }
    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnProgressChanged(ModelSetupProgress value) => NotifyState();
    private void Localized(object? sender, PropertyChangedEventArgs e) => NotifyState();
    private void NotifyState()
    {
        foreach (var property in new[] { nameof(IsRightToLeft), nameof(IsReady), nameof(CanDownload), nameof(CanUseLocal), nameof(HasProgress), nameof(HasTransferProgress), nameof(ShowDownload), nameof(ShowRefresh), nameof(CanCancel), nameof(CompactStatus), nameof(IsIndeterminate), nameof(Percent), nameof(Recommendation), nameof(TermsText), nameof(TermsLabel), nameof(ProgressText), nameof(SpeedText), nameof(DiskText), nameof(StatusText), nameof(DownloadLabel) })
            OnPropertyChanged(property);
        DownloadCommand.NotifyCanExecuteChanged();
        UseLocalCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;
    [RelayCommand]
    private void NotNow()
    {
        service.RememberTemplates();
        templates();
        IsExpanded = false;
    }
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => DownloadCommand.Cancel();

    [RelayCommand(CanExecute = nameof(CanUseLocal))]
    private async Task UseLocalAsync()
    {
        service.RememberLocal();
        await useLocal();
        IsExpanded = false;
    }
    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            Progress = new ModelSetupProgress(ModelSetupStage.Checking, SelectedModel.Name);
            Progress = await service.InspectAsync(lifetime.Token);
            SelectedModel = Models.First(m => m.Name == Progress.Model);
            IsExpanded = !IsReady && !service.PreferTemplates;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException or UnauthorizedAccessException)
        { Progress = new ModelSetupProgress(ModelSetupStage.EngineFailed, SelectedModel.Name); }
        finally { IsBusy = false; }
    }
    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        Progress = new ModelSetupProgress(ModelSetupStage.Preparing, SelectedModel.Name);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        using var reports = new SetupReports(SynchronizationContext.Current, p => Progress = p);
        ModelSetupProgress? result = null;
        try
        {
            await service.DownloadAsync(SelectedModel.Name, reports, linked.Token);
            result = reports.Latest;
        }
        catch (OperationCanceledException) { result = new ModelSetupProgress(ModelSetupStage.Cancelled, SelectedModel.Name); }
        catch (ModelSetupException ex) { result = new ModelSetupProgress(ex.Stage, SelectedModel.Name); }
        catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException or UnauthorizedAccessException)
        { result = new ModelSetupProgress(ModelSetupStage.EngineFailed, SelectedModel.Name); }
        finally
        {
            reports.Dispose();
            if (result is not null) Progress = result;
            IsBusy = false;
        }
    }

    private sealed class SetupReports(SynchronizationContext? context, Action<ModelSetupProgress> update) : IProgress<ModelSetupProgress>, IDisposable
    {
        private ModelSetupProgress? latest;
        private bool ended;
        public ModelSetupProgress? Latest => Volatile.Read(ref latest);
        public void Report(ModelSetupProgress value)
        {
            if (Volatile.Read(ref ended)) return;
            Volatile.Write(ref latest, value);
            if (context is null) update(value);
            else context.Post(_ => { if (!Volatile.Read(ref ended)) update(value); }, null);
        }
        public void Dispose() => Volatile.Write(ref ended, true);
    }

    public void Dispose()
    {
        L.PropertyChanged -= Localized;
        lifetime.Cancel();
        lifetime.Dispose();
        (service as IDisposable)?.Dispose();
    }
}