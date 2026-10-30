namespace DzLegal.Storage;

/// <summary>The lawyer's professional details, kept per user and pre-filled into new petitions.</summary>
public sealed record LawyerProfile
{
    public string? NameAr { get; init; }
    public string? NameFr { get; init; }
    public string? Gender { get; init; }
    public string? Council { get; init; }
    public string? Bar { get; init; }
    public bool SupremeCourt { get; init; }
    public string? Standing { get; init; }
    public bool StandingEdited { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Firm { get; init; }
    public string? Logo { get; init; }
    public string? DefaultPlace { get; init; }
    public string? DefaultCouncil { get; init; }
    public string? DefaultCourt { get; init; }
    public bool WatermarkByDefault { get; init; } = true;
    public string FileNaming { get; init; } = "current";

    public static LawyerProfile Default => new()
    {
        SupremeCourt = false,
        StandingEdited = false,
        WatermarkByDefault = true,
        FileNaming = "current",
    };

    /// <summary>
    /// Suggested standing for the petition: the advocate title, the judicial council, and, when accredited,
    /// the appended form of address. Empty when there is no council or no form of address yet.
    /// </summary>
    public static string? SuggestedStanding(string? council, bool accredited, string? gender)
    {
        if (string.IsNullOrWhiteSpace(council) || (gender != "male" && gender != "female"))
        {
            return string.Empty;
        }

        var advocate = gender == "male" ? "محامٍ" : "محامية";
        var status = accredited ? (gender == "male" ? "معتمد" : "معتمدة") : string.Empty;
        var suffix = accredited ? $"، {status} لدى المحكمة العليا ومجلس الدولة" : string.Empty;
        return $"{advocate} لدى مجلس قضاء {council}{suffix}";
    }
}
