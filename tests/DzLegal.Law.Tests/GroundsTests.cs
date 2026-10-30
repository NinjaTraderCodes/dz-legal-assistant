using DzLegal.Domain;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class LegalAreaTests
{
    [Theory]
    [InlineData("القسم المدني", "civil")]
    [InlineData("القسم العقاري", "civil")]
    [InlineData("القسم التجاري", "commercial")]
    [InlineData("قسم شؤون الأسرة", "family")]
    [InlineData("القسم الاجتماعي", "social")]
    [InlineData("القسم الاستعجالي", null)]
    [InlineData("المحكمة الإدارية", LegalAreas.Administrative)]
    [InlineData("الغرفة الإدارية", LegalAreas.Administrative)]
    [InlineData("", null)]
    public void TheAreaComesFromTheCourtSection(string section, string? expected) => Assert.Equal(expected, LegalAreas.AreaOf(section));
}

public sealed class ClaimCatalogValidationTests
{
    private static readonly LawSourceManifest Manifest = new()
    {
        Codes = [new LawSourceEntry { Id = "fictitious-code", Title = new LocalizedText("قانون تجريبي", "Fictitious"), CitationName = "القانون التجريبي", Reference = "نص وهمي" }],
    };

    private static ClaimCatalogFile File(params ClaimArticle[] articles) => new()
    {
        Area = "civil",
        Claims =
        [
            new ClaimType
            {
                Id = "c",
                Label = new LocalizedText("قرض", "c"),
                Description = "وصف وهمي",
                Articles = articles,
            },
        ],
    };

    private static ClaimArticle Ref(string code, string number) => new() { Code = code, Number = number, Why = new LocalizedText("x", "x") };

    [Fact]
    public void CitationsOfCodesNotYetImportedAreAccepted() =>
        Assert.Empty(LegalKnowledgeLoader.Validate(File(Ref("fictitious-code", "999")), Manifest, LawLibrary.Empty));

    [Fact]
    public void OnceImportedEveryCitedArticleMustExistAndBeInForce()
    {
        var library = new LawLibrary([Fixtures.Code(Fixtures.Article("5", "(ملغاة)", repealed: true), Fixtures.Article("6", "نص وهمي."))]);
        var errors = LegalKnowledgeLoader.Validate(File(Ref("fictitious-code", "5"), Ref("fictitious-code", "7"), Ref("fictitious-code", "6"), Ref("unknown", "1")), Manifest, library);
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Contains("article 5 of fictitious-code is repealed", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("article 7 does not exist", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("unknown code 'unknown'", StringComparison.Ordinal));
    }

    [Fact]
    public void AClaimNeedsAnArabicLabel()
    {
        var file = File(Ref("fictitious-code", "6"));
        Assert.Contains(LegalKnowledgeLoader.Validate(file with { Claims = [file.Claims[0] with { Label = new LocalizedText(" ", "c") }] }, Manifest, LawLibrary.Empty), e => e.Contains("Arabic label and description", StringComparison.Ordinal));
        Assert.Contains(LegalKnowledgeLoader.Validate(file with { Claims = [file.Claims[0] with { Description = "" }] }, Manifest, LawLibrary.Empty), e => e.Contains("Arabic label and description", StringComparison.Ordinal));
    }
}

public sealed class ArticleIndexTests
{
    private static readonly LawLibrary Library = new(
    [
        Fixtures.Code(
            Fixtures.Article("10", "يلتزم المقترض الوهمي برد مثل ما اقترضه عند نهاية الأجل.") with { Path = ["الفصل الوهمي — في القرض"] },
            Fixtures.Article("11", "يدفع المستأجر الوهمي بدل الإيجار في المواعيد المتفق عليها.") with { Path = ["الفصل الوهمي — في الإيجار"] },
            Fixtures.Article("12", "كل خطأ وهمي سبب ضررا للغير يلزم مرتكبه بالتعويض.") with { Path = ["الفصل الوهمي — في المسؤولية"] },
            Fixtures.Article("13", "(ملغاة)", repealed: true) with { Path = ["الفصل الوهمي — في القرض"] }),
    ]);

