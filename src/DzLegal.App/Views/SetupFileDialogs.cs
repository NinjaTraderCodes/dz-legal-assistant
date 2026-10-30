using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Views;

/// <summary>Native onboarding pickers without changes to the case-file service.</summary>
public sealed class SetupFileDialogs(Func<TopLevel?> topLevel) : ISetupFiles
{
    private static readonly FilePickerFileType ImageType = new("PNG / JPG") { Patterns = ["*.png", "*.jpg", "*.jpeg"] };
    private static readonly FilePickerFileType ProfileType = new("DZ Legal profile") { Patterns = ["*.dzprofile.json"] };

    public async Task<SetupImage?> ChooseImageAsync()
    {
        if (topLevel() is not { } top) return null;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false, FileTypeFilter = [ImageType],
        });
        if (files.Count == 0) return null;
        using var file = files[0];
        await using var input = await file.OpenReadAsync();
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (output.Length <= OnboardingProfileAssets.MaximumBytes)
        {
            var read = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length,
                OnboardingProfileAssets.MaximumBytes + 1 - output.Length)));
            if (read == 0) break;
            output.Write(buffer, 0, read);
        }
        return new SetupImage(file.Name, output.ToArray());
    }

    public async Task<string?> ChooseFolderAsync(string? currentFolder)
    {
        if (topLevel() is not { } top) return null;
        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = string.IsNullOrWhiteSpace(currentFolder) ? null
                : await top.StorageProvider.TryGetFolderFromPathAsync(currentFolder),
        });
        if (folders.Count == 0) return null;
        using var folder = folders[0];
        return folder.TryGetLocalPath();
    }

    public async Task<string?> ChooseImportAsync()
    {
        if (topLevel() is not { } top) return null;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false, FileTypeFilter = [ProfileType],
        });
        if (files.Count == 0) return null;
        using var file = files[0];
        return file.TryGetLocalPath();
    }

    public async Task<string?> ChooseExportAsync()
    {
        if (topLevel() is not { } top) return null;
        using var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "profile.dzprofile.json", DefaultExtension = "dzprofile.json",
            FileTypeChoices = [ProfileType],
        });
        return file?.TryGetLocalPath();
    }

    public bool IsReadableImage(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var bitmap = Bitmap.DecodeToWidth(stream, 128);
            return bitmap.PixelSize.Width > 0 && bitmap.PixelSize.Height > 0;
        }
        // Avalonia's Skia decoder can throw NullReferenceException for a truncated PNG.
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException or IOException or NullReferenceException)
        {
            return false;
        }
    }
}
