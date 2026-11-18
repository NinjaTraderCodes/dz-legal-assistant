using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Tests;

/// <summary>
/// The challenge set's four recipes of sampler 1.4 (specs/scenario-sampler.md, "The challenge set's recipes"): each draw
/// has its constraint, the forced style reaches the writer, the code check passes a note that has the difficulty and fails
/// one that lacks it, and the base sets draw the same bytes as before. The note texts are fictitious test fixtures.
/// </summary>
public sealed partial class ChallengeTests
{
    private static readonly NotesFiles Files = NotesFiles.Find(null);

    private static readonly string[] Ids = ["doubleNegation", "restriction", "contestOfAmount", "documentAgainstContest"];

    private static readonly Lazy<Dictionary<string, (IReadOnlyList<JsonObject> Draws, Dictionary<string, int> Rejected)>> Drawn = new(() =>
        Ids.ToDictionary(id => id, id =>
        {
            var sampler = Sampler();
            return (sampler.Draw(new SampleRequest("challenge", 300, 1811, Difficulty: id)), sampler.Rejected);
        }));

    private static ScenarioSampler Sampler() => new(WritingTests.Data, SampleCommand.FinalRecords(Files));

    private static string Lines(IEnumerable<JsonObject> draws) => string.Concat(draws.Select(s => s.ToJsonString(NotesJson.Compact) + "\n"));

    private static JsonObject WithoutOutcome(JsonObject s)
    {
        var copy = (JsonObject)s.DeepClone();
        copy.Remove("outcome");
        return copy;
    }

    private static JsonObject Target(JsonObject s) => ScenarioSampler.Event(s, (string)s["target"]!);

