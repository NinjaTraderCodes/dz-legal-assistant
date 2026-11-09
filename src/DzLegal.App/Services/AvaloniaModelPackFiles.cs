using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DzLegal.Presentation;
using DzLegal.Presentation.Services;

namespace DzLegal.App.Services;

public sealed class AvaloniaModelPackFiles(Func<TopLevel?> topLevel, Loc loc) : IModelPackFiles
{
    private FilePickerFileType PackType => new(loc["ModelSetupPackFileType"]) { Patterns = ["*.dzmodel"] };
    public async Task<string?> PickImportAsync()
    {
        if (topLevel() is not { } top) return null;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = loc["ModelSetupChoosePack"], AllowMultiple = false, FileTypeFilter = [PackType],
        });
        return files.Count == 1 ? files[0].TryGetLocalPath() : null;
    }
    public async Task<string?> PickExportAsync(string model)
    {
        if (topLevel() is not { } top) return null;
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = loc["ModelSetupSavePackTitle"], SuggestedFileName = model.Replace(':', '-') + ".dzmodel",
            DefaultExtension = "dzmodel", FileTypeChoices = [PackType], ShowOverwritePrompt = true,
        });
        return file?.TryGetLocalPath();
    }
}