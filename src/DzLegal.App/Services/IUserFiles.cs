namespace DzLegal.App.Services;

/// <summary>File pickers and clipboard, abstracted so view models stay testable without a window.</summary>
public interface IUserFiles
{
    /// <summary>Asks the user for a file to open; null when cancelled.</summary>
    Task<(Stream Stream, string Name)?> OpenAsync();

    /// <summary>Asks the user where to save; null when cancelled.</summary>
    Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName);

    Task CopyTextAsync(string text);
}
