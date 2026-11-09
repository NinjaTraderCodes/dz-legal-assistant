using System.Diagnostics;
using System.Text.Json;
using DzLegal.Presentation.Services;

namespace DzLegal.App.Services.LocalModels;

/// <summary>Counts each blob once; the transfer backend's aggregate is an alternative, never another blob.</summary>
internal sealed class PullProgress(SupportedModel model)
{
    private readonly Dictionary<string, long> completed = new(StringComparer.Ordinal);
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private long previousBytes;
    private double previousSeconds;
    private double speed;
    private long lastCount;
    private bool baseline;
    public bool Success { get; private set; }

    public ModelSetupProgress Parse(string line)
    {
        if (line.Length > 64 * 1024) throw new ModelSetupException(ModelSetupStage.VerificationFailed);
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        if (root.TryGetProperty("error", out _)) throw new ModelSetupException(ModelSetupStage.ConnectionFailed);
        var status = root.TryGetProperty("status", out var value) ? value.GetString() : null;
        var stage = status == "verifying sha256 digest" ? ModelSetupStage.Verifying : ModelSetupStage.Downloading;
        if (status == "success") { Success = true; return new ModelSetupProgress(ModelSetupStage.Verifying, model.Name, model.Info.Bytes, model.Info.Bytes); }
        if (root.TryGetProperty("digest", out var digestElement))
        {
            var digest = digestElement.GetString()!;
            var total = root.TryGetProperty("total", out var t) ? t.GetInt64() : 0;
            var count = root.TryGetProperty("completed", out var c) ? c.GetInt64() : 0;
            var blob = model.Blobs.FirstOrDefault(b => "sha256:" + b.Sha256 == digest);
            var expected = digest == "sha256:model" ? model.Info.Bytes : blob?.Size
                ?? throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            if (total < 0 || total > expected || count < 0 || count > expected)
                throw new ModelSetupException(ModelSetupStage.VerificationFailed);
            completed[digest] = Math.Max(completed.GetValueOrDefault(digest), count);
        }
        var bytes = completed.TryGetValue("sha256:model", out var aggregate) ? aggregate : completed.Values.Sum();
        lastCount = Math.Max(lastCount, Math.Min(model.Info.Bytes, bytes));
        var now = clock.Elapsed.TotalSeconds;
        if (!baseline)
        {
            baseline = true;
            previousBytes = lastCount;
            previousSeconds = now;
        }
        if (now - previousSeconds >= .5)
        {
            var sample = (lastCount - previousBytes) / (now - previousSeconds);
            speed = speed == 0 ? sample : .3 * sample + .7 * speed;
            previousSeconds = now;
            previousBytes = lastCount;
        }
        return new ModelSetupProgress(stage, model.Name, lastCount, model.Info.Bytes, speed);
    }
}