using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>Where an imported text came from, so anyone can re-check it against the official publication.</summary>
public sealed record LawSource
{
    public required string Url { get; init; }

    /// <summary>SHA-256 of the downloaded file, to detect a changed publication on re-import.</summary>
    public string Sha256 { get; init; } = string.Empty;

    public string RetrievedOn { get; init; } = string.Empty;
}

/// <summary>One article, with its official Arabic text exactly as published (never written from memory).</summary>
public sealed record LawArticle
{
    /// <summary>Canonical number: "106", "124 مكرر", "124 مكرر 1".</summary>
    public required string Number { get; init; }

    /// <summary>Book / title / chapter headings the article sits under.</summary>
    public IReadOnlyList<string> Path { get; init; } = [];

    public required string Text { get; init; }

    public bool Repealed { get; init; }

    /// <summary>Reference of the amending text, when the consolidated source says so.</summary>
    public string? AmendedBy { get; init; }

    /// <summary>Page of the source document the text was taken from, so a reader can check it there.</summary>
    public int? Page { get; init; }

    /// <summary>
    /// The source document's letters and digits for this article, in order (see <see cref="LawTextFold"/>). The text
    /// must fold to exactly this, which proves that only spacing and punctuation were touched when it was cleaned up.
    /// </summary>
    public string? SourceExtract { get; init; }

    /// <summary>
    /// The publication this article comes from when it is not the code's own source (an article amended by a later
    /// law: «المادة 73-4» from the 1996 ordinance); <see cref="Page"/> is then a page of this document.
    /// </summary>
    public LawSource? Source { get; init; }

    /// <summary>A remark for the reviewing lawyer (e.g. an oddity of the source edition).</summary>
    public string? Note { get; init; }

    /// <summary>True once a lawyer has compared this article with the source page.</summary>
    public bool Verified { get; init; }

    public string? VerifiedBy { get; init; }

    /// <summary>yyyy-mm-dd.</summary>
    public string? VerifiedOn { get; init; }
}

/// <summary>A code or law (e.g. the Civil Code), as imported from an official source.</summary>
public sealed record LawCode
{
    public required string Id { get; init; }

    public required LocalizedText Title { get; init; }

    /// <summary>Short Arabic name used in citations: «القانون المدني», «قانون الإجراءات المدنية والإدارية».</summary>
    public required string CitationName { get; init; }

    /// <summary>Official reference, e.g. «الأمر رقم 75-58 المؤرخ في 26 سبتمبر 1975».</summary>
    public required string Reference { get; init; }

    /// <summary>Date up to which the consolidated text includes amendments (yyyy-mm-dd), when known.</summary>
    public string? ConsolidatedAsOf { get; init; }

    /// <summary>Which publication the text comes from, e.g. «الجريدة الرسمية العدد 21 المؤرخة في 23 أبريل 2008».</summary>
    public string? Edition { get; init; }

    /// <summary>True when only some articles of the code are in the library (those the claim catalog relies on).</summary>
    public bool Partial { get; init; }

    public LawSource? Source { get; init; }

    /// <summary>False until a lawyer has checked the import against the official text.</summary>
    public bool Verified { get; init; }

    /// <summary>
    /// How the text was taken from page images when the official PDF has no usable text layer (scans, or a font that
    /// extracts as other characters), so no article carries a <see cref="LawArticle.SourceExtract"/>; null for a
    /// code proved letter by letter against its PDF.
    /// </summary>
    public string? Transcription { get; init; }

    /// <summary>Known, explained irregularities (e.g. "articles 1-3 repealed by law 05-10"), shown by the import report.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    public IReadOnlyList<LawArticle> Articles { get; init; } = [];

    public LawArticle? FindArticle(string number)
    {
        var key = ArticleNumbers.Canonical(number);
        return Articles.FirstOrDefault(a => a.Number == key);
    }
}

/// <summary>All codes available offline.</summary>
public sealed class LawLibrary(IReadOnlyList<LawCode> codes)
{
    public static LawLibrary Empty { get; } = new([]);

    public IReadOnlyList<LawCode> Codes { get; } = codes;

    public LawCode? FindCode(string id) => Codes.FirstOrDefault(c => c.Id == id);

    public LawArticle? FindArticle(string codeId, string number) => FindCode(codeId)?.FindArticle(number);

    private ArticleIndex? _index;

    /// <summary>Full-text index over the in-force articles, built on first use.</summary>
    public ArticleIndex Index => _index ??= new ArticleIndex(Codes);
}
