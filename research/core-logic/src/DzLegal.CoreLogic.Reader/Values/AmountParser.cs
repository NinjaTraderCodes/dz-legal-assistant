using System.Globalization;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Reader.Values;

/// <summary>
/// The amounts of a note, read by code (specs/reader.md, rung 2; the labelling guide, section 8). An amount is anchored on
/// its unit (دج، د.ج، دينار، سنتيم): the parser walks back from the unit over the number written before it, so that an
/// invoice number, an area or an article number is never read as money. Normal form: digits only, in dinars; centimes
/// divided by 100; a real fraction keeps two decimals after a point. A foreign currency is not emitted.
/// <para>The span: from the first number token to the end of the unit («خمسمائة ألف (500.000) دينار» is one amount).</para>
/// </summary>
public static partial class AmountParser
{
    private static readonly Dictionary<string, long> Small = new(StringComparer.Ordinal)
    {
        ["واحد"] = 1, ["واحدا"] = 1, ["واحدة"] = 1, ["أحد"] = 1, ["احد"] = 1, ["إحدى"] = 1, ["احدى"] = 1,
        ["اثنان"] = 2, ["اثنين"] = 2, ["اثنا"] = 2, ["اثني"] = 2, ["إثنان"] = 2, ["إثنين"] = 2,
        ["ثلاث"] = 3, ["ثلاثة"] = 3, ["أربع"] = 4, ["اربع"] = 4, ["أربعة"] = 4, ["اربعة"] = 4,
        ["خمس"] = 5, ["خمسة"] = 5, ["ست"] = 6, ["ستة"] = 6, ["سبع"] = 7, ["سبعة"] = 7,
        ["ثمان"] = 8, ["ثماني"] = 8, ["ثمانية"] = 8, ["تسع"] = 9, ["تسعة"] = 9, ["عشر"] = 10, ["عشرة"] = 10,
        ["عشرون"] = 20, ["عشرين"] = 20, ["ثلاثون"] = 30, ["ثلاثين"] = 30, ["أربعون"] = 40, ["اربعون"] = 40, ["أربعين"] = 40,
        ["اربعين"] = 40, ["خمسون"] = 50, ["خمسين"] = 50, ["ستون"] = 60, ["ستين"] = 60, ["سبعون"] = 70, ["سبعين"] = 70,
        ["ثمانون"] = 80, ["ثمانين"] = 80, ["تسعون"] = 90, ["تسعين"] = 90,
        ["مائة"] = 100, ["مئة"] = 100, ["مائتان"] = 200, ["مائتين"] = 200, ["مئتان"] = 200, ["مئتين"] = 200, ["مائتا"] = 200, ["مائتي"] = 200, ["مئتي"] = 200,
        ["مئتا"] = 200, ["ثلاثمائة"] = 300, ["ثلاثمئة"] = 300, ["أربعمائة"] = 400, ["اربعمائة"] = 400, ["أربعمئة"] = 400,
        ["خمسمائة"] = 500, ["خمسمئة"] = 500, ["ستمائة"] = 600, ["ستمئة"] = 600, ["سبعمائة"] = 700, ["سبعمئة"] = 700,
        ["ثمانمائة"] = 800, ["ثمانمئة"] = 800, ["تسعمائة"] = 900, ["تسعمئة"] = 900,
    };

    /// <summary>Scale words: (value, count it carries itself: 2 for a dual, 1 otherwise).</summary>
    private static readonly Dictionary<string, (long Scale, long Count)> Scales = new(StringComparer.Ordinal)
    {
        ["ألف"] = (1_000, 1), ["الف"] = (1_000, 1), ["آلاف"] = (1_000, 1), ["الاف"] = (1_000, 1), ["ألاف"] = (1_000, 1),
        ["ألفا"] = (1_000, 1), ["ألفان"] = (1_000, 2), ["ألفين"] = (1_000, 2), ["الفين"] = (1_000, 2),
        ["مليون"] = (1_000_000, 1), ["ملايين"] = (1_000_000, 1), ["مليونا"] = (1_000_000, 1), ["مليونان"] = (1_000_000, 2),
        ["مليونين"] = (1_000_000, 2),
        ["مليار"] = (1_000_000_000, 1), ["ملايير"] = (1_000_000_000, 1), ["مليارات"] = (1_000_000_000, 1),
        ["مليارا"] = (1_000_000_000, 1), ["ملياران"] = (1_000_000_000, 2), ["مليارين"] = (1_000_000_000, 2),
    };

