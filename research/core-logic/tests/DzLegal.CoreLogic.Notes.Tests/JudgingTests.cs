using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Judging;
using DzLegal.CoreLogic.Notes.Sampler;

namespace DzLegal.CoreLogic.Notes.Tests;

/// <summary>The blind relabel of test notes and the comparison that keeps or drops them (specs/notes-pipeline.md, 4b).</summary>
public sealed class JudgingTests
{
    private static readonly Lazy<SamplerData> Data = new(() => SamplerData.Load(NotesFiles.Find(null)));

    private static JsonObject Text() => new()
    {
        ["subject"] = "دعوى نفقة",
        ["facts"] = "تزوج المدعي والمدعى عليها في 3 أفريل 2019. ترك الزوج بيت الزوجية. ولم يدفع النفقة منذ ذلك اليوم.",
        ["requests"] = "الحكم بنفقة شهرية قدرها 20.000 دج.",
        ["documents"] = "",
    };

    private static JsonObject Q(string field, string quote, int start) =>
        new() { ["field"] = field, ["quote"] = quote, ["occurrence"] = 1, ["start"] = start, ["length"] = quote.Length };

    private static JsonObject Labels() => new()
    {
        ["claims"] = new JsonArray("maintenance"),
        ["claimEvidence"] = new JsonArray(Q("requests", "الحكم بنفقة شهرية", 0)),
        ["roles"] = new JsonArray(),
        ["events"] = new JsonArray(new JsonObject
        {
            ["claim"] = "maintenance", ["kind"] = "marriage", ["polarity"] = "stated", ["actor"] = "unknown",
            ["evidence"] = new JsonArray(Q("facts", "تزوج المدعي والمدعى عليها", 0)),
            ["date"] = new JsonObject { ["kind"] = "date", ["at"] = Q("facts", "3 أفريل 2019", 29), ["normalized"] = "2019-04-03" },
        }),
        ["slots"] = new JsonArray(),
        ["requests"] = new JsonArray(new JsonObject
        {
            ["kind"] = "maintenance", ["evidence"] = new JsonArray(Q("requests", "الحكم بنفقة شهرية", 0)),
            ["amount"] = new JsonObject { ["kind"] = "amount", ["at"] = Q("requests", "20.000 دج", 24), ["normalized"] = "20000" },
        }),
        ["values"] = new JsonArray(
            new JsonObject { ["kind"] = "date", ["at"] = Q("facts", "3 أفريل 2019", 29), ["normalized"] = "2019-04-03", ["roles"] = new JsonArray("marriage") },
            new JsonObject { ["kind"] = "amount", ["at"] = Q("requests", "20.000 دج", 24), ["normalized"] = "20000", ["roles"] = new JsonArray("maintenance") }),
    };

    [Fact]
    public void TheFixtureQuotesAreWhereTheySay()
    {
        var labels = Labels();
        Assert.Equal(0, RelabelCommand.Locate(Text(), labels));
    }

    [Fact]
    public void TheSameLabelsAgreeEverywhere() =>
        Assert.Empty(CompareCommand.Differences(Data.Value, Text(), Labels(), Labels()));

    [Fact]
    public void AnyDifferenceOrUnclearOrMissingQuoteIsADisagreement()
    {
        var other = Labels();
        other["events"]![0]!["polarity"] = "denied";
        Assert.Contains("events/maintenance/marriage/polarity", CompareCommand.Differences(Data.Value, Text(), Labels(), other));

        other = Labels();
        other["claims"] = new JsonArray("unclear");
        Assert.Equal(["claims"], CompareCommand.Differences(Data.Value, Text(), Labels(), other));

        other = Labels();
        other["requests"]![0]!["evidence"] = new JsonArray(new JsonObject { ["field"] = "requests", ["quote"] = "لا توجد", ["occurrence"] = 1 });
        RelabelCommand.Locate(Text(), other);
        Assert.Contains("requests/maintenance", CompareCommand.Differences(Data.Value, Text(), Labels(), other));

        other = Labels();
        other["values"]!.AsArray().RemoveAt(1);
        Assert.Contains("values/requests:24", CompareCommand.Differences(Data.Value, Text(), Labels(), other));
    }

    [Fact]
    public void AValueWithoutANormalFormIsNotCompared()
    {
        var other = Labels();
        other["values"]!.AsArray().Add(new JsonObject
        {
            ["kind"] = "date", ["at"] = Q("facts", "ذلك اليوم", 0), ["relative"] = true, ["roles"] = new JsonArray(),
        });
        Assert.Empty(CompareCommand.Differences(Data.Value, Text(), Labels(), other));
    }

    [Fact]
    public void ABareDateOrAmountIsReadByItsNormalForm()
    {
        var other = Labels();
        other["events"]![0]!["date"] = "2019-04-03";
        other["requests"]![0]!["amount"] = "20000";
        RelabelCommand.Normalise(other);
        Assert.Empty(CompareCommand.Differences(Data.Value, Text(), Labels(), other));
        other["events"]![0]!["date"] = "2019-04-04";
        RelabelCommand.Normalise(other);
        Assert.Contains("events/maintenance/marriage/date", CompareCommand.Differences(Data.Value, Text(), Labels(), other));
    }

    [Fact]
    public void QuotesForOneFieldMustOverlapOrShareASentence()
    {
        var other = Labels();
        other["events"]![0]!["evidence"] = new JsonArray(Q("facts", "3 أفريل 2019", 29));
        Assert.Empty(CompareCommand.Differences(Data.Value, Text(), Labels(), other));
        other["events"]![0]!["evidence"] = new JsonArray(Q("facts", "ترك الزوج بيت الزوجية", 43));
        Assert.Contains("events/maintenance/marriage/evidence", CompareCommand.Differences(Data.Value, Text(), Labels(), other));
        Assert.Equal(0, CompareCommand.Sentence("مبلغ 1.200.000 دج. ثم", 10));
        Assert.Equal(1, CompareCommand.Sentence("مبلغ 1.200.000 دج. ثم", 19));
    }

    [Fact]
    public void NotesGoToTheOtherModelAndAGroupIsNeverInOneRun()
    {
        Assert.Equal("sonnet", RelabelCommand.RelabellerOf("codex"));
        Assert.Equal("opus", RelabelCommand.RelabellerOf("sonnet"));
        Assert.Throws<InvalidDataException>(() => RelabelCommand.RelabellerOf("opus"));
        var scenarios = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var lines = new List<JsonObject>();
        for (var i = 0; i < 30; i++)
        {
            scenarios[$"s{i}"] = new JsonObject { ["group"] = i < 4 ? "g1" : null };
            lines.Add(new JsonObject { ["scenario"] = $"s{i}" });
        }

        var runs = RelabelCommand.Runs(lines, scenarios);
        Assert.Equal(30, runs.Sum(r => r.Count));
        Assert.All(runs, r => Assert.True(r.Count <= RelabelCommand.PerRun));
        Assert.All(runs, r => Assert.True(r.Count(l => (string)l["scenario"]! is "s0" or "s1" or "s2" or "s3") <= 1));
    }
}
