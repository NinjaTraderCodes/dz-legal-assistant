using System.Text.Json;
using DzLegal.Storage;

namespace DzLegal.Presentation.ViewModels;

public static class HomeLaunchRouting
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    public static bool CanGoHome(WelcomePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (preferences.HasSetupSeen) return true;
        if (!preferences.HasProfile) return false;
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(preferences.ProfilePath));
            if (json.RootElement.ValueKind != JsonValueKind.Object) return false;
            return json.RootElement.Deserialize<LawyerProfile>(Options) is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        { return false; }
    }
}
