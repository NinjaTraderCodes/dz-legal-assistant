using System.Text.Json;

namespace DzLegal.Storage;

public sealed record RecentCase(string Path, string FileName, string Subject, DateTimeOffset LastOpened);

/// <summary>Only display metadata, never case contents. Removing an entry never deletes its case.</summary>
public sealed class RecentCasesStore(string root, Func<DateTimeOffset>? clock = null)
{
    public const int Limit = 10;
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static readonly object Gate = new();
    public string FilePath => System.IO.Path.Combine(root, "recent.json");

    public IReadOnlyList<RecentCase> Load()
    {
        lock (Gate)
        {
            try
            {
                if (!File.Exists(FilePath)) return [];
                var entries = JsonSerializer.Deserialize<List<RecentCase>>(File.ReadAllText(FilePath), Options);
                return (entries ?? []).Where(e => e is not null && !string.IsNullOrWhiteSpace(e.Path)
                        && System.IO.Path.IsPathFullyQualified(e.Path) && e.FileName is not null && e.Subject is not null)
                    .OrderByDescending(e => e.LastOpened).DistinctBy(e => e.Path, StringComparer.OrdinalIgnoreCase).Take(Limit).ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
            {
                return [];
            }
        }
    }

    public bool RecordOpened(string path, string? subject) => Record(path, subject);
    public bool RecordSaved(string path, string? subject) => Record(path, subject);

    private bool Record(string path, string? subject)
    {
        lock (Gate)
        {
            try
            {
                if (!System.IO.Path.IsPathFullyQualified(path)) return false;
                var fullPath = System.IO.Path.GetFullPath(path);
                var entry = new RecentCase(fullPath, System.IO.Path.GetFileName(fullPath), subject ?? string.Empty,
                    (clock ?? (() => DateTimeOffset.Now))());
                return Save(new[] { entry }.Concat(Load().Where(e => !string.Equals(e.Path, fullPath, StringComparison.OrdinalIgnoreCase))).Take(Limit));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    public bool Remove(string path)
    {
        lock (Gate) return Save(Load().Where(e => !string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase)));
    }

    public bool Clear() { lock (Gate) return Save([]); }

    private bool Save(IEnumerable<RecentCase> entries)
    {
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(entries.ToArray(), Options));
            File.Move(FilePath + ".tmp", FilePath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
