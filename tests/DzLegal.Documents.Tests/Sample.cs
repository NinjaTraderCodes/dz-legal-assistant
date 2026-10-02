using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Harness;
using DzLegal.ModelProviders.Stub;

namespace DzLegal.Documents.Tests;

/// <summary>The bench debt case drafted in template mode: a realistic, complete requête.</summary>
internal static class Sample
{
    public static DocumentTypeDefinition Requete { get; } = DefinitionLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "data", "document-types", "requete-introductive"));

    public static async Task<DraftDocument> DebtCaseAsync()
    {
        var drafting = DraftingCaseLoader.Load(Path.Combine(AppContext.BaseDirectory, "drafting-cases", "01-debt-arabic.json"));
        var inputs = CaseInputs.CreateDefaults(Requete, new DateOnly(2026, 10, 1));
        foreach (var (k, v) in drafting.Inputs)
        {
            inputs[k] = v;
        }

        return await new DraftingService(new StubModelProvider()).DraftAsync(Requete, inputs);
    }

    /// <summary>Writes review files to $DZLEGAL_SCREENSHOTS when set.</summary>
    public static void Save(string name, byte[] bytes)
    {
        var folder = Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS");
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name), bytes);
        }
    }
}
