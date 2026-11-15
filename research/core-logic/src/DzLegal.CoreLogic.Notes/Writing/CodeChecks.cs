using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Reader.Values;

namespace DzLegal.CoreLogic.Notes.Writing;

/// <summary>
/// The code checks on every written note, before any model reads it (specs/notes-pipeline.md, section 3): quotes found
/// and every fact quoted (1); every value parses to the scenario's form, and the note holds no other value (2); formal
/// Arabic only (3); length within the style card's range (4); no 6-word window copied from the guide or the prompt (5);
/// no name of the cast lists but the scenario's own (6). Each failure is a reason; a note with one is invalid.
/// </summary>
public sealed partial class CodeChecks
{
    /// <summary>The Darija words of check 3, as whole words (the list is extended only before the method is frozen).</summary>
    public static readonly IReadOnlySet<string> Darija = new HashSet<string>(StringComparer.Ordinal)
    {
        "راني", "راه", "راهي", "كاين", "كاينة", "ماكانش", "مكانش", "بزاف", "واش", "علاش", "كيفاش", "شحال", "وين", "تاع", "نتاع",
        "ديال", "باش", "برك", "درك", "ضرك", "هاذ", "هاذي", "هاذو", "بصح", "خاطر", "قاع", "كيما", "نحب", "حاب", "خدام", "دراهم",
    };

    /// <summary>The list after the same normalisation as the note's words (ة → ه), so that «كاينة» is found.</summary>
    private static readonly HashSet<string> DarijaNormal = [.. Darija.Select(Normal)];

    private static readonly string[] Fields = ["subject", "facts", "requests", "documents"];
    private readonly HashSet<string> windows = new(StringComparer.Ordinal);
    private readonly HashSet<string> given;
    private readonly HashSet<string> family;

    public CodeChecks(SamplerData data, IEnumerable<string> copiedFrom)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(copiedFrom);
        foreach (var text in copiedFrom)
        {
            var words = Words(text);
            for (var i = 0; i + 6 <= words.Count; i++)
            {
                windows.Add(string.Join(' ', words.Skip(i).Take(6)));
            }
        }

