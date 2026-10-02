using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Drafting;

public enum NarrativeIssueKind
{
    /// <summary>Letters from a script other than Arabic or Latin (e.g. CJK leaking from the model). Hard failure.</summary>
    ForeignScript,

    /// <summary>Most letters are not Arabic (e.g. the model reasoned in English). Hard failure; CPCA art. 8.</summary>
    NotArabic,

    /// <summary>A number in the text that appears nowhere in the lawyer's inputs (invented amount, date or article).</summary>
    UnknownNumber,

    /// <summary>A number from the lawyer's notes for this section that the text dropped.</summary>
    MissingNumber,

    /// <summary>A Latin-script word that appears nowhere in the lawyer's inputs (e.g. "caso" leaking from the model).</summary>
    UnexpectedLatinWord,

    /// <summary>A legal qualification (e.g. «عقد المقاولة» in a loan case) that appears nowhere in the lawyer's inputs.</summary>
    UngroundedTerm,

    /// <summary>An article cited in the text that is neither in the lawyer's notes nor among the selected articles.</summary>
    UncitedArticle,

    /// <summary>A quotation «…» attributed to a selected article that is not in that article's official text.</summary>
    MisquotedArticle,
}

public sealed record NarrativeIssue(NarrativeIssueKind Kind, string Detail)
{
    /// <summary>Hard issues make the text unusable: the drafting layer retries, then falls back to the notes.</summary>
    public bool IsHard => Kind is NarrativeIssueKind.ForeignScript or NarrativeIssueKind.NotArabic;

    /// <summary>Law the lawyer did not provide (a qualification, an article, a quotation): worth one more try.</summary>
    public bool IsInventedLaw => Kind is NarrativeIssueKind.UngroundedTerm or NarrativeIssueKind.UncitedArticle or NarrativeIssueKind.MisquotedArticle;
}

/// <summary>
/// Deterministic checks of model-written text against the lawyer's own material. They catch corruption and
/// changed or invented numbers; they cannot judge meaning (e.g. swapped roles), which stays the lawyer's review.
/// </summary>
public static partial class NarrativeChecks
{
    /// <summary>Same threshold as the Form Engine's art. 8 rule in the shipped definition.</summary>
    public const double MinArabicRatio = 0.6;

    private static readonly string[][] MonthNames =
    [
        ["جانفي", "يناير", "janvier"], ["فيفري", "فبراير", "février", "fevrier"], ["مارس", "mars"], ["أفريل", "افريل", "أبريل", "ابريل", "avril"],
        ["ماي", "مايو", "mai"], ["جوان", "يونيو", "juin"], ["جويلية", "يوليو", "juillet"], ["أوت", "اوت", "أغسطس", "août", "aout"],
        ["سبتمبر", "septembre"], ["أكتوبر", "اكتوبر", "octobre"], ["نوفمبر", "novembre"], ["ديسمبر", "décembre", "decembre"],
    ];

