using DzLegal.Web.Components;
using DzLegal.Web.Services;
using DzLegal.Web.Site;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// One message carries a whole field: the fields are bounded (FieldViewModel.MaxLength), and this leaves room for them
// in Arabic (2 bytes a letter) without letting a huge paste through.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o => o.MaximumReceiveMessageSize = 64 * 1024);
builder.Services.AddSingleton(EngineSettings.From(builder.Configuration));
builder.Services.AddSingleton(sp => new WebEngine(
    sp.GetRequiredService<EngineSettings>(),
    AppContext.BaseDirectory,
    sp.GetRequiredService<ILogger<WebEngine>>()));
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.AddSiteCompression();

builder.Services.AddHttpContextAccessor();

// Anonymous visit counts (docs/WEB-DEMO.md, "Visit counting"): only with a database, DATABASE_URL in the host's settings
// (Visits:Store = memory keeps them in memory, for a local run). Nothing identifying is ever recorded; see VisitEvent.
IVisitStore? visitStore = builder.Configuration["Visits:Store"] == "memory"
    ? new MemoryVisitStore()
    : PostgresVisitStore.FromUrl(builder.Configuration["DATABASE_URL"]);
var statsToken = builder.Configuration["STATS_TOKEN"];
builder.Services.AddSingleton(sp => new VisitRecorder(visitStore, sp.GetRequiredService<ILogger<VisitRecorder>>())
{
    OwnerMark = VisitFilter.OwnerMark(statsToken),
});
builder.Services.AddHostedService(sp => sp.GetRequiredService<VisitRecorder>());

// Behind the host's proxy (Render): the visitor's address is the last one the proxy adds to X-Forwarded-For.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();

