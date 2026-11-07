using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using DzLegal.Drafting;
using DzLegal.ModelProviders.Ollama;

namespace DzLegal.Harness;

public sealed record ScoredStatusQuestion(string Event, IReadOnlyList<double> Scores, int PromptTokens, int CachedTokens,
    double PromptSeconds, double AnswerSeconds, double WallSeconds);

public sealed record ScoredStatusChain(bool Reverse, IReadOnlyList<ScoredStatusQuestion> Questions, double WallSeconds);

/// <summary>Harness experiment only: four pre-mask scores, with literal zero as the append-only history carrier.</summary>
public sealed class ScoredStatusReader
{
    private readonly HttpClient _client;
    private readonly string _model;
    private readonly string _prompt;

    public ScoredStatusReader(HttpClient client, string model, string prompt)
    {
        ArgumentNullException.ThrowIfNull(client);
        LoopbackGuard.EnsureLoopback(client.BaseAddress ?? throw new ArgumentException("A loopback base address is required.", nameof(client)));
        if (model is not ("gemma3:4b" or "gemma4:e4b"))
        {
            throw new ArgumentException("Only the two declared installed local models are permitted.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(prompt) || !prompt.Contains("{{ case }}", StringComparison.Ordinal)
            || !prompt.Contains("{{ claim }}", StringComparison.Ordinal))
        {
            throw new ArgumentException("The scored prompt needs case and claim placeholders.", nameof(prompt));
        }

        _client = client;
        _model = model;
        _prompt = prompt;
    }

    public async Task<ScoredStatusChain> ReadAsync(CaseText text, ClaimElements claim, bool reverse,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(claim);
        var history = new List<Message> { new("system", string.Empty) };
        var common = _prompt.Replace("{{ case }}", text.Render(), StringComparison.Ordinal)
            .Replace("{{ claim }}", claim.Claim, StringComparison.Ordinal);
        var elements = reverse ? claim.Elements.Reverse().ToArray() : claim.Elements.ToArray();
        var rows = new List<ScoredStatusQuestion>();
        var chainClock = Stopwatch.StartNew();
        foreach (var element in elements)
        {
            var question = "\nالسؤال: " + element.Label + "\nالجواب:";
            history.Add(new Message("user", (rows.Count == 0 ? common : string.Empty) + question));
            var body = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["model"] = _model, ["stream"] = false, ["keep_alive"] = "15m",
                ["messages"] = history.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                ["format"] = new { type = "integer", @enum = new[] { 0 } },
                ["logprobs"] = true, ["top_logprobs"] = 20,
                ["options"] = new { num_ctx = 4096, num_gpu = 0, num_predict = 1, temperature = .2, repeat_penalty = 1.1, seed = 0 },
            };
            if (_model == "gemma4:e4b")
            {
                body["think"] = false;
            }

            var clock = Stopwatch.StartNew();
            using var response = await _client.PostAsJsonAsync("api/chat", body, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            clock.Stop();
            var row = ParseResponse(document.RootElement, element.Event, rows.Count > 0, clock.Elapsed.TotalSeconds);
            rows.Add(row);
            // Preserve the model's actual carrier; selected classes never enter later prompts.
            history.Add(new Message("assistant", document.RootElement.GetProperty("message").GetProperty("content").GetString()!));
        }

        return new ScoredStatusChain(reverse, rows, chainClock.Elapsed.TotalSeconds);
    }

    public static ScoredStatusQuestion ParseResponse(JsonElement response, string eventKind, bool extension, double wallSeconds)
    {
        if (!response.GetProperty("done").GetBoolean() || response.GetProperty("eval_count").GetInt32() != 1
            || response.GetProperty("message").GetProperty("content").GetString() != "0")
        {
            throw new InvalidDataException("SCORE PRECONDITION: expected exactly one actual neutral zero token.");
        }

        var records = response.GetProperty("logprobs");
        if (records.GetArrayLength() != 1 || records[0].GetProperty("token").GetString() != "0"
            || !BytesEqual(records[0], (byte)'0'))
        {
            throw new InvalidDataException("SCORE PRECONDITION: neutral carrier is not a byte-exact single token.");
        }

        var scores = new double[4];
        for (var i = 0; i < scores.Length; i++)
        {
            var code = ((char)('1' + i)).ToString();
            var alternatives = records[0].GetProperty("top_logprobs").EnumerateArray()
                .Where(a => a.GetProperty("token").GetString() == code && BytesEqual(a, (byte)('1' + i))).ToArray();
            if (alternatives.Length != 1 || alternatives[0].GetProperty("logprob").ValueKind != JsonValueKind.Number
                || !alternatives[0].GetProperty("logprob").TryGetDouble(out var score) || !double.IsFinite(score))
            {
                throw new InvalidDataException("SCORE PRECONDITION: missing, duplicate or nonfinite byte-exact class score " + code + ".");
            }

            scores[i] = score;
        }

        var prompt = response.GetProperty("prompt_eval_count").GetInt32();
        var cached = response.GetProperty("prompt_eval_cached_count").GetInt32();
        if (prompt < 0 || cached < 0 || prompt >= 4096 || cached >= 4096 || extension && cached == 0)
        {
            throw new InvalidDataException("SCORE PRECONDITION: context allowance or append-only cache reuse failed.");
        }

        return new ScoredStatusQuestion(eventKind, scores, prompt, cached,
            response.GetProperty("prompt_eval_duration").GetDouble() / 1e9,
            response.GetProperty("eval_duration").GetDouble() / 1e9, wallSeconds);
    }

    public static int Choose(IReadOnlyList<double> scores, IReadOnlyList<double> offsets)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(offsets);
        if (scores.Count != 4 || offsets.Count != 4 || scores.Concat(offsets).Any(x => !double.IsFinite(x)))
        {
            throw new ArgumentException("Four finite scores and offsets are required.", nameof(scores));
        }

        var best = 0;
        for (var i = 1; i < 4; i++)
        {
            if (scores[i] + offsets[i] > scores[best] + offsets[best])
            {
                best = i;
            }
        }

        return best + 1;
    }

    public static CaseFacts Apply(CaseFacts baseline, ClaimElements claim, IReadOnlyDictionary<string, int> choices)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(choices);
        if (choices.Count != claim.Elements.Count || claim.Elements.Any(e => !choices.TryGetValue(e.Event, out var c) || c is < 1 or > 4))
        {
            throw new ArgumentException("Every asked event needs exactly one resolved class.", nameof(choices));
        }

        var events = new List<FactEvent>();
        foreach (var element in claim.Elements)
        {
            var choice = choices[element.Event];
            if (choice == 4)
            {
                continue;
            }

            var polarity = choice switch { 1 => FactPolarity.Affirmed, 2 => FactPolarity.Denied, _ => FactPolarity.Disputed };
            var prior = baseline.Events.SingleOrDefault(e => e.Kind == element.Event);
            var actor = element.Actor.Length == 0 || FactVocabulary.Actors.Contains(element.Actor) ? element.Actor
                : element.Actor == baseline.Position ? "plaintiff" : "defendant";
            events.Add(prior is null ? new FactEvent(element.Event, null, null, actor, polarity, []) : prior with { Polarity = polarity });
        }

        return baseline with { Events = events };
    }

    private static bool BytesEqual(JsonElement token, byte expected) =>
        token.GetProperty("bytes") is var bytes && bytes.GetArrayLength() == 1 && bytes[0].GetInt32() == expected;

    private sealed record Message(string Role, string Content);
}
