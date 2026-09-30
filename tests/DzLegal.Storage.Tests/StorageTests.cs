using DzLegal.Domain;
using DzLegal.Storage;

namespace DzLegal.Storage.Tests;

public sealed class StorageTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("dzlegal-storage-");

    private static DraftDocument SampleDraft() => new()
    {
        DocumentTypeId = "requete-introductive",
        DefinitionVersion = "0.1.0",
        Inputs = new() { ["plaintiff.lastName"] = "بن علي", ["plaintiff.isLegalEntity"] = "false" },
        Sections = [new DraftSection { Id = "facts", Text = "حيث إن العارض..." }],
        GeneratedBy = "stub",
    };

    [Fact]
    public async Task DraftRoundTripsAndKeepsArabicReadable()
    {
        var path = Path.Combine(_dir.FullName, "d" + DraftFile.Extension);
        await DraftFile.SaveAsync(SampleDraft(), path);

        Assert.Contains("بن علي", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        var loaded = await DraftFile.LoadAsync(path);
        Assert.Equal("requete-introductive", loaded.DocumentTypeId);
        Assert.Equal("بن علي", loaded.Inputs["plaintiff.lastName"]);
        Assert.Equal("حيث إن العارض...", loaded.FindSection("facts")!.Text);
    }

    [Fact]
    public async Task EvaluationRecordsAreStoredLocally()
    {
        var store = new EvaluationStore(Path.Combine(_dir.FullName, "evaluations"));
        var record = new EvaluationRecord
        {
            DocumentTypeId = "requete-introductive",
            GeneratedDraft = SampleDraft(),
            FinalDraft = SampleDraft(),
            Verdict = DraftVerdict.MinorEdits,
            MinutesSaved = 25,
        };

        var path = await store.SaveAsync(record);

        Assert.StartsWith(_dir.FullName, path, StringComparison.Ordinal);
        var loaded = Assert.Single(store.LoadAll());
        Assert.Equal(record.Id, loaded.Id);
        Assert.Equal(DraftVerdict.MinorEdits, loaded.Verdict);
        Assert.Equal(25, loaded.MinutesSaved);
    }

    public void Dispose() => _dir.Delete(recursive: true);
}
