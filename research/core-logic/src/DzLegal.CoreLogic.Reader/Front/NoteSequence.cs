using DzLegal.CoreLogic.Contracts;
using Microsoft.ML.Tokenizers;

namespace DzLegal.CoreLogic.Reader.Front;

/// <summary>
/// One piece of a note's sequence: its vocabulary id, the field it came from (the [CLS] has none), and its offset in that
/// field as the lawyer typed it (UTF-16). A field's closing [SEP] belongs to that field, with an empty span at its end.
/// </summary>
public sealed record Piece(int Id, NoteField? Field, int Start, int Length);

/// <summary>A window of the encoder: the indexes of the note's pieces it holds (its own [CLS] and closing [SEP] aside).</summary>
public sealed record Window(int First, int Count);

/// <summary>
/// The reader's input (specs/reader.md, "The input" and "Windows"): one sequence per note, <c>[CLS] subject [SEP] facts [SEP]
/// requests [SEP] documents [SEP]</c>, every piece carrying its field and offset; windows of at most 512 pieces overlapping
/// by 128, at most three; the stitching rule. The WordPiece tokenizer is built from vocab.txt with the model's
/// tokenizer_config.json settings (CAMeLBERT-mix: no lowercasing, so no accent stripping either). The .NET tokenizer is the
/// reference: training uses these ids, never Python's.
/// </summary>
public sealed class NoteSequence
{
    public const int WindowSize = 512;
    public const int Overlap = 128;
    public const int MaxWindows = 3;

    private static readonly NoteField[] Order = [NoteField.Subject, NoteField.Facts, NoteField.Requests, NoteField.Documents];
    private readonly BertTokenizer tokenizer;

    private NoteSequence(BertTokenizer tokenizer) => this.tokenizer = tokenizer;

    public static NoteSequence FromVocabulary(string vocabPath) =>
        new(BertTokenizer.Create(vocabPath, new BertOptions { LowerCaseBeforeTokenization = false, RemoveNonSpacingMarks = false }));

    public int ClsId => tokenizer.ClassificationTokenId;

    public int SepId => tokenizer.SeparatorTokenId;

    /// <summary>The note's pieces after its [CLS]: each field's pieces, then the field's [SEP].</summary>
    public IReadOnlyList<Piece> Pieces(NoteInput note)
    {
        ArgumentNullException.ThrowIfNull(note);
        var pieces = new List<Piece>();
        foreach (var field in Order)
        {
            var text = field switch
            {
                NoteField.Subject => note.Subject,
                NoteField.Facts => note.Facts,
                NoteField.Requests => note.Requests,
                _ => note.Documents,
            } ?? string.Empty;
            foreach (var token in tokenizer.EncodeToTokens(text, out _))
            {
                var (start, length) = token.Offset.GetOffsetAndLength(text.Length);
                pieces.Add(new Piece(token.Id, field, start, length));
            }

            pieces.Add(new Piece(SepId, field, text.Length, 0));
        }

        return pieces;
    }

    /// <summary>
    /// The windows over the note's pieces (its [CLS] aside): one when [CLS] and the pieces fit in 512; otherwise windows of
    /// 510 pieces (each encoded with its own [CLS] first and [SEP] last) starting every 382 pieces, at most three. Returns
    /// the windows and how many pieces were cut off after the third.
    /// </summary>
    public static (IReadOnlyList<Window> Windows, int CutOff) Cut(int pieces)
    {
        if (pieces + 1 <= WindowSize)
        {
            return ([new Window(0, pieces)], 0);
        }

        var body = WindowSize - 2;
        var step = body - Overlap;
        var windows = new List<Window>();
        for (var first = 0; first < pieces && windows.Count < MaxWindows; first += step)
        {
            windows.Add(new Window(first, Math.Min(body, pieces - first)));
            if (first + body >= pieces)
            {
                break;
            }
        }

        var covered = windows[^1].First + windows[^1].Count;
        return (windows, pieces - covered);
    }

    /// <summary>
    /// For each piece, the window whose vector it keeps: in an overlap, the window in which it lies farther from the edge.
    /// Pieces cut off after the last window keep none (-1).
    /// </summary>
    public static int[] Stitch(int pieces, IReadOnlyList<Window> windows)
    {
        var chosen = Enumerable.Repeat(-1, pieces).ToArray();
        var distance = new int[pieces];
        for (var w = 0; w < windows.Count; w++)
        {
            var window = windows[w];
            for (var i = window.First; i < window.First + window.Count && i < pieces; i++)
            {
                var d = Math.Min(i - window.First, window.First + window.Count - 1 - i);
                if (chosen[i] < 0 || d > distance[i])
                {
                    chosen[i] = w;
                    distance[i] = d;
                }
            }
        }

        return chosen;
    }

    /// <summary>A window's input ids: [CLS], its pieces, and [SEP] unless its last piece is the note's closing [SEP].</summary>
    public IReadOnlyList<int> Ids(IReadOnlyList<Piece> pieces, Window window)
    {
        ArgumentNullException.ThrowIfNull(pieces);
        ArgumentNullException.ThrowIfNull(window);
        var ids = new List<int>(window.Count + 2) { ClsId };
        ids.AddRange(pieces.Skip(window.First).Take(window.Count).Select(p => p.Id));
        if (window.First + window.Count < pieces.Count || pieces[^1].Id != SepId)
        {
            ids.Add(SepId);
        }

        return ids;
    }
}
