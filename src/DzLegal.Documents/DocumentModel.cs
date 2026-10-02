using DzLegal.Domain;

namespace DzLegal.Documents;

/// <summary>A draft laid out for print: an ordered list of blocks, independent of the output format.</summary>
public sealed record DocumentModel(string Title, IReadOnlyList<DocBlock> Blocks);

/// <param name="SectionId">The draft section the block comes from.</param>
/// <param name="Heading">Printed section title, when the section shows one.</param>
/// <param name="Lines">The section's lines, one paragraph each.</param>
public sealed record DocBlock(string SectionId, string? Heading, IReadOnlyList<string> Lines, SectionLayout Layout);

public static class DocumentLayoutBuilder
{
    /// <summary>Builds the printable model: non-empty sections, in draft order, with their layout and titles.</summary>
    public static DocumentModel Build(DocumentTypeDefinition definition, DraftDocument draft)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(draft);
        var blocks = new List<DocBlock>();
        foreach (var section in draft.Sections)
        {
            var lines = section.Text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(l => l.Trim())
                .ToList();
            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            if (lines.All(l => l.Length == 0))
            {
                continue;
            }

            var sectionDefinition = definition.FindSection(section.Id);
            blocks.Add(new DocBlock(
                section.Id,
                sectionDefinition is { ShowTitle: true } ? sectionDefinition.Title.Ar : null,
                lines,
                sectionDefinition?.Layout ?? new SectionLayout()));
        }

        return new DocumentModel(definition.Name.Ar, blocks);
    }
}
