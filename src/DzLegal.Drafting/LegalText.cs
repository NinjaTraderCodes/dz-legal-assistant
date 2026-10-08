namespace DzLegal.Drafting;

/// <summary>
/// An article the lawyer chose to rely on, with its official text from the law library. These texts are given to
/// the model as the only law it may cite or quote, and the checks compare quotations against them.
/// </summary>
public sealed record LegalText(string Number, string CitationName, string Text)
{
    /// <summary>«المادة 106 من القانون المدني».</summary>
    public string Citation => $"المادة {Number} من {CitationName}";

    /// <summary>
    /// The block inserted in prompts as <c>legal_texts</c>: one «citation: «text»» line per article, or null when
    /// none is selected (so <c>{{ if legal_texts }}</c> leaves the prompt unchanged).
    /// </summary>
    public static string? Block(IReadOnlyList<LegalText>? texts) =>
        texts is null || texts.Count == 0 ? null : string.Join('\n', texts.Select(t => $"- {t.Citation}: «{Shorten(t.Text.ReplaceLineEndings(" "))}»"));

    /// <summary>
    /// Longest article text put in a prompt. Long articles cost a slow local model tens of seconds to read; quotations
    /// are still checked against the whole official text.
    /// </summary>
    public const int MaxPromptLength = 800;

    private static string Shorten(string text)
    {
        if (text.Length <= MaxPromptLength)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', MaxPromptLength);
        return text[..(cut > 0 ? cut : MaxPromptLength)].TrimEnd() + " …";
    }
}
