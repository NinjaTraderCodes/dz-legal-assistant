namespace DzLegal.Presentation.ViewModels;

/// <summary>Onboarding pickers stay separate from the protected case-file contract.</summary>
public interface ISetupFiles
{
    Task<SetupImage?> ChooseImageAsync();
    Task<string?> ChooseFolderAsync(string? currentFolder);
    Task<string?> ChooseImportAsync();
    Task<string?> ChooseExportAsync();
    bool IsReadableImage(byte[] bytes);
}

public sealed record SetupImage(string Name, byte[] Bytes);
