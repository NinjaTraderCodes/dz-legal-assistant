using System.Globalization;
using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes.Sampler;

/// <summary>A style card (specs/scenario-sampler.md, "Style cards"): how a note is written, never what it says.</summary>
public sealed record StyleCard(string Id, IReadOnlyDictionary<string, string> Parameters, string Text);

/// <summary>
/// The 40 style cards. Each parameter's values are spread evenly over the cards (each value on 40 / n cards) and shuffled
/// by a fixed seed, so that no two parameters move together; the English paragraph is the parameters' sentences. Written
/// once by <c>notes style-cards --write</c> into notes/sampler/style-cards.json; the sampler reads that file.
/// </summary>
public static class StyleCards
{
    public const int Count = 40;

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

    /// <summary>The 40 cards, built from the parameters by the fixed seed.</summary>
    public static IReadOnlyList<StyleCard> Build(int seed = 20261115)
    {
        var columns = Parameters.Select((p, i) =>
        {
            var values = Enumerable.Range(0, Count).Select(k => p.Values[k % p.Values.Count]).ToList();
            var rng = new Rng(Rng.Derive(seed, i));
            for (var k = values.Count - 1; k > 0; k--)
            {
                var j = rng.Between(0, k);
                (values[k], values[j]) = (values[j], values[k]);
            }

            return values;
        }).ToList();
        return [.. Enumerable.Range(0, Count).Select(k =>
        {
            var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            var sentences = new List<string>();
            for (var i = 0; i < Parameters.Count; i++)
            {
                parameters[Parameters[i].Name] = columns[i][k].Value;
                sentences.Add(columns[i][k].Sentence);
            }

            sentences.Add("Write every year in full, with four digits.");
            return new StyleCard("c-" + (k + 1).ToString("00", CultureInfo.InvariantCulture), parameters, string.Join(" ", sentences));
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
            ["about"] = "The 40 style cards of the scenario sampler (specs/scenario-sampler.md), written by `notes style-cards --write`. A card says how a note is written, never what it says; it names no claim type, element or polarity.",
            ["cards"] = array,
        }.ToJsonString(NotesJson.Options) + "\n";
    }

    public static IReadOnlyList<StyleCard> Load(string path) =>
        [.. NotesJson.Load(path)["cards"]!.AsArray().Select(c => new StyleCard((string)c!["id"]!,
            c["parameters"]!.AsObject().ToDictionary(p => p.Key, p => (string)p.Value!, StringComparer.Ordinal),
            (string)c["text"]!))];
}