    private static readonly string[] Dinars = ["دج", "د.ج", "دينار", "دينارا", "دينارات", "دنانير"];
    private static readonly string[] Centimes = ["سنتيم", "سنتيما", "سنتيمات"];

    public static IReadOnlyList<ValueReading> Read(string text, NoteField field)
    {
        ArgumentNullException.ThrowIfNull(text);
        var western = Digits.Western(text);
        var tokens = Tokenize(western);
        var found = new List<ValueReading>();
        var usedUpTo = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            var unit = UnitAt(tokens, i, out var unitEnd, out var centimes, out var unitTokens);
            if (unit is null)
            {
                continue;
            }

            var first = i;
            while (first > 0 && first - 1 > usedUpTo && IsNumberToken(tokens[first - 1].Word))
            {
                first--;
            }

            while (first < i && Bare(tokens[first].Word) == "و")
            {
                first++;  // a lone conjunction never starts an amount
            }

            // a glued unit («500دج») carries its digits in the same token
            var numberTokens = tokens.Skip(first).Take(i - first).Select(t => t.Word).ToList();
            var glued = GluedNumber(tokens[i].Word);
            if (glued is not null)
            {
                // «(9200000)دينار» (the training notes, 17 November 2026): digits in brackets glued to the unit repeat the
                // words before them, as in the spaced form «… ألف (500.000) دينار», so they keep their brackets.
                numberTokens.Add(tokens[i].Word.TrimStart().StartsWith('(') ? $"({glued})" : glued);
            }

            if (numberTokens.Count == 0 || Value(numberTokens) is not { } value)
            {
                i += unitTokens - 1;
                continue;
            }

            if (centimes)
            {
                value /= 100;
            }

            var startToken = tokens[first == i && glued is not null ? i : first];
            var startBare = Bare(startToken.Word);
            var start = startToken.Start + (Unprefixed(startBare).Length < startBare.Length
                ? startToken.Word.IndexOf(startBare, StringComparison.Ordinal) + 1  // the span starts after «بـ»
                : 0);
            found.Add(new ValueReading
            {
                Kind = ValueKind.Amount,
                Normalized = Normal(value),
                Span = new TextSpan(field, start, unitEnd - start),
            });
            usedUpTo = i + unitTokens - 1;
            i = usedUpTo;
        }

