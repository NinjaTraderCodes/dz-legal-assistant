using System.Text.Json;

namespace DzLegal.App.Tests;

public sealed class PhaseOneSampleTests
{
    [Fact]
    public async Task SuppliedDebtCaseCompletesAndExportsInTemplateModeWithoutAnalysis()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DzLegal.Web", "Samples", "debt.json"));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var inputs = json.RootElement.GetProperty("inputs").EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
        var files = new FakeFiles();
        using var model = Vm.Create(files, noLocalModel: true);
        var sample = SampleCase.LoadAll(Path.GetDirectoryName(path)!).Single(item => item.Id == "debt");
        await model.OpenSampleAsync(sample);
        var originalInputs = model.CurrentInputs().OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        Assert.All(inputs, pair => Assert.Equal(pair.Value, model.CurrentInputs()[pair.Key]));
        var guide = Assert.IsType<SampleGuideViewModel>(model.SampleGuide);
        Assert.True(guide.NeedsLocalModel);
        Assert.True(guide.Tasks[1].NeedsModel);
        Assert.True(guide.Tasks[2].NeedsModel);
        Assert.All(guide.Tasks.Where(task => task.Number != 5), task => Assert.False(task.IsComplete));
        Assert.True(guide.Tasks[4].IsComplete);
        guide.Tasks[0].ShowMeCommand.Execute(null);
        Assert.True(guide.Tasks[0].IsComplete);

        await model.GenerateCommand.ExecuteAsync(null);

        Assert.Null(model.Analysis);
        Assert.False(model.IsClaimConfirmed);
        Assert.True(model.HasGeneratedDraft);
        Assert.True(model.IsAdmissible);
        Assert.True(model.HasExportBlockers);
        var grounds = model.Sections.Single(section => section.Id == "grounds");
        Assert.Contains("(...)", grounds.Text, StringComparison.Ordinal);
        await model.ExportPdfCommand.ExecuteAsync(null);
        await model.ExportWordCommand.ExecuteAsync(null);
        Assert.Empty(files.SavedByKind);

        grounds.Text = grounds.Text.Replace("(...)", "وحيث إن المدعى عليه لم ينفذ التزامه برد مبلغ القرض رغم حلول الأجل والإعذار.", StringComparison.Ordinal);

        Assert.False(model.HasExportBlockers, model.ExportBlockers);
        Assert.Equal(originalInputs, model.CurrentInputs().OrderBy(pair => pair.Key, StringComparer.Ordinal));
        Assert.DoesNotContain(model.Sections, section => section.NeedsReview);
        await model.ExportPdfCommand.ExecuteAsync(null);
        await model.ExportWordCommand.ExecuteAsync(null);
        Assert.Contains(SaveKind.Pdf, files.SavedByKind.Keys);
        Assert.Contains(SaveKind.Word, files.SavedByKind.Keys);
        Assert.True(guide.IsDone);
        Assert.False(guide.Tasks[1].IsComplete);
        Assert.False(guide.Tasks[2].IsComplete);
        Assert.True(model.DraftWatermark);
    }
}
