using Avalonia.Controls;

namespace DzLegal.App.Services;

/// <summary>
/// Opens a link in the lawyer's browser, only when they click it. The app itself never connects anywhere; the
/// link carries no case data.
/// </summary>
public interface IExternalLinks
{
    Task<bool> OpenAsync(Uri uri);
}

/// <summary>Uses the operating system's default browser.</summary>
public sealed class AvaloniaExternalLinks(Func<TopLevel?> topLevel) : IExternalLinks
{
    public async Task<bool> OpenAsync(Uri uri) =>
        topLevel() is { } top && await top.Launcher.LaunchUriAsync(uri);
}

/// <summary>For contexts without a window: never opens anything.</summary>
public sealed class NoExternalLinks : IExternalLinks
{
    public Task<bool> OpenAsync(Uri uri) => Task.FromResult(false);
}
