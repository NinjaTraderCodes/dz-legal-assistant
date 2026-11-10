using DzLegal.App;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public class ManualAnalysisTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TheDesktopRequiresTheAnalyseButtonEvenWithOldAutomaticSettings(bool legacySetting)
    {
        var parent = Path.Combine(AppPaths.Default.Root, "manual-analysis-tests");
        var root = Path.Combine(parent, Guid.NewGuid().ToString("N"));
        var settings = new AppSettings { DefaultProvider = "stub", AnalyseInBackground = legacySetting };
        var vm = Composition.CreateMainViewModel(settings, new FakeFiles(), new AppPaths(root), new Loc()).Filled();
        try
        {
            var recorder = Analysis.Model();
            vm.SelectedProvider = new ProviderOption("fictitious-local", _ => "fictitious local", () => recorder);
            await vm.CheckModelStatusAsync();
            Assert.False(vm.AnalyseInBackground);
            Assert.Equal(ModelState.Ready, vm.ModelStatus.State);
            vm.SelectStepCommand.Execute(vm.FieldGroups.Single(g => g.Id == "facts"));
            vm.SelectStepCommand.Execute(vm.FieldGroups.Single(g => g.Id == "grounds"));
            Assert.Empty(recorder.Requests);
            Assert.Null(vm.Analysis);
            Assert.False(vm.IsAnalysisAutomatic);

            await vm.AnalyseGroundsCommand.ExecuteAsync(null);
            Assert.Equal(2, recorder.Requests.Count);
            Assert.NotNull(vm.Analysis);
            Assert.True(vm.NeedsClaimConfirmation);
            Assert.Empty(vm.SelectedArticles);
        }
        finally
        {
            vm.Dispose();
            if (Directory.Exists(root))
            {
                Assert.Equal(Path.GetFullPath(parent), Path.GetDirectoryName(Path.GetFullPath(root)));
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void TheDefaultAndShippedDesktopSettingsUseManualAnalysis()
    {
        Assert.False(new AppSettings().AnalyseInBackground);
        var file = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        Assert.True(File.Exists(file));
        Assert.False(AppSettings.Load(file).AnalyseInBackground);
    }
}
