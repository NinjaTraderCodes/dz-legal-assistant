using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Drafting;

/// <summary>
/// A formal Arabic wording of the lawyer's requests, for the lawyer to accept or ignore. Offered only when it passed
/// every check (<see cref="Problems"/> is empty); never applied automatically.
/// </summary>
/// <param name="Lines">One proposed line per request, in the lawyer's order.</param>
/// <param name="Problems">Why the proposal is not offered (empty when it is).</param>
public sealed record RequestsProposal(IReadOnlyList<string> Lines, IReadOnlyList<string> Problems)
{
    public bool IsOffered => Problems.Count == 0 && Lines.Count > 0;
}

/// <summary>
/// The requests in the document are the lawyer's own lines. This optional helper asks the model for a formal wording of
/// those lines (notes written in Darija or French), then checks deterministically that the proposal changes no relief:
/// the same number of requests, the same numbers in each, every alternative («أو») kept, no amount spelled in words,
/// no Latin text. A proposal that fails is retried once, then withheld with its reasons.
/// </summary>
public sealed class RequestsFormaliser(IModelProvider provider)
{
    /// <summary>Request id for the model call (used by recording providers and logs).</summary>
    public const string RequestId = "requests-wording";

    private const int MaxTokens = 450;

    private static readonly string[] NumberWords =
    [
        "واحد", "اثنان", "اثنين", "ثلاثة", "ثلاث", "أربعة", "أربع", "خمسة", "خمس", "ستة", "ست", "سبعة", "سبع", "ثمانية", "ثمان", "تسعة", "تسع",
        "عشرة", "عشر", "عشرون", "عشرين", "ثلاثون", "ثلاثين", "أربعون", "أربعين", "خمسون", "خمسين", "ستون", "ستين", "سبعون", "سبعين",
        "ثمانون", "ثمانين", "تسعون", "تسعين", "مائة", "مئة", "مائتين", "مئتين", "مائتان", "ألف", "آلاف", "ألفين", "ألفان", "مليون", "ملايين", "مليونين", "مليار",
    ];

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly TemplateRenderer _renderer = new();

    public async Task<RequestsProposal> ProposeAsync(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inputs);
        var original = Lines(inputs.GetValueOrDefault("notes.requests", string.Empty));
        if (original.Count == 0)
        {
            return new RequestsProposal([], ["no requests"]);
        }

        var request = BuildRequest(definition, inputs, original);
        RequestsProposal? last = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var answer = new StringBuilder();
            await foreach (var piece in _provider.StreamAsync(request, cancellationToken))
            {
                answer.Append(piece);
            }

            var proposed = Lines(DraftingService.ApplyCorrections(definition, StripReasoning(answer.ToString())));
            last = new RequestsProposal(proposed, Check(original, proposed));
            if (last.IsOffered)
            {
                return last;
            }
        }

        return last!;
    }

    public ModelRequest BuildRequest(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(lines);
        var prompt = definition.FormaliseRequestsPrompt ?? throw new InvalidOperationException($"Document type '{definition.Id}' has no formaliseRequestsPrompt.");
        var notes = string.Join('\n', lines);
        var user = _renderer.RenderFile(definition, prompt, inputs, new Dictionary<string, object?>
        {
            ["requests_notes"] = notes,
            ["line_count"] = lines.Count,
        });
        return new ModelRequest(RequestId, string.Empty, user, notes) { MaxTokens = MaxTokens };
    }

    /// <summary>
    /// Why a proposal would change the relief asked of the court; empty when it is a faithful rewording. Line by line:
    /// the same numbers (digits compared, separators ignored), «أو» kept where the lawyer gave an alternative, no amount
    /// in words the lawyer didn't write, no Latin letters.
    /// </summary>
    public static IReadOnlyList<string> Check(IReadOnlyList<string> original, IReadOnlyList<string> proposed)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(proposed);
        var problems = new List<string>();
        if (proposed.Count != original.Count)
        {
            problems.Add($"lines: {original.Count} requested, {proposed.Count} proposed");
            return problems;
        }

        for (var i = 0; i < original.Count; i++)
        {
            var before = Numbers(original[i]);
            var after = Numbers(proposed[i]);
            if (!before.Order(StringComparer.Ordinal).SequenceEqual(after.Order(StringComparer.Ordinal)))
            {
                problems.Add($"line {i + 1}: numbers {string.Join(" ", before)} became {string.Join(" ", after)}");
            }

            if (IsAlternative(original[i]) && !HasWord(proposed[i], "أو"))
            {
                problems.Add($"line {i + 1}: the alternative («أو») was lost");
            }

            var words = NumberWords.Where(w => HasWord(proposed[i], w) && !HasWord(original[i], w)).ToList();
            if (words.Count > 0)
            {
                problems.Add($"line {i + 1}: number in words «{string.Join(" ", words)}»");
            }

            if (proposed[i].Any(c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')))
            {
                problems.Add($"line {i + 1}: Latin text");
            }
        }

        return problems;
    }

    /// <summary>The request lines as the document shows them: list markers and the frame removed.</summary>
    public static IReadOnlyList<string> Lines(string? text) =>
        TemplateRenderer.RequestLines(text).Cast<string>().ToList();

    private static List<string> Numbers(string line) =>
        Regex.Matches(ArabicScript.ToAsciiDigits(line), @"\d[\d.,\s]*\d|\d")
            .Select(m => Regex.Replace(m.Value, @"[.,\s]", string.Empty).TrimStart('0'))
            .Where(n => n.Length > 0)
            .ToList();

    private static bool IsAlternative(string line) =>
        HasWord(line, "أو") || HasWord(line, "ولا") || HasWord(line, "وإلا") || HasWord(line, "والا") || HasWord(line, "ou");

    private static bool HasWord(string text, string word) =>
        Regex.IsMatch(ArabicScript.Normalize(text), $@"(?<![\p{{L}}]){Regex.Escape(ArabicScript.Normalize(word))}(?![\p{{L}}])");

    private static string StripReasoning(string text) => Regex.Replace(text, @"<think>.*?</think>", string.Empty, RegexOptions.Singleline);
}
