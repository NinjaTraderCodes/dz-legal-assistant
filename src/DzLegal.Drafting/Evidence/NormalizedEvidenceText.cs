using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using DzLegal.Domain;

namespace DzLegal.Drafting.Evidence;

/// <summary>Search normalization with reversible offsets into the untouched field. Never used as drafting text.</summary>
public sealed record NormalizedEvidenceText(string FieldId, string Text, ImmutableArray<(int Start, int Length)> OriginalRanges)
{
    public static NormalizedEvidenceText From(string fieldId, string original)
    {
        ArgumentNullException.ThrowIfNull(original);
        var text = new StringBuilder();
        var ranges = ImmutableArray.CreateBuilder<(int, int)>();
        var elements = StringInfo.GetTextElementEnumerator(original);
        var pendingSpace = -1;
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            var start = elements.ElementIndex;
            if (element.All(char.IsWhiteSpace)) { if (text.Length > 0 && pendingSpace < 0) { pendingSpace = start; } continue; }
            var normalized = ArabicScript.Normalize(element);
            if (normalized.Length == 0) { continue; }
            if (pendingSpace >= 0) { text.Append(' '); ranges.Add((pendingSpace, start - pendingSpace)); pendingSpace = -1; }
            text.Append(normalized);
            foreach (var unused in normalized) { ranges.Add((start, element.Length)); }
        }

        return new(fieldId, text.ToString(), ranges.ToImmutable());
    }

    public EvidenceSpan Map(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, OriginalRanges.Length - start);
        var first = OriginalRanges[start];
        var last = OriginalRanges[start + length - 1];
        return new(FieldId, first.Start, last.Start + last.Length - first.Start);
    }
}
