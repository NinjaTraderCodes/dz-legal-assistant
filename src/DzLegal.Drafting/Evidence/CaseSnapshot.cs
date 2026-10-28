using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DzLegal.Drafting.Evidence;

/// <summary>Offsets use UTF-16 code units in the original, unchanged field, as .NET strings do.</summary>
public sealed record EvidenceSpan(string FieldId, int Start, int Length);

public sealed record SnapshotSentence(string Id, string FieldId, int Start, string Text);

public sealed record PartyIdentity(string Id, ImmutableArray<string> FieldIds);

/// <summary>A full immutable input revision. No model context limit changes the stored case.</summary>
public sealed partial class CaseSnapshot
{
    private static readonly string[] ProceduralParties = ["plaintiff", "defendant"];
    private CaseSnapshot(string caseId, long revision, ImmutableSortedDictionary<string, string> fields)
    {
        CaseId = caseId;
        Revision = revision;
        Fields = fields;
        var sentences = ImmutableArray.CreateBuilder<SnapshotSentence>();
        foreach (var (field, text) in fields)
        {
            var start = 0;
            foreach (var end in Boundaries().Matches(text).Select(m => m.Index).Append(text.Length))
            {
                var a = start;
                var b = end;
                while (a < b && char.IsWhiteSpace(text[a])) { a++; }
                while (b > a && char.IsWhiteSpace(text[b - 1])) { b--; }
                if (b > a)
                {
                    sentences.Add(new("s" + (sentences.Count + 1).ToString(CultureInfo.InvariantCulture), field, a, text[a..b]));
                }

                start = end;
            }
        }

        Sentences = sentences.ToImmutable();
        Parties = [.. ProceduralParties.Select(id => new PartyIdentity(id, [.. fields.Keys.Where(k => k.StartsWith(id + ".", StringComparison.Ordinal))]))];
        Fingerprint = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { CaseId, Revision, Fields })));
    }

    public string CaseId { get; }
    public long Revision { get; }
    public string Fingerprint { get; }
    public ImmutableSortedDictionary<string, string> Fields { get; }
    public ImmutableArray<SnapshotSentence> Sentences { get; }
    public ImmutableArray<PartyIdentity> Parties { get; }

    public static CaseSnapshot Create(string caseId, long revision, IReadOnlyDictionary<string, string> fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentOutOfRangeException.ThrowIfNegative(revision);
        ArgumentNullException.ThrowIfNull(fields);
        return new(caseId, revision, fields.ToImmutableSortedDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty, StringComparer.Ordinal));
    }

    public string? Read(EvidenceSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);
        return Fields.TryGetValue(span.FieldId, out var text) && span.Start >= 0 && span.Length > 0
            && span.Start <= text.Length - span.Length
            && !SplitsSurrogate(text, span.Start) && !SplitsSurrogate(text, span.Start + span.Length)
            ? text.Substring(span.Start, span.Length) : null;
    }

    public ImmutableArray<string> SentenceIds(EvidenceSpan span) =>
        Read(span) is null ? [] : [.. Sentences.Where(s => s.FieldId == span.FieldId && span.Start < s.Start + s.Text.Length && span.Start + span.Length > s.Start).Select(s => s.Id)];

    private static bool SplitsSurrogate(string text, int index) => index > 0 && index < text.Length && char.IsHighSurrogate(text[index - 1]) && char.IsLowSurrogate(text[index]);

    [GeneratedRegex(@"(?<=[.؟!])(?=\s)|[\r\n]+")]
    private static partial Regex Boundaries();
}

/// <summary>Version all analysis dependencies, not only the model prompt or the facts field.</summary>
public sealed record AnalysisIdentity(string SnapshotFingerprint, string Schema, string ReaderDigest, string ReaderOptions, string CorpusVersion, string RuleVersion, string AssessmentVersion = "unassessed")
{
    public bool IsCurrent(CaseSnapshot snapshot, AnalysisIdentity current) =>
        snapshot.Fingerprint == SnapshotFingerprint && this == current;
}
