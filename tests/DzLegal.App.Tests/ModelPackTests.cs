using System.IO.Compression;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DzLegal.App.Services.LocalModels;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public class ModelPackTests
{
    private sealed class Resources : IComputerResources
    {
        public ulong InstalledMemoryBytes => 32UL * 1024 * 1024 * 1024;
        public ulong AvailableMemoryBytes { get; set; } = 16UL * 1024 * 1024 * 1024;
        public Func<long> Free { get; set; } = () => long.MaxValue;
        public long FreeDiskBytes(string path) => Free();
    }
    private sealed class Capture : IProgress<ModelSetupProgress>
    {
        public List<ModelSetupProgress> Values { get; } = [];
        public void Report(ModelSetupProgress value) => Values.Add(value);
    }
    private sealed class Fixture : IDisposable
    {
        private readonly string parent = Path.Combine(AppPaths.Default.Root, "model-pack-tests");
        public string Root { get; }
        public string Source => Path.Combine(Root, "source");
        public string Target => Path.Combine(Root, "models");
        public string Pack => Path.Combine(Root, "fictitious.dzmodel");
        public Resources Resources { get; } = new();
        public SupportedModel Model { get; }
        public List<byte[]> Data { get; } = [Encoding.UTF8.GetBytes("fictitious weights, never a real model"), Encoding.UTF8.GetBytes("fictitious metadata")];
        public ModelPackStore Store => new(Target, Resources, [Model]);
        public Fixture()
        {
            Root = Path.Combine(parent, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Source);
            var blobs = Data.Select(b => new ModelBlob(Convert.ToHexStringLower(SHA256.HashData(b)), b.Length)).ToArray();
            var manifest = Encoding.UTF8.GetBytes("{\"fixture\":\"fictitious-model\"}");
            Model = new SupportedModel("fictitious:1", Convert.ToHexStringLower(SHA256.HashData(manifest)), Convert.ToBase64String(manifest), blobs, "Fictitious terms", 1);
            for (var i = 0; i < Data.Count; i++) File.WriteAllBytes(Blob(Source, i), Data[i]);
        }
        public string Blob(string directory, int i = 0) => Path.Combine(directory, "sha256-" + Model.Blobs[i].Sha256);
        public void WritePack(string variant = "valid")
        {
            using var zip = ZipFile.Open(Pack, ZipArchiveMode.Create);
            Add("manifest.json", variant == "manifest" ? Encoding.UTF8.GetBytes("{}") : Model.Manifest);
            for (var i = 0; i < Data.Count; i++)
            {
                if (i == 0 && variant == "missing") continue;
                var name = "blobs/sha256-" + Model.Blobs[i].Sha256;
                if (i == 0)
                    name = variant switch { "traversal" => "../outside.bin", "backslash" => name.Replace('/', '\\'), "absolute" => "C:/outside.bin", "case" => name.ToUpperInvariant(), _ => name };
                var bytes = Data[i].ToArray();
                if (i == 0 && variant == "hash") bytes[0] ^= 1;
                if (i == 0 && variant == "size") bytes = [..bytes, 0];
                var entry = Add(name, bytes);
                if (i == 0 && variant == "symlink") entry.ExternalAttributes = unchecked((int)0xa1ff0000);
            }
            if (variant == "duplicate") Add("manifest.json", Model.Manifest);
            if (variant == "extra") Add("settings.json", Encoding.UTF8.GetBytes("{}"));
            ZipArchiveEntry Add(string name, byte[] bytes)
            {
                var entry = zip.CreateEntry(name, CompressionLevel.NoCompression);
                using var output = entry.Open();
                output.Write(bytes);
                return entry;
            }
        }
        public void Dispose()
        {
            Assert.Equal(Path.GetFullPath(parent), Path.GetDirectoryName(Path.GetFullPath(Root)));
            Directory.Delete(Root, recursive: true);
        }
    }

    [Fact]
    public async Task AVerifiedExportRoundTripsWithOnlyTheManifestAndExpectedBlobs()
    {
        using var fixture = new Fixture();
        var progress = new Capture();
        await fixture.Store.ExportAsync(fixture.Model, fixture.Source, fixture.Pack, progress, CancellationToken.None);
        using (var zip = ZipFile.OpenRead(fixture.Pack))
            Assert.Equal(new[] { "manifest.json" }.Concat(fixture.Model.Blobs.Select(b => "blobs/sha256-" + b.Sha256)), zip.Entries.Select(e => e.FullName));
        Assert.Equal(fixture.Model.Name, (await fixture.Store.ReadModelAsync(fixture.Pack, CancellationToken.None)).Name);
        await fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, progress, CancellationToken.None);
        Assert.Equal(fixture.Model.Manifest, await File.ReadAllBytesAsync(fixture.Model.ManifestPath(fixture.Target)));
        for (var i = 0; i < fixture.Data.Count; i++) Assert.Equal(fixture.Data[i], await File.ReadAllBytesAsync(fixture.Blob(Path.Combine(fixture.Target, "blobs"), i)));
        Assert.Contains(progress.Values, p => p.Stage == ModelSetupStage.Importing && p.Completed == fixture.Model.Info.Bytes);
        Assert.Contains(progress.Values, p => p.Stage == ModelSetupStage.Exporting && p.Completed == fixture.Model.Info.Bytes);
    }

    [Theory]
    [InlineData("manifest")]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("extra")]
    [InlineData("traversal")]
    [InlineData("absolute")]
    [InlineData("backslash")]
    [InlineData("case")]
    [InlineData("symlink")]
    [InlineData("size")]
    [InlineData("hash")]
    public async Task UnsafeOrChangedPacksCannotPublishAModel(string variant)
    {
        using var fixture = new Fixture();
        fixture.WritePack(variant);
        var error = await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None));
        Assert.Equal(ModelSetupStage.InvalidPack, error.Stage);
        Assert.False(File.Exists(fixture.Model.ManifestPath(fixture.Target)));
        Assert.False(File.Exists(Path.Combine(fixture.Root, "outside.bin")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Zip64CountsAreBoundedBeforeTheArchiveReaderAllocates(bool forged)
    {
        using var fixture = new Fixture();
        fixture.WritePack();
        var original = await File.ReadAllBytesAsync(fixture.Pack);
        var endOffset = original.Length - 22;
        var end = new byte[76];
        BinaryPrimitives.WriteUInt32LittleEndian(end, 0x06064b50);
        BinaryPrimitives.WriteUInt64LittleEndian(end.AsSpan(4), 44);
        BinaryPrimitives.WriteUInt16LittleEndian(end.AsSpan(12), 45);
        BinaryPrimitives.WriteUInt16LittleEndian(end.AsSpan(14), 45);
        var count = BinaryPrimitives.ReadUInt16LittleEndian(original.AsSpan(endOffset + 10));
        BinaryPrimitives.WriteUInt64LittleEndian(end.AsSpan(24), forged ? ulong.MaxValue : count);
        BinaryPrimitives.WriteUInt64LittleEndian(end.AsSpan(32), forged ? ulong.MaxValue : count);
        BinaryPrimitives.WriteUInt64LittleEndian(end.AsSpan(40), BinaryPrimitives.ReadUInt32LittleEndian(original.AsSpan(endOffset + 12)));
        BinaryPrimitives.WriteUInt64LittleEndian(end.AsSpan(48), BinaryPrimitives.ReadUInt32LittleEndian(original.AsSpan(endOffset + 16)));
        BinaryPrimitives.WriteUInt32LittleEndian(end.AsSpan(56), 0x07064b50);
        BinaryPrimitives.WriteUInt64LittleEndian(end.AsSpan(64), (ulong)endOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(end.AsSpan(72), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(endOffset + 16), uint.MaxValue);
        await File.WriteAllBytesAsync(fixture.Pack, [..original[..endOffset], ..end, ..original[endOffset..]]);
        if (forged)
            await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ReadModelAsync(fixture.Pack, CancellationToken.None));
        else
            await fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None);
        Assert.Equal(!forged, File.Exists(fixture.Model.ManifestPath(fixture.Target)));
    }

    [Fact]
    public async Task AChangedPackCannotSilentlyChangeTheModelWhoseTermsWereShown()
    {
        using var fixture = new Fixture();
        fixture.WritePack();
        Assert.Equal(fixture.Model.Name, (await fixture.Store.ReadModelAsync(fixture.Pack, CancellationToken.None)).Name);
        await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ImportAsync(fixture.Pack, "another:1", new Capture(), CancellationToken.None));
        Assert.False(Directory.Exists(fixture.Target));
    }

    [Fact]
    public async Task FailedVerificationKeepsThePreviousManifestAndExistingBlob()
    {
        using var fixture = new Fixture();
        fixture.WritePack("hash");
        var blobRoot = Path.Combine(fixture.Target, "blobs");
        Directory.CreateDirectory(blobRoot);
        var oldBytes = Encoding.UTF8.GetBytes("previous unverified blob");
        await File.WriteAllBytesAsync(fixture.Blob(blobRoot), oldBytes);
        var manifest = fixture.Model.ManifestPath(fixture.Target);
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        await File.WriteAllTextAsync(manifest, "previous manifest");
        await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None));
        Assert.Equal("previous manifest", await File.ReadAllTextAsync(manifest));
        Assert.Equal(oldBytes, await File.ReadAllBytesAsync(fixture.Blob(blobRoot)));
        Assert.Empty(Directory.GetDirectories(Path.Combine(fixture.Target, "staging")));
    }

    [Fact]
    public async Task ASecondImportStillVerifiesArchiveBlobsEvenWhenLocalOnesExist()
    {
        using var fixture = new Fixture();
        fixture.WritePack();
        await fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None);
        File.Delete(fixture.Pack);
        fixture.WritePack("hash");
        await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None));
        Assert.Equal(fixture.Data[0], await File.ReadAllBytesAsync(fixture.Blob(Path.Combine(fixture.Target, "blobs"))));
    }

    [Fact]
    public async Task CancellationDuringCopyPublishesNothingAndRemovesOnlyStaging()
    {
        using var fixture = new Fixture();
        fixture.WritePack();
        using var cancel = new CancellationTokenSource();
        var checks = 0;
        fixture.Resources.Free = () => { if (++checks == 2) cancel.Cancel(); return long.MaxValue; };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), cancel.Token));
        Assert.False(File.Exists(fixture.Model.ManifestPath(fixture.Target)));
        Assert.Empty(Directory.GetDirectories(Path.Combine(fixture.Target, "staging")));
        Assert.True(File.Exists(fixture.Pack));
    }

    [Fact]
    public async Task TheDiskReserveIsCheckedBeforeCopyAndDuringCopy()
    {
        using var fixture = new Fixture();
        fixture.WritePack();
        fixture.Resources.Free = () => LocalModelSetupService.DiskReserveBytes;
        var preflight = await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None));
        Assert.Equal(ModelSetupStage.DiskFull, preflight.Stage);
        var checks = 0;
        fixture.Resources.Free = () => ++checks == 1 ? long.MaxValue : 0;
        var during = await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None));
        Assert.Equal(ModelSetupStage.DiskFull, during.Stage);
        Assert.False(File.Exists(fixture.Model.ManifestPath(fixture.Target)));
    }

    [Fact]
    public async Task BadInstalledBytesCannotReplaceAnExistingExport()
    {
        using var fixture = new Fixture();
        await File.WriteAllTextAsync(fixture.Pack, "previous export");
        await File.WriteAllTextAsync(fixture.Blob(fixture.Source), "bad weights");
        await Assert.ThrowsAsync<ModelSetupException>(() => fixture.Store.ExportAsync(fixture.Model, fixture.Source, fixture.Pack, new Capture(), CancellationToken.None));
        Assert.Equal("previous export", await File.ReadAllTextAsync(fixture.Pack));
        Assert.Empty(Directory.GetFiles(fixture.Root, ".dzmodel-*.tmp"));
    }
    [Fact]
    public async Task AnOlderPinnedPackKeepsItsExactRevisionThroughReadAndImport()
    {
        using var fixture = new Fixture();
        var manifest = Encoding.UTF8.GetBytes("{\"fixture\":\"fictitious-new-revision\"}");
        var current = fixture.Model with
        {
            ManifestSha256 = Convert.ToHexStringLower(SHA256.HashData(manifest)),
            ManifestBase64 = Convert.ToBase64String(manifest),
        };
        var store = new ModelPackStore(fixture.Target, fixture.Resources, [current, fixture.Model]);
        await store.ExportAsync(fixture.Model, fixture.Source, fixture.Pack, new Capture(), CancellationToken.None);
        Assert.Equal(fixture.Model.ManifestSha256, (await store.ReadModelAsync(fixture.Pack, CancellationToken.None)).ManifestSha256);
        var installed = await store.ImportAsync(fixture.Pack, fixture.Model.Name, new Capture(), CancellationToken.None);
        Assert.Equal(fixture.Model.ManifestSha256, installed.ManifestSha256);
        Assert.Equal(fixture.Model.Manifest, await File.ReadAllBytesAsync(installed.ManifestPath(fixture.Target)));
    }

}