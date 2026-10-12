using System.Diagnostics;
using DzLegal.Drafting;
using DzLegal.ModelProviders.Hosted;

namespace DzLegal.Harness;

/// <summary>
/// `ping --models google[:model]`: which Gemma models this Google AI Studio key can call, then one tiny request to the
/// model under test, with a verdict. The key itself is never printed.
/// </summary>
public static class GoogleDiagnostics
{
    public static async Task<int> RunAsync(GoogleAiOptions options, TextWriter output, HttpMessageHandler? handler = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        output.WriteLine($"Google AI Studio check for {options.Model} (key from GOOGLE_API_KEY, not shown)");
        output.WriteLine();

        output.WriteLine("1. Gemma models this key can call");
        IReadOnlyList<string> models;
        try
        {
            models = await GoogleAiModelProvider.ListModelsAsync(options, handler, cancellationToken);
        }
        catch (ModelProviderException ex)
        {
            output.WriteLine("   " + ex.Message);
            output.WriteLine();
            output.WriteLine("Verdict: the key cannot list models. Check GOOGLE_API_KEY (an AI Studio key, aistudio.google.com).");
            return 0;
        }

        var gemma = models.Where(m => m.StartsWith("gemma", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        foreach (var model in gemma)
        {
            var mark = model == options.Model ? "  ← under test" : model.Contains("-e4b", StringComparison.Ordinal) || model.Contains("-e2b", StringComparison.Ordinal) ? "  (laptop size)" : string.Empty;
            output.WriteLine($"   {model}{mark}");
        }

        if (gemma.Count == 0)
        {
            output.WriteLine("   none");
        }

        output.WriteLine();
        output.WriteLine($"2. A tiny request to {options.Model}");
        var clock = Stopwatch.StartNew();
        string verdict;
        try
        {
            using var provider = new GoogleAiModelProvider(options with { MaxTokens = 20 }, handler);
            var answer = await provider.GenerateAsync(new ModelRequest("ping", string.Empty, "أجب بكلمة واحدة: نعم", string.Empty), cancellationToken);
            output.WriteLine($"   answered in {clock.Elapsed.TotalSeconds:0.0} s: {answer.Trim()}");
            verdict = $"available. Run: --models google:{options.Model}";
        }
        catch (ModelProviderException ex)
        {
            output.WriteLine($"   failed in {clock.Elapsed.TotalSeconds:0.0} s: {ex.Message}");
            var nearby = gemma.Where(m => m.StartsWith("gemma-4", StringComparison.Ordinal) || m.StartsWith("gemma-3", StringComparison.Ordinal)).Take(6).ToList();
            verdict = ex.IsRateLimited
                ? "rate-limited by Google right now (free-tier per-minute or per-day quota). Wait, or use --delay-seconds 5."
                : !models.Contains(options.Model)
                    ? $"{options.Model} is not served to this key. Gemma models it can use: {(nearby.Count == 0 ? "none" : string.Join(", ", nearby))}."
                    : "the model is listed but refused the request; see the error above.";
        }

        output.WriteLine();
        output.WriteLine("Verdict: " + verdict);
        return 0;
    }
}
