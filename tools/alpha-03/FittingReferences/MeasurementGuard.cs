using System.Globalization;
using System.Text.Json;

namespace DzLegal.Alpha03.Measurement;

internal static class MeasurementGuard
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    internal static async Task<JsonDocument> LatestAsync(string runRoot, CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(Path.Combine(runRoot, "resources.jsonl"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(-Math.Min(stream.Length, 65536), SeekOrigin.End);
        using var reader = new StreamReader(stream);
        var lines = (await reader.ReadToEndAsync(cancellationToken)).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Reverse())
        {
            try { return JsonDocument.Parse(line); }
            catch (JsonException) { }
        }

        throw new IOException("MEASUREMENT PRECONDITION: no complete resource sample");
    }

    internal static async Task WaitAfterAsync(string runRoot, DateTimeOffset after)
    {
        for (var i = 0; i < 100; i++)
        {
            using var sample = await LatestAsync(runRoot);
            if (Time(sample.RootElement) >= after)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new IOException("MEASUREMENT PRECONDITION: monitor did not update after planned unload");
    }

    internal static async Task CheckAsync(string runRoot, CancellationToken cancellationToken = default)
    {
        using var sample = await LatestAsync(runRoot, cancellationToken);
        var r = sample.RootElement;
        var p = r.GetProperty("power");
        const string best = "ded574b5-45a0-4f42-8737-46345c09c238";
        var reason = File.Exists(Path.Combine(runRoot, "measurement.stop")) ? "operator stop recorded"
            : (DateTimeOffset.UtcNow - Time(r)).TotalSeconds > 10 ? "stale resource monitor"
            : p.GetProperty("acLineStatus").GetInt32() != 1 ? "mains disconnected"
            : p.GetProperty("actualOverlayGuid").GetString() != best || p.GetProperty("effectiveOverlayGuid").GetString() != best || p.GetProperty("configuredAcGuid").GetString() != best ? "Best performance not verified"
            : r.GetProperty("appWorkingSetBytes").GetInt64() <= 0 ? "idle app not resident"
            : r.GetProperty("availablePhysicalBytes").GetInt64() < 512L * 1024 * 1024 ? "machine available physical below 512 MiB"
            : r.GetProperty("modelPrivateBytes").GetInt64() + r.GetProperty("appWorkingSetBytes").GetInt64() > r.GetProperty("totalPhysicalBytes").GetInt64() ? "model private plus app working set exceeds physical RAM"
            : null;
        if (reason is not null)
        {
            await File.WriteAllTextAsync(Path.Combine(runRoot, "precondition-failure.json"), JsonSerializer.Serialize(new
            {
                Utc = DateTimeOffset.UtcNow, Reason = reason, Sample = r,
            }, Options), cancellationToken);
            throw new IOException("MEASUREMENT PRECONDITION: " + reason);
        }
    }

    private static DateTimeOffset Time(JsonElement sample) => DateTimeOffset.Parse(sample.GetProperty("utc").GetString()!, CultureInfo.InvariantCulture);
}
