using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>Article numbers as written in Algerian texts: «106», «124 مكرر», «124 مكرر 1», «الأولى».</summary>
public static partial class ArticleNumbers
{
    /// <summary>"124مكرر1", "١٢٤ مكرر ١" and "124 مكرر 1" all become "124 مكرر 1"; «الأولى» becomes "1".</summary>
    public static string Canonical(string number)
    {
        ArgumentNullException.ThrowIfNull(number);
        var text = ArabicScript.ToAsciiDigits(ArabicScript.StripDiacritics(number)).Trim();
        if (text is "الأولى" or "الاولى" or "الأولي" or "الاولي")
        {
            return "1";
        }

        var m = Parts().Match(text);
        if (!m.Success)
        {
            return text;
        }

        var result = m.Groups[1].Value.TrimStart('0');
        if (m.Groups[2].Success)
        {
            result += " مكرر";
            if (m.Groups[3].Success)
            {
                result += " " + m.Groups[3].Value.TrimStart('0');
            }
        }

        return result;
    }

    /// <summary>The numeric part ("124 مكرر 1" → 124), or null for a non-numeric number.</summary>
    public static int? Base(string canonical)
    {
        ArgumentNullException.ThrowIfNull(canonical);
        var digits = new string(canonical.TakeWhile(char.IsAsciiDigit).ToArray());
        return int.TryParse(digits, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    public static bool IsBis(string canonical) => canonical.Contains("مكرر", StringComparison.Ordinal);

    [GeneratedRegex(@"^(\d+)\s*(مكرر)?\s*(\d+)?$")]
    private static partial Regex Parts();
}
