using System.Text.RegularExpressions;

namespace DzLegal.CoreLogic.Notes;

/// <summary>
/// A prompt file of docs/research/core-logic/prompts: the rules above the «--- PROMPT ---» line, the text given to the
/// agent below it, with «{{name}}» placeholders. The same rules as scripts/prompts.py: a placeholder left empty, or a value
/// that itself holds one, is refused.
/// </summary>
public static partial class PromptTemplate
{
    public const string Mark = "--- PROMPT ---";

    public static string Body(string promptsFolder, string name)
    {
        var text = File.ReadAllText(Path.Combine(promptsFolder, name + ".md")).ReplaceLineEndings("\n");
        var at = text.IndexOf(Mark, StringComparison.Ordinal);
        if (at < 0)
        {
            throw new InvalidDataException($"{name}: no «{Mark}» line");
        }

        return text[(at + Mark.Length)..].Trim('\n') + "\n";
    }

    public static string Fill(string body, IReadOnlyDictionary<string, string> values)
    {
        var missing = Placeholder().Matches(body).Select(match => match.Groups[1].Value)
            .Where(name => !values.ContainsKey(name)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            throw new KeyNotFoundException($"placeholders not filled: {string.Join(", ", missing)}");
        }

        var filled = Placeholder().Replace(body, match => values[match.Groups[1].Value]);
        var left = Placeholder().Matches(filled).Select(match => match.Value).ToList();
        return left.Count == 0 ? filled : throw new InvalidDataException($"placeholders left after filling: {string.Join(", ", left)}");
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Placeholder();
}
