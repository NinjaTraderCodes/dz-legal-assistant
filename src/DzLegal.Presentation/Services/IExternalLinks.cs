namespace DzLegal.Presentation.Services;

/// <summary>
/// Opens a link in the lawyer's browser, only when they click it. The app itself never connects anywhere; the
/// link carries no case data.
/// </summary>
public interface IExternalLinks
{
    Task<bool> OpenAsync(Uri uri);
}

/// <summary>For contexts without a window: never opens anything.</summary>
public sealed class NoExternalLinks : IExternalLinks
{
    public Task<bool> OpenAsync(Uri uri) => Task.FromResult(false);
}
