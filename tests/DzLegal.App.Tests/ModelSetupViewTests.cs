using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.App.Services.LocalModels;
using DzLegal.App.Views;

namespace DzLegal.App.Tests;

internal sealed class FakeModelSetup : ILocalModelSetup, ILocalModelPacks
{
    public IReadOnlyList<SetupModel> Models => SupportedModels.All.Select(m => m.Info).ToArray();
    public ModelSetupProgress Result { get; set; } = new(ModelSetupStage.Available, "gemma4:e4b");
    public Func<IProgress<ModelSetupProgress>, CancellationToken, Task>? Download { get; set; }
    public Func<IProgress<ModelSetupProgress>, CancellationToken, Task>? Import { get; set; }
    public Func<IProgress<ModelSetupProgress>, CancellationToken, Task>? Export { get; set; }
    public Func<CancellationToken, Task<ModelSetupProgress>>? Inspect { get; set; }
    public Func<string, ModelSetupProgress> SelectedResult { get; set; } = model => new ModelSetupProgress(ModelSetupStage.Available, model);
    public Func<string, CancellationToken, Task<ModelSetupProgress>>? InspectSelection { get; set; }
    public Task<ModelSetupProgress> InspectModelAsync(string model, CancellationToken cancellationToken) => InspectSelection?.Invoke(model, cancellationToken) ?? Task.FromResult(SelectedResult(model));
    public int Imports { get; private set; }
    public int Exports { get; private set; }
    public string? ImportedModel { get; private set; }
    public int Downloads { get; private set; }
    public bool PreferTemplates { get; private set; }
    public void RememberTemplates() => PreferTemplates = true;
    public void RememberLocal() => PreferTemplates = false;
    public Task<ModelSetupProgress> InspectAsync(CancellationToken cancellationToken) => Inspect?.Invoke(cancellationToken) ?? Task.FromResult(Result);
    public Task<SetupModel> ReadPackAsync(string path, CancellationToken cancellationToken) => Task.FromResult(Models.Single(m => m.Name == "gemma4:e4b"));
    public Task ImportAsync(string path, string expectedModel, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        Imports++;
        ImportedModel = expectedModel;
        if (Import is not null) return Import(progress, cancellationToken);
        progress.Report(new ModelSetupProgress(ModelSetupStage.Ready, expectedModel));
        return Task.CompletedTask;
    }
    public Task ExportAsync(string model, string path, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        Exports++;
        if (Export is not null) return Export(progress, cancellationToken);
        progress.Report(new ModelSetupProgress(ModelSetupStage.Exported, model, FilesVerified: true));
        return Task.CompletedTask;
    }
    public Task DownloadAsync(string model, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        Downloads++;
        return Download?.Invoke(progress, cancellationToken) ?? Task.CompletedTask;
    }
}

internal sealed class FakePackFiles : IModelPackFiles
{
    public Task<string?> PickImportAsync() => Task.FromResult<string?>("fictitious-model.dzmodel");
    public Task<string?> PickExportAsync(string model) => Task.FromResult<string?>("fictitious-model.dzmodel");
}

public class ModelSetupViewTests
{
    [Fact]
    public async Task DiscoveryAndNotNowDoNotDownloadAndTemplatesRemainUsable()
    {
        var service = new FakeModelSetup();
        var templates = false;
        using var vm = new ModelSetupViewModel(new Loc(), service, () => templates = true, () => Task.CompletedTask);
        await vm.RefreshAsync();
        Assert.Equal(0, service.Downloads);
        vm.NotNowCommand.Execute(null);
        Assert.True(templates);
        Assert.True(service.PreferTemplates);
        Assert.False(vm.IsExpanded);
        Assert.Equal(0, service.Downloads);
    }

