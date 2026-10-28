using System.Collections.Immutable;
using System.Globalization;
using DzLegal.Drafting;

namespace DzLegal.Harness;

/// <summary>Versioned semantic audit. V1 gold is not exhaustive: unannotated additions are reported, not called false.</summary>
public static class StrictFactsEvaluator
{
    public const string Version = "facts.strict/1";

    public static ImmutableArray<FactError> Score(FactGold gold, CaseFacts facts)
    {
        ArgumentNullException.ThrowIfNull(gold);
        ArgumentNullException.ThrowIfNull(facts);
        var errors = ImmutableArray.CreateBuilder<FactError>();
        void Error(string type, string detail, bool critical = true) => errors.Add(new(type, detail, critical));
        static string[] Codes(string code) => code.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!Codes(gold.Relation).Contains(facts.Relation)) { Error("relation", facts.Relation); }
        if (!Codes(gold.Position).Contains(facts.Position)) { Error("plaintiff position", facts.Position); }

        // Matching uses kind and prediction order, never the gold date/amount/polarity to choose a convenient answer.
        var usedEvents = new HashSet<int>();
        foreach (var expected in gold.Events)
        {
            var index = Enumerable.Range(0, facts.Events.Count).FirstOrDefault(i => !usedEvents.Contains(i) && Codes(expected.Kind).Contains(facts.Events[i].Kind), -1);
            if (index < 0) { Error(expected.Decisive ? "missing decisive event" : "missing event", expected.Kind, expected.Decisive); continue; }
            usedEvents.Add(index);
            var actual = facts.Events[index];
            if (actual.Polarity != expected.Polarity) { Error("polarity", expected.Kind); }
            if (expected.Actor is "plaintiff" or "defendant" && actual.Actor != expected.Actor) { Error("actor", expected.Kind); }
            if (expected.Date is not null && actual.Date?.Normalized != FactValues.NormalizeDate(expected.Date)) { Error(actual.Date is null ? "missing date" : "date binding", expected.Kind, expected.Decisive); }
            if (expected.Amount is not null && actual.Amount?.Normalized != expected.Amount) { Error(actual.Amount is null ? "missing amount" : "amount binding", expected.Kind, expected.Decisive); }
        }

        foreach (var group in facts.Events.GroupBy(e => e.Kind))
        {
            var expectedCount = gold.Events.Count(e => Codes(e.Kind).Contains(group.Key));
            if (group.Count() > Math.Max(expectedCount, 1) && group.Select(e => (e.Polarity, e.Actor, e.Date?.Normalized, e.Amount?.Normalized)).Distinct().Count() > 1)
            {
                Error("ambiguous duplicate events", group.Key);
            }
        }

        foreach (var i in Enumerable.Range(0, facts.Events.Count).Where(i => !usedEvents.Contains(i))) { Error("unannotated event", facts.Events[i].Kind, critical: false); }
        foreach (var ev in facts.Events)
        {
            if (ev.Sentences.Count == 0) { Error("missing source", ev.Kind); }
            foreach (var forbidden in gold.Forbidden.Where(f => Codes(f.Kind).Contains(ev.Kind)))
            {
                if ((forbidden.Date is null && forbidden.Amount is null && ev.Polarity == FactPolarity.Affirmed)
                    || (forbidden.Date is not null && ev.Date?.Normalized == FactValues.NormalizeDate(forbidden.Date))
                    || (forbidden.Amount is not null && ev.Amount?.Normalized == forbidden.Amount)) { Error("forbidden reading", ev.Kind); }
            }
        }

        var usedRequests = new HashSet<int>();
        foreach (var expected in gold.Requests)
        {
            var index = Enumerable.Range(0, facts.Requests.Count).FirstOrDefault(i => !usedRequests.Contains(i) && Codes(expected.Kind).Contains(facts.Requests[i].Kind), -1);
            if (index < 0) { Error("missing request", expected.Kind); continue; }
            usedRequests.Add(index);
            if (expected.Amount is not null && facts.Requests[index].Amount?.Normalized != expected.Amount) { Error("request amount", expected.Kind); }
        }

        foreach (var i in Enumerable.Range(0, facts.Requests.Count).Where(i => !usedRequests.Contains(i))) { Error("unannotated request", facts.Requests[i].Kind, critical: false); }
        foreach (var request in facts.Requests.Where(r => r.Sentences.Count == 0)) { Error("missing source", request.Kind); }
        foreach (var value in facts.Values.Where(v => v.Check != ValueCheck.InCitedSentence)) { Error(value.Check == ValueCheck.NotInCase ? "invented value" : "wrong source", value.Text); }
        if (facts.Rejected.Count > 0) { Error("unreadable output", facts.Rejected.Count.ToString(CultureInfo.InvariantCulture)); }
        return errors.ToImmutable();
    }

    /// <summary>Full denominators; missing events cannot inflate the binding and polarity percentages.</summary>
    public static (int Correct, int Total) Dates(FactGold gold, CaseFacts facts) =>
        (gold.Events.Count(g => g.Date is not null && facts.Events.Count(e => g.Kind.Split('|').Contains(e.Kind) && e.Date?.Normalized == FactValues.NormalizeDate(g.Date)) == 1), gold.Events.Count(g => g.Date is not null));

    public static (int Correct, int Total) Polarities(FactGold gold, CaseFacts facts) =>
        (gold.Events.Count(g => facts.Events.Count(e => g.Kind.Split('|').Contains(e.Kind)) == 1 && facts.Events.Any(e => g.Kind.Split('|').Contains(e.Kind) && e.Polarity == g.Polarity)), gold.Events.Count);
}
