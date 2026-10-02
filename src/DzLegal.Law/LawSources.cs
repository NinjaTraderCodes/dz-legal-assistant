using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Law;

/// <summary>The manifest of official texts to import (<c>data/law/sources.json</c>).</summary>
public sealed record LawSourceManifest
{
    public const string FileName = "sources.json";

    public IReadOnlyList<LawSourceEntry> Codes { get; init; } = [];

    public static LawSourceManifest Load(string lawRoot) =>
        JsonSerializer.Deserialize<LawSourceManifest>(File.ReadAllText(Path.Combine(lawRoot, FileName)), DefinitionJson.Options)
        ?? throw new LawDataException(Path.Combine(lawRoot, FileName), ["File is empty."]);
}

public sealed record LawSourceEntry
{
    public required string Id { get; init; }

    public required LocalizedText Title { get; init; }

    public required string CitationName { get; init; }

    public required string Reference { get; init; }
}
