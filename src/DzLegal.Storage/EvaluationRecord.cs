using System.Text.Json;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Storage;

/// <summary>The lawyer's judgement of a generated draft ("use it, or rewrite it?").</summary>
public enum DraftVerdict
{
    UsedAsIs,
    MinorEdits,
    MajorRewrite,
    Unusable,
}

/// <summary>
/// One data point for draft-quality evaluation (Target 2): what the model produced, what the lawyer
/// ended up with, and their verdict. Stored locally only; never transmitted.
/// </summary>
public sealed record EvaluationRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.Now;
    public required string DocumentTypeId { get; init; }
    public string? DefinitionVersion { get; init; }
    public string? GeneratedBy { get; init; }
    public required DraftDocument GeneratedDraft { get; init; }
    public required DraftDocument FinalDraft { get; init; }
    public DraftVerdict Verdict { get; init; }

    /// <summary>Lawyer's estimate of minutes saved compared with drafting by hand (may be negative).</summary>
    public int? MinutesSaved { get; init; }

    public string? Comments { get; init; }
}

public sealed class EvaluationStore
{
    private readonly string _folder;

    /// <summary>The folder is created on the first save, so a store that never saves (the web demo) writes nothing.</summary>
    public EvaluationStore(string folder) => _folder = folder;

    public async Task<string> SaveAsync(EvaluationRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, $"{record.RecordedAt:yyyyMMdd-HHmmss}-{record.Id:N}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, record, DefinitionJson.Options, cancellationToken);
        return path;
    }

    public IReadOnlyList<EvaluationRecord> LoadAll() =>
        !Directory.Exists(_folder) ? [] : Directory.GetFiles(_folder, "*.json")
            .Order(StringComparer.Ordinal)
            .Select(f => JsonSerializer.Deserialize<EvaluationRecord>(File.ReadAllText(f), DefinitionJson.Options))
            .OfType<EvaluationRecord>()
            .ToList();
}
