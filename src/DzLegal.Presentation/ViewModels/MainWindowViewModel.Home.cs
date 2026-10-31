using DzLegal.Storage;

namespace DzLegal.Presentation.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly RecentCasesStore? _recentCases;
    private string? _pendingOpenPath;

    public async Task OpenRecentCaseAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await RequestOpenAsync(path);
    }

    private async Task RequestOpenAsync(string? path)
    {
        if (AskBeforeReplacing(ReplaceAction.Open)) _pendingOpenPath = path;
        else await OpenFileAsync(path);
    }

    private void RecordRecentCase(bool saved)
    {
        if (_recentCases is null || _files is not IHomeRecentFiles files) return;
        var path = saved ? files.LastSavedPath : files.LastOpenedPath;
        if (path is null || !Path.IsPathFullyQualified(path)
            || !path.EndsWith(DraftFile.Extension, StringComparison.OrdinalIgnoreCase)) return;

        var inputs = CurrentInputs();
        var subject = inputs.GetValueOrDefault("claim.subject", string.Empty);
        if (string.IsNullOrWhiteSpace(subject))
            subject = string.Join(' ', new[] { inputs.GetValueOrDefault("plaintiff.lastName"), inputs.GetValueOrDefault("plaintiff.firstName") }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (saved) _recentCases.RecordSaved(path, subject);
        else _recentCases.RecordOpened(path, subject);
    }
}
