using System.Text;

namespace DzLegal.Documents.Tests;

public class LayoutAndPdfTests
{
    [Fact]
    public async Task LayoutFollowsTheDraftAndSkipsEmptySections()
    {
        var draft = await Sample.DebtCaseAsync();
        draft.FindSection("documents")!.Text = "  ";

        var model = DocumentLayoutBuilder.Build(Sample.Requete, draft);

        Assert.Equal("header", model.Blocks[0].SectionId);
        Assert.Equal(Domain.BlockAlign.Center, model.Blocks[0].Layout.Align);
        Assert.DoesNotContain(model.Blocks, b => b.SectionId == "documents");
        var facts = model.Blocks.Single(b => b.SectionId == "facts");
        Assert.Equal("الوقائع", facts.Heading);
        Assert.StartsWith("حيث إنه بتاريخ 15 جانفي 2025", facts.Lines[0], StringComparison.Ordinal);
        Assert.Equal(Domain.BlockAlign.End, model.Blocks[^1].Layout.Align);
    }

    [Fact]
    public async Task PdfIsProducedWithTheArabicFontEmbedded()
    {
        var model = DocumentLayoutBuilder.Build(Sample.Requete, await Sample.DebtCaseAsync());

        var pdf = PdfRenderer.RenderPdf(model);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4), StringComparison.Ordinal);
        Assert.Contains("NotoNaskhArabic", Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);
        Sample.Save("requete.pdf", pdf);
    }

    [Fact]
    public async Task PreviewImagesArePngPagesOfTheSameDocument()
    {
        var model = DocumentLayoutBuilder.Build(Sample.Requete, await Sample.DebtCaseAsync());

        var pages = PdfRenderer.RenderPageImages(model);

        Assert.InRange(pages.Count, 1, 3);
        Assert.All(pages, p => Assert.Equal(0x89, p[0]));
        for (var i = 0; i < pages.Count; i++)
        {
            Sample.Save($"requete-page{i + 1}.png", pages[i]);
        }
    }

    [Fact]
    public async Task WatermarkCanBeTurnedOff()
    {
        var model = DocumentLayoutBuilder.Build(Sample.Requete, await Sample.DebtCaseAsync());
        var with = PdfRenderer.RenderPdf(model);
        var without = PdfRenderer.RenderPdf(model, new DocumentRenderOptions { DraftWatermark = false });
        Assert.NotEqual(with.Length, without.Length);
    }
}

public class DocxRendererTests
{
    [Fact]
    public async Task WordDocumentHasRightToLeftParagraphsWithTheDraftText()
    {
        var model = DocumentLayoutBuilder.Build(Sample.Requete, await Sample.DebtCaseAsync());

        var bytes = DocxRenderer.Render(model);
        Sample.Save("requete.docx", bytes);

        using var stream = new MemoryStream(bytes);
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream, false);
        var errors = new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(doc).Select(e => $"{e.Path?.XPath}: {e.Description}").ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        var paragraphs = doc.MainDocumentPart!.Document!.Body!.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();
        Assert.Equal("الجمهورية الجزائرية الديمقراطية الشعبية", paragraphs[0].InnerText);
        Assert.All(paragraphs, p => Assert.NotNull(p.ParagraphProperties?.BiDi));
        Assert.Contains(paragraphs, p => p.InnerText == "الوقائع:");
        Assert.Contains(paragraphs, p => p.InnerText.StartsWith("حيث إنه بتاريخ 15 جانفي 2025", StringComparison.Ordinal));
        Assert.Equal("التوقيع:", paragraphs[^1].InnerText);
    }
}
