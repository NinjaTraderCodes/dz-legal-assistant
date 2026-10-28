using DzLegal.Law.Corpus;

namespace DzLegal.Law.Tests.Corpus;

/// <summary>Entirely invented publications and text. No fixture is Algerian law.</summary>
public class CorpusLoaderTests
{
    private static readonly DateOnly Start = new(2020, 1, 1);
    private static readonly DateOnly Cutoff = new(2026, 10, 25);

    [Fact]
    public void ResolvesHalfOpenIntervalsAndRetainsRepeal()
    {
        var end = new DateOnly(2022, 1, 1);
        var original = Version() with { InForceTo = end };
        var repealed = Version() with { VersionId = "fictitious:1@repeal", InForceFrom = end, Status = "repealed", Text = "", Paragraphs = [] };
        var corpus = CorpusLoader.Validate([Code(original, repealed)]);
        Assert.Equal(original, corpus.VersionOn("fictitious:1", end.AddDays(-1)).Version);
        Assert.Equal("repealed", corpus.VersionOn("fictitious:1", end).State);
        Assert.Equal("not-in-force", corpus.VersionOn("fictitious:1", Start.AddDays(-1)).State);
        Assert.Equal("missing", corpus.VersionOn("fictitious:9", Start).State);
    }

    [Fact]
    public void UnknownCommencementNeverMeansAnUnlimitedInterval()
    {
        var corpus = CorpusLoader.Validate([Code(Version() with { InForceFrom = null, TemporalStatus = "unsettled" })]);
        Assert.Null(corpus.VersionOn("fictitious:1", Start).Version);
        Assert.Equal("unsettled", corpus.VersionOn("fictitious:1", Start).State);
    }

    [Fact]
    public void RefusesFutureResolutionAfterDiscoveryCutoff()
    {
        var corpus = CorpusLoader.Validate([Code(Version())]);
        Assert.Equal("unsettled", corpus.VersionOn("fictitious:1", Cutoff.AddDays(1)).State);
    }

    [Fact]
    public void DoesNotExposeAnUnsettledTranscriptionAsAResolvedVersion()
    {
        var version = Version() with { Verification = new("unsettled", [], ["Unresolved invented OCR discrepancy"]) };
        var result = CorpusLoader.Validate([Code(version)]).VersionOn("fictitious:1", Start);
        Assert.Equal("unsettled", result.State);
        Assert.Null(result.Version);
    }

