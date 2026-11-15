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
/// no person's name drawn for another scenario of the set (6). Each failure is a reason; a note with one is invalid.
/// Check 6 compares full names drawn by the sampler, not the cast lists' words: a given name alone is an ordinary word
/// often enough (and level 2 makes «على» read «علي»), so the lists themselves would raise false alarms.
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
    private static readonly HashSet<string> DarijaNormal = [.. Darija.Select(w => Normal(w))];

    private static readonly string[] Fields = ["subject", "facts", "requests", "documents"];
    private readonly HashSet<string> windows = new(StringComparer.Ordinal);
    private readonly HashSet<string> castNames;

    /// <param name="data">The sampler's data.</param>
    /// <param name="copiedFrom">The texts no 6-word window may be copied from (the guide, the prompt).</param>
    /// <param name="castNames">The full names of the people drawn for the set's scenarios (check 6).</param>
    public CodeChecks(SamplerData data, IEnumerable<string> copiedFrom, IEnumerable<string>? castNames = null)
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

        this.castNames = [.. (castNames ?? []).Select(n => string.Join(' ', NameWords(n))).Where(n => n.Contains(' ', StringComparison.Ordinal))];
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

        var foreign = Foreign(scenario, all);
        if (foreign.Count > 0)
        {
            reasons.Add("names: a person of another scenario: " + string.Join("، ", foreign));
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

    /// <summary>
    /// The full names of the set's other scenarios found in the note as whole words, except a name whose family name is
    /// one of this scenario's people's (a child named with a parent's family name is this scenario's own).
    /// </summary>
    private List<string> Foreign(JsonObject scenario, string text)
    {
        var own = new HashSet<string>(StringComparer.Ordinal);
        var families = new HashSet<string>(StringComparer.Ordinal);
        foreach (var party in new[] { "plaintiff", "defendant" })
        {
            if ((string?)scenario["cast"]?[party]?["name"] is string name)
            {
                var words = NameWords(name);
                own.Add(string.Join(' ', words));
                families.Add(words[^1]);
                if (words.Count > 2)
                {
                    families.Add(string.Join(' ', words.TakeLast(2)));
                }
            }
        }

        var padded = " " + string.Join(' ', NameWords(text)) + " ";
        return [.. castNames.Where(n => !own.Contains(n) && padded.Contains(" " + n + " ", StringComparison.Ordinal) &&
                                        !families.Any(f => n.EndsWith(" " + f, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>The words of a text after the level-2 normalisation of specs/article-store.md (letters only, no punctuation).</summary>
    public static List<string> Words(string text) =>
        [.. NotWord().Split(Normal(text)).Where(w => w.Length > 0)];

    /// <summary>The words for comparing names: level 2 but for alef maqsura, so that the preposition «على» is not «علي».</summary>
    private static List<string> NameWords(string text) =>
        [.. NotWord().Split(Normal(text, keepAlefMaqsura: true)).Where(w => w.Length > 0)];

    /// <summary>Level 2 of the text comparison: NFC, no tatweel or diacritics, one alef, ya for alef maqsura, ha for ta marbuta, Western digits.</summary>
    public static string Normal(string text, bool keepAlefMaqsura = false)
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
                'ى' when !keepAlefMaqsura => 'ي',
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
