using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DzLegal.Harness;

/// <summary>
/// What a bench run was made of, written beside its results (manifest.json) so that two runs are only compared when
/// they are comparable: the code commit, fingerprints of the law data, prompts and cases, the engine and the options.
/// </summary>
public sealed record RunManifest
{
    public required string Command { get; init; }

    public required DateTimeOffset StartedUtc { get; init; }

    /// <summary>The git commit of the checkout the harness runs in (uncommitted changes show in the fingerprints).</summary>
    public string? Commit { get; init; }

    public required IReadOnlyList<string> Models { get; init; }

    /// <summary>SHA-256 over every file of the folder (path and bytes), so any change to the data shows.</summary>
    public required IReadOnlyDictionary<string, string> Fingerprints { get; init; }

    public required IReadOnlyDictionary<string, string> Options { get; init; }

    public static RunManifest Create(string command, IReadOnlyList<string> models, IReadOnlyDictionary<string, string> folders, IReadOnlyDictionary<string, string> options) => new()
    {
        Command = command,
        StartedUtc = DateTimeOffset.UtcNow,
        Commit = GitCommit(Directory.GetCurrentDirectory()),
        Models = models,
        Fingerprints = folders.ToDictionary(kv => kv.Key, kv => FolderHash(kv.Value)),
        Options = options,
    };

    public void Write(string folder) =>
        File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonSerializer.Serialize(this, Indented));

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>Hash of a file or a folder's files in ordinal path order; "missing" when absent.</summary>
    public static string FolderHash(string path)
    {
        if (File.Exists(path))
        {
            return Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        }

        if (!Directory.Exists(path))
        {
            return "missing";
        }

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                     .Select(f => (Full: f, Relative: Path.GetRelativePath(path, f).Replace('\\', '/')))
                     .Where(f => !f.Relative.StartsWith("corpus/.cache/", StringComparison.Ordinal))
                     .OrderBy(f => f.Relative, StringComparer.Ordinal))
        {
            sha.AppendData(Encoding.UTF8.GetBytes(file.Relative + "\n"));
            sha.AppendData(File.ReadAllBytes(file.Full));
        }

        return Convert.ToHexStringLower(sha.GetHashAndReset());
    }

    /// <summary>Reads HEAD from the nearest .git folder (no git process needed); null outside a checkout.</summary>
    public static string? GitCommit(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var git = Path.Combine(dir.FullName, ".git");
            if (!Directory.Exists(git))
            {
                continue;
            }

            var head = File.ReadAllText(Path.Combine(git, "HEAD")).Trim();
            if (!head.StartsWith("ref: ", StringComparison.Ordinal))
            {
                return head;
            }

            var reference = head[5..];
            var loose = Path.Combine(git, reference.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(loose))
            {
                return File.ReadAllText(loose).Trim();
            }

            var packed = Path.Combine(git, "packed-refs");
            return File.Exists(packed)
                ? File.ReadLines(packed).Select(l => l.Split(' ')).FirstOrDefault(p => p.Length == 2 && p[1] == reference)?[0]
                : null;
        }

        return null;
    }
}
