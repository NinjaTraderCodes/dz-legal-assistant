using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using static System.Net.WebUtility;

namespace DzLegal.Web.Services;

/// <summary>
/// The owner's view of the counted events: /stats (a page of tables) and /stats.csv (every event). Both exist only when
/// STATS_TOKEN is set, and ask for it as the password of the browser's own sign-in box (any user name). Every value is
/// HTML-encoded: referring hosts come from request headers anyone can write.
/// </summary>
public static class VisitStats
{
    public const int DefaultDays = 14;
    public const int MaxDays = 60;
    private const int MaxEvents = 100_000;

    public enum Access { Hidden, Ask, Allowed }

    public static Access Check(HttpRequest request, string? token)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(token))
        {
            return Access.Hidden;
        }

        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Access.Ask;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header[6..].Trim()));
        }
        catch (FormatException)
        {
            return Access.Ask;
        }

        var password = decoded.Split(':', 2) is { Length: 2 } parts ? parts[1] : string.Empty;
        // Compared as hashes, so the time taken says nothing about the token's length or its first letters.
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(password)), SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            ? Access.Allowed
            : Access.Ask;
    }

    public static int Days(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var days) ? Math.Clamp(days, 1, MaxDays) : DefaultDays;

    public static Task<IReadOnlyList<VisitEvent>> ReadAsync(IVisitStore store, DateTimeOffset now, int days, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store.ReadAsync(now - TimeSpan.FromDays(days), MaxEvents, cancellationToken);
    }

    /// <summary>An arrival from outside the site: another site, or a typed or bookmarked address.</summary>
    public static bool IsArrival(VisitEvent e) => e.Kind == VisitKinds.Page && e.Arrival is "cross-site" or "none" or "-" && e.Referrer != "(this site)";

    public static string Html(IReadOnlyList<VisitEvent> events, int days, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(events);
        var pages = events.Where(e => e.Kind == VisitKinds.Page).ToList();
        var demo = events.Where(e => e.Kind != VisitKinds.Page).ToList();
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
          .Append("<meta name=\"robots\" content=\"noindex, nofollow\"><title>Visits</title><style>")
          .Append("body{font:15px/1.5 system-ui,sans-serif;margin:24px;color:#1d1a17;background:#fbfaf6}h1{font-size:1.4rem}h2{font-size:1.1rem;margin-top:28px}")
          .Append("table{border-collapse:collapse;margin-top:8px}th,td{padding:4px 10px;border-bottom:1px solid #ddd6c8;text-align:left;vertical-align:top}")
          .Append("td.n,th.n{text-align:right;font-variant-numeric:tabular-nums}.muted{color:#6b6458}.totals span{display:inline-block;margin-right:20px}</style></head><body>")
          .Append(CultureInfo.InvariantCulture, $"<h1>Visits, last {days} days</h1>")
          .Append(CultureInfo.InvariantCulture, $"<p class=\"muted\">Algeria time (UTC+1). Generated {VisitTime.Local(now)}. Anonymous counts: no address, no browser string, no identifier, nothing typed. <a href=\"/stats.csv?days={days}\">Download every event (CSV)</a></p>");

        sb.Append("<p class=\"totals\">")
          .Append(Total("page views", pages.Count))
          .Append(Total("arrivals from outside", pages.Count(IsArrival)))
          .Append(Total("demo opened", pages.Count(e => e.Page == "/app")))
          .Append(Total("analyses", demo.Count(e => e.Kind == VisitKinds.Ai && e.Detail == "grounds.reading")))
          .Append(Total("drafts", demo.Count(e => e.Kind == VisitKinds.Ai && e.Detail == "grounds-argued")))
          .Append(Total("exports", demo.Count(e => e.Kind == VisitKinds.Export)))
          .Append("</p>");

        Table(sb, "By day", ["Day", "Page views", "Arrivals", "Demo opened", "AI calls", "Guide steps", "Exports"],
            events.GroupBy(e => VisitTime.Day(e.At)).OrderByDescending(g => g.Key).Select(g => new[]
            {
                g.Key,
                N(g.Count(e => e.Kind == VisitKinds.Page)),
                N(g.Count(IsArrival)),
                N(g.Count(e => e.Kind == VisitKinds.Page && e.Page == "/app")),
                N(g.Count(e => e.Kind == VisitKinds.Ai)),
                N(g.Count(e => e.Kind == VisitKinds.Guide)),
                N(g.Count(e => e.Kind == VisitKinds.Export)),
            }), numeric: 1);

        Table(sb, "Where arrivals came from (referring site, as the browser sent it)", ["Referring site", "Arrivals", "First", "Last"],
            pages.Where(IsArrival).GroupBy(e => e.Referrer).OrderByDescending(g => g.Count()).Select(g => new[]
            {
                g.Key, N(g.Count()), VisitTime.Local(g.Min(e => e.At)), VisitTime.Local(g.Max(e => e.At)),
            }), numeric: 1, last: 2);

        Table(sb, "Pages", ["Page", "Language", "Views"],
            pages.GroupBy(e => (e.Page, e.Lang)).OrderByDescending(g => g.Count()).Select(g => new[] { g.Key.Page, g.Key.Lang, N(g.Count()) }), numeric: 2);

        Table(sb, "In the demo", ["Event", "Detail", "Count", "Last"],
            demo.GroupBy(e => (e.Kind, e.Detail)).OrderBy(g => g.Key.Kind, StringComparer.Ordinal).ThenBy(g => g.Key.Detail, StringComparer.Ordinal)
                .Select(g => new[] { g.Key.Kind, g.Key.Detail, N(g.Count()), VisitTime.Local(g.Max(e => e.At)) }), numeric: 2, last: 1);

        Table(sb, "Last 150 events", ["Time", "Event", "Page", "Language", "Referring site", "Arrival", "Detail"],
            events.OrderByDescending(e => e.At).Take(150).Select(e => new[] { VisitTime.Local(e.At), e.Kind, e.Page, e.Lang, e.Referrer, e.Arrival, e.Detail }));

        return sb.Append("</body></html>").ToString();
    }

    public static string Csv(IReadOnlyList<VisitEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var sb = new StringBuilder("time_utc,time_algeria,event,page,language,referring_site,arrival,detail\r\n");
        foreach (var e in events)
        {
            sb.AppendJoin(',', new[]
            {
                e.At.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), VisitTime.Local(e.At),
                Cell(e.Kind), Cell(e.Page), Cell(e.Lang), Cell(e.Referrer), Cell(e.Arrival), Cell(e.Detail),
            }).Append("\r\n");
        }

        return sb.ToString();
    }

    /// <summary>A CSV cell: quoted when needed, and never read as a formula by a spreadsheet.</summary>
    private static string Cell(string value)
    {
        var safe = value.Length > 0 && value[0] is '=' or '+' or '-' or '@' && value != "-" ? "'" + value : value;
        return safe.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"" : safe;
    }

    private static string Total(string label, int count) => $"<span><strong>{N(count)}</strong> {label}</span>";

    private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <param name="numeric">Columns from this index on are right-aligned numbers, until the last <paramref name="last"/>.</param>
    private static void Table(StringBuilder sb, string title, string[] head, IEnumerable<string[]> rows, int numeric = int.MaxValue, int last = 0)
    {
        bool IsNumber(int i) => i >= numeric && i < head.Length - last;
        sb.Append("<h2>").Append(HtmlEncode(title)).Append("</h2><table><thead><tr>");
        for (var i = 0; i < head.Length; i++)
        {
            sb.Append(IsNumber(i) ? "<th class=\"n\">" : "<th>").Append(HtmlEncode(head[i])).Append("</th>");
        }

        sb.Append("</tr></thead><tbody>");
        var any = false;
        foreach (var row in rows)
        {
            any = true;
            sb.Append("<tr>");
            for (var i = 0; i < row.Length; i++)
            {
                sb.Append(IsNumber(i) ? "<td class=\"n\">" : "<td>").Append(HtmlEncode(row[i])).Append("</td>");
            }

            sb.Append("</tr>");
        }

        if (!any)
        {
            sb.Append(CultureInfo.InvariantCulture, $"<tr><td colspan=\"{head.Length}\" class=\"muted\">Nothing yet.</td></tr>");
        }

        sb.Append("</tbody></table>");
    }
}
