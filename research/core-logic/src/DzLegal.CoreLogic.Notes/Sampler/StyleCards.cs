using System.Globalization;
using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes.Sampler;

/// <summary>A style card (specs/scenario-sampler.md, "Style cards"): how a note is written, never what it says.</summary>
public sealed record StyleCard(string Id, IReadOnlyDictionary<string, string> Parameters, string Text);

/// <summary>
/// The style cards, in two generations. **Version 1** (c-01 to c-40, 15 November 2026): each parameter's values spread
/// evenly over the 40 cards and shuffled by a fixed seed, so that no two parameters move together; the English paragraph is
/// the parameters' sentences. **Version 2** (s2-01 to s2-80, 17 November 2026, the owner: "widen the style variety ...
/// shorter and messier notes"): the same parameters with the lengths weighted towards short, and two more: how messy the
/// note is, and no set formula for the parties (the writer-identity probe found Codex's «شخصان طبيعيان»). A version-1 card
/// keeps its text; it takes the new parameters' neutral values. Written by <c>notes style-cards --write</c> into
/// notes/sampler/style-cards.json; the sampler draws from every card.
/// </summary>
public static class StyleCards
{
    public const int CountV1 = 40;
    public const int CountV2 = 80;
    public const int Count = CountV1 + CountV2;

    /// <summary>The words-over-four-fields range of each length (notes-pipeline.md, code check 4).</summary>
    public static readonly IReadOnlyDictionary<string, (int Min, int Max)> LengthWords = new Dictionary<string, (int, int)>
    {
        ["veryShort"] = (15, 60), ["short"] = (60, 150), ["medium"] = (150, 300), ["long"] = (300, 500),
    };

    /// <summary>Each parameter, its values and the sentence each value adds to the card.</summary>
    public static readonly IReadOnlyList<(string Name, IReadOnlyList<(string Value, string Sentence)> Values)> Parameters =
    [
        ("length", [
            ("veryShort", "Very short: two to four lines, 15 to 60 words in all."),
            ("short", "Short: 60 to 150 words in all."),
            ("medium", "Medium: 150 to 300 words in all."),
            ("long", "Long, about a page: 300 to 500 words in all."),
        ]),
        ("order", [
            ("chronological", "Tell the facts in the order they happened."),
            ("requestsFirst", "Start with what the client wants, then the facts."),
            ("problemFirst", "Start with the problem as it stands today, then go back over the history."),
            ("documentsFirst", "Start from the papers the client brought, then the facts they show."),
        ]),
        ("tidiness", [
            ("drafted", "Write in full, drafted sentences."),
            ("dashes", "Write telegraphic notes, one short point per line, each starting with a dash."),
            ("numbered", "Write numbered points."),
            ("oneBlock", "Write one block of text without paragraphs."),
        ]),
        ("fields", [
            ("factsOnly", "Put everything in \"facts\"; leave \"subject\", \"requests\" and \"documents\" empty."),
            ("factsRequests", "Use \"facts\" and \"requests\"; leave \"subject\" and \"documents\" empty."),
            ("allFour", "Use all four fields; \"documents\" as a list of the papers."),
            ("noSubject", "Use \"facts\", \"requests\" and \"documents\"; leave \"subject\" empty."),
            ("noDocuments", "Use \"subject\", \"facts\" and \"requests\"; leave \"documents\" empty."),
        ]),
        ("voice", [
            ("thirdPerson", "Speak of the parties in the third person (the plaintiff, the defendant)."),
            ("clientByName", "Speak of the client by name."),
            ("officePlural", "Write as the office, in the first person plural (we ask, we note)."),
            ("nominal", "Use nominal fragments rather than full verbs where you can."),
        ]),
        ("dates", [
            ("dmySlash", "Write dates as day/month/year in digits (12/03/2025)."),
            ("ymdSlash", "Write dates year first, in digits (2025/03/12)."),
            ("dayMonthName", "Write dates with the month's name (12 مارس 2025)."),
            ("longForm", "Write dates in the long form (في 12 من شهر مارس 2025)."),
            ("arabicIndic", "Write the digits of dates and sums in Arabic-Indic digits (١٢/٠٣/٢٠٢٥)."),
            ("algerianMonths", "Use the Algerian month names (جانفي، فيفري، مارس، أفريل …)."),
            ("mixed", "Mix two ways of writing dates in the same note."),
        ]),
        ("amounts", [
            ("points", "Write sums with points between thousands and the abbreviation of dinars (500.000 دج)."),
            ("spaces", "Write sums with spaces between thousands and the word dinar (500 000 دينار)."),
            ("wordsAndDigits", "Write at least one sum in words with the digits beside it."),
            ("centimes", "Where natural, give a sum in centimes as people say it (خمسون مليون سنتيم)."),
            ("arabicIndicAmounts", "Write the digits of sums in Arabic-Indic digits."),
        ]),
        ("negation", [
            ("lam", "When something did not happen, say it with «لم» and a verb."),
            ("adam", "When something did not happen, say it with «عدم» and a noun."),
            ("dun", "When something did not happen, prefer «دون» or «بدون»."),
            ("laWujud", "When something does not exist, prefer «لا وجود لـ»."),
            ("restriction", "Where it fits, say what happened by restriction («لم … إلا», «سوى»)."),
            ("refusalVerbs", "Prefer verbs of refusal (امتنع، رفض، تخلف، تقاعس)."),
        ]),
        ("dispute", [
            ("yunkir", "When the other side contests something, prefer «ينكر»."),
            ("yunazi", "When the other side contests something, prefer «ينازع في»."),
            ("yaddai", "When the other side tells its own version, prefer «يدعي» or «يزعم»."),
            ("yatamassak", "When the other side relies on its own version, prefer «يتمسك بـ» or «يدفع بـ»."),
            ("reported", "Report the other side's version in the plaintiff's voice («حسب المدعى عليه …»)."),
        ]),
        ("extras", [
            ("correction", "Correct yourself once in the middle of the note, without changing any date or sum."),
            ("irrelevant", "Add one detail that does not matter to the case."),
            ("selfQuestion", "Note one question for yourself about the file."),
            ("reminder", "Add one reminder of a paper to ask the client for."),
            ("none", "No extras."),
        ]),
        ("terms", [
            ("usual", "Name each act with the words lawyers usually use for it."),
            ("synonym", "Name each act with the concept the scenario gives for it, in your own Arabic, never with the usual set phrase."),
        ]),
    ];

