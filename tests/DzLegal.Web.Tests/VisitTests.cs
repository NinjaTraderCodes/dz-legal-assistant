using System.Text;
using DzLegal.Drafting;
using DzLegal.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DzLegal.Web.Tests;

public class VisitFilterTests
{
    private const string Chrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private static DefaultHttpContext Page(string path, string userAgent = Chrome, string? referer = null, string? fetchSite = null, string query = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("dz-legal-assistant-ai.onrender.com");
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        context.Request.Headers.UserAgent = userAgent;
        if (referer is not null)
        {
            context.Request.Headers.Referer = referer;
        }

        if (fetchSite is not null)
        {
            context.Request.Headers["Sec-Fetch-Site"] = fetchSite;
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/html; charset=utf-8";
        return context;
    }

    [Fact]
    public void AnArrivalFromAnotherSiteKeepsOnlyThatSitesHost()
    {
        var context = Page("/en/", referer: "https://platform.example/projects/4521?reviewer=amina", fetchSite: "cross-site");

        Assert.True(VisitFilter.ShouldCount(context));
        var visit = VisitFilter.PageView(context, DateTimeOffset.UnixEpoch);
        Assert.Equal(("/en/", "en", "platform.example", "cross-site"), (visit.Page, visit.Lang, visit.Referrer, visit.Arrival));
        Assert.DoesNotContain("4521", visit.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("amina", visit.ToString(), StringComparison.Ordinal);
        Assert.True(VisitStats.IsArrival(visit));
    }

    [Fact]
    public void AClickInsideTheSiteIsAViewButNotAnArrival()
    {
        var inside = VisitFilter.PageView(Page("/ar/about/", referer: "https://dz-legal-assistant-ai.onrender.com/ar/", fetchSite: "same-origin"), DateTimeOffset.UnixEpoch);
        Assert.Equal(("ar", "(this site)"), (inside.Lang, inside.Referrer));
        Assert.False(VisitStats.IsArrival(inside));

        // The site sends no referrer between its own pages: the browser's Sec-Fetch-Site still says it was a click inside.
        var noReferrer = VisitFilter.PageView(Page("/en/law/", fetchSite: "same-origin"), DateTimeOffset.UnixEpoch);
        Assert.Equal("(none)", noReferrer.Referrer);
        Assert.False(VisitStats.IsArrival(noReferrer));

        var typed = VisitFilter.PageView(Page("/en/", fetchSite: "none"), DateTimeOffset.UnixEpoch);
        Assert.True(VisitStats.IsArrival(typed));
    }

    [Fact]
    public void TheDemoTakesTheVisitorsLanguageAndNotesTheGuideLink()
    {
        var context = Page("/app", query: "?guide=1");
        context.Request.Headers.Cookie = "dzlegal.lang=ar";

        var visit = VisitFilter.PageView(context, DateTimeOffset.UnixEpoch);

        Assert.Equal(("/app", "ar", "guide"), (visit.Page, visit.Lang, visit.Detail));
    }

    [Theory]
    [InlineData("curl/8.5.0")]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/131.0.0.0 Safari/537.36")]
    [InlineData("Slackbot-LinkExpanding 1.0 (+https://api.slack.com/robots)")]
    [InlineData("WhatsApp/2.23.20.0")]
    [InlineData("")]
    public void UptimeChecksBotsTestBrowsersAndLinkPreviewsAreNotCounted(string userAgent) =>
        Assert.False(VisitFilter.ShouldCount(Page("/en/", userAgent)));

    [Fact]
    public void PrefetchesAssetsServerPathsAndFailedRequestsAreNotCounted()
    {
        var prefetch = Page("/en/");
        prefetch.Request.Headers["Sec-Purpose"] = "prefetch";
        Assert.False(VisitFilter.ShouldCount(prefetch));

        var asset = Page("/_astro/home.css");
        asset.Response.ContentType = "text/css";
        Assert.False(VisitFilter.ShouldCount(asset));

        Assert.False(VisitFilter.ShouldCount(Page("/healthz")));
        Assert.False(VisitFilter.ShouldCount(Page("/stats")));
        Assert.False(VisitFilter.ShouldCount(Page("/_blazor")));

        var missing = Page("/en/nothing/");
        missing.Response.StatusCode = 404;
        Assert.False(VisitFilter.ShouldCount(missing));

        var post = Page("/app");
        post.Request.Method = "POST";
        Assert.False(VisitFilter.ShouldCount(post));
    }

    [Fact]
    public void TheOwnersMarkedBrowserIsNotCountedAndNoOneElseIsAffected()
    {
        var mark = VisitFilter.OwnerMark("s3cret-token");
        var owner = Page("/en/");
        owner.Request.Headers.Cookie = $"{VisitFilter.OwnerCookie}={mark}";
        Assert.False(VisitFilter.ShouldCount(owner, mark));

        var guessed = Page("/en/");
        guessed.Request.Headers.Cookie = $"{VisitFilter.OwnerCookie}=1";
        Assert.True(VisitFilter.ShouldCount(guessed, mark));
        Assert.True(VisitFilter.ShouldCount(Page("/en/"), mark));

        // Without a token there is no mark, and a cookie left from an old token no longer hides anyone.
        Assert.True(VisitFilter.ShouldCount(owner, null));
        Assert.True(VisitFilter.ShouldCount(owner, VisitFilter.OwnerMark("a-new-token")));
    }

    [Fact]
    public void TheOwnersMarkIsDerivedFromTheTokenWithoutRevealingIt()
    {
        Assert.Null(VisitFilter.OwnerMark(null));
        Assert.Null(VisitFilter.OwnerMark(string.Empty));
        var mark = VisitFilter.OwnerMark("s3cret-token")!;
        Assert.Equal(mark, VisitFilter.OwnerMark("s3cret-token"));
        Assert.Matches("^[0-9a-f]{32}$", mark);
        Assert.DoesNotContain("s3cret", mark, StringComparison.Ordinal);
        Assert.NotEqual(mark, VisitFilter.OwnerMark("s3cret-token2"));
    }

    [Fact]
    public void TheRecorderKnowsTheOwnersBrowserForTheDemosSteps()
    {
        var recorder = new VisitRecorder(new MemoryVisitStore(), NullLogger<VisitRecorder>.Instance) { OwnerMark = VisitFilter.OwnerMark("s3cret-token") };
        Assert.True(recorder.IsOwner(VisitFilter.OwnerMark("s3cret-token")));
        Assert.False(recorder.IsOwner(null));
        Assert.False(new VisitRecorder(new MemoryVisitStore(), NullLogger<VisitRecorder>.Instance).IsOwner(VisitFilter.OwnerMark("s3cret-token")));
    }

    /// <summary>
    /// The privacy page promises no address, no browser string, no identifier and nothing typed: the event has no field
    /// for any of them. Adding a field fails this test, so the promise and the privacy page are revisited together.
    /// </summary>
    [Fact]
    public void AVisitHasNoFieldThatCouldIdentifySomeone()
    {
        var fields = typeof(VisitEvent).GetProperties().Select(p => p.Name).Order(StringComparer.Ordinal);
        Assert.Equal(["Arrival", "At", "Detail", "Kind", "Lang", "Page", "Referrer"], fields);
    }
}

public class VisitStatsTests
{
    private static HttpRequest WithPassword(string? user, string password)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
        return context.Request;
    }

    [Fact]
    public void TheStatsExistOnlyWithATokenAndOpenOnlyWithIt()
    {
        Assert.Equal(VisitStats.Access.Hidden, VisitStats.Check(new DefaultHttpContext().Request, null));
        Assert.Equal(VisitStats.Access.Hidden, VisitStats.Check(WithPassword("owner", "secret"), string.Empty));
        Assert.Equal(VisitStats.Access.Ask, VisitStats.Check(new DefaultHttpContext().Request, "s3cret-token"));
        Assert.Equal(VisitStats.Access.Ask, VisitStats.Check(WithPassword("owner", "wrong"), "s3cret-token"));
        Assert.Equal(VisitStats.Access.Allowed, VisitStats.Check(WithPassword("anyone", "s3cret-token"), "s3cret-token"));

        var garbled = new DefaultHttpContext();
        garbled.Request.Headers.Authorization = "Basic not-base64!";
        Assert.Equal(VisitStats.Access.Ask, VisitStats.Check(garbled.Request, "s3cret-token"));
    }

    [Fact]
    public void ThePageCountsArrivalsBySiteAndEncodesWhatHeadersCarry()
    {
        var at = new DateTimeOffset(2026, 11, 9, 14, 0, 0, TimeSpan.Zero);
        VisitEvent[] events =
        [
            new(at, VisitKinds.Page, "/en/", "en", "platform.example", "cross-site", ""),
            new(at.AddMinutes(1), VisitKinds.Page, "/en/about/", "en", "(none)", "same-origin", ""),
            new(at.AddMinutes(2), VisitKinds.Page, "/app", "en", "(none)", "same-origin", "guide"),
            new(at.AddMinutes(3), VisitKinds.Ai, "/app", "en", "", "", "grounds.reading"),
            new(at.AddMinutes(4), VisitKinds.Page, "/ar/", "ar", "<script>alert(1)</script>", "cross-site", ""),
        ];

        var html = VisitStats.Html(events, 14, at.AddHours(1));

        Assert.Contains("<strong>2</strong> arrivals from outside", html, StringComparison.Ordinal);
        Assert.Contains("<strong>1</strong> analyses", html, StringComparison.Ordinal);
        Assert.Contains("<td>platform.example</td><td class=\"n\">1</td><td>2026-11-09 15:00</td>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCsvQuotesAndNeverStartsAFormula()
    {
        var csv = VisitStats.Csv([new(DateTimeOffset.UnixEpoch, VisitKinds.Page, "/en/", "en", "=HYPERLINK(\"x\")", "-", "a,b")]);

        Assert.StartsWith("time_utc,time_algeria,event,page,language,referring_site,arrival,detail\r\n", csv, StringComparison.Ordinal);
        Assert.Contains(",\"'=HYPERLINK(\"\"x\"\")\",-,\"a,b\"\r\n", csv, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, 14)]
    [InlineData("3", 3)]
    [InlineData("999", 60)]
    [InlineData("0", 1)]
    [InlineData("x", 14)]
    public void TheDayRangeIsBounded(string? value, int days) => Assert.Equal(days, VisitStats.Days(value));
}

public class VisitStoreTests
{
    [Fact]
    public void TheHostsDatabaseUrlBecomesAConnectionString()
    {
        var connection = PostgresVisitStore.ConnectionString("postgresql://visits_user:p%40ss@dpg-abc123-a/visits_db");

        Assert.NotNull(connection);
        Assert.Contains("Host=dpg-abc123-a", connection, StringComparison.Ordinal);
        Assert.Contains("Port=5432", connection, StringComparison.Ordinal);
        Assert.Contains("Database=visits_db", connection, StringComparison.Ordinal);
        Assert.Contains("Username=visits_user", connection, StringComparison.Ordinal);
        Assert.Contains("Password=p@ss", connection, StringComparison.Ordinal);
        Assert.Contains("Port=6543", PostgresVisitStore.ConnectionString("postgres://u:p@db.example.com:6543/d"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com/db")]
    [InlineData("not a url")]
    public void AnythingElseMeansNoCounting(string? url) => Assert.Null(PostgresVisitStore.ConnectionString(url));

    [Fact]
    public async Task WithoutAStoreNothingIsRecordedAndNothingRuns()
    {
        using var recorder = new VisitRecorder(null, NullLogger<VisitRecorder>.Instance);
        recorder.Record(new VisitEvent(DateTimeOffset.UnixEpoch, VisitKinds.Page, "/en/", "en", "(none)", "none", ""));
        await recorder.StartAsync(CancellationToken.None);
        await recorder.StopAsync(CancellationToken.None);

        Assert.False(recorder.Enabled);
    }

    [Fact]
    public async Task RecordedEventsReachTheStoreAndThoseStillQueuedAreWrittenOnStop()
    {
        var store = new MemoryVisitStore();
        using var recorder = new VisitRecorder(store, NullLogger<VisitRecorder>.Instance);
        await recorder.StartAsync(CancellationToken.None);

        recorder.Record(new VisitEvent(DateTimeOffset.UnixEpoch, VisitKinds.Page, "/en/", "en", "platform.example", "cross-site", ""));
        recorder.Demo(VisitKinds.Sample, "en", "debt");
        await recorder.StopAsync(CancellationToken.None);

        Assert.Equal([VisitKinds.Page, VisitKinds.Sample], store.Snapshot().Select(e => e.Kind));
        Assert.Equal(("/app", "debt"), (store.Snapshot()[1].Page, store.Snapshot()[1].Detail));
    }

    [Fact]
    public async Task AStoreThatFailsNeverReachesTheVisitor()
    {
        using var recorder = new VisitRecorder(new BrokenStore(), NullLogger<VisitRecorder>.Instance);
        await recorder.StartAsync(CancellationToken.None);

        recorder.Demo(VisitKinds.Guide, "ar", "1");
        await recorder.StopAsync(CancellationToken.None);

        Assert.True(recorder.Enabled);
    }

    private sealed class BrokenStore : IVisitStore
    {
        public Task EnsureReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AddAsync(IReadOnlyList<VisitEvent> events, CancellationToken cancellationToken) => throw new IOException("database unreachable");

        public Task<IReadOnlyList<VisitEvent>> ReadAsync(DateTimeOffset since, int limit, CancellationToken cancellationToken) => throw new IOException("database unreachable");
    }
}

public class AiCallCountTests
{
    [Fact]
    public async Task EachAdmittedCallIsCountedByItsKindAndARefusedOneIsNot()
    {
        using var gate = new ModelGate(1);
        var counted = new List<string>();
        using var provider = new ProtectedModelProvider(new Echo(), gate, [new CallQuota(1, "limit reached")], onCall: counted.Add);

        await provider.GenerateAsync(new ModelRequest("grounds.reading", string.Empty, "case text", "case text"), CancellationToken.None);
        await Assert.ThrowsAsync<ModelProviderException>(() => provider.GenerateAsync(new ModelRequest("grounds-argued", string.Empty, "x", "x"), CancellationToken.None));

        Assert.Equal(["grounds.reading"], counted);
    }

    private sealed class Echo : IModelProvider
    {
        public string Name => "echo";

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) => Task.FromResult(request.UserPrompt);
    }
}
