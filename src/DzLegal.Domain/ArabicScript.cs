using System.Text;

namespace DzLegal.Domain;

/// <summary>Script detection used by the "proceedings in Arabic" rule, and normalisation for matching Arabic text.</summary>
public static class ArabicScript
{
    public static bool IsArabicLetter(char c) =>
        c is >= '؀' and <= 'ۿ'
            or >= 'ݐ' and <= 'ݿ'
            or >= 'ࢠ' and <= 'ࣿ'
            or >= 'ﭐ' and <= '﷿'
            or >= 'ﹰ' and <= '﻿';

    /// <summary>
    /// Share of letters that are Arabic. Digits, punctuation and spaces are ignored.
    /// Text with no letters at all returns 1, so blank sections are left to the emptiness checks.
    /// </summary>
    public static double Ratio(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        int letters = 0, arabic = 0;
        foreach (var c in text)
        {
            if (!char.IsLetter(c))
            {
                continue;
            }

            letters++;
            if (IsArabicLetter(c))
            {
                arabic++;
            }
        }

        return letters == 0 ? 1 : (double)arabic / letters;
    }

    /// <summary>Drops Arabic short vowels and tatweel, so «حيثُ» and «حيث» compare equal.</summary>
    public static string StripDiacritics(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new(text.Where(c => c is not (>= '\u064B' and <= '\u065F') and not '\u0640' and not '\u0670').ToArray());
    }

    /// <summary>Arabic-Indic and Persian digits become ASCII digits; everything else is unchanged.</summary>
    public static string ToAsciiDigits(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] switch
            {
                >= '\u0660' and <= '\u0669' => (char)('0' + (chars[i] - '\u0660')),
                >= '\u06F0' and <= '\u06F9' => (char)('0' + (chars[i] - '\u06F0')),
                _ => chars[i],
            };
        }

        return new string(chars);
    }

    /// <summary>
    /// Canonical form for comparing and searching Arabic text: presentation forms (common in PDF extractions)
    /// become ordinary letters, diacritics and tatweel are dropped, hamza-carrying alefs become «ا», «ى» becomes
    /// «ي», «ة» becomes «ه», «ؤ»/«ئ» lose their hamza, digits become ASCII, Latin is lower-cased and runs of
    /// whitespace collapse to one space. Two texts that differ only in these ways compare equal.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var decomposed = StripDiacritics(ToAsciiDigits(text.Normalize(NormalizationForm.FormKC)));
        var sb = new StringBuilder(decomposed.Length);
        var space = false;
        foreach (var c in decomposed)
        {
            if (char.IsWhiteSpace(c))
            {
                space = sb.Length > 0;
                continue;
            }

            if (space)
            {
                sb.Append(' ');
                space = false;
            }

            sb.Append(c switch
            {
                '\u0623' or '\u0625' or '\u0622' or '\u0671' => '\u0627', // أ إ آ ٱ → ا
                '\u0649' => '\u064A', // ى → ي
                '\u0629' => '\u0647', // ة → ه
                '\u0624' => '\u0648', // ؤ → و
                '\u0626' => '\u064A', // ئ → ي
                _ => char.ToLowerInvariant(c),
            });
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalised word tokens with light prefix stripping, for keyword matching: the article «ال» and the
    /// clitics «و» «ف» «ب» «ل» «ك» in front of it are removed («والقرض» → «قرض»). A lone clitic without the
    /// article is left alone and handled by <see cref="MatchesWord"/>. Punctuation separates tokens.
    /// </summary>
    public static IReadOnlyList<string> Tokens(string? text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        foreach (var c in Normalize(text))
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
                continue;
            }

            Flush();
        }

        Flush();
        return tokens;

        void Flush()
        {
            if (current.Length > 0)
            {
                tokens.Add(Stem(current.ToString()));
                current.Clear();
            }
        }
    }

    private static readonly string[] ArticlePrefixes = ["وبال", "وال", "فال", "بال", "كال", "لل", "ال"];

    private static string Stem(string word)
    {
        foreach (var prefix in ArticlePrefixes)
        {
            if (word.Length - prefix.Length >= 2 && word.StartsWith(prefix, StringComparison.Ordinal))
            {
                return prefix == "لل" ? word[2..] : word[prefix.Length..];
            }
        }

        return word;
    }

    /// <summary>
    /// Whether a token from <see cref="Tokens"/> is the given (tokenised) term, allowing one leading conjunction
    /// or preposition «و» «ف» «ب» «ل» on the token when the term has at least three letters («وامتنع» matches
    /// «امتنع», while «بائع» still matches «بائع» and «فسخ» does not match «سخ»).
    /// </summary>
    public static bool MatchesWord(string token, string term)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(term);
        return string.Equals(token, term, StringComparison.Ordinal)
            || (term.Length >= 3
                && token.Length == term.Length + 1
                && token[0] is '\u0648' or '\u0641' or '\u0628' or '\u0644'
                && token.AsSpan(1).SequenceEqual(term));
    }
}
