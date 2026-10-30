using System.Text.Json;
using DzLegal.Domain.Serialization;

namespace DzLegal.Storage;

/// <summary>The lawyer details of one profile, plus the preferences and the embedded logo of one export.</summary>
public sealed record ExportedProfile(LawyerProfile Profile, ProfilePreferences Preferences, ExportedLogo? Logo);

/// <summary>The logo of an exported profile: file name and its bytes embedded as base64.</summary>
public sealed record ExportedLogo(string FileName, string Data);

/// <summary>Per-user petition profile, persisted as <c>profile.json</c> and exported as one <c>profile.dzprofile.json</c>.</summary>
public sealed class ProfileStore(string root)
{
    public string Root => root;
    public string ProfilePath => Path.Combine(Root, "profile.json");
    public string ExportPath => Path.Combine(Root, "profile.dzprofile.json");

    public LawyerProfile Load()
    {
        try
        {
            if (File.Exists(ProfilePath))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(ProfilePath));
                if (json.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return json.RootElement.Deserialize<LawyerProfile>(DefinitionJson.Options) ?? LawyerProfile.Default;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            // A broken profile must never prevent the lawyer from drafting.
        }

        return LawyerProfile.Default;
    }

    public void Save(LawyerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Directory.CreateDirectory(Root);
        var temporary = ProfilePath + ".tmp";
        using (var stream = File.Create(temporary))
        {
            JsonSerializer.Serialize(stream, profile, DefinitionJson.Options);
        }

        File.Move(temporary, ProfilePath, overwrite: true);
    }

    public static string Export(string exportPath, LawyerProfile profile, ProfilePreferences preferences, ExportedLogo? logo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
        var exported = new ExportedProfile(profile, preferences, logo);
        var temporary = exportPath + ".tmp";
        using (var stream = File.Create(temporary))
        {
            JsonSerializer.Serialize(stream, exported, DefinitionJson.Options);
        }

        File.Move(temporary, exportPath, overwrite: true);
        return exportPath;
    }

    public static ExportedProfile? Import(string importPath)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(importPath));
            var rootElement = json.RootElement;
            if (rootElement.ValueKind != JsonValueKind.Object
                || rootElement.GetProperty("profile").ValueKind != JsonValueKind.Object
                || rootElement.GetProperty("preferences").ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var profile = rootElement.GetProperty("profile").Deserialize<LawyerProfile>(DefinitionJson.Options);
            var preferences = rootElement.GetProperty("preferences").Deserialize<ProfilePreferences>(DefinitionJson.Options);
            ExportedLogo? logo = null;
            if (rootElement.TryGetProperty("logo", out var logoToken) && logoToken.ValueKind == JsonValueKind.Object
                && logoToken.TryGetProperty("fileName", out var fileName) && fileName.ValueKind == JsonValueKind.String
                && logoToken.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.String)
            {
                logo = new ExportedLogo(fileName.GetString()!, data.GetString()!);
            }

            return new ExportedProfile(profile ?? LawyerProfile.Default, preferences ?? ProfilePreferences.Default, logo);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }
}
