namespace DzLegal.Storage;

/// <summary>Owns only profile images; a new selection never overwrites a saved logo.</summary>
public sealed class OnboardingProfileAssets(string root)
{
    public const int MaximumBytes = 2 * 1024 * 1024;

    public static bool HasImageSignature(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
    }

    public string Copy(string name, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg") || bytes.Length > MaximumBytes || !HasImageSignature(bytes))
            throw new ArgumentException("Invalid profile image.", nameof(bytes));
        var relative = Path.Combine("profile", Guid.NewGuid().ToString("N") + extension);
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.Combine(root, "profile"));
        File.WriteAllBytes(destination + ".tmp", bytes);
        File.Move(destination + ".tmp", destination);
        return relative;
    }

    public byte[]? Read(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) return null;
        try
        {
            var directory = Path.GetFullPath(Path.Combine(root, "profile")) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(directory, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(path) || new FileInfo(path).Length > MaximumBytes) return null;
            var bytes = File.ReadAllBytes(path);
            return HasImageSignature(bytes) ? bytes : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}
