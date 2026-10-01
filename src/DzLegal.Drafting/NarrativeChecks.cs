using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Drafting;

public enum NarrativeIssueKind
{
    /// <summary>Letters from a script other than Arabic or Latin (e.g. CJK leaking from the model). Hard failure.</summary>
    ForeignScript,

    /// <summary>A number in the text that appears nowhere in the lawyer's inputs (invented amount, date or article).</summary>
    UnknownNumber,

    /// <summary>A number from the lawyer's notes for this section that the text dropped.</summary>
    MissingNumber,
}

public sealed record NarrativeIssue(NarrativeIssueKind Kind, string Detail)
{
    /// <summary>Hard issues make the text unusable: the drafting layer retries, then falls back to the notes.</summary>
    public bool IsHard => Kind == NarrativeIssueKind.ForeignScript;
}

/// <summary>
/// Deterministic checks of model-written text against the lawyer's own material. They catch corruption and
/// changed or invented numbers; they cannot judge meaning (e.g. swapped roles), which stays the lawyer's review.
/// </summary>
public static partial class NarrativeChecks
{
    public static IReadOnlyList<NarrativeIssue> Check(string text, string sectionNotes, IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var issues = new List<NarrativeIssue>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return issues;
        }

        var foreign = ForeignRuns(text);
        if (foreign.Count > 0)
        {
            issues.Add(new NarrativeIssue(NarrativeIssueKind.ForeignScript, string.Join(" ", foreign)));
        }

        var inText = Numbers(text);
        var known = inputs.Values.SelectMany(Numbers).ToHashSet(StringComparer.Ordinal);
        issues.AddRange(inText.Where(n => !known.Contains(n)).Distinct()
            .Select(n => new NarrativeIssue(NarrativeIssueKind.UnknownNumber, n)));
        issues.AddRange(Numbers(sectionNotes ?? string.Empty).Where(n => !inText.Contains(n)).Distinct()
            .Select(n => new NarrativeIssue(NarrativeIssueKind.MissingNumber, n)));
        return issues;
    }

    public static bool IsArabicOrLatinLetter(char c) =>
        ArabicScript.IsArabicLetter(c)
        || c is >= 'A' and <= 'Z' or >= 'a' and <= 'z'
        || c is >= 'À' and <= 'ɏ' // Latin-1 supplement and Latin Extended (é, è, ç...)
        || c is >= 'Ḁ' and <= 'ỿ';

    private static List<string> ForeignRuns(string text)
    {
        var runs = new List<string>();
        var current = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsLetter(c) && !IsArabicOrLatinLetter(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                runs.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            runs.Add(current.ToString());
        }

        return runs.Distinct().ToList();
    }

    /// <summary>
    /// Normalized numbers in a text: Arabic-Indic digits become ASCII, thousands separators are removed
    /// ("1,500,000" → "1500000"), leading zeros dropped ("09" → "9"). List markers ("1." at line start) are ignored.
    /// </summary>
    internal static List<string> Numbers(string text)
    {
        var withoutMarkers = ListMarker().Replace(ToAsciiDigits(text), string.Empty);
        return NumberToken().Matches(withoutMarkers)
            .Select(m => Separators().Replace(m.Value, string.Empty).TrimStart('0'))
            .Select(n => n.Length == 0 ? "0" : n)
            .ToList();
    }

    private static string ToAsciiDigits(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] switch
            {
                >= '٠' and <= '٩' => (char)('0' + (chars[i] - '٠')),
                >= '۰' and <= '۹' => (char)('0' + (chars[i] - '۰')),
                _ => chars[i],
            };
        }

        return new string(chars);
    }

    [GeneratedRegex(@"\d{1,3}(?:[,.\u066C\u00A0\u202F ]\d{3})+(?!\d)|\d+")]
    private static partial Regex NumberToken();

    [GeneratedRegex(@"[,.\u066C\u00A0\u202F ]")]
    private static partial Regex Separators();

    [GeneratedRegex(@"^\s*[-•*]?\s*\d{1,2}\s*[.)\-]\s", RegexOptions.Multiline)]
    private static partial Regex ListMarker();
}
