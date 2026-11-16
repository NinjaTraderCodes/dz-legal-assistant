using System.Text;
using System.Text.Json;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Reader.Cues;

/// <summary>One kind's triggers (cues.json).</summary>
public sealed record KindCues(IReadOnlyList<string> Triggers, bool Negative = false, IReadOnlyList<string>? AbstainWhen = null);

/// <summary>A vote of the cue table on one element: a polarity and the words it rests on.</summary>
public sealed record CueVote(Polarity Polarity, TextSpan Span);

/// <summary>
/// The polarity cues of rung 2 (specs/reader.md): a table of trigger words per event kind and of negation, restriction,
/// dispute, refusal, "despite", future and doubt words, applied clause by clause. It votes only when the cues are plain and
/// gives no vote otherwise: it must be independent of the learned model, never complete. A vote:
/// <list type="bullet">
/// <item>a dispute verb before the trigger in its clause: disputed («ينكر المدعى عليه أنه تسلم المبلغ»); a negated dispute
/// verb («لا ينكر», «لم ينازع») admits: stated;</item>
/// <item>a negation particle before the trigger: denied, unless a restriction (إلا، سوى) follows: stated; a kind whose
/// proposition is negative is never voted denied;</item>
/// <item>a refusal, "despite", future, doubt or question word in the clause, or a word the kind asks to abstain on: no vote;</item>
/// <item>otherwise: stated.</item>
/// </list>
/// </summary>
public sealed class CueEngine
{
    private static readonly string[] Negations = ["لم", "لا", "لن", "ليس", "ليست", "غير", "عدم", "دون", "بدون", "بلا"];
    private static readonly string[] NotNegations = ["ما زال", "ما يزال", "لا يزال", "لا تزال", "ما زالت", "ما تزال"];
    private static readonly string[] Restrictions = ["الا", "سوي"];
    private static readonly string[] Disputes =
    [
        "ينكر", "تنكر", "ينفي", "تنفي", "ينازع", "تنازع", "يدعي", "تدعي", "يزعم", "تزعم", "يتمسك", "تتمسك", "يطعن", "تطعن",
        "يعترض", "تعترض", "يدفع ب", "تدفع ب",
    ];
    private static readonly string[] Refusals = ["امتنع", "امتناع", "رفض", "تخلف", "تقاعس", "احجم", "اغفل", "اهمل"];
    private static readonly string[] Despite = ["رغم", "بالرغم", "مع ان"];
    private static readonly string[] Doubt = ["لا يعلم", "لا تعلم", "لا يدري", "يجهل", "ان كان", "ما اذا", "هل ", "يتحقق", "لا يعرف", "غير معروف", "سوف", "يعتزم", "تعتزم", "ينوي", "تنوي"];
    private static readonly string[] FutureStop = ["سيارة", "سيدة", "ستة", "سنة", "سنوات", "سند", "ستمائة", "سنتيم", "ستين", "ستون", "سيما", "سير", "سيولة", "ستار"];

    private readonly IReadOnlyDictionary<string, KindCues> kinds;

    public CueEngine(IReadOnlyDictionary<string, KindCues> kinds) => this.kinds = kinds;

    /// <summary>The table shipped beside the reader (src/DzLegal.CoreLogic.Reader/cues.json).</summary>
    public static CueEngine Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var kinds = new Dictionary<string, KindCues>(StringComparer.Ordinal);
        foreach (var kind in doc.RootElement.GetProperty("kinds").EnumerateObject())
        {
            var value = kind.Value;
            kinds[kind.Name] = new KindCues(
                [.. value.GetProperty("triggers").EnumerateArray().Select(t => Normalize(t.GetString()!))],
                value.TryGetProperty("negative", out var negative) && negative.GetBoolean(),
                value.TryGetProperty("abstainWhen", out var abstain) ? [.. abstain.EnumerateArray().Select(t => Normalize(t.GetString()!))] : null);
        }