    [Fact]
    public void RanksTheArticleThatMatchesTheLegalVocabularyFirst()
    {
        Assert.Equal("10", Library.Index.Search("القرض ورد المثل")[0].Article.Number);
        Assert.Equal("11", Library.Index.Search("بدل الإيجار غير المدفوع")[0].Article.Number);
        Assert.Equal("12", Library.Index.Search("التعويض عن الضرر")[0].Article.Number);
    }

    [Fact]
    public void ChapterHeadingsCountAndRepealedArticlesAreNotIndexed()
    {
        Assert.Equal(3, Library.Index.Count);
        // «القرض» appears only in the heading of article 10's chapter besides its text; the repealed 13 never comes back.
        Assert.DoesNotContain(Library.Index.Search("القرض"), h => h.Article.Number == "13");
    }

    [Fact]
    public void WeightedQueriesAddUp()
    {
        var hits = Library.Index.Search([("الإيجار", 0.5), ("القرض", 1.0)]);
        Assert.Equal("10", hits[0].Article.Number);
        Assert.Contains(hits, h => h.Article.Number == "11");
        Assert.Empty(Library.Index.Search("كلمات لا وجود لها إطلاقا"));
    }
}

public sealed class ArticleSearchTests
{
    private static readonly LawLibrary Library = new(
    [
        Fixtures.Code(
            Fixtures.Article("124", "كل فعل وهمي يرتكبه الشخص بخطئه ويسبب ضررا للغير يلزم من كان سببا في حدوثه بالتعويض الوهمي."),
            Fixtures.Article("124 مكرر", "نص وهمي مكرر عن التعسف."),
            Fixtures.Article("73-4", "نص وهمي عن التسريح."),
            Fixtures.Article("125", "(ملغاة)", repealed: true)),
    ]);

    [Theory]
    [InlineData("124", "124")]
    [InlineData("المادة ١٢٤", "124")]
    [InlineData("م 124 مكرر", "124 مكرر")]
    [InlineData("المادة 73 - 4", "73-4")]
    public void FindsByNumber(string query, string expected) => Assert.Equal(expected, Assert.Single(ArticleSearch.Find(Library, query)).Article.Number);

    [Fact]
    public void FindsByWordsAndNeverReturnsRepealedArticles()
    {
        Assert.Equal("124", ArticleSearch.Find(Library, "الخطأ والضرر والتعويض")[0].Article.Number);
        Assert.Empty(ArticleSearch.Find(Library, "125"));
        Assert.Empty(ArticleSearch.Find(Library, "كلمات لا وجود لها إطلاقا"));
    }

    [Fact]
    public void ParsesHyphenatedArticleNumbers()
    {
        var result = LawTextParser.Parse("المادة 73-4: نص وهمي عن التسريح المخالف للإجراءات.\nالمادة 73-5 - نص وهمي آخر طويل بما يكفي.");
        Assert.Equal(["73-4", "73-5"], result.Articles.Select(a => a.Number));
    }
}

public sealed class ShippedCatalogTests
{
    private static string LawRoot => Path.Combine(AppContext.BaseDirectory, "data", "law");