        return found;
    }

    public static string Normal(decimal value) =>
        value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.00", CultureInfo.InvariantCulture);

    private sealed record Token(string Word, int Start, int End);

    private static List<Token> Tokenize(string text) =>
        [.. TokenPattern().Matches(text).Select(m => new Token(m.Value, m.Index, m.Index + m.Length))];

    private static string Bare(string word) => word.TrimEnd('،', ',', '.', '؛', ';', ':', ')', '(').TrimStart('(');

    /// <summary>The unit that starts at token i, with the end of its last letter; «د ج» is two tokens.</summary>
    private static string? UnitAt(List<Token> tokens, int i, out int end, out bool centimes, out int count)
    {
        var word = tokens[i].Word;
        count = 1;
        centimes = false;
        end = tokens[i].End;
        var bare = Bare(word);
        var gluedUnit = GluedUnit(word);
        if (gluedUnit is not null)
        {
            bare = gluedUnit;
        }

        if (bare == "د" && i + 1 < tokens.Count && Bare(tokens[i + 1].Word) == "ج")
        {
            count = 2;
            end = tokens[i + 1].Start + 1;
            return "دج";
        }

        if (Dinars.Contains(bare, StringComparer.Ordinal))
        {
            end = tokens[i].Start + word.IndexOf(bare, StringComparison.Ordinal) + bare.Length;
            if (i + 1 < tokens.Count && Bare(tokens[i + 1].Word) is "جزائري" or "جزائريا" or "جزائرية")
            {
                count = 2;
                end = tokens[i + 1].Start + Bare(tokens[i + 1].Word).Length;
            }

            return bare;
        }

        if (Centimes.Contains(bare, StringComparer.Ordinal))
        {
            centimes = true;
            end = tokens[i].Start + word.IndexOf(bare, StringComparison.Ordinal) + bare.Length;
            return bare;
        }

        return null;
    }

    private static string? GluedUnit(string word)
    {
        var m = Glued().Match(Bare(word));
        return m.Success ? m.Groups["unit"].Value : null;
    }

    private static string? GluedNumber(string word)
    {
        var m = Glued().Match(Bare(word));
        return m.Success ? m.Groups["number"].Value : null;
    }

    /// <summary>
    /// A number word or digits with the preposition «بـ» attached («بعشرة ملايين», «ب500.000»): the number without it.
    /// Any other word is returned as it is.
    /// </summary>
    private static string Unprefixed(string bare)
    {
        if (bare.Length < 2 || bare[0] != 'ب')
        {
            return bare;
        }

        var rest = bare[1..];
        var word = rest.StartsWith('و') && rest.Length > 1 ? rest[1..] : rest;
        return char.IsAsciiDigit(rest[0]) || Small.ContainsKey(word) || Scales.ContainsKey(word) ? rest : bare;
    }

    private static bool IsNumberToken(string word)
    {
        var bare = Unprefixed(Bare(word));
        if (bare.Length == 0)
        {
            return false;
        }

        if (DigitGroup().IsMatch(bare))
        {
            return true;
        }

        var w = bare.StartsWith('و') && bare.Length > 1 && (Small.ContainsKey(bare[1..]) || Scales.ContainsKey(bare[1..]) || bare[1..] == "نصف")
            ? bare[1..] : bare;
        return Small.ContainsKey(w) || Scales.ContainsKey(w) || w == "نصف" || bare == "و";
    }

    /// <summary>The value of the number tokens written before a unit, or null when they make no number.</summary>
    private static decimal? Value(List<string> words)
    {
        // Digits in brackets after words («خمسمائة ألف (500.000)»): the digits give the value. A bracket may hold groups
        // split by spaces («(1 050 000)»): the tokens up to the closing bracket are one number.
        var open = words.FindLastIndex(w => w.TrimStart().StartsWith('(') && DigitGroup().IsMatch(Bare(w)));
        if (open >= 0)
        {
            var close = open;
            while (!words[close].TrimEnd().EndsWith(')') && close + 1 < words.Count && DigitGroup().IsMatch(Bare(words[close + 1])))
            {
                close++;
            }

            return ParseDigits(Bare(string.Join(' ', words.Skip(open).Take(close - open + 1))));
        }

        decimal total = 0, current = 0;
        long lastScale = 0;
        var any = false;
        for (var k = 0; k < words.Count; k++)
        {
            var bare = Unprefixed(Bare(words[k]));
            if (bare == "و")
            {
                continue;
            }

            if (DigitGroup().IsMatch(bare))
            {
                // «500 000»: a group of three digits after a number continues it
                var number = ParseDigits(bare);
                if (number is null)
                {
                    return null;
                }

                current = any && current > 0 && bare.Length == 3 && lastScale == 0 ? current * 1000 + number.Value : current + number.Value;
                any = true;
                continue;
            }

            var w = bare.StartsWith('و') && bare.Length > 1 && (Small.ContainsKey(bare[1..]) || Scales.ContainsKey(bare[1..]) || bare[1..] == "نصف")
                ? bare[1..] : bare;
            if (w == "نصف")
            {
                if (lastScale == 0)
                {
                    return null;
                }

                total += lastScale / 2m;
                if (k + 1 < words.Count && Scales.TryGetValue(Bare(words[k + 1]), out var again) && again.Scale == lastScale)
                {
                    k++;  // «مليون ونصف مليون»: the unit repeated after «ونصف»
                }

                any = true;
                continue;
            }

            if (Scales.TryGetValue(w, out var scale))
            {
                var count = current == 0 ? scale.Count : current;
                total += count * scale.Scale;
                current = 0;
                lastScale = scale.Scale;
                any = true;
                continue;
            }

            if (Small.TryGetValue(w, out var small))
            {
                current += small;
                any = true;
                continue;
            }

            return null;
        }

        return any ? total + current : null;
    }

    /// <summary>Digits with «.», «,» or a space between thousands; a comma (or a point) followed by one or two digits at
    /// the end is the decimal part (three digits are thousands).</summary>
    private static decimal? ParseDigits(string group)
    {
        var compact = group.Replace(" ", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        var dec = DecimalTail().Match(compact);
        var whole = dec.Success ? compact[..dec.Index] : compact;
        var fraction = dec.Success ? dec.Groups["f"].Value : "";
        whole = whole.Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
        if (whole.Length == 0 || !whole.All(char.IsAsciiDigit))
        {
            return null;
        }

        return decimal.Parse(whole + (fraction.Length > 0 ? "." + fraction : ""), CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"\S+")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^\(?\d[\d.,]*\)?$")]
    private static partial Regex DigitGroup();

    [GeneratedRegex(@"(?<=\d)[.,](?<f>\d{1,2})$")]
    private static partial Regex DecimalTail();

    [GeneratedRegex(@"^(?<number>\d[\d.,]*)\)?(?<unit>دج|د\.ج|دينار|سنتيم)$")]
    private static partial Regex Glued();
}
