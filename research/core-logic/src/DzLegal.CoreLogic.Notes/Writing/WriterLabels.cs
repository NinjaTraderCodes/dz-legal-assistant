using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;

namespace DzLegal.CoreLogic.Notes.Writing;

/// <summary>
/// <c>writer-labels</c> and <c>build-spans</c> (specs/notes-pipeline.md, section 3): a written note's labels are the
/// scenario's labels plus the writer's quotes, field for field as the pipeline's table says; the writer chooses no label.
/// Every quote is then found in its field, letter for letter, its <c>occurrence</c>-th time, and gets its start and length.
/// </summary>
public static class WriterLabels
{
    public static readonly IReadOnlySet<string> DateSlots = new HashSet<string>(StringComparer.Ordinal)
    {
        "notice_date", "nonconciliation_record_delivered_on", "contract_date",
    };

    /// <summary>The writer's quotes by reference, with their spans; the problems of the quotes (code check 1).</summary>
    public sealed record Pointed(IReadOnlyDictionary<string, List<JsonObject>> Quotes, IReadOnlyList<string> Problems);

    public static Pointed Point(JsonObject scenario, JsonObject note)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(note);
        var problems = new List<string>();
        var quotes = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        var expected = WriteCommand.Refs(scenario).ToDictionary(r => r.Ref, r => r.Optional, StringComparer.Ordinal);
        foreach (var fact in note["facts_of_scenario"]?.AsArray() ?? [])
        {
            // The writer's view marks an optional fact «… (optional)»; a writer that copies the mark into its reference names
            // the same fact (four pilot notes of calls 4 to 8 did).
            var reference = ((string)fact!["ref"]!).Trim();
            if (reference.EndsWith(" (optional)", StringComparison.Ordinal))
            {
                reference = reference[..^" (optional)".Length];
            }

            if (!expected.TryGetValue(reference, out var optional))
            {
                problems.Add($"a reference the scenario does not have: {reference}");
                continue;
            }

            var list = fact["quotes"]?.AsArray() ?? [];
            if (!((bool?)fact["expressed"] ?? false))
            {
                if (!optional || list.Count > 0)
                {
                    problems.Add($"{reference} not expressed, and it is not an optional extra value");
                }

                quotes[reference] = [];
                continue;
            }

            if (list.Count is < 1 or > 3)
            {
                problems.Add($"{reference}: {list.Count} quotes (one to three)");
            }

            var found = new List<JsonObject>();
            foreach (var q in list)
            {
                var span = Span(note, (string)q!["field"]!, (string)q["quote"]!, (int?)q["occurrence"] ?? 1);
                if (span is null)
                {
                    problems.Add($"{reference}: quote not found in {q["field"]}: «{q["quote"]}»");
                }
                else
                {
                    found.Add(span);
                }
            }

            quotes[reference] = found;
        }

        foreach (var missing in expected.Keys.Where(r => !quotes.ContainsKey(r)))
        {
            problems.Add($"no entry for {missing}");
        }

