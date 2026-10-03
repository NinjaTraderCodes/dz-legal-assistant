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
        file = file with { Claims = [file.Claims[0] with { Label = new LocalizedText(" ", "c") }] };
        Assert.Contains(LegalKnowledgeLoader.Validate(file, Manifest, LawLibrary.Empty), e => e.Contains("Arabic label", StringComparison.Ordinal));
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
    public void TheShippedCatalogLoadsAndIsUnverified()
    {
        var knowledge = LegalKnowledgeLoader.Load(LawRoot);
        Assert.True(knowledge.Claims.Count >= 18);
        Assert.All(knowledge.Claims, c => Assert.False(c.Verified));
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
        Assert.Equal(["450", "106", "107", "323", "333", "454", "176"], loan.Select(a => a.Reference.Number));
        var first = loan[0].Provenance;
        Assert.StartsWith("الأمر رقم 75-58", first.Reference, StringComparison.Ordinal);
        Assert.Equal("https://www.joradp.dz", first.SourceUrl);
    }
}
