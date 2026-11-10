using System.Security.Cryptography;
using System.Text;
using DzLegal.App.Services.LocalModels;

namespace DzLegal.App.Tests;

public class ModelAcquisitionTests
{
    private sealed class Capture : IProgress<ModelSetupProgress>
    {
        public List<ModelSetupProgress> Values { get; } = [];
        public void Report(ModelSetupProgress value) => Values.Add(value);
    }
    private sealed class FragmentedStream(byte[] data) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 3)], cancellationToken);
    }
    private static SupportedModel Tiny()
    {
        var blob = new ModelBlob(new string('a', 64), 100);
        return new SupportedModel("fictitious:1", "", "", [blob], "Fictitious test terms", 1);
    }

    [Theory]
    [InlineData(0UL, "gemma3:4b")]
    [InlineData(17179869183UL, "gemma3:4b")]
    [InlineData(17179869184UL, "gemma4:e4b")]
    [InlineData(34359738368UL, "gemma4:e4b")]
    public void TheRecommendationUsesInstalledMemoryAtTheExactBoundary(ulong memory, string expected) =>
        Assert.Equal(expected, WindowsComputerResources.RecommendedModel(memory));

    [Fact]
    public void TheTwoModelChoicesPinEveryKnownManifestAndItsEntireBlobClosure()
    {
        Assert.Equal(["gemma3:4b", "gemma4:e4b"], SupportedModels.All.Select(m => m.Name));
        foreach (var model in SupportedModels.Known)
        {
            Assert.Equal(model.ManifestSha256, Convert.ToHexStringLower(SHA256.HashData(model.Manifest)));
            Assert.NotEmpty(model.Terms);
            Assert.All(model.Blobs, blob => { Assert.Equal(64, blob.Sha256.Length); Assert.True(blob.Size > 0); });
        }
        Assert.Equal(3350272403, SupportedModels.Find("gemma3:4b").Info.Bytes);
        Assert.Equal(3349593375, SupportedModels.Versions("gemma3:4b").Single(m => m.ManifestSha256 == "c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291").Info.Bytes);
        Assert.Equal(3, SupportedModels.Known.Count);
        Assert.Equal(3, SupportedModels.Known.Select(m => m.ManifestSha256).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(6583656505, SupportedModels.Find("gemma4:e4b").Info.Bytes);
    }

    [Fact]
    public void DuplicateAndReorderedCountersNeverDoubleCountOrMoveBackwards()
    {
        var parser = new PullProgress(Tiny());
        var digest = "sha256:" + new string('a', 64);
        Assert.Equal(60, parser.Parse($$"""{"digest":"{{digest}}","total":100,"completed":60}""").Completed);
        Assert.Equal(60, parser.Parse($$"""{"digest":"{{digest}}","total":100,"completed":20}""").Completed);
        Assert.Equal(60, parser.Parse($$"""{"digest":"{{digest}}","total":100,"completed":60}""").Completed);
        Assert.Equal(90, parser.Parse("{\"digest\":\"sha256:model\",\"total\":100,\"completed\":90}").Completed);
    }

    [Theory]
    [InlineData("{\"digest\":\"sha256:model\",\"total\":101,\"completed\":1}")]
    [InlineData("{\"digest\":\"sha256:model\",\"total\":100,\"completed\":-1}")]
    public void UnknownOrOversizedTransfersFailClosed(string json) =>
        Assert.Equal(ModelSetupStage.VerificationFailed, Assert.Throws<ModelSetupException>(() => new PullProgress(Tiny()).Parse(json)).Stage);

    [Fact]
    public async Task SplitUtf8AndJsonChunksAreReadAsWholeProgressRecords()
    {
        await using var input = new FragmentedStream(Encoding.UTF8.GetBytes("{\"status\":\"جارٍ\"}\n{\"digest\":\"sha256:model\",\"total\":100,\"completed\":80}\n{\"status\":\"success\"}\n"));
        var capture = new Capture();
        var guards = 0;
        await LocalModelSetupService.ReadPullAsync(input, Tiny(), capture, () => guards++, CancellationToken.None);
        Assert.Equal(3, guards);
        Assert.Equal(80, capture.Values[1].Completed);
        Assert.Equal(ModelSetupStage.Verifying, capture.Values[^1].Stage);
    }

    [Fact]
    public async Task AnInterruptedStreamCannotBecomeReady()
    {
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes("{\"status\":\"pulling manifest\"}\n"));
        var failure = await Assert.ThrowsAsync<ModelSetupException>(() => LocalModelSetupService.ReadPullAsync(input, Tiny(), new Capture(), () => { }, CancellationToken.None));
        Assert.Equal(ModelSetupStage.ConnectionFailed, failure.Stage);
    }

    [Fact]
    public async Task TheDiskGuardAbortsBeforeAnotherProgressUpdate()
    {
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes("{\"status\":\"pulling manifest\"}\n"));
        var capture = new Capture();
        var failure = await Assert.ThrowsAsync<ModelSetupException>(() => LocalModelSetupService.ReadPullAsync(input, Tiny(), capture,
            () => throw new ModelSetupException(ModelSetupStage.DiskFull), CancellationToken.None));
        Assert.Equal(ModelSetupStage.DiskFull, failure.Stage);
        Assert.Empty(capture.Values);
        Assert.Equal(2147483648, LocalModelSetupService.DiskReserveBytes);
    }
    [Fact]
    public void AChangedModelRevisionFailsWithAnActionableVersionStatus()
    {
        var failure = Assert.Throws<ModelSetupException>(() => new PullProgress(Tiny()).Parse(
            "{\"digest\":\"sha256:unexpected\",\"total\":100,\"completed\":1}"));
        Assert.Equal(ModelSetupStage.ModelVersionChanged, failure.Stage);
    }

    [Fact]
    public void TheRegistryRevisionIsAcceptedOnlyByItsOwnReviewedInventory()
    {
        var current = SupportedModels.Find("gemma3:4b");
        var legacy = SupportedModels.Versions(current.Name).Single(m => m != current);
        var weight = current.Blobs.Single(b => b.Size > 2_000_000_000);
        var line = $$"""{"digest":"sha256:{{weight.Sha256}}","total":{{weight.Size}},"completed":1024}""";
        Assert.Equal(1024, new PullProgress(current).Parse(line).Completed);
        Assert.Equal(ModelSetupStage.ModelVersionChanged, Assert.Throws<ModelSetupException>(() => new PullProgress(legacy).Parse(line)).Stage);
    }

}