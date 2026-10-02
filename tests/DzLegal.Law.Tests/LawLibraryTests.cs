using DzLegal.Domain;
using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class ArabicNormalizationTests
{
    [Theory]
    [InlineData("أَحْكَامٌ", "احكام")]
    [InlineData("إلزامُ  المدعى\tعليه", "الزام المدعي عليه")]
    [InlineData("مسؤولية", "مسووليه")]
    [InlineData("المادة ١٠٦", "الماده 106")]
    [InlineData("ﺍﻟﻤﺎﺩﺓ", "الماده")] // presentation forms from a PDF
    [InlineData("Code CIVIL", "code civil")]
    public void NormalizeMakesEquivalentSpellingsEqual(string input, string expected) =>
        Assert.Equal(expected, ArabicScript.Normalize(input));

    [Fact]
    public void TokensDropTheArticleAndItsClitics()
    {
        Assert.Equal(["قرض", "مدعي", "وامتنع", "عن", "رد"], ArabicScript.Tokens("والقرض، للمدعي: وامتنع عن الرد!"));
        Assert.Equal(["عقد", "شريعه", "متعاقدين"], ArabicScript.Tokens("العقدُ شريعةُ المتعاقدين"));
    }

    [Theory]
    [InlineData("وامتنع", "امتنع", true)]
    [InlineData("بايع", "بايع", true)]
    [InlineData("فسخ", "سخ", false)]
    public void MatchesWordAllowsOneLeadingClitic(string token, string term, bool expected) =>
        Assert.Equal(expected, ArabicScript.MatchesWord(token, term));
}

public sealed class ArticleNumberTests
{
    [Theory]
    [InlineData("106", "106")]
    [InlineData("0106", "106")]
    [InlineData("١٢٤", "124")]
    [InlineData("124مكرر", "124 مكرر")]
    [InlineData("124  مكرر  1", "124 مكرر 1")]
    [InlineData("الأولى", "1")]
    public void NumbersAreCanonical(string input, string expected) => Assert.Equal(expected, ArticleNumbers.Canonical(input));
}

public sealed class LawTextParserTests
{
    private static readonly ParseResult Result = LawTextParser.Parse(Fixtures.FictitiousCode);

    [Fact]
    public void SplitsOnArticleHeadingsIncludingWordsAndBis()
    {
        Assert.Equal(["1", "2", "3", "3 مكرر", "3 مكرر 1", "5", "6"], Result.Articles.Select(a => a.Number));
        Assert.False(Result.WasReversed);
    }

    [Fact]
    public void KeepsTheTextAndRejoinsWrappedLines()
    {
        var first = Result.Articles[0];
        Assert.Equal("يسري هذا النص الوهمي على الأطراف التجريبيين فقط.", first.Text);
        Assert.Equal("يلتزم الطرف التجريبي بما يأتي:\n1- أن يحترم النص الوهمي،\n2- أن يرد ما اقترضه.", Result.Articles[1].Text);
    }

    [Fact]
    public void TracksBookTitleAndChapterHeadings()
    {
        Assert.Equal(["الكتاب الأول — أحكام عامة", "الباب الأول — في الالتزامات التجريبية"], Result.Articles[0].Path);
        Assert.Equal(["الكتاب الأول — أحكام عامة", "الباب الأول — في الالتزامات التجريبية", "الفصل الأول"], Result.Articles[2].Path);
        Assert.Equal(["الكتاب الأول — أحكام عامة", "الباب الثاني — التعويض التجريبي"], Result.Articles[^1].Path);
    }

    [Fact]
    public void MarksRepealedArticlesAndReportsGaps()
    {
        Assert.True(Result.Articles.Single(a => a.Number == "5").Repealed);
        Assert.False(Result.Articles.Single(a => a.Number == "6").Repealed);
        Assert.Contains(Result.Warnings, w => w.Contains("missing: 4", StringComparison.Ordinal));
    }

    [Fact]
    public void FlipsTextExtractedInVisualOrder()
    {
        var reversed = string.Join('\n', Fixtures.FictitiousCode.Split('\n').Select(l => new string(l.Trim().Reverse().ToArray())));
        var result = LawTextParser.Parse(reversed);
        Assert.True(result.WasReversed);
        Assert.Equal(Result.Articles.Select(a => a.Number), result.Articles.Select(a => a.Number));
    }

    [Fact]
    public void ReportsDuplicatesAndKeepsTheFirst()
    {
        var result = LawTextParser.Parse("المادة 1: نص وهمي أول طويل بما يكفي للاختبار.\nالمادة 1: نص وهمي مكرر طويل بما يكفي.");
        Assert.Single(result.Articles);
        Assert.StartsWith("نص وهمي أول", result.Articles[0].Text, StringComparison.Ordinal);
        Assert.Contains(result.Warnings, w => w.Contains("appears again", StringComparison.Ordinal));
    }
}

