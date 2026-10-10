using Avalonia.Controls;

namespace DzLegal.App.Services;

/// <summary>Uses the operating system's default browser.</summary>
public sealed class AvaloniaExternalLinks(Func<TopLevel?> topLevel) : IExternalLinks
{
    public async Task<bool> OpenAsync(Uri uri) =>
        topLevel() is { } top && await top.Launcher.LaunchUriAsync(uri);
}
