using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.Drafting;
using DzLegal.Law;
using DzLegal.ModelProviders.Ollama;

// app-reader --notes <export notes.jsonl> --law <data/law> --model gemma4:e4b --out <folder> [--limit <n>]
// The owner's evidence test 1 (19 November 2026): "score Gemma 4 E4B as the app runs it today (the fact reader, thinking
// off)". The claim type comes from the app's GroundsAnalyzer (stage 1, the reading: every catalog claim offered, since a
// note has no court section); its article selection (stage 3) is answered empty here, since it does not change the claim
// types. The facts come from the app's ClaimFactReader (the Alpha-01 reader) on each gold claim type (the scorer's
// "type given" setting). The Ollama options are the app's shipped ones (Composition.cs, appsettings.json): temperature
// 0.2, maxTokens 1000, repeat penalty 1.1, keepAlive 15m, timeout 10 min, and thinking off for Gemma 4 (ThinkFor).
// Writes answers.jsonl (end to end: the claim block), given.jsonl (type given: claims, events, requests, values) and
// calls.jsonl (one line per model call, with its seconds).
Console.OutputEncoding = System.Text.Encoding.UTF8;
var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (var i = 0; i + 1 < args.Length; i += 2)
{
    options[args[i]] = args[i + 1];
}

string Need(string name) => options.TryGetValue(name, out var v) ? v : throw new ArgumentException($"app-reader needs {name}");
var model = Need("--model");
var lawRoot = Need("--law");
var outDir = Need("--out");
var limit = options.TryGetValue("--limit", out var l) ? int.Parse(l, CultureInfo.InvariantCulture) : int.MaxValue;
Directory.CreateDirectory(outDir);

var ollama = new OllamaOptions
{
    Endpoint = new Uri("http://127.0.0.1:11434"),
    Model = model,
    Timeout = TimeSpan.FromMinutes(10),
    Temperature = 0.2,
    MaxTokens = 1000,
    RepeatPenalty = 1.1,
    Think = model.StartsWith("gemma4", StringComparison.OrdinalIgnoreCase) ? false : null,
    KeepAlive = "15m",
};
using var inner = new OllamaModelProvider(ollama);
var calls = new List<string>();
var provider = new AppReaderParts.Timed(inner, calls);
var knowledge = LegalKnowledgeLoader.Load(lawRoot);
var analyzer = new GroundsAnalyzer(provider);
var reader = new ClaimFactReader(provider, ClaimFactReader.LoadPrompt(lawRoot), ClaimFactReader.LoadElements(lawRoot));
var elements = ClaimFactReader.LoadElements(lawRoot);
var labels = knowledge.Claims.ToDictionary(c => c.Id, c => c.Label.Ar, StringComparer.Ordinal);
var json = new JsonSerializerOptions { WriteIndented = false };

var notes = File.ReadLines(Need("--notes")).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => JsonNode.Parse(x)!).Take(limit).ToList();
using var answers = new StreamWriter(Path.Combine(outDir, "answers.jsonl"), false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };
using var given = new StreamWriter(Path.Combine(outDir, "given.jsonl"), false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };
var done = 0;
foreach (var note in notes)
{
    var id = note["id"]!.GetValue<string>();
    var text = note["text"]!;
    string F(string name) => text[name]?.GetValue<string>() ?? string.Empty;
    var fields = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["subject"] = F("subject"), ["facts"] = F("facts"), ["requests"] = F("requests"), ["documents"] = F("documents"),
    };

    // End to end: the claim types the app's grounds analysis recognises (empty: no catalog claim -> outside).
    provider.Note = id;
    var analysis = await analyzer.AnalyseAsync(knowledge, fields["subject"], fields["facts"], courtSection: null);
    var e2eClaims = analysis.ClaimIds.Count > 0 ? analysis.ClaimIds.ToList() : ["outside"];
    answers.WriteLine(JsonSerializer.Serialize(new JsonObject { ["id"] = id, ["reading"] = AppReaderParts.Mapping.Reading(e2eClaims, [], [], []) }, json));

    // Type given: the app's ClaimFactReader on each gold claim type.
    var goldClaims = note["gold"]!["claims"]!.AsArray().Select(c => c!.GetValue<string>()).ToList();
    var events = new JsonArray();
    var requests = new JsonArray();
    var values = new JsonArray();
    var caseText = CaseText.From(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["claim.subject"] = fields["subject"], ["notes.facts"] = fields["facts"], ["notes.requests"] = fields["requests"], ["documents.list"] = fields["documents"],
    });
    foreach (var claim in goldClaims.Where(c => c != "outside" && reader.Knows(c)))
    {
        var (facts, _) = await reader.ReadAsync(caseText, claim, labels.GetValueOrDefault(claim, claim));
        foreach (var element in elements[claim].Elements)
        {
            var e = facts.Events.FirstOrDefault(x => x.Kind == element.Event);
            events.Add(new JsonObject
            {
                ["claim"] = claim, ["kind"] = element.Event, ["polarity"] = AppReaderParts.Mapping.Polarity(e?.Polarity), ["confidence"] = 1.0,
                ["abstained"] = false, ["actor"] = e?.Actor is { Length: > 0 } a ? a : "unknown", ["evidence"] = new JsonArray(),
            });
            if (e is not null)
            {
                foreach (var (value, kind) in new[] { (e.Date, "date"), (e.Amount, "amount") })
                {
                    if (value is not null && AppReaderParts.Mapping.Value(value, kind, element.Event, e.Sentences, caseText, fields) is { } v)
                    {
                        values.Add(v);
                    }
                }
            }
        }

        foreach (var r in facts.Requests)
        {
            var req = new JsonObject { ["kind"] = r.Kind, ["confidence"] = 1.0, ["evidence"] = new JsonArray() };
            if (r.Amount is not null && AppReaderParts.Mapping.Value(r.Amount, "amount", r.Kind, r.Sentences, caseText, fields) is { } v)
            {
                req["amount"] = v.DeepClone();
                values.Add(v);
            }

            requests.Add(req);
        }
    }

    given.WriteLine(JsonSerializer.Serialize(new JsonObject { ["id"] = id, ["reading"] = AppReaderParts.Mapping.Reading(goldClaims, events, requests, values) }, json));
    answers.Flush();
    given.Flush();
    done++;
    Console.WriteLine($"{done}/{notes.Count} {id}: e2e {string.Join('+', e2eClaims)}; {events.Count} events, {requests.Count} requests, {values.Count} values");
}

