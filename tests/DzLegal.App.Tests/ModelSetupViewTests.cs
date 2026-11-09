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

internal sealed class FakeModelSetup : ILocalModelSetup
{
    public IReadOnlyList<SetupModel> Models => SupportedModels.All.Select(m => m.Info).ToArray();
    public ModelSetupProgress Result { get; set; } = new(ModelSetupStage.Available, "gemma4:e4b");
    public Func<IProgress<ModelSetupProgress>, CancellationToken, Task>? Download { get; set; }
    public int Downloads { get; private set; }
    public bool PreferTemplates { get; private set; }
    public void RememberTemplates() => PreferTemplates = true;
    public void RememberLocal() => PreferTemplates = false;
    public Task<ModelSetupProgress> InspectAsync(CancellationToken cancellationToken) => Task.FromResult(Result);
    public Task DownloadAsync(string model, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        Downloads++;
        return Download?.Invoke(progress, cancellationToken) ?? Task.CompletedTask;
    }
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
            foreach (var stage in Enum.GetValues<ModelSetupStage>())
            {
                app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
                var host = Vm.Create().Filled();
                host.IsDarkTheme = dark;
                var loc = host.L;
                loc.SetLanguage(language);
                using var vm = new ModelSetupViewModel(loc, new FakeModelSetup(), () => { }, () => Task.CompletedTask)
                {
                    SelectedModel = SupportedModels.Find("gemma4:e4b").Info,
                    IsExpanded = true,
                    IsBusy = stage is ModelSetupStage.Checking or ModelSetupStage.Downloading or ModelSetupStage.Verifying or ModelSetupStage.Testing or ModelSetupStage.Preparing,
                    Progress = new ModelSetupProgress(stage, "gemma4:e4b", 3_000_000_000, 6_583_656_505, 2_000_000, stage == ModelSetupStage.DiskFull ? 1_000_000_000 : 12_000_000_000, 8_731_140_153),
                };
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
                Assert.NotEmpty(view.FindControl<TextBlock>("ModelSetupTerms")!.Text!);
                Assert.Equal(vm.StatusText, view.FindControl<TextBlock>("ModelSetupStatus")!.Text);
                if (Environment.GetEnvironmentVariable("DZLEGAL_MODEL_SETUP_SCREENSHOTS") is { Length: > 0 } folder)
                {
                    Directory.CreateDirectory(folder);
                    using var frame = window.CaptureRenderedFrame();
                    Assert.NotNull(frame);
                    Assert.Equal(dimensions.Item1, frame.PixelSize.Width);
                    frame.Save(Path.Combine(folder, $"{stage}-{language}-{(dark ? "dark" : "light")}-{dimensions.Item1}x{dimensions.Item2}.png"), new PngBitmapEncoderOptions());
                }
                window.Close();
            }
        }
        finally { app.RequestedThemeVariant = originalTheme; }
    }
}