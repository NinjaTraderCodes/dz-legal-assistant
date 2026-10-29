using DzLegal.Drafting;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class AlphaFactsTests
{
    private static readonly string LawRoot = Path.Combine(AppContext.BaseDirectory, "data", "law");
    private static readonly IReadOnlyDictionary<string, ClaimElements> Elements = ClaimFactReader.LoadElements(LawRoot);
    private static readonly IReadOnlyList<FactCase> Cases = FactsEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "fact-cases"));

    private static FactCase Case(string name) => Cases.Single(c => c.Name == name);

    [Fact]
    public void EveryCatalogClaimHasElementsAndEveryCaseNamesAKnownClaim()
    {
        var knowledge = LegalKnowledgeLoader.Load(LawRoot);
        Assert.Equal(knowledge.Claims.Select(c => c.Id).Order(), Elements.Keys.Order());
        Assert.All(Elements.Values, c => Assert.Empty(ClaimFactReader.Validate(c)));
        Assert.All(Cases, c => Assert.True(c.Claim == FactCase.OutOfScope || Elements.ContainsKey(c.Claim), c.Name));
    }

    /// <summary>
    /// The ceiling: for every case inside the catalog, answering the questions as the answer key reads the case gives no
    /// critical error. A role, event, value or request the answer key needs but the claim's questions cannot express
    /// would fail here, before any model is blamed for it.
    /// </summary>
    [Fact]
    public void AnsweringAsTheAnswerKeyReadsEachCaseScoresWithoutCriticalErrors()
    {
        Assert.All(Cases.Where(c => c.Claim != FactCase.OutOfScope), c =>
        {
            var claim = Elements[c.Claim];
            var text = CaseText.From(c.Inputs);
            var values = ClaimFactReader.Values(text);
            var lines = new List<string>();
            var role = claim.Roles.ToList().FindIndex(r => c.Gold.Position.Split('|').Contains(r.Position));
            if (claim.Roles.Count > 1)
            {
                lines.Add($"p | {role + 1}");
            }

            foreach (var g in c.Gold.Events)
            {
                var n = claim.Elements.ToList().FindIndex(e => g.Kind.Split('|').Contains(e.Event));
                var ids = values.Where(v => (v.Kind == CaseValue.Date && g.Date is not null && v.Normalized == FactValues.NormalizeDate(g.Date))
                    || (v.Kind == CaseValue.Amount && v.Normalized == g.Amount)).Select(v => v.Id).Take(2);
                var status = g.Polarity switch { FactPolarity.Denied => "no", FactPolarity.Disputed => "disputed", _ => "yes" };
                lines.Add($"{n + 1} | {status} | {string.Join(' ', ids)}");
            }

            foreach (var g in c.Gold.Requests)
            {
                var n = claim.Requests.ToList().FindIndex(q => g.Kind.Split('|').Contains(q.Kind));
                var id = values.FirstOrDefault(v => v.Kind == CaseValue.Amount && v.Normalized == g.Amount)?.Id ?? "-";
                lines.Add($"r{n + 1} | yes | {id}");
            }

            var errors = FactsEvaluator.Score(c.Gold, ClaimFactReader.Parse(string.Join('\n', lines), text, claim));
            Assert.True(errors.All(e => !e.Critical), $"{c.Name}: {string.Join("; ", errors.Where(e => e.Critical).Select(e => e.Type + " " + e.Detail))}");
        });
    }

    [Fact]
    public void TheRoleFollowsTheClaimTypeAndIsAskedOnlyWhenThereIsAChoice()
    {
        var khul = Elements["khul"];
        Assert.DoesNotContain("p)", ClaimFactReader.Questions(khul), StringComparison.Ordinal);
        var facts = ClaimFactReader.Parse("p | 2\n1 | absent | -", CaseText.From(Case("khul").Inputs), khul);
        Assert.Equal(("marriage", "wife"), (facts.Relation, facts.Position));

        var custody = Elements["custody"];
        Assert.StartsWith("p) من هو المدعي؟ 1 = المدعية هي الأم، 2 = المدعي هو الأب", ClaimFactReader.Questions(custody), StringComparison.Ordinal);
        Assert.Equal("father", ClaimFactReader.Parse("p | 2", CaseText.From(Case("custody-transfer").Inputs), custody).Position);
        Assert.Equal("mother", ClaimFactReader.Parse("p | 9", CaseText.From(Case("custody-transfer").Inputs), custody).Position);
    }

    [Fact]
    public void TheAnswerTemplateHasOneLinePerQuestionAndAValueSlotOnlyWhereOneIsTaken()
    {
        Assert.Equal("1 | ? | ?\nr1 | ? | ?", ClaimFactReader.Template(Elements["khul"]));
        Assert.Equal("p | ?\n1 | ? | ?\n2 | ?\nr1 | ?\nr2 | ?\nr3 | ? | ?", ClaimFactReader.Template(Elements["custody"]));
    }

    [Fact]
    public void ValuesAreTakenByNumberOnlyWhenTheyExistAndFitTheElement()
    {
        var text = CaseText.From(Case("loan-basic").Inputs);
        var values = ClaimFactReader.Values(text);
        Assert.Equal(["10/01/2024", "500000"], values.Select(v => v.Normalized));
        var loan = Elements["loan-repayment"];

        // 1 = money lent (date and amount), 5 = not repaid (amount only), 6 = notice (date only).
        var facts = ClaimFactReader.Parse("1 | yes | v2 v1\n5 | yes | v1 v9\n6 | no | -", text, loan);

        var lent = facts.Events.Single(e => e.Kind == "money_lent");
        Assert.Equal(("10/01/2024", "500000", "plaintiff"), (lent.Date?.Normalized, lent.Amount?.Normalized, lent.Actor));
        Assert.Equal(ValueCheck.InCitedSentence, lent.Amount!.Check);
        var unpaid = facts.Events.Single(e => e.Kind == "debt_unpaid");
        Assert.Null(unpaid.Date);
        Assert.Null(unpaid.Amount);
        Assert.Equal("defendant", unpaid.Actor);
        Assert.Equal(FactPolarity.Denied, facts.Events.Single(e => e.Kind == "notice_served").Polarity);
    }

    [Fact]
    public void AbsentFactsAreLeftOutAndUnreadableLinesAreKept()
    {
        var text = CaseText.From(Case("rent-basic").Inputs);
        var facts = ClaimFactReader.Parse("1 | absent | -\n3) disputed\n4 | maybe | -\n9 | yes | -\nr2 | yes\nr7 | yes | -\nThe answer is:", text, Elements["unpaid-rent"]);

        Assert.Equal(["rent_unpaid"], facts.Events.Select(e => e.Kind));
        Assert.Equal(FactPolarity.Disputed, facts.Events[0].Polarity);
        Assert.Equal(["eviction"], facts.Requests.Select(r => r.Kind));
        Assert.Equal(["4 | maybe | -", "9 | yes | -", "r7 | yes | -"], facts.Rejected);
    }

    /// <summary>The slips of the second local run (29 Oct): a status left out, a value copied out in full, yes/no.</summary>
    [Fact]
    public void CommonAnswerSlipsAreReadWithoutAcceptingAnythingNotInTheText()
    {
        var text = CaseText.From(Case("khul").Inputs);
        var facts = ClaimFactReader.Parse("1 | absent | absent\nr1 | yes | 300000", text, Elements["khul"]);
        Assert.Equal("300000", facts.Requests.Single().Amount?.Normalized);
        Assert.Null(ClaimFactReader.Parse("r1 | stated | 450000", text, Elements["khul"]).Requests.Single().Amount);

        var sale = CaseText.From(Case("annulment-fraud-land").Inputs);
        var read = ClaimFactReader.Parse("p | 1\n1 | v1 | v2\n2 | stated\nr2 | v2", sale, Elements["sale-annulment-fraud"]);
        var concluded = read.Events.Single(e => e.Kind == "contract_concluded");
        Assert.Equal((FactPolarity.Affirmed, "12/02/2026", "9000000"), (concluded.Polarity, concluded.Date?.Normalized, concluded.Amount?.Normalized));
        Assert.Equal(FactPolarity.Affirmed, read.Events.Single(e => e.Kind == "concealment").Polarity);
        Assert.Equal("restitution", read.Requests.Single().Kind);
        Assert.Equal("buyer", read.Position);

        Assert.Equal(FactPolarity.Denied, ClaimFactReader.Parse("2 | denied | -", sale, Elements["sale-annulment-fraud"]).Events.Single().Polarity);
    }

    /// <summary>The slips of the third local run (29 Oct): «s1» for question 1, a value number in the status's place.</summary>
    [Fact]
    public void AQuestionNumberedLikeASentenceAndAValueInTheStatusPlaceAreRead()
    {
        var divorce = ClaimFactReader.Parse("s1 | v1 | stated\nr1 | stated", CaseText.From(Case("divorce-husband").Inputs), Elements["divorce-by-husband"]);
        Assert.Equal(("marriage", "2015"), (divorce.Events.Single().Kind, divorce.Events.Single().Date?.Normalized));

        var loan = ClaimFactReader.Parse("6 | v6 | ?", CaseText.From(Case("loan-basic").Inputs), Elements["loan-repayment"]);
        var notice = loan.Events.Single();
        Assert.Equal(("notice_served", FactPolarity.Affirmed), (notice.Kind, notice.Polarity));
        Assert.Null(notice.Date);
    }

    [Fact]
    public async Task TheLinesWrittenBeforeTheAnswerWasCutOffAreKept()
    {
        var reader = new ClaimFactReader(new CutOff("p | 6\n1 | stated | -\n2 | sta"), ClaimFactReader.LoadPrompt(LawRoot), Elements);
        var (facts, answer) = await reader.ReadAsync(CaseText.From(Case("tenant-plaintiff").Inputs), "contract-damages", "x");

        Assert.Equal("lessee", facts.Position);
        Assert.Equal(["contract_concluded"], facts.Events.Select(e => e.Kind));
        Assert.EndsWith("[cut off at the answer length limit]", answer, StringComparison.Ordinal);
    }

    [Fact]
    public void AnActorNamedByPositionIsWhicheverPartyHoldsIt()
    {
        var claim = Elements["commercial-lease-eviction"];
        var text = CaseText.From(Case("commercial-lease-indemnity").Inputs);

        // 4 = the lessor refused renewal: the defendant when the plaintiff is the lessee, the plaintiff otherwise.
        Assert.Equal("defendant", ClaimFactReader.Parse("p | 2\n4 | yes | -", text, claim).Events.Single().Actor);
        Assert.Equal("plaintiff", ClaimFactReader.Parse("p | 1\n4 | yes | -", text, claim).Events.Single().Actor);
    }

    [Fact]
    public async Task WithTheClaimGivenCasesOutsideTheCatalogAreLeftOutAndTheOthersAreReadOnce()
    {
        var model = new Scripted("1 | yes | v1 v2\n4 | yes | -");
        var cases = new[] { Case("none-inheritance"), Case("sale-price") };
        var report = await FactsEvaluator.RunAsync(cases, "scripted", AlphaFacts.Reader(model, LawRoot, AlphaFacts.Given), TextWriter.Null);

        Assert.True(report.Results[0].Excluded);
        Assert.Equal("unpaid-sale-price", report.Results[1].Claim);
        Assert.Single(model.Requests);
        Assert.Equal(ClaimFactReader.RequestId, model.Requests[0].SectionId);
        Assert.Contains("نوع الدعوى: ", model.Requests[0].UserPrompt, StringComparison.Ordinal);
        Assert.Contains("2) سلّم المدعي المبيع", model.Requests[0].UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", model.Requests[0].UserPrompt, StringComparison.Ordinal);
        Assert.EndsWith("1 | ? | ?\n2 | ? | ?\n3 | ? | ?\n4 | ? | ?\n5 | ? | ?\nr1 | ? | ?\nr2 | ? | ?", model.Requests[0].UserPrompt.TrimEnd(), StringComparison.Ordinal);
        Assert.Contains("1 case(s) outside the claim catalogue", FactsEvaluator.Summary(report), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithTheClaimReadAWrongClaimTypeIsCriticalAndAnOutOfScopeCaseMustBeLeftAlone()
    {
        var model = new Scripted("نوع الدعوى: loan-repayment");
        var report = await FactsEvaluator.RunAsync(
            [Case("sale-price"), Case("borrower-plaintiff")],
            "scripted",
            AlphaFacts.Reader(model, LawRoot, AlphaFacts.Read),
            TextWriter.Null);

        Assert.Contains(report.Results[0].Errors, e => e.Type == "wrong claim type" && e.Critical);
        Assert.Equal("loan-repayment", report.Results[0].Claim);
        Assert.Contains(report.Results[1].Errors, e => e.Type == "claim proposed outside the catalogue" && e.Critical);
        Assert.False(report.Results[1].Excluded);

        // sale-price: the claim, then the reading; borrower-plaintiff: the claim only.
        Assert.Equal([GroundsAnalyzer.ReadingRequestId, ClaimFactReader.RequestId, GroundsAnalyzer.ReadingRequestId], model.Requests.Select(r => r.SectionId));
    }

    private sealed class CutOff(string written) : IModelProvider
    {
        public string Name => "cut off";

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return written;
            throw new ModelProviderException("stopped at its answer length limit") { IsCutOff = true };
        }
    }

    private sealed class Scripted(string answer) : IModelProvider
    {
        public List<ModelRequest> Requests { get; } = [];

        public string Name => "scripted";

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(answer);
        }
    }
}
