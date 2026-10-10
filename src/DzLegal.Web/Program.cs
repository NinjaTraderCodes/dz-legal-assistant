using DzLegal.Web.Components;
using DzLegal.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton(EngineSettings.From(builder.Configuration));
builder.Services.AddSingleton(sp => new WebEngine(
    sp.GetRequiredService<EngineSettings>(),
    AppContext.BaseDirectory,
    sp.GetRequiredService<ILogger<WebEngine>>()));

builder.Services.AddHttpContextAccessor();

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
    var headers = context.Response.Headers;
    headers.ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; " +
        "font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";
    headers.XContentTypeOptions = "nosniff";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Check which configured Gemma models Google still serves, without delaying the start (the host's wake-up is slow enough).
var engine = app.Services.GetRequiredService<WebEngine>();
_ = Task.Run(async () =>
{
    using var lookup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await engine.ResolveModelsAsync(null, lookup.Token);
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

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

// For uptime checks: is the server up, and is AI configured? Engine names and the queue stay in the server log.
app.MapGet("/healthz", (WebEngine engine) => Results.Ok(new { status = "ok", ai = engine.Settings.HasAnyEngine }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
