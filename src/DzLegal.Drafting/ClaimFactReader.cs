using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Drafting;

/// <summary>A date or amount found in the case by the program, numbered for the reader to choose from.</summary>
public sealed record CaseValue(string Id, string Kind, string Normalized, string SentenceId)
{
    public const string Date = "date";
    public const string Amount = "amount";
}

/// <summary>A role a claim type allows: the relationship and the plaintiff's position in it.</summary>
public sealed record ClaimRole(string Relation, string Position, string Label);

/// <summary>A fact the reader looks for in a claim of this type, and which values it may carry.</summary>
public sealed record ClaimElement
{
    public required string Event { get; init; }

    /// <summary>plaintiff, defendant, a position (lessor…), or empty when it does not matter.</summary>
    public string Actor { get; init; } = string.Empty;

    public required string Label { get; init; }

    public bool Date { get; init; }

    public bool Amount { get; init; }
}

public sealed record ClaimRequestElement
{
    public required string Kind { get; init; }

    public required string Label { get; init; }

    public bool Amount { get; init; }
}

/// <summary>What a reader looks for once the claim type is known (<c>data/law/claim-elements.json</c>).</summary>
public sealed record ClaimElements
{
    public required string Claim { get; init; }

    public IReadOnlyList<ClaimRole> Roles { get; init; } = [];

    public IReadOnlyList<ClaimElement> Elements { get; init; } = [];

    public IReadOnlyList<ClaimRequestElement> Requests { get; init; } = [];
}

public sealed record ClaimElementsFile
{
    public IReadOnlyList<ClaimElements> Claims { get; init; } = [];
}

/// <summary>
/// Alpha-01 (harness only): reads the facts of a case whose claim type the lawyer has confirmed. The relationship and the
/// plaintiff's position follow from the claim type, by code; the model is asked to choose a role only when the claim type
/// allows several. It then answers one short line per fact and request of that claim type: whether the text states it,
/// denies it or reports it disputed, and which of the dates and amounts the program found belong to it. It cannot invent
/// a value, and it is never asked anything outside the claim type.
/// </summary>
public sealed partial class ClaimFactReader(IModelProvider provider, string prompt, IReadOnlyDictionary<string, ClaimElements> catalog)
{
    public const string RequestId = "facts.claim";
    public const string PromptFile = "prompts/read-claim-elements.ar.txt";
    public const string ElementsFile = "claim-elements.json";

    /// <summary>About ten short lines.</summary>
    public const int MaxTokens = 160;

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly IReadOnlyDictionary<string, ClaimElements> _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    private readonly string _prompt = prompt is { Length: > 0 } && prompt.Contains("{{ case }}", StringComparison.Ordinal) && prompt.Contains("{{ questions }}", StringComparison.Ordinal)
        ? prompt
        : throw new ArgumentException("The claim-elements prompt needs {{ case }} and {{ questions }} placeholders.", nameof(prompt));

    public static string LoadPrompt(string lawRoot) => File.ReadAllText(Path.Combine(lawRoot, PromptFile));

    /// <summary>The elements of every claim type, checked against the closed vocabularies.</summary>
    public static IReadOnlyDictionary<string, ClaimElements> LoadElements(string lawRoot)
    {
        var path = Path.Combine(lawRoot, ElementsFile);
        var file = JsonSerializer.Deserialize<ClaimElementsFile>(File.ReadAllText(path), DefinitionJson.Options) ?? new ClaimElementsFile();
        var errors = file.Claims.SelectMany(Validate).ToList();
        return errors.Count > 0
            ? throw new InvalidDataException($"{path}: {string.Join("; ", errors)}")
            : file.Claims.ToDictionary(c => c.Claim, StringComparer.Ordinal);
    }

    public static IEnumerable<string> Validate(ClaimElements claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (claim.Roles.Count == 0)
        {
            yield return $"{claim.Claim}: no role";
        }

        foreach (var r in claim.Roles.Where(r => !FactVocabulary.Relations.Contains(r.Relation) || !FactVocabulary.Positions.Contains(r.Position)))
        {
            yield return $"{claim.Claim}: unknown role {r.Relation}/{r.Position}";
        }

        foreach (var e in claim.Elements.Where(e => !FactVocabulary.Events.Contains(e.Event)
            || (e.Actor.Length > 0 && !FactVocabulary.Actors.Contains(e.Actor) && !FactVocabulary.Positions.Contains(e.Actor))))
        {
            yield return $"{claim.Claim}: unknown event or actor {e.Event}/{e.Actor}";
        }

        foreach (var q in claim.Requests.Where(q => !FactVocabulary.Requests.Contains(q.Kind)))
        {
            yield return $"{claim.Claim}: unknown request {q.Kind}";
        }
    }