    [Fact]
    public void EveryShippedTextIsProvedAgainstItsOfficialSource()
    {
        var library = LawLibraryLoader.Load(LawRoot);
        Assert.Equal(["civil-code", "commercial-code", "cpca", "family-code", "labour-disputes", "labour-relations"], library.Codes.Select(c => c.Id).Order(StringComparer.Ordinal));
        Assert.All(library.Codes.Where(c => c.Transcription is null).SelectMany(c => c.Articles), a =>
        {
            Assert.NotNull(a.SourceExtract);
            Assert.Equal(a.SourceExtract, LawTextFold.Fold(a.Text));
            Assert.True(a.Page > 0);
        });

        // The labour-law PDFs have no usable text layer: transcribed from the page images and read twice. The Family
        // Code and the CPCA come complete from the law corpus, each text read against the official gazette page that
        // published it. Either way: each article with its page and the publication it is printed in, nothing marked
        // verified.
        Assert.Equal(["cpca", "family-code", "labour-disputes", "labour-relations"], library.Codes.Where(c => c.Transcription is not null).Select(c => c.Id).Order(StringComparer.Ordinal));
        Assert.All(library.Codes.Where(c => c.Transcription is not null), c =>
        {
            Assert.False(c.Verified);
            Assert.All(c.Articles, a =>
            {
                var url = (a.Source ?? c.Source)!.Url;
                Assert.Null(a.SourceExtract);
                Assert.True(a.Page > 0);
                Assert.False(a.Verified);
                Assert.True(File.Exists(Path.Combine(LawRoot, "..", "..", url)) || url.StartsWith("docs/", StringComparison.Ordinal) || url.StartsWith("https://www.joradp.dz/FTP/jo-arabe/", StringComparison.Ordinal), url);
            });
        });
        Assert.Equal((227, 1084), (library.FindCode("family-code")!.Articles.Count, library.FindCode("cpca")!.Articles.Count));
        Assert.Contains("يمنح القاضي الولاية لمن أسندت له حضانة الأولاد", library.FindArticle("family-code", "87")!.Text, StringComparison.Ordinal);
        Assert.Contains("في موضوع النفقة الغذائية بموطن الدائن بها", library.FindArticle("cpca", "426")!.Text, StringComparison.Ordinal);
        Assert.Contains("الخلافات الفردية للعمل", library.FindArticle("labour-disputes", "19")!.Text, StringComparison.Ordinal); // JO 6/1990, p. 242
        Assert.Contains("مدة ستة (6) أشهر", library.FindArticle("labour-relations", "73-4")!.Text, StringComparison.Ordinal);
        Assert.Contains("يجب أن تتضمن عريضة افتتاح الدعوى", library.FindArticle("cpca", "15")!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEditThatChangesALetterIsRejectedButSpacingIsNot()
    {
        var code = LawLibraryLoader.Load(LawRoot).FindCode("civil-code")!;
        var article = code.FindArticle("323")!;
        Assert.Empty(LawLibraryLoader.Validate(code with { Articles = [article with { Text = article.Text.Replace(" ", "  ", StringComparison.Ordinal) }] }));
        Assert.Empty(LawLibraryLoader.Validate(code with { Articles = [article with { Text = article.Text.Replace("ك", "آ", StringComparison.Ordinal) }] }));
        var edited = article with { Text = article.Text.Replace("الدائن", "المدين", StringComparison.Ordinal) };
        Assert.Contains(LawLibraryLoader.Validate(code with { Articles = [edited] }), e => e.Contains("no longer matches its source", StringComparison.Ordinal));
    }

    [Fact]
    public void TheShippedCatalogLoadsAndIsUnverified()
    {
        var knowledge = LegalKnowledgeLoader.Load(LawRoot);
        Assert.True(knowledge.Claims.Count >= 18);
        Assert.All(knowledge.Claims, c => Assert.False(c.Verified));
        Assert.All(knowledge.Claims, c => Assert.NotEmpty(c.Description));
        Assert.Equal(LegalKnowledgeLoader.Areas.Order(), knowledge.Claims.Select(c => c.Area).Distinct().Order());
    }

    [Fact]
    public void EveryClaimIsExercisedByTheEvaluationCasesAndTheirArticlesExist()
    {
        var knowledge = LegalKnowledgeLoader.Load(LawRoot);
        var cases = GroundsEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "grounds-cases"));
        Assert.All(knowledge.Claims, c => Assert.Contains(cases, x => x.Expect == c.Id));
        Assert.Contains(cases, x => x.Expect.Length == 0);
        Assert.All(cases.Where(c => c.Expect.Length > 0), c =>
        {
            var claim = knowledge.Claims.Single(x => x.Id == c.Expect);
            Assert.NotEmpty(c.ExpectArticles);
            Assert.All(c.ExpectArticles, a => Assert.Contains(claim.Articles, x => $"{x.Code}:{x.Number}" == a));
        });
    }

    [Fact]
    public void TheDogAndWallCasesRequireTheirOwnArticlesAndTheGapsAreReported()
    {
        var knowledge = LegalKnowledgeLoader.Load(LawRoot);
        var cases = GroundsEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "grounds-cases"));
        Assert.Equal(["civil-code:139"], cases.Single(c => c.Name == "things-dog").LegalReference);
        Assert.Equal(["civil-code:140"], cases.Single(c => c.Name == "things-wall").LegalReference);
        Assert.All(cases, c => Assert.Contains(c.Partition, new[] { "development", "validation", "release", "robustness" }));
        Assert.All(cases.Where(c => c.ReferenceBasis != "engineering"), c => Assert.False(string.IsNullOrWhiteSpace(c.Source)));

