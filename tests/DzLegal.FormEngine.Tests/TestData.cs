using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.FormEngine.Tests;

internal static class TestData
{
    public static string DocumentTypesRoot => Path.Combine(AppContext.BaseDirectory, "data", "document-types");

    public static string CorpusRoot => Path.Combine(AppContext.BaseDirectory, "corpus");

    public static DocumentTypeDefinition Requete { get; } =
        DefinitionLoader.Load(Path.Combine(DocumentTypesRoot, "requete-introductive"));

    /// <summary>A tiny in-memory definition for exercising each check kind in isolation.</summary>
    public static DocumentTypeDefinition Tiny(params RequiredElement[] elements) => new()
    {
        Id = "tiny",
        Version = "1",
        Name = new LocalizedText("تجربة", "test"),
        Fields =
        [
            new FieldDefinition { Id = "a", Label = new("أ", "a") },
            new FieldDefinition { Id = "b", Label = new("ب", "b") },
            new FieldDefinition { Id = "flag", Label = new("ع", "flag"), Kind = FieldKind.Boolean },
        ],
        Sections =
        [
            new SectionDefinition { Id = "s1", Title = new("1", "1") },
            new SectionDefinition { Id = "s2", Title = new("2", "2") },
            new SectionDefinition { Id = "s3", Title = new("3", "3") },
        ],
        Elements = elements,
    };

    public static RequiredElement Element(RuleCheck check, params Condition[] appliesWhen) => new()
    {
        Id = "e",
        Article = "art. test",
        Label = new("عنصر", "element"),
        Check = check,
        AppliesWhen = appliesWhen,
        MissingMessage = new("ناقص", "missing"),
    };

    public static DraftDocument Draft(Dictionary<string, string>? inputs = null, params (string Id, string Text)[] sections) => new()
    {
        DocumentTypeId = "tiny",
        Inputs = inputs ?? [],
        Sections = sections.Select(s => new DraftSection { Id = s.Id, Text = s.Text }).ToList(),
    };
}