public sealed class LawLibraryLoaderTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("dzlegal-law-");

    public void Dispose() => _root.Delete(recursive: true);

    private string Write(LawCode code, string? fileName = null)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_root.FullName, LawLibraryLoader.CodesFolder));
        var path = Path.Combine(folder.FullName, (fileName ?? code.Id) + ".json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(code, Domain.Serialization.DefinitionJson.Options));
        return path;
    }

    [Fact]
    public void AMissingFolderIsAnEmptyLibrary() => Assert.Empty(LawLibraryLoader.Load(_root.FullName).Codes);

    [Fact]
    public void LoadsAndFindsArticles()
    {
        Write(Fixtures.Code(Fixtures.Article("124 مكرر", "نص وهمي مكرر للاختبار فقط.")));
        var library = LawLibraryLoader.Load(_root.FullName);
        Assert.Equal("نص وهمي مكرر للاختبار فقط.", library.FindArticle("fictitious-code", "124مكرر")!.Text);
        Assert.Null(library.FindArticle("fictitious-code", "125"));
        Assert.Null(library.FindArticle("other", "124 مكرر"));
    }

    [Fact]
    public void RejectsDuplicatesNonCanonicalNumbersEmptyTextAndMisnamedFiles()
    {
        var path = Write(
            Fixtures.Code(
                Fixtures.Article("1", "نص وهمي."),
                Fixtures.Article("1", "نص وهمي آخر."),
                Fixtures.Article("02", "نص وهمي."),
                Fixtures.Article("3", " ")),
            "wrong-name");
        var ex = Assert.Throws<LawDataException>(() => LawLibraryLoader.LoadCode(path));
        Assert.Contains(ex.Errors, e => e.Contains("appears 2 times", StringComparison.Ordinal));
        Assert.Contains(ex.Errors, e => e.Contains("not canonical", StringComparison.Ordinal));
        Assert.Contains(ex.Errors, e => e.Contains("Article 3 has no text", StringComparison.Ordinal));
        Assert.Contains(ex.Errors, e => e.Contains("File name", StringComparison.Ordinal));
    }

    [Fact]
    public void DiffFindsAmendments()
    {
        var before = Fixtures.Code(Fixtures.Article("1", "نص وهمي."), Fixtures.Article("2", "نص وهمي ثان."), Fixtures.Article("3", "نص وهمي ثالث."));
        var after = Fixtures.Code(Fixtures.Article("1", "نصٌّ  وهمي."), Fixtures.Article("2", "نص وهمي معدل."), Fixtures.Article("3", "(ملغاة)", repealed: true), Fixtures.Article("4", "نص وهمي جديد."));
        var diff = LawDiff.Compare(before, after);
        Assert.Equal(["4"], diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Equal(["2", "3"], diff.Changed);
        Assert.Equal(["3"], diff.NewlyRepealed);
    }
}

public sealed class LawImportTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("dzlegal-import-");

    public void Dispose() => _root.Delete(recursive: true);

    [Fact]
    public async Task ImportsAFileWritesTheCodeAndAReviewReport()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "data", "law", LawSourceManifest.FileName), Path.Combine(_root.FullName, LawSourceManifest.FileName));
        var source = Path.Combine(_root.FullName, "fixture.html");
        await File.WriteAllTextAsync(source, "<html><body><p>" + Fixtures.FictitiousCode.Replace("\n", "<br>", StringComparison.Ordinal) + "</p><script>var x = 'المادة 99';</script></body></html>");

        var output = new StringWriter();
        Assert.Equal(0, await LawImport.RunAsync("civil-code", url: null, source, _root.FullName, "2026-01-01", output));

        var code = LawLibraryLoader.Load(_root.FullName).FindCode("civil-code")!;
        Assert.Equal(7, code.Articles.Count);
        Assert.False(code.Verified);
        Assert.Equal("2026-01-01", code.ConsolidatedAsOf);
        Assert.Equal(64, code.Source!.Sha256.Length);
        Assert.Contains("missing: 4", await File.ReadAllTextAsync(Path.Combine(_root.FullName, "reports", "civil-code.md")), StringComparison.Ordinal);

        // A second import reports what changed.
        await File.WriteAllTextAsync(source, Fixtures.FictitiousCode.Replace("كل خطأ وهمي", "كل خطأ وهمي معدل", StringComparison.Ordinal));
        output = new StringWriter();
        Assert.Equal(0, await LawImport.RunAsync("civil-code", url: null, source, _root.FullName, null, output));
        Assert.Contains("Text changed: 6", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefusesUnknownCodesAndAmbiguousSources()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "data", "law", LawSourceManifest.FileName), Path.Combine(_root.FullName, LawSourceManifest.FileName));
        Assert.Equal(2, await LawImport.RunAsync("no-such-code", null, "x", _root.FullName, null, TextWriter.Null));
        Assert.Equal(2, await LawImport.RunAsync("civil-code", null, null, _root.FullName, null, TextWriter.Null));
    }
}

public sealed class ShippedLawDataTests
{
    private static string LawRoot => Path.Combine(AppContext.BaseDirectory, "data", "law");

    [Fact]
    public void TheShippedLibraryLoadsAndOnlyContainsManifestCodes()
    {
        var manifest = LawSourceManifest.Load(LawRoot);
        Assert.Equal(manifest.Codes.Count, manifest.Codes.Select(c => c.Id).Distinct().Count());
        var library = LawLibraryLoader.Load(LawRoot);
        Assert.All(library.Codes, c => Assert.Contains(manifest.Codes, m => m.Id == c.Id));
    }
}
