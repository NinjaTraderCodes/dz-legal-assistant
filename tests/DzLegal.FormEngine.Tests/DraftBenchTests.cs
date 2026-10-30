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
            Assert.Contains($"| stub | {id}", summary, StringComparison.Ordinal); // «(robustness)» follows the Darija and French cases
            Assert.True(File.Exists(Path.Combine(_out.FullName, "stub", id + ".md")), id);
        }
    }

    [Fact]
    public async Task SkeletonSampleWritesBothVersionsPerCase()
    {
        await SkeletonSample.RunAsync(
            new BenchOptions
            {
                Models = ["stub"],
                CasesFolder = Path.Combine(AppContext.BaseDirectory, "drafting-cases"),
                OutFolder = _out.FullName,
                DataRoot = TestData.DocumentTypesRoot,
                Case = "01-debt-arabic",
            },
            TextWriter.Null);

        var file = await File.ReadAllTextAsync(Path.Combine(_out.FullName, "stub", "01-debt-arabic.md"));
        Assert.Contains("## A. Today", file, StringComparison.Ordinal);
        Assert.Contains("## B. Skeleton-first", file, StringComparison.Ordinal);
        Assert.Contains("| stub | 01-debt-arabic |", await File.ReadAllTextAsync(Path.Combine(_out.FullName, "summary.md")), StringComparison.Ordinal);
    }

    [Fact]
    public void CasesExtendTheirBase()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "drafting-cases");
        var french = DraftingCaseLoader.Load(Path.Combine(folder, "02-debt-french-notes.json"));
        Assert.Equal("محكمة سيدي امحمد", french.Inputs["court.name"]);
        Assert.StartsWith("Articles 106 et 107", french.Inputs["notes.grounds"], StringComparison.Ordinal);
        Assert.StartsWith("بتاريخ 15 جانفي 2025", french.Inputs["notes.facts"], StringComparison.Ordinal);
    }

    public void Dispose() => _out.Delete(recursive: true);
}
