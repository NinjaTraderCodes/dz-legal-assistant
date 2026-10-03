using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Law;

/// <summary>The manifest of official texts to import (<c>data/law/sources.json</c>).</summary>
public sealed record LawSourceManifest
{
    public const string FileName = "sources.json";

    public IReadOnlyList<LawSourceEntry> Codes { get; init; } = [];

    public static LawSourceManifest Load(string lawRoot)
    {
        var path = Path.Combine(lawRoot, FileName);
        var manifest = JsonSerializer.Deserialize<LawSourceManifest>(File.ReadAllText(path), DefinitionJson.Options)
            ?? throw new LawDataException(path, ["File is empty."]);
        var errors = manifest.Codes
            .Where(c => !OfficialLinks.IsOfficial(c.OfficialSite))
            .Select(c => $"Code '{c.Id}': officialSite '{c.OfficialSite}' must be an https link to {string.Join(" or ", OfficialLinks.Hosts)}.")
            .ToList();
        return errors.Count > 0 ? throw new LawDataException(path, errors) : manifest;
    }
}

public sealed record LawSourceEntry
{
    public required string Id { get; init; }

    public required LocalizedText Title { get; init; }

    public required string CitationName { get; init; }

    public required string Reference { get; init; }

    /// <summary>Official site where the text can be checked before it is imported (https, official host).</summary>
    public string OfficialSite { get; init; } = string.Empty;

    /// <summary>Journal Officiel issue of the original text, e.g. «الجريدة الرسمية عدد ... لسنة ...»; filled by a lawyer.</summary>
    public string? JoReference { get; init; }
}
