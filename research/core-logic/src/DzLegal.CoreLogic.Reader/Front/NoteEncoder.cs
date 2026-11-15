using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DzLegal.CoreLogic.Contracts;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace DzLegal.CoreLogic.Reader.Front;

/// <summary>A note encoded: the stitched vectors of its own pieces, the note vector, and the pieces they belong to.</summary>
public sealed record EncodedNote(IReadOnlyList<Piece> Pieces, float[,] Tokens, float[] Note, int Windows, int CutOff);

/// <summary>
/// The 8-bit encoder run in .NET through ONNX Runtime with the .NET tokenizer's ids (specs/reader.md, "The stored encoder
/// outputs"). Each window is encoded with its own [CLS] and [SEP]; the windows' vectors are stitched into one sequence of
/// the note's own pieces; the note vector is the mean of the windows' [CLS] vectors.
/// </summary>
public sealed class NoteEncoder : IDisposable
{
    private readonly InferenceSession session;
    private readonly NoteSequence sequence;

    public NoteEncoder(string modelPath, NoteSequence sequence)
    {
        var options = new SessionOptions { IntraOpNumThreads = 4, GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
        session = new InferenceSession(modelPath, options);
        this.sequence = sequence;
        ModelSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(modelPath))).ToLowerInvariant();
    }

    public string ModelSha256 { get; }

    public EncodedNote Encode(NoteInput note)
    {
        var pieces = sequence.Pieces(note);
        var (windows, cutOff) = NoteSequence.Cut(pieces.Count);
        var chosen = NoteSequence.Stitch(pieces.Count, windows);
        var outputs = windows.Select(window => Run(sequence.Ids(pieces, window))).ToList();
        var hidden = outputs[0].GetLength(1);
        var kept = pieces.Count - cutOff;
        var tokens = new float[kept, hidden];
        for (var i = 0; i < kept; i++)
        {
            var w = chosen[i];
            var row = 1 + i - windows[w].First;  // the window's own [CLS] is row 0
            for (var h = 0; h < hidden; h++)
            {
                tokens[i, h] = outputs[w][row, h];
            }
        }

        var noteVector = new float[hidden];
        foreach (var output in outputs)
        {
            for (var h = 0; h < hidden; h++)
            {
                noteVector[h] += output[0, h] / outputs.Count;
            }
        }

        return new EncodedNote([.. pieces.Take(kept)], tokens, noteVector, windows.Count, cutOff);
    }

    private float[,] Run(IReadOnlyList<int> ids)
    {
        var n = ids.Count;
        var shape = new[] { 1, n };
        var input = new DenseTensor<long>(ids.Select(id => (long)id).ToArray(), shape);
        var mask = new DenseTensor<long>(Enumerable.Repeat(1L, n).ToArray(), shape);
        var types = new DenseTensor<long>(new long[n], shape);
        using var results = session.Run(
        [
            NamedOnnxValue.CreateFromTensor("input_ids", input),
            NamedOnnxValue.CreateFromTensor("attention_mask", mask),
            NamedOnnxValue.CreateFromTensor("token_type_ids", types),
        ]);
        var tensor = results.First(r => r.Name == "last_hidden_state").AsTensor<float>();
        var hidden = tensor.Dimensions[2];
        var matrix = new float[n, hidden];
        for (var t = 0; t < n; t++)
        {
            for (var h = 0; h < hidden; h++)
            {
                matrix[t, h] = tensor[0, t, h];
            }
        }

        return matrix;
    }

    public void Dispose() => session.Dispose();
}

/// <summary>
/// The cache of stored encoder outputs: <c>&lt;cache&gt;\&lt;first 12 hex digits of the encoder's SHA-256&gt;\&lt;note id&gt;.npz</c>
/// (two arrays of 16-bit floats: tokens, T × hidden; note, hidden) and <c>&lt;note id&gt;.json</c> (the piece ids, each
/// piece's field and offsets).
/// </summary>
public static class EncoderCache
{
    public static string Folder(string cache, string modelSha256) => Path.Combine(cache, modelSha256[..12]);

    public static void Write(string folder, string noteId, EncodedNote encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        Directory.CreateDirectory(folder);
        using (var zip = ZipFile.Open(Path.Combine(folder, noteId + ".npz"), ZipArchiveMode.Create))
        {
            WriteArray(zip, "tokens.npy", encoded.Tokens);
            var note = new float[1, encoded.Note.Length];
            for (var h = 0; h < encoded.Note.Length; h++)
            {
                note[0, h] = encoded.Note[h];
            }

            WriteArray(zip, "note.npy", note, vector: true);
        }

        var json = JsonSerializer.Serialize(new
        {
            id = noteId,
            windows = encoded.Windows,
            cutOff = encoded.CutOff,
            pieces = encoded.Pieces.Select(p => new { p.Id, field = p.Field?.ToString().ToLowerInvariant(), p.Start, p.Length }),
        });
        File.WriteAllText(Path.Combine(folder, noteId + ".json"), json);
    }

    /// <summary>One .npy array of 16-bit floats, little-endian, C order (the format NumPy reads).</summary>
    private static void WriteArray(ZipArchive zip, string name, float[,] values, bool vector = false)
    {
        var rows = values.GetLength(0);
        var cols = values.GetLength(1);
        var shape = vector ? $"({cols},)" : $"({rows}, {cols})";
        var header = $"{{'descr': '<f2', 'fortran_order': False, 'shape': {shape}, }}";
        var total = 10 + header.Length + 1;
        header = header.PadRight(header.Length + (64 - total % 64) % 64) + "\n";
        using var stream = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
        using var writer = new BinaryWriter(stream, Encoding.ASCII);
        writer.Write((byte)0x93);
        writer.Write(Encoding.ASCII.GetBytes("NUMPY"));
        writer.Write((byte)1);
        writer.Write((byte)0);
        writer.Write((ushort)header.Length);
        writer.Write(Encoding.ASCII.GetBytes(header));
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                writer.Write((Half)values[r, c]);
            }
        }
    }
}
