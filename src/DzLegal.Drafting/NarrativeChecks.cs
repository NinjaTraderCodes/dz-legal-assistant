using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;
using DzLegal.Law;

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

    /// <summary>
    /// A quotation («…», "…" or “…”), or a «تنص على أن …» statement, attributed to an article but not found in that
    /// article's official text (nor in the lawyer's own notes).
    /// </summary>
    MisquotedArticle,

    /// <summary>
    /// Content attributed to an article whose official text the model was not given (it is not in the library, or
    /// the lawyer did not select it) and that the lawyer's notes do not state either: the model wrote the law from
    /// memory.
    /// </summary>
    UnsupportedArticleContent,
}

public sealed record NarrativeIssue(NarrativeIssueKind Kind, string Detail)
{
    /// <summary>Hard issues make the text unusable: the drafting layer retries, then falls back to the notes.</summary>
    public bool IsHard => Kind is NarrativeIssueKind.ForeignScript or NarrativeIssueKind.NotArabic;

    /// <summary>Law the lawyer did not provide (a qualification, an article, a quotation): worth one more try.</summary>
    public bool IsInventedLaw => Kind is NarrativeIssueKind.UngroundedTerm or NarrativeIssueKind.UncitedArticle
        or NarrativeIssueKind.MisquotedArticle or NarrativeIssueKind.UnsupportedArticleContent;

    /// <summary>Wrong law attached to an article: a paragraph that still has it after the retry is removed.</summary>
    public bool IsWrongArticleContent => Kind is NarrativeIssueKind.MisquotedArticle or NarrativeIssueKind.UnsupportedArticleContent;
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

        // Hidden characters, tatweel, «الماده», «رقم» and article numbers in words must not hide a citation.
        text = CanonicalForReferences(text);
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

        // The right number with another code than the selected one («المادة 106 من القانون التجاري» when only the
        // Civil Code's 106 was given) is an article nobody selected.
        var ownNotes = Comparable(string.Join('\n', inputs.Values));
        foreach (var reference in FullArticleReferences(text).Where(r => r.Code is not null).DistinctBy(r => (r.Number, r.Code)))
        {
            var sameNumber = legalTexts.Where(t => ArticleNumbers.Canonical(t.Number) == reference.Number).ToList();
            var codeName = CodeName(reference.Code!);
            if (sameNumber.Count > 0 && TextsFor(reference, legalTexts).Count == 0 && !ownNotes.Contains(Comparable(reference.Number + " " + codeName), StringComparison.Ordinal))
            {
                issues.Add(new NarrativeIssue(NarrativeIssueKind.UncitedArticle, $"{reference.Number} ({codeName})"));
            }
        }

        var ownWords = string.Join('\n', inputs.Values.Append(sectionNotes ?? string.Empty));
        var (misquoted, unsupported) = Misquotations(text, legalTexts, ownWords);
        issues.AddRange(misquoted.Select(n => new NarrativeIssue(NarrativeIssueKind.MisquotedArticle, n)));
        issues.AddRange(unsupported.Select(n => new NarrativeIssue(NarrativeIssueKind.UnsupportedArticleContent, n)));
        issues.AddRange(StatedContents(text, legalTexts, ownWords).Where(i => !issues.Contains(i)));
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
    public static IEnumerable<(string Number, int Index)> ArticleReferences(string text)
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
    /// The full number of each article reference («182 مكرر», not just 182) with its position, and the code named
    /// right after it when there is one («من القانون التجاري»), so CC 176 and ComC 176 are told apart.
    /// </summary>
    public static IEnumerable<(string Number, int Index, string? Code)> FullArticleReferences(string text)
    {
        var ascii = ToAsciiDigits(text);
        foreach (Match m in ArticleReference().Matches(ascii))
        {
            var after = ascii[(m.Index + m.Length)..Math.Min(ascii.Length, m.Index + m.Length + 40)];
            var code = CodeAfterReference().Match(after) is { Success: true } c ? c.Groups[1].Value : null;
            foreach (Match n in ArticleNumber().Matches(m.Groups[1].Value))
            {
                yield return (ArticleNumbers.Canonical(n.Value), m.Index, code);
            }
        }
    }