        // Until the corpus brings them in, these are gaps of the library, not of the analysis.
        Assert.False(GroundsEvaluator.InLibrary(knowledge, "civil-code:139"));
        Assert.True(GroundsEvaluator.InLibrary(knowledge, "civil-code:138"));
    }

    [Fact]
    public void TheRunManifestFingerprintsChangeWhenTheDataChanges()
    {
        var folder = Directory.CreateTempSubdirectory("dzlegal-manifest-");
        try
        {
            File.WriteAllText(Path.Combine(folder.FullName, "a.json"), "{}");
            var before = RunManifest.FolderHash(folder.FullName);
            Assert.Equal(before, RunManifest.FolderHash(folder.FullName));
            File.WriteAllText(Path.Combine(folder.FullName, "a.json"), "{ }");
            Assert.NotEqual(before, RunManifest.FolderHash(folder.FullName));
            Assert.Equal("missing", RunManifest.FolderHash(Path.Combine(folder.FullName, "none")));

            RunManifest.Create("grounds", ["stub"], new Dictionary<string, string> { ["cases"] = folder.FullName }, new Dictionary<string, string>()).Write(folder.FullName);
            Assert.Contains("\"cases\"", File.ReadAllText(Path.Combine(folder.FullName, "manifest.json")), StringComparison.Ordinal);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task TheBenchWritesOneFilePerCaseAndASummary()
    {
        var folder = Directory.CreateTempSubdirectory("dzlegal-grounds-bench-");
        try
        {
            var knowledge = LegalKnowledgeLoader.Load(LawRoot);
            // The first three cases include loan-darija: written in Darija, it is drafted and reported apart.
            var cases = GroundsEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "grounds-cases")).Take(3).ToList();
            var report = await GroundsEvaluator.EvaluateAsync(knowledge, cases, new DzLegal.ModelProviders.Stub.StubModelProvider(), TextWriter.Null, folder.FullName);
            Assert.Equal(2, report.Results.Count);
            Assert.Equal("loan-darija", Assert.Single(report.Robustness).Case.Name);
            var files = Directory.GetFiles(Path.Combine(folder.FullName, "stub__template_mode__no_AI_"), "*.md");
            Assert.Equal(3, files.Length); // every case keeps its trace, robustness included
            Assert.Contains("## reading.answer", await File.ReadAllTextAsync(files[0]), StringComparison.Ordinal);
            var summary = await File.ReadAllTextAsync(Path.Combine(folder.FullName, "summary.md"));
            Assert.Contains("| claim exact |", summary, StringComparison.Ordinal);
            Assert.Contains("| claim acceptable | legal recall", summary, StringComparison.Ordinal);
            Assert.Contains("### Robustness: facts in Darija or French (not counted above)", summary, StringComparison.Ordinal);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void TheAnalysisPromptsShip()
    {
        var prompts = LegalKnowledgeLoader.Load(LawRoot).Prompts;
        Assert.True(prompts.IsAvailable);
        Assert.Contains("{{ claims }}", prompts.ReadFacts, StringComparison.Ordinal);
        Assert.Contains("{{ candidates }}", prompts.SelectArticles, StringComparison.Ordinal);
    }
}

public sealed class ProvenanceTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("dzlegal-prov-");

    public void Dispose() => _root.Delete(recursive: true);

    private static readonly LawSourceManifest Manifest = new()
    {
        Codes =
        [
            new LawSourceEntry
            {
                Id = "fictitious-code",
                Title = new LocalizedText("قانون تجريبي", "Fictitious"),
                CitationName = "القانون التجريبي",
                Reference = "الأمر الوهمي رقم 00-00",
                OfficialSite = "https://www.joradp.dz",
                JoReference = "الجريدة الرسمية الوهمية عدد 0",
            },
        ],
    };

    [Theory]
    [InlineData("https://www.joradp.dz/HAR/Index.htm", true)]
    [InlineData("https://joradp.dz", true)]
    [InlineData("https://www.mjustice.dz/ar/", true)]
    [InlineData("http://www.joradp.dz", false)]
    [InlineData("https://joradp.dz.example.com", false)]
    [InlineData("https://example.com/joradp.dz", false)]
    [InlineData("fixture", false)]
    public void OnlyHttpsOfficialHostsAreOfficial(string url, bool expected) => Assert.Equal(expected, OfficialLinks.IsOfficial(url));

    [Fact]
    public void BeforeImportTheLinkIsTheOfficialSite()
    {
        var knowledge = new LegalKnowledge(LawLibrary.Empty, Manifest, []);
        var p = knowledge.ProvenanceOf("fictitious-code", null);
        Assert.Equal("الأمر الوهمي رقم 00-00 — الجريدة الرسمية الوهمية عدد 0", p.Reference);
        Assert.Equal("https://www.joradp.dz", p.SourceUrl);
        Assert.False(p.SourceIsExactText);
        Assert.False(p.IsTextVerified);
        Assert.Empty(p.Location);
    }

    [Fact]
    public void AfterImportTheLinkIsTheExactDocumentAndTheLocationIsKnown()
    {
        var article = Fixtures.Article("6", "نص وهمي.") with { Path = ["الكتاب الأول", "الباب الثاني"] };
        var code = Fixtures.Code(article) with
        {
            Id = "fictitious-code",
            Verified = true,
            Source = new LawSource { Url = "https://www.joradp.dz/fictitious.pdf", RetrievedOn = "2026-10-03" },
        };
        var p = new LegalKnowledge(new LawLibrary([code]), Manifest, []).ProvenanceOf("fictitious-code", article);
        Assert.Equal("https://www.joradp.dz/fictitious.pdf", p.SourceUrl);
        Assert.True(p.SourceIsExactText);
        Assert.Equal("الكتاب الأول › الباب الثاني", p.Location);
        Assert.Equal("2026-10-03", p.RetrievedOn);
        Assert.True(p.IsTextVerified);
    }

    [Fact]
    public void TheLoaderRejectsNonOfficialLinks()
    {
        File.WriteAllText(Path.Combine(_root.FullName, LawSourceManifest.FileName),
            """{ "codes": [ { "id": "x", "title": { "ar": "x", "en": "x" }, "citationName": "x", "reference": "x", "officialSite": "http://example.com" } ] }""");
        Assert.Throws<LawDataException>(() => LawSourceManifest.Load(_root.FullName));

        var code = Fixtures.Code(Fixtures.Article("1", "نص وهمي.")) with { Source = new LawSource { Url = "https://example.com/code.pdf" } };
        Assert.Contains(LawLibraryLoader.Validate(code), e => e.Contains("must be an https link", StringComparison.Ordinal));
        Assert.Empty(LawLibraryLoader.Validate(code with { Source = new LawSource { Url = "code-civil.pdf" } }));
    }

    [Fact]
    public async Task TheImporterRefusesNonOfficialUrls()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "data", "law", LawSourceManifest.FileName), Path.Combine(_root.FullName, LawSourceManifest.FileName));
        var output = new StringWriter();
        Assert.Equal(2, await DzLegal.Harness.LawImport.RunAsync("civil-code", "https://example.com/code.pdf", null, _root.FullName, null, output));
        Assert.Contains("only official sources", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ShippedSuggestionsCarryTheOfficialReferenceAndSite()
    {
        var knowledge = LegalKnowledgeLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "law"));
        var loan = knowledge.Resolve(knowledge.Claims.Single(c => c.Id == "loan-repayment"));
        Assert.Equal(["450", "106", "107", "323", "324 مكرر 5", "333", "454", "179", "186", "182"], loan.Select(a => a.Reference.Number));
        Assert.All(loan, a => Assert.True(a.IsAvailable)); // every catalog article has its official text
        var first = loan[0].Provenance;
        Assert.StartsWith("الأمر رقم 75-58", first.Reference, StringComparison.Ordinal);
        Assert.Equal("https://www.joradp.dz", first.SourceUrl);
        Assert.Equal("docs/official-law-texts/75-58_wipo_Version.pdf", first.SourceFile);
        Assert.Equal(77, first.Page);
        Assert.False(first.IsTextVerified); // until a lawyer has checked it against the page
    }
}

