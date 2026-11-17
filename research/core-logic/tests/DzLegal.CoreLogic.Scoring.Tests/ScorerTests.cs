using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Scoring.Tests;

/// <summary>The tests the scorer must have before any model exists (specs/scorer.md, the last section).</summary>
public sealed class ScorerTests
{
    private const string Marker = "MARKER-نص-سري-7731";

    /// <summary>The contract's options on one line: an answers file is JSONL.</summary>
    private static readonly System.Text.Json.JsonSerializerOptions OneLine = new(ContractJson.Options) { WriteIndented = false };

    private static JsonObject Quote(string field, int start, int length) =>
        new() { ["field"] = field, ["quote"] = "x", ["start"] = start, ["length"] = length };

    private static JsonObject Event(string claim, string kind, string polarity, int start = 0) => new()
    {
        ["claim"] = claim, ["kind"] = kind, ["polarity"] = polarity, ["actor"] = "defendant",
        ["evidence"] = polarity == "notMentioned" ? new JsonArray() : new JsonArray(Quote("facts", start, 5)),
    };

    private static string Note(string id, string[] claims, JsonObject[] events, JsonObject[]? slots = null, string[]? requests = null,
        JsonObject[]? values = null, JsonObject[]? roles = null) => new JsonObject
    {
        ["id"] = id,
        ["set"] = "dev",
        ["text"] = new JsonObject { ["facts"] = "أقرض المدعي المدعى عليه مبلغا. لم يرد المبلغ. " + Marker },
        ["provenance"] = new JsonObject { ["writer"] = new JsonObject { ["family"] = "openai", ["model"] = "m", ["date"] = "d" } },
        ["status"] = "kept",
        ["gold"] = new JsonObject
        {
            ["claims"] = new JsonArray([.. claims.Select(c => (JsonNode)c)]),
            ["claimEvidence"] = new JsonArray(Quote("facts", 0, 5)),
            ["roles"] = new JsonArray([.. (roles ?? []).Select(r => (JsonNode)r)]),
            ["events"] = new JsonArray([.. events.Select(e => (JsonNode)e)]),
            ["slots"] = new JsonArray([.. (slots ?? []).Select(s => (JsonNode)s)]),
            ["requests"] = new JsonArray([.. (requests ?? []).Select(k => (JsonNode)new JsonObject
            {
                ["kind"] = k, ["evidence"] = new JsonArray(Quote("facts", 30, 4)),
            })]),
            ["values"] = new JsonArray([.. (values ?? []).Select(v => (JsonNode)v)]),
        },
    }.ToJsonString();

    private static JsonObject Slot(string slot, string value) => new()
    {
        ["slot"] = slot, ["value"] = value, ["evidence"] = value == "unknown" ? new JsonArray() : new JsonArray(Quote("facts", 10, 6)),
    };

    private static JsonObject Value(string kind, string normalized, int start, params string[] roles) => new()
    {
        ["kind"] = kind, ["at"] = Quote("facts", start, 6), ["normalized"] = normalized,
        ["roles"] = new JsonArray([.. roles.Select(r => (JsonNode)r)]),
    };

    /// <summary>Three notes: a loan note, a two-claim note with a role, an outside note.</summary>
    private static List<GoldNote> Gold() =>
    [
        GoldNote.Parse(Note("n1", ["loan-repayment"],
            [Event("loan-repayment", "money_lent", "stated"), Event("loan-repayment", "debt_unpaid", "stated", 20),
             Event("loan-repayment", "payment_made", "denied", 25), Event("loan-repayment", "notice_served", "notMentioned")],
            [Slot("notice_served", "unknown"), Slot("contract_date", "2025-01-10")], ["payment"],
            [Value("amount", "500000", 10, "money_lent"), Value("date", "2025-01-10", 40, "money_lent")])),
        GoldNote.Parse(Note("n2", ["unpaid-rent", "contract-rescission"],
            [Event("unpaid-rent", "rent_unpaid", "stated"), Event("contract-rescission", "notice_served", "disputed", 15),
             Event("unpaid-rent", "notice_served", "notMentioned")],
            [Slot("notice_served", "yes")], ["rescission"], [],
            [new JsonObject { ["claim"] = "contract-rescission", ["relation"] = "sale", ["position"] = "buyer" }])),
        GoldNote.Parse(Note("n3", ["outside"], [])),
    ];

    private static Polarity Polar(string name) => name switch
    {
        "stated" => Polarity.Stated,
        "denied" => Polarity.Denied,
        "disputed" => Polarity.Disputed,
        _ => Polarity.NotMentioned,
    };

