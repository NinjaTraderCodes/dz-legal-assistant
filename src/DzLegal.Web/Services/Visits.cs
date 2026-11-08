using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using DzLegal.Web.Site;
using Npgsql;

namespace DzLegal.Web.Services;

/// <summary>
/// One counted event of the website or the demo. It holds nothing that identifies a visitor, by construction: no network
/// address, no browser string, no cookie or other identifier, and never anything typed. The privacy page says so
/// (site/src/i18n/site.*.json, privacy.site.1 and privacy.demo.6).
/// </summary>
/// <param name="Page">The page's path, without its query (/en/, /app).</param>
/// <param name="Lang">en, ar, or - when unknown.</param>
/// <param name="Referrer">The referring site's host only (example.org), (this site), or (none).</param>
/// <param name="Arrival">The browser's Sec-Fetch-Site: cross-site, same-origin, same-site, none, or -.</param>
/// <param name="Detail">For demo events: the sample's id, the guide step, the AI call's kind, the export's format.</param>
public sealed record VisitEvent(DateTimeOffset At, string Kind, string Page, string Lang, string Referrer, string Arrival, string Detail);

public static class VisitKinds
{
    public const string Page = "page";
    public const string Sample = "sample";
    public const string Guide = "guide";
    public const string Ai = "ai";
    public const string Export = "export";
}

/// <summary>Where counted events are kept: the host's database (PostgreSQL) on the demo server, memory in tests.</summary>
public interface IVisitStore
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);

    Task AddAsync(IReadOnlyList<VisitEvent> events, CancellationToken cancellationToken);

    /// <summary>The events since <paramref name="since"/>, oldest first, at most <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<VisitEvent>> ReadAsync(DateTimeOffset since, int limit, CancellationToken cancellationToken);
}

/// <summary>Which requests count as a visit, and what is kept of them. Pure functions of the request, for testing.</summary>
public static partial class VisitFilter
{
    private const int MaxLength = 120;

    /// <summary>
    /// A page someone looked at: a GET answered with an HTML page, from a browser (not our uptime checks, a bot, a link
    /// preview, a headless test browser or a prefetch), and not one of the demo's or the server's own paths.
    /// </summary>
    public static bool ShouldCount(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) || context.Response.StatusCode != StatusCodes.Status200OK
            || context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true)
        {
            return false;
        }

        var path = request.Path.Value ?? "/";
        if (path.StartsWith("/_", StringComparison.Ordinal) || path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/stats", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/lang/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var purpose = request.Headers["Sec-Purpose"].ToString() + request.Headers["Purpose"].ToString();
        return !purpose.Contains("prefetch", StringComparison.OrdinalIgnoreCase) && IsPerson(request.Headers.UserAgent.ToString());
    }

    /// <summary>A browser a person uses; the browser string is only read here, never kept.</summary>
    public static bool IsPerson(string userAgent) => userAgent.Length > 0 && !NotAPerson().IsMatch(userAgent);

    public static VisitEvent PageView(HttpContext context, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Request;
        var path = Cut((request.Path.Value ?? "/").ToLowerInvariant());
        var lang = path.StartsWith("/en", StringComparison.Ordinal) ? "en"
            : path.StartsWith("/ar", StringComparison.Ordinal) ? "ar"
            : LanguageChoice.FromRequest(request);
        var detail = request.Query["guide"] == "1" ? "guide" : string.Empty;
        return new VisitEvent(at, VisitKinds.Page, path, lang, Referrer(request), Arrival(request), detail);
    }

    /// <summary>The referring site's host only: never its path or query, which could carry someone's details.</summary>
    public static string Referrer(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var header = request.Headers.Referer.ToString();
        if (header.Length == 0 || !Uri.TryCreate(header, UriKind.Absolute, out var uri) || uri.Host.Length == 0)
        {
            return "(none)";
        }

        return string.Equals(uri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase) ? "(this site)" : Cut(uri.Host.ToLowerInvariant());
    }

    /// <summary>
    /// Where the browser says the navigation came from. The site sends no referrer between its own pages, so this tells
    /// a click inside the site (same-origin) from an arrival (cross-site from another site, none for a typed address).
    /// </summary>
    public static string Arrival(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var site = request.Headers["Sec-Fetch-Site"].ToString().ToLowerInvariant();
        return site is "cross-site" or "same-origin" or "same-site" or "none" ? site : "-";
    }

    public static string Cut(string value) => value.Length <= MaxLength ? value : value[..MaxLength];

    [GeneratedRegex(@"bot|crawl|spider|slurp|curl|wget|python|go-http|java/|okhttp|headless|playwright|puppeteer|lighthouse|pagespeed|uptime|monitor|pingdom|preview|facebookexternalhit|whatsapp|telegram|slack|discord|linkedin|embedly|skype|bing|yandex|google-inspection|ahrefs|semrush", RegexOptions.IgnoreCase)]
    private static partial Regex NotAPerson();
}

