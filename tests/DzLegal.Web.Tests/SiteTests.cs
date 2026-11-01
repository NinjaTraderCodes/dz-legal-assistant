using DzLegal.Web.Site;
using Microsoft.AspNetCore.Http;

namespace DzLegal.Web.Tests;

public sealed class LanguageChoiceTests
{
    [Theory]
    [InlineData("ar-DZ,fr;q=0.8,en;q=0.5", true)]
    [InlineData("ar", true)]
    [InlineData("AR-SA", true)]
    [InlineData("fr-FR,ar;q=0.9", false)]
    [InlineData("en-US,en;q=0.9", false)]
    [InlineData("fr;q=0.5,ar;q=0.9", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("*", false)]
    public void TheBrowserLanguageDecides(string? acceptLanguage, bool arabic)
        => Assert.Equal(arabic, LanguageChoice.PrefersArabic(acceptLanguage));

    [Fact]
    public void ARememberedChoiceWinsOverTheBrowser()
        => Assert.Equal("en", LanguageChoice.Resolve("en", null, "ar-DZ"));

    [Fact]
    public void TheQueryWinsOverTheBrowserButNotOverTheCookie()
    {
        Assert.Equal("ar", LanguageChoice.Resolve(null, "ar", "en-US"));
        Assert.Equal("en", LanguageChoice.Resolve("en", "ar", "ar"));
    }

    [Fact]
    public void UnknownValuesAreIgnored()
    {
        Assert.Equal("en", LanguageChoice.Resolve("de", "xx", "fr"));
        Assert.Equal("ar", LanguageChoice.Resolve("de", "xx", "ar"));
    }

    [Fact]
    public void TheDirectionFollowsTheLanguage()
    {
        Assert.Equal("rtl", LanguageChoice.Dir("ar"));
        Assert.Equal("ltr", LanguageChoice.Dir("en"));
    }
}

public sealed class LanguageSwitchTests
{
    [Theory]
    [InlineData("ar", "/ar/download/", "/ar/download/")]
    [InlineData("en", null, "/en/")]
    [InlineData("en", "", "/en/")]
    [InlineData("en", "//evil.example", "/en/")]
    [InlineData("en", "https://evil.example/", "/en/")]
    [InlineData("en", "/\\evil.example", "/en/")]
    [InlineData("en", "relative", "/en/")]
    public void TheSwitchOnlyReturnsToThisSite(string code, string? to, string expected)
        => Assert.Equal(expected, LanguageSwitch.Plan(code, to));

    [Theory]
    [InlineData("de")]
    [InlineData("")]
    [InlineData(null)]
    public void AnUnknownLanguageIsRefused(string? code) => Assert.Null(LanguageSwitch.Plan(code, "/"));

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("dark", true)]
    [InlineData("anything", true)]
    [InlineData("light", false)]
    public void DarkIsTheDefaultUnlessLightWasChosen(string? cookie, bool dark)
        => Assert.Equal(dark, LanguageSwitch.IsDarkTheme(cookie));

    [Fact]
    public void TheCookieIsAYearLongAndNotHttpOnly()
    {
        var options = LanguageSwitch.CookieOptions(secure: true);
        Assert.Equal("/", options.Path);
        Assert.Equal(TimeSpan.FromDays(365), options.MaxAge);
        Assert.True(options.Secure);
        Assert.False(options.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.SameSite);
    }
}

