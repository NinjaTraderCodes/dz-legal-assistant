using Microsoft.AspNetCore.StaticFiles;

namespace DzLegal.Web.Site;

/// <summary>
/// Serves the built website (Astro's output: one folder per page with an index.html, hashed assets under /_astro)
/// from the same app as the demo. Paths the demo or the framework own always win: the middleware only answers
/// requests no endpoint matched. "/" belongs to the site only once ServeRoot is on; until then it stays the demo's.
/// When the folder is missing, nothing changes for the visitor.
/// </summary>
internal sealed class StaticSite
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private StaticSite(string root, bool available, bool serveRoot)
    {
        Root = root;
        IsAvailable = available;
        ServeRoot = serveRoot;
    }

    public string Root { get; }

    public bool IsAvailable { get; }

    public bool ServeRoot { get; }

    /// <summary>"/" shows the demo whenever the site does not take it: the switch is off, or the folder is missing.</summary>
    public bool DemoAnswersRoot => !ServeRoot;

    public static StaticSite Probe(SiteOptions options, string contentRoot)
    {
        ArgumentNullException.ThrowIfNull(options);
        var root = string.IsNullOrWhiteSpace(options.Root)
            ? Path.Combine(AppContext.BaseDirectory, "site")
            : Path.GetFullPath(options.Root, contentRoot);
        var available = File.Exists(Path.Combine(root, LanguageChoice.Arabic, "index.html"))
            && File.Exists(Path.Combine(root, LanguageChoice.English, "index.html"));
        return new StaticSite(root, available, options.ServeRoot && available);
    }

    /// <summary>The home page of a language, or null when the site is not built.</summary>
    public string? HomeFile(string language) => IsAvailable ? Path.Combine(Root, language, "index.html") : null;

    /// <summary>The not-found page of a language, or null when the site is not built.</summary>
    public string? NotFoundFile(string language)
    {
        if (!IsAvailable)
        {
            return null;
        }

        var file = Path.Combine(Root, language, "404", "index.html");
        return File.Exists(file) ? file : null;
    }

    /// <summary>
    /// Maps a request path to a file of the site. A directory with an index.html answers at its trailing-slash path
    /// and redirects to it from the bare path, as Astro's links expect. Anything outside the root, hidden, or absent
    /// is not the site's business.
    /// </summary>
    public bool TryResolve(string path, out string file, out string? redirectTo)
    {
        file = string.Empty;
        redirectTo = null;
        if (!IsAvailable || string.IsNullOrEmpty(path) || path[0] != '/' || path.Contains("..", StringComparison.Ordinal) || path.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        var relative = Uri.UnescapeDataString(path.TrimStart('/'));
        if (relative.Contains("..", StringComparison.Ordinal) || relative.Split('/').Any(part => part.StartsWith('.')))
        {
            return false;
        }

        var full = Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(Root, StringComparison.Ordinal))
        {
            return false;
        }

        if (path.EndsWith('/'))
        {
            var index = Path.Combine(full, "index.html");
            if (File.Exists(index))
            {
                file = index;
                return true;
            }

            return false;
        }

        if (File.Exists(full))
        {
            file = full;
            return true;
        }

        if (Directory.Exists(full) && File.Exists(Path.Combine(full, "index.html")))
        {
            redirectTo = path + "/";
            return true;
        }

        return false;
    }

    public static string ContentTypeOf(string file)
        => ContentTypes.TryGetContentType(file, out var type) ? type : "application/octet-stream";

    /// <summary>HTML is revalidated every time; Astro's hashed assets never change; the rest is good for a day.</summary>
    public static string CacheControlOf(string file, string requestPath)
    {
        if (file.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            return "no-cache";
        }

        if (requestPath.StartsWith("/_astro/", StringComparison.Ordinal))
        {
            return "public, max-age=31536000, immutable";
        }

        return "public, max-age=86400";
    }
}