File.WriteAllLines(Path.Combine(outDir, "calls.jsonl"), calls);
return 0;

namespace AppReaderParts
{
    /// <summary>Times each model call; the article selection (stage 3) is answered empty: it never changes the claim types.</summary>
    internal sealed class Timed(IModelProvider inner, List<string> log) : IModelProvider
    {
        public string Note { get; set; } = string.Empty;

        public string Name => inner.Name;

        public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
        {
            if (request.SectionId == GroundsAnalyzer.SelectionRequestId)
            {
                return string.Empty;
            }

            var clock = Stopwatch.StartNew();
            var answer = await inner.GenerateAsync(request, cancellationToken);
            log.Add(JsonSerializer.Serialize(new { note = Note, call = request.SectionId, seconds = Math.Round(clock.Elapsed.TotalSeconds, 1) }));
            return answer;
        }
    }

    internal static class Mapping
    {
        public static string Polarity(string? p) => p switch
        {
            FactPolarity.Affirmed => "stated",
            FactPolarity.Denied => "denied",
            FactPolarity.Disputed => "disputed",
            _ => "notMentioned",
        };

        public static JsonObject Reading(IReadOnlyList<string> claims, JsonArray events, JsonArray requests, JsonArray values) => new()
        {
            ["claim"] = new JsonObject
            {
                ["answers"] = new JsonArray([.. claims.Select(c => (JsonNode)new JsonObject
                {
                    ["claimId"] = c, ["confidence"] = 1.0, ["evidence"] = new JsonArray(),
                })]),
                ["others"] = new JsonArray(),
                ["abstained"] = false,
            },
            ["events"] = events,
            ["slots"] = new JsonArray(),
            ["requests"] = requests,
            ["values"] = values,
            ["info"] = new JsonObject { ["name"] = "app-alpha01", ["version"] = "GroundsAnalyzer stage 1 + ClaimFactReader", ["files"] = new JsonObject() },
        };

        /// <summary>
        /// A value the app read, in the scheme's form: its place in the note (the first place its text occurs in one of the
        /// sentences it was read from, else in the note), the normalised form (a date as yyyy-mm-dd), and the role (the
        /// event kind, or the request kind for a requested amount).
        /// </summary>
        public static JsonObject? Value(FactValue value, string kind, string role, IReadOnlyList<string> sentences, CaseText text, IReadOnlyDictionary<string, string> fields)
        {
            var surface = value.Text.Trim();
            if (surface.Length == 0)
            {
                return null;
            }

            var places = sentences.Select(text.Find).OfType<CaseSentence>().Concat(text.Sentences);
            foreach (var sentence in places)
            {
                var field = sentence.FieldId switch { "claim.subject" => "subject", "notes.facts" => "facts", "notes.requests" => "requests", _ => "documents" };
                var full = fields[field];
                var at = full.IndexOf(sentence.Text, StringComparison.Ordinal);
                var inside = sentence.Text.IndexOf(surface, StringComparison.Ordinal);
                if (at < 0 || inside < 0)
                {
                    continue;
                }

                return new JsonObject
                {
                    ["kind"] = kind,
                    ["normalized"] = kind == "date" ? Date(value.Normalized) : value.Normalized,
                    ["span"] = new JsonObject { ["field"] = field, ["start"] = at + inside, ["length"] = surface.Length },
                    ["roles"] = new JsonArray(role),
                    ["confidence"] = 1.0,
                };
            }

            return null;
        }

        /// <summary>The app writes dd/mm/yyyy (or yyyy); the scheme writes yyyy-mm-dd.</summary>
        private static string Date(string normalized)
        {
            var parts = normalized.Split('/');
            return parts.Length == 3 ? $"{parts[2]}-{parts[1]}-{parts[0]}" : normalized;
        }
    }
}