    private static TextSpan Span(GoldSpan span) => new(span.Field, span.Start, span.Length);

    /// <summary>The gold turned into answers.</summary>
    private static NoteReading Perfect(GoldNote note) => new()
    {
        Claim = new ClaimReading
        {
            Abstained = false,
            Answers = [.. note.Claims.Select(claim => new ClaimGuess
            {
                ClaimId = claim, Confidence = 0.95, Evidence = [.. note.ClaimEvidence.Select(Span)],
                Role = note.Roles.FirstOrDefault(r => r.Claim == claim) is { } role
                    ? new RoleReading { Relation = role.Relation, Position = role.Position, Confidence = 0.9 } : null,
            })],
        },
        Events = [.. note.Events.Select(e => new EventReading
        {
            Claim = e.Claim, Kind = e.Kind, Polarity = Polar(e.Polarity), Confidence = 0.9, Actor = e.Actor ?? "unknown",
            Evidence = [.. e.Evidence.Select(Span)],
        })],
        Slots = [.. note.Slots.Select(s => new SlotReading { Slot = s.Slot, Value = s.Value, Confidence = 0.9, Evidence = [.. s.Evidence.Select(Span)] })],
        Requests = [.. note.Requests.Select(r => new RequestReading { Kind = r.Kind, Confidence = 0.9, Evidence = [.. r.Evidence.Select(Span)] })],
        Values = [.. note.Values.Select(v => new ValueReading { Kind = v.Kind, Normalized = v.Normalized!, Span = Span(v.At!), Roles = v.Roles })],
        Info = new ReaderInfo { Name = "test", Version = "0" },
    };

    private static NoteReading Empty() => new()
    {
        Claim = new ClaimReading { Abstained = true, Answers = [] },
        Events = [], Slots = [], Requests = [], Values = [], Info = new ReaderInfo { Name = "empty", Version = "0" },
    };

    private static Dictionary<string, NoteReading> Readings(IEnumerable<GoldNote> gold, Func<GoldNote, NoteReading> make) =>
        gold.ToDictionary(note => note.Id, make);

    private static ScoreReport Run(List<GoldNote> gold, Func<GoldNote, NoteReading> make) =>
        Scorer.Score(gold, Readings(gold, make), Readings(gold, make));

    private static ScoreLine Line(ScoreReport report, string measure, string part, string setting = Scorer.TypeGiven) =>
        report.Lines.Single(line => line.Measure == measure && line.Part == part && line.Setting == setting);

    /// <summary>The measures with a target, and the role line: the ones a planted error must move one at a time.</summary>
    private static Dictionary<string, (int, int, int)> Primary(ScoreReport report) =>
        report.Lines.Where(line => Scorer.Targets.Any(t => t.Measure == line.Measure) || line.Measure.StartsWith("slot ", StringComparison.Ordinal)
                || line.Measure == "plaintiff's role")
            .ToDictionary(line => $"{line.Measure} / {line.Part} / {line.Setting}", line => (line.Total, line.Answered, line.Right));

    [Fact]
    public void APerfectReaderScoresFullyEverywhereAndAnEmptyOneScoresNothingWithFullAbstention()
    {
        var gold = Gold();
        var perfect = Run(gold, Perfect);
        Assert.All(perfect.Lines.Where(line => line.Answered > 0), line => Assert.Equal(line.Answered, line.Right));
        Assert.All(perfect.Lines.Where(line => line.HasCoverage && line.Total > 0), line => Assert.Equal(line.Total, line.Answered));
        Assert.Equal(1.0, Scorer.TuningScore(gold, Readings(gold, Perfect), Readings(gold, Perfect)));

        var empty = Run(gold, _ => Empty());
        Assert.All(empty.Lines.Where(line => line.HasCoverage), line => Assert.Equal(0, line.Answered));
        Assert.Equal(0, Line(empty, "outside recognised", "outside notes", Scorer.EndToEnd).Right);
        Assert.Equal(1, empty.Counts["outside: abstentions on outside notes"]);
        var missing = Scorer.Score(gold, new Dictionary<string, NoteReading>(), new Dictionary<string, NoteReading>());
        Assert.Equal(3, missing.AnswersMissing);
    }