    /// <summary>
    /// The bytes `notes sample` writes for three base sets, recorded with sampler 1.3 before the recipes were added: a
    /// base set (no --difficulty) draws exactly what it drew before. The hashes move only if the seed files change.
    /// Hashed without each scenario's `outcome`, which the rule evaluator computes from whichever final records exist
    /// (18 November 2026: the land-registry-publication final moved it; the draws themselves did not change). The
    /// hashes were recomputed that day with the sampler code unchanged since this test last passed (9809f7a).
    /// </summary>
    [Theory]
    [InlineData("train", 400, 20261117, 8, "49a74f8ea7d8a199a0eb76ec1815c2a75134b173d40968d48afec24da3cc1e26")]
    [InlineData("dev", 300, 1612, 5, "8235131445196894ee62b0de9058854ddbe96fa2064b886142897e213d3b2368")]
    [InlineData("pilot", 200, 500, 0, "f673e6086d95c0c3547303eb3a26ab2bc54da6c95a0160c42c7296ab3c8c8adc")]
    public void TheBaseSetsDrawTheSameBytesAsSampler13(string set, int count, int seed, int groups, string sha256)
    {
        var lines = Lines(Sampler().Draw(new SampleRequest(set, count, seed, groups)).Select(WithoutOutcome));
        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lines))).ToLowerInvariant());
    }

    [Theory]
    [InlineData("doubleNegation")]
    [InlineData("restriction")]
    [InlineData("contestOfAmount")]
    [InlineData("documentAgainstContest")]
    public void TheSameSeedGivesTheSameChallengeScenarios(string id)
    {
        Assert.Equal(Lines(Sampler().Draw(new SampleRequest("challenge", 60, 9, Difficulty: id))),
            Lines(Sampler().Draw(new SampleRequest("challenge", 60, 9, Difficulty: id))));
    }

    [Fact]
    public void EveryDrawHasItsDifficultyItsTargetAndItsIdsAndTwoRecipesShareNoDraw()
    {
        foreach (var id in Ids)
        {
            var (draws, _) = Drawn.Value[id];
            Assert.Equal(300, draws.Count);
            Assert.All(draws, s => Assert.Equal(id, (string)s["difficulty"]!));
            Assert.All(draws, s => Assert.Equal("challenge", (string)s["set"]!));
            Assert.All(draws, s => Assert.StartsWith($"s-challenge-{id.ToLowerInvariant()}-", (string)s["id"]!, StringComparison.Ordinal));
            Assert.Equal(300, draws.Select(s => (string)s["family"]!).Distinct().Count());
            Assert.All(draws, s => Assert.Null(s["group"]));
            var recipe = ChallengeRecipes.Get(id);
            Assert.All(draws, s => Assert.Equal(recipe.StyleValue, (string?)s["styleForced"]?[recipe.StyleParameter ?? ""]));
        }

        // Two recipes drawn with one seed take their own streams: their casts differ.
        string Cast(string id, int i) => Drawn.Value[id].Draws[i]["cast"]!.ToJsonString();
        Assert.True(Enumerable.Range(0, 20).Count(i => Cast("doubleNegation", i) == Cast("documentAgainstContest", i)) <= 1);
    }

    [Fact]
    public void DoubleNegationTargetsAStatedEventWithoutAContest()
    {
        Assert.All(Drawn.Value["doubleNegation"].Draws, s =>
        {
            Assert.Equal("stated", (string)Target(s)["polarity"]!);
            Assert.Null(Target(s)["contest"]);
        });
    }

    [Fact]
    public void RestrictionTargetsAPartPaymentWithBothSums()
    {
        Assert.All(Drawn.Value["restriction"].Draws, s =>
        {
            var payment = Target(s);
            Assert.Equal("payment_made", (string)payment["kind"]!);
            Assert.Equal("stated", (string)payment["polarity"]!);
            Assert.Equal("defendant", (string)payment["actor"]!);
            Assert.Null(payment["contest"]);
            var paid = (long)payment["amount"]!;
            Assert.Contains(s["events"]!.AsArray(), e => e != payment && (string)e!["polarity"]! == "stated" && (long?)e["amount"] > paid);
        });
    }

    [Fact]
    public void ContestOfAmountTargetsAStatedEventWithBothFigures()
    {
        Assert.All(Drawn.Value["contestOfAmount"].Draws, s =>
        {
            var e = Target(s);
            Assert.Equal("stated", (string)e["polarity"]!);
            Assert.Equal("amount", (string)e["contest"]!["of"]!);
            Assert.True((long)e["contest"]!["amount"]! < (long)e["amount"]!);
        });
        Assert.Empty(Drawn.Value["contestOfAmount"].Rejected);  // forced in the draw: nothing to reject
    }

    /// <summary>18 November 2026: «لا ننفي» (we do not deny) is a double negation, as «لا ينفي» is; a single cue is not.</summary>
    [Theory]
    [InlineData("لا ننفي توجيهنا تنبيها بالإخلاء", 2)]
    [InlineData("لا تنفي المستأجرة استلام الإنذار", 2)]
    [InlineData("لا أنكر أنه سلمني المبلغ", 2)]
    [InlineData("لا ينكر المدين توقيعه", 2)]
    [InlineData("ننفي استلام المبلغ", 1)]
    [InlineData("لم يسدد المدين الدين", 1)]
    public void ADisputeVerbCountsInEveryPersonOfItsPresent(string sentence, int count) =>
        Assert.Equal(count, ChallengeChecks.NegationCount(sentence));

    [Fact]
    public void DocumentAgainstContestTargetsADisputedEventThatAPaperRecords()
    {
        Assert.All(Drawn.Value["documentAgainstContest"].Draws, s =>
        {
            var e = Target(s);
            Assert.Equal("disputed", (string)e["polarity"]!);
            Assert.Contains((string)e["kind"]!, ChallengeRecipes.DocumentKinds);
            Assert.Equal(ChallengeRecipes.DocumentLine, (string)e["document"]!);
        });

        // Filtered from base draws, so the rejected draws are counted by reason, under the recipe's id.
        var rejected = Drawn.Value["documentAgainstContest"].Rejected;
        Assert.True(rejected.GetValueOrDefault("documentAgainstContest: no disputed event") > 0);
        Assert.All(rejected.Keys, k => Assert.StartsWith("documentAgainstContest: ", k, StringComparison.Ordinal));
    }

    /// <summary>The trap of the spec: the sampler writes no Arabic. A challenge scenario's Arabic is its cast's names and
    /// places only, and the forced sentences carry only the cue words the recipe names.</summary>
    [Fact]
    public void TheSamplerWritesNoArabicBeyondTheRecipesCueWords()
    {
        foreach (var s in Ids.SelectMany(id => Drawn.Value[id].Draws))
        {
            Assert.All(s.Where(kv => kv.Key != "cast"), kv => Assert.DoesNotMatch(Arabic(), kv.Value?.ToJsonString(NotesJson.Compact) ?? ""));
        }

        string[] named = ["إلا", "سوى", "غير", "لم … إلا", "لا ينكر", "لم يمتنع عن"];
        foreach (var sentence in new[] { ChallengeRecipes.ForcedSentence("negation", "restriction", "c/k"),
                     ChallengeRecipes.ForcedSentence("negation", "doubleNegation", "c/k"), ChallengeRecipes.DocumentLine })
        {
            Assert.All(Quoted().Matches(sentence).Select(m => m.Groups[1].Value), word => Assert.Contains(word, named));
            Assert.DoesNotMatch(Arabic(), Quoted().Replace(sentence, ""));
        }
    }

    [Fact]
    public void TheForcedStyleValueReachesTheWriterAsTheCardsOwnValuesDo()
    {
        var cards = WritingTests.Data.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        foreach (var (id, parameter, value) in new[] { ("doubleNegation", "negation", "doubleNegation"), ("restriction", "negation", "restriction"),
                     ("documentAgainstContest", "fields", "allFour") })
        {
            foreach (var s in Drawn.Value[id].Draws.Take(40))
            {
                var card = cards[(string)s["style"]!];
                var view = WriteCommand.WriterView(s, 1, cards);
                Assert.Equal(value, (string)view["style"]!["parameters"]![parameter]!);
                var text = (string)view["style"]!["text"]!;
                Assert.Contains(ChallengeRecipes.ForcedSentence(parameter, value, (string)s["target"]!), text, StringComparison.Ordinal);
                if (card.Parameters[parameter] != value)
                {
                    Assert.DoesNotContain(StyleCards.Sentence(parameter, card.Parameters[parameter]), text, StringComparison.Ordinal);
                }

                Assert.All(card.Parameters.Where(p => p.Key != parameter),
                    p => Assert.Equal(p.Value, (string)view["style"]!["parameters"]![p.Key]!));
                Assert.Null(view["difficulty"]);
                Assert.Null(view["target"]);
                Assert.Null(view["styleForced"]);
            }
        }

        // A scenario of a base set: the card exactly as it is.
        var plain = Sampler().Draw(new SampleRequest("dev", 1, 3))[0];
        var plainCard = cards[(string)plain["style"]!];
        Assert.Equal(plainCard.Text, (string)WriteCommand.WriterView(plain, 1, cards)["style"]!["text"]!);
    }

    [Theory]
    [InlineData("despite", "not implemented")]
    [InlineData("nearMiss", "not implemented")]
    [InlineData("polarity", "no difficulty")]
    public void AnUnknownOrUnimplementedDifficultyIsAnErrorNamingTheFour(string id, string kind)
    {
        var error = Assert.Throws<ArgumentException>(() => ChallengeRecipes.Get(id));
        Assert.Contains(kind, error.Message, StringComparison.Ordinal);
        Assert.All(Ids, i => Assert.Contains(i, error.Message, StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => Sampler().Draw(new SampleRequest("challenge", 1, 1, Difficulty: id)));
        Assert.Throws<ArgumentException>(() => SampleCommand.Run(Files, "challenge",
            new Dictionary<string, string> { ["--difficulty"] = id, ["--count"] = "1", ["--seed"] = "1" }));
    }

    [Fact]
    public void ADifficultyGoesWithTheChallengeSetOnlyAndDrawsNoGroup()
    {
        Assert.Throws<ArgumentException>(() => SampleCommand.Run(Files, "dev",
            new Dictionary<string, string> { ["--difficulty"] = "restriction", ["--count"] = "1", ["--seed"] = "1" }));
        Assert.Throws<ArgumentException>(() => SampleCommand.Run(Files, "challenge",
            new Dictionary<string, string> { ["--count"] = "1", ["--seed"] = "1" }));
        Assert.Throws<ArgumentException>(() => Sampler().Draw(new SampleRequest("challenge", 8, 1, Groups: 1, Difficulty: "restriction")));
    }

    // --- the code checks ---------------------------------------------------------------------------------------------

    private static JsonObject Scenario(string id, string kind, string polarity, long? amount = null, long? contest = null)
    {
        var e = new JsonObject { ["claim"] = "loan-repayment", ["kind"] = kind, ["polarity"] = polarity, ["actor"] = "defendant" };
        if (amount is long a)
        {
            e["amount"] = a;
        }

        if (contest is long c)
        {
            e["contest"] = new JsonObject { ["of"] = "amount", ["by"] = "defendant", ["how"] = "h", ["amount"] = c };
        }

        return new JsonObject
        {
            ["id"] = "s-challenge-fx-00001", ["claims"] = new JsonArray("loan-repayment"), ["events"] = new JsonArray(e),
            ["difficulty"] = id, ["target"] = "loan-repayment/" + kind,
        };
    }

    private static IReadOnlyList<string> Check(JsonObject scenario, string facts, string? quote, string documents = "")
    {
        var note = new JsonObject { ["subject"] = "", ["facts"] = facts, ["requests"] = "", ["documents"] = documents };
        var quotes = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        if (quote is not null)
        {
            quotes["event:" + (string)scenario["target"]!] = [WriterLabels.Span(note, "facts", quote, 1) ?? throw new ArgumentException(quote)];
        }

        return ChallengeChecks.Run(scenario, note, new WriterLabels.Pointed(quotes, []));
    }

    [Theory]
    [InlineData("اقترض المدعى عليه من موكلنا 500.000 دج. لم يسدد منه إلا 150.000 دج، وبقي الباقي في ذمته.", "لم يسدد منه إلا 150.000 دج", true)]
    [InlineData("اقترض المدعى عليه 500.000 دج، ولم يدفع منها سوى 150.000 دج.", "ولم يدفع منها سوى 150.000 دج", true)]
    [InlineData("لم يرد المدعى عليه غير 150.000 دج من أصل 500.000 دج.", "لم يرد المدعى عليه غير 150.000 دج", true)]
    [InlineData("سدد المدعى عليه 150.000 دج من أصل 500.000 دج فقط.", "سدد المدعى عليه 150.000 دج", false)]
    [InlineData("سدد المدين 150.000 دج من أصل 500.000 دج. ولا يملك إلا محلا صغيرا.", "سدد المدين 150.000 دج", false)]
    public void RestrictionIsANegationThenARestrictionInTheSentenceOfThePayment(string facts, string quote, bool passes)
    {
        var reasons = Check(Scenario("restriction", "payment_made", "stated", 150000), facts, quote);
        Assert.Equal(passes, reasons.Count == 0);
        Assert.All(reasons, r => Assert.StartsWith("challenge: restriction: ", r, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("لا ينكر المدعى عليه أنه اقترض المبلغ من موكلنا.", "لا ينكر المدعى عليه أنه اقترض المبلغ", true)]
    [InlineData("لم يمتنع المدعى عليه عن الإقرار بالقرض أمام الشهود.", "لم يمتنع المدعى عليه عن الإقرار بالقرض", true)]
    [InlineData("اقترض المدعى عليه المبلغ من موكلنا.", "اقترض المدعى عليه المبلغ", false)]
    [InlineData("اقترض المدعى عليه المبلغ دون كتابة.", "اقترض المدعى عليه المبلغ", false)]
    [InlineData("لا يزال المدعى عليه يقر بأنه اقترض المبلغ.", "يقر بأنه اقترض المبلغ", false)]
    public void DoubleNegationIsTwoNegationCuesInTheSentenceOfTheEvent(string facts, string quote, bool passes)
    {
        var reasons = Check(Scenario("doubleNegation", "money_lent", "stated"), facts, quote);
        Assert.Equal(passes, reasons.Count == 0);
        Assert.All(reasons, r => Assert.StartsWith("challenge: doubleNegation: fewer than two negation cues", r, StringComparison.Ordinal));
    }

    [Fact]
    public void ContestOfAmountNeedsBothFiguresReadByTheParsers()
    {
        var scenario = Scenario("contestOfAmount", "money_lent", "stated", 750000, 300000);
        Assert.Empty(Check(scenario, "يقر المدعى عليه بالقرض، غير أنه يقول إن مبلغه 300.000 دج لا 750.000 دج كما يقول موكلنا.", "يقر المدعى عليه بالقرض"));
        var reasons = Check(scenario, "يقر المدعى عليه بالقرض، غير أنه يقول إن مبلغه أقل بكثير من 750.000 دج.", "يقر المدعى عليه بالقرض");
        Assert.Equal("challenge: contestOfAmount: the parsers do not find 300000 (the other side's figure) in the note", Assert.Single(reasons));
    }

    [Fact]
    public void DocumentAgainstContestNeedsTheDocumentsAndAQuoteForTheContest()
    {
        var scenario = Scenario("documentAgainstContest", "debt_acknowledged", "disputed");
        const string Facts = "ينكر المدعى عليه أنه وقع الاعتراف بالدين المحتج به.";
        const string Quote = "ينكر المدعى عليه أنه وقع الاعتراف بالدين";
        Assert.Empty(Check(scenario, Facts, Quote, "- اعتراف بالدين مكتوب\n- نسخة من بطاقة التعريف"));
        Assert.Equal("challenge: documentAgainstContest: the documents field is empty", Assert.Single(Check(scenario, Facts, Quote)));
        Assert.Contains("challenge: documentAgainstContest: no quote for the contest of event:loan-repayment/debt_acknowledged",
            Check(scenario, Facts, null, "- اعتراف بالدين مكتوب"));
    }

    /// <summary>
    /// Through the code checks of every note: the writing fixture as a challenge note of a contest of the amount passes;
    /// as a double negation of its delivery it fails with a reason naming the check, so ingest makes it invalid and
    /// rewrites it once, like any note.
    /// </summary>
    [Fact]
    public void AChallengeNoteThatFailsItsCheckIsInvalidWithAReasonNamingIt()
    {
        var checks = new CodeChecks(WritingTests.Data, [File.ReadAllText(WritingTests.Data.Files.Guide)]);
        var card = WritingTests.Data.Cards.First(c => c.Parameters["length"] == "short");
        var note = WritingTests.Note();
        var amount = WritingTests.Scenario();
        amount["difficulty"] = "contestOfAmount";
        amount["target"] = "unpaid-sale-price/payment_made";
        Assert.Empty(checks.Run(amount, note, WriterLabels.Point(amount, note), card));

        var negation = WritingTests.Scenario();
        negation["difficulty"] = "doubleNegation";
        negation["target"] = "unpaid-sale-price/goods_delivered";
        negation["styleForced"] = new JsonObject { ["negation"] = "doubleNegation" };
        var reasons = checks.Run(negation, note, WriterLabels.Point(negation, note), card);
        Assert.Equal("challenge: doubleNegation: fewer than two negation cues of the cue table in the sentence of event:unpaid-sale-price/goods_delivered",
            Assert.Single(reasons));
    }

    [GeneratedRegex(@"[؀-ۿ]")]
    private static partial Regex Arabic();

    [GeneratedRegex("«([^»]*)»")]
    private static partial Regex Quoted();
}
