using System.Text.Json;
using DzLegal.Domain.Serialization;

namespace DzLegal.Storage;

/// <summary>Per-user preferences persisted as <c>preferences.json</c>.</summary>
public sealed class PreferencesStore(string root)
{
    public string Root => root;
    public string PreferencesPath => Path.Combine(Root, "preferences.json");

    public ProfilePreferences Load()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(PreferencesPath));
                if (json.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return json.RootElement.Deserialize<ProfilePreferences>(DefinitionJson.Options) ?? ProfilePreferences.Default;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            // A broken preferences file must never prevent the lawyer from drafting.
        }

        return ProfilePreferences.Default;
    }

    public void Save(ProfilePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        Directory.CreateDirectory(Root);
        var temporary = PreferencesPath + ".tmp";
        using (var stream = File.Create(temporary))
        {
            JsonSerializer.Serialize(stream, preferences, DefinitionJson.Options);
        }

        File.Move(temporary, PreferencesPath, overwrite: true);
    }
}
