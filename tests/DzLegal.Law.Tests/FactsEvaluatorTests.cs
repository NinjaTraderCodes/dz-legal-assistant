using DzLegal.Drafting;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class FactsEvaluatorTests
{
    private static readonly IReadOnlyList<FactCase> Cases = FactsEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "fact-cases"));

    private static FactCase Case(string name) => Cases.Single(c => c.Name == name);

    private static IReadOnlyList<FactError> Score(string name, string answer) =>
        FactsEvaluator.Score(Case(name).Gold, FactReader.Parse(answer, CaseText.From(Case(name).Inputs)));

    [Fact]
    public void TheAnswerKeyUsesOnlyKnownCodesAndCoversEveryPartition()
    {
        static IEnumerable<string> Codes(string value) => value.Split('|');
        Assert.True(Cases.Count >= 60);
        Assert.Equal(Cases.Count, Cases.Select(c => c.Name).Distinct().Count());
        Assert.All(Cases, c =>
        {
            Assert.All(Codes(c.Gold.Relation), r => Assert.Contains(r, FactVocabulary.Relations));
            Assert.All(Codes(c.Gold.Position), p => Assert.Contains(p, FactVocabulary.Positions));
            Assert.All(c.Gold.Events.SelectMany(e => Codes(e.Kind)).Concat(c.Gold.Forbidden.SelectMany(f => Codes(f.Kind))), k => Assert.Contains(k, FactVocabulary.Events));
            Assert.All(c.Gold.Events, e => Assert.Contains(e.Polarity, FactPolarity.All));
            Assert.All(c.Gold.Requests.SelectMany(r => Codes(r.Kind)), k => Assert.Contains(k, FactVocabulary.Requests));
            Assert.NotEmpty(CaseText.From(c.Inputs).Sentences);
        });
        Assert.Equal(["development", "robustness", "validation"], Cases.Select(c => c.Partition).Distinct().Order());
    }

    [Fact]
    public void EveryDateAndAmountOfTheAnswerKeyIsWrittenInItsCase()
    {
        Assert.All(Cases, c =>
        {
            var text = CaseText.From(c.Inputs);
            var dates = text.Sentences.SelectMany(s => FactValues.DatesIn(s.Text)).ToHashSet();
            var amounts = text.Sentences.SelectMany(s => FactValues.AmountsIn(s.Text)).ToHashSet();
            Assert.All(c.Gold.Events.Where(e => e.Date is not null), e => Assert.Contains(FactValues.NormalizeDate(e.Date), dates));
            Assert.All(c.Gold.Events.Where(e => e.Amount is not null).Select(e => e.Amount!).Concat(c.Gold.Requests.Where(r => r.Amount is not null).Select(r => r.Amount!)), a => Assert.Contains(a, amounts));
        });
    }

    [Fact]
    public void ACorrectReadingHasNoCriticalError()
    {
        var errors = Score("lease-start-vs-signing", """
            علاقة | lease | lessor | s1
            حدث | contract_concluded | 15/02/2024 | - | - | affirmed | s1
            حدث | lease_started | 01/03/2024 | 35.000 | - | affirmed | s2
            حدث | rent_unpaid | - | - | defendant | affirmed | s3
            حدث | notice_served | 10/12/2024 | - | plaintiff | affirmed | s3
            طلب | payment | - | s4
            طلب | eviction | - | s5
            """);
        Assert.DoesNotContain(errors, e => e.Critical);
    }

    [Fact]
    public void TheStartDateGivenAsTheSigningDateIsCritical()
    {
        var errors = Score("lease-start-vs-signing", """
            علاقة | lease | lessor | s1
            حدث | contract_concluded | 01/03/2024 | - | - | affirmed | s2
            حدث | lease_started | 01/03/2024 | - | - | affirmed | s2
            حدث | rent_unpaid | - | - | defendant | affirmed | s3
            حدث | notice_served | 10/12/2024 | - | plaintiff | affirmed | s3
            """);
        Assert.Contains(errors, e => e is { Type: "date on wrong event", Critical: true });
        Assert.Contains(errors, e => e is { Type: "forbidden reading", Critical: true });
    }

    [Fact]
    public void ADeniedFactReadAsAffirmedAReversedRoleAndAnInventedValueAreCritical()
    {
        Assert.Contains(Score("rent-no-notice", """
            علاقة | lease | lessor | s1
            حدث | rent_unpaid | - | - | defendant | affirmed | s2
            حدث | notice_served | - | - | plaintiff | affirmed | s3
            """), e => e is { Type: "forbidden reading", Critical: true });

        Assert.Contains(Score("borrower-plaintiff", """
            علاقة | loan | lender | s1
            حدث | money_lent | - | 400.000 | plaintiff | affirmed | s1
            """), e => e is { Type: "plaintiff position", Critical: true });

        Assert.Contains(Score("loan-basic", """
            علاقة | loan | lender | s1
            حدث | money_lent | 12/12/2023 | 500.000 | plaintiff | affirmed | s1
            حدث | debt_unpaid | - | - | defendant | affirmed | s3
            حدث | notice_served | - | - | plaintiff | affirmed | s3
            """), e => e is { Type: "invented value", Critical: true });
    }

    [Fact]
    public void AMissingDecisiveEventIsCriticalAndAMissingRequestIsNot()
    {
        var errors = Score("rent-basic", """
            علاقة | lease | lessor | s1
            حدث | rent_unpaid | - | - | defendant | affirmed | s2
            """);
        Assert.Contains(errors, e => e is { Type: "missing decisive event", Detail: "notice_served", Critical: true });
        Assert.Contains(errors, e => e is { Type: "missing request", Critical: false });
    }

    [Fact]
    public void ReadingsFromAnotherToolAreScoredFromAPredictionsFile()
    {
        var folder = Directory.CreateTempSubdirectory("dzlegal-facts-");
        try
        {
            var file = Path.Combine(folder.FullName, "encoder.json");
            File.WriteAllText(file, """
                {
                  "things-dog": { "relation": "none", "position": "victim", "seconds": 0.4,
                                  "events": [ { "kind": "harm_by_animal", "actor": "defendant", "sentences": ["s1"] } ],
                                  "requests": [ { "kind": "damages", "sentences": ["s2"] } ] },
                  "things-wall": { "answer": "علاقة | none | victim | s1\nحدث | harm_by_person | - | - | defendant | affirmed | s1" }
                }
                """);
            var report = FactsEvaluator.ScorePredictions([Case("things-dog"), Case("things-wall"), Case("khul")], "encoder", file);
            Assert.False(report.Results[0].HasCritical);
            Assert.True(report.Results[1].HasCritical); // the wall read as a person's act: the decisive event is missing
            Assert.Equal("no prediction", report.Results[2].Failure);
            Assert.Contains("| development | 3 | 2 |", FactsEvaluator.Summary(report), StringComparison.Ordinal);
            Assert.Contains("\"id\": \"s1\"", FactsEvaluator.Export([Case("things-dog")]), StringComparison.Ordinal);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
