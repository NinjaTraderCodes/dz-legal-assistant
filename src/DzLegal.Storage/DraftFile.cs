using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Storage;

/// <summary>Saves and opens drafts as local JSON files (extension <c>.dzdraft.json</c>).</summary>
public static class DraftFile
{
    public const string Extension = ".dzdraft.json";

    public static async Task SaveAsync(DraftDocument draft, Stream destination, CancellationToken cancellationToken = default) =>
        await JsonSerializer.SerializeAsync(destination, draft, DefinitionJson.Options, cancellationToken).ConfigureAwait(false);

    public static async Task<DraftDocument> LoadAsync(Stream source, CancellationToken cancellationToken = default) =>
        await JsonSerializer.DeserializeAsync<DraftDocument>(source, DefinitionJson.Options, cancellationToken).ConfigureAwait(false)
        ?? throw new InvalidDataException("The draft file is empty.");

    public static async Task SaveAsync(DraftDocument draft, string path, CancellationToken cancellationToken = default)
    {
        var stream = File.Create(path);
        await using (stream.ConfigureAwait(false))
        {
            await SaveAsync(draft, stream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Saves synchronously, for callers on the UI thread that cannot await (autosave timer, window closing).
    /// Writes a temporary file first, so an interrupted save never corrupts the previous one.
    /// </summary>
    public static void Save(DraftDocument draft, string path)
    {
        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        {
            JsonSerializer.Serialize(stream, draft, DefinitionJson.Options);
        }

        File.Move(temp, path, overwrite: true);
    }

    public static async Task<DraftDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var stream = File.OpenRead(path);
        await using (stream.ConfigureAwait(false))
        {
            return await LoadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }
}
