using DzLegal.Presentation.Services;
using DzLegal.Storage;
using Microsoft.JSInterop;

namespace DzLegal.Web.Services;

/// <summary>
/// Files in the browser: an export becomes a download, a copy goes to the clipboard. Nothing is written on the server.
/// </summary>
public sealed class BrowserFiles(IJSRuntime js) : IUserFiles
{
    public Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind) =>
        Task.FromResult<(Stream Stream, string Name)?>((new DownloadStream(js, suggestedName), suggestedName));

    private (Stream Stream, string Name)? _picked;

    /// <summary>
    /// A draft file the visitor chose with the page's file input (a real input, so every browser lets it open). The
    /// next <see cref="OpenAsync"/> hands it to the app; the file never goes further than this session's memory.
    /// </summary>
    public void Pick(Stream stream, string name) => _picked = (stream, name);

    public Task<(Stream Stream, string Name)?> OpenAsync()
    {
        var picked = _picked;
        _picked = null;
        return Task.FromResult(picked);
    }

    public async Task CopyTextAsync(string text) => await js.InvokeVoidAsync("dzlegal.copyText", text);

    /// <summary>Collects what the app writes, then hands it to the browser as a download when the app closes the stream.</summary>
    private sealed class DownloadStream(IJSRuntime js, string name) : MemoryStream
    {
        private bool _sent;

        public override async ValueTask DisposeAsync()
        {
            if (!_sent)
            {
                _sent = true;
                using var content = new MemoryStream(ToArray());
                using var reference = new DotNetStreamReference(content);
                await js.InvokeVoidAsync("dzlegal.download", name, reference);
            }

            await base.DisposeAsync();
        }
    }
}

/// <summary>Opens official-source links (Journal Officiel, ministry) in a new browser tab.</summary>
public sealed class BrowserLinks(IJSRuntime js) : IExternalLinks
{
    public async Task<bool> OpenAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        await js.InvokeVoidAsync("open", uri.ToString(), "_blank", "noopener");
        return true;
    }
}