public sealed class GroundsStabilityTests
{
    private static readonly GroundsCase Loan = new() { Name = "loan", Facts = "…", Expect = "loan-repayment", ExpectArticles = ["civil-code:450", "civil-code:107"] };

    private static GroundsReport Run(string? claim, string[] articles, string[] modelChosen, string? error = null) =>
        new("m", [new GroundsCaseResult(Loan, new GroundsAnalysis
        {
            FactsHash = "h",
            ClaimIds = claim is null ? [] : [claim],
            Articles = articles.Select(a => new AnalysedArticle(a.Split(':')[0], a.Split(':')[1], "basis", "", AnalysedArticle.FromCatalog)).ToList(),
        }, TimeSpan.Zero, []) { ModelChosen = modelChosen, Error = error }]);

    [Fact]
    public void IdenticalRunsAreFullyStable()
    {
        var runs = new[] { Run("loan-repayment", ["civil-code:450"], ["civil-code:450"]), Run("loan-repayment", ["civil-code:450"], ["civil-code:450"]) };
        var s = Assert.Single(GroundsEvaluator.Stability(runs));
        Assert.Equal(2, s.Runs);
        Assert.Equal(1, s.ClaimAgreement);
        Assert.Equal("loan-repayment", s.MostCommonClaim);
        Assert.Equal(1, s.ArticleAgreement);
        Assert.Equal(1, s.ClaimExactRate);
        Assert.Equal(0.5, s.ModelRecall);
    }

