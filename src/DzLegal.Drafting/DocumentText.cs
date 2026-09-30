using DzLegal.Domain;

namespace DzLegal.Drafting;

/// <summary>Assembles a draft into the plain text of the document, in section order.</summary>
public static class DocumentText
{
    public static string Compose(DocumentTypeDefinition definition, DraftDocument draft)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(draft);
        var blocks = new List<string>();
        foreach (var section in draft.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Text))
            {
                continue;
            }

            var sectionDefinition = definition.FindSection(section.Id);
            blocks.Add(sectionDefinition is { ShowTitle: true }
                ? $"{sectionDefinition.Title.Ar}:\n{section.Text.Trim()}"
                : section.Text.Trim());
        }

        return string.Join("\n\n", blocks);
    }
}
