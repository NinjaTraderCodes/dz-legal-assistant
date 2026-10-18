using System.Text.Json;

namespace DzLegal.Presentation.Services;

/// <summary>
/// How long the model's long tasks (the analysis, a full draft) took on this computer, so the progress panel can say
/// how much is left. Measured here, per engine: a promise from another laptop would be wrong on this one.
/// </summary>
public interface ITaskTimings
{
    /// <summary>The usual duration of <paramref name="task"/> with <paramref name="engine"/>, or null before the first run.</summary>
    TimeSpan? Typical(string task, string engine);

    void Record(string task, string engine, TimeSpan duration);
}

/// <summary>
/// The median of the last <see cref="Kept"/> runs, so one cold start (the model loading into memory) or one run beside
/// another busy program does not set the estimate. Kept in memory, and in a small local file when one is given; a file
/// that cannot be read or written only loses the estimate.
/// </summary>
public sealed class TaskTimings : ITaskTimings
{
    public const int Kept = 5;

    private readonly string? _file;
    private readonly Dictionary<string, List<double>> _seconds;
    private readonly Lock _lock = new();

    public TaskTimings(string? file = null)
    {
        _file = file;
        _seconds = Load(file);
    }

    public TimeSpan? Typical(string task, string engine)
    {
        lock (_lock)
        {
            if (!_seconds.TryGetValue(Key(task, engine), out var runs) || runs.Count == 0)
            {
                return null;
            }

            var sorted = runs.Order().ToList();
            var middle = sorted.Count / 2;
            var median = sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
            return TimeSpan.FromSeconds(median);
        }
    }

    public void Record(string task, string engine, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        lock (_lock)
        {
            var key = Key(task, engine);
            if (!_seconds.TryGetValue(key, out var runs))
            {
                _seconds[key] = runs = [];
            }

            runs.Add(Math.Round(duration.TotalSeconds, 1));
            if (runs.Count > Kept)
            {
                runs.RemoveRange(0, runs.Count - Kept);
            }

            Save();
        }
    }

    private static string Key(string task, string engine) => task + "|" + engine;

    private static Dictionary<string, List<double>> Load(string? file)
    {
        try
        {
            if (file is not null && File.Exists(file))
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<string, List<double>>>(File.ReadAllText(file));
                if (loaded is not null)
                {
                    return new Dictionary<string, List<double>>(loaded.Where(x => x.Value is not null), StringComparer.Ordinal);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // A damaged file only loses the estimates.
        }

        return new Dictionary<string, List<double>>(StringComparer.Ordinal);
    }

    private void Save()
    {
        if (_file is null)
        {
            return;
        }

        try
        {
            File.WriteAllText(_file, JsonSerializer.Serialize(_seconds));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not critical: the estimate is kept for this session.
        }
    }
}