    [Fact]
    public void DisagreementLowersTheScoresAndFailuresAreCounted()
    {
        var runs = new[]
        {
            Run("loan-repayment", ["civil-code:450", "civil-code:107"], ["civil-code:450", "civil-code:107"]),
            Run("loan-repayment", ["civil-code:450"], []),
            Run(null, [], [], error: "not reachable"),
        };
        var s = Assert.Single(GroundsEvaluator.Stability(runs));
        Assert.Equal(3, s.Runs);
        Assert.Equal(1, s.Failures);
        // The failed run is left out: both answered runs gave the loan claim; Jaccard {450,107} vs {450} = 1/2.
        Assert.Equal(1, s.ClaimAgreement);
        Assert.Equal(0.5, s.ArticleAgreement, 3);
        Assert.Equal(0.5, s.ModelRecall, 3); // mean of the per-run shares: 2/2, 0
    }

    [Fact]
    public void TheLegalReferenceSeparatesCoverageGapsFromMissesAndCountsExcludedProposals()
    {
        var dog = new GroundsCase
        {
            Name = "dog",
            Facts = "…",
            Expect = "liability-for-things",
            ExpectArticles = ["civil-code:138"],
            Required = ["civil-code:139", "civil-code:124"],
            Excluded = ["civil-code:450"],
            AcceptableClaims = ["tort-damages"],
        };
        var result = new GroundsCaseResult(dog, new GroundsAnalysis
        {
            FactsHash = "h",
            ClaimIds = ["tort-damages"],
            Articles = [new AnalysedArticle("civil-code", "124", "basis", "", AnalysedArticle.FromCatalog), new AnalysedArticle("civil-code", "450", "basis", "", AnalysedArticle.FromCatalog)],
        }, TimeSpan.Zero, []) { NotInLibrary = ["civil-code:139"] };
        var report = new GroundsReport("m", [result]);

        Assert.False(result.ClaimExact);
        Assert.True(result.ClaimAcceptable);
        Assert.Equal(["civil-code:124"], result.Proposable); // 139 cannot be proposed: a gap, not a miss
        Assert.Equal(1, report.LegalRecall);
        Assert.Equal("civil-code:139", Assert.Single(report.CoverageGaps).Authority);
        Assert.Equal(1, report.ExcludedProposedCount);
        Assert.Equal(0, report.ArticleRecall); // the catalog's answer (138) is still measured as before
    }

    [Fact]
    public void WithoutARequiredListTheLegalReferenceIsTheCatalogsAnswer()
    {
        Assert.Equal(Loan.ExpectArticles, Loan.LegalReference);
        Assert.Equal("development", Loan.Partition);
    }

