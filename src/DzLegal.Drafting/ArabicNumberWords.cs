using System.Text.RegularExpressions;

namespace DzLegal.Drafting;

/// <summary>
/// Article numbers written in words («المادة مائة وأربعة وعشرين» = 124), so the checks see a citation a model spelled
/// out. Covers 1 to 9999 in the usual forms (with or without hamza, ة or not, «و» joining the parts).
/// </summary>
public static class ArabicNumberWords
{
    private static readonly (string Word, int Value)[] Words = BuildWords();

    /// <summary>Regex alternation of every number word, longest first (for use inside a larger pattern).</summary>
    public const string Pattern =
        "تسعمائة|ثمانمائة|سبعمائة|ستمائة|خمسمائة|أربعمائة|اربعمائة|ثلاثمائة|تسعمئة|ثمانمئة|سبعمئة|ستمئة|خمسمئة|أربعمئة|اربعمئة|ثلاثمئة|" +
        "تسعون|تسعين|ثمانون|ثمانين|سبعون|سبعين|ستون|ستين|خمسون|خمسين|أربعون|أربعين|اربعون|اربعين|ثلاثون|ثلاثين|عشرون|عشرين|" +
        "مائتان|مائتين|مئتان|مئتين|مائة|مئة|ألفان|ألفين|آلاف|ألف|الف|" +
        "واحدة|واحد|إحدى|احدى|أحد|احد|اثنتان|اثنتين|اثنان|اثنين|اثنا|اثني|ثلاثة|ثلاث|أربعة|أربع|اربعة|اربع|خمسة|خمس|ستة|ست|سبعة|سبع|ثمانية|ثماني|ثمان|تسعة|تسع|عشرة|عشر";

    public static bool TryParse(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var tokens = Regex.Matches(text, @"(?:^|\s|و)(" + Pattern + @")(?=\s|$|و)").Select(m => m.Groups[1].Value).ToList();
        if (tokens.Count == 0)
        {
            return false;
        }

        var total = 0;
        var previous = 0;
        foreach (var token in tokens)
        {
            var v = Words.First(w => w.Word == token).Value;
            if (v is 100 or 1000 && previous is > 2 and < 10)
            {
                // «ثلاث مائة», «ثلاثة آلاف»: the unit multiplies, it was already added once.
                total += (previous * v) - previous;
            }
            else
            {
                total += v;
            }

            previous = v;
        }

        value = total;
        return total is > 0 and < 10000;
    }

    private static (string, int)[] BuildWords()
    {
        var list = new List<(string, int)>();
        void Add(int v, params string[] words) => list.AddRange(words.Select(w => (w, v)));
        Add(1, "واحد", "واحدة", "أحد", "احد", "إحدى", "احدى");
        Add(2, "اثنان", "اثنين", "اثنتان", "اثنتين", "اثنا", "اثني");
        Add(3, "ثلاثة", "ثلاث");
        Add(4, "أربعة", "أربع", "اربعة", "اربع");
        Add(5, "خمسة", "خمس");
        Add(6, "ستة", "ست");
        Add(7, "سبعة", "سبع");
        Add(8, "ثمانية", "ثماني", "ثمان");
        Add(9, "تسعة", "تسع");
        Add(10, "عشرة", "عشر");
        Add(20, "عشرون", "عشرين");
        Add(30, "ثلاثون", "ثلاثين");
        Add(40, "أربعون", "أربعين", "اربعون", "اربعين");
        Add(50, "خمسون", "خمسين");
        Add(60, "ستون", "ستين");
        Add(70, "سبعون", "سبعين");
        Add(80, "ثمانون", "ثمانين");
        Add(90, "تسعون", "تسعين");
        Add(100, "مائة", "مئة");
        Add(200, "مائتان", "مائتين", "مئتان", "مئتين");
        Add(300, "ثلاثمائة", "ثلاثمئة");
        Add(400, "أربعمائة", "اربعمائة", "أربعمئة", "اربعمئة");
        Add(500, "خمسمائة", "خمسمئة");
        Add(600, "ستمائة", "ستمئة");
        Add(700, "سبعمائة", "سبعمئة");
        Add(800, "ثمانمائة", "ثمانمئة");
        Add(900, "تسعمائة", "تسعمئة");
        Add(1000, "ألف", "الف");
        Add(1000, "آلاف");
        Add(2000, "ألفان", "ألفين");
        return [.. list];
    }
}