    public static TheoryData<string, string[]> Plants() => new()
    {
        { "flipped polarity", ["polarity / mentioned / type given", "polarity / mentioned / end to end"] },
        { "missed event", ["polarity / mentioned / type given", "polarity / mentioned / end to end"] },
        { "wrong slot", ["slot contract_date / all notes / type given", "slot contract_date / known values / type given",
                         "slot contract_date / all notes / end to end", "slot contract_date / known values / end to end"] },
        { "right value, wrong role", ["dates and amounts / recall / type given", "dates and amounts / recall / end to end"] },
        { "missed request", ["requests / pairs / type given", "requests / pairs / end to end"] },
        { "outside called inside", ["outside recognised / outside notes / end to end"] },
        { "abstention on an outside note", ["outside recognised / outside notes / end to end"] },
        { "event under the wrong claim type", ["polarity / mentioned / type given", "polarity / mentioned / end to end",
                                               "polarity / absent / type given", "polarity / absent / end to end"] },  // it also answers the other claim's element
        { "wrong role", ["plaintiff's role / claim types with a choice / type given"] },
    };

    [Theory]
    [MemberData(nameof(Plants))]
    public void OnePlantedErrorMovesExactlyTheMeasureItShould(string plant, string[] expected)
    {
        var gold = Gold();
        NoteReading Planted(GoldNote note)
        {
            var r = Perfect(note);
            return (plant, note.Id) switch
            {
                ("flipped polarity", "n1") => r with { Events = [.. r.Events.Select(e => e.Kind == "payment_made" ? e with { Polarity = Polarity.Stated } : e)] },
                ("missed event", "n1") => r with { Events = [.. r.Events.Where(e => e.Kind != "debt_unpaid")] },
                ("wrong slot", "n1") => r with { Slots = [.. r.Slots.Select(s => s.Slot == "contract_date" ? s with { Value = "2025-01-11" } : s)] },
                ("right value, wrong role", "n1") => r with { Values = [.. r.Values.Select(v => v.Kind == ValueKind.Amount ? v with { Roles = ["debt_unpaid"] } : v)] },
                ("missed request", "n1") => r with { Requests = [] },
                ("outside called inside", "n3") => r with { Claim = new ClaimReading { Abstained = false, Answers = [new ClaimGuess { ClaimId = "loan-repayment", Confidence = 0.6 }] } },
                ("abstention on an outside note", "n3") => r with { Claim = new ClaimReading { Abstained = true, Answers = [] } },
                ("event under the wrong claim type", "n2") => r with
                {
                    Events = [.. r.Events.Select(e => e.Claim == "contract-rescission" && e.Kind == "notice_served" ? e with { Claim = "unpaid-rent" } : e)
                        .Where(e => !(e.Claim == "unpaid-rent" && e.Kind == "notice_served" && e.Polarity == Polarity.NotMentioned))],
                },
                ("wrong role", "n2") => r with { Claim = r.Claim with { Answers = [.. r.Claim.Answers.Select(a => a.Role is null ? a : a with { Role = a.Role with { Position = "seller" } })] } },
                _ => r,
            };
        }

        var before = Primary(Run(gold, Perfect));
        var after = Primary(Run(gold, Planted));
        var moved = before.Keys.Where(key => before[key] != after[key]).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(expected.Order(StringComparer.Ordinal), moved);
    }