        return new CueEngine(kinds);
    }

    public IReadOnlyCollection<string> Kinds => [.. kinds.Keys];

    /// <summary>
    /// Every place a kind's trigger words occur in a text, as spans of the original text (for linking a date or an amount to
    /// the event whose trigger is nearest in its sentence: the route experiment of 17 November 2026). No polarity is read.
    /// </summary>
    public IReadOnlyList<(string Kind, TextSpan Span)> Triggers(string text, NoteField field)
    {
        ArgumentNullException.ThrowIfNull(text);
        var (normal, original) = Map(text);
        var found = new List<(string Kind, TextSpan Span)>();
        foreach (var (kind, cues) in kinds)
        {
            foreach (var trigger in cues.Triggers.Where(t => t.Length > 0))
            {
                for (var i = normal.IndexOf(trigger, StringComparison.Ordinal); i >= 0; i = normal.IndexOf(trigger, i + 1, StringComparison.Ordinal))
                {
                    var first = original[i];
                    var last = original[i + trigger.Length - 1];
                    found.Add((kind, new TextSpan(field, first, last - first + 1)));
                }
            }
        }

        return [.. found.OrderBy(f => f.Span.Start).ThenBy(f => f.Kind, StringComparer.Ordinal)];
    }

    /// <summary>The vote on one event kind in one field of a note, or null when the cues are not plain.</summary>
    public CueVote? Vote(string text, string kind, NoteField field = NoteField.Facts)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!kinds.TryGetValue(kind, out var cues))
        {
            return null;
        }

        var (normal, original) = Map(text);
        if (cues.AbstainWhen is { } words && words.Any(word => normal.Contains(word, StringComparison.Ordinal)))
        {
            return null;
        }

        CueVote? vote = null;
        foreach (var clause in Clauses(normal))
        {
            var found = Find(clause.Text, cues.Triggers);
            if (found is { } at)
            {
                var polarity = Decide(clause.Text, at.Index, at.Length, cues);
                if (polarity is null)
                {
                    return null;  // a clause that is not plain: no vote for the note
                }

                var first = original[clause.Start + at.Index];
                var last = original[clause.Start + at.Index + at.Length - 1];
                var span = new TextSpan(field, first, last - first + 1);
                if (vote is not null && vote.Polarity != polarity)
                {
                    return null;  // two clauses that disagree: no vote
                }

                vote ??= new CueVote(polarity.Value, span);
            }
        }

        return vote;
    }

    private static Polarity? Decide(string clause, int at, int length, KindCues cues)
    {
        var before = clause[..at];
        var after = clause[(at + length)..];
        var trigger = clause.Substring(at, length);
        if (clause.Contains('؟') || clause.Contains('?') || Doubt.Any(word => clause.Contains(word, StringComparison.Ordinal))
            || Despite.Any(word => clause.Contains(word, StringComparison.Ordinal)) || Future(clause))
        {
            return null;
        }

        if (!Refusals.Any(word => trigger.Contains(word, StringComparison.Ordinal))
            && Refusals.Any(word => Words(before).Concat(Words(after)).Any(w => w.StartsWith(word, StringComparison.Ordinal))))
        {
            return null;
        }

        var dispute = Disputes.Select(word => before.IndexOf(word, StringComparison.Ordinal)).Where(i => i >= 0).DefaultIfEmpty(-1).Min();
        if (dispute >= 0)
        {
            var lead = Words(before[..dispute]).LastOrDefault();
            return lead is not null && Negations.Contains(lead, StringComparer.Ordinal) ? Polarity.Stated : Polarity.Disputed;
        }

        var cleaned = NotNegations.Aggregate(before, (text, phrase) => text.Replace(phrase, " ", StringComparison.Ordinal));
        var negated = Words(cleaned).Any(word => Negations.Contains(word, StringComparer.Ordinal));
        if (!negated || cues.Negative)
        {
            return Polarity.Stated;
        }

        return Words(after).Any(word => Restrictions.Contains(word, StringComparer.Ordinal)) ? Polarity.Stated : Polarity.Denied;
    }

    /// <summary>A verb in the future («سيوجه», «ستدفع»), never a word that only begins like one.</summary>
    private static bool Future(string clause) =>
        Words(clause).Any(word => word.Length >= 5 && word[0] == 'س' && word[1] is 'ي' or 'ت' or 'ن'
            && !FutureStop.Any(stop => word.StartsWith(stop, StringComparison.Ordinal)));

    private static (int Index, int Length)? Find(string clause, IReadOnlyList<string> triggers)
    {
        (int, int)? best = null;
        foreach (var trigger in triggers)
        {
            var i = clause.IndexOf(trigger, StringComparison.Ordinal);
            if (i >= 0 && (best is null || i < best.Value.Item1))
            {
                best = (i, trigger.Length);
            }
        }

        return best;
    }

    private static IEnumerable<string> Words(string text) =>
        text.Split([' ', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.StartsWith('و') && word.Length > 2 && Negations.Contains(word[1..], StringComparer.Ordinal) ? word[1..] : word);

    private static IEnumerable<(string Text, int Start)> Clauses(string text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '،' or ',' or '.' or '؛' or ';' or '!' or '\n')
            {
                yield return (text[start..i], start);
                start = i + 1;
            }
        }

        yield return (text[start..], start);
    }

    /// <summary>Normalised text: no diacritics or tatweel; أ إ آ read as ا; ى as ي.</summary>
    public static string Normalize(string text) => Map(text).Text;

    /// <summary>The normalised text and, for each of its units, the offset of that unit in the original.</summary>
    public static (string Text, int[] Original) Map(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var b = new StringBuilder(text.Length);
        var map = new List<int>(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is >= 'ً' and <= 'ْ' or 'ـ')  // the diacritics and the tatweel
            {
                continue;
            }

            b.Append(c switch { 'أ' or 'إ' or 'آ' => 'ا', 'ى' => 'ي', _ => c });
            map.Add(i);
        }

        return (b.ToString(), [.. map]);
    }
}
