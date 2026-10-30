using DzLegal.Storage;

namespace DzLegal.Storage.Tests;

public sealed class RecentCasesTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("dzlegal-recent-");
    [Fact]
    public void LimitOrderDedupeAndAtomicWriteKeepOnlyDisplayMetadata()
    {
        var moment = new DateTimeOffset(2026, 10, 29, 0, 0, 0, TimeSpan.Zero);
        var store = new RecentCasesStore(_folder.FullName, () => moment);
        for (var i = 0; i < 15; i++)
        {
            moment = moment.AddMinutes(1);
            Assert.True(store.RecordSaved(Path.Combine(_folder.FullName, $"fictitious-{i}.dzdraft.json"), $"موضوع افتراضي {i}"));
        }
        var entries = store.Load();
        Assert.Equal(10, entries.Count);
        Assert.Equal("fictitious-14.dzdraft.json", entries[0].FileName);
        Assert.Equal("fictitious-5.dzdraft.json", entries[^1].FileName);
        moment = moment.AddMinutes(1);
        Assert.True(store.RecordOpened(entries[3].Path.ToUpperInvariant(), "موضوع محدّث افتراضي"));
        Assert.Equal(10, store.Load().Count);
        Assert.Equal("موضوع محدّث افتراضي", store.Load()[0].Subject);
        Assert.False(File.Exists(store.FilePath + ".tmp"));
        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(store.FilePath));
        Assert.Equal(new[] { "fileName", "lastOpened", "path", "subject" }, json.RootElement[0].EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }
    [Theory]
    [InlineData("")][InlineData("{broken")][InlineData("null")][InlineData("{}")]
    public void CorruptedFileStartsEmpty(string content)
    {
        var store = new RecentCasesStore(_folder.FullName);
        Assert.Empty(store.Load());
        File.WriteAllText(store.FilePath, content);
        Assert.Empty(store.Load());
        Assert.True(store.RecordOpened(Path.Combine(_folder.FullName, "fixture.dzdraft.json"), "fictional"));
        Assert.Single(store.Load());
    }
    [Fact]
    public void MissingCasesAreRetainedAndRemovingOrClearingNeverDeletesFiles()
    {
        var path = Path.Combine(_folder.FullName, "fictitious.dzdraft.json");
        File.WriteAllText(path, "fixture");
        var store = new RecentCasesStore(_folder.FullName);
        Assert.True(store.RecordOpened(path, "fictional"));
        Assert.True(store.Remove(path));
        Assert.True(File.Exists(path));
        Assert.Empty(store.Load());
        Assert.True(store.RecordOpened(path, "fictional"));
        File.Delete(path);
        Assert.Single(store.Load());
        File.WriteAllText(path, "fixture");
        Assert.True(store.Clear());
        Assert.Empty(store.Load());
        Assert.True(File.Exists(path));
    }
    [Fact]
    public void UnwritableRootAndRelativePathsFailWithoutCrashing()
    {
        var path = Path.Combine(_folder.FullName, "a-file");
        File.WriteAllText(path, "fixture");
        var store = new RecentCasesStore(path);
        Assert.False(store.RecordOpened(Path.Combine(_folder.FullName, "fixture.dzdraft.json"), "fictional"));
        Assert.False(store.RecordSaved("relative.dzdraft.json", "fictional"));
        Assert.False(store.Clear());
        Assert.Empty(store.Load());
    }
    public void Dispose() => _folder.Delete(true);
}
