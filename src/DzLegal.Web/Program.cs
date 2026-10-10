using DzLegal.Web.Components;
using DzLegal.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton(EngineSettings.From(builder.Configuration));
builder.Services.AddSingleton(sp => new WebEngine(
    sp.GetRequiredService<EngineSettings>(),
    AppContext.BaseDirectory,
    sp.GetRequiredService<ILogger<WebEngine>>()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

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

// For uptime checks: is the server up, and which engines are configured (names only, never keys)?
app.MapGet("/healthz", (WebEngine engine) => Results.Ok(new
{
    status = "ok",
    engines = engine.EngineNames,
    queue = new { capacity = engine.Gate.Capacity, waiting = engine.Gate.Waiting },
}));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
