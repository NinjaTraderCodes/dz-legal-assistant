using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Reader.Front;

namespace DzLegal.CoreLogic.Reader.Tests;

/// <summary>The reader's input, windows and stitching (specs/reader.md), on a tiny vocabulary fixture (no model file).</summary>
public sealed class NoteSequenceTests
{
    private static readonly NoteSequence Sequence = NoteSequence.FromVocabulary(Path.Combine(AppContext.BaseDirectory, "tiny-vocab.txt"));

    [Fact]
    public void EveryPieceKeepsItsFieldAndItsOffsetInTheTextAsTyped()
    {
        var note = new NoteInput { Facts = "أقرض المدعي مبلغ 800 دج.", Subject = "الوقائع", Documents = "" };
        var pieces = Sequence.Pieces(note);
        var facts = pieces.Where(p => p.Field == NoteField.Facts && p.Id != Sequence.SepId).ToList();
        Assert.Equal(["أقر", "ض", "المدعي", "مبلغ", "800", "دج", "."], facts.Select(p => note.Facts.Substring(p.Start, p.Length)));
        Assert.Equal(4, pieces.Count(p => p.Id == Sequence.SepId));  // each field keeps its [SEP], an empty one too
        Assert.Equal(NoteField.Subject, pieces[0].Field);
        Assert.Equal(new Piece(Sequence.SepId, NoteField.Documents, 0, 0), pieces[^1]);
    }

    [Fact]
    public void ANoteThatFitsIsOneWindowWithoutASecondClosingSep()
    {
        var pieces = Sequence.Pieces(new NoteInput { Facts = "أقرض المدعي" });
        var (windows, cut) = NoteSequence.Cut(pieces.Count);
        Assert.Equal((1, 0), (windows.Count, cut));
        var ids = Sequence.Ids(pieces, windows[0]);
        Assert.Equal(Sequence.ClsId, ids[0]);
        Assert.Equal(pieces.Count + 1, ids.Count);
    }

    [Theory]
    [InlineData(511, 1, 0)]  // [CLS] and 511 pieces fill one window
    [InlineData(512, 2, 0)]
    [InlineData(892, 2, 0)]  // 382 + 510 = 892: two windows reach the end
    [InlineData(893, 3, 0)]
    [InlineData(1274, 3, 0)]  // 764 + 510
    [InlineData(1300, 3, 26)]  // beyond three windows: cut and counted
    public void LongNotesAreCutIntoAtMostThreeOverlappingWindows(int pieces, int windows, int cutOff)
    {
        var (w, cut) = NoteSequence.Cut(pieces);
        Assert.Equal((windows, cutOff), (w.Count, cut));
        Assert.All(w, window => Assert.True(window.Count <= NoteSequence.WindowSize - 2 || w.Count == 1));
        for (var i = 1; i < w.Count; i++)
        {
            Assert.Equal(NoteSequence.Overlap, w[i - 1].First + w[i - 1].Count - w[i].First);
        }
    }

    [Fact]
    public void InAnOverlapEachPieceKeepsTheWindowWhereItLiesFartherFromTheEdge()
    {
        var (windows, _) = NoteSequence.Cut(892);
        var chosen = NoteSequence.Stitch(892, windows);
        Assert.Equal(0, chosen[0]);
        Assert.Equal(0, chosen[382 + 63]);  // in the overlap 382..509, the first half stays with the first window
        Assert.Equal(1, chosen[382 + 65]);
        Assert.Equal(1, chosen[891]);
        Assert.DoesNotContain(-1, chosen);
    }
}
