namespace DzLegal.Drafting;

public enum DiffKind
{
    Same,
    Added,
    Removed,
}

public sealed record DiffToken(string Text, DiffKind Kind);

/// <summary>Word-level diff (longest common subsequence), used to show a proposed revision against the current text.</summary>
public static class TextDiff
{
    public static IReadOnlyList<DiffToken> Words(string before, string after)
    {
        var a = Tokenize(before);
        var b = Tokenize(after);
        var lcs = new int[a.Length + 1, b.Length + 1];
        for (var i = a.Length - 1; i >= 0; i--)
        {
            for (var j = b.Length - 1; j >= 0; j--)
            {
                lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var tokens = new List<DiffToken>();
        int x = 0, y = 0;
        while (x < a.Length || y < b.Length)
        {
            if (x < a.Length && y < b.Length && a[x] == b[y])
            {
                tokens.Add(new DiffToken(a[x], DiffKind.Same));
                x++;
                y++;
            }
            else if (y < b.Length && (x == a.Length || lcs[x, y + 1] >= lcs[x + 1, y]))
            {
                tokens.Add(new DiffToken(b[y], DiffKind.Added));
                y++;
            }
            else
            {
                tokens.Add(new DiffToken(a[x], DiffKind.Removed));
                x++;
            }
        }

        return tokens;
    }

    private static string[] Tokenize(string text) =>
        (text ?? string.Empty).Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
}