    /// <param name="legalTexts">
    /// Official texts of the articles the lawyer selected. Their numbers count as known, and quotations attributed
    /// to them must match their text.
    /// </param>
    public static IReadOnlyList<NarrativeIssue> Check(
        string text,
        string sectionNotes,
        IReadOnlyDictionary<string, string> inputs,
        IReadOnlyList<string>? groundedTerms = null,
        IReadOnlyList<LegalText>? legalTexts = null)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var issues = new List<NarrativeIssue>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return issues;
        }

        var ratio = ArabicScript.Ratio(text);
        if (ratio < MinArabicRatio)
        {
            issues.Add(new NarrativeIssue(NarrativeIssueKind.NotArabic, ratio.ToString("P0", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var foreign = ForeignRuns(text);
        if (foreign.Count > 0)
        {
            issues.Add(new NarrativeIssue(NarrativeIssueKind.ForeignScript, string.Join(" ", foreign)));
        }

        var inText = Numbers(text);
        legalTexts ??= [];
        var known = inputs.Values.Concat(legalTexts.Select(t => t.Number + " " + t.Text)).SelectMany(Numbers).ToHashSet(StringComparer.Ordinal);
        var articleNumbers = ArticleReferences(text).Select(r => r.Number).ToHashSet(StringComparer.Ordinal);
        foreach (var n in inText.Where(n => !known.Contains(n)).Distinct())
        {
            issues.Add(new NarrativeIssue(articleNumbers.Contains(n) ? NarrativeIssueKind.UncitedArticle : NarrativeIssueKind.UnknownNumber, n));
        }

        issues.AddRange(Misquotations(text, legalTexts).Select(n => new NarrativeIssue(NarrativeIssueKind.MisquotedArticle, n)));
        issues.AddRange(Numbers(sectionNotes ?? string.Empty).Where(n => !inText.Contains(n)).Distinct()
            .Select(n => new NarrativeIssue(NarrativeIssueKind.MissingNumber, n)));

        var knownLatin = inputs.Values.SelectMany(LatinWords).ToHashSet(StringComparer.OrdinalIgnoreCase);
        issues.AddRange(LatinWords(text).Where(w => !knownLatin.Contains(w)).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(w => new NarrativeIssue(NarrativeIssueKind.UnexpectedLatinWord, w)));

        if (groundedTerms is { Count: > 0 })
        {
            var plain = StripDiacritics(text);
            var inputText = StripDiacritics(string.Join('\n', inputs.Values));
            issues.AddRange(groundedTerms
                .Select(StripDiacritics)
                .Where(t => plain.Contains(t, StringComparison.Ordinal) && !inputText.Contains(t, StringComparison.Ordinal))
                .Distinct()
                .Select(t => new NarrativeIssue(NarrativeIssueKind.UngroundedTerm, t)));
        }

        return issues;
    }

    /// <summary>
    /// Article references in a text: «المادة 106», «المادتين 106 و107», «المواد 124 و131 و182 مكرر», with the
    /// position of each, as the base number (normalised like <see cref="Numbers"/>).
    /// </summary>
    internal static IEnumerable<(string Number, int Index)> ArticleReferences(string text)
    {
        foreach (Match m in ArticleReference().Matches(ToAsciiDigits(text)))
        {
            foreach (Match n in ArticleNumber().Matches(m.Groups[1].Value))
            {
                yield return (n.Groups[1].Value.TrimStart('0'), m.Index);
            }
        }
    }

    /// <summary>
    /// Quotations «…» of ten letters or more that follow a reference to a selected article in the same paragraph
    /// but are not part of that article's official text (after normalisation). Returns the article numbers.
    /// </summary>
    private static IEnumerable<string> Misquotations(string text, IReadOnlyList<LegalText> legalTexts)
    {
        if (legalTexts.Count == 0)
        {
            return [];
        }

        var found = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var references = ArticleReferences(paragraph).ToList();
            foreach (Match quote in Quotation().Matches(paragraph))
            {
                var quoted = Comparable(quote.Groups[1].Value);
                var reference = references.LastOrDefault(r => r.Index < quote.Index);
                if (quoted.Count(char.IsLetter) < 10 || reference.Number is null)
                {
                    continue;
                }

                var candidates = legalTexts.Where(t => Numbers(t.Number).FirstOrDefault() == reference.Number).ToList();
                if (candidates.Count > 0 && !candidates.Any(t => Comparable(t.Text).Contains(quoted, StringComparison.Ordinal)))
                {
                    found.Add(reference.Number);
                }
            }
        }

        return found.Distinct();
    }

    /// <summary>Normalised letters and digits only, single-spaced, so punctuation and layout do not matter.</summary>
    private static string Comparable(string text)
    {
        var normalized = ArabicScript.Normalize(text);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (sb.Length > 0 && sb[^1] != ' ')
            {
                sb.Append(' ');
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Removes paragraphs that copy the prompt's worked example. A paragraph is a copy when it contains one of the
    /// example's placeholder tokens (such as «(س)») that the lawyer's notes do not contain, or when at least 80% of
    /// its word trigrams occur in the example but not in the notes (a near-verbatim copy). Returns the remaining
    /// text and how many paragraphs were removed.
    /// </summary>
    public static (string Text, int Removed) RemoveCopiedExample(string text, string exampleText, string sectionNotes)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(exampleText))
        {
            return (text, 0);
        }

        sectionNotes ??= string.Empty;
        var placeholders = Placeholder().Matches(exampleText)
            .Select(m => m.Value)
            .Where(p => !sectionNotes.Contains(p, StringComparison.Ordinal))
            .Distinct()
            .ToList();
        var example = Trigrams(exampleText);
        var notes = Trigrams(sectionNotes);
        var kept = new List<string>();
        var removed = 0;
        foreach (var paragraph in text.Split('\n'))
        {
            var grams = Trigrams(paragraph);
            var copied = placeholders.Any(p => paragraph.Contains(p, StringComparison.Ordinal))
                || (grams.Count > 0 && grams.Count(g => example.Contains(g) && !notes.Contains(g)) * 5 >= grams.Count * 4);
            if (copied)
            {
                removed++;
            }
            else
            {
                kept.Add(paragraph);
            }
        }

        return (removed == 0 ? text : string.Join('\n', kept).Trim(), removed);
    }

    private static HashSet<string> Trigrams(string text)
    {
        var words = WordToken().Matches(StripDiacritics(text)).Select(m => m.Value).ToList();
        var grams = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i + 2 < words.Count; i++)
        {
            grams.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
        }

        return grams;
    }

    private static string StripDiacritics(string text) => ArabicScript.StripDiacritics(text);

    private static IEnumerable<string> LatinWords(string text) =>
        LatinWord().Matches(text).Select(m => m.Value.ToLowerInvariant());

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
        var withoutMarkers = ListMarker().Replace(ToAsciiDigits(MonthsToNumbers(text)), string.Empty);
        return NumberToken().Matches(withoutMarkers)
            .Select(m => Separators().Replace(m.Value, string.Empty).TrimStart('0'))
            .Select(n => n.Length == 0 ? "0" : n)
            .ToList();
    }

    /// <summary>«15 جانفي 2025» → «15 1 2025», so it matches «15/01/2025» (Algerian, Mashriq and French month names).</summary>
    private static string MonthsToNumbers(string text)
    {
        var tokens = MonthToken().Split(text);
        for (var i = 0; i < tokens.Length; i++)
        {
            for (var m = 0; m < MonthNames.Length; m++)
            {
                if (MonthNames[m].Contains(tokens[i], StringComparer.OrdinalIgnoreCase))
                {
                    tokens[i] = (m + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                }
            }
        }

        return string.Concat(tokens);
    }

    private static string ToAsciiDigits(string text) => ArabicScript.ToAsciiDigits(text);

    [GeneratedRegex(@"(?:ال)?(?:مادة|مادتين|مادتان|مواد)\s+((?:\d+(?:\s*-\s*\d+)?(?:\s*مكرر(?:\s*\d+)?)?)(?:\s*(?:،|,|و|أو)\s*\d+(?:\s*-\s*\d+)?(?:\s*مكرر(?:\s*\d+)?)?)*)")]
    private static partial Regex ArticleReference();

    [GeneratedRegex(@"(\d+)(?:\s*-\s*\d+)?(?:\s*مكرر(?:\s*\d+)?)?")]
    private static partial Regex ArticleNumber();

    [GeneratedRegex(@"«([^«»]+)»")]
    private static partial Regex Quotation();

    [GeneratedRegex(@"\d{1,3}(?:[,.\u066C\u00A0\u202F ]\d{3})+(?!\d)|\d+")]
    private static partial Regex NumberToken();

    [GeneratedRegex(@"[,.\u066C\u00A0\u202F ]")]
    private static partial Regex Separators();

    /// <summary>Splits text into word and non-word runs (kept), so month names can be replaced in place.</summary>
    [GeneratedRegex(@"([\p{L}]+)")]
    private static partial Regex MonthToken();

    /// <summary>Short parenthesised placeholders used in prompt examples, e.g. «(س)», «(ت)».</summary>
    [GeneratedRegex(@"\([^()\n]{1,12}\)")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"[\p{L}\p{Nd}]+")]
    private static partial Regex WordToken();

    [GeneratedRegex(@"[A-Za-z\u00C0-\u024F\u1E00-\u1EFF]{2,}")]
    private static partial Regex LatinWord();

    [GeneratedRegex(@"^\s*[-•*]?\s*\d{1,2}\s*[.)\-]\s", RegexOptions.Multiline)]
    private static partial Regex ListMarker();
}
