using System.Text.RegularExpressions;

namespace DzLegal.Drafting;

/// <summary>
/// Tells whether a text the lawyer wrote looks like Darija or mostly French rather than formal Arabic. The app supports
/// formal Arabic input; such a text is only pointed out (never refused), because the local model reads it less well.
/// Tuned to stay silent on formal text: proper names, a French term or two, and numbers do not count.
/// </summary>
public static partial class WritingStyle
{
    /// <summary>Common Darija words that formal legal Arabic does not use (ambiguous ones such as «لي» are left out).</summary>
    private static readonly HashSet<string> DarijaWords = new(StringComparer.Ordinal)
    {
        "تاع", "تاعو", "تاعه", "تاعها", "تاعهم", "نتاع", "ديال", "راه", "راهو", "راهي", "راني", "راهم", "واش", "كيفاش",
        "وقتاش", "علاش", "بزاف", "شوية", "ماشي", "باش", "هاذي", "هاد", "هاذ", "بلي", "كاين", "كاينة", "مكانش",
        "زعما", "الكرا", "كرا", "دراهمو", "ضرك", "دروك",
    };

    /// <summary>The Darija negation «ما … ش» (ما خلصش، ما رجعش، ماخلصش), a strong sign by itself.</summary>
    [GeneratedRegex(@"(?:^|[\s،.,:؛])و?ما\s?\p{IsArabic}{2,}ش(?=$|[\s،.,:؛])")]
    private static partial Regex DarijaNegation();

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex Words();

    [GeneratedRegex(@"^[A-Za-zÀ-ÿ]{3,}$")]
    private static partial Regex LatinWord();

    /// <summary>The text looks like Darija, or is mostly written in French.</summary>
    public static bool LooksInformal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var words = Words().Matches(text).Select(m => m.Value).ToList();
        if (words.Count == 0)
        {
            return false;
        }

        var latin = words.Count(w => LatinWord().IsMatch(w));
        if (latin >= 4 && latin * 4 >= words.Count)
        {
            return true;
        }

        var darija = words.Count(w => DarijaWords.Contains(w) || (w.StartsWith('و') && DarijaWords.Contains(w[1..])))
            + (2 * DarijaNegation().Count(text));
        return darija >= 2;
    }
}
