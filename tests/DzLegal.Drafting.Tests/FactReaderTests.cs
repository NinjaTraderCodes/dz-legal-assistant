namespace DzLegal.Drafting.Tests;

public sealed class FactReaderTests
{
    private static readonly CaseText Lease = CaseText.From(new Dictionary<string, string>
    {
        ["notes.facts"] = "أبرم المدعي مع المدعى عليه عقد إيجار شقة بتاريخ 15/02/2024. بدأ سريان الإيجار بتاريخ 01/03/2024 ببدل شهري قدره 35.000 دج.\nلم يوجه المدعي أي إعذار.",
        ["notes.requests"] = "إلزام المدعى عليه بدفع مبلغ 210.000 دج.",
        ["documents.list"] = "عقد إيجار مؤرخ في 15/02/2024",
        ["court.section"] = "القسم المدني",
    });

    [Fact]
    public void EveryFieldIsCutIntoNumberedSentencesAndTheDotInsideAnAmountStays()
    {
        Assert.Equal(["s1", "s2", "s3", "s4", "s5"], Lease.Sentences.Select(s => s.Id));
        Assert.Equal("بدأ سريان الإيجار بتاريخ 01/03/2024 ببدل شهري قدره 35.000 دج.", Lease.Sentences[1].Text);
        Assert.Equal(["notes.facts", "notes.facts", "notes.facts", "notes.requests", "documents.list"], Lease.Sentences.Select(s => s.FieldId));
        Assert.Equal("القسم المدني", Lease.CourtSection);
        Assert.StartsWith("s1 [الوقائع] أبرم", Lease.Render(), StringComparison.Ordinal);
        Assert.Contains("s5 [المستندات] عقد إيجار", Lease.Render(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1/3/2024", "01/03/2024")]
    [InlineData("٠١/٠٣/٢٠٢٤", "01/03/2024")]
    [InlineData("01-03-2024", "01/03/2024")]
    [InlineData("سنة 2015", "2015")]
    [InlineData("-", "")]
    public void DatesAreNormalized(string text, string expected) => Assert.Equal(expected, FactValues.NormalizeDate(text));

    [Theory]
    [InlineData("500.000 دج", "500000")]
    [InlineData("1,500,000", "1500000")]
    [InlineData("200 ألف دينار", "200000")]
    [InlineData("2 مليون", "2000000")]
    [InlineData("لا شيء", "")]
    public void AmountsAreNormalized(string text, string expected) => Assert.Equal(expected, FactValues.NormalizeAmount(text));

    [Fact]
    public void DatesAndAmountsOfASentenceAreFoundAndDatesAreNotAmounts()
    {
        Assert.Equal(["01/03/2024"], FactValues.DatesIn("بدأ الإيجار بتاريخ 01/03/2024 ببدل 35.000 دج"));
        Assert.Equal(["35000"], FactValues.AmountsIn("بدأ الإيجار بتاريخ 01/03/2024 ببدل 35.000 دج"));
    }

    [Fact]
    public void ValuesAreCheckedAgainstTheCitedSentences()
    {
        var parsed = FactReader.Parse("""
            علاقة | lease | lessor | s1
            حدث | contract_concluded | 15/02/2024 | - | - | affirmed | s1
            حدث | lease_started | 01/03/2024 | 35.000 | - | affirmed | s1
            حدث | notice_served | - | - | plaintiff | denied | s3
            حدث | rent_unpaid | 10/10/2024 | 999.000 | defendant | affirmed | s9
            طلب | payment | 210.000 | s4
            """, Lease);

        Assert.Equal(("lease", "lessor"), (parsed.Relation, parsed.Position));
        Assert.Equal(ValueCheck.InCitedSentence, parsed.Events[0].Date!.Check);
        Assert.Equal(ValueCheck.ElsewhereInCase, parsed.Events[1].Date!.Check); // the start date is in s2, not s1
        Assert.Equal(FactPolarity.Denied, parsed.Events[2].Polarity);
        Assert.Equal(ValueCheck.NotInCase, parsed.Events[3].Date!.Check); // invented
        Assert.Equal(ValueCheck.NotInCase, parsed.Events[3].Amount!.Check);
        Assert.Empty(parsed.Events[3].Sentences); // s9 does not exist
        Assert.Equal("210000", parsed.Requests[0].Amount!.Normalized);
        Assert.Equal(ValueCheck.InCitedSentence, parsed.Requests[0].Amount!.Check);
    }

    [Fact]
    public void UnknownCodesAndBrokenLinesAreRejectedNotGuessed()
    {
        var parsed = FactReader.Parse("""
            **علاقة | lease | landlord | s1**
            حدث | rent_missing | - | - | defendant | affirmed | s2
            حدث | lease_started | 01/03/2024
            طلب | payment | - | s4
            كلام آخر بلا رموز
            """, Lease);

        Assert.Equal(string.Empty, parsed.Relation);
        Assert.Empty(parsed.Events);
        Assert.Equal(3, parsed.Rejected.Count);
        Assert.Equal("payment", Assert.Single(parsed.Requests).Kind);
    }

    [Fact]
    public void TheShippedPromptAsksForEveryCodeAndKeepsTheCaseLast()
    {
        var prompt = FactReader.LoadPrompt(Path.Combine(AppContext.BaseDirectory, "data", "law"));
        Assert.All(FactVocabulary.Events.Concat(FactVocabulary.Relations).Concat(FactVocabulary.Positions).Concat(FactVocabulary.Requests),
            code => Assert.Contains(code, prompt, StringComparison.Ordinal));

        var request = new FactReader(new RecordingProvider(_ => string.Empty), prompt).BuildRequest(Lease);
        Assert.Contains("القسم الذي اختاره المحامي: القسم المدني", request.UserPrompt, StringComparison.Ordinal);
        Assert.True(request.UserPrompt.IndexOf("s1 [الوقائع]", StringComparison.Ordinal) > request.UserPrompt.IndexOf("مثال", StringComparison.Ordinal));
        Assert.Equal(FactReader.MaxTokens, request.MaxTokens);
    }
}
