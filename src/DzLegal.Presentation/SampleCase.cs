using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Presentation;

/// <summary>Fictitious case details shared by the desktop guide and the web demo.</summary>
public sealed record SampleCase(string Id, LocalizedText Title, LocalizedText Summary, IReadOnlyDictionary<string, string> Inputs)
{
    public static IReadOnlyList<SampleCase> LoadAll(string folder) =>
        Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.json")
                .Order(StringComparer.Ordinal)
                .Select(file => JsonSerializer.Deserialize<SampleCase>(File.ReadAllText(file), DefinitionJson.Options)!)
                .ToList()
            : [];
}
