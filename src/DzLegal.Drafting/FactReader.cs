using System.Text;

namespace DzLegal.Drafting;

/// <summary>
/// Reads the facts of a case with a model: the relationship and the plaintiff's position in it, the events with their
/// dates, amounts, actor and polarity, and the requests, each pointing to the numbered sentences it came from. The
/// model only reads and writes short coded lines; the program checks every value against the text and decides
/// nothing legal from it here. The instructions come first and are the same for every case, so a local server can
/// keep them read.
/// </summary>
public sealed class FactReader(IModelProvider provider, string prompt)
{
    public const string RequestId = "facts.reading";
    public const string PromptFile = "prompts/read-case-facts.ar.txt";

    /// <summary>Room for about 20 short lines.</summary>
    public const int MaxTokens = 500;

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly string _prompt = prompt is { Length: > 0 } && prompt.Contains("{{ case }}", StringComparison.Ordinal)
        ? prompt
        : throw new ArgumentException("The fact-reading prompt needs a {{ case }} placeholder.", nameof(prompt));

    /// <summary>The prompt shipped in the law data folder.</summary>
    public static string LoadPrompt(string lawRoot) => File.ReadAllText(Path.Combine(lawRoot, PromptFile));

    public ModelRequest BuildRequest(CaseText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var user = _prompt
            .Replace("{{ section }}", text.CourtSection.Length > 0 ? text.CourtSection : "لم يحدد", StringComparison.Ordinal)
            .Replace("{{ case }}", text.Render(), StringComparison.Ordinal);
        return new ModelRequest(RequestId, string.Empty, user, string.Join('\n', text.Sentences.Select(s => s.Text))) { MaxTokens = MaxTokens };
    }

    /// <returns>The facts, checked, and the model's raw answer.</returns>
    public async Task<(CaseFacts Facts, string Answer)> ReadAsync(CaseText text, CancellationToken cancellationToken = default)
    {
        var answer = new StringBuilder();
        await foreach (var piece in _provider.StreamAsync(BuildRequest(text), cancellationToken))
        {
            answer.Append(piece);
        }

        return (Parse(answer.ToString(), text), answer.ToString());
    }

    /// <summary>
    /// Reads the coded lines: unknown codes, missing fields and citations of sentences that do not exist are rejected
    /// or dropped; every date and amount is checked against the sentences (<see cref="ValueCheck"/>).
    /// </summary>
    public static CaseFacts Parse(string answer, CaseText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string relation = string.Empty, position = string.Empty;
        var events = new List<FactEvent>();
        var requests = new List<FactRequest>();
        var rejected = new List<string>();
        foreach (var raw in (answer ?? string.Empty).Split('\n'))
        {
            var line = raw.Trim().Trim('*', '`', '-', '•').Trim();
            var parts = line.Split('|').Select(p => p.Trim()).ToArray();
            if (parts.Length < 2)
            {
                continue;
            }

            var head = Domain.ArabicScript.Normalize(parts[0]);
            if (head is "علاقه" or "relation" && parts.Length >= 3 && relation.Length == 0)
            {
                var r = Code(parts[1]);
                var p = Code(parts[2]);
                if (FactVocabulary.Relations.Contains(r) && FactVocabulary.Positions.Contains(p))
                {
                    relation = r;
                    position = p;
                }
                else
                {
                    rejected.Add(line);
                }
            }
            else if (head is "حدث" or "event" && parts.Length >= 7)
            {
                var kind = Code(parts[1]);
                var actor = Code(parts[4]);
                var polarity = Code(parts[5]);
                var cited = Cited(parts[6], text);
                if (!FactVocabulary.Events.Contains(kind) || !FactPolarity.All.Contains(polarity) || (actor.Length > 0 && actor != "-" && !FactVocabulary.Actors.Contains(actor)))
                {
                    rejected.Add(line);
                    continue;
                }

                events.Add(new FactEvent(
                    kind,
                    FactValues.Check(parts[2], isDate: true, cited, text),
                    FactValues.Check(parts[3], isDate: false, cited, text),
                    actor == "-" ? string.Empty : actor,
                    polarity,
                    cited));
            }
            else if (head is "طلب" or "request" && parts.Length >= 4)
            {
                var kind = Code(parts[1]);
                if (!FactVocabulary.Requests.Contains(kind))
                {
                    rejected.Add(line);
                    continue;
                }

                var cited = Cited(parts[3], text);
                requests.Add(new FactRequest(kind, FactValues.Check(parts[2], isDate: false, cited, text), cited));
            }
            else if (head is "علاقه" or "relation" or "حدث" or "event" or "طلب" or "request")
            {
                rejected.Add(line);
            }
        }

        return new CaseFacts { Relation = relation, Position = position, Events = events, Requests = requests, Rejected = rejected };
    }

    private static string Code(string value) => value.Trim().Trim('`', '"', '\'', '.').ToLowerInvariant();

    /// <summary>The cited sentence ids that exist («s2,s3», «s2 و s3», «2»).</summary>
    private static List<string> Cited(string value, CaseText text) =>
        Domain.ArabicScript.ToAsciiDigits(value)
            .Split([',', '،', ' ', '؛', ';', 'و'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => v.StartsWith('s') || v.StartsWith('S') ? "s" + v[1..] : "s" + v)
            .Where(id => text.Find(id) is not null)
            .Distinct()
            .ToList();
}