    /// <summary>The selected texts a reference can mean: same full number, and same code when the text names one.</summary>
    private static List<LegalText> TextsFor((string Number, int Index, string? Code) reference, IReadOnlyList<LegalText> legalTexts) =>
        legalTexts.Where(t => ArticleNumbers.Canonical(t.Number) == reference.Number
            && (reference.Code is null || ArabicScript.Normalize(t.CitationName).Contains(ArabicScript.Normalize(CodeName(reference.Code)), StringComparison.Ordinal)))
            .ToList();

    /// <summary>
    /// Quotations («…», "…" or “…») of ten letters or more that follow an article reference in the same paragraph but
    /// are in neither that article's official text nor the lawyer's own notes. Returns the article numbers: misquoted
    /// (the text was given and says otherwise) and unsupported (no text was given, so the quotation came from the
    /// model's memory: an article not in the library, another code, an invented provision).
    /// </summary>
    private static (List<string> Misquoted, List<string> Unsupported) Misquotations(string text, IReadOnlyList<LegalText> legalTexts, string ownWords)
    {
        var own = Comparable(ownWords);
        var found = new List<string>();
        var unsupported = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var references = FullArticleReferences(paragraph).ToList();
            foreach (Match quote in Quotation().Matches(paragraph))
            {
                var quoted = Comparable(QuotedText(quote));
                var reference = references.LastOrDefault(r => r.Index < quote.Index);
                if (quoted.Count(char.IsLetter) < 10 || reference.Number is null || own.Contains(quoted, StringComparison.Ordinal))
                {
                    continue;
                }

                var candidates = TextsFor(reference, legalTexts);
                if (candidates.Count == 0)
                {
                    unsupported.Add(reference.Number);
                }
                else if (!candidates.Any(t => Comparable(t.Text).Contains(quoted, StringComparison.Ordinal)))
                {
                    found.Add(reference.Number);
                }
            }
        }

