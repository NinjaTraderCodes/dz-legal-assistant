using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes.Filtering;

/// <summary>One answer of the local model: its letters (null when unreadable), the raw text, and the seconds it took.</summary>
public sealed record ModelAnswer(IReadOnlyList<string>? Letters, string Raw, double Seconds);

/// <summary>Something that answers a filled sheet prompt: the local model, or a test's stand-in.</summary>
public interface ISheetAnswerer
{
    string Model { get; }

    string Digest { get; }

    /// <param name="prompt">The filled prompt.</param>
    /// <param name="options">For each question in order, the letters of its options.</param>
    ModelAnswer Answer(string prompt, IReadOnlyList<IReadOnlyList<string>> options);
}

/// <summary>
/// The local filter model through Ollama, on loopback only (prompts/notes-filter-local.md): one note per call, temperature
/// 0, a fixed seed, an output budget that grows with the sheet (<see cref="TokenBudget"/>), thinking off, and the answer
/// constrained to the shape of schemas/filter-sheet.schema.json made exact for the sheet (<see cref="SheetSchema"/>).
/// </summary>
public sealed class LocalModel : ISheetAnswerer, IDisposable
{
    public const string Endpoint = "http://127.0.0.1:11434";
    private readonly HttpClient http = new() { BaseAddress = new Uri(Endpoint), Timeout = TimeSpan.FromMinutes(5) };

    public LocalModel(string model)
    {
        Model = model;
        var tags = JsonNode.Parse(http.GetStringAsync(new Uri("/api/tags", UriKind.Relative)).GetAwaiter().GetResult())!;
        Digest = (string?)tags["models"]!.AsArray().FirstOrDefault(m => (string)m!["name"]! == model || (string)m!["name"]! == model + ":latest")?["digest"]
                 ?? throw new InvalidOperationException($"Ollama has no model {model}");
    }

    public string Model { get; }

    public string Digest { get; }

    /// <summary>
    /// False once Ollama refused to constrain this model's answer («Failed to initialize samplers»: Jais-3-3.7B on 16
    /// November 2026). The prompt is then sent without the constraint and the letters are read from the reply; the trial's
    /// report says so.
    /// </summary>
    public bool Constrained { get; private set; } = true;

    public ModelAnswer Answer(string prompt, IReadOnlyList<IReadOnlyList<string>> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var clock = Stopwatch.StartNew();
        var (ok, text) = Send(prompt, options);
        if (!ok && Constrained && text.Contains("Failed to initialize samplers", StringComparison.Ordinal))
        {
            Constrained = false;
            (ok, text) = Send(prompt, options);
        }

        clock.Stop();
        if (!ok)
        {
            throw new InvalidOperationException($"Ollama: {text}");
        }

        var raw = (string?)JsonNode.Parse(text)?["message"]?["content"] ?? "";
        return new ModelAnswer(Letters(raw), raw, clock.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// The answer's token budget grows with the sheet: 120 tokens (the prompt's figure, written for sheets of 18 to 25
    /// questions) cut off the letters of a two-claim note's sheet of 33 to 37 questions (16 November 2026). Six tokens a
    /// question and twenty for the braces, never under 120.
    /// </summary>
    public static int TokenBudget(int questions) => Math.Max(120, questions * 6 + 20);

    /// <summary>
    /// The schema of one sheet's answer: <c>{"answers": [...]}</c> with exactly one letter per question, each position
    /// allowing only its own question's letters. With one list of a to l for every position, Gemma answered «d» ("not
    /// mentioned" on a four-option question) to three-option questions, and 12 of its first 17 trial notes were invalid
    /// (16 November 2026). (Ollama refuses <c>"items": false</c> and <c>additionalProperties</c> here.)
    /// </summary>
    public static JsonObject SheetSchema(IReadOnlyList<IReadOnlyList<string>> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new JsonObject
        {
            ["type"] = "object",
            ["required"] = new JsonArray("answers"),
            ["properties"] = new JsonObject
            {
                ["answers"] = new JsonObject
                {
                    ["type"] = "array",
                    ["prefixItems"] = new JsonArray([.. options.Select(o => (JsonNode)new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray([.. o.Select(l => (JsonNode)l)]),
                    })]),
                    ["minItems"] = options.Count,
                    ["maxItems"] = options.Count,
                },
            },
        };
    }

    private (bool Ok, string Text) Send(string prompt, IReadOnlyList<IReadOnlyList<string>> options)
    {
        var body = new JsonObject
        {
            ["model"] = Model,
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = prompt }),
            ["stream"] = false,
            ["think"] = false,
            ["options"] = new JsonObject { ["temperature"] = 0, ["seed"] = 7, ["num_predict"] = TokenBudget(options.Count) },
        };
        if (Constrained)
        {
            body["format"] = SheetSchema(options);
        }

        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = http.PostAsync(new Uri("/api/chat", UriKind.Relative), content).GetAwaiter().GetResult();
        var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return (response.IsSuccessStatusCode, response.IsSuccessStatusCode ? text : $"{(int)response.StatusCode} {text}");
    }

    /// <summary>
    /// The letters of an answer, from the first «{» to the last «}» (a Jais 3 copy may put marker lines before its answer);
    /// null when that is not the schema's object.
    /// </summary>
    public static IReadOnlyList<string>? Letters(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var first = raw.IndexOf('{', StringComparison.Ordinal);
        var last = raw.LastIndexOf('}');
        if (first < 0 || last < first)
        {
            return null;
        }

        try
        {
            var answers = JsonNode.Parse(raw[first..(last + 1)])?["answers"]?.AsArray();
            return answers?.Select(a => (string?)a ?? "").ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public void Dispose() => http.Dispose();
}
