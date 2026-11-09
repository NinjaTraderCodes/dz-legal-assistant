using System.Security.Cryptography;
using System.Text.Json;

namespace DzLegal.App.Services.LocalModels;

internal static class EngineBundle
{
    public static void Verify(string directory)
    {
        using var resource = typeof(EngineBundle).Assembly.GetManifestResourceStream("DzLegal.App.EngineFiles")
            ?? throw new IOException("The engine inventory is missing.");
        using var json = JsonDocument.Parse(resource);
        foreach (var file in json.RootElement.GetProperty("files").EnumerateArray())
        {
            var relative = file.GetProperty("path").GetString()!;
            var path = Path.Combine(directory, relative);
            for (var current = path; current.Length >= directory.Length; current = Path.GetDirectoryName(current)!)
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("An engine file is redirected.");
                if (current == directory) break;
            }
            using var input = File.OpenRead(path);
            if (input.Length != file.GetProperty("size").GetInt64()
                || !Convert.ToHexStringLower(SHA256.HashData(input)).Equals(file.GetProperty("sha256").GetString(), StringComparison.Ordinal))
                throw new IOException("The bundled engine failed verification.");
        }
    }
}