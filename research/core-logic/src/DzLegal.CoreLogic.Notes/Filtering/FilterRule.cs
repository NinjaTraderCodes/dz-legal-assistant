using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes.Filtering;

/// <summary>What the five-step rule decides about one answered sheet.</summary>
public enum FilterVerdict
{
    /// <summary>Rule 1: the answer does not fit the sheet.</summary>
    Invalid,

    /// <summary>Rule 2: a decoy was answered as present.</summary>
    Unsure,

    /// <summary>Rule 3: a claim type of the scenario was answered "no".</summary>
    ClaimDisagreement,

    /// <summary>Rule 4: more than a fifth of the counted fields would be blanked.</summary>
    TooManyBlanks,
    Kept,
    KeptWithBlanks,
}

public sealed record FilterResult(FilterVerdict Verdict, IReadOnlyList<string> Blanked, int Counted, IReadOnlyDictionary<string, string> Answers);

/// <summary>The rule of specs/notes-pipeline.md, section 4a, steps 1 to 5 (the retries are the command's).</summary>
public static class FilterRule
{
    public static readonly IReadOnlySet<string> Silent = new HashSet<string>(StringComparer.Ordinal)
    {
        FilterSheet.NotMentioned, "unknown", FilterSheet.NoneOfThese,
    };

    public static FilterResult Apply(FilterSheet sheet, IReadOnlyList<string>? letters)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var answers = new Dictionary<string, string>(StringComparer.Ordinal);
        if (letters is null || letters.Count != sheet.Questions.Count)
        {
            return new FilterResult(FilterVerdict.Invalid, [], 0, answers);
        }

        var values = new List<string>();
        for (var i = 0; i < letters.Count; i++)
        {
            var option = sheet.Questions[i].Options.FirstOrDefault(o => o.Letter == letters[i]);
            if (option is null)
            {
                return new FilterResult(FilterVerdict.Invalid, [], 0, answers);
            }

            values.Add(option.Value);
        }

        var blanked = new List<string>();
        var counted = 0;
        var claimNo = false;
        for (var i = 0; i < values.Count; i++)
        {
            var q = sheet.Questions[i];
            var v = values[i];
            switch (q.Kind)
            {
                case "distractorClaim" or "decoyElement" or "decoyRequest" when !q.Expected.Contains(v):
                    return new FilterResult(FilterVerdict.Unsure, [], 0, answers);
                case "distractorClaim" or "decoyElement" or "decoyRequest":
                    continue;
                case "claim":
                    counted++;
                    claimNo |= v != "yes";
                    continue;
            }

            answers[q.Path] = v;
            if (q.Counted || !Silent.Contains(v))
            {
                counted++;
            }

            if (!q.Expected.Contains(v))
            {
                blanked.Add(q.Path);
            }
        }

        if (claimNo)
        {
            return new FilterResult(FilterVerdict.ClaimDisagreement, blanked, counted, answers);
        }

        if (counted > 0 && blanked.Count > counted * 0.2)
        {
            return new FilterResult(FilterVerdict.TooManyBlanks, blanked, counted, answers);
        }

        return new FilterResult(blanked.Count == 0 ? FilterVerdict.Kept : FilterVerdict.KeptWithBlanks, blanked, counted, answers);
    }

    /// <summary>
    /// The note's gold: the writer's labels less the blanked fields, as section 5 says: a blanked polarity is "blanked" and its
    /// actor, date and amount leave with it; a slot's value "blanked" without evidence; a role or a request left out; a value's
    /// roles emptied.
    /// </summary>
    public static JsonObject Gold(JsonObject writerLabels, IReadOnlyList<string> blanked)
    {
        ArgumentNullException.ThrowIfNull(writerLabels);
        ArgumentNullException.ThrowIfNull(blanked);
        var gold = writerLabels.DeepClone().AsObject();
        foreach (var path in blanked)
        {
            var parts = path.Split('/');
            switch (parts[0])
            {
                case "events":
                    foreach (var e in gold["events"]!.AsArray().Where(e => (string)e!["claim"]! == parts[1] && (string)e["kind"]! == parts[2]))
                    {
                        var o = e!.AsObject();
                        o["polarity"] = "blanked";
                        foreach (var key in new[] { "actor", "disputedBy", "date", "amount", "evidence" })
                        {
                            o.Remove(key);
                        }
                    }

                    break;
                case "slots":
                    foreach (var s in gold["slots"]!.AsArray().Where(s => (string)s!["slot"]! == parts[1]))
                    {
                        s!["value"] = "blanked";
                        s.AsObject().Remove("evidence");
                    }

                    break;
                case "roles":
                    Remove(gold["roles"]!.AsArray(), r => (string)r["claim"]! == parts[1]);
                    break;
                case "requests":
                    Remove(gold["requests"]!.AsArray(), r => (string)r["kind"]! == parts[1]);
                    break;
                case "values":
                    var at = parts[1].Split(':');
                    foreach (var v in gold["values"]!.AsArray().Where(v => (string)v!["at"]!["field"]! == at[0] &&
                                                                       ((int)v["at"]!["start"]!).ToString(System.Globalization.CultureInfo.InvariantCulture) == at[1]))
                    {
                        v!["roles"] = new JsonArray();
                    }

                    break;
            }
        }

        return gold;
    }

    private static void Remove(JsonArray array, Func<JsonNode, bool> match)
    {
        foreach (var item in array.Where(x => x is not null && match(x)).ToList())
        {
            array.Remove(item);
        }
    }
}