        return ([.. found.Distinct()], [.. unsupported.Distinct()]);
    }

    /// <summary>The code a reference names, written out («ق.م» → «القانون المدني»), so it can be compared with a citation.</summary>
    private static string CodeName(string code)
    {
        var compact = Regex.Replace(ArabicScript.Normalize(code), @"[\s.]", string.Empty);
        return compact switch
        {
            "قم" or "قمج" => "القانون المدني",
            "قت" or "قتج" => "القانون التجاري",
            "قامواد" or "قاما" or "قاماا" or "قامام" => "قانون الإجراءات المدنية والإدارية",
            "قا" or "قاج" => "قانون الأسرة",
            _ => code,
        };
    }

    /// <summary>
    /// The text as citations should be read: no invisible format characters or tatweel, «الماده» → «المادة»,
    /// «المادة رقم 12» → «المادة 12», and an article number written in words («المادة مائة وأربعة وعشرين») in digits.
    /// Applied before every check, so a model cannot hide a citation from them by spelling it differently.
    /// </summary>
    public static string CanonicalForReferences(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var clean = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c != '\u0640' && char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.Format)
            {
                clean.Append(c);
            }
        }

        var result = ArticleWordFix().Replace(clean.ToString(), m => m.Value[..^1] + "ة");
        result = ArticleGluedToNumber().Replace(result, "$1 ");
        result = ArticleNumberSign().Replace(result, "$1 ");
        return ArticleInWords().Replace(result, m => ArabicNumberWords.TryParse(m.Groups["words"].Value, out var n)
            ? m.Groups["head"].Value + n.ToString(System.Globalization.CultureInfo.InvariantCulture) + m.Groups["tail"].Value
            : m.Value);
    }

    /// <summary>Share of a stated content's words that must appear in the article's text (or the lawyer's notes).</summary>
    public const double MinStatedCoverage = 0.6;

    /// <summary>
    /// «المادة 323 تنص على أن …» and the like: what the text says an article provides. When the article's official
    /// text was given, most of the stated words must come from it (or from the lawyer's notes), otherwise the
    /// statement misquotes it; when no text was given, the statement must come from the lawyer's notes, otherwise the
    /// model wrote the law from memory.
    /// </summary>
    private static IEnumerable<NarrativeIssue> StatedContents(string text, IReadOnlyList<LegalText> legalTexts, string ownWords)
    {
        var own = ContentWords(ownWords);
        var issues = new List<NarrativeIssue>();
        foreach (var paragraph in text.Split('\n'))
        {
            var references = FullArticleReferences(paragraph).ToList();
            foreach (Match statement in Statement().Matches(paragraph))
            {
                var reference = references.LastOrDefault(r => r.Index < statement.Index);
                if (reference.Number is null || references.Any(r => r.Index > reference.Index && r.Index < statement.Index && r.Number != reference.Number))
                {
                    continue;
                }

                // A quotation is judged by Misquotations, letter for letter.
                if (Quotation().IsMatch(statement.Groups["content"].Value))
                {
                    continue;
                }

                var stated = ContentWords(statement.Groups["content"].Value);
                if (stated.Count < 4)
                {
                    continue;
                }

                var texts = TextsFor(reference, legalTexts);
                var source = texts.SelectMany(t => ContentWords(t.Text)).Concat(own).ToList();
                var covered = stated.Count(w => source.Any(o => ArabicScript.MatchesWord(w, o) || ArabicScript.MatchesWord(o, w)));
                if (covered < MinStatedCoverage * stated.Count)
                {
                    issues.Add(new NarrativeIssue(texts.Count > 0 ? NarrativeIssueKind.MisquotedArticle : NarrativeIssueKind.UnsupportedArticleContent, reference.Number));
                }
            }
        }

        return issues.DistinctBy(i => (i.Kind, i.Detail));
    }

    private static readonly HashSet<string> StopWords = new(
        ["على", "التي", "الذي", "الذين", "هذا", "هذه", "ذلك", "إلى", "الى", "إذا", "اذا", "إلا", "الا", "كان", "كانت", "يكون", "تكون", "أو", "او", "ما", "لا", "لم", "من", "في", "عن", "أن", "ان", "بأن", "بان", "وأن", "كل", "قد", "مع", "هو", "هي"],
        StringComparer.Ordinal);

    private static List<string> ContentWords(string text) =>
        ArabicScript.Tokens(text).Where(t => t.Length >= 3 && !StopWords.Contains(t)).ToList();

    private static string QuotedText(Match quote) =>
        quote.Groups[1].Success ? quote.Groups[1].Value : quote.Groups[2].Success ? quote.Groups[2].Value : quote.Groups[3].Value;

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

    /// <summary>«الماده»، «الماده» at a word end: the article noun written with ه instead of ة.</summary>
    [GeneratedRegex(@"(?<![\p{L}])(?:ال|لل|ل)?ماده(?![\p{L}])")]
    private static partial Regex ArticleWordFix();

    [GeneratedRegex(@"((?:ال|لل|ل)?(?:مادة|مادتين|مادتان))(?=[0-9\u0660-\u0669])")]
    private static partial Regex ArticleGluedToNumber();

    [GeneratedRegex(@"((?:ال|لل|ل)?(?:مادة|مادتين|مادتان))\s+رقم\s+")]
    private static partial Regex ArticleNumberSign();

    [GeneratedRegex(@"(?<head>(?:ال|لل|ل)?مادة\s+)(?<words>(?:(?:و\s*)?(?:" + ArabicNumberWords.Pattern + @")\s*){1,6})(?<tail>\s|$|[.،,؛])")]
    private static partial Regex ArticleInWords();

    [GeneratedRegex(@"^\s*(?:من\s+)?(القانون المدني|القانون التجاري|قانون الإجراءات المدنية والإدارية|قانون الأسرة|الدستور|قانون العمل|ق\.?\s*إ\.?\s*م\.?\s*إ|ق\.?\s*م\.?\s*ج|ق\.?\s*ت\.?\s*ج|ق\.?\s*م|ق\.?\s*ت|ق\.?\s*أ)")]
    private static partial Regex CodeAfterReference();

    /// <summary>«تنص على أن …», «التي تقضي بأن …», «مفادها أن …», up to the end of the sentence or the next «وحيث».</summary>
    [GeneratedRegex(@"(?:تنصان|تنص|نصتا|نصت|تقضيان|تقضي|قضت|مفادها|مضمونها|جاء فيها|(?<=(?:وفقا|طبقا|عملا|استنادا)\s+(?:لنص\s+)?ل?لمادة[^.؛\n]{0,80}?)فإن)\s+(?:على\s+)?(?:أن|ان|أنه|بأن|بان|ب)?\s*(?<content>[^.؛\n]+?)(?=\s*(?:[.؛]|\n|$|،?\s*(?:و)?حيث|،?\s*فإن|،?\s*مما))")]
    private static partial Regex Statement();

    [GeneratedRegex(@"«([^«»]+)»|""([^""\n]+)""|“([^”\n]+)”")]
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
