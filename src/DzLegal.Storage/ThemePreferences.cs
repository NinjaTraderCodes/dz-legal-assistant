using System.Text.Json;

namespace DzLegal.Storage;

/// <summary>Desktop appearance preference, kept beside the other per-user preferences.</summary>
public sealed class ThemePreferences(string root)
{
    public string FilePath => Path.Combine(root, "theme-preferences.json");

    public bool LoadDarkTheme()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(FilePath));
                return json.RootElement.GetProperty("theme").GetString() != "light";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            // Match the existing desktop default if a preference cannot be read.
        }

        return true;
    }

    public bool Save(bool dark)
    {
        try
        {
            Directory.CreateDirectory(root);
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { theme = dark ? "dark" : "light" }));
            File.Move(temporary, FilePath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
