using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DzLegal.App.Services.LocalModels;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public class ModelIntegrityTests
{
    [Fact]
    public async Task AChangedBlobCannotUseAnEarlierSuccessfulVerification()
    {
        var parent = Path.Combine(AppPaths.Default.Root, "model-setup-tests");
        var root = Path.Combine(parent, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var bytes = Encoding.UTF8.GetBytes("fictitious model bytes");
            var blob = new ModelBlob(Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.Length);
            var model = new SupportedModel("fictitious:1", "", "", [blob], "Fictitious test terms", 1);
            var path = Path.Combine(root, "sha256-" + blob.Sha256);
            await File.WriteAllBytesAsync(path, bytes);
            using var verifier = new ModelVerifier();
            Assert.True(await verifier.VerifyAsync(model, root, CancellationToken.None));
            bytes[0] ^= 1;
            await File.WriteAllBytesAsync(path, bytes);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(2));
            Assert.False(await verifier.VerifyAsync(model, root, CancellationToken.None));
            Assert.False(await verifier.VerifyAsync(model, root, CancellationToken.None, force: true));
        }
        finally
        {
            Assert.Equal(Path.GetFullPath(parent), Path.GetDirectoryName(Path.GetFullPath(root)));
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EveryManifestComponentAppearsInTheBuiltInBlobInventory()
    {
        foreach (var model in SupportedModels.All)
        {
            using var json = JsonDocument.Parse(model.Manifest);
            var components = new[] { json.RootElement.GetProperty("config") }
                .Concat(json.RootElement.GetProperty("layers").EnumerateArray());
            var expected = components.Select(c => (Sha: c.GetProperty("digest").GetString()![7..], Size: c.GetProperty("size").GetInt64())).ToArray();
            Assert.Equal(expected, model.Blobs.Select(b => (b.Sha256, b.Size)));
        }
    }
}