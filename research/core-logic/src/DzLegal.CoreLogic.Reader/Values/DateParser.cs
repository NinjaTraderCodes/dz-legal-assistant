using System.Globalization;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Reader.Values;

/// <summary>
/// The dates of a note, read by code (specs/reader.md, rung 2; the labelling guide, section 8): day before month always;
/// dd/mm/yyyy and yyyy/mm/dd with «/», «-» or «.»; day, month name and year («12 مارس 2025», «12 من شهر مارس 2025»); month
/// and year (yyyy-mm); «سنة 2015» or «عام 2015» (yyyy); Arabic-Indic digits alike. A two-digit year, an impossible date, a
/// relative date or a Hijri date is not emitted.
/// <para>The span: from the first digit or month word to the end of the year. Lead words («في», «بتاريخ», «شهر», «سنة»,
/// «عام») are outside it, «من شهر» between the day and the month inside it.</para>
/// </summary>
public static partial class DateParser
{
    /// <summary>The month names of the guide (Algerian), the Egyptian ones, and the Levantine ones, with their variants.</summary>
    private static readonly (string Name, int Month)[] Months =
    [
        ("جانفي", 1), ("يناير", 1), ("كانون الثاني", 1),
        ("فيفري", 2), ("فبراير", 2), ("شباط", 2),
        ("مارس", 3), ("آذار", 3), ("اذار", 3),
        ("أفريل", 4), ("افريل", 4), ("إفريل", 4), ("أبريل", 4), ("ابريل", 4), ("إبريل", 4), ("نيسان", 4),
        ("ماي", 5), ("مايو", 5), ("أيار", 5), ("ايار", 5),
        ("جوان", 6), ("يونيو", 6), ("يونيه", 6), ("حزيران", 6),
        ("جويلية", 7), ("يوليو", 7), ("يوليه", 7), ("تموز", 7),
        ("أوت", 8), ("اوت", 8), ("أغسطس", 8), ("اغسطس", 8), ("آب", 8),
        ("سبتمبر", 9), ("أيلول", 9), ("ايلول", 9),
        ("أكتوبر", 10), ("اكتوبر", 10), ("تشرين الأول", 10), ("تشرين الاول", 10),
        ("نوفمبر", 11), ("تشرين الثاني", 11),
        ("ديسمبر", 12), ("كانون الأول", 12), ("كانون الاول", 12),
    ];

    private static readonly string MonthPattern =
        string.Join("|", Months.Select(m => Regex.Escape(m.Name)).OrderByDescending(name => name.Length));

    /// <summary>Day (optional, «من شهر» allowed), month name, year; lead words stay outside the match.</summary>
    private static readonly Regex Named = new(
        $@"(?<![\p{{L}}\d])(?:(?<day>\d{{1,2}})\s+(?:من\s+(?:شهر\s+)?)?)?(?<month>{MonthPattern})(?:\s+(?:من\s+)?(?:سنة|عام)?\s*)(?<year>\d{{4}})(?!\d)",
        RegexOptions.CultureInvariant);

    /// <summary>Every date the text gives, in the order written; the spans count UTF-16 units of <paramref name="text"/>.</summary>
    public static IReadOnlyList<ValueReading> Read(string text, NoteField field)
    {
        ArgumentNullException.ThrowIfNull(text);
        var digits = Digits.Western(text);  // same length: every digit is one UTF-16 unit in both scripts
        var found = new List<ValueReading>();
        var taken = new bool[digits.Length];

        void Add(int start, int length, string normalized)
        {
            if (Enumerable.Range(start, length).Any(i => taken[i]))
            {
                return;
            }

            for (var i = start; i < start + length; i++)
            {
                taken[i] = true;
            }

            found.Add(new ValueReading { Kind = ValueKind.Date, Normalized = normalized, Span = new TextSpan(field, start, length) });
        }

        foreach (Match m in Numeric().Matches(digits))
        {
            var (a, b, c) = (m.Groups["a"].Value, m.Groups["b"].Value, m.Groups["c"].Value);
            var date = a.Length == 4 ? Make(a, b, c) : c.Length == 4 ? Make(c, b, a) : null;
            if (date is not null)
            {
                Add(m.Index, m.Length, date);
            }
        }

        foreach (Match m in Named.Matches(digits))
        {
            var month = Months.First(x => x.Name == m.Groups["month"].Value).Month.ToString("00", CultureInfo.InvariantCulture);
            var year = m.Groups["year"].Value;
            if (m.Groups["day"].Success)
            {
                if (Make(year, month, m.Groups["day"].Value) is { } full)
                {
                    Add(m.Index, m.Length, full);
                }
            }
            else
            {
                Add(m.Index, m.Length, year + "-" + month);
            }
        }

        foreach (Match m in YearOnly().Matches(digits))
        {
            var year = m.Groups["year"];
            Add(year.Index, year.Length, year.Value);
        }

        return [.. found.OrderBy(v => v.Span.Start)];
    }

    /// <summary>yyyy-mm-dd when the parts make a real date; null otherwise.</summary>
    private static string? Make(string year, string month, string day)
    {
        var (y, mo, d) = (int.Parse(year, CultureInfo.InvariantCulture), int.Parse(month, CultureInfo.InvariantCulture),
            int.Parse(day, CultureInfo.InvariantCulture));
        if (y < 1900 || y > 2100 || mo is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(y, mo))
        {
            return null;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{y:0000}-{mo:00}-{d:00}");
    }

    [GeneratedRegex(@"(?<!\d)(?<a>\d{1,4})(?<s>[/.\-])(?<b>\d{1,2})\k<s>(?<c>\d{1,4})(?!\d)")]
    private static partial Regex Numeric();

    [GeneratedRegex(@"(?<![\p{L}])(?:سنة|عام)\s+(?<year>\d{4})(?!\d)")]
    private static partial Regex YearOnly();
}

/// <summary>Digits: Arabic-Indic (٠-٩) and Extended Arabic-Indic (۰-۹) read as Western digits, one unit for one.</summary>
public static class Digits
{
    public static string Western(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] switch
            {
                >= '٠' and <= '٩' => (char)('0' + chars[i] - '٠'),
                >= '۰' and <= '۹' => (char)('0' + chars[i] - '۰'),
                '٬' => ',',  // the Arabic thousands separator «٬»
                '٫' => '.',  // the Arabic decimal separator «٫»
                _ => chars[i],
            };
        }

        return new string(chars);
    }
}
