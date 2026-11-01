namespace DzLegal.Web.Site;

internal static class StaticSiteMiddleware
{
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

        return app.Use(async (context, next) =>
        {
            var request = context.Request;
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            {
                await next(context);
                return;
            }

            var path = request.Path.Value ?? "/";
            if (path == "/")
            {
                if (!site.ServeRoot)
                {
                    await next(context);
                    return;
                }

                // The home page in the visitor's language, at the submitted address itself: no redirect, so link
                // previews and plain fetches see the page.
                var language = LanguageChoice.FromRequest(request);
                context.Response.Headers.Vary = "Cookie, Accept-Language";
                context.Response.Headers.ContentLanguage = language;
                await SendAsync(context, site.HomeFile(language)!, path);
                return;
            }

            if (context.GetEndpoint() is not null || !site.TryResolve(path, out var file, out var redirectTo))
            {
                await next(context);
                return;
            }

            if (redirectTo is not null)
            {
                context.Response.Redirect(redirectTo + request.QueryString.Value, permanent: true);
                return;
            }

            await SendAsync(context, file, path);
        });
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