/// <summary>
/// Takes counted events from the page and the demo without making them wait: they go into a bounded queue that a
/// background loop writes to the store in batches. Without a store (no DATABASE_URL), nothing is recorded at all. A
/// store that fails never reaches the visitor: the batch is dropped and the server log says so, at most once a minute.
/// </summary>
public sealed partial class VisitRecorder : BackgroundService
{
    private readonly Channel<VisitEvent> _queue = Channel.CreateBounded<VisitEvent>(new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
    });

    private readonly ILogger<VisitRecorder> _log;
    private readonly TimeProvider _time;
    private DateTimeOffset _lastWarning = DateTimeOffset.MinValue;

    public VisitRecorder(IVisitStore? store, ILogger<VisitRecorder> log, TimeProvider? time = null)
    {
        Store = store;
        _log = log;
        _time = time ?? TimeProvider.System;
    }

    public IVisitStore? Store { get; }

    public bool Enabled => Store is not null;

    public DateTimeOffset Now => _time.GetUtcNow();

    public void Record(VisitEvent visit)
    {
        if (Enabled)
        {
            _queue.Writer.TryWrite(visit);
        }
    }

    /// <summary>A step in the demo; the page is always /app and the referrer is the demo's own page view's.</summary>
    public void Demo(string kind, string lang, string detail) =>
        Record(new VisitEvent(Now, kind, "/app", lang, string.Empty, string.Empty, VisitFilter.Cut(detail)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Store is null)
        {
            return;
        }

        // The database may still be waking up when the server starts: try again every minute until the table exists.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Store.EnsureReadyAsync(stoppingToken);
                StoreReady(_log);
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Warn(ex.Message);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        var batch = new List<VisitEvent>(200);
        while (await _queue.Reader.WaitToReadAsync(stoppingToken))
        {
            batch.Clear();
            while (batch.Count < 200 && _queue.Reader.TryRead(out var visit))
            {
                batch.Add(visit);
            }

            try
            {
                await Store.AddAsync(batch, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Warn(ex.Message);
            }
        }
    }

    /// <summary>Writes what is still queued when the server stops (a deploy or the host's restart).</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (Store is null)
        {
            return;
        }

        var rest = new List<VisitEvent>();
        while (_queue.Reader.TryRead(out var visit))
        {
            rest.Add(visit);
        }

        if (rest.Count > 0)
        {
            try
            {
                await Store.AddAsync(rest, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Warn(ex.Message);
            }
        }
    }

    private void Warn(string reason)
    {
        var now = _time.GetUtcNow();
        if (now - _lastWarning >= TimeSpan.FromMinutes(1))
        {
            _lastWarning = now;
            StoreFailed(_log, reason);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Visit counting: the database is ready.")]
    private static partial void StoreReady(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Visit counting: the database did not answer ({Reason}); the events are dropped.")]
    private static partial void StoreFailed(ILogger logger, string reason);
}

/// <summary>The events in the host's PostgreSQL database (Render's internal connection), one row each.</summary>
public sealed class PostgresVisitStore(NpgsqlDataSource source) : IVisitStore, IAsyncDisposable
{
    /// <summary>The store for Render's DATABASE_URL (postgresql://user:password@host:port/database), or null without one.</summary>
    public static PostgresVisitStore? FromUrl(string? url) =>
        ConnectionString(url) is { } connection ? new PostgresVisitStore(NpgsqlDataSource.Create(connection)) : null;

    /// <summary>Turns the URL form the host gives into the key=value form the driver reads; null for anything else.</summary>
    public static string? ConnectionString(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("postgres" or "postgresql") || uri.Host.Length == 0)
        {
            return null;
        }

        var user = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(user[0]),
            Password = user.Length > 1 ? Uri.UnescapeDataString(user[1]) : null,
            // The internal address needs no encryption, the external one requires it: prefer it whenever offered.
            SslMode = SslMode.Prefer,
            Timeout = 10,
            CommandTimeout = 15,
        };
        return builder.ConnectionString;
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand("""
            CREATE TABLE IF NOT EXISTS visit_events (
                id bigserial PRIMARY KEY,
                at timestamptz NOT NULL,
                kind text NOT NULL,
                page text NOT NULL,
                lang text NOT NULL,
                referrer text NOT NULL,
                arrival text NOT NULL,
                detail text NOT NULL);
            CREATE INDEX IF NOT EXISTS visit_events_at ON visit_events (at);
            """);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddAsync(IReadOnlyList<VisitEvent> events, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0)
        {
            return;
        }

        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var writer = await connection.BeginBinaryImportAsync(
            "COPY visit_events (at, kind, page, lang, referrer, arrival, detail) FROM STDIN (FORMAT BINARY)", cancellationToken);
        foreach (var e in events)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(e.At.ToUniversalTime(), NpgsqlTypes.NpgsqlDbType.TimestampTz, cancellationToken);
            await writer.WriteAsync(e.Kind, cancellationToken);
            await writer.WriteAsync(e.Page, cancellationToken);
            await writer.WriteAsync(e.Lang, cancellationToken);
            await writer.WriteAsync(e.Referrer, cancellationToken);
            await writer.WriteAsync(e.Arrival, cancellationToken);
            await writer.WriteAsync(e.Detail, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VisitEvent>> ReadAsync(DateTimeOffset since, int limit, CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand(
            "SELECT at, kind, page, lang, referrer, arrival, detail FROM visit_events WHERE at >= $1 ORDER BY at LIMIT $2");
        command.Parameters.AddWithValue(since.ToUniversalTime());
        command.Parameters.AddWithValue(limit);
        var events = new List<VisitEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(new VisitEvent(
                new DateTimeOffset(reader.GetDateTime(0), TimeSpan.Zero),
                reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6)));
        }

        return events;
    }

    public ValueTask DisposeAsync() => source.DisposeAsync();
}

/// <summary>Events kept in memory, for tests and for running the server locally without a database.</summary>
public sealed class MemoryVisitStore : IVisitStore
{
    private readonly List<VisitEvent> _events = [];
    private readonly Lock _lock = new();

    public Task EnsureReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AddAsync(IReadOnlyList<VisitEvent> events, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _events.AddRange(events);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VisitEvent>> ReadAsync(DateTimeOffset since, int limit, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<VisitEvent>>(_events.Where(e => e.At >= since).OrderBy(e => e.At).Take(limit).ToList());
        }
    }

    public IReadOnlyList<VisitEvent> Snapshot()
    {
        lock (_lock)
        {
            return [.. _events];
        }
    }
}

internal static class VisitTime
{
    /// <summary>Algeria's time (UTC+1 all year, no daylight saving), for reading the stats as the owner lives them.</summary>
    public static readonly TimeSpan Algeria = TimeSpan.FromHours(1);

    public static string Local(DateTimeOffset at) => at.ToOffset(Algeria).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public static string Day(DateTimeOffset at) => at.ToOffset(Algeria).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
