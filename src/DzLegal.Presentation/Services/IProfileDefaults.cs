namespace DzLegal.Presentation.Services;

/// <summary>Profile values offered to a new case, so an empty case can be pre-filled from the lawyer's profile.</summary>
public interface IProfileDefaults
{
    /// <summary>Profile values keyed by form field id; empty values are not offered.</summary>
    IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Whether a new case starts with the draft watermark on.</summary>
    bool WatermarkByDefault { get; }
}