        return new Pointed(quotes, problems);
    }

    /// <summary>A quote found in its field: field, quote, occurrence, start, length (UTF-16 offsets); null when not found.</summary>
    public static JsonObject? Span(JsonObject note, string field, string quote, int occurrence)
    {
        ArgumentNullException.ThrowIfNull(note);
        var text = (string?)note[field] ?? "";
        if (quote.Length == 0 || occurrence < 1)
        {
            return null;
        }

        // Occurrences are counted among the matches that stand on word edges, so that «200.000 دج» is not found inside
        // «1.200.000 دج»; a quote found only inside other words is taken as the writer gave it, by its raw occurrence.
        var all = new List<int>();
        for (var at = text.IndexOf(quote, StringComparison.Ordinal); at >= 0; at = text.IndexOf(quote, at + 1, StringComparison.Ordinal))
        {
            all.Add(at);
        }

        var whole = all.Where(at => Edge(text, at - 1, -1) && Edge(text, at + quote.Length, +1)).ToList();
        var pool = whole.Count > 0 ? whole : all;
        return occurrence > pool.Count
            ? null
            : new JsonObject { ["field"] = field, ["quote"] = quote, ["occurrence"] = occurrence, ["start"] = pool[occurrence - 1], ["length"] = quote.Length };
    }

    /// <summary>
    /// True when the character at <paramref name="i"/> does not continue a word or a number (or lies outside the text). A
    /// point or a comma continues a number only when a digit lies beyond it (step <paramref name="away"/>): «1.200.000».
    /// </summary>
    private static bool Edge(string text, int i, int away)
    {
        if (i < 0 || i >= text.Length)
        {
            return true;
        }

        var c = text[i];
        if (c is '.' or ',')
        {
            var beyond = i + away;
            return beyond < 0 || beyond >= text.Length || !char.IsDigit(text[beyond]);
        }

        return !(char.IsLetterOrDigit(c) || c == 'ـ' ||
                 char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark);
    }

    /// <summary>The labels of note.schema.json from the scenario and the quotes (the pipeline's table, field for field).</summary>
    public static JsonObject Labels(SamplerData data, JsonObject scenario, IReadOnlyDictionary<string, List<JsonObject>> quotes)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(quotes);
        JsonArray Q(string reference) => new([.. (quotes.GetValueOrDefault(reference) ?? []).Select(q => (JsonNode)q.DeepClone())]);
        JsonObject? First(string reference) => quotes.GetValueOrDefault(reference)?.FirstOrDefault();

        var claims = scenario["claims"]!.AsArray().Select(c => (string)c!).ToList();
        var values = Values(scenario, quotes);

        // An event dated «في اليوم نفسه» takes the date as it is written elsewhere in the note (guide, section 8).
        JsonObject DateAt(JsonObject quote, string normal) =>
            CodeChecks.IsSameDayReference((string)quote["quote"]!) &&
            values.FirstOrDefault(v => (string)v!["kind"]! == "date" && (string?)v["normalized"] == normal)?["at"] is JsonObject written
                ? written
                : quote;
        var labels = new JsonObject { ["claims"] = new JsonArray([.. claims.Select(c => (JsonNode)c)]) };
        if ((string?)scenario["outsideKind"] is string kind)
        {
            labels["outsideKind"] = kind;
        }

        labels["claimEvidence"] = Q("claim");
        var roles = new JsonArray();
        var events = new JsonArray();
        foreach (var claim in claims.Where(c => c != "outside"))
        {
            var def = data.ClaimById[claim];
            var role = scenario["roles"]?[claim];
            var only = def.Roles.Count == 1 ? def.Roles[0] : null;
            roles.Add(new JsonObject
            {
                ["claim"] = claim,
                ["relation"] = role is not null ? (string)role["relation"]! : only?.Relation ?? "unknown",
                ["position"] = role is not null ? (string)role["position"]! : only?.Position ?? "unknown",
                ["evidence"] = Q($"role:{claim}"),
            });
        }

        foreach (var e in scenario["events"]?.AsArray() ?? [])
        {
            var key = $"{e!["claim"]}/{e["kind"]}";
            var polarity = (string)e["polarity"]!;
            var ev = new JsonObject
            {
                ["claim"] = (string)e["claim"]!, ["kind"] = (string)e["kind"]!, ["polarity"] = polarity, ["actor"] = (string)e["actor"]!,
            };
            if (e["disputedBy"] is not null)
            {
                ev["disputedBy"] = (string)e["disputedBy"]!;
            }

            ev["evidence"] = polarity == "notMentioned" ? new JsonArray() : Q($"event:{key}");
            if (e["date"] is not null && First($"event-date:{key}") is JsonObject dq)
            {
                ev["date"] = Value("date", DateAt(dq, (string)e["date"]!), (string)e["date"]!, false);
            }

            if (e["amount"] is not null && First($"event-amount:{key}") is JsonObject aq)
            {
                ev["amount"] = Value("amount", aq, Digits(e["amount"]!), e["contest"]?["amount"] is not null);
            }

            events.Add(ev);
        }

        labels["roles"] = roles;
        labels["events"] = events;

        var slots = new JsonArray();
        foreach (var (slot, value) in scenario["slots"]?.AsObject() ?? [])
        {
            var v = (string)value!;
            slots.Add(new JsonObject { ["slot"] = slot, ["value"] = v, ["evidence"] = v == "unknown" ? new JsonArray() : Q($"slot:{slot}") });
        }

        labels["slots"] = slots;
        var requests = new JsonArray();
        foreach (var r in scenario["requests"]?.AsArray() ?? [])
        {
            var kind2 = (string)r!["kind"]!;
            var o = new JsonObject { ["kind"] = kind2, ["evidence"] = Q($"request:{kind2}") };
            if (r["amount"] is not null && First($"request-amount:{kind2}") is JsonObject rq)
            {
                o["amount"] = Value("amount", rq, Digits(r["amount"]!), false);
            }

            requests.Add(o);
        }

        labels["requests"] = requests;
        labels["values"] = values;
        return labels;
    }

    /// <summary>
    /// One entry per place a value is written: each quote of an event's date or amount, a request's amount, an extra value,
    /// or a date slot; quotes of the same words merge, their roles joined; a contest's figure is its own entry.
    /// </summary>
    private static JsonArray Values(JsonObject scenario, IReadOnlyDictionary<string, List<JsonObject>> quotes)
    {
        var entries = new List<(JsonObject At, string Kind, string Normal, SortedSet<string> Roles, bool Disputed)>();
        var sameDay = new List<(string Normal, string? Role)>();
        void Add(string reference, string kind, string normal, string? role, bool disputed)
        {
            foreach (var q in quotes.GetValueOrDefault(reference) ?? [])
            {
                // «في اليوم نفسه»: nothing new is added to the values; the date written once carries the role (guide, section 8).
                if (kind == "date" && CodeChecks.IsSameDayReference((string)q["quote"]!))
                {
                    sameDay.Add((normal, role));
                    continue;
                }

                var same = entries.FindIndex(x => (string)x.At["field"]! == (string)q["field"]! && (int)x.At["start"]! == (int)q["start"]! &&
                                                  (int)x.At["length"]! == (int)q["length"]!);
                if (same >= 0)
                {
                    if (role is not null)
                    {
                        entries[same].Roles.Add(role);
                    }

                    continue;
                }

                var roles = new SortedSet<string>(StringComparer.Ordinal);
                if (role is not null)
                {
                    roles.Add(role);
                }

                entries.Add((q, kind, normal, roles, disputed));
            }
        }

        foreach (var e in scenario["events"]?.AsArray() ?? [])
        {
            var key = $"{e!["claim"]}/{e["kind"]}";
            var kind = (string)e["kind"]!;
            var contested = e["contest"]?["amount"] is not null;
            if (e["date"] is not null)
            {
                Add($"event-date:{key}", "date", (string)e["date"]!, kind, false);
            }

            if (e["amount"] is not null)
            {
                Add($"event-amount:{key}", "amount", Digits(e["amount"]!), kind, contested);
            }

            if (contested)
            {
                Add($"contest-amount:{key}", "amount", Digits(e["contest"]!["amount"]!), kind, true);
            }
        }

        foreach (var r in scenario["requests"]?.AsArray() ?? [])
        {
            if (r!["amount"] is not null)
            {
                Add($"request-amount:{r["kind"]}", "amount", Digits(r["amount"]!), (string)r["kind"]!, false);
            }
        }

        foreach (var (slot, value) in scenario["slots"]?.AsObject() ?? [])
        {
            if (DateSlots.Contains(slot) && (string?)value is string v && v != "unknown")
            {
                Add($"slot:{slot}", "date", v, slot, false);
            }
        }

        foreach (var x in scenario["extraValues"]?.AsArray() ?? [])
        {
            var kind = (string)x!["kind"]!;
            Add($"value:{x["what"]}", kind, kind == "date" ? (string)x["value"]! : Digits(x["value"]!), null, false);
        }

        foreach (var (normal, role) in sameDay.Where(x => x.Role is not null))
        {
            foreach (var entry in entries.Where(x => x.Kind == "date" && x.Normal == normal))
            {
                entry.Roles.Add(role!);
            }
        }

        return new JsonArray([.. entries.Select(x =>
        {
            var o = Value(x.Kind, x.At, x.Normal, x.Disputed);
            o["roles"] = new JsonArray([.. x.Roles.Select(r => (JsonNode)r)]);
            return (JsonNode)o;
        })]);
    }

    private static JsonObject Value(string kind, JsonObject at, string normalized, bool disputed)
    {
        var o = new JsonObject { ["kind"] = kind, ["at"] = at.DeepClone(), ["normalized"] = normalized };
        if (disputed)
        {
            o["amountDisputed"] = true;
        }

        return o;
    }

    public static string Digits(JsonNode value) =>
        value.GetValueKind() == System.Text.Json.JsonValueKind.String ? (string)value! : ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
