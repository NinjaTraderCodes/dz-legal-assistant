using System.Text.Json;

namespace DzLegal.Drafting.Tests;

public sealed class StructuredFactReaderTests
{
    private static readonly CaseText Lease = CaseText.From(new Dictionary<string, string>
    {
        ["notes.facts"] = "أبرم الطرفان عقد إيجار شقة بتاريخ 15/02/2024.\nبدأ سريان الإيجار بتاريخ 01/03/2024 ببدل شهري قدره 35.000 دج.\nيعمل المدعى عليه منذ 2016.",
        ["notes.requests"] = "إلزام المدعى عليه بدفع مبلغ 210.000 دج.",
    });

    [Fact]
    public void EveryDateAndAmountIsNumberedInReadingOrderAndAYearIsADate()
    {
        var values = StructuredFactReader.Values(Lease);
        Assert.Equal(
            ["v1=15/02/2024 date s1", "v2=01/03/2024 date s2", "v3=35000 amount s2", "v4=2016 date s3", "v5=210000 amount s4"],
            values.Select(v => $"{v.Id}={v.Normalized} {v.Kind} {v.SentenceId}"));
    }

    [Fact]
    public void TheSchemaOnlyAllowsThisCasesSentencesValuesAndCodes()
    {
        var request = new StructuredFactReader(new RecordingProvider(_ => "{}"), "{{ case }}\n{{ values }}").BuildRequest(Lease);
        var schema = request.OutputSchema!.Value;
        var ev = schema.GetProperty("properties").GetProperty("events").GetProperty("items").GetProperty("properties");
        Assert.Equal(["none", "v1", "v2", "v4"], ev.GetProperty("date").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(["none", "v3", "v5"], ev.GetProperty("amount").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(["s1", "s2", "s3", "s4"], ev.GetProperty("sentences").GetProperty("items").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("lease_started", ev.GetProperty("kind").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("v2 = 01/03/2024 (تاريخ، في s2)", request.UserPrompt, StringComparison.Ordinal);
        Assert.Equal(StructuredFactReader.MaxTokens, request.MaxTokens);
    }

    [Fact]
    public void ValueNumbersAreResolvedAndCheckedLikeTheCodedLines()
    {
        const string answer = """
            ```json
            {"relation":"lease","position":"lessor","relation_sentences":["s1"],
             "events":[{"kind":"contract_concluded","sentences":["s1"],"actor":"none","polarity":"affirmed","date":"v1","amount":"none"},
                       {"kind":"lease_started","sentences":["s1"],"actor":"none","polarity":"affirmed","date":"v2","amount":"v3"}],
             "requests":[{"kind":"payment","sentences":["s4"],"amount":"v5"}]}
            ```
            """;
        Assert.StartsWith("علاقة | lease | lessor | s1\nحدث | contract_concluded | 15/02/2024 | - | - | affirmed | s1", StructuredFactReader.ToLines(answer, Lease), StringComparison.Ordinal);

        var facts = StructuredFactReader.Parse(answer, Lease);
        Assert.Equal(("lease", "lessor"), (facts.Relation, facts.Position));
        Assert.Equal(ValueCheck.InCitedSentence, facts.Events[0].Date!.Check);
        Assert.Equal(ValueCheck.ElsewhereInCase, facts.Events[1].Date!.Check); // v2 is in s2, cited as s1
        Assert.Equal("210000", facts.Requests[0].Amount!.Normalized);
        Assert.Empty(facts.Rejected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("{\"relation\": ")]
    public void AnAnswerThatIsNotJsonGivesNoFacts(string answer)
    {
        var facts = StructuredFactReader.Parse(answer, Lease);
        Assert.Equal(string.Empty, facts.Relation);
        Assert.Empty(facts.Events);
    }

    [Fact]
    public void TheShippedPromptHasTheCodesTheValueListAndAValidExample()
    {
        var prompt = StructuredFactReader.LoadPrompt(Path.Combine(AppContext.BaseDirectory, "data", "law"));
        Assert.All(FactVocabulary.Events.Concat(FactVocabulary.Requests), code => Assert.Contains(code, prompt, StringComparison.Ordinal));
        var example = prompt.Split('\n').Single(l => l.StartsWith("{\"relation\"", StringComparison.Ordinal));
        using var doc = JsonDocument.Parse(example);
        Assert.Equal("seller", doc.RootElement.GetProperty("position").GetString());
        Assert.True(prompt.IndexOf("{{ values }}", StringComparison.Ordinal) > prompt.IndexOf("{{ case }}", StringComparison.Ordinal));
    }
}
