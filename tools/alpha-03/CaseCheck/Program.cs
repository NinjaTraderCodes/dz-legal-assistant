using System.Text.Json;
using DzLegal.Drafting;
using DzLegal.Harness;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: CaseCheck <alpha-03-cases> <law root>");
    return 2;
}

var cases = FactsEvaluator.LoadCases(args[0]);
var catalog = ClaimFactReader.LoadElements(args[1]);
var problems = FactCaseCheck.Problems(cases, catalog).ToList();
using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(args[0], "cases.json")));
var denseCases = document.RootElement.GetProperty("cases").EnumerateArray().ToDictionary(c => S(c, "name"), StringComparer.Ordinal);
var eventCount = 0;
var requestCount = 0;
var evidenceCount = 0;
foreach (var c in cases)
{
    void Check(bool condition, string detail)
    {
        if (!condition)
        {
            problems.Add(c.Name + ": " + detail);
        }
    }

    var text = CaseText.From(c.Inputs);
    var annotation = denseCases[c.Name].GetProperty("annotation");
    var sentences = annotation.GetProperty("sentences").EnumerateArray().ToArray();
    Check(sentences.Length == text.Sentences.Count, "sentence count differs from CaseText");
    foreach (var sentence in sentences)
    {
        var actual = text.Find(S(sentence, "id"));
        Check(actual is not null && actual.FieldId == S(sentence, "field") && actual.Text == S(sentence, "text"), "sentence identity/text mismatch");
    }

    string[] Evidence(JsonElement row, string property)
    {
        var ids = row.GetProperty(property).EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray();
        Check(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length && ids.All(id => text.Find(id) is not null), "invalid/duplicate evidence IDs");
        evidenceCount += ids.Length;
        return ids;
    }

    void Value(JsonElement row, string property, bool date)
    {
        var raw = S(row, property);
        var ids = Evidence(row, property + "Evidence");
        Check((raw.Length == 0) == (ids.Length == 0), "value/evidence absence mismatch");
        if (raw.Length == 0)
        {
            return;
        }

        var value = FactValues.Check(raw, date, ids, text);
        Check(value?.Check == ValueCheck.InCitedSentence && value.Normalized == raw, "value missing from its cited sentence or not normalized");
        Check(ids.All(id => Evidence(row, "evidence").Contains(id, StringComparer.Ordinal)), "value citation not in item evidence");
    }

    Check(Evidence(annotation, "roleEvidence").Length > 0, "role lacks evidence");
    var claim = catalog[c.Claim];
    Check(claim.Roles.Any(r => r.Position == c.Gold.Position && r.Relation == c.Gold.Relation), "relation/position combination not in catalogue");
    var events = annotation.GetProperty("events").EnumerateArray().ToArray();
    var requests = annotation.GetProperty("requests").EnumerateArray().ToArray();
    Check(events.Select(e => S(e, "kind")).SequenceEqual(claim.Elements.Select(e => e.Event)), "dense events do not cover catalogue exactly in order");
    Check(requests.Select(r => S(r, "kind")).SequenceEqual(claim.Requests.Select(r => r.Kind)), "dense requests do not cover catalogue exactly in order");
    eventCount += events.Length;
    requestCount += requests.Length;
    foreach (var row in events)
    {
        var kind = S(row, "kind");
        var polarity = S(row, "polarity");
        var ids = Evidence(row, "evidence");
        Check(polarity == "absent" || FactPolarity.All.Contains(polarity), "unknown dense polarity");
        Check(polarity == "absent" ? ids.Length == 0 && S(row, "actor").Length == 0 : ids.Length > 0 && FactVocabulary.Actors.Contains(S(row, "actor")), "event evidence/actor incompatible with status");
        var spec = claim.Elements.Single(e => e.Event == kind);
        Check((S(row, "date").Length == 0 || spec.Date) && (S(row, "amount").Length == 0 || spec.Amount), "value not asked for this event");
        var actor = spec.Actor.Length == 0 || FactVocabulary.Actors.Contains(spec.Actor) ? spec.Actor : spec.Actor == c.Gold.Position ? "plaintiff" : "defendant";
        Check(polarity == "absent" || actor.Length == 0 || S(row, "actor") == actor, "actor differs from catalogue role resolution");
        Check(row.GetProperty("documentsOnly").GetBoolean() == (ids.Length > 0 && ids.All(id => text.Find(id)?.FieldId == "documents.list")), "documents-only marker incorrect");
        Value(row, "date", true);
        Value(row, "amount", false);
        var gold = c.Gold.Events.SingleOrDefault(e => e.Kind == kind);
        Check(polarity == "absent" ? gold is null : gold is not null && gold.Polarity == polarity && (gold.Date ?? string.Empty) == S(row, "date") && (gold.Amount ?? string.Empty) == S(row, "amount"), "legacy event projection differs from dense key");
        Check(c.Gold.Forbidden.Any(f => f.Kind == kind) == (polarity != FactPolarity.Affirmed), "legacy forbidden projection differs");
    }

    foreach (var row in requests)
    {
        var kind = S(row, "kind");
        var present = row.GetProperty("present").GetBoolean();
        Check((Evidence(row, "evidence").Length > 0) == present, "request evidence/presence mismatch");
        Check(S(row, "amount").Length == 0 || present && claim.Requests.Single(r => r.Kind == kind).Amount, "amount on absent/non-amount request");
        Value(row, "amount", false);
        var gold = c.Gold.Requests.SingleOrDefault(r => r.Kind == kind);
        Check(present ? gold is not null && (gold.Amount ?? string.Empty) == S(row, "amount") : gold is null, "legacy request projection differs");
    }

    foreach (var row in annotation.GetProperty("valueNotes").EnumerateArray())
    {
        var ids = Evidence(row, "evidence");
        var raw = S(row, "value");
        Check(ids.Length > 0 && raw.Length > 0 && S(row, "purpose").Length > 0, "value note lacks context");
        Check(S(row, "kind") == "identifier"
            ? ids.Any(id => text.Find(id)?.Text.Contains(raw, StringComparison.Ordinal) == true)
            : FactValues.Check(raw, S(row, "kind") == "date", ids, text)?.Check == ValueCheck.InCitedSentence, "value note absent from cited source");
    }
}

Console.WriteLine($"Static check: {cases.Count} cases; {eventCount} dense event labels; {requestCount} dense request labels; {evidenceCount} citation checks; {problems.Count} problems.");
foreach (var problem in problems)
{
    Console.WriteLine(problem);
}

return problems.Count == 0 && cases.Count == 144 ? 0 : 1;

static string S(JsonElement element, string property) => element.GetProperty(property).GetString() ?? string.Empty;