    /// <summary>The parameters version 2 adds, with the neutral value a version-1 card takes.</summary>
    public static readonly IReadOnlyList<(string Name, string Neutral, IReadOnlyList<(string Value, string Sentence)> Values)> Added =
    [
        ("mess", "clean", [
            ("clean", "Keep the note tidy."),
            ("slips", "Leave two or three small typing slips a hurried lawyer makes (a missing hamza, ة written as ه, a doubled letter), never in a name, a date or a sum."),
            ("fragments", "Leave some points as bare fragments, without a verb, as hurried notes are."),
            ("runOn", "Run several facts together in long sentences joined by و and ثم, with little punctuation."),
            ("shorthand", "Write in shorthand: drop linking words and articles wherever the meaning stays clear."),
        ]),
        ("setPhrases", "free", [
            ("free", "Describe the parties as you like."),
            ("noFormula", "Never describe the parties with a set formula (such as «شخصان طبيعيان» or «فردان»); let their names, trades or roles show who they are."),
        ]),
    ];

    /// <summary>Version 2's lengths: very short and short on five cards in eight, medium on two, long on one.</summary>
    public static readonly IReadOnlyList<string> LengthsV2 = ["veryShort", "veryShort", "short", "short", "short", "medium", "medium", "long"];

    /// <summary>Every parameter name a card carries.</summary>
    public static IReadOnlyList<string> Names => [.. Parameters.Select(p => p.Name), .. Added.Select(a => a.Name)];

    /// <summary>The 120 cards: version 1 (as built on 15 November) and version 2, by the fixed seed.</summary>
    public static IReadOnlyList<StyleCard> Build(int seed = 20261115)
    {
        var v1 = Generation(seed, Parameters, CountV1, "c-", 0);
        var cards = v1.Select(c =>
        {
            var parameters = new Dictionary<string, string>(c.Parameters, StringComparer.Ordinal);
            foreach (var (name, neutral, _) in Added)
            {
                parameters[name] = neutral;
            }

            return c with { Parameters = parameters };
        }).ToList();
        var lengths = Parameters.First(p => p.Name == "length").Values.ToDictionary(v => v.Value, v => v.Sentence, StringComparer.Ordinal);
        var v2 = Parameters.Select(p => p.Name == "length"
                ? (p.Name, (IReadOnlyList<(string Value, string Sentence)>)[.. LengthsV2.Select(l => (l, lengths[l]))])
                : p)
            .Concat(Added.Select(a => (a.Name, a.Values))).ToList();
        cards.AddRange(Generation(seed, v2, CountV2, "s2-", 100));
        return cards;
    }

    private static List<StyleCard> Generation(int seed, IReadOnlyList<(string Name, IReadOnlyList<(string Value, string Sentence)> Values)> parameters,
        int count, string prefix, int salt)
    {
        var columns = parameters.Select((p, i) =>
        {
            var values = Enumerable.Range(0, count).Select(k => p.Values[k % p.Values.Count]).ToList();
            var rng = new Rng(Rng.Derive(seed, i + salt));
            for (var k = values.Count - 1; k > 0; k--)
            {
                var j = rng.Between(0, k);
                (values[k], values[j]) = (values[j], values[k]);
            }

            return values;
        }).ToList();
        return [.. Enumerable.Range(0, count).Select(k =>
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var sentences = new List<string>();
            for (var i = 0; i < parameters.Count; i++)
            {
                values[parameters[i].Name] = columns[i][k].Value;
                sentences.Add(columns[i][k].Sentence);
            }

            sentences.Add("Write every year in full, with four digits.");
            return new StyleCard(prefix + (k + 1).ToString("00", CultureInfo.InvariantCulture), values, string.Join(" ", sentences));
        })];
    }

    public static string ToJson(IReadOnlyList<StyleCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        var array = new JsonArray();
        foreach (var card in cards)
        {
            var parameters = new JsonObject();
            foreach (var (name, value) in card.Parameters)
            {
                parameters[name] = value;
            }

            array.Add(new JsonObject { ["id"] = card.Id, ["parameters"] = parameters, ["text"] = card.Text });
        }

        return new JsonObject
        {
            ["about"] = "The style cards of the scenario sampler (specs/scenario-sampler.md), written by `notes style-cards --write`: version 1 (c-01 to c-40, 15 November 2026) and version 2 (s2-01 to s2-80, 17 November 2026: shorter and messier, no set formula for the parties). A card says how a note is written, never what it says; it names no claim type, element or polarity.",
            ["cards"] = array,
        }.ToJsonString(NotesJson.Options) + "\n";
    }

    public static IReadOnlyList<StyleCard> Load(string path) =>
        [.. NotesJson.Load(path)["cards"]!.AsArray().Select(c => new StyleCard((string)c!["id"]!,
            c["parameters"]!.AsObject().ToDictionary(p => p.Key, p => (string)p.Value!, StringComparer.Ordinal),
            (string)c["text"]!))];
}