    [Fact]
    public async Task NotNowKeepsAnAuthorisedDownloadRunningAndCancelIsExplicit()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeModelSetup { Download = async (_, ct) => { started.SetResult(); await Task.Delay(Timeout.Infinite, ct); } };
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask);
        var download = vm.DownloadCommand.ExecuteAsync(null);
        await started.Task;
        vm.NotNowCommand.Execute(null);
        Assert.True(vm.IsBusy);
        Assert.False(vm.IsExpanded);
        vm.DownloadCancelCommand.Execute(null);
        await download;
        Assert.Equal(ModelSetupStage.Cancelled, vm.Progress.Stage);
        Assert.False(vm.IsBusy);
        Assert.Equal(1, service.Downloads);
    }

    [Fact]
    public async Task RawServerMessagesAreNeverDisplayed()
    {
        var service = new FakeModelSetup { Download = (_, _) => throw new HttpRequestException("private diagnostic data") };
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask);
        await vm.DownloadCommand.ExecuteAsync(null);
        Assert.Equal(ModelSetupStage.EngineFailed, vm.Progress.Stage);
        Assert.DoesNotContain("private diagnostic", vm.StatusText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ModelSetupStage.Ready)]
    [InlineData(ModelSetupStage.MemoryLow)]
    [InlineData(ModelSetupStage.Available)]
    public async Task SwitchingModelsChecksReadinessAgainInsteadOfAssumingADownload(ModelSetupStage gemma4State)
    {
        var service = new FakeModelSetup
        {
            Result = new ModelSetupProgress(ModelSetupStage.Ready, "gemma3:4b"),
            SelectedResult = name => new ModelSetupProgress(name == "gemma4:e4b" ? gemma4State : ModelSetupStage.Ready, name),
        };
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask);
        await vm.RefreshAsync();
        Assert.True(vm.IsReady);
        vm.SelectedModel = vm.Models.Single(m => m.Name == "gemma4:e4b");
        Assert.Equal(gemma4State, vm.Progress.Stage);
        Assert.Equal(gemma4State == ModelSetupStage.Available, vm.ShowDownload);
        vm.SelectedModel = vm.Models.Single(m => m.Name == "gemma3:4b");
        Assert.True(vm.IsReady);
        Assert.False(vm.ShowDownload);
        Assert.True(vm.CanUseLocal);
        Assert.Equal(0, service.Downloads);
    }

    [Fact]
    public async Task AnOlderSelectionCheckCannotReplaceTheCurrentModelsResult()
    {
        var pending = new TaskCompletionSource<ModelSetupProgress>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeModelSetup
        {
            Result = new ModelSetupProgress(ModelSetupStage.Ready, "gemma3:4b"),
            InspectSelection = (name, _) => name == "gemma4:e4b" ? pending.Task : Task.FromResult(new ModelSetupProgress(ModelSetupStage.Ready, name)),
        };
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask);
        await vm.RefreshAsync();
        vm.SelectedModel = vm.Models.Single(m => m.Name == "gemma4:e4b");
        Assert.True(vm.IsBusy);
        var earlierCheck = vm.SelectionTask;
        vm.SelectedModel = vm.Models.Single(m => m.Name == "gemma3:4b");
        pending.SetResult(new ModelSetupProgress(ModelSetupStage.MemoryLow, "gemma4:e4b"));
        await earlierCheck;
        Assert.Equal("gemma3:4b", vm.Progress.Model);
        Assert.True(vm.CanUseLocal);
        Assert.False(vm.ShowDownload);
    }

    [Fact]
    public async Task ChoosingAPackShowsItsTermsAndNeedsASeparateInstallAction()
    {
        var service = new FakeModelSetup();
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask, new FakePackFiles());
        await vm.ChoosePackCommand.ExecuteAsync(null);
        Assert.Equal(ModelSetupStage.ImportSelected, vm.Progress.Stage);
        Assert.Equal("gemma4:e4b", vm.SelectedModel.Name);
        Assert.Equal(service.Models.Single(m => m.Name == vm.SelectedModel.Name).Terms, vm.TermsText);
        Assert.Equal(0, service.Imports);
        Assert.Equal(0, service.Downloads);
        Assert.True(vm.CanInstallPack);
        Assert.False(vm.ShowDownload);
        await vm.InstallPackCommand.ExecuteAsync(null);
        Assert.Equal(1, service.Imports);
        Assert.Equal("gemma4:e4b", service.ImportedModel);
        Assert.True(vm.CanUseLocal);
    }

    [Fact]
    public async Task ExportIsExplicitAndTemplatesDoNotCancelIt()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeModelSetup { Export = async (_, ct) => { started.SetResult(); await Task.Delay(Timeout.Infinite, ct); } };
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask, new FakePackFiles())
        { Progress = new ModelSetupProgress(ModelSetupStage.Ready, "gemma3:4b") };
        Assert.True(vm.CanExport);
        Assert.Equal(0, service.Exports);
        var export = vm.SavePackCommand.ExecuteAsync(null);
        await started.Task;
        Assert.True(vm.CanCancel);
        vm.NotNowCommand.Execute(null);
        Assert.True(vm.IsBusy);
        vm.CancelCommand.Execute(null);
        await export;
        Assert.Equal(ModelSetupStage.Cancelled, vm.Progress.Stage);
        Assert.Equal(1, service.Exports);
        Assert.True(vm.CanUseLocal);
        Assert.False(vm.ShowDownload);
        Assert.Equal(0, service.Downloads);
    }

    [Fact]
    public async Task VerifiedModelFilesCanBeSharedWithoutEnoughMemoryToLoadTheModel()
    {
        var service = new FakeModelSetup();
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask, new FakePackFiles())
        { Progress = new ModelSetupProgress(ModelSetupStage.MemoryLow, "gemma3:4b", FilesVerified: true) };
        Assert.False(vm.CanUseLocal);
        Assert.True(vm.CanExport);
        Assert.False(vm.ShowDownload);
        await vm.SavePackCommand.ExecuteAsync(null);
        Assert.Equal(1, service.Exports);
        Assert.Equal(ModelSetupStage.Exported, vm.Progress.Stage);
        Assert.False(vm.CanUseLocal);
        Assert.True(vm.CanExport);
        Assert.False(vm.ShowDownload);
        Assert.Equal(0, service.Downloads);
    }

    [Fact]
    public async Task ExportFailuresExposeOnlyLocalisedCategories()
    {
        var service = new FakeModelSetup { Export = (_, _) => throw new IOException("private diagnostic content") };
        using var vm = new ModelSetupViewModel(new Loc(), service, () => { }, () => Task.CompletedTask, new FakePackFiles())
        { Progress = new ModelSetupProgress(ModelSetupStage.Ready, "gemma3:4b") };
        await vm.SavePackCommand.ExecuteAsync(null);
        Assert.Equal(ModelSetupStage.ExportFailed, vm.Progress.Stage);
        Assert.True(vm.CanUseLocal);
        Assert.DoesNotContain("private diagnostic", vm.StatusText, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task AReadyResultUpdatesTheLiveCardWithoutOfferingAnotherDownload()
    {
        var result = new TaskCompletionSource<ModelSetupProgress>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeModelSetup { Inspect = _ => result.Task };
        var host = Vm.Create().Filled();
        using var vm = new ModelSetupViewModel(host.L, service, () => { }, () => Task.CompletedTask);
        host.ModelSetup = vm;
        var window = new MainWindow { Width = 1366, Height = 768, DataContext = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var checking = vm.RefreshAsync();
        await Task.Run(() => result.SetResult(new ModelSetupProgress(ModelSetupStage.Ready, "gemma4:e4b")));
        await checking;
        vm.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        var view = window.FindControl<ModelSetupView>("ModelSetupCard")!;
        Assert.Equal(ModelSetupStage.Ready, vm.Progress.Stage);
        Assert.True(vm.IsReady);
        Assert.False(vm.ShowDownload);
        Assert.False(view.FindControl<Button>("ModelSetupDownload")!.IsVisible);
        Assert.Equal(vm.StatusText, view.FindControl<TextBlock>("ModelSetupStatus")!.Text);
        Assert.Equal("gemma4:e4b", vm.SelectedModel.Name);
        window.Close();
    }

    [AvaloniaFact]
    public void EverySetupStateFitsBothLanguagesThemesAndScreenSizes()
    {
        var app = Application.Current!;
        var originalTheme = app.RequestedThemeVariant;
        try
        {
            foreach (var language in new[] { "ar", "en" })
            foreach (var dark in new[] { false, true })
            foreach (var dimensions in new[] { (1366, 768), (1920, 1080) })
            foreach (var (stage, modelName) in Enum.GetValues<ModelSetupStage>().Select(s => (s, "gemma4:e4b"))
                .Concat([(ModelSetupStage.Available, "gemma3:4b"), (ModelSetupStage.Ready, "gemma3:4b")]))
            {
                app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
                var host = Vm.Create().Filled();
                host.IsDarkTheme = dark;
                var loc = host.L;
                loc.SetLanguage(language);
                var setup = new FakeModelSetup();
                var info = SupportedModels.Find(modelName).Info;
                using var vm = new ModelSetupViewModel(loc, setup, () => { }, () => Task.CompletedTask, new FakePackFiles())
                {
                    SelectedModel = info,
                    IsExpanded = true,
                    IsBusy = stage is ModelSetupStage.Checking or ModelSetupStage.Downloading or ModelSetupStage.Verifying or ModelSetupStage.Testing or ModelSetupStage.Preparing or ModelSetupStage.Importing or ModelSetupStage.Exporting,
                    Progress = new ModelSetupProgress(stage, modelName, 3_000_000_000, info.Bytes, 2_000_000, stage == ModelSetupStage.DiskFull ? 1_000_000_000 : 12_000_000_000, info.Bytes + LocalModelSetupService.DiskReserveBytes, FilesVerified: stage == ModelSetupStage.MemoryLow),
                };
                if (stage is ModelSetupStage.ImportSelected or ModelSetupStage.ImportFailed)
                {
                    vm.ChoosePackCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                    if (stage == ModelSetupStage.ImportFailed)
                    {
                        setup.Import = (_, _) => throw new ModelSetupException(stage);
                        vm.InstallPackCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                    }
                }
                if (stage is ModelSetupStage.ExportFailed or ModelSetupStage.PackTooLarge or ModelSetupStage.Exported)
                {
                    vm.Progress = new ModelSetupProgress(ModelSetupStage.Ready, modelName);
                    if (stage != ModelSetupStage.Exported) setup.Export = (_, _) => throw new ModelSetupException(stage);
                    vm.SavePackCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                }
                Assert.Equal(stage, vm.Progress.Stage);
                host.ModelSetup = vm;
                var window = new MainWindow { Width = dimensions.Item1, Height = dimensions.Item2, DataContext = host };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var view = window.FindControl<ModelSetupView>("ModelSetupCard")!;
                Assert.True(window.FindControl<Border>("FormPanel")!.Bounds.Width > 0);
                Assert.Equal(language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, view.FlowDirection);
                Assert.DoesNotContain("[ModelSetup", vm.StatusText, StringComparison.Ordinal);
                if (language == "ar") Assert.Contains(vm.StatusText, c => c is >= '\u0600' and <= '\u06ff');
                var notNow = view.FindControl<Button>("ModelSetupNotNow")!;
                var point = notNow.TranslatePoint(default, window)!.Value;
                Assert.True(point.Y >= 0 && point.Y + notNow.Bounds.Height <= window.Bounds.Height);
                if (!vm.IsBusy)
                {
                    var terms = view.FindControl<ScrollViewer>("ModelSetupTermsScroll")!;
                    var termsPoint = terms.TranslatePoint(default, window)!.Value;
                    Assert.True(terms.IsEffectivelyVisible);
                    Assert.True(termsPoint.Y >= 0 && termsPoint.Y + terms.Bounds.Height < point.Y);
                    if (vm.ShowDownload)
                        Assert.True(termsPoint.Y + terms.Bounds.Height <= view.FindControl<Button>("ModelSetupDownload")!.TranslatePoint(default, window)!.Value.Y);
                    if (vm.CanInstallPack)
                        Assert.True(termsPoint.Y + terms.Bounds.Height <= view.FindControl<Button>("ModelSetupInstallPack")!.TranslatePoint(default, window)!.Value.Y);
                }
                Assert.False(vm.IsReady && view.FindControl<Button>("ModelSetupDownload")!.IsVisible);
                Assert.NotEmpty(view.FindControl<TextBlock>("ModelSetupTerms")!.Text!);
                Assert.Equal(vm.StatusText, view.FindControl<TextBlock>("ModelSetupStatus")!.Text);
                if (Environment.GetEnvironmentVariable("DZLEGAL_MODEL_SETUP_SCREENSHOTS") is { Length: > 0 } folder)
                {
                    Directory.CreateDirectory(folder);
                    using var frame = window.CaptureRenderedFrame();
                    Assert.NotNull(frame);
                    Assert.Equal(dimensions.Item1, frame.PixelSize.Width);
                    frame.Save(Path.Combine(folder, $"{(modelName == "gemma3:4b" ? "Gemma3" : "")}{stage}-{language}-{(dark ? "dark" : "light")}-{dimensions.Item1}x{dimensions.Item2}.png"), new PngBitmapEncoderOptions());
                }
                window.Close();
            }
        }
        finally { app.RequestedThemeVariant = originalTheme; }
    }
}