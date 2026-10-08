using System.Globalization;
using DzLegal.Drafting;

namespace DzLegal.Harness;

/// <summary>
/// Collects the local server's timings for every model call of a bench run and summarises where the time goes:
/// loading the model, reading the prompt, writing the answer.
/// </summary>
public sealed class SpeedLog
{
    private readonly List<ModelTimings> _calls = [];

    public void Add(ModelTimings timings) => _calls.Add(timings);

    public int Count => _calls.Count;

    /// <summary>One markdown table row per kind of call (grounds.reading, grounds, requests…), plus a header.</summary>
    public string Table()
    {
        if (_calls.Count == 0)
        {
            return "No timings reported (template mode, or a server that does not report them).\n";
        }

        var ci = CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder()
            .AppendLine("| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |")
            .AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var group in _calls.GroupBy(c => c.SectionId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var reading = group.Sum(c => c.PromptReading.TotalSeconds);
            var writing = group.Sum(c => c.Writing.TotalSeconds);
            sb.AppendLine(ci,
                $"| {group.Key} | {group.Count()} | {group.Average(c => c.Load.TotalSeconds):F1} | {group.Average(c => c.PromptTokens):F0} | {(reading > 0 ? group.Sum(c => c.PromptTokens) / reading : 0):F1} | {group.Average(c => c.AnswerTokens):F0} | {(writing > 0 ? group.Sum(c => c.AnswerTokens) / writing : 0):F1} | {group.Average(c => c.FirstWord.TotalSeconds):F1} | {group.Count(c => c.PromptMayBeTruncated)} |");
        }

        return sb.ToString();
    }
}
