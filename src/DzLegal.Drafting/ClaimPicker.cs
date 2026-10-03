using System.Text.RegularExpressions;

namespace DzLegal.Drafting;

/// <summary>A claim type the model may choose, by id (from the local claim catalog).</summary>
public sealed record ClaimOption(string Id, string Label);

/// <summary>
/// Asks the local model which claim types of the catalog fit the facts. The model sees a closed list and may only
/// answer with ids from it; anything else in its answer is ignored. It never names an article: the articles come
/// from the catalog entry of each chosen claim.
/// </summary>
public sealed partial class ClaimPicker(IModelProvider provider)
{
    public const int MaxPicks = 3;

    /// <summary>What the prompt tells the model to write when nothing in the list fits.</summary>
    public const string NoneMarker = "لا شيء";

    public const string RequestId = "claims";

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    /// Fills the prompt template: <c>{{ subject }}</c>, <c>{{ facts }}</c> and <c>{{ claims }}</c> (one
    /// «id — label» line per allowed claim type).
    /// </summary>
    public static ModelRequest BuildRequest(string promptTemplate, string subject, string facts, IReadOnlyList<ClaimOption> options)
    {
        ArgumentNullException.ThrowIfNull(promptTemplate);
        ArgumentNullException.ThrowIfNull(options);
        var list = string.Join('\n', options.Select(o => $"{o.Id} — {o.Label}"));
        var user = promptTemplate
            .Replace("{{ subject }}", subject?.Trim() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{{ facts }}", facts?.Trim() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{{ claims }}", list, StringComparison.Ordinal);
        return new ModelRequest(RequestId, string.Empty, TemplateRenderer.Normalize(user), facts?.Trim() ?? string.Empty);
    }

    /// <summary>Returns up to <see cref="MaxPicks"/> claim ids from the list, most fitting first; empty when none fits.</summary>
    public async Task<IReadOnlyList<string>> PickAsync(
        string promptTemplate,
        string subject,
        string facts,
        IReadOnlyList<ClaimOption> options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0 || string.IsNullOrWhiteSpace(facts))
        {
            return [];
        }

        var answer = await _provider.GenerateAsync(BuildRequest(promptTemplate, subject, facts, options), cancellationToken);
        return Parse(answer, options);
    }

    /// <summary>Keeps only exact ids from the list, in the order given, without duplicates; reasoning blocks are ignored.</summary>
    public static IReadOnlyList<string> Parse(string answer, IReadOnlyList<ClaimOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var allowed = options.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);
        var text = UnclosedThink().Replace(ThinkBlock().Replace(answer ?? string.Empty, string.Empty), string.Empty);
        return IdToken().Matches(text.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(allowed.Contains)
            .Distinct()
            .Take(MaxPicks)
            .ToList();
    }

    [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline)]
    private static partial Regex ThinkBlock();

    [GeneratedRegex(@"<think>.*", RegexOptions.Singleline)]
    private static partial Regex UnclosedThink();

    [GeneratedRegex(@"[a-z0-9]+(?:-[a-z0-9]+)*")]
    private static partial Regex IdToken();
}
