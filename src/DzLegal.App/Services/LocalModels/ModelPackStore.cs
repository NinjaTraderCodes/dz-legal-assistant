using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using DzLegal.Presentation.Services;

namespace DzLegal.App.Services.LocalModels;

/// <summary>The pack contains only a built-in manifest and its exact content-addressed payload.</summary>
internal sealed class ModelPackStore(string modelRoot, IComputerResources resources, IReadOnlyList<SupportedModel>? inventory = null)
{
    private readonly IReadOnlyList<SupportedModel> models = inventory ?? SupportedModels.All;
    private readonly string root = Path.GetFullPath(modelRoot);

    public async Task<SupportedModel> ReadModelAsync(string path, CancellationToken cancellationToken)
    {
        await using var input = OpenPack(path);
        await CheckEnvelopeAsync(input, cancellationToken).ConfigureAwait(false);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read);
        var model = await IdentifyAsync(zip, cancellationToken).ConfigureAwait(false);
        CheckEntries(zip, model);
        return model;
    }

    public async Task<SupportedModel> ImportAsync(string path, string expectedModel, IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        await using var input = OpenPack(path);
        await CheckEnvelopeAsync(input, cancellationToken).ConfigureAwait(false);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read);
        var model = await IdentifyAsync(zip, cancellationToken).ConfigureAwait(false);
        CheckEntries(zip, model);
        if (model.Name != expectedModel) Fail();
        SafeDirectory(root);
        var blobRoot = Path.Combine(root, "blobs");
        SafeDirectory(blobRoot);
        var missing = new List<ModelBlob>();
        foreach (var blob in model.Blobs)
            if (!await ValidFileAsync(BlobPath(blobRoot, blob), blob, cancellationToken).ConfigureAwait(false)) missing.Add(blob);
        var required = checked(missing.Sum(b => b.Size) + LocalModelSetupService.DiskReserveBytes + 65536);
        if (resources.FreeDiskBytes(root) < required) throw new ModelSetupException(ModelSetupStage.DiskFull);
        var stagingParent = Path.Combine(root, "staging");
        SafeDirectory(stagingParent);
        var staging = Path.Combine(stagingParent, Guid.NewGuid().ToString("N"));
        if (Path.GetDirectoryName(Path.GetFullPath(staging)) != Path.GetFullPath(stagingParent))
            throw new IOException("Invalid staging location.");
        SafeDirectory(staging);
        var meter = new PackMeter(progress, ModelSetupStage.Importing, model);
        try
        {
            foreach (var blob in model.Blobs)
            {
                var entry = zip.GetEntry("blobs/sha256-" + blob.Sha256)!;
                await using var source = entry.Open();
                await using var output = missing.Contains(blob)
                    ? new FileStream(BlobPath(staging, blob), FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous)
                    : Stream.Null;
                await CopyCheckedAsync(source, output, blob, () => Guard(root, true), meter.Add, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            SafeDirectory(blobRoot);
            foreach (var blob in missing)
                File.Move(BlobPath(staging, blob), BlobPath(blobRoot, blob), overwrite: true);
            await ModelVerifier.PublishManifestAsync(model, root, cancellationToken).ConfigureAwait(false);
            meter.Finish();
            return model;
        }
        finally
        {
            foreach (var blob in missing)
            {
                var file = BlobPath(staging, blob);
                if (File.Exists(file)) File.Delete(file);
            }
            Directory.Delete(staging);
        }
    }

    public async Task ExportAsync(SupportedModel model, string blobRoot, string destination,
        IProgress<ModelSetupProgress> progress, CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(destination).Equals(".dzmodel", StringComparison.OrdinalIgnoreCase)) Fail();
        var target = Path.GetFullPath(destination);
        var parent = Path.GetDirectoryName(target)!;
        if (!Directory.Exists(parent)) throw new IOException("The destination folder is unavailable.");
        var sameDrive = string.Equals(Path.GetPathRoot(target), Path.GetPathRoot(root), StringComparison.OrdinalIgnoreCase);
        var reserve = sameDrive ? LocalModelSetupService.DiskReserveBytes : 65536;
        if (resources.FreeDiskBytes(parent) < checked(model.Info.Bytes + reserve + 65536))
            throw new ModelSetupException(ModelSetupStage.DiskFull);
        if (OperatingSystem.IsWindows() && model.Info.Bytes > uint.MaxValue
            && new DriveInfo(Path.GetPathRoot(target)!).DriveFormat.Equals("FAT32", StringComparison.OrdinalIgnoreCase))
            throw new ModelSetupException(ModelSetupStage.PackTooLarge);
        var temporary = Path.Combine(parent, ".dzmodel-" + Guid.NewGuid().ToString("N") + ".tmp");
        var meter = new PackMeter(progress, ModelSetupStage.Exporting, model);
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 128 * 1024, FileOptions.Asynchronous))
            {
                using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
                var manifest = zip.CreateEntry("manifest.json", CompressionLevel.NoCompression);
                await using (var entry = manifest.Open()) await entry.WriteAsync(model.Manifest, cancellationToken).ConfigureAwait(false);
                foreach (var blob in model.Blobs)
                {
                    if ((File.GetAttributes(BlobPath(blobRoot, blob)) & FileAttributes.ReparsePoint) != 0) Fail();
                    await using var source = new FileStream(BlobPath(blobRoot, blob), FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    var entry = zip.CreateEntry("blobs/sha256-" + blob.Sha256, CompressionLevel.NoCompression);
                    await using var entryOutput = entry.Open();
                    await CopyCheckedAsync(source, entryOutput, blob, () => Guard(parent, sameDrive), meter.Add, cancellationToken).ConfigureAwait(false);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, target, overwrite: true);
            meter.Finish();
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private FileStream OpenPack(string path)
    {
        var file = new FileInfo(path);
        if (!Path.GetExtension(path).Equals(".dzmodel", StringComparison.OrdinalIgnoreCase)
            || !file.Exists || file.Length > models.Max(m => m.Info.Bytes) + 65536) Fail();
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.RandomAccess);
    }

    private async Task CheckEnvelopeAsync(Stream input, CancellationToken cancellationToken)
    {
        if (input.Length < 22) Fail();
        input.Seek(-22, SeekOrigin.End);
        var footer = new byte[22];
        await input.ReadExactlyAsync(footer, cancellationToken).ConfigureAwait(false);
        if (BinaryPrimitives.ReadUInt32LittleEndian(footer) != 0x06054b50
            || BinaryPrimitives.ReadUInt16LittleEndian(footer.AsSpan(4)) != 0
            || BinaryPrimitives.ReadUInt16LittleEndian(footer.AsSpan(6)) != 0
            || BinaryPrimitives.ReadUInt16LittleEndian(footer.AsSpan(20)) != 0
            || BinaryPrimitives.ReadUInt32LittleEndian(footer.AsSpan(12)) > 65536) Fail();
        var count = BinaryPrimitives.ReadUInt16LittleEndian(footer.AsSpan(10));
        if (count < 2 || count > models.Max(m => m.Blobs.Count) + 1
            || count != BinaryPrimitives.ReadUInt16LittleEndian(footer.AsSpan(8))) Fail();
        var centralSize = BinaryPrimitives.ReadUInt32LittleEndian(footer.AsSpan(12));
        var centralOffset = BinaryPrimitives.ReadUInt32LittleEndian(footer.AsSpan(16));
        if (centralOffset == uint.MaxValue)
        {
            // Bound ZIP64 metadata before ZipArchive uses its declared entry count.
            if (input.Length < 98) Fail();
            input.Seek(-98, SeekOrigin.End);
            var end = new byte[76];
            await input.ReadExactlyAsync(end, cancellationToken).ConfigureAwait(false);
            var recordOffset = (ulong)(input.Length - 98);
            if (BinaryPrimitives.ReadUInt32LittleEndian(end) != 0x06064b50
                || BinaryPrimitives.ReadUInt64LittleEndian(end.AsSpan(4)) != 44
                || BinaryPrimitives.ReadUInt32LittleEndian(end.AsSpan(16)) != 0
                || BinaryPrimitives.ReadUInt32LittleEndian(end.AsSpan(20)) != 0
                || BinaryPrimitives.ReadUInt64LittleEndian(end.AsSpan(24)) != count
                || BinaryPrimitives.ReadUInt64LittleEndian(end.AsSpan(32)) != count
                || BinaryPrimitives.ReadUInt64LittleEndian(end.AsSpan(40)) != centralSize
                || BinaryPrimitives.ReadUInt64LittleEndian(end.AsSpan(48)) > recordOffset
                || BinaryPrimitives.ReadUInt64LittleEndian(end.AsSpan(48)) + centralSize != recordOffset
                || BinaryPrimitives.ReadUInt32LittleEndian(end.AsSpan(56)) != 0x07064b50
                || BinaryPrimitives.ReadUInt32LittleEndian(end.AsSpan(60)) != 0
                || BinaryPrimitives.ReadUInt64LittleEndian(end.AsSpan(64)) != recordOffset
                || BinaryPrimitives.ReadUInt32LittleEndian(end.AsSpan(72)) != 1) Fail();
        }
        else if ((ulong)centralOffset + centralSize != (ulong)(input.Length - 22)) Fail();
        input.Position = 0;
    }

    private async Task<SupportedModel> IdentifyAsync(ZipArchive zip, CancellationToken cancellationToken)
    {
        var entry = zip.GetEntry("manifest.json");
        if (entry is null || entry.Length > 65536) { Fail(); return null!; }
        await using var stream = entry.Open();
        using var memory = new MemoryStream();
        var buffer = new byte[4096];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (memory.Length + read > 65536) Fail();
            memory.Write(buffer, 0, read);
        }
        var data = memory.ToArray();
        return models.FirstOrDefault(m => m.Manifest.AsSpan().SequenceEqual(data))
            ?? throw new ModelSetupException(ModelSetupStage.InvalidPack);
    }

    private static void CheckEntries(ZipArchive zip, SupportedModel model)
    {
        var expected = model.Blobs.ToDictionary(b => "blobs/sha256-" + b.Sha256, b => b.Size, StringComparer.Ordinal);
        expected.Add("manifest.json", model.Manifest.Length);
        if (zip.Entries.Count != expected.Count) Fail();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
        {
            var mode = (entry.ExternalAttributes >> 16) & 0xf000;
            if (!seen.Add(entry.FullName) || !expected.TryGetValue(entry.FullName, out var length) || entry.Length != length
                || mode is not (0 or 0x8000) || (entry.ExternalAttributes & 0x400) != 0
                || entry.Length > 1024 * 1024 && entry.CompressedLength < entry.Length / 100) Fail();
        }
    }

    private static string BlobPath(string folder, ModelBlob blob) => Path.Combine(folder, "sha256-" + blob.Sha256);
    private static void Fail() => throw new ModelSetupException(ModelSetupStage.InvalidPack);

    private void SafeDirectory(string path)
    {
        var full = Path.GetFullPath(path);
        if (full != root && !full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Fail();
        for (var current = full; current.Length >= root.Length; current = Path.GetDirectoryName(current)!)
        {
            if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) Fail();
            if (current == root) break;
        }
        Directory.CreateDirectory(full);
    }

    private void Guard(string path, bool reserve)
    {
        if (resources.FreeDiskBytes(path) < (reserve ? LocalModelSetupService.DiskReserveBytes : 65536))
            throw new ModelSetupException(ModelSetupStage.DiskFull);
        if (resources.AvailableMemoryBytes < 512UL * 1024 * 1024) throw new ModelSetupException(ModelSetupStage.MemoryLow);
    }

    private static async Task<bool> ValidFileAsync(string path, ModelBlob blob, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return false;
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) Fail();
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return input.Length == blob.Size && Convert.ToHexStringLower(await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false)) == blob.Sha256;
    }

    private static async Task CopyCheckedAsync(Stream source, Stream output, ModelBlob blob, Action guard, Action<int> added, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[128 * 1024];
        long total = 0;
        var untilGuard = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            total += read;
            if (total > blob.Size) Fail();
            if (untilGuard <= 0) { guard(); untilGuard = 1024 * 1024; }
            untilGuard -= read;
            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            added(read);
        }
        if (total != blob.Size || Convert.ToHexStringLower(hash.GetHashAndReset()) != blob.Sha256) Fail();
    }

    private sealed class PackMeter(IProgress<ModelSetupProgress> progress, ModelSetupStage stage, SupportedModel model)
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private long completed;
        private long last;
        public void Add(int count)
        {
            completed += count;
            if (clock.ElapsedMilliseconds - last >= 100) Emit();
        }
        public void Finish() => Emit();
        private void Emit()
        {
            last = clock.ElapsedMilliseconds;
            progress.Report(new ModelSetupProgress(stage, model.Name, completed, model.Info.Bytes,
                clock.Elapsed.TotalSeconds > 0 ? completed / clock.Elapsed.TotalSeconds : 0));
        }
    }
}