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

    ModelAnswer Answer(string prompt, int questions);
}

/// <summary>
/// The local filter model through Ollama, on loopback only (prompts/notes-filter-local.md): one note per call, temperature
/// 0, a fixed seed, at most 120 output tokens, the answer constrained to schemas/filter-sheet.schema.json, thinking off.
/// </summary>
public sealed class LocalModel : ISheetAnswerer, IDisposable
{
    public const string Endpoint = "http://127.0.0.1:11434";
    private readonly HttpClient http = new() { BaseAddress = new Uri(Endpoint), Timeout = TimeSpan.FromMinutes(5) };
    private readonly JsonNode schema;

    public LocalModel(string model, string schemaPath)
    {
        Model = model;
        schema = NotesJson.Load(schemaPath);
        var tags = JsonNode.Parse(http.GetStringAsync(new Uri("/api/tags", UriKind.Relative)).GetAwaiter().GetResult())!;
        Digest = (string?)tags["models"]!.AsArray().FirstOrDefault(m => (string)m!["name"]! == model || (string)m!["name"]! == model + ":latest")?["digest"]
                 ?? throw new InvalidOperationException($"Ollama has no model {model}");
    }

    public string Model { get; }

    public string Digest { get; }

    public ModelAnswer Answer(string prompt, int questions)
    {
        var body = new JsonObject
        {
            ["model"] = Model,
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = prompt }),
            ["format"] = schema.DeepClone(),
            ["stream"] = false,
            ["think"] = false,
            ["options"] = new JsonObject { ["temperature"] = 0, ["seed"] = 7, ["num_predict"] = 120 },
        };
        var clock = Stopwatch.StartNew();
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = http.PostAsync(new Uri("/api/chat", UriKind.Relative), content).GetAwaiter().GetResult();
        var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        clock.Stop();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Ollama: {(int)response.StatusCode} {text}");
        }

        var raw = (string?)JsonNode.Parse(text)?["message"]?["content"] ?? "";
        return new ModelAnswer(Letters(raw), raw, clock.Elapsed.TotalSeconds);
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
