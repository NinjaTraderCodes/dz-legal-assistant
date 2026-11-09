using System.Text.Json;
using DzLegal.Presentation.Services;

namespace DzLegal.App.Services.LocalModels;

internal sealed record ModelBlob(string Sha256, long Size);
internal sealed record SupportedModel(string Name, string ManifestSha256, string ManifestBase64,
    IReadOnlyList<ModelBlob> Blobs, string Terms, long RequiredMemoryBytes)
{
    public byte[] Manifest => Convert.FromBase64String(ManifestBase64);
    public SetupModel Info => new(Name, Blobs.Sum(b => b.Size), Terms, RequiredMemoryBytes);
    public string ManifestPath(string root) => Path.Combine(root, "manifests", "registry.ollama.ai", "library", Name.Replace(':', Path.DirectorySeparatorChar));
}

internal static class SupportedModels
{
    public static IReadOnlyList<SupportedModel> All { get; } = Load();
    public static SupportedModel Find(string name) => All.FirstOrDefault(m => m.Name == name)
        ?? throw new ModelSetupException(ModelSetupStage.VerificationFailed);
    private static List<SupportedModel> Load()
    {
        using var input = typeof(SupportedModels).Assembly.GetManifestResourceStream("DzLegal.App.SupportedModels")
            ?? throw new IOException("The supported model inventory is missing.");
        return JsonSerializer.Deserialize<List<SupportedModel>>(input, JsonSerializerOptions.Web)
            ?? throw new IOException("The supported model inventory is invalid.");
    }
}