using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using DzLegal.Storage;

namespace DzLegal.App.Services;

public sealed class AvaloniaUserFiles(Func<TopLevel?> topLevel, AppPaths paths) : IUserFiles
{
    private static readonly FilePickerFileType DraftType = new("Draft") { Patterns = ["*" + DraftFile.Extension] };
    private static readonly FilePickerFileType PdfType = new("PDF") { Patterns = ["*.pdf"], MimeTypes = ["application/pdf"] };
    private static readonly FilePickerFileType WordType = new("Word") { Patterns = ["*.docx"] };

    public async Task<(Stream Stream, string Name)?> OpenAsync()
    {
        if (topLevel() is not { } top)
        {
            return null;
        }

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [DraftType],
            SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(paths.Drafts),
        });
        return files.Count == 0 ? null : (await files[0].OpenReadAsync(), files[0].Name);
    }

    public async Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind)
    {
        var (type, extension) = kind switch
        {
            SaveKind.Pdf => (PdfType, ".pdf"),
            SaveKind.Word => (WordType, ".docx"),
            _ => (DraftType, DraftFile.Extension),
        };

        if (topLevel() is not { } top)
        {
            return null;
        }

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices = [type],
            SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(paths.Drafts),
        });
        return file is null ? null : (await file.OpenWriteAsync(), file.Name);
    }

    public async Task CopyTextAsync(string text)
    {
        if (topLevel()?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
