using DzLegal.Drafting;

namespace DzLegal.Harness;

/// <summary>
/// Checks a set of fact cases written elsewhere (a hidden evaluation set, docs/HIDDEN-SET.md) before it is run: known
/// codes and claim types, values that are really written in the case, and answers the Alpha-01 questions can express.
/// It names the case and the problem only, never the case's text, so the set stays unseen.
/// </summary>
public static class FactCaseCheck
{
    public static IReadOnlyList<string> Problems(IReadOnlyList<FactCase> cases, IReadOnlyDictionary<string, ClaimElements> elements)
    {
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(elements);
        static string[] Codes(string value) => value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var problems = new List<string>();
        foreach (var name in cases.GroupBy(c => c.Name).Where(g => g.Count() > 1).Select(g => g.Key))
        {
            problems.Add($"{name}: name used more than once");
        }

        foreach (var c in cases)
        {
            void Add(string problem) => problems.Add($"{c.Name}: {problem}");
            var text = CaseText.From(c.Inputs);
            if (text.Sentences.Count == 0)
            {
                Add("no text in claim.subject, notes.facts, notes.requests or documents.list");
            }

            if (c.Partition.Length == 0)
            {
                Add("no partition");
            }

            if (c.Claim != FactCase.OutOfScope && !elements.ContainsKey(c.Claim))
            {
                Add($"unknown claim type '{c.Claim}'");
            }

            var g = c.Gold;
            foreach (var code in Codes(g.Relation).Where(r => !FactVocabulary.Relations.Contains(r)))
            {
                Add($"unknown relation '{code}'");
            }

            foreach (var code in Codes(g.Position).Where(p => !FactVocabulary.Positions.Contains(p)))
            {
                Add($"unknown position '{code}'");
            }

            foreach (var code in g.Events.SelectMany(e => Codes(e.Kind)).Concat(g.Forbidden.SelectMany(f => Codes(f.Kind))).Where(k => !FactVocabulary.Events.Contains(k)))
            {
                Add($"unknown event '{code}'");
            }

            foreach (var code in g.Requests.SelectMany(r => Codes(r.Kind)).Where(k => !FactVocabulary.Requests.Contains(k)))
            {
                Add($"unknown request '{code}'");
            }

            foreach (var e in g.Events.Where(e => !FactPolarity.All.Contains(e.Polarity)))
            {
                Add($"unknown polarity '{e.Polarity}'");
            }

            var dates = text.Sentences.SelectMany(s => FactValues.DatesIn(s.Text)).ToHashSet();
            var amounts = text.Sentences.SelectMany(s => FactValues.AmountsIn(s.Text)).ToHashSet();
            foreach (var d in g.Events.Select(e => e.Date).Concat(g.Forbidden.Select(f => f.Date)).OfType<string>().Where(d => !dates.Contains(FactValues.NormalizeDate(d))))
            {
                Add($"date {d} is not written in the case (dd/mm/yyyy or a year)");
            }

            foreach (var a in g.Events.Select(e => e.Amount).Concat(g.Requests.Select(r => r.Amount)).OfType<string>().Where(a => !amounts.Contains(a)))
            {
                Add($"amount {a} is not written in the case (digits only in the key)");
            }

            if (!elements.TryGetValue(c.Claim, out var claim))
            {
                continue;
            }

            // What the Alpha-01 questions of this claim type cannot express can never be read; the key is then wrong
            // for this claim type, or the claim type lacks a question.
            if (g.Position.Length > 0 && !claim.Roles.Any(r => Codes(g.Position).Contains(r.Position)))
            {
                Add($"position '{g.Position}' is not a role of {c.Claim}");
            }

            foreach (var e in g.Events)
            {
                var element = claim.Elements.FirstOrDefault(x => Codes(e.Kind).Contains(x.Event));
                if (element is null)
                {
                    Add($"event '{e.Kind}' is not asked for {c.Claim}");
                }
                else if ((e.Date is not null && !element.Date) || (e.Amount is not null && !element.Amount))
                {
                    Add($"event '{e.Kind}' carries a value its question for {c.Claim} does not take");
                }
            }

            foreach (var r in g.Requests.Where(r => !claim.Requests.Any(q => Codes(r.Kind).Contains(q.Kind))))
            {
                Add($"request '{r.Kind}' is not asked for {c.Claim}");
            }
        }

        return problems;
    }
}
