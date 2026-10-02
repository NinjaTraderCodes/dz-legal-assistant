using DzLegal.Domain;

namespace DzLegal.Law;

/// <summary>What changed between two imports of the same code (an amendment, or a corrected extraction).</summary>
public sealed record LawDiff(IReadOnlyList<string> Added, IReadOnlyList<string> Removed, IReadOnlyList<string> Changed, IReadOnlyList<string> NewlyRepealed)
{
    public bool IsEmpty => Added.Count == 0 && Removed.Count == 0 && Changed.Count == 0 && NewlyRepealed.Count == 0;

    /// <summary>Texts are compared after <see cref="ArabicScript.Normalize"/>, so layout-only differences are ignored.</summary>
    public static LawDiff Compare(LawCode before, LawCode after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var old = before.Articles.ToDictionary(a => a.Number);
        var now = after.Articles.ToDictionary(a => a.Number);
        return new LawDiff(
            now.Keys.Where(k => !old.ContainsKey(k)).ToList(),
            old.Keys.Where(k => !now.ContainsKey(k)).ToList(),
            now.Values.Where(a => old.TryGetValue(a.Number, out var o) && ArabicScript.Normalize(o.Text) != ArabicScript.Normalize(a.Text)).Select(a => a.Number).ToList(),
            now.Values.Where(a => a.Repealed && old.TryGetValue(a.Number, out var o) && !o.Repealed).Select(a => a.Number).ToList());
    }
}
