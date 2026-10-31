using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using DzLegal.Presentation.ViewModels;
using DzLegal.Storage;

namespace DzLegal.App.Services;

public sealed class AvaloniaUserFiles(Func<TopLevel?> topLevel, AppPaths paths) : IUserFiles, IHomeRecentFiles
{
    private static readonly FilePickerFileType DraftType = new("Draft") { Patterns = ["*" + DraftFile.Extension] };
    private static readonly FilePickerFileType PdfType = new("PDF") { Patterns = ["*.pdf"], MimeTypes = ["application/pdf"] };
    private static readonly FilePickerFileType WordType = new("Word") { Patterns = ["*.docx"] };

    public string? LastOpenedPath { get; private set; }
    public string? LastSavedPath { get; private set; }

    public async Task<(Stream Stream, string Name)?> OpenAsync()
    {
        LastOpenedPath = null;
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
        if (files.Count == 0) return null;
        var stream = await files[0].OpenReadAsync();
        LastOpenedPath = files[0].TryGetLocalPath();
        return (stream, files[0].Name);
    }

    public Task<(Stream Stream, string Name)?> OpenPathAsync(string path)
    {
        LastOpenedPath = null;
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        LastOpenedPath = fullPath;
        return Task.FromResult<(Stream, string)?>((stream, Path.GetFileName(fullPath)));
    }

    public async Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind)
    {
        LastSavedPath = null;
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
        if (file is null) return null;
        var stream = await file.OpenWriteAsync();
        if (kind == SaveKind.Draft) LastSavedPath = file.TryGetLocalPath();
        return (stream, file.Name);
    }

    public async Task CopyTextAsync(string text)
    {
        if (topLevel()?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