    /// <summary>Every date and amount written in the case, numbered in reading order (a value repeated in two sentences gets two numbers).</summary>
    public static IReadOnlyList<CaseValue> Values(CaseText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var values = new List<CaseValue>();
        string Next() => "v" + (values.Count + 1).ToString(CultureInfo.InvariantCulture);
        foreach (var s in text.Sentences)
        {
            var dates = FactValues.DatesIn(s.Text);
            foreach (var d in dates)
            {
                values.Add(new CaseValue(Next(), CaseValue.Date, d, s.Id));
            }

            // A lone year (2016) is a date, not an amount.
            foreach (var a in FactValues.AmountsIn(s.Text).Where(a => !(a.Length == 4 && dates.Contains(a))))
            {
                values.Add(new CaseValue(Next(), CaseValue.Amount, a, s.Id));
            }
        }

        return values;
    }

    public bool Knows(string claimId) => _catalog.ContainsKey(claimId);

    public ModelRequest BuildRequest(CaseText text, string claimId, string claimLabel)
    {
        ArgumentNullException.ThrowIfNull(text);
        var claim = _catalog[claimId];
        var values = Values(text);
        var list = values.Count == 0
            ? "لا توجد تواريخ ولا مبالغ في النص."
            : string.Join('\n', values.Select(v => $"{v.Id} = {v.Normalized} ({(v.Kind == CaseValue.Date ? "تاريخ" : "مبلغ")}، في {v.SentenceId})"));
        var user = _prompt
            .Replace("{{ claim }}", claimLabel, StringComparison.Ordinal)
            .Replace("{{ case }}", text.Render(), StringComparison.Ordinal)
            .Replace("{{ values }}", list, StringComparison.Ordinal)
            .Replace("{{ questions }}", Questions(claim), StringComparison.Ordinal);
        return new ModelRequest(RequestId, string.Empty, user, string.Join('\n', text.Sentences.Select(s => s.Text))) { MaxTokens = MaxTokens };
    }

