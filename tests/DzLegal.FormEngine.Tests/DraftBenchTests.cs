using DzLegal.Harness;

namespace DzLegal.FormEngine.Tests;

public sealed class DraftBenchTests : IDisposable
{
    private readonly DirectoryInfo _out = Directory.CreateTempSubdirectory("dzlegal-bench-");

    [Fact]
    public async Task BenchRunsEveryCaseAndWritesASummary()
    {
        await DraftBench.RunAsync(
            new BenchOptions
            {
                Models = ["stub"],
                CasesFolder = Path.Combine(AppContext.BaseDirectory, "drafting-cases"),
                OutFolder = _out.FullName,
                DataRoot = TestData.DocumentTypesRoot,
            },
            TextWriter.Null);

        var summary = await File.ReadAllTextAsync(Path.Combine(_out.FullName, "summary.md"));
        var cases = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "drafting-cases"), "*.json");
        Assert.True(cases.Length >= 5);
        foreach (var c in cases)
        {
            var id = Path.GetFileNameWithoutExtension(c);
            Assert.Contains($"| stub | {id} |", summary, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(_out.FullName, "stub", id + ".md")), id);
        }
    }

    [Fact]
    public void CasesExtendTheirBase()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "drafting-cases");
        var french = DraftingCaseLoader.Load(Path.Combine(folder, "02-debt-french-notes.json"));
        Assert.Equal("محكمة سيدي امحمد", french.Inputs["court.name"]);
        Assert.StartsWith("Le 15/01/2025", french.Inputs["notes.facts"], StringComparison.Ordinal);
    }

    public void Dispose() => _out.Delete(recursive: true);
}
