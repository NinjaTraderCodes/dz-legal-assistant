using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Drafting;

/// <summary>
/// One ground: the sentence the code writes from the official texts («حيث إن المادة 106 من القانون المدني تنص على أن
/// «…»»), and the lawyer's note it comes from, which the model turns into the argument.
/// </summary>
/// <param name="Lead">The code-written part, ending without punctuation; empty for a note that cites no article.</param>
/// <param name="Note">The lawyer's line for this ground.</param>
/// <param name="Articles">The articles cited, e.g. "106 (القانون المدني)".</param>
public sealed record SkeletonParagraph(string Lead, string Note, IReadOnlyList<string> Articles);

/// <param name="Text">The assembled grounds; a missing or rejected argument is left as «(...)» for the lawyer.</param>
/// <param name="Problems">Why an argument was left out, one line each.</param>
/// <param name="Retried">An argument was missing or rejected and the model was asked once more.</param>
public sealed record SkeletonDraft(string Text, IReadOnlyList<SkeletonParagraph> Paragraphs, IReadOnlyList<string> Problems)
{
    public bool Retried { get; init; }
}

/// <summary>
/// Skeleton-first grounds: the code writes every sentence that states what an
/// article provides, letter for letter from the official text, and the model writes only the argument that applies it
/// to the facts. An argument that cites or quotes an article is rejected, so the model never states the law.
/// </summary>
public sealed partial class GroundsSkeleton(IModelProvider provider)
{
    /// <summary>Request id for the model call (used by recording providers and logs).</summary>
    public const string RequestId = "grounds-argument";

    private const string Placeholder = "(...)";

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly TemplateRenderer _renderer = new();

    /// <summary>One paragraph per line of the lawyer's grounds notes, in order, and one per loaded text cited in no Arabic reference.</summary>
    public static IReadOnlyList<SkeletonParagraph> Build(string groundsNotes, IReadOnlyList<LegalText> legalTexts)
    {
        ArgumentNullException.ThrowIfNull(legalTexts);
        var paragraphs = new List<SkeletonParagraph>();
        var used = new HashSet<LegalText>();
        foreach (var line in (groundsNotes ?? string.Empty).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            var references = NarrativeChecks.FullArticleReferences(NarrativeChecks.CanonicalForReferences(line))
                .Select(r => (r.Number, CodeName: r.Code is null ? null : NarrativeChecks.CodeName(r.Code)))
                .Concat(NarrativeChecks.FrenchArticleReferences(line).Select(r => (r.Number, CodeName: (string?)r.CodeName)))
                .Distinct()
                .ToList();
            var quoted = new List<string>();
            var unloaded = new List<string>();
            var labels = new List<string>();
            foreach (var (number, codeName) in references)
            {
                var text = legalTexts.FirstOrDefault(t => ArticleNumbers.Canonical(t.Number) == number
                    && (codeName is null || ArabicScript.Normalize(t.CitationName).Contains(ArabicScript.Normalize(codeName), StringComparison.Ordinal)));
                var name = text?.CitationName ?? codeName ?? "القانون المدني";
                labels.Add($"{number} ({name})");
                if (text is null)
                {
                    unloaded.Add($"المادة {number} من {name}");
                }
                else if (used.Add(text))
                {
                    quoted.Add(Provides(text, line));
                }
            }

            paragraphs.Add(new SkeletonParagraph(Lead(quoted, unloaded), line, labels));
        }

        // Texts the notes cite in a way the Arabic references miss («Articles 106 et 107 du code civil»).
        foreach (var text in legalTexts.Where(t => !used.Contains(t)))
        {
            paragraphs.Add(new SkeletonParagraph(Lead([Provides(text, string.Empty)], []), string.Empty, [$"{text.Number} ({text.CitationName})"]));
        }

        return paragraphs;
    }

    public ModelRequest BuildRequest(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<SkeletonParagraph> paragraphs, string promptPath)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(paragraphs);
        var numbered = new StringBuilder();
        for (var i = 0; i < paragraphs.Count; i++)
        {
            var p = paragraphs[i];
            numbered.Append(CultureInfo.InvariantCulture, $"[{i + 1}] ");
            numbered.AppendLine(p.Lead.Length > 0 ? Opening(i) + p.Lead + "، …" : "(سند بلا مادة)");
            if (p.Note.Length > 0)
            {
                numbered.AppendLine("    ملاحظة المحامي: " + p.Note);
            }
        }

        var user = _renderer.RenderFile(definition, promptPath, inputs, new Dictionary<string, object?>
        {
            ["paragraphs"] = numbered.ToString().TrimEnd(),
            ["paragraph_count"] = paragraphs.Count,
        });
        var system = definition.SystemPrompt is null
            ? string.Empty
            : File.ReadAllText(Path.Combine(definition.SourceDirectory, definition.SystemPrompt)).Trim();
        var notes = string.Join('\n', paragraphs.Select(p => p.Note));
        return new ModelRequest(RequestId, system, TemplateRenderer.Normalize(user), notes) { MaxTokens = 250 + (250 * paragraphs.Count) };
    }

    /// <param name="section">The grounds section: its <see cref="SectionDefinition.ArgumentPrompt"/> and notes field.</param>
    public async Task<SkeletonDraft> DraftAsync(DocumentTypeDefinition definition, SectionDefinition section, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText> legalTexts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(inputs);
        var promptPath = section.ArgumentPrompt ?? throw new ArgumentException($"Section '{section.Id}' has no argumentPrompt.", nameof(section));
        var paragraphs = Build(inputs.GetValueOrDefault(section.SourceField ?? string.Empty, string.Empty), legalTexts);
        if (paragraphs.Count == 0)
        {
            return new SkeletonDraft(string.Empty, paragraphs, ["no grounds in the notes"]);
        }

        // A missing or rejected argument is asked once more; only the failed numbers take the second answer.
        var request = BuildRequest(definition, inputs, paragraphs, promptPath);
        var arguments = new Dictionary<int, string>(ParseArguments(await AskAsync(definition, request, cancellationToken)));
        var failed = Enumerable.Range(1, paragraphs.Count).Where(n => Problem(definition, arguments.GetValueOrDefault(n), paragraphs, inputs, legalTexts) is not null).ToList();
        if (failed.Count > 0)
        {
            var second = ParseArguments(await AskAsync(definition, request, cancellationToken));
            foreach (var n in failed.Where(n => second.TryGetValue(n, out var retry) && Problem(definition, retry, paragraphs, inputs, legalTexts) is null))
            {
                arguments[n] = second[n];
            }
        }

        return Assemble(definition, paragraphs, arguments, inputs, legalTexts) with { Retried = failed.Count > 0 };
    }

    private async Task<string> AskAsync(DocumentTypeDefinition definition, ModelRequest request, CancellationToken cancellationToken)
    {
        var answer = new StringBuilder();
        await foreach (var piece in _provider.StreamAsync(request, cancellationToken))
        {
            answer.Append(piece);
        }

        return DraftingService.ApplyCorrections(definition, answer.ToString());
    }

    /// <summary>
    /// Joins each code-written lead with its argument. The leads are never changed. An argument is dropped, and «(...)»
    /// left for the lawyer, when it is missing, cites or quotes an article, or fails the narrative checks (foreign
    /// script, a number found nowhere in the notes).
    /// </summary>
    public static SkeletonDraft Assemble(DocumentTypeDefinition? definition, IReadOnlyList<SkeletonParagraph> paragraphs, string answer, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText> legalTexts) =>
        Assemble(definition, paragraphs, ParseArguments(answer), inputs, legalTexts);

    public static SkeletonDraft Assemble(DocumentTypeDefinition? definition, IReadOnlyList<SkeletonParagraph> paragraphs, IReadOnlyDictionary<int, string> arguments, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText> legalTexts)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(inputs);
        var problems = new List<string>();
        var lines = new List<string>();
        for (var i = 0; i < paragraphs.Count; i++)
        {
            var argument = arguments.GetValueOrDefault(i + 1);
            if (Problem(definition, argument, paragraphs, inputs, legalTexts) is { } problem)
            {
                problems.Add($"[{i + 1}] argument {problem}: left as {Placeholder}" + (argument is null ? string.Empty : $" («{argument}»)"));
                argument = "وحيث إن " + Placeholder;
            }

            var lead = paragraphs[i].Lead;
            lines.Add(lead.Length == 0
                ? Reopen(argument!, i)
                : Opening(i) + lead + "، " + Continue(argument!) + (argument!.TrimEnd().EndsWith('.') ? string.Empty : "."));
        }

        return new SkeletonDraft(string.Join('\n', lines), paragraphs, problems);
    }

    /// <summary>Why an argument cannot be used, or null: missing, cites or quotes an article, foreign script, a number found nowhere in the notes.</summary>
    private static string? Problem(DocumentTypeDefinition? definition, string? argument, IReadOnlyList<SkeletonParagraph> paragraphs, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText> legalTexts)
    {
        if (argument is null)
        {
            return "missing";
        }

        var sources = string.Join('\n', paragraphs.Select(p => p.Note)
            .Append(inputs.GetValueOrDefault("notes.facts", string.Empty))
            .Append(inputs.GetValueOrDefault("notes.requests", string.Empty))
            .Append(inputs.GetValueOrDefault("documents.list", string.Empty)));
        return NarrativeChecks.ArticleReferences(NarrativeChecks.CanonicalForReferences(argument)).Any() ? "cites an article"
            : QuoteMark().IsMatch(argument) ? "quotes a text"
            : NarrativeChecks.Check(argument, sources, inputs, definition?.GroundedTerms, legalTexts)
                .FirstOrDefault(x => x.IsHard || x.Kind == NarrativeIssueKind.UnknownNumber) is { } issue ? $"{issue.Kind} {issue.Detail}"
            : null;
    }

    /// <summary>The model's answer as argument per paragraph number («[1] …», «1. …», «(1) …»); later duplicates are ignored.</summary>
    public static IReadOnlyDictionary<int, string> ParseArguments(string answer)
    {
        var result = new Dictionary<int, string>();
        foreach (Match m in ArgumentLine().Matches(ArabicScript.ToAsciiDigits(answer ?? string.Empty)))
        {
            var text = m.Groups["text"].Value.Trim();
            if (int.TryParse(m.Groups["n"].Value, CultureInfo.InvariantCulture, out var n) && text.Length > 0)
            {
                result.TryAdd(n, text);
            }
        }

        return result;
    }

    private static string Provides(LegalText text, string note) => $"{text.Citation} تنص على أن «{Excerpt(text.Text, note)}»";

    /// <summary>
    /// Articles up to this length are quoted whole: art. 86's second sentence (silence as fraud) is often the one that
    /// fits the facts, though the lawyer's note paraphrases only the first.
    /// </summary>
    public const int FullQuoteLength = 400;

    /// <summary>
    /// The part of a long official text the lawyer relies on, copied letter for letter: the sentences that share words
    /// with the lawyer's note (at least two words and 30 % of the sentence's), from the first to the last of them (art. 107's first sentence, not its paragraph on
    /// unforeseen events), or the first sentence when none does. Without its final full stop, as it sits inside «».
    /// </summary>
    public static string Excerpt(string officialText, string note)
    {
        ArgumentNullException.ThrowIfNull(officialText);
        var text = officialText.ReplaceLineEndings(" ").Trim();
        if (text.Length <= FullQuoteLength)
        {
            return text.TrimEnd('.', ' ');
        }

        var sentences = Sentence().Matches(text).Where(m => m.Value.Trim().Length > 0).ToList();
        if (sentences.Count == 0)
        {
            return text.TrimEnd('.', ' ');
        }

        var noteWords = ArabicScript.Tokens(note).Where(t => t.Length >= 3).Select(ArabicScript.SearchKey).ToHashSet(StringComparer.Ordinal);
        var matching = sentences.Where(m =>
            {
                var words = ArabicScript.Tokens(m.Value).Where(t => t.Length >= 3).Select(ArabicScript.SearchKey).Distinct().ToList();
                var shared = words.Count(noteWords.Contains);
                return shared >= 2 && shared >= 0.3 * words.Count;
            })
            .ToList();
        var first = matching.Count > 0 ? matching[0] : sentences[0];
        var last = matching.Count > 0 ? matching[^1] : sentences[0];
        return text[first.Index..(last.Index + last.Length)].Trim().TrimEnd('.', ' ');
    }

    private static string Lead(List<string> quoted, List<string> unloaded)
    {
        if (quoted.Count == 0 && unloaded.Count == 0)
        {
            return string.Empty;
        }

        var lead = quoted.Count > 0 ? string.Join("، و", quoted) : "الدعوى تستند إلى " + string.Join(" و", unloaded);
        return quoted.Count > 0 && unloaded.Count > 0 ? lead + "، وذلك طبقا لل" + string.Join(" و", unloaded)[2..] : lead;
    }

    private static string Opening(int index) => index == 0 ? "حيث إن " : "وحيث إن ";

    /// <summary>The argument continues the sentence: «وحيث إن …» or «فإن …» as the model wrote it, without a leading comma.</summary>
    private static string Continue(string argument) => argument.TrimStart('،', ',', ' ', '-');

    /// <summary>A note with no article: the argument is the whole paragraph, opened like the others.</summary>
    internal static string Reopen(string argument, int index)
    {
        var text = Continue(argument);
        var body = OpeningWords().Replace(text, string.Empty, 1);
        return Opening(index) + body + (body.TrimEnd().EndsWith('.') ? string.Empty : ".");
    }

    [GeneratedRegex(@"^\s*[\[(]?(?<n>\d{1,2})[\])\.:\-]\s*(?<text>.+)$", RegexOptions.Multiline)]
    private static partial Regex ArgumentLine();

    [GeneratedRegex(@"[^.]+(?:\.|$)")]
    private static partial Regex Sentence();

    [GeneratedRegex(@"[«»""“”]")]
    private static partial Regex QuoteMark();

    [GeneratedRegex(@"^(?:و?حيث\s+(?:إن|ان|أن)\s+)")]
    private static partial Regex OpeningWords();
}
