using System.Text.Json;

namespace DzLegal.Storage;

/// <summary>Welcome preferences are separate from the profile: choosing a language never completes setup.</summary>
public sealed class WelcomePreferences(string root)
{
    public string Root => root;

    public string ProfilePath => Path.Combine(root, "profile.json");

    public bool HasProfile => File.Exists(ProfilePath);

    public string SetupSeenPath => Path.Combine(root, "setup-seen.json");

    public bool HasSetupSeen => File.Exists(SetupSeenPath);

    public bool WasSetupSkipped
    {
        get
        {
            try
            {
                if (!HasSetupSeen) return false;
                using var json = JsonDocument.Parse(File.ReadAllText(SetupSeenPath));
                if (json.RootElement.ValueKind != JsonValueKind.Object) return false;
                if (json.RootElement.TryGetProperty("skipped", out var skipped)) return skipped.ValueKind == JsonValueKind.True;
                // Earlier marker-only skips used an empty object.
                return !json.RootElement.EnumerateObject().Any();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            { return false; }
        }
    }

    public bool MarkSetupSeen(bool skipped = true)
    {
        try
        {
            Directory.CreateDirectory(root);
            var temporary = SetupSeenPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { skipped }));
            File.Move(temporary, SetupSeenPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A marker must never prevent the lawyer from opening the application.
            return false;
        }
    }

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

        return new PreferencesStore(root).Load().Language == "en" ? "en" : "ar";
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