// No framing by other sites, no third-party scripts: the page only talks to its own server.
app.Use(async (context, next) =>
{
    SecurityHeaders.Apply(context.Response.Headers);
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// A page someone looked at is counted after it was sent, so counting never delays or breaks a page.
var visits = app.Services.GetRequiredService<VisitRecorder>();
if (visits.Enabled)
{
    app.Use(async (context, next) =>
    {
        await next();
        if (VisitFilter.ShouldCount(context, visits.OwnerMark))
        {
            visits.Record(VisitFilter.PageView(context, visits.Now));
        }
    });
}

// Check which configured Gemma models Google still serves, without delaying the start (the host's wake-up is slow enough).
var engine = app.Services.GetRequiredService<WebEngine>();
_ = Task.Run(async () =>
{
    using var lookup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await engine.ResolveModelsAsync(null, lookup.Token);
});

// The public website (site/, built by Astro) is served next to the demo. Without it nothing changes: "/" is the demo
// and unknown paths show its not-found page.
var site = StaticSite.Probe(app.Services.GetRequiredService<IOptions<SiteOptions>>().Value, app.Environment.ContentRootPath);
if (site.IsAvailable)
{
    SiteLog.SiteServed(app.Logger, site.Root, site.ServeRoot);
    app.UseStatusCodePages(async statusContext =>
    {
        var context = statusContext.HttpContext;
        if (context.Response.StatusCode != StatusCodes.Status404NotFound
            || site.NotFoundFile(LanguageChoice.FromRequest(context.Request)) is not { } page)
        {
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.SendFileAsync(page, context.RequestAborted);
    });
}
else
{
    SiteLog.SiteMissing(app.Logger, site.Root);
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
}

// "/" belongs to the website once Site:ServeRoot is on. Otherwise (the switch off, or the site folder missing from the
// image) the demo answers there, rewritten internally so the submitted address keeps working without a redirect.
if (site.DemoAnswersRoot)
{
    app.UseRewriter(new RewriteOptions().AddRewrite("^$", "app", skipRemainingRules: true));
}

app.UseRouting();
app.UseAntiforgery();
app.UseStaticSite(site);

app.MapStaticAssets();

// The document font (Noto Naskh Arabic, SIL OFL), served from the copy the PDF engine embeds: one file, no duplicate.
app.MapGet("/fonts/{file}", (string file, HttpContext context) =>
{
    if (file is not ("NotoNaskhArabic-Regular.ttf" or "NotoNaskhArabic-Bold.ttf")
        || typeof(DzLegal.Documents.DocumentFonts).Assembly.GetManifestResourceStream("Fonts." + file) is not { } font)
    {
        return Results.NotFound();
    }

    context.Response.Headers.CacheControl = "public, max-age=2592000";
    return Results.Stream(font, "font/ttf");
});

// The official PDFs the law texts come from (only those the library names), opened inline at the cited page.
app.MapGet("/law-sources/{file}", (string file, WebEngine engine, HttpContext context) =>
{
    var allowed = engine.Knowledge.Library.Codes
        .SelectMany(c => c.Articles.Select(a => a.Source?.Url).Append(c.Source?.Url))
        .Select(url => Path.GetFileName(url ?? string.Empty));
    var path = Path.Combine(AppContext.BaseDirectory, "law-sources", file);
    if (!allowed.Contains(file, StringComparer.Ordinal) || !File.Exists(path))
    {
        return Results.NotFound();
    }

    context.Response.Headers.CacheControl = "public, max-age=2592000";
    return Results.File(path, "application/pdf", enableRangeProcessing: true);
});

// The visitor's language, remembered in a first-party cookie, then back to the page that asked (a local path only).
app.MapGet("/lang/{code}", (string code, string? to, HttpContext context) =>
{
    if (LanguageSwitch.Plan(code, to) is not { } target)
    {
        return Results.NotFound();
    }

    context.Response.Cookies.Append(LanguageChoice.CookieName, code, LanguageSwitch.CookieOptions(context.Request.IsHttps));
    return Results.LocalRedirect(target);
});

// For uptime checks: is the server up, is AI configured, and is the website built? Engine names and the queue stay in
// the server log. GET or HEAD, whichever the monitor sends: an external monitor calling it every 5 minutes keeps the
// free host awake (Render sleeps after 15 minutes without a request). "started" stays the same as long as the server
// never slept or restarted, so it shows whether the pings work.
var started = DateTimeOffset.UtcNow;
app.MapMethods("/healthz", ["GET", "HEAD"], (WebEngine engine) => Results.Ok(new
{
    status = "ok",
    ai = engine.Settings.HasAnyEngine,
    site = site.IsAvailable,
    started = started.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
}));

// The owner's view of the visit counts, behind the browser's sign-in box (STATS_TOKEN as the password). Without the
// token the address does not exist.
app.MapGet("/stats", async (HttpContext context, string? days) =>
{
    if (await StatsAccess(context) is { } refused)
    {
        return refused;
    }

    var span = VisitStats.Days(days);
    var events = visits.Store is { } store ? await VisitStats.ReadAsync(store, visits.Now, span, context.RequestAborted) : [];
    return Results.Content(VisitStats.Html(events, span, visits.Now), "text/html; charset=utf-8");
});
app.MapGet("/stats.csv", async (HttpContext context, string? days) =>
{
    if (await StatsAccess(context) is { } refused)
    {
        return refused;
    }

    var events = visits.Store is { } store ? await VisitStats.ReadAsync(store, visits.Now, VisitStats.Days(days), context.RequestAborted) : [];
    context.Response.Headers.ContentDisposition = "attachment; filename=\"visits.csv\"";
    return Results.Text("\uFEFF" + VisitStats.Csv(events), "text/csv; charset=utf-8");
});

// The framework would add a second Content-Security-Policy header (frame-ancestors) to the circuit's pages; the
// page's own policy already forbids framing, so one header carries the whole rule.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = null);

app.Run();

Task<IResult?> StatsAccess(HttpContext context)
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    IResult? refused = VisitStats.Check(context.Request, statsToken) switch
    {
        VisitStats.Access.Hidden => Results.NotFound(),
        VisitStats.Access.Ask => AskForToken(context),
        _ => null,
    };

    // The owner has signed in: mark this browser, so its own visits stop being counted (docs/WEB-DEMO.md).
    if (refused is null && visits.OwnerMark is { } mark)
    {
        var options = LanguageSwitch.CookieOptions(context.Request.IsHttps);
        options.HttpOnly = true;
        context.Response.Cookies.Append(VisitFilter.OwnerCookie, mark, options);
    }

    return Task.FromResult(refused);
}

static IResult AskForToken(HttpContext context)
{
    context.Response.Headers.WWWAuthenticate = "Basic realm=\"stats\", charset=\"UTF-8\"";
    return Results.Unauthorized();
}
