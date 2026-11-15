using System.IO.Compression;
using System.Text;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Reader.Front;

namespace DzLegal.CoreLogic.Reader.Tests;

/// <summary>The encoder runner and its cache, on the tiny fixture encoder (random weights; shapes and plumbing only).</summary>
public sealed class NoteEncoderTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "encoder-tests-" + Guid.NewGuid().ToString("N"));
    private readonly NoteEncoder encoder = new(Path.Combine(AppContext.BaseDirectory, "tiny-encoder.onnx"),
        NoteSequence.FromVocabulary(Path.Combine(AppContext.BaseDirectory, "tiny-vocab.txt")));

    public void Dispose()
    {
        encoder.Dispose();
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void AShortNoteIsOneWindowAndEachOfItsPiecesHasAVector()
    {
        var encoded = encoder.Encode(new NoteInput { Facts = "أقرض المدعي مبلغ 800 دج." });
        Assert.Equal((1, 0), (encoded.Windows, encoded.CutOff));
        Assert.Equal(encoded.Pieces.Count, encoded.Tokens.GetLength(0));
        Assert.Equal(8, encoded.Tokens.GetLength(1));
        Assert.Equal(8, encoded.Note.Length);
    }

    [Fact]
    public void ALongNoteIsEncodedInWindowsAndStitchedIntoOneSequence()
    {
        var facts = string.Join(" ", Enumerable.Repeat("المدعي مبلغ", 300));  // 600 pieces and more
        var encoded = encoder.Encode(new NoteInput { Facts = facts });
        Assert.Equal(2, encoded.Windows);
        Assert.Equal(encoded.Pieces.Count, encoded.Tokens.GetLength(0));
        Assert.True(encoded.Pieces.Count > 512);
    }

    [Fact]
    public void TheCacheWritesTwoSixteenBitArraysNumPyCanReadAndThePieces()
    {
        var encoded = encoder.Encode(new NoteInput { Facts = "أقرض المدعي مبلغ 800 دج." });
        var dir = EncoderCache.Folder(folder, encoder.ModelSha256);
        EncoderCache.Write(dir, "n-000001", encoded);
        using var zip = ZipFile.OpenRead(Path.Combine(dir, "n-000001.npz"));
        Assert.Equal(["note.npy", "tokens.npy"], zip.Entries.Select(e => e.Name).Order(StringComparer.Ordinal));
        using var stream = zip.GetEntry("tokens.npy")!.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        Assert.Equal(0x93, bytes[0]);
        var headerLength = BitConverter.ToUInt16(bytes, 8);
        var header = Encoding.ASCII.GetString(bytes, 10, headerLength);
        Assert.Contains("'descr': '<f2'", header, StringComparison.Ordinal);
        Assert.Contains($"'shape': ({encoded.Pieces.Count}, 8)", header, StringComparison.Ordinal);
        Assert.Equal(0, (10 + headerLength) % 64);
        Assert.Equal(10 + headerLength + encoded.Pieces.Count * 8 * 2, bytes.Length);
        Assert.Contains("\"field\":\"facts\"", File.ReadAllText(Path.Combine(dir, "n-000001.json")), StringComparison.Ordinal);
    }
}
