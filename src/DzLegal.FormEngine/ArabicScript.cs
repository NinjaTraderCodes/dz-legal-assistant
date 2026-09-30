namespace DzLegal.FormEngine;

/// <summary>Script detection used by the "proceedings in Arabic" rule.</summary>
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
}
