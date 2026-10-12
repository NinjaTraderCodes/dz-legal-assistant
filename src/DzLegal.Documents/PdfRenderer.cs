using DzLegal.Domain;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DzLegal.Documents;

public sealed record DocumentRenderOptions
{
    /// <summary>Diagonal «مسودة» across each page: on by default, the output is a draft until the lawyer signs.</summary>
    public bool DraftWatermark { get; init; } = true;

    /// <summary>
    /// Part of the text was written by an AI model. Recorded in the file's properties (Word: description), so the
    /// document says so wherever it travels.
    /// </summary>
    public bool AiAssisted { get; init; }

    /// <summary>The draft line Word documents carry in their page header while <see cref="DraftWatermark"/> is on.</summary>
    public const string DraftHeader = "مسودة — نص أُعدّ بمساعدة الذكاء الاصطناعي، يراجعه المحامي قبل التوقيع";

    public const string AiAssistedDescription = "AI-assisted draft: part of the text was written by an AI model and must be reviewed by the lawyer.";

    public float FontSize { get; init; } = 13;
}

/// <summary>Renders a <see cref="DocumentModel"/> as an A4, right-to-left PDF (and page images for the preview).</summary>
public static class PdfRenderer
{
    private static readonly Lazy<bool> Initialized = new(() =>
    {
        // Community license: free while annual revenue is under USD 1M (see README).
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = false;
        foreach (var font in DocumentFonts.OpenAll())
        {
            using (font)
            {
                FontManager.RegisterFontFromStream(font);
            }
        }

        return true;
    });

    public static byte[] RenderPdf(DocumentModel model, DocumentRenderOptions? options = null) =>
        Create(model, options ?? new DocumentRenderOptions()).GeneratePdf();

    /// <summary>One PNG per page, for an exact on-screen preview of the PDF.</summary>
    public static IReadOnlyList<byte[]> RenderPageImages(DocumentModel model, DocumentRenderOptions? options = null, int dpi = 96) =>
        Create(model, options ?? new DocumentRenderOptions())
            .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = dpi })
            .ToList();

    private static Document Create(DocumentModel model, DocumentRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(model);
        _ = Initialized.Value;
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2.2f, Unit.Centimetre);
            page.ContentFromRightToLeft();
            page.PageColor(Colors.White);
            page.DefaultTextStyle(t => t.FontFamily(DocumentFonts.Family).FontSize(options.FontSize).LineHeight(1.45f));

            if (options.DraftWatermark)
            {
                page.Foreground().AlignCenter().AlignMiddle().Rotate(-40)
                    .Text("مسودة").FontSize(110).FontColor("#14000000").Bold();
            }

            page.Content().Column(column =>
            {
                foreach (var block in model.Blocks)
                {
                    // Keep a heading with the start of its section (no orphaned «قائمة المرفقات:» at a page end).
                    var item = column.Item().PaddingTop(block.Layout.SpaceBefore).EnsureSpace(72);

                    // End-aligned blocks (date, signature) form a compact block on the left, lines centred in it.
                    if (block.Layout.Align == BlockAlign.End)
                    {
                        item = item.AlignLeft().Width(7, Unit.Centimetre);
                    }

                    item.Column(section =>
                    {
                        if (block.Heading is not null)
                        {
                            section.Item().PaddingBottom(2).Text(block.Heading + ":").Bold().Underline();
                        }

                        foreach (var line in block.Lines)
                        {
                            section.Item().Element(e => Paragraph(e, line, block.Layout, options));
                        }
                    });
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Darken1));
                t.Span("الصفحة ");
                t.CurrentPageNumber();
                t.Span(" من ");
                t.TotalPages();
            });
        }));
    }

    private static void Paragraph(IContainer container, string line, SectionLayout layout, DocumentRenderOptions options)
    {
        if (line.Length == 0)
        {
            container.Height(options.FontSize * 0.6f);
            return;
        }

        var text = container.Text(line).FontSize(layout.Size ?? options.FontSize);
        if (layout.Bold)
        {
            text.Bold();
        }

        // With ContentFromRightToLeft, "start" is the right-hand side.
        _ = layout.Align switch
        {
            BlockAlign.Center => text.AlignCenter(),
            BlockAlign.End => text.AlignCenter(), // centred inside the end-side block
            BlockAlign.Justify => text.Justify(),
            _ => text.AlignStart(),
        };
    }
}