    [Fact]
    public void AFactWrittenTwiceMayBeQuotedAtEitherPlaceSinceScorer11()
    {
        // Guide 1.3: the reader quotes the second place of the same words, and of the same amount written twice.
        const string facts = "لم يرد المبلغ وقدره 500 دج. أقرض المدعي المدعى عليه مبلغا. لم يرد المبلغ وقدره 500 دج.";
        var first = facts.IndexOf("لم يرد المبلغ", StringComparison.Ordinal);
        var second = facts.LastIndexOf("لم يرد المبلغ", StringComparison.Ordinal);
        var amount1 = facts.IndexOf("500 دج", StringComparison.Ordinal);
        var amount2 = facts.LastIndexOf("500 دج", StringComparison.Ordinal);
        JsonObject Q(int start, int length) => new() { ["field"] = "facts", ["quote"] = facts.Substring(start, length), ["start"] = start, ["length"] = length };
        var note = GoldNote.Parse(new JsonObject
        {
            ["id"] = "t1", ["set"] = "dev", ["text"] = new JsonObject { ["facts"] = facts },
            ["provenance"] = new JsonObject { ["writer"] = new JsonObject { ["family"] = "openai", ["model"] = "m", ["date"] = "d" } },
            ["status"] = "kept",
            ["gold"] = new JsonObject
            {
                ["claims"] = new JsonArray("loan-repayment"), ["claimEvidence"] = new JsonArray(Q(first, 13)), ["roles"] = new JsonArray(),
                ["events"] = new JsonArray(new JsonObject
                {
                    ["claim"] = "loan-repayment", ["kind"] = "debt_unpaid", ["polarity"] = "stated", ["actor"] = "defendant",
                    ["evidence"] = new JsonArray(Q(first, 13)),
                }),
                ["slots"] = new JsonArray(),
                ["requests"] = new JsonArray(new JsonObject { ["kind"] = "payment", ["evidence"] = new JsonArray(Q(amount1, 6)) }),
                ["values"] = new JsonArray(
                    new JsonObject { ["kind"] = "amount", ["at"] = Q(amount1, 6), ["normalized"] = "500", ["roles"] = new JsonArray("debt_unpaid", "payment") },
                    new JsonObject { ["kind"] = "amount", ["at"] = Q(amount2, 6), ["normalized"] = "500", ["roles"] = new JsonArray("payment") }),
            },
        }.ToJsonString());
        List<GoldNote> gold = [note];
        NoteReading Second(GoldNote n)
        {
            var r = Perfect(n);
            return r with
            {
                Events = [.. r.Events.Select(e => e with { Evidence = [new TextSpan(NoteField.Facts, second, 13)] })],
                Requests = [.. r.Requests.Select(q => q with { Evidence = [new TextSpan(NoteField.Facts, amount2, 6)] })],
            };
        }

        var readings = Readings(gold, Second);
        var old = Scorer.Score(gold, readings, readings, evidenceRule: "1.0");
        var now = Scorer.Score(gold, readings, readings);
        // The two second places lie in another sentence: scorer 1.0 counts both unsupported, 1.1 both supported.
        Assert.Equal(1, Line(old, "evidence", "right answers with words").Right);  // only the claim, quoted at its own place
        Assert.Equal(Line(now, "evidence", "right answers with words").Answered, Line(now, "evidence", "right answers with words").Right);
        Assert.Equal(Primary(old), Primary(now));
        Assert.Throws<ScoreException>(() => Scorer.Score(gold, readings, readings, evidenceRule: "2.0"));
    }

    [Fact]
    public void ARightAnswerWithItsSpanElsewhereMovesOnlyTheEvidenceLine()
    {
        var gold = Gold();
        NoteReading Elsewhere(GoldNote note)
        {
            var r = Perfect(note);
            return note.Id == "n1" ? r with { Requests = [.. r.Requests.Select(q => q with { Evidence = [new TextSpan(NoteField.Subject, 0, 2)] })] } : r;
        }

        var before = Run(gold, Perfect);
        var after = Run(gold, Elsewhere);
        Assert.Equal(Primary(before), Primary(after));
        Assert.Equal(Line(before, "evidence", "right answers with words").Right - 1, Line(after, "evidence", "right answers with words").Right);
    }

    [Fact]
    public void AnAbstentionLeavesAccuracyOnAnswersAsItWasLowersCoverageAndIsWrongInTheThirdFigure()
    {
        var gold = Gold();
        NoteReading Abstains(GoldNote note)
        {
            var r = Perfect(note);
            return note.Id == "n1" ? r with { Events = [.. r.Events.Select(e => e.Kind == "money_lent" ? e with { Abstained = true } : e)] } : r;
        }

        var line = Line(Run(gold, Abstains), "polarity", "mentioned");
        Assert.Equal(1.0, line.Rate);
        Assert.True(line.Coverage < 1.0);
        Assert.True(line.RateCountingAbstentionsWrong < 1.0);
        var polarity = Run(gold, Abstains).Targets.Single(t => t.Target.Measure == "polarity");
        Assert.Equal(Verdict.NotMetCoverage, polarity.Verdict);  // 4 of 5 mentioned answered: 80 % under the 90 % floor
    }

    [Theory]
    [InlineData(900, 0.95, 868, 842)]
    [InlineData(200, 0.95, 197, 183)]
    [InlineData(100, 0.90, 96, 84)]
    [InlineData(1100, 0.90, 1010, 970)]
    [InlineData(2000, 0.95, 1920, 1880)]
    [InlineData(2000, 0.90, 1827, 1773)]
    [InlineData(600, 0.95, 581, 559)]
    [InlineData(50, 0.95, null, 44)]
    [InlineData(360, 0.95, 351, 333)]
    [InlineData(80, 0.95, 80, 72)]
    [InlineData(40, 0.90, 40, 32)]
    [InlineData(59, 0.95, null, 52)]
    public void EveryRowOfTheSizeTable(int n, double target, int? metFrom, int? notMetUpTo) =>
        Assert.Equal((metFrom, notMetUpTo), Interval.Thresholds(n, target));

