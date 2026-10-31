using System.Text.Json;

namespace DzLegal.Storage;

/// <summary>Only the first-setup tour offer is remembered; case contents never belong here.</summary>
public sealed class GuidePreferences(string root)
{
    public string FilePath => Path.Combine(root, "guide-preferences.json");

    public bool TourOffered
    {
        get
        {
            try
            {
                if (!File.Exists(FilePath)) return false;
                using var json = JsonDocument.Parse(File.ReadAllText(FilePath));
                return json.RootElement.ValueKind == JsonValueKind.Object
                    && json.RootElement.TryGetProperty("tourOffered", out var offered)
                    && offered.ValueKind == JsonValueKind.True;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            { return false; }
        }
    }

    public bool MarkTourOffered()
    {
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(FilePath + ".tmp", "{\"tourOffered\":true}");
            File.Move(FilePath + ".tmp", FilePath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return false; }
    }
}