        given = [.. SamplerData.Strings(data.Cast["maleGiven"]).Concat(SamplerData.Strings(data.Cast["femaleGiven"])).Select(Normal)];
        family = [.. SamplerData.Strings(data.Cast["family"]).Concat(SamplerData.Strings(data.Cast["maleGiven"]).Select(n => "بن " + n)).Select(Normal)];
    }

    public IReadOnlyList<string> Run(JsonObject scenario, JsonObject note, WriterLabels.Pointed pointed, StyleCard card)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(pointed);
        ArgumentNullException.ThrowIfNull(card);
        var reasons = new List<string>();
        reasons.AddRange(pointed.Problems.Select(p => "quotes: " + p));
        reasons.AddRange(Values(scenario, note, pointed));
        var all = string.Join("\n", Fields.Select(f => (string?)note[f] ?? ""));
        if (LatinLetter().IsMatch(all))
        {
            reasons.Add("arabic: a Latin letter");
        }

        var darija = Words(all).Where(w => DarijaNormal.Contains(w)).Distinct(StringComparer.Ordinal).ToList();
        if (darija.Count > 0)
        {
            reasons.Add("arabic: Darija words " + string.Join("، ", darija));
        }

        var count = all.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var (min, max) = StyleCards.LengthWords[card.Parameters["length"]];
        if (count < min || count > max)
        {
            reasons.Add($"length: {count} words, the card asks {min} to {max}");
        }

        foreach (var field in Fields)
        {
            var words = Words((string?)note[field] ?? "");
            for (var i = 0; i + 6 <= words.Count; i++)
            {
                var window = string.Join(' ', words.Skip(i).Take(6));
                if (windows.Contains(window))
                {
                    reasons.Add($"copied: «{window}» is in the guide or the prompt");
                    break;
                }
            }
        }

        var own = OwnNames(scenario);
        var foreign = Names(all).Where(n => !own.Contains(n)).Distinct(StringComparer.Ordinal).ToList();
        if (foreign.Count > 0)
        {
            reasons.Add("names: a cast name not of this scenario: " + string.Join("، ", foreign));
        }

        return reasons;
    }

    /// <summary>Check 2: each quoted value parses to the scenario's form; the parsers find no value the scenario does not hold.</summary>
    private static List<string> Values(JsonObject scenario, JsonObject note, WriterLabels.Pointed pointed)
    {
        var dates = new HashSet<string>(StringComparer.Ordinal);
        var amounts = new HashSet<string>(StringComparer.Ordinal);
        var found = new List<string>();
        void Expect(string reference, string kind, string normal)
        {
            (kind == "date" ? dates : amounts).Add(normal);
            foreach (var q in pointed.Quotes.GetValueOrDefault(reference) ?? [])
            {
                var field = Enum.Parse<NoteField>((string)q["field"]!, ignoreCase: true);
                var read = kind == "date" ? DateParser.Read((string)q["quote"]!, field) : AmountParser.Read((string)q["quote"]!, field);
                if (!read.Any(r => r.Normalized == normal))
                {
                    found.Add($"values: {reference}: «{q["quote"]}» does not read as {normal}");
                }
            }
        }

        foreach (var e in scenario["events"]?.AsArray() ?? [])
        {
            var key = $"{e!["claim"]}/{e["kind"]}";
            if (e["date"] is not null)
            {
                Expect($"event-date:{key}", "date", (string)e["date"]!);
            }

            if (e["amount"] is not null)
            {
                Expect($"event-amount:{key}", "amount", WriterLabels.Digits(e["amount"]!));
            }

            if (e["contest"]?["amount"] is JsonNode c)
            {
                Expect($"contest-amount:{key}", "amount", WriterLabels.Digits(c));
            }
        }

        foreach (var r in scenario["requests"]?.AsArray() ?? [])
        {
            if (r!["amount"] is not null)
            {
                Expect($"request-amount:{r["kind"]}", "amount", WriterLabels.Digits(r["amount"]!));
            }
        }

        foreach (var (slot, value) in scenario["slots"]?.AsObject() ?? [])
        {
            if (WriterLabels.DateSlots.Contains(slot) && (string?)value is string v && v != "unknown")
            {
                Expect($"slot:{slot}", "date", v);
            }
        }

        foreach (var x in scenario["extraValues"]?.AsArray() ?? [])
        {
            var kind = (string)x!["kind"]!;
            Expect($"value:{x["what"]}", kind, kind == "date" ? (string)x["value"]! : WriterLabels.Digits(x["value"]!));
        }

        foreach (var field in Fields)
        {
            var text = (string?)note[field] ?? "";
            var f = Enum.Parse<NoteField>(field, ignoreCase: true);
            foreach (var r in DateParser.Read(text, f).Where(r => !dates.Contains(r.Normalized)))
            {
                found.Add($"values: a date the scenario does not hold: {r.Normalized} in {field}");
            }

            foreach (var r in AmountParser.Read(text, f).Where(r => !amounts.Contains(r.Normalized)))
            {
                found.Add($"values: an amount the scenario does not hold: {r.Normalized} in {field}");
            }
        }

        return found;
    }

    private static HashSet<string> OwnNames(JsonObject scenario)
    {
        var own = new HashSet<string>(StringComparer.Ordinal);
        foreach (var party in new[] { "plaintiff", "defendant" })
        {
            if ((string?)scenario["cast"]?[party]?["name"] is string name)
            {
                own.Add(Normal(name));
            }
        }

        return own;
    }

    /// <summary>Full names of the cast lists in the text: a given name (one to three words) then a family name (one or two).</summary>
    private List<string> Names(string text)
    {
        var words = Words(text);
        var found = new List<string>();
        for (var i = 0; i < words.Count; i++)
        {
            for (var g = 1; g <= 3 && i + g < words.Count; g++)
            {
                if (!given.Contains(string.Join(' ', words.Skip(i).Take(g))))
                {
                    continue;
                }

                for (var f = 1; f <= 2 && i + g + f <= words.Count; f++)
                {
                    var last = string.Join(' ', words.Skip(i + g).Take(f));
                    if (family.Contains(last))
                    {
                        found.Add(string.Join(' ', words.Skip(i).Take(g)) + " " + last);
                    }
                }
            }
        }

        return found;
    }

    /// <summary>The words of a text after the level-2 normalisation of specs/article-store.md (letters only, no punctuation).</summary>
    public static List<string> Words(string text) =>
        [.. NotWord().Split(Normal(text)).Where(w => w.Length > 0)];

    /// <summary>Level 2 of the text comparison: NFC, no tatweel or diacritics, one alef, ya for alef maqsura, ha for ta marbuta, Western digits.</summary>
    public static string Normal(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var s = new StringBuilder(text.Normalize(NormalizationForm.FormC).Length);
        foreach (var ch in text.Normalize(NormalizationForm.FormC))
        {
            if (ch == 'ـ' || (ch >= 'ً' && ch <= 'ْ') || ch == 'ٰ')
            {
                continue;
            }

            s.Append(ch switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                _ => ch,
            });
        }

        return s.ToString();
    }

    [GeneratedRegex("[A-Za-z]")]
    private static partial Regex LatinLetter();

    [GeneratedRegex(@"[^\p{L}\p{Nd}]+")]
    private static partial Regex NotWord();
}
