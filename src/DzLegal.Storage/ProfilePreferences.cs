namespace DzLegal.Storage;

/// <summary>Per-user preferences: interface language, appearance, default engine, background analysis and save folder.</summary>
public sealed record ProfilePreferences
{
    public const string DefaultLanguage = "ar";
    public const string DefaultTheme = "dark";
    public const string DefaultEngine = "ollama";

    public string Language { get; init; } = DefaultLanguage;
    public string Theme { get; init; } = DefaultTheme;
    public string Engine { get; init; } = DefaultEngine;
    public bool AnalyseInBackground { get; init; } = true;
    public string? SaveFolder { get; init; }

    public static ProfilePreferences Default => new();
}
