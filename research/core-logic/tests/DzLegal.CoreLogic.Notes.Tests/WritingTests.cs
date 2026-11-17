using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Sampler;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Tests;

/// <summary>
/// writer-labels, build-spans and the code checks (specs/notes-pipeline.md, section 3) on a fixture of every reference
/// kind; then notes write and notes ingest end to end in a temporary work folder. The fixture note is a test fixture,
/// fictitious, never part of any set.
/// </summary>
public sealed class WritingTests : IDisposable
{
    private static readonly Lazy<SamplerData> LazyData = new(() => SamplerData.Load(NotesFiles.Find(null)));
    private readonly string work = Path.Combine(Path.GetTempPath(), "notes-writing-" + Guid.NewGuid().ToString("N"));

    public WritingTests() => Directory.CreateDirectory(work);

    internal static SamplerData Data => LazyData.Value;

    private static StyleCard ShortCard => Data.Cards.First(c => c.Parameters["length"] == "short");

    public void Dispose() => Directory.Delete(work, recursive: true);

    internal static JsonObject Scenario() => JsonNode.Parse($$$"""
        {"id":"s-fx-00001","set":"pilot","family":"f-fx-00001","claims":["unpaid-sale-price","contract-damages"],"outsideKind":null,
         "roles":{"contract-damages":{"relation":"sale","position":"seller"}},
         "events":[
          {"claim":"unpaid-sale-price","kind":"contract_concluded","polarity":"stated","actor":"unknown","date":"2024-03-10","amount":1200000},
          {"claim":"unpaid-sale-price","kind":"goods_delivered","polarity":"stated","actor":"plaintiff"},
          {"claim":"unpaid-sale-price","kind":"payment_made","polarity":"stated","actor":"defendant","amount":200000,
           "contest":{"of":"amount","by":"plaintiff","how":"h","amount":100000}},
          {"claim":"unpaid-sale-price","kind":"price_unpaid","polarity":"disputed","actor":"defendant","disputedBy":"defendant","amount":1000000,"how":"h"},
          {"claim":"unpaid-sale-price","kind":"notice_served","polarity":"stated","actor":"plaintiff","date":"2025-01-05","form":"f"},
          {"claim":"contract-damages","kind":"contract_concluded","polarity":"stated","actor":"unknown","date":"2024-03-10","amount":1200000},
          {"claim":"contract-damages","kind":"delivery_late","polarity":"notMentioned","actor":"defendant"},
          {"claim":"contract-damages","kind":"delivery_missing","polarity":"notMentioned","actor":"defendant"},
          {"claim":"contract-damages","kind":"work_stopped","polarity":"notMentioned","actor":"defendant"},
          {"claim":"contract-damages","kind":"other","polarity":"stated","actor":"defendant"},
          {"claim":"contract-damages","kind":"notice_served","polarity":"stated","actor":"plaintiff","date":"2025-01-05","form":"f"}],
         "slots":{"notice_served":"yes","notice_date":"2025-01-05","notice_means":"bailiff","contract_date":"2024-03-10",
                  "public_body_party":"no","property_registered":"unknown"},
         "requests":[{"kind":"payment","amount":1000000},{"kind":"damages","amount":150000}],
         "extraValues":[{"kind":"amount","value":30000,"what":"the fee for copies","optional":false},
                        {"kind":"amount","value":999000,"what":"the value of a car","optional":true}],
         "cast":{"plaintiff":{"kind":"company","name":"سيرتا","describe":"d"},"defendant":{"kind":"person","name":"كريم بلعيد","gender":"m"},"place":"سطيف"},
         "distractors":[],"petitionDate":"2025-06-01","style":"{{{ShortCard.Id}}}","difficulty":null}
        """)!.AsObject();

    private const string Facts = "أبرمت شركة سيرتا مع السيد كريم بلعيد عقد بيع بتاريخ 10/03/2024 بثمن قدره 1.200.000 دج، وسلمته البضاعة كاملة. " +
        "دفع المشتري 200.000 دج غير أن موكلتنا تقول إنها لم تقبض منه إلا 100.000 دج. يزعم المشتري أنه سدد الباقي نقدا وقدره 1.000.000 دج. " +
        "كما خالف المشتري التزامه بعدم إعادة بيع السلعة خارج الولاية. أعذر بواسطة محضر قضائي بتاريخ 05/01/2025 ولم يستجب. " +
        "صرفت الموكلة 30.000 دج على نسخ الوثائق. الطرفان شخص طبيعي وشركة خاصة، وتطلب الشركة الفصل في أقرب الآجال.";

