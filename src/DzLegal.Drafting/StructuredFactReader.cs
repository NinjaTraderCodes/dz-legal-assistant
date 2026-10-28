using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DzLegal.Drafting;

/// <summary>A date or amount found in the case by the program, numbered for the reader to choose from.</summary>
public sealed record CaseValue(string Id, string Kind, string Normalized, string SentenceId);

/// <summary>
/// The fact reader with an enforced answer format (trial, harness only). The program lists every date and amount of the
/// case under a number (v1, v2…); the model answers in JSON whose schema, built for this case, only allows the case's own
/// sentence numbers, value numbers and the closed codes. It can therefore not invent a value, cite a sentence that does
/// not exist or write an unreadable line; whether it ties each value to the right event is what remains to measure.
/// </summary>
public sealed class StructuredFactReader(IModelProvider provider, string prompt)
{
    public const string RequestId = "facts.structured";
    public const string PromptFile = "prompts/read-case-facts-json.ar.txt";
    public const string None = "none";

    /// <summary>JSON is longer than the coded lines; room for about 15 events.</summary>
    public const int MaxTokens = 900;

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly string _prompt = prompt is { Length: > 0 } && prompt.Contains("{{ case }}", StringComparison.Ordinal) && prompt.Contains("{{ values }}", StringComparison.Ordinal)
        ? prompt
        : throw new ArgumentException("The structured fact-reading prompt needs {{ case }} and {{ values }} placeholders.", nameof(prompt));

    public static string LoadPrompt(string lawRoot) => File.ReadAllText(Path.Combine(lawRoot, PromptFile));

    /// <summary>Every date and amount written in the case, numbered in reading order (a value repeated in two sentences gets two numbers).</summary>
    public static IReadOnlyList<CaseValue> Values(CaseText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var values = new List<CaseValue>();
        foreach (var s in text.Sentences)
        {
            foreach (var d in FactValues.DatesIn(s.Text))
            {
                values.Add(new CaseValue("v" + (values.Count + 1).ToString(CultureInfo.InvariantCulture), "date", d, s.Id));
            }

            foreach (var a in FactValues.AmountsIn(s.Text).Where(a => !IsYear(a, s.Text)))
            {
                values.Add(new CaseValue("v" + (values.Count + 1).ToString(CultureInfo.InvariantCulture), "amount", a, s.Id));
            }
        }

        return values;
    }

    /// <summary>A lone year (2016) is listed as a date, not as an amount.</summary>
    private static bool IsYear(string amount, string sentence) =>
        amount.Length == 4 && amount[0] is '1' or '2' && FactValues.DatesIn(sentence).Contains(amount);

    public ModelRequest BuildRequest(CaseText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var values = Values(text);
        var list = values.Count == 0
            ? "لا توجد تواريخ ولا مبالغ في النص."
            : string.Join('\n', values.Select(v => $"{v.Id} = {v.Normalized} ({(v.Kind == "date" ? "تاريخ" : "مبلغ")}، في {v.SentenceId})"));
        var user = _prompt
            .Replace("{{ section }}", text.CourtSection.Length > 0 ? text.CourtSection : "لم يحدد", StringComparison.Ordinal)
            .Replace("{{ case }}", text.Render(), StringComparison.Ordinal)
            .Replace("{{ values }}", list, StringComparison.Ordinal);
        return new ModelRequest(RequestId, string.Empty, user, string.Join('\n', text.Sentences.Select(s => s.Text)))
        {
            MaxTokens = MaxTokens,
            OutputSchema = Schema(text, values),
        };
    }

    /// <summary>The answer's JSON schema for this case: closed codes, and only this case's sentence and value numbers.</summary>
    public static JsonElement Schema(CaseText text, IReadOnlyList<CaseValue> values)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(values);
        static JsonObject Enum(IEnumerable<string> items) => new() { ["type"] = "string", ["enum"] = new JsonArray([.. items.Select(i => (JsonNode?)i)]) };
        JsonObject Sentences() => new() { ["type"] = "array", ["items"] = Enum(text.Sentences.Select(s => s.Id)), ["minItems"] = 1, ["maxItems"] = 3 };
        JsonObject ValueOf(string kind) => Enum(values.Where(v => v.Kind == kind).Select(v => v.Id).Prepend(None));

        static JsonObject Obj(JsonObject properties) => new()
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray([.. properties.Select(p => (JsonNode?)p.Key)]),
            ["additionalProperties"] = false,
        };

        var schema = Obj(new JsonObject
        {
            ["relation"] = Enum(FactVocabulary.Relations),
            ["position"] = Enum(FactVocabulary.Positions),
            ["relation_sentences"] = Sentences(),
            ["events"] = new JsonObject
            {
                ["type"] = "array",
                ["maxItems"] = 12,
                ["items"] = Obj(new JsonObject
                {
                    ["kind"] = Enum(FactVocabulary.Events),
                    ["sentences"] = Sentences(),
                    ["actor"] = Enum(FactVocabulary.Actors.Append(None)),
                    ["polarity"] = Enum(FactPolarity.All),
                    ["date"] = ValueOf("date"),
                    ["amount"] = ValueOf("amount"),
                }),
            },
            ["requests"] = new JsonObject
            {
                ["type"] = "array",
                ["maxItems"] = 6,
                ["items"] = Obj(new JsonObject
                {
                    ["kind"] = Enum(FactVocabulary.Requests),
                    ["sentences"] = Sentences(),
                    ["amount"] = ValueOf("amount"),
                }),
            },
        });
        return JsonSerializer.SerializeToElement(schema);
    }

    /// <returns>The facts, checked as for <see cref="FactReader"/>, and the model's raw JSON.</returns>
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
    /// The JSON answer, with value numbers replaced by the values they stand for, read through the same checks as the
    /// coded lines (<see cref="FactReader.Parse"/>). A JSON that cannot be read gives no facts, never a guess.
    /// </summary>
    public static CaseFacts Parse(string answer, CaseText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return FactReader.Parse(ToLines(answer, text), text);
    }

    /// <summary>The JSON answer as coded lines (value numbers resolved), for the shared checks and the saved run records.</summary>
    public static string ToLines(string answer, CaseText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var start = (answer ?? string.Empty).IndexOf('{', StringComparison.Ordinal);
        var end = (answer ?? string.Empty).LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return string.Empty;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(answer![start..(end + 1)]) as JsonObject;
        }
        catch (JsonException)
        {
            return string.Empty;
        }

        if (root is null)
        {
            return string.Empty;
        }

        var values = Values(text).ToDictionary(v => v.Id, v => v.Normalized, StringComparer.Ordinal);
        string S(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) && s.Length > 0 && s != None ? s : "-";
        string V(JsonNode? n) => S(n) is var id && values.TryGetValue(id, out var value) ? value : "-";
        string Ids(JsonNode? n) => n is JsonArray a && a.Count > 0 ? string.Join(',', a.Select(S)) : "-";

        var lines = new List<string> { $"علاقة | {S(root["relation"])} | {S(root["position"])} | {Ids(root["relation_sentences"])}" };
        foreach (var e in (root["events"] as JsonArray ?? []).OfType<JsonObject>())
        {
            lines.Add($"حدث | {S(e["kind"])} | {V(e["date"])} | {V(e["amount"])} | {S(e["actor"])} | {S(e["polarity"])} | {Ids(e["sentences"])}");
        }

        foreach (var r in (root["requests"] as JsonArray ?? []).OfType<JsonObject>())
        {
            lines.Add($"طلب | {S(r["kind"])} | {V(r["amount"])} | {Ids(r["sentences"])}");
        }

        return string.Join('\n', lines);
    }
}
