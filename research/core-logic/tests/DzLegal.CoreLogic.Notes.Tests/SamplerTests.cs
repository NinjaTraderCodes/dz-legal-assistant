using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DzLegal.CoreLogic.Notes.Sampler;

namespace DzLegal.CoreLogic.Notes.Tests;

/// <summary>The scenario sampler's tests (specs/scenario-sampler.md, "Tests"), on 10,000 draws of the real seed files.</summary>
public sealed partial class SamplerTests
{
    private static readonly Lazy<(SamplerData Data, ScenarioSampler Sampler, IReadOnlyList<JsonObject> Draws)> Big = new(() =>
    {
        var files = NotesFiles.Find(null);
        var data = SamplerData.Load(files);
        var sampler = new ScenarioSampler(data, SampleCommand.FinalRecords(files));
        return (data, sampler, sampler.Draw(new SampleRequest("test", 10_000, 4242, Groups: 200)));
    });

    private static SamplerData Data => Big.Value.Data;

    private static IReadOnlyList<JsonObject> Draws => Big.Value.Draws;

    private static IEnumerable<JsonObject> Events(JsonObject s) => s["events"]?.AsArray().Select(e => e!.AsObject()) ?? [];

    [Fact]
    public void TheSameSeedGivesTheSameScenarios()
    {
        var files = NotesFiles.Find(null);
        var data = SamplerData.Load(files);
        string Run() => string.Concat(new ScenarioSampler(data, SampleCommand.FinalRecords(files))
            .Draw(new SampleRequest("same", 300, 77, Groups: 5)).Select(s => s.ToJsonString()));
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void TheRoleSexDefectIsFoundFromTheScenarioAlone()
    {
        JsonObject S(string gender, params (string Claim, string Position)[] roles) => new()
        {
            ["roles"] = new JsonObject(roles.Select(r => new KeyValuePair<string, JsonNode?>(r.Claim, new JsonObject { ["relation"] = "marriage", ["position"] = r.Position }))),
            ["cast"] = new JsonObject { ["plaintiff"] = new JsonObject { ["gender"] = gender } },
        };
        Assert.True(RefillCommand.RoleSexDefect(S("f", ("custody", "father"))));
        Assert.True(RefillCommand.RoleSexDefect(S("m", ("tatliq", "wife"), ("custody", "father"))));
        Assert.False(RefillCommand.RoleSexDefect(S("f", ("custody", "mother"))));
        Assert.False(RefillCommand.RoleSexDefect(S("f", ("custody", "other"))));
        Assert.False(RefillCommand.RoleSexDefect(S("m", ("contract-rescission", "other"))));
    }

    [Fact]
    public void TheTwoHalvesAlternateByFamilyAndNeverSplitAGroup()
    {
        var files = NotesFiles.Find(null);
        var draws = new ScenarioSampler(SamplerData.Load(files), SampleCommand.FinalRecords(files))
            .Draw(new SampleRequest("halves", 120, 91, Groups: 4));
        var writerOf = SampleCommand.WriterOf(draws, new Dictionary<string, string> { ["--half"] = "codex,sonnet" });
        var families = draws.Select(s => (string)s["family"]!).Distinct().ToList();
        Assert.All(families.Select((f, i) => (f, i)), x => Assert.Equal(x.i % 2 == 0 ? "codex" : "sonnet", writerOf[x.f]));
        Assert.All(draws.Where(s => s["group"] is not null).GroupBy(s => (string)s["group"]!),
            g => Assert.Single(g.Select(s => writerOf[(string)s["family"]!]).Distinct()));
        Assert.Throws<ArgumentException>(() => SampleCommand.WriterOf(draws, new Dictionary<string, string> { ["--half"] = "codex,codex" }));
        Assert.All(SampleCommand.WriterOf(draws, new Dictionary<string, string>()).Values, w => Assert.Equal("codex", w));
    }

    [Fact]
    public void EveryDrawnCellIsFilledAndNoOtherCell()
    {
        var seen = Draws.SelectMany(Events).Select(e => $"{e["claim"]}/{e["kind"]}/{e["polarity"]}").ToHashSet();
        foreach (var claim in Data.Claims)
        {
            foreach (var e in claim.Elements)
            {
                var key = $"{claim.Id}/{e.Kind}";
                foreach (var polarity in new[] { "stated", "disputed", "notMentioned" })
                {
                    Assert.Contains($"{key}/{polarity}", seen);
                }

                Assert.Equal(Data.DeniedDrawn[key], seen.Contains($"{key}/denied"));
            }
        }
    }

    [Fact]
    public void EverySlotReadFromTheNoteTakesEveryValue()
    {
        foreach (var slot in Data.Slots)
        {
            var values = Draws.Select(s => (string?)s["slots"]?[slot.Id]).Where(v => v is not null).ToHashSet();
            var expected = slot.Type switch
            {
                "yesNoUnknown" => new[] { "yes", "no", "unknown" },
                "choice" => [.. slot.Choices, "unknown"],
                _ => ["unknown"],
            };
            foreach (var value in expected)
            {
                Assert.True(values.Contains(value), $"{slot.Id} never takes {value}");
            }

            if (slot.Type == "date")
            {
                Assert.Contains(values, v => DateOnly.TryParseExact(v, "yyyy-MM-dd", out _));
            }
        }
    }

    [Fact]
    public void EveryScenarioIsCoherent()
    {
        foreach (var s in Draws)
        {
            var claims = s["claims"]!.AsArray().Select(c => (string)c!).ToList();
            var petition = (string)s["petitionDate"]!;
            var slots = s["slots"]!.AsObject();
            if (claims[0] == "outside")
            {
                Assert.NotNull((string?)s["outsideKind"]);
                Assert.All(slots, kv => Assert.Equal("unknown", (string?)kv.Value));
                continue;
            }

            var sexes = (s["roles"]?.AsObject() ?? []).Select(kv => ScenarioSampler.SexOf(kv.Key, (string)kv.Value!["position"]!))
                .Where(x => x is not null).Distinct().ToList();
            Assert.True(sexes.Count <= 1, $"{s["id"]}: the roles give the plaintiff two sexes");
            if (sexes.Count == 1 && (string?)s["cast"]!["plaintiff"]!["gender"] is { } gender)
            {
                Assert.Equal(sexes[0], gender);
            }

            foreach (var claim in claims)
            {
                var kinds = Events(s).Where(e => (string)e["claim"]! == claim).Select(e => (string)e["kind"]!).ToList();
                Assert.Equal(Data.ClaimById[claim].Elements.Select(e => e.Kind), kinds);
                if (kinds.Count > 1)
                {
                    Assert.Contains(Events(s), e => (string)e["claim"]! == claim && (string)e["polarity"]! != "notMentioned");
                }
            }

            foreach (var e in Events(s))
            {
                Assert.True(e["date"] is null || string.CompareOrdinal((string)e["date"]!, petition) < 0);
                Assert.True((string)e["polarity"]! is not ("denied" or "notMentioned") || (e["date"] is null && e["amount"] is null));
                Assert.Equal((string)e["polarity"]! == "disputed", e["disputedBy"] is not null);
            }

            string Slot(string id) => (string?)slots[id] ?? "unknown";
            Assert.True(Slot("notice_served") == "yes" || (Slot("notice_date") == "unknown" && Slot("notice_means") == "unknown"));
            Assert.True(Slot("nonconciliation_record") == "yes" || Slot("nonconciliation_record_delivered_on") == "unknown");
            Assert.True(Slot("contract_date") == "unknown" ||
                        claims.Any(c => Data.ClaimById[c].Elements.Any(e => e.Kind == "contract_concluded")));
            Assert.All(claims, c => Assert.Contains(s["requests"]!.AsArray(), r => (string)r!["kind"]! == Data.ClaimById[c].Requests[0].Kind));

            // Sampler 1.3 (the owner, 17 November 2026): no khul, tatliq or husband's divorce beside an earlier divorce.
            Assert.False(ScenarioSampler.DissolutionBesideEarlierDivorce(s), $"{s["id"]}: a dissolution beside an earlier divorce");
        }
    }

    [Fact]
    public void ADissolutionBesideAnEarlierDivorceIsFoundAndTheRulesPairsStillOccur()
    {
        JsonObject S(string polarity, params string[] claims) => new()
        {
            ["claims"] = new JsonArray([.. claims.Select(c => (JsonNode)c)]),
            ["events"] = new JsonArray(new JsonObject { ["claim"] = "custody", ["kind"] = "divorce", ["polarity"] = polarity }),
        };
        Assert.True(ScenarioSampler.DissolutionBesideEarlierDivorce(S("stated", "khul", "custody")));
        Assert.True(ScenarioSampler.DissolutionBesideEarlierDivorce(S("disputed", "tatliq", "custody")));
        Assert.True(ScenarioSampler.DissolutionBesideEarlierDivorce(S("stated", "divorce-by-husband", "custody")));
        Assert.False(ScenarioSampler.DissolutionBesideEarlierDivorce(S("notMentioned", "khul", "custody")));
        Assert.False(ScenarioSampler.DissolutionBesideEarlierDivorce(S("denied", "tatliq", "custody")));
        Assert.False(ScenarioSampler.DissolutionBesideEarlierDivorce(S("stated", "nafaqa", "custody")));

        // The pairs of a dissolution with custody are still drawn: only their earlier divorce is excluded.
        Assert.Contains(Draws, s => s["claims"]!.AsArray().Select(c => (string)c!).Order().SequenceEqual(new[] { "custody", "khul" }));
        Assert.Contains(Draws, s => s["claims"]!.AsArray().Select(c => (string)c!).Order().SequenceEqual(new[] { "custody", "tatliq" }));
    }

    [Fact]
    public void TheMixMatchesTheRecipe()
    {
        double Share(Func<JsonObject, bool> test) => Draws.Count(test) / (double)Draws.Count;
        var outside = Share(s => (string)s["claims"]![0]! == "outside");
        var two = Share(s => s["claims"]!.AsArray().Count == 2);
        Assert.InRange(outside, 0.09, 0.15);
        Assert.InRange(two, 0.09, 0.15);
        var nearMiss = Draws.Where(s => (string)s["claims"]![0]! == "outside").Count(s => s["resembles"] is not null);
        Assert.True(nearMiss >= Draws.Count(s => (string)s["claims"]![0]! == "outside") / 2.0, "at least half the outside notes are near-misses");
        var mentioned = Draws.SelectMany(Events).Where(e => (string)e["polarity"]! != "notMentioned").ToList();
        Assert.InRange(mentioned.Count(e => (string)e["polarity"]! == "disputed") / (double)mentioned.Count, 0.10, 0.25);
        Assert.InRange(mentioned.Count(e => (string)e["polarity"]! == "denied") / (double)mentioned.Count, 0.08, 0.25);
        Assert.Contains(Draws.SelectMany(Events), e => e["contest"]?["of"]?.ToString() == "amount");
        Assert.Contains(Draws.SelectMany(Events), e => e["contest"]?["of"]?.ToString() == "cause");
        var groups = Draws.Where(s => s["group"] is not null).GroupBy(s => (string)s["group"]!).ToList();
        Assert.Equal(200, groups.Count);
        Assert.All(groups, g => Assert.InRange(g.Count(), 3, 4));
        Assert.All(groups, g => Assert.Single(g.Select(s => (string)s["family"]!).Distinct()));
    }

    /// <summary>No style parameter tells a label: the mutual information of each parameter with the claim type, the pooled
    /// polarity of the events, and each slot's value stays under 0.02 bits (specs/scenario-sampler.md, "Tests"). The
    /// estimate is corrected for its known upward bias (Miller and Madow: the plug-in estimate of a quantity that is zero by
    /// construction still averages (cells − rows − columns + 1) / (2 n ln 2), about 0.014 bits for 33 claim sets × 7 values
    /// at 10,000 draws).</summary>
    [Fact]
    public void NoStyleParameterPredictsALabel()
    {
        var cards = Data.Cards.ToDictionary(c => c.Id);
        var largest = 0.0;
        foreach (var parameter in StyleCards.Names)
        {
            string P(JsonObject s) => cards[(string)s["style"]!].Parameters[parameter];
            var pairs = new List<(string, string)>();
            pairs.AddRange(Draws.Select(s => (P(s), string.Join("+", s["claims"]!.AsArray().Select(c => (string)c!)))));
            largest = Math.Max(largest, MutualInformation(pairs));
            largest = Math.Max(largest, MutualInformation([.. Draws.SelectMany(s => Events(s).Select(e => (P(s), (string)e["polarity"]!)))]));
            foreach (var slot in Data.Slots)
            {
                largest = Math.Max(largest, MutualInformation([.. Draws.Where(s => s["slots"]?[slot.Id] is not null)
                    .Select(s => (P(s), Regex.IsMatch((string)s["slots"]![slot.Id]!, @"^\d{4}-") ? "date" : (string)s["slots"]![slot.Id]!))]));
            }
        }

        Assert.True(largest < 0.02, $"the largest mutual information is {largest:0.0000} bits");
    }

    private static double MutualInformation(List<(string A, string B)> pairs)
    {
        var n = (double)pairs.Count;
        var a = pairs.GroupBy(p => p.A).ToDictionary(g => g.Key, g => g.Count() / n);
        var b = pairs.GroupBy(p => p.B).ToDictionary(g => g.Key, g => g.Count() / n);
        var bias = (pairs.Distinct().Count() - a.Count - b.Count + 1) / (2 * n * Math.Log(2));
        return -bias + pairs.GroupBy(p => p).Sum(g =>
        {
            var pab = g.Count() / n;
            return pab * Math.Log2(pab / (a[g.Key.A] * b[g.Key.B]));
        });
    }

    [Fact]
    public void NoNameOfTheCastIsARefusedName()
    {
        var refused = SamplerData.Strings(Data.Cast["refused"]).ToHashSet();
        Assert.DoesNotContain(Big.Value.Sampler.AllNames(), refused.Contains);
        Assert.True(SamplerData.Strings(Data.Cast["maleGiven"]).Count + SamplerData.Strings(Data.Cast["femaleGiven"]).Count >= 400);
        Assert.True(SamplerData.Strings(Data.Cast["family"]).Count + SamplerData.Strings(Data.Cast["maleGiven"]).Count >= 600);
        Assert.True(SamplerData.Strings(Data.Cast["companyFirst"]).Count * SamplerData.Strings(Data.Cast["companySecond"]).Count >= 300);
        Assert.Equal(58, SamplerData.Strings(Data.Cast["wilayas"]).Distinct().Count());
        foreach (var list in new[] { "maleGiven", "femaleGiven", "family", "wilayas", "companyFirst", "companySecond" })
        {
            Assert.All(SamplerData.Strings(Data.Cast[list]), name => Assert.DoesNotMatch("[A-Za-z]", name));
        }
    }

    [Fact]
    public void TheSeedFilesCoverEveryElementAndMatchTheScheme()
    {
        foreach (var claim in Data.Claims)
        {
            foreach (var e in claim.Elements)
            {
                var seeds = Data.Seeds(claim.Id, e.Kind);
                Assert.True(seeds.Concepts.Count >= 4, $"{claim.Id}/{e.Kind}: fewer than four concepts");
                Assert.NotEmpty(seeds.Disputed);
                if (Data.DeniedDrawn[$"{claim.Id}/{e.Kind}"])
                {
                    Assert.NotEmpty(seeds.Denied);
                }

                Assert.Contains(seeds.DisputedBy, new[] { "defendant", "actor", "notActor" });
            }
        }

        // SCHEME.md section 9: the pairs and the outside kinds of table B.
        var scheme = File.ReadAllText(Data.Files.Scheme);
        var pairsLine = scheme[scheme.IndexOf("the pairs the sampler draws in version 1 are", StringComparison.Ordinal)..];
        pairsLine = pairsLine[..pairsLine.IndexOf('\n', StringComparison.Ordinal)];
        var pairs = Data.Tables["pairs"]!.AsArray().Select(p => $"`{p![0]}` + `{p[1]}`").ToList();
        Assert.Equal(pairs.Count, PairPattern().Count(pairsLine));
        Assert.All(pairs, p => Assert.Contains(p, pairsLine, StringComparison.Ordinal));
        var kinds = Data.Tables["nearMiss"]!.AsObject().Select(k => k.Key).ToHashSet();
        var tableB = scheme[scheme.IndexOf("**B. Near-misses", StringComparison.Ordinal)..scheme.IndexOf("**Two claim types in one note**", StringComparison.Ordinal)];
        Assert.Equal(kinds, OutsideRow().Matches(tableB).Select(m => m.Groups[1].Value).ToHashSet());
    }

    [Fact]
    public void TheStyleCardsFileIsTheBuiltOneAndEachValueIsSpreadEvenly()
    {
        var built = StyleCards.Build();
        Assert.Equal(StyleCards.ToJson(built).ReplaceLineEndings("\n"), File.ReadAllText(Path.Combine(Data.Folder, "style-cards.json")).ReplaceLineEndings("\n"));
        Assert.Equal(StyleCards.Count, built.Count);
        var v1 = built.Where(c => c.Id.StartsWith("c-", StringComparison.Ordinal)).ToList();
        var v2 = built.Where(c => c.Id.StartsWith("s2-", StringComparison.Ordinal)).ToList();
        Assert.Equal(StyleCards.CountV1, v1.Count);
        Assert.Equal(StyleCards.CountV2, v2.Count);
        foreach (var (name, values) in StyleCards.Parameters)
        {
            var counts = v1.GroupBy(c => c.Parameters[name]).Select(g => g.Count()).ToList();
            Assert.Equal(values.Count, counts.Count);
            Assert.True(counts.Max() - counts.Min() <= 1, $"{name} is not spread evenly");
            if (name != "length")
            {
                var counts2 = v2.GroupBy(c => c.Parameters[name]).Select(g => g.Count()).ToList();
                Assert.True(counts2.Max() - counts2.Min() <= 1, $"{name} is not spread evenly in version 2");
            }
        }

        // Version 2: shorter (five cards in eight very short or short), and the two added parameters spread evenly; a
        // version-1 card keeps its text and takes their neutral values.
        Assert.Equal(50, v2.Count(c => c.Parameters["length"] is "veryShort" or "short"));
        foreach (var (name, neutral, values) in StyleCards.Added)
        {
            Assert.All(v1, c => Assert.Equal(neutral, c.Parameters[name]));
            var counts = v2.GroupBy(c => c.Parameters[name]).Select(g => g.Count()).ToList();
            Assert.Equal(values.Count, counts.Count);
            Assert.True(counts.Max() - counts.Min() <= 1, $"{name} is not spread evenly in version 2");
        }

        Assert.DoesNotContain(v1, c => c.Text.Contains("formula", StringComparison.Ordinal) || c.Text.Contains("tidy", StringComparison.Ordinal));

        foreach (var card in built)
        {
            foreach (var claim in Data.Claims)
            {
                Assert.DoesNotContain(claim.Id, card.Text, StringComparison.Ordinal);
            }

            Assert.DoesNotMatch(@"\b(stated|denied|disputed|notMentioned)\b", card.Text);
        }
    }

    [GeneratedRegex(@"`[a-z-]+` \+ `[a-z-]+`")]
    private static partial Regex PairPattern();

    [GeneratedRegex(@"^\| `([a-z-]+)` \|", RegexOptions.Multiline)]
    private static partial Regex OutsideRow();
}