    [Fact]
    public void TheIntervalAndTheThreeVerdictsOnHandComputedCases()
    {
        var (low, high) = Interval.Wilson(197, 200);
        Assert.Equal(0.9568, low, 3);
        Assert.Equal(0.9949, high, 3);
        Assert.Equal(Verdict.Met, Interval.Of(197, 200, 0.95));
        Assert.Equal(Verdict.NotMet, Interval.Of(183, 200, 0.95));
        Assert.Equal(Verdict.Inconclusive, Interval.Of(190, 200, 0.95));
        Assert.Equal(Verdict.NotMeasurable, Interval.Of(0, 0, 0.95));
        Assert.Null(Interval.NeededToDecide(190, 200, 0.95));  // 95 % exactly: no size decides it
        Assert.NotNull(Interval.NeededToDecide(195, 200, 0.95));
    }

    [Fact]
    public void TheSummaryHoldsNoNoteTextAndNoNoteId()
    {
        var gold = Gold();
        var report = Run(gold, Perfect);
        var markdown = Summary.Markdown(report, "dev", "hash");
        var json = Summary.Json(report, "dev", "hash");
        foreach (var output in new[] { markdown, json })
        {
            Assert.DoesNotContain(Marker, output, StringComparison.Ordinal);
            Assert.DoesNotContain("أقرض", output, StringComparison.Ordinal);
            Assert.DoesNotContain("\"n1\"", output, StringComparison.Ordinal);
            Assert.DoesNotContain("| n1 ", output, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheSameInputsGiveTheSameBytesAndTheThreeDateFormsAreRead()
    {
        var gold = Gold();
        Assert.Equal(Summary.Markdown(Run(gold, Perfect), "dev", "h"), Summary.Markdown(Run(gold, Perfect), "dev", "h"));
        Assert.Equal(Scorer.Normal("2025-01"), Scorer.Normal("2025-01"));
        foreach (var date in new[] { "2025-01-10", "2025-01", "2025" })
        {
            var note = GoldNote.Parse(Note("d", ["loan-repayment"], [], values: [Value("date", date, 40)]));
            var report = Scorer.Score([note], Readings([note], Perfect), Readings([note], Perfect));
            Assert.Equal(1, Line(report, "dates and amounts", "recall").Right);
        }
    }

    [Fact]
    public void TheTuningScoreComputedByHand()
    {
        // Six made-up readings over the three notes: claims 2 of 3 right (n3 answered loan-repayment); polarity shares
        // stated 3/3, denied 0/1 (read stated), disputed 1/1, notMentioned 2/2 -> mean 0.75; slots known 1/2
        // (contract_date wrong), unknown 1/1 -> 0.75; requests: gold {n1 payment, n2 rescission}, answered {n1 payment}
        // -> F1 = 2*1/(2+1) = 0.6667. Mean: (0.6667 + 0.75 + 0.75 + 0.6667) / 4 = 0.7083.
        var gold = Gold();
        NoteReading Made(GoldNote note)
        {
            var r = Perfect(note);
            return note.Id switch
            {
                "n1" => r with
                {
                    Events = [.. r.Events.Select(e => e.Kind == "payment_made" ? e with { Polarity = Polarity.Stated } : e)],
                    Slots = [.. r.Slots.Select(s => s.Slot == "contract_date" ? s with { Value = "2024-01-10" } : s)],
                },
                "n2" => r with { Requests = [] },
                "n3" => r with { Claim = new ClaimReading { Abstained = false, Answers = [new ClaimGuess { ClaimId = "loan-repayment", Confidence = 0.6 }] } },
                _ => r,
            };
        }

        Assert.Equal(0.7083, Scorer.TuningScore(gold, Readings(gold, Made), Readings(gold, Made)), 4);
    }

    [Fact]
    public void AnAnswerForANoteNotInTheGoldOrTwiceIsAFixedError()
    {
        var folder = Path.Combine(Path.GetTempPath(), "scorer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var reading = System.Text.Json.JsonSerializer.Serialize(new AnswerLine { Id = "zz", Reading = Empty() }, OneLine);
            File.WriteAllText(Path.Combine(folder, "a.jsonl"), reading + "\n");
            var unknown = Assert.Throws<ScoreException>(() => Answers.Load(Path.Combine(folder, "a.jsonl"), new HashSet<string> { "n1" }));
            Assert.Equal("A03", unknown.Code);
            Assert.DoesNotContain("zz", unknown.Message, StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(folder, "b.jsonl"), reading + "\n" + reading + "\n");
            Assert.Equal("A04", Assert.Throws<ScoreException>(() => Answers.Load(Path.Combine(folder, "b.jsonl"), new HashSet<string> { "zz" })).Code);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