    [Fact]
    public void TheModelsOwnChoiceIsReadFromItsAnswerAndOnlyCandidatesCount()
    {
        var trace = new List<(string, string)>
        {
            ("candidates", "civil-code:450 — catalog\ncivil-code:107 — catalog\ncivil-code:124 — search"),
            ("selection.answer", "- civil-code:٤٥٠ | أساس | اقترض\ncivil-code:999 | أساس | مختلق\n* CIVIL-CODE:124 | تعويض | ضرر"),
        };
        Assert.Equal(["civil-code:450", "civil-code:124"], GroundsEvaluator.ModelChosen(trace));
        Assert.Empty(GroundsEvaluator.ModelChosen([("candidates", "civil-code:450 — catalog")]));
    }
}

public sealed class GroundsBatchTests
{
    private sealed class Down : DzLegal.Drafting.IModelProvider
    {
        public int Calls { get; private set; }

        public string Name => "down";

        public Task<string> GenerateAsync(DzLegal.Drafting.ModelRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            throw new DzLegal.Drafting.ModelProviderException("down is rate-limited by its provider (too many requests).") { IsTransient = true, IsRateLimited = true };
        }

        public async IAsyncEnumerable<string> StreamAsync(DzLegal.Drafting.ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return await GenerateAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task ABatchStopsAfterThreeFailuresInARowInsteadOfFailingEveryCase()
    {
        var knowledge = LegalKnowledgeLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "law"));
        var cases = GroundsEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "grounds-cases")).Where(c => c.Partition != GroundsEvaluator.Robustness).Take(10).ToList();
        var engine = new Down();
        var log = new StringWriter();

        var report = await GroundsEvaluator.EvaluateAsync(knowledge, cases, engine, log, maxConsecutiveFailures: 3);

        Assert.Equal(3, report.Results.Count);
        Assert.Equal(3, report.Failures);
        Assert.True(report.StoppedEarly);
        Assert.Contains("stopped: the engine failed 3 cases in a row", log.ToString(), StringComparison.Ordinal);
        Assert.Equal(3, engine.Calls);
    }

    [Fact]
    public async Task AnEngineThatAlwaysFailsReadsNotApplicableNeverRightOrStable()
    {
        var knowledge = LegalKnowledgeLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "law"));
        var cases = GroundsEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "grounds-cases"));
        // Not an administrative court: the code itself answers those, with no engine call.
        var outOfScope = cases.Where(c => c.Expect.Length == 0 && LegalAreas.AreaOf(c.CourtSection) != LegalAreas.Administrative).Take(2).Concat(cases.Where(c => c.Expect.Length > 0).Take(2)).ToList();

        var runs = new[]
        {
            await GroundsEvaluator.EvaluateAsync(knowledge, outOfScope, new Down(), TextWriter.Null),
            await GroundsEvaluator.EvaluateAsync(knowledge, outOfScope, new Down(), TextWriter.Null),
        };

        Assert.Equal(0, runs[0].ClaimExactRate); // a failure is not a correct "nothing applies"
        Assert.True(double.IsNaN(runs[0].OutOfScopeAbstention));
        Assert.True(double.IsNaN(runs[0].FalseAbstention));
        Assert.All(GroundsEvaluator.Stability(runs), s =>
        {
            Assert.True(double.IsNaN(s.ClaimAgreement));
            Assert.Equal("n/a", s.MostCommonClaim);
            Assert.Equal(2, s.Failures);
        });
        Assert.Contains("| n/a | n/a |", GroundsEvaluator.StabilityTable("down", GroundsEvaluator.Stability(runs)), StringComparison.Ordinal);
    }

    [Fact]
    public void TheHarnessGivesTheTextsOfArticlesCitedInFrench()
    {
        // Week 1 case a8: «Articles 106 et 107 du code civil» gave the model no official text.
        var texts = DzLegal.Harness.DraftBench.LegalTextsCited(LegalKnowledgeLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "law")), "Articles 106 et 107 du code civil : le contrat fait la loi des parties.");
        Assert.Equal(["106", "107"], texts.Select(t => t.Number).Order(StringComparer.Ordinal));
    }
}
