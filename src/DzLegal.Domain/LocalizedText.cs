namespace DzLegal.Domain;

/// <summary>
/// Text carried in both supported UI languages. Arabic is the primary language;
/// English is used for the English UI and for non-Arabic-speaking reviewers.
/// </summary>
public sealed record LocalizedText(string Ar, string En)
{
    public string Get(string languageCode) =>
        languageCode.StartsWith("en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(En) ? En : Ar;

    public override string ToString() => Ar;
}