public sealed class StaticSiteTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dzlegal-site-" + Guid.NewGuid().ToString("N"));

    public StaticSiteTests()
    {
        foreach (var file in new[] { "ar/index.html", "en/index.html", "ar/404/index.html", "en/download/index.html", "_astro/site.css", "robots.txt" })
        {
            var path = Path.Combine(_root, file.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file);
        }
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private StaticSite Site(bool serveRoot = true) => StaticSite.Probe(new SiteOptions { Root = _root, ServeRoot = serveRoot }, Path.GetTempPath());

    [Fact]
    public void ABuiltSiteIsAvailable()
    {
        var site = Site();
        Assert.True(site.IsAvailable);
        Assert.True(site.ServeRoot);
        Assert.Equal(Path.Combine(_root, "ar", "index.html"), site.HomeFile("ar"));
        Assert.Equal(Path.Combine(_root, "ar", "404", "index.html"), site.NotFoundFile("ar"));
        Assert.Null(site.NotFoundFile("en"));
    }

    [Fact]
    public void AMissingSiteChangesNothing()
    {
        var site = StaticSite.Probe(new SiteOptions { Root = Path.Combine(_root, "nowhere"), ServeRoot = true }, Path.GetTempPath());
        Assert.False(site.IsAvailable);
        Assert.False(site.ServeRoot);
        Assert.Null(site.HomeFile("ar"));
        Assert.False(site.TryResolve("/ar/", out _, out _));
    }

    [Fact]
    public void TheRootServesTheSiteWhenAsked()
    {
        var site = Site(serveRoot: true);
        Assert.True(site.ServeRoot);
        Assert.False(site.DemoAnswersRoot);
        Assert.Equal(Path.Combine(_root, "en", "index.html"), site.HomeFile("en"));
    }

    [Fact]
    public void TheDemoAnswersTheRootWhenTheSwitchIsOff()
    {
        var site = Site(serveRoot: false);
        Assert.False(site.ServeRoot);
        Assert.True(site.DemoAnswersRoot);
    }

    [Fact]
    public void TheDemoAnswersTheRootWhenTheSiteFolderIsMissing()
    {
        var site = StaticSite.Probe(new SiteOptions { Root = Path.Combine(_root, "nowhere"), ServeRoot = true }, Path.GetTempPath());
        Assert.False(site.IsAvailable);
        Assert.True(site.DemoAnswersRoot);
    }

    [Theory]
    [InlineData("/en/download/", "en/download/index.html", null)]
    [InlineData("/en/download", "", "/en/download/")]
    [InlineData("/_astro/site.css", "_astro/site.css", null)]
    [InlineData("/robots.txt", "robots.txt", null)]
    public void PathsMapToFilesAndDirectoriesRedirectToTheirSlash(string path, string file, string? redirect)
    {
        Assert.True(Site().TryResolve(path, out var resolved, out var redirectTo));
        Assert.Equal(redirect, redirectTo);
        if (file.Length > 0)
        {
            Assert.Equal(Path.Combine(_root, file.Replace('/', Path.DirectorySeparatorChar)), resolved);
        }
    }

    [Theory]
    [InlineData("/en/missing/")]
    [InlineData("/en/download/index.html/")]
    [InlineData("/../robots.txt")]
    [InlineData("/en/../../x")]
    [InlineData("/.git/config")]
    [InlineData("/en\\download")]
    [InlineData("robots.txt")]
    public void NothingOutsideTheSiteIsServed(string path) => Assert.False(Site().TryResolve(path, out _, out _));

    [Fact]
    public void HtmlIsRevalidatedAndHashedAssetsAreImmutable()
    {
        Assert.Equal("no-cache", StaticSite.CacheControlOf("x/index.html", "/x/"));
        Assert.Equal("public, max-age=31536000, immutable", StaticSite.CacheControlOf("_astro/a.css", "/_astro/a.css"));
        Assert.Equal("public, max-age=86400", StaticSite.CacheControlOf("robots.txt", "/robots.txt"));
        Assert.Equal("text/html", StaticSite.ContentTypeOf("index.html"));
        Assert.Equal("font/woff2", StaticSite.ContentTypeOf("a.woff2"));
    }
}

public sealed class SecurityHeadersTests
{
    [Fact]
    public void ThePolicyAllowsOnlyThisOrigin()
    {
        var headers = new HeaderDictionary();
        SecurityHeaders.Apply(headers);
        var policy = headers["Content-Security-Policy"].ToString();
        Assert.Contains("script-src 'self'", policy, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", policy, StringComparison.Ordinal);
        Assert.Single(headers["Content-Security-Policy"]);
        Assert.Equal("no-referrer", headers["Referrer-Policy"]);
        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
    }
}