    private const string Requests = "إلزام المشتري بدفع 1.000.000 دج وتعويض قدره 150.000 دج.";

    internal static JsonObject Note(string facts = Facts, string requests = Requests)
    {
        JsonObject F(string reference, params (string Field, string Quote)[] quotes) => new()
        {
            ["ref"] = reference, ["expressed"] = quotes.Length > 0,
            ["quotes"] = new JsonArray([.. quotes.Select(q => (JsonNode)new JsonObject { ["field"] = q.Field, ["quote"] = q.Quote, ["occurrence"] = 1 })]),
        };
        return new JsonObject
        {
            ["n"] = 1, ["subject"] = "", ["facts"] = facts, ["requests"] = requests, ["documents"] = "",
            ["facts_of_scenario"] = new JsonArray(
                F("claim", ("requests", "إلزام المشتري بدفع")),
                F("role:contract-damages", ("facts", "أبرمت شركة سيرتا مع السيد كريم بلعيد عقد بيع")),
                F("event:unpaid-sale-price/contract_concluded", ("facts", "عقد بيع بتاريخ 10/03/2024")),
                F("event-date:unpaid-sale-price/contract_concluded", ("facts", "10/03/2024")),
                F("event-amount:unpaid-sale-price/contract_concluded", ("facts", "1.200.000 دج")),
                F("event:unpaid-sale-price/goods_delivered", ("facts", "وسلمته البضاعة كاملة")),
                F("event:unpaid-sale-price/payment_made", ("facts", "دفع المشتري 200.000 دج")),
                F("event-amount:unpaid-sale-price/payment_made", ("facts", "200.000 دج")),
                F("contest-amount:unpaid-sale-price/payment_made", ("facts", "100.000 دج")),
                F("event:unpaid-sale-price/price_unpaid", ("facts", "يزعم المشتري أنه سدد الباقي نقدا")),
                F("event-amount:unpaid-sale-price/price_unpaid", ("facts", "1.000.000 دج")),
                F("event:unpaid-sale-price/notice_served", ("facts", "أعذر بواسطة محضر قضائي")),
                F("event-date:unpaid-sale-price/notice_served", ("facts", "05/01/2025")),
                F("event:contract-damages/contract_concluded", ("facts", "عقد بيع بتاريخ 10/03/2024")),
                F("event-date:contract-damages/contract_concluded", ("facts", "10/03/2024")),
                F("event-amount:contract-damages/contract_concluded", ("facts", "1.200.000 دج")),
                F("event:contract-damages/other", ("facts", "خالف المشتري التزامه بعدم إعادة بيع السلعة")),
                F("event:contract-damages/notice_served", ("facts", "أعذر بواسطة محضر قضائي")),
                F("event-date:contract-damages/notice_served", ("facts", "05/01/2025")),
                F("slot:notice_served", ("facts", "أعذر بواسطة محضر قضائي")),
                F("slot:notice_date", ("facts", "05/01/2025")),
                F("slot:notice_means", ("facts", "بواسطة محضر قضائي")),
                F("slot:contract_date", ("facts", "10/03/2024")),
                F("slot:public_body_party", ("facts", "الطرفان شخص طبيعي وشركة خاصة")),
                F("request:payment", ("requests", "إلزام المشتري بدفع 1.000.000 دج")),
                F("request-amount:payment", ("requests", "1.000.000 دج")),
                F("request:damages", ("requests", "وتعويض قدره 150.000 دج")),
                F("request-amount:damages", ("requests", "150.000 دج")),
                F("value:the fee for copies", ("facts", "30.000 دج")),
                F("value:the value of a car")),
        };
    }

    /// <summary>The checks, with the people of two other scenarios of the set (and this one's own defendant).</summary>
    private static CodeChecks Checks() => new(Data, [File.ReadAllText(Data.Files.Guide)], ["سعاد حمداني", "علي مهدي", "كريم بلعيد"]);

