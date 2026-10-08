using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Drafting.Tests;

public sealed class GroundsAnalyzerTests
{
    private static readonly AnalysisPrompts Prompts = LegalKnowledgeLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "law")).Prompts;

    private static readonly LawSourceManifest Manifest = new()
    {
        Codes = [new LawSourceEntry { Id = "fictitious-code", Title = new LocalizedText("قانون تجريبي", "Fictitious"), CitationName = "القانون التجريبي", Reference = "نص وهمي", OfficialSite = "https://www.joradp.dz" }],
    };

    private static ClaimType Claim(string id, string area, string label, params (string Number, ArticleRole Role)[] articles) => new()
    {
        Id = id,
        Area = area,
        Label = new LocalizedText(label, id),
        Description = "وصف وهمي لـ" + label,
        Articles = articles.Select(a => new ClaimArticle { Code = "fictitious-code", Number = a.Number, Role = a.Role, Why = new LocalizedText("سبب وهمي للمادة " + a.Number, "x") }).ToList(),
    };

    private static readonly ClaimType Loan = Claim("loan", "civil", "استرداد قرض وهمي", ("10", ArticleRole.Basis), ("20", ArticleRole.Proof));
    private static readonly ClaimType Khul = Claim("khul", "family", "خلع وهمي", ("30", ArticleRole.Basis));

    /// <summary>FICTITIOUS library: what the knowledge looks like once codes are imported.</summary>
    private static readonly LawLibrary Library = new(
    [
        new LawCode
        {
            Id = "fictitious-code",
            Title = new LocalizedText("قانون تجريبي", "Fictitious"),
            CitationName = "القانون التجريبي",
            Reference = "نص وهمي",
            Source = new LawSource { Url = "fixture" },
            Articles =
            [
                new LawArticle { Number = "10", Text = "يلتزم المقترض الوهمي برد مثل ما اقترضه.", Path = ["في القرض الوهمي"] },
                new LawArticle { Number = "20", Text = "على الدائن الوهمي إثبات الالتزام.", Path = ["في الإثبات الوهمي"] },
                new LawArticle { Number = "30", Text = "يجوز للزوجة الوهمية مخالعة نفسها.", Path = ["في الخلع الوهمي"] },
                new LawArticle { Number = "40", Text = "يستحق الدائن الوهمي التعويض عن التأخير في الوفاء بعد الإعذار.", Path = ["في التعويض الوهمي"] },
            ],
        },
    ]);

    private static LegalKnowledge Knowledge(LawLibrary? library = null) => new(library ?? LawLibrary.Empty, Manifest, [Loan, Khul], Prompts);

    private const string Reading = """
        العلاقة القانونية: عقد قرض بين شخصين (المادة 999)
        الالتزام المخل به: لم يرد المقترض المبلغ
        المطلوب: إلزامه برد المبلغ والتعويض
        نوع الدعوى: loan, invented-claim, khul, loan
        كلمات البحث: التعويض عن التأخير، الإعذار، رد المثل
        """;

    [Fact]
    public void ReadingKeepsCatalogIdsOnlyAndNoArticleNumbers()
    {
        var reading = GroundsAnalyzer.ParseReading("<think>khul?</think>\n" + Reading, [Loan, Khul]);
        Assert.Equal("عقد قرض بين شخصين", reading.Relationship);
        Assert.Equal("لم يرد المقترض المبلغ", reading.Breach);
        Assert.Equal(["loan", "khul"], reading.ClaimIds);
        Assert.Equal(["التعويض عن التأخير", "الإعذار", "رد المثل"], reading.SearchTerms);

        Assert.Empty(GroundsAnalyzer.ParseReading("نوع الدعوى: لا شيء", [Loan]).ClaimIds);
        Assert.Equal(FactsReading.Empty with { }, GroundsAnalyzer.ParseReading("كلام بلا عناوين", [Loan]) with { ClaimIds = [], SearchTerms = [] });
    }

    [Theory]
    [InlineData("**نوع الدعوى:** loan")]
    [InlineData("- نوع الدعوى: استرداد قرض وهمي")]
    [InlineData("نوع الدعوي: استرداد قرض وهمي (بسبب عدم الرد)")]
    [InlineData("نوع الدعوى:\nloan")]
    [InlineData("العلاقة القانونية: عقد قرض\nالدعوى المناسبة هي loan")]
    public void ReadingAcceptsTheFormatsSmallModelsWrite(string answer) =>
        Assert.Equal(["loan"], GroundsAnalyzer.ParseReading(answer, [Loan, Khul]).ClaimIds);

    [Fact]
    public async Task OtherMeansNothingAppliesAndSkipsTheSecondCall()
    {
        var request = GroundsAnalyzer.BuildReadingRequest(Knowledge(), "", "وقائع", [Loan]);
        Assert.EndsWith("other — غير ذلك: دعوى لا يطابقها أي نوع أعلاه (مثل الدعاوى الإدارية، التركات، الحالة المدنية، الشركات)",
            request.UserPrompt.Split('\n').First(l => l.StartsWith("other", StringComparison.Ordinal)), StringComparison.Ordinal);

        Assert.Empty(GroundsAnalyzer.ParseReading("نوع الدعوى: other", [Loan, Khul]).ClaimIds);
        Assert.Empty(GroundsAnalyzer.ParseReading("نوع الدعوى: غير ذلك", [Loan, Khul]).ClaimIds);
        Assert.Equal(["khul"], GroundsAnalyzer.ParseReading("نوع الدعوى: other, khul", [Loan, Khul]).ClaimIds);

        var provider = new RecordingProvider(_ => "نوع الدعوى: other");
        var analysis = await new GroundsAnalyzer(provider).AnalyseAsync(Knowledge(Library), "", "طلب تصحيح تاريخ ميلاد", null);
        Assert.Empty(analysis.ClaimIds);
        Assert.Single(provider.Requests);
    }

    [Fact]
    public void ANameMentionedOutsideTheClaimLineDoesNotCountWhenTheLineExists() =>
        Assert.Empty(GroundsAnalyzer.ParseReading("المطلوب: خلع وهمي؟ لا\nنوع الدعوى: لا شيء", [Loan, Khul]).ClaimIds);

    [Fact]
    public async Task TheTraceReceivesPromptsAnswersAndCandidates()
    {
        var provider = new RecordingProvider(r => r.SectionId == GroundsAnalyzer.ReadingRequestId ? Reading : "fictitious-code:10 | أساس | سبب");
        var trace = new List<string>();
        await new GroundsAnalyzer(provider) { Trace = (step, _) => trace.Add(step) }.AnalyseAsync(Knowledge(Library), "دين", "أقرض المدعي", "القسم المدني");
        Assert.Equal(["reading.prompt", "reading.answer", "candidates", "selection.prompt", "selection.answer"], trace);
    }

    [Fact]
    public void ThePromptsListOnlyTheAreaClaimsAndTheCandidates()
    {
        var reading = GroundsAnalyzer.BuildReadingRequest(Knowledge(), "دين", "أقرض المدعي", [Loan]);
        Assert.Contains("loan — استرداد قرض وهمي: وصف وهمي لـاسترداد قرض وهمي", reading.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("khul", reading.UserPrompt, StringComparison.Ordinal);

        var candidates = GroundsAnalyzer.Candidates(Knowledge(Library), GroundsAnalyzer.ParseReading(Reading, [Loan]), "أقرض المدعي", [Loan]);
        var selection = GroundsAnalyzer.BuildSelectionRequest(Knowledge(Library), "أقرض المدعي", FactsReading.Empty, candidates);
        Assert.Contains("fictitious-code:10 — المادة 10 من القانون التجريبي — يلتزم المقترض الوهمي", selection.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void CandidatesAreTheClaimArticlesThenLibraryMatches()
    {
        var reading = GroundsAnalyzer.ParseReading(Reading, [Loan]);

        // Before import: only the catalog articles, described by their reason.
        var before = GroundsAnalyzer.Candidates(Knowledge(), reading, "facts", [Loan]);
        Assert.Equal(["fictitious-code:10", "fictitious-code:20"], before.Select(c => c.Key));
        Assert.All(before, c => Assert.Equal(AnalysedArticle.FromCatalog, c.Source));
        Assert.StartsWith("سبب وهمي", before[0].Excerpt, StringComparison.Ordinal);

        // After import: library search adds article 40 (the model's terms «التعويض عن التأخير»، «الإعذار»).
        var after = GroundsAnalyzer.Candidates(Knowledge(Library), reading, "facts", [Loan]);
        Assert.Equal("fictitious-code:10", after[0].Key);
        Assert.StartsWith("يلتزم المقترض", after[0].Excerpt, StringComparison.Ordinal);
        var searched = Assert.Single(after, c => c.Key == "fictitious-code:40");
        Assert.Equal(AnalysedArticle.FromSearch, searched.Source);
        Assert.Equal(after.Count, after.Select(c => c.Key).Distinct().Count());
    }

    [Fact]
    public void SelectionKeepsCandidateKeysWithRolesAndSafeReasons()
    {
        var candidates = GroundsAnalyzer.Candidates(Knowledge(Library), GroundsAnalyzer.ParseReading(Reading, [Loan]), "facts", [Loan]);
        var selected = GroundsAnalyzer.ParseSelection("""
            fictitious-code:10 | أساس | لأن المدعى عليه اقترض ولم يرد
            fictitious-code:999 | أساس | مادة مخترعة
            المادة 40 | تعويض | التأخير بعد الإعذار يوجب التعويض
            fictitious-code:20 | ؟ | يستند إلى المادة 777 أيضا
            fictitious-code:10 | أساس | مكرر
            """, candidates);

        Assert.Equal(["10", "40", "20"], selected.Select(a => a.Number));
        Assert.Equal(["basis", "damages", "proof"], selected.Select(a => a.Role)); // 20: catalog role when unclear
        Assert.Equal("لأن المدعى عليه اقترض ولم يرد", selected[0].Reason);
        Assert.Empty(selected[2].Reason); // a reason citing another article is dropped
        Assert.Equal(AnalysedArticle.FromSearch, selected[1].Source);
        Assert.Empty(GroundsAnalyzer.ParseSelection("لا شيء", candidates));
    }

    [Fact]
    public async Task TheWholeAnalysisRunsInThreeStages()
    {
        var provider = new RecordingProvider(r => r.SectionId == GroundsAnalyzer.ReadingRequestId ? Reading : "fictitious-code:10 | أساس | سبب");
        var stages = new List<AnalysisStage>();
        var analysis = await new GroundsAnalyzer(provider).AnalyseAsync(Knowledge(Library), "دين", "أقرض المدعي المدعى عليه ولم يرد", "القسم المدني", new InlineProgress(stages.Add));

        Assert.Equal([AnalysisStage.Reading, AnalysisStage.Searching, AnalysisStage.Selecting], stages);
        Assert.Equal(["loan"], analysis.ClaimIds); // khul is a family claim: not offered for a civil section
        Assert.Equal("10", Assert.Single(analysis.Articles).Number);
        Assert.Equal(GroundsAnalyzer.FactsHash("دين", "أقرض المدعي المدعى عليه ولم يرد", "القسم المدني"), analysis.FactsHash);
        Assert.Equal("recording", analysis.Model);
    }

    [Fact]
    public async Task BothAnswersStreamLiveAndTheCandidatesAreReported()
    {
        var provider = new ChunkedProvider(r => r.SectionId == GroundsAnalyzer.ReadingRequestId ? Reading : "fictitious-code:10 | أساس | سبب");
        var partials = new List<(AnalysisStage Stage, string Text)>();
        var candidates = -1;
        var analysis = await new GroundsAnalyzer(provider)
        {
            OnPartial = (stage, text) => partials.Add((stage, text)),
            OnCandidates = found => candidates = found.Count,
        }.AnalyseAsync(Knowledge(Library), "دين", "أقرض المدعي المدعى عليه ولم يرد", "القسم المدني");

        Assert.True(partials.Count(p => p.Stage == AnalysisStage.Reading) > 1); // several pieces, growing
        Assert.Equal(Reading, partials.Last(p => p.Stage == AnalysisStage.Reading).Text);
        Assert.Equal("fictitious-code:10 | أساس | سبب", partials.Last(p => p.Stage == AnalysisStage.Selecting).Text);
        Assert.True(candidates > 0);
        Assert.Equal("10", Assert.Single(analysis.Articles).Number); // parsing is unchanged by streaming
    }

    /// <summary>Streams each answer in pieces of 20 characters, like a local model writing token by token.</summary>
    private sealed class ChunkedProvider(Func<ModelRequest, string> reply) : IModelProvider
    {
        public string Name => "chunked";

        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) => Task.FromResult(reply(request));

        public async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var text = reply(request);
            for (var i = 0; i < text.Length; i += 20)
            {
                await Task.Yield();
                yield return text.Substring(i, Math.Min(20, text.Length - i));
            }
        }
    }

    [Fact]
    public async Task BothAnalysisCallsAreShortByDesign()
    {
        var provider = new RecordingProvider(r => r.SectionId == GroundsAnalyzer.ReadingRequestId ? Reading : "fictitious-code:10 | أساس | سبب");
        await new GroundsAnalyzer(provider).AnalyseAsync(Knowledge(Library), "دين", "أقرض المدعي المدعى عليه ولم يرد", "القسم المدني");
        Assert.Equal([GroundsAnalyzer.ReadingMaxTokens, GroundsAnalyzer.SelectionMaxTokens], provider.Requests.Select(r => r.MaxTokens));
    }

    [Fact]
    public async Task NoCandidatesMeansNoSecondCall()
    {
        var provider = new RecordingProvider(_ => "نوع الدعوى: لا شيء");
        var analysis = await new GroundsAnalyzer(provider).AnalyseAsync(Knowledge(), "", "وقائع لا تخص الفهرس", null);
        Assert.Empty(analysis.Articles);
        Assert.Single(provider.Requests);
    }

    [Fact]
    public void TheFactsHashIgnoresSpellingVariantsButNotContent()
    {
        Assert.Equal(GroundsAnalyzer.FactsHash("دين", "أقرضَ  المدعي", null), GroundsAnalyzer.FactsHash("دين", "اقرض المدعي", null));
        Assert.NotEqual(GroundsAnalyzer.FactsHash("دين", "أقرض المدعي", null), GroundsAnalyzer.FactsHash("دين", "أقرض المدعى عليه", null));
    }

    private sealed class InlineProgress(Action<AnalysisStage> report) : IProgress<AnalysisStage>
    {
        public void Report(AnalysisStage value) => report(value);
    }
}
