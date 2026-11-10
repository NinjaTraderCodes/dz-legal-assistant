using System.Security.Cryptography;
using DzLegal.Presentation.Services;

namespace DzLegal.App.Services.LocalModels;

internal sealed class ModelVerifier : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<string, string> verified = new(StringComparer.Ordinal);

    public async Task<bool> VerifyAsync(SupportedModel model, string blobRoot, CancellationToken cancellationToken, bool force = false)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var key = model.Name + "|" + model.ManifestSha256 + "|" + Path.GetFullPath(blobRoot);
            var fingerprint = string.Join('|', model.Blobs.Select(b =>
            {
                var info = new FileInfo(Path.Combine(blobRoot, "sha256-" + b.Sha256));
                return info.Exists ? $"{info.Length}:{info.LastWriteTimeUtc.Ticks}" : "missing";
            }));
            if (!force && verified.TryGetValue(key, out var previous) && previous == fingerprint) return true;
            foreach (var blob in model.Blobs)
            {
                var path = Path.Combine(blobRoot, "sha256-" + blob.Sha256);
                if (!File.Exists(path)) return false;
                await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                if (input.Length != blob.Size || !Convert.ToHexStringLower(await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false)).Equals(blob.Sha256, StringComparison.Ordinal))
                    return false;
            }
            verified[key] = fingerprint;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return false; }
        finally { gate.Release(); }
    }

    public void Dispose()
    {
        gate.Wait();
        gate.Release();
        gate.Dispose();
    }

    internal static async Task PublishManifestAsync(SupportedModel model, string modelRoot, CancellationToken cancellationToken)
    {
        var path = model.ManifestPath(modelRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, model.Manifest, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}