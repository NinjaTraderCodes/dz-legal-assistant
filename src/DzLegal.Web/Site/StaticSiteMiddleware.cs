using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace DzLegal.Web.Site;

internal static class StaticSiteMiddleware
{
    private static readonly object AnswerKey = new();

    // The text the site serves: pages, scripts, SVG, robots, the sitemap. Fonts and images are compressed formats already.
    private static readonly string[] CompressibleTypes =
    [
        "text/html", "text/css", "text/javascript", "application/javascript", "application/json",
        "application/xml", "text/xml", "text/plain", "image/svg+xml", "application/manifest+json",
    ];

    /// <summary>
    /// The site leaves compressed (Brotli, else gzip), as the host's proxy already does for the visitor, so a local
    /// measurement sees what the visitor gets. Only the site is compressed: its files are the same for everyone,
    /// whereas the demo's page carries the antiforgery token, which compressing would expose (BREACH); the demo and
    /// its circuit are never touched.
    /// </summary>
    public static IServiceCollection AddSiteCompression(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        // At the fastest level the Arabic home leaves at 31 KB; one level up gives 22 KB, at a cost
        // of a few milliseconds for a page every visitor downloads.
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
        return services.AddResponseCompression(options =>
        {
            // Behind the host's proxy every request counts as HTTPS; the site holds no secret, so this is safe here.
            options.EnableForHttps = true;
            options.MimeTypes = CompressibleTypes;
        });
    }

    /// <summary>
    /// Answers GET and HEAD requests for the built website. Requests an endpoint already claimed (the demo, the
    /// framework's assets, the health check) pass through untouched, so nothing of the demo can be shadowed.
    /// </summary>
    public static IApplicationBuilder UseStaticSite(this IApplicationBuilder app, StaticSite site)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(site);
        if (!site.IsAvailable)
        {
            return app;
        }

        // The decision is taken once, before compression is switched on for the request, and kept for the sender.
        return app.UseWhen(context => Answer(site, context) is not null, branch => branch.UseResponseCompression().Run(async context =>
        {
            var answer = Answer(site, context)!;
            if (answer.RedirectTo is not null)
            {
                context.Response.Redirect(answer.RedirectTo + context.Request.QueryString.Value, permanent: true);
                return;
            }

            if (answer.Language is not null)
            {
                context.Response.Headers.Vary = "Cookie, Accept-Language";
                context.Response.Headers.ContentLanguage = answer.Language;
            }

            await SendAsync(context, answer.File, answer.RequestPath);
        }));
    }

    private sealed record SiteAnswer(string File, string RequestPath, string? RedirectTo, string? Language);

    private static SiteAnswer? Answer(StaticSite site, HttpContext context)
    {
        if (context.Items.TryGetValue(AnswerKey, out var known))
        {
            return known as SiteAnswer;
        }

        var answer = Resolve(site, context);
        context.Items[AnswerKey] = answer;
        return answer;
    }

    private static SiteAnswer? Resolve(StaticSite site, HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return null;
        }

        var path = request.Path.Value ?? "/";
        if (path == "/")
        {
            if (!site.ServeRoot)
            {
                return null;
            }

            // The home page in the visitor's language, at the submitted address itself: no redirect, so link
            // previews and plain fetches see the page.
            var language = LanguageChoice.FromRequest(request);
            return new SiteAnswer(site.HomeFile(language)!, path, null, language);
        }

        if (context.GetEndpoint() is not null || !site.TryResolve(path, out var file, out var redirectTo))
        {
            return null;
        }

        return new SiteAnswer(file, path, redirectTo, null);
    }

    private static async Task SendAsync(HttpContext context, string file, string requestPath)
    {
        var response = context.Response;
        var contentType = StaticSite.ContentTypeOf(file);
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = contentType.StartsWith("text/", StringComparison.Ordinal) ? contentType + "; charset=utf-8" : contentType;
        response.Headers.CacheControl = StaticSite.CacheControlOf(file, requestPath);
        response.ContentLength = new FileInfo(file).Length;
        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }

        await response.SendFileAsync(file, context.RequestAborted);
    }
}