    /// <summary>The numbered questions: the role (only when the claim type allows several), the facts, the requests.</summary>
    public static string Questions(ClaimElements claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var sb = new StringBuilder();
        if (claim.Roles.Count > 1)
        {
            sb.AppendLine("p) من هو المدعي؟ " + string.Join("، ", claim.Roles.Select((r, i) => $"{i + 1} = {r.Label}")));
        }

        static string Slots(bool date, bool amount) => (date, amount) switch
        {
            (true, true) => " [التاريخ والمبلغ]",
            (true, false) => " [التاريخ]",
            (false, true) => " [المبلغ]",
            _ => string.Empty,
        };

        for (var i = 0; i < claim.Elements.Count; i++)
        {
            var e = claim.Elements[i];
            sb.AppendLine(CultureInfo.InvariantCulture, $"{i + 1}) {e.Label}{Slots(e.Date, e.Amount)}");
        }

        for (var i = 0; i < claim.Requests.Count; i++)
        {
            var q = claim.Requests[i];
            sb.AppendLine(CultureInfo.InvariantCulture, $"r{i + 1}) طلب: {q.Label}{Slots(false, q.Amount)}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <returns>The facts, checked, and the model's raw answer.</returns>
    public async Task<(CaseFacts Facts, string Answer)> ReadAsync(CaseText text, string claimId, string claimLabel, CancellationToken cancellationToken = default)
    {
        var answer = new StringBuilder();
        await foreach (var piece in _provider.StreamAsync(BuildRequest(text, claimId, claimLabel), cancellationToken))
        {
            answer.Append(piece);
        }

        return (Parse(answer.ToString(), text, _catalog[claimId]), answer.ToString());
    }

    /// <summary>
    /// Turns the answer lines into facts. The role comes from the claim type (or the reader's choice among its roles); a
    /// value number that does not exist, or of the wrong kind for the element, is dropped; a line that cannot be read is
    /// kept in <see cref="CaseFacts.Rejected"/>; a question left unanswered counts as not mentioned.
    /// </summary>
    public static CaseFacts Parse(string answer, CaseText text, ClaimElements claim)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(claim);
        var values = Values(text).ToDictionary(v => v.Id, StringComparer.Ordinal);
        var role = claim.Roles[0];
        var events = new List<FactEvent>();
        var requests = new List<FactRequest>();
        var rejected = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<(string Id, string Status, List<CaseValue> Values, string Line)>();
        foreach (var raw in (answer ?? string.Empty).Split('\n'))
        {
            var line = ArabicScript.ToAsciiDigits(raw).Trim().TrimStart('*', '`', '-', '•').TrimEnd('*', '`').Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var m = AnswerLine().Match(line);
            if (!m.Success)
            {
                if (line.Contains('|', StringComparison.Ordinal))
                {
                    rejected.Add(line);
                }

                continue;
            }

            var id = m.Groups["id"].Value.ToLowerInvariant();
            if (!seen.Add(id))
            {
                continue;
            }

            if (id == "p")
            {
                if (int.TryParse(m.Groups["status"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var choice) && choice >= 1 && choice <= claim.Roles.Count)
                {
                    role = claim.Roles[choice - 1];
                }
                else
                {
                    rejected.Add(line);
                }

                continue;
            }

            var ids = ValueId().Matches(m.Groups["rest"].Value).Select(v => v.Value.ToLowerInvariant()).ToList();
            rows.Add((id, m.Groups["status"].Value.ToLowerInvariant(), [.. ids.Select(v => values.GetValueOrDefault(v)).OfType<CaseValue>()], line));
        }

        foreach (var (id, status, found, line) in rows)
        {
            var polarity = status switch
            {
                "yes" => FactPolarity.Affirmed,
                "no" => FactPolarity.Denied,
                "disputed" => FactPolarity.Disputed,
                "absent" => null,
                _ => "?",
            };
            if (polarity == "?")
            {
                rejected.Add(line);
                continue;
            }

            if (polarity is null)
            {
                continue;
            }

            if (id.StartsWith('r'))
            {
                if (!int.TryParse(id[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var r) || r < 1 || r > claim.Requests.Count)
                {
                    rejected.Add(line);
                    continue;
                }

                var q = claim.Requests[r - 1];
                if (polarity == FactPolarity.Affirmed)
                {
                    var amount = q.Amount ? found.FirstOrDefault(v => v.Kind == CaseValue.Amount) : null;
                    requests.Add(new FactRequest(q.Kind, Value(amount), amount is null ? [] : [amount.SentenceId]));
                }

                continue;
            }

            if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1 || n > claim.Elements.Count)
            {
                rejected.Add(line);
                continue;
            }

            var e = claim.Elements[n - 1];
            var date = e.Date ? found.FirstOrDefault(v => v.Kind == CaseValue.Date) : null;
            var sum = e.Amount ? found.FirstOrDefault(v => v.Kind == CaseValue.Amount) : null;
            events.Add(new FactEvent(
                e.Event,
                Value(date),
                Value(sum),
                Actor(e.Actor, role),
                polarity,
                [.. new[] { date, sum }.OfType<CaseValue>().Select(v => v.SentenceId).Distinct()]));
        }

        return new CaseFacts { Relation = role.Relation, Position = role.Position, Events = events, Requests = requests, Rejected = rejected };
    }

    /// <summary>A value chosen by number is in its sentence by construction.</summary>
    private static FactValue? Value(CaseValue? v) => v is null ? null : new FactValue(v.Normalized, v.Normalized, ValueCheck.InCitedSentence);

    private static string Actor(string actor, ClaimRole role) =>
        actor.Length == 0 || FactVocabulary.Actors.Contains(actor) ? actor
        : actor == role.Position ? "plaintiff"
        : "defendant";

    /// <summary>«3 | yes | v1 v2», «r1 | absent | -», «p | 2»; a colon or dash may stand for the bar.</summary>
    [GeneratedRegex(@"^(?<id>[pPrR]?\d+|[pP])\s*[)|:\-]\s*(?<status>[A-Za-z]+|\d+)\s*(?:[|:\-]\s*(?<rest>.*))?$")]
    private static partial Regex AnswerLine();

    [GeneratedRegex(@"\b[vV]\d+\b")]
    private static partial Regex ValueId();
}
