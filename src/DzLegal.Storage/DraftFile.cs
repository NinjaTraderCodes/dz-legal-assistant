using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Storage;

/// <summary>Saves and opens drafts as local JSON files (extension <c>.dzdraft.json</c>).</summary>
public static class DraftFile
{
    public const string Extension = ".dzdraft.json";

    public static async Task SaveAsync(DraftDocument draft, Stream destination, CancellationToken cancellationToken = default) =>
        await JsonSerializer.SerializeAsync(destination, draft, DefinitionJson.Options, cancellationToken);

    public static async Task<DraftDocument> LoadAsync(Stream source, CancellationToken cancellationToken = default) =>
        await JsonSerializer.DeserializeAsync<DraftDocument>(source, DefinitionJson.Options, cancellationToken)
        ?? throw new InvalidDataException("The draft file is empty.");

    public static async Task SaveAsync(DraftDocument draft, string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(path);
        await SaveAsync(draft, stream, cancellationToken);
    }

    public static async Task<DraftDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await LoadAsync(stream, cancellationToken);
    }
}
