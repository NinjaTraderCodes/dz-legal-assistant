using System.Text.Json;

namespace DzLegal.Storage;

/// <summary>Welcome preferences are separate from the profile: choosing a language never completes setup.</summary>
public sealed class WelcomePreferences(string root)
{
    public string ProfilePath => Path.Combine(root, "profile.json");

    public bool HasProfile => File.Exists(ProfilePath);

    private string PreferencesPath => Path.Combine(root, "welcome-preferences.json");

    public string LoadLanguage()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(PreferencesPath));
                return json.RootElement.GetProperty("language").GetString() == "en" ? "en" : "ar";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            // A preference must never prevent the lawyer from opening the application.
        }

        return "ar";
    }

    public bool SaveLanguage(string language)
    {
        try
        {
            Directory.CreateDirectory(root);
            var temp = PreferencesPath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new { language = language == "en" ? "en" : "ar" }));
            File.Move(temp, PreferencesPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