    [Fact]
    public void RejectsOverlapIncludingAnUnclosedEarlierVersion()
    {
        var later = Version() with { VersionId = "fictitious:1@later", InForceFrom = Start.AddYears(1) };
        var ex = Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([Code(Version(), later)]));
        Assert.Contains(ex.Errors, e => e.Contains("overlapping", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsMissingCrossReferenceAndFalseQuote()
    {
        var version = Version() with { CrossReferences = [new("fictitious:99", 1, "not in source")] };
        var ex = Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([Code(version)]));
        Assert.Contains(ex.Errors, e => e.Contains("target is absent", StringComparison.Ordinal));
        Assert.Contains(ex.Errors, e => e.Contains("source paragraph", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsAClaimOfCheckedWithoutSupportingChecks()
    {
        var version = Version() with { Verification = new("checked", [], []) };
        var ex = Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([Code(version)]));
        Assert.Contains(ex.Errors, e => e.Contains("checked lacks", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsUnofficialTextSourcesButDoesNotChangeLegacyAllowlist()
    {
        var code = Code(Version());
        var publication = code.Ledger.Publications[0];
        code = code with { Ledger = code.Ledger with { Publications = [publication with { Arabic = publication.Arabic with { Url = "https://joradp.dz.example.org/fake.pdf" } }] } };
        Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([code]));
        Assert.False(OfficialLinks.IsOfficial("https://mtess.gov.dz/fixture.pdf"));
    }

    [Fact]
    public void RejectsWrongParagraphReconstructionAndInventory()
    {
        var code = Code(Version() with { Text = "invented mismatch" });
        code = code with { Report = code.Report with { Inventory = code.Report.Inventory with { Present = ["2"] } } };
        var ex = Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([code]));
        Assert.Contains(ex.Errors, e => e.Contains("reconstruct", StringComparison.Ordinal));
        Assert.Contains(ex.Errors, e => e.Contains("inventory/report", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("187 مكرر 1", "fictitious:187-bis-1")]
    [InlineData("73-4", "fictitious:73-4")]
    public void StableIdentitySupportsBisAndHyphenNumbers(string number, string expected)
    {
        Assert.Equal(expected, CorpusLoader.ArticleId("fictitious", number));
    }

    [Theory]
    [InlineData("1.002", "1002")]
    [InlineData("١.٠٠٣", "1003")]
    [InlineData("١٨٧ مكرر ١", "187 مكرر 1")]
    public void CanonicalizesPrintedNumbersWithoutFoldingArticleText(string number, string expected)
    {
        Assert.Equal(expected, CorpusLoader.CanonicalNumber(number));
    }

    [Fact]
    public void RejectsSourceFragmentsPointingAtUnknownPublications()
    {
        var version = Version();
        version = version with { Source = version.Source with { Fragments = [new("absent", 1, "", "", "evidence-1", [])] } };
        Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([Code(version)]));
    }

    [Fact]
    public void RejectsEditedTranscriptionWithAStaleImageReviewHash()
    {
        var version = Version() with
        {
            Verification = new("partly-checked", [new("discrepancy-resolved-from-image", "Invented image review", Start,
                "passed", "Fictitious evidence", new('b', 64), [new('c', 64)])], ["Fixture inventory incomplete"]),
        };
        var ex = Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([Code(version)]));
        Assert.Contains(ex.Errors, e => e.Contains("not bound", StringComparison.Ordinal));
    }

    [Fact]
    public void PartialChapterInventoryCannotBePresentedAsACompleteCode()
    {
        var code = Code(Version());
        CorpusChapter[] chapters = [new("fiction-chapter", ["Invented heading"], ["1"])];
        code = code with
        {
            Ledger = code.Ledger with { Scope = code.Ledger.Scope with { Kind = "chapter-subset", CoveredChapters = chapters } },
            Report = code.Report with { Inventory = code.Report.Inventory with { Scope = "chapter-subset" }, CoveredChapters = chapters },
        };
        Assert.Single(CorpusLoader.Validate([code]).Codes);
        var dishonest = code with { Report = code.Report with { Coverage = "complete" } };
        var ex = Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([dishonest]));
        Assert.Contains(ex.Errors, e => e.Contains("partial/complete coverage", StringComparison.Ordinal));
    }

    [Fact]
    public void MixedPublicationFragmentsNeedEveryParagraphAndCurrentParagraphHashes()
    {
        var version = Version() with { Text = "kept\nchanged", Paragraphs = ["kept", "changed"] };
        var code = Code(version);
        var newer = code.Ledger.Publications[0] with { Id = "new-publication" };
        var keptHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("kept")));
        var changedHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("changed")));
        version = version with
        {
            IntroducedBy = newer.Id,
            Source = version.Source with
            {
                PublicationId = newer.Id,
                Fragments = [new("fiction-001", 1, "", "", "kept-crop", [], 1, new('a', 64), keptHash),
                             new(newer.Id, 1, "", "", "changed-crop", [], 2, new('a', 64), changedHash)],
            },
        };
        code = Code(version) with { Ledger = code.Ledger with { Publications = [code.Ledger.Publications[0], newer] } };
        Assert.Single(CorpusLoader.Validate([code]).Codes);
        var edited = version with { Text = "edited\nchanged", Paragraphs = ["edited", "changed"] };
        var ex = Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([code with
        {
            Content = code.Content with { Articles = [code.Content.Articles[0] with { Versions = [edited] }] },
        }]));
        Assert.Contains(ex.Errors, e => e.Contains("paragraph provenance", StringComparison.Ordinal));
    }

    [Fact]
    public void CompilationWitnessKeepsItsOwnDocumentWithoutReplacingTheLegislativeSource()
    {
        var version = Version();
        var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(version.Text)));
        var witness = new CorpusTranscriptionWitness(
            new("https://droit.mjustice.gov.dz/fictitious-compilation.pdf", new('b', 64), Start),
            [3, 4], "Invented raw layer", "Invented raw OCR", ["invented-3", "invented-4"], hash,
            "Fictitious compilation; no legal applicability or transcription check inferred.");
        version = version with { Source = version.Source with { TranscriptionWitnesses = [witness] } };
        var loaded = CorpusLoader.Validate([Code(version)]).Codes[0].Content.Articles[0].Versions[0];
        Assert.Equal("fiction-001", loaded.Source.PublicationId);
        Assert.Equal(new string('b', 64), loaded.Source.TranscriptionWitnesses[0].Document.Sha256);
        Assert.Empty(loaded.Verification.Checks);
        var wrongPublication = version with { Source = version.Source with { PublicationId = "absent" } };
        Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([Code(wrongPublication)]));
    }

    [Theory]
    [InlineData("unofficial")]
    [InlineData("stale-text")]
    [InlineData("missing-ocr")]
    [InlineData("duplicate-page")]
    public void RejectsInvalidCompilationWitnesses(string defect)
    {
        var version = Version();
        var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(version.Text)));
        var witness = new CorpusTranscriptionWitness(
            new("https://droit.mjustice.gov.dz/fictitious.pdf", new('b', 64), Start),
            [3], "Invented layer", "Invented OCR", ["invented-3"], hash, "Invented fixture");
        witness = defect switch
        {
            "unofficial" => witness with { Document = witness.Document with { Url = "https://droit.mjustice.gov.dz.example.org/fake.pdf" } },
            "stale-text" => witness with { TextSha256 = new('c', 64) },
            "missing-ocr" => witness with { Ocr = "" },
            "duplicate-page" => witness with { Pages = [3, 3] },
            _ => throw new InvalidOperationException("Unknown fixture defect"),
        };
        version = version with { Source = version.Source with { TranscriptionWitnesses = [witness] } };
        Assert.Throws<CorpusValidationException>(() => CorpusLoader.Validate([Code(version)]));
    }

    private static CorpusArticleVersion Version() => new()
    {
        VersionId = "fictitious:1@original", Text = "نص وهمي للاختبار فقط.", Paragraphs = ["نص وهمي للاختبار فقط."],
        InForceFrom = Start, TemporalStatus = "resolved", IntroducedBy = "fiction-001", Status = "in-force",
        Source = new() { PublicationId = "fiction-001", Page = 1, TextLayer = "نص وهمي للاختبار فقط.", Ocr = "نص وهمي للاختبار فقط." },
        Verification = new("partly-checked", [], ["Invented fixture; no actual source."]),
    };

    private static CorpusCode Code(params CorpusArticleVersion[] versions) => new(
        new CorpusLedger
        {
            Id = "fictitious", CitationName = "قانون وهمي", Reference = "Invented for tests",
            Scope = new() { Kind = "priority-subset", TargetArticles = ["1"], AmendmentDiscoveryThrough = Cutoff },
            Publications = [new()
            {
                Id = "fiction-001", Kind = "law", Number = "fiction-1", Date = Start,
                Jo = new("fiction-1", Start, [1]), Arabic = new("https://www.joradp.dz/fictitious.pdf", new('a', 64), Start),
                ArticlesTouched = ["1"], Discovery = [new("https://www.joradp.dz/fictitious-index", Start, "Invented fixture")],
            }],
        },
        new CorpusArticlesFile
        {
            CodeId = "fictitious", Articles = [new() { Id = "fictitious:1", Number = "1", CanonicalNumber = "1", Versions = versions }],
        },
        new CorpusReport
        {
            CodeId = "fictitious", GeneratedOn = Cutoff,
            Inventory = new() { Scope = "priority-subset", Expected = ["1"], Present = ["1"], Complete = true },
            CountsByState = new Dictionary<string, int> { ["checked"] = versions.Count(v => v.Verification.State == "checked"), ["partly-checked"] = versions.Count(v => v.Verification.State == "partly-checked"), ["unsettled"] = versions.Count(v => v.Verification.State == "unsettled") },
            Unsettled = versions.Where(v => v.Verification.State != "checked" || v.TemporalStatus == "unsettled").Select(v => new CorpusUnsettledItem(v.VersionId, "Invented fixture verification incomplete")).ToList(),
        });
}