    [Fact]
    public void TheWriterPromptTakesTheFramingOfItsCallAndNothingIsLeftUnfilled()
    {
        Assert.Equal("desk", WriteCommand.FramingOf(1).Id);
        Assert.Equal("visit", WriteCommand.FramingOf(2).Id);
        Assert.Equal("desk", WriteCommand.FramingOf(5).Id);
        Assert.Equal(4, WriteCommand.Framings.Select(f => f.Id).Distinct().Count());
        var files = NotesFiles.Find(null);
        var scenarios = new ScenarioSampler(Data, SampleCommand.FinalRecords(files)).Draw(new SampleRequest("framing", 3, 31, Groups: 0));
        var prompt = WriteCommand.Prompt(files, Data, scenarios, WriteCommand.FramingOf(3).Text);
        Assert.Contains(WriteCommand.FramingOf(3).Text, prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void QuotesAreFoundOnWordEdgesAndEveryFactIsPointedAt()
    {
        var pointed = WriterLabels.Point(Scenario(), Note());
        Assert.Empty(pointed.Problems);
        var paid = pointed.Quotes["event-amount:unpaid-sale-price/payment_made"].Single();
        Assert.Equal(Facts.IndexOf("دفع المشتري 200.000", StringComparison.Ordinal) + "دفع المشتري ".Length, (int)paid["start"]!);
        Assert.Empty(pointed.Quotes["value:the value of a car"]);
    }

    [Fact]
    public void TheLabelsAreTheScenariosWithTheWritersQuotes()
    {
        var pointed = WriterLabels.Point(Scenario(), Note());
        var labels = WriterLabels.Labels(Data, Scenario(), pointed.Quotes);
        Assert.Equal(["unpaid-sale-price", "contract-damages"], labels["claims"]!.AsArray().Select(c => (string)c!));
        var roles = labels["roles"]!.AsArray();
        Assert.Equal("seller", (string)roles.Single(r => (string)r!["claim"]! == "contract-damages")!["position"]!);
        Assert.Equal("seller", (string)roles.Single(r => (string)r!["claim"]! == "unpaid-sale-price")!["position"]!);
        Assert.Equal(11, labels["events"]!.AsArray().Count);
        var paid = labels["events"]!.AsArray().Single(e => (string)e!["kind"]! == "payment_made")!;
        Assert.True((bool)paid["amount"]!["amountDisputed"]!);
        Assert.Empty(labels["events"]!.AsArray().Single(e => (string)e!["kind"]! == "delivery_late")!["evidence"]!.AsArray());
        Assert.Equal(6, labels["slots"]!.AsArray().Count);
        Assert.Empty(labels["slots"]!.AsArray().Single(s => (string)s!["slot"]! == "property_registered")!["evidence"]!.AsArray());

        // One entry per place a value is written; the same words merge their roles; the contest's figure is its own entry.
        var values = labels["values"]!.AsArray().Select(v => v!.AsObject()).ToList();
        Assert.Equal(9, values.Count);
        string Roles(string quote, string field = "facts") => string.Join(",", values
            .Single(v => (string)v["at"]!["quote"]! == quote && (string)v["at"]!["field"]! == field)["roles"]!.AsArray().Select(r => (string)r!));
        Assert.Equal("contract_concluded,contract_date", Roles("10/03/2024"));
        Assert.Equal("notice_date,notice_served", Roles("05/01/2025"));
        Assert.Equal("price_unpaid", Roles("1.000.000 دج"));
        Assert.Equal("payment", Roles("1.000.000 دج", "requests"));
        Assert.Equal("payment_made", Roles("100.000 دج"));
        Assert.Equal("", Roles("30.000 دج"));
        Assert.True((bool)values.Single(v => (string)v["at"]!["quote"]! == "100.000 دج")["amountDisputed"]!);
    }

    /// <summary>
    /// The guide, section 8: a date given as the same day as another date of the note («في اليوم نفسه») takes that date;
    /// nothing new is added to the values, and the date written once carries the role (pilot, 16 November 2026).
    /// </summary>
    [Fact]
    public void ADateGivenAsTheSameDayTakesTheDateWrittenElsewhere()
    {
        var scenario = Scenario();
        foreach (var e in scenario["events"]!.AsArray().Where(e => (string)e!["kind"]! == "notice_served"))
        {
            e!["date"] = "2024-03-10";
        }

        scenario["slots"]!["notice_date"] = "2024-03-10";
        var note = Note(facts: Facts.Replace("بتاريخ 05/01/2025", "في اليوم نفسه", StringComparison.Ordinal));
        foreach (var fact in note["facts_of_scenario"]!.AsArray())
        {
            if ((string)fact!["ref"]! is "event-date:unpaid-sale-price/notice_served" or "event-date:contract-damages/notice_served" or "slot:notice_date")
            {
                fact["quotes"]![0]!["quote"] = "في اليوم نفسه";
            }
        }

        var pointed = WriterLabels.Point(scenario, note);
        Assert.Empty(Checks().Run(scenario, note, pointed, ShortCard));
        var labels = WriterLabels.Labels(Data, scenario, pointed.Quotes);
        var values = labels["values"]!.AsArray();
        Assert.Equal(8, values.Count);  // no entry for «في اليوم نفسه»
        var date = values.Single(v => (string)v!["at"]!["quote"]! == "10/03/2024")!;
        Assert.Equal(["contract_concluded", "contract_date", "notice_date", "notice_served"], date["roles"]!.AsArray().Select(r => (string)r!));
        var notice = labels["events"]!.AsArray().First(e => (string)e!["kind"]! == "notice_served")!;
        Assert.Equal("10/03/2024", (string)notice["date"]!["at"]!["quote"]!);

        // Without the date written anywhere in the note, the reference is refused.
        var alone = Note(facts: Facts.Replace("بتاريخ 05/01/2025", "في اليوم نفسه", StringComparison.Ordinal)
            .Replace("بتاريخ 10/03/2024", "", StringComparison.Ordinal));
        Assert.False(CodeChecks.NoteWritesDate(alone, "2024-03-10"));
    }

    [Fact]
    public void AGoodNotePassesTheCodeChecks()
    {
        var note = Note();
        Assert.Empty(Checks().Run(Scenario(), note, WriterLabels.Point(Scenario(), note), ShortCard));
    }

    [Theory]
    [InlineData("وقدره 1.000.000 دج", "وقدره 1.000.000 دج وقيمة السيارة 450.000 دج", "values: an amount the scenario does not hold")]
    [InlineData("ولم يستجب", "ولم يستجب، راه يماطل", "arabic: Darija")]
    [InlineData("ولم يستجب", "ولم يستجب (SMS)", "arabic: a Latin letter")]
    [InlineData("ولم يستجب", "ولم يستجب، وقد شهدت سعاد حمداني بذلك", "names: a person of another scenario")]
    [InlineData("ولم يستجب", "ولم يستجب في 12/07/2025", "values: a date the scenario does not hold")]
    public void EachCheckFindsWhatItLooksFor(string from, string to, string reason)
    {
        var note = Note(facts: Facts.Replace(from, to, StringComparison.Ordinal));
        var reasons = Checks().Run(Scenario(), note, WriterLabels.Point(Scenario(), note), ShortCard);
        Assert.Contains(reasons, r => r.StartsWith(reason, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ولم يستجب، وعوّل على مهدي البضاعة")]  // «على» reads «علي» at level 2: not a person
    [InlineData("ولم يستجب، وحضر عمر بلعيد أخ المشتري")]  // this scenario's own family name
    public void WordsThatOnlyLookLikeAnotherScenariosPersonAreNotFlagged(string to)
    {
        var note = Note(facts: Facts.Replace("ولم يستجب", to, StringComparison.Ordinal));
        Assert.DoesNotContain(Checks().Run(Scenario(), note, WriterLabels.Point(Scenario(), note), ShortCard),
            r => r.StartsWith("names:", StringComparison.Ordinal));
    }

    [Fact]
    public void ALengthOutsideTheCardAndACopiedWindowAndAMissingQuoteAreFound()
    {
        var veryShort = Data.Cards.First(c => c.Parameters["length"] == "veryShort");
        var note = Note();
        Assert.Contains(Checks().Run(Scenario(), note, WriterLabels.Point(Scenario(), note), veryShort), r => r.StartsWith("length:", StringComparison.Ordinal));
        var copied = Note(facts: Facts + " " + "أعذر المدعى عليه بموجب محضر قضائي بتاريخ");
        Assert.Contains(new CodeChecks(Data, ["أعذر المدعى عليه بموجب محضر قضائي بتاريخ 12 مارس"]).Run(Scenario(), copied, WriterLabels.Point(Scenario(), copied), ShortCard),
            r => r.StartsWith("copied:", StringComparison.Ordinal));
        var missing = Note(facts: Facts.Replace("وسلمته البضاعة كاملة", "وسلمته السلعة", StringComparison.Ordinal));
        Assert.Contains(WriterLabels.Point(Scenario(), missing).Problems, p => p.Contains("quote not found", StringComparison.Ordinal));
    }

    [Fact]
    public void WriteThenIngestMovesTheScenarioToWrittenAndAFailureBackToTheQueueOnce()
    {
        var files = NotesFiles.Find(null);
        Directory.CreateDirectory(Path.Combine(work, "scenarios"));
        var scenario = Scenario();
        NotesJson.Write(Path.Combine(work, "scenarios", "pilot.jsonl"), scenario.ToJsonString(NotesJson.Compact) + "\n");
        NotesJson.Write(Path.Combine(work, "queue.jsonl"),
            """{"scenario":"s-fx-00001","set":"pilot","family":"f-fx-00001","writer":"codex","state":"drawn","attempts":0}""" + "\n");
        var options = new Dictionary<string, string> { ["--calls"] = "1", ["--writer"] = "codex", ["--work"] = work };
        Assert.Equal(0, WriteCommand.Run(files, "pilot", options));
        var prompt = File.ReadAllText(Path.Combine(work, "calls", "w-pilot-0001.prompt.md"));
        Assert.DoesNotContain("{{", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("2025-06-01", prompt, StringComparison.Ordinal);  // the petition date is never shown
        Assert.Contains("factsToPointAt", prompt, StringComparison.Ordinal);
        Assert.Single(File.ReadAllLines(Path.Combine(work, $"calls-{DateTime.UtcNow:yyyy-MM-dd}.jsonl")));

        // A failed note: back to the queue once.
        Directory.CreateDirectory(Path.Combine(work, "answers"));
        var bad = Note(facts: Facts + " (SMS)");
        NotesJson.Write(Path.Combine(work, "answers", "w-pilot-0001.json"), new JsonObject { ["notes"] = new JsonArray(bad) }.ToJsonString());
        var ingest = new Dictionary<string, string> { ["--work"] = work };
        Assert.Equal(0, IngestCommand.Run(files, "w-pilot-0001", ingest, new HashSet<string>()));
        var line = File.ReadAllLines(Path.Combine(work, "queue.jsonl")).Single();
        Assert.Contains("\"state\":\"drawn\"", line, StringComparison.Ordinal);
        Assert.DoesNotContain("writeCall", line, StringComparison.Ordinal);

        // The second call: a good note is written.
        Assert.Equal(0, WriteCommand.Run(files, "pilot", options));
        NotesJson.Write(Path.Combine(work, "answers", "w-pilot-0002.json"), new JsonObject { ["notes"] = new JsonArray(Note()) }.ToJsonString());
        Assert.Equal(0, IngestCommand.Run(files, "w-pilot-0002", ingest, new HashSet<string>()));
        Assert.Contains("\"state\":\"written\"", File.ReadAllLines(Path.Combine(work, "queue.jsonl")).Single(), StringComparison.Ordinal);
        var written = JsonNode.Parse(File.ReadAllLines(Path.Combine(work, "written", "pilot.jsonl")).Single())!;
        Assert.Equal("openai", (string)written["provenance"]!["writer"]!["family"]!);
        Assert.True((bool)written["provenance"]!["trainingAllowed"]!);
        Assert.Equal(9, written["provenance"]!["writerLabels"]!["values"]!.AsArray().Count);
    }
}
