using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DzLegal.Domain;

namespace DzLegal.Documents;

/// <summary>
/// Renders a <see cref="DocumentModel"/> as an editable Word document: A4, right-to-left paragraphs and section,
/// Arabic set in a font that ships with Windows, so lawyers can keep working on it in Word.
/// </summary>
public static class DocxRenderer
{
    private const int TwipsPerPoint = 20;

    public static byte[] Render(DocumentModel model, DocumentRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= new DocumentRenderOptions();
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            AddStyles(main, options);
            var body = main.Document.Body!;

            foreach (var block in model.Blocks)
            {
                var first = true;
                if (block.Heading is not null)
                {
                    body.Append(Paragraph(block.Heading + ":", BlockAlign.Start, bold: true, underline: true, size: null, spaceBefore: block.Layout.SpaceBefore, keepNext: true));
                    first = false;
                }

                foreach (var line in block.Lines)
                {
                    body.Append(Paragraph(line, block.Layout.Align, block.Layout.Bold, underline: false, block.Layout.Size, first ? block.Layout.SpaceBefore : 0, keepNext: false));
                    first = false;
                }
            }

            var sectionProperties = new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 }, // A4
                new PageMargin { Top = 1247, Bottom = 1247, Left = 1247U, Right = 1247U, Header = 708U, Footer = 708U },
                new BiDi());
            if (options.DraftWatermark)
            {
                // Word has no reliable diagonal watermark without VML; a header line on every page says the same.
                var header = main.AddNewPart<HeaderPart>();
                header.Header = new Header(Paragraph(DocumentRenderOptions.DraftHeader, BlockAlign.Center, bold: true, underline: false, size: 9, spaceBefore: 0, keepNext: false));
                header.Header.Save();
                sectionProperties.PrependChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = main.GetIdOfPart(header) });
            }

            body.Append(sectionProperties);
            main.Document.Save();
            if (options.AiAssisted)
            {
                document.PackageProperties.Description = DocumentRenderOptions.AiAssistedDescription;
            }
        }

        return stream.ToArray();
    }

    private static Paragraph Paragraph(string text, BlockAlign align, bool bold, bool underline, float? size, float spaceBefore, bool keepNext)
    {
        var properties = new ParagraphProperties();
        if (keepNext)
        {
            properties.Append(new KeepNext());
        }

        properties.Append(new BiDi());
        properties.Append(new SpacingBetweenLines { Before = ((int)(spaceBefore * TwipsPerPoint)).ToString(System.Globalization.CultureInfo.InvariantCulture) });
        if (align == BlockAlign.End)
        {
            // Date and signature: a compact block on the left side of the page, lines centred within it.
            // In a right-to-left paragraph Word applies "left" indentation on the leading (right) side,
            // which pushes the block to the left half of the page.
            properties.Append(new Indentation { Left = "5670" });
        }

        properties.Append(new Justification
        {
            Val = align switch
            {
                BlockAlign.Center or BlockAlign.End => JustificationValues.Center,
                BlockAlign.Justify => JustificationValues.Both,
                _ => JustificationValues.Left, // in a right-to-left paragraph Word reads "left" as the leading (right) edge
            },
        });

        // Child order follows the Open XML schema (b, bCs, sz, szCs, u, rtl); Word rejects other orders.
        var runProperties = new RunProperties();
        if (bold)
        {
            runProperties.Append(new Bold(), new BoldComplexScript());
        }

        if (size is { } points)
        {
            var halfPoints = ((int)(points * 2)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            runProperties.Append(new FontSize { Val = halfPoints }, new FontSizeComplexScript { Val = halfPoints });
        }

        if (underline)
        {
            runProperties.Append(new Underline { Val = UnderlineValues.Single });
        }

        runProperties.Append(new RightToLeftText());

        return new Paragraph(properties, new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static void AddStyles(MainDocumentPart main, DocumentRenderOptions options)
    {
        var halfPoints = ((int)(options.FontSize * 2)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var styles = main.AddNewPart<StyleDefinitionsPart>();
        styles.Styles = new Styles(new DocDefaults(
            new RunPropertiesDefault(new RunPropertiesBaseStyle(
                new RunFonts
                {
                    Ascii = DocumentFonts.WordComplexScriptFamily,
                    HighAnsi = DocumentFonts.WordComplexScriptFamily,
                    ComplexScript = DocumentFonts.WordComplexScriptFamily,
                },
                new FontSize { Val = halfPoints },
                new FontSizeComplexScript { Val = halfPoints },
                new Languages { Bidi = "ar-DZ", Val = "fr-FR" })),
            new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
                new BiDi(),
                new SpacingBetweenLines { After = "40", Line = "340", LineRule = LineSpacingRuleValues.Auto }))));
    }
}
