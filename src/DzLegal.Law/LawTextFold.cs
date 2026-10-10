using System.Text;

namespace DzLegal.Law;

/// <summary>
/// The letters and digits of an Arabic legal text, in order: no spaces, punctuation, tatweel or diacritics, and the
/// letters آ and ك folded together (the official PDFs encode ك as آ). Two texts with the same fold differ only in
/// layout. Must match <c>fold</c> in tools/law/build_codes.py.
/// </summary>
public static class LawTextFold
{
    public static string Fold(string? text)
    {
        var builder = new StringBuilder();
        foreach (var c in (text ?? string.Empty).Normalize(NormalizationForm.FormKC))
        {
            var letter = c == 'آ' ? 'ك' : c;
            if ((letter is >= '\u0621' and <= '\u064A' && letter != '\u0640') || char.IsDigit(letter))
            {
                builder.Append(letter);
            }
        }

        return builder.ToString();
    }
}
