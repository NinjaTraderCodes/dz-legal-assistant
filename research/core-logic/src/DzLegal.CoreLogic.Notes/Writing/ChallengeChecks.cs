using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Reader.Cues;
using DzLegal.CoreLogic.Reader.Values;

namespace DzLegal.CoreLogic.Notes.Writing;

/// <summary>
/// The code check of a challenge note's difficulty (specs/scenario-sampler.md, "The challenge set's recipes", last column).
/// A failure is a reason «challenge: &lt;id&gt;: …», and the note is invalid like any other (rewritten once). The words come from
/// the reader's cue table (<see cref="CueEngine"/>) and the values from its parsers; the only word added is «غير» as a
/// restriction, which the recipe names.
/// </summary>
public static partial class ChallengeChecks
{
    private static readonly string[] Fields = ["subject", "facts", "requests", "documents"];

    public static IReadOnlyList<string> Run(JsonObject scenario, JsonObject note, WriterLabels.Pointed pointed)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(pointed);
        if ((string?)scenario["difficulty"] is not string id)
        {
            return [];
        }

        var recipe = ChallengeRecipes.Get(id);
        var target = (string?)scenario["target"] ?? throw new InvalidDataException($"{scenario["id"]}: a {id} scenario without its target event");
        var reference = "event:" + target;
        var sentences = Sentences(note, pointed.Quotes.GetValueOrDefault(reference) ?? []);
        string Fail(string what) => $"challenge: {recipe.Id}: {what}";
        switch (recipe.Id)
        {
            case "restriction":
                return sentences.Any(Restricted) ? [] : [Fail($"no «إلا», «سوى» or «غير» after a negation particle in the sentence of {reference}")];

            case "doubleNegation":
                return sentences.Any(s => NegationCount(s) >= 2) ? [] : [Fail($"fewer than two negation cues of the cue table in the sentence of {reference}")];

            case "contestOfAmount":
            {
                var e = ScenarioSampler.Event(scenario, target);
                var read = Fields.SelectMany(f => AmountParser.Read((string?)note[f] ?? "", Enum.Parse<NoteField>(f, ignoreCase: true)))
                    .Select(r => r.Normalized).ToHashSet(StringComparer.Ordinal);
                var own = WriterLabels.Digits(e["amount"]!);
                var other = WriterLabels.Digits(e["contest"]!["amount"]!);
                return [.. new[] { (own, "the event's figure"), (other, "the other side's figure") }
                    .Where(x => !read.Contains(x.Item1)).Select(x => Fail($"the parsers do not find {x.Item1} ({x.Item2}) in the note"))];
            }

            case "documentAgainstContest":
            {
                var reasons = new List<string>();
                if (string.IsNullOrWhiteSpace((string?)note["documents"]))
                {
                    reasons.Add(Fail("the documents field is empty"));
                }

                if (sentences.Count == 0)
                {
                    reasons.Add(Fail($"no quote for the contest of {reference}"));
                }

                return reasons;
            }

            default:
                throw new ArgumentException($"difficulty {recipe.Id} has no code check; the implemented ones are {ChallengeRecipes.Ids}");
        }
    }

    /// <summary>
    /// The sentence around each quote, from the last sentence end before it to the first one after it (a point that is not
    /// inside a number, «!», «?», «؟», «؛», «;», a line end).
    /// </summary>
    public static IReadOnlyList<string> Sentences(JsonObject note, IEnumerable<JsonObject> quotes)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(quotes);
        var found = new List<string>();
        foreach (var q in quotes)
        {
            var text = (string?)note[(string)q["field"]!] ?? "";
            var start = (int)q["start"]!;
            var end = start + (int)q["length"]!;
            var from = start;
            while (from > 0 && !End(text, from - 1))
            {
                from--;
            }

            var to = end;
            while (to < text.Length && !End(text, to))
            {
                to++;
            }

            found.Add(text[from..to]);
        }

        return found;
    }

    private static bool End(string text, int i) => text[i] switch
    {
        '.' => !(i > 0 && char.IsDigit(text[i - 1]) && i + 1 < text.Length && char.IsDigit(text[i + 1])),
        '!' or '?' or '؟' or '؛' or ';' or '\n' => true,
        _ => false,
    };

    /// <summary>The words of a sentence, normalised as the cue table is, a conjunction «و» or «ف» taken off a negation particle.</summary>
    private static List<string> Words(string sentence)
    {
        var normal = CueEngine.NotNegationCues.Aggregate(CueEngine.Normalize(sentence), (t, phrase) => t.Replace(phrase, " ", StringComparison.Ordinal));
        return [.. NotWord().Split(normal).Where(w => w.Length > 0)
            .Select(w => w.Length > 2 && w[0] is 'و' or 'ف' && CueEngine.NegationCues.Contains(w[1..], StringComparer.Ordinal) ? w[1..] : w)];
    }

    /// <summary>«إلا», «سوى» or «غير» after a negation particle, in the same sentence.</summary>
    public static bool Restricted(string sentence)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        var words = Words(sentence);
        var negation = words.FindIndex(w => CueEngine.NegationCues.Contains(w, StringComparer.Ordinal));
        return negation >= 0 && words.Skip(negation + 1).Any(w => CueEngine.RestrictionCues.Contains(w, StringComparer.Ordinal) || w == "غير");
    }

    /// <summary>
    /// The negation cues of the cue table in a sentence: each negation particle, and each verb of dispute («ينكر», «ينفي» …)
    /// or of refusal («امتنع», «رفض» …), a refusal verb also in its present («يمتنع», «يرفض»: a prefix, the perfect without
    /// its first alef).
    /// </summary>
    public static int NegationCount(string sentence)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        var disputes = CueEngine.DisputeCues.Where(d => !d.Contains(' ', StringComparison.Ordinal)).ToList();
        bool Refusal(string w) => CueEngine.RefusalCues.Any(r => w.StartsWith(r, StringComparison.Ordinal) ||
            (w.Length > 1 && w[0] is 'ي' or 'ت' or 'ن' && w[1..].StartsWith(r[0] == 'ا' ? r[1..] : r, StringComparison.Ordinal)));
        return Words(sentence).Count(w => CueEngine.NegationCues.Contains(w, StringComparer.Ordinal) || Refusal(w) ||
                                          disputes.Any(d => w.StartsWith(d, StringComparison.Ordinal)));
    }

    [GeneratedRegex(@"[^\p{L}\p{Nd}]+")]
    private static partial Regex NotWord();
}
