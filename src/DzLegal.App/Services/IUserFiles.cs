namespace DzLegal.App.Services;

public enum SaveKind
{
    Draft,
    Pdf,
    Word,
}

/// <summary>File pickers and clipboard, abstracted so view models stay testable without a window.</summary>
public interface IUserFiles
{
    /// <summary>Asks the user for a file to open; null when cancelled.</summary>
    Task<(Stream Stream, string Name)?> OpenAsync();

    /// <summary>Asks the user where to save a file of the given kind; null when cancelled.</summary>
    Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind);

    Task CopyTextAsync(string text);
}
