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
        texts is null || texts.Count == 0 ? null : string.Join('\n', texts.Select(t => $"- {t.Citation}: «{t.Text.ReplaceLineEndings(" ")}»"));
}
