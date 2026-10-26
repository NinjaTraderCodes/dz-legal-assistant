namespace DzLegal.Drafting;

/// <summary>
/// A text-generation backend. The desktop app only uses providers that run on the user's machine and never send the
/// request to a remote host (see docs/OFFLINE.md). Hosted providers exist only for the public web demo, in a separate
/// project the desktop app does not reference (docs/WEB-DEMO.md).
/// </summary>
public interface IModelProvider
{
    /// <summary>Short description shown in the UI and saved in evaluation records.</summary>
    string Name { get; }

    Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Streams the answer as it is written (text pieces to append). Providers that cannot stream yield the whole
    /// answer once.
    /// </summary>
    async IAsyncEnumerable<string> StreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return await GenerateAsync(request, cancellationToken);
    }
}

/// <param name="SectionId">The section being written.</param>
/// <param name="SystemPrompt">Shared drafting rules.</param>
/// <param name="UserPrompt">Section instructions with the lawyer's notes embedded.</param>
/// <param name="SourceText">The lawyer's raw notes for this section (the grounding material).</param>
/// <param name="ExampleText">The worked example shown in the prompt, used to detect output that copies it.</param>
/// <param name="InstructionText">The prompt without its example: wording found there (e.g. «في الشكل: قبول الدعوى شكلا»)
/// is mandated, so it never counts as copying the example.</param>
public sealed record ModelRequest(string SectionId, string SystemPrompt, string UserPrompt, string SourceText, string ExampleText = "", string InstructionText = "")
{
    /// <summary>
    /// Upper bound on the answer's length for this call (tokens). Short answers (the analysis) get a small cap, so a slow
    /// local model cannot ramble; null uses the provider's default.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Turns sent before <see cref="UserPrompt"/>, oldest first: a follow-up call (the analysis's choice of articles)
    /// continues the conversation of the call before it, so a local server reuses everything already read instead of
    /// reading the case again.
    /// </summary>
    public IReadOnlyList<ModelTurn> Earlier { get; init; } = [];

    /// <summary>
    /// The user and assistant turns to send, ending with <see cref="UserPrompt"/>. With <paramref name="mergeSystem"/>,
    /// for models without a system role, the system rules open the first user turn.
    /// </summary>
    public IReadOnlyList<ModelTurn> Turns(bool mergeSystem)
    {
        List<ModelTurn> turns = [.. Earlier, new ModelTurn(ModelTurn.User, UserPrompt)];
        if (mergeSystem && SystemPrompt.Length > 0)
        {
            turns[0] = turns[0] with { Text = SystemPrompt + "\n\n" + turns[0].Text };
        }

        return turns;
    }
}

/// <summary>One turn of a conversation with the model: <see cref="User"/> or <see cref="Assistant"/>.</summary>
public sealed record ModelTurn(string Role, string Text)
{
    public const string User = "user";
    public const string Assistant = "assistant";
}

/// <summary>
/// How long one model call took, as reported by the local server: loading the model, reading the prompt, writing the
/// answer. Used by the benches to see where the time goes.
/// </summary>
public sealed record ModelTimings(
    string SectionId,
    TimeSpan Load,
    int PromptTokens,
    TimeSpan PromptReading,
    int AnswerTokens,
    TimeSpan Writing,
    TimeSpan FirstWord,
    bool PromptMayBeTruncated)
{
    public double PromptTokensPerSecond => PromptReading.TotalSeconds > 0 ? PromptTokens / PromptReading.TotalSeconds : 0;

    public double AnswerTokensPerSecond => Writing.TotalSeconds > 0 ? AnswerTokens / Writing.TotalSeconds : 0;
}

public sealed class ModelProviderException : Exception
{
    /// <summary>
    /// The engine is busy or briefly unavailable (rate limit, overload, timeout): the same request may succeed later or
    /// on another engine. False for errors a retry cannot fix (bad key, unknown model, refused request).
    /// </summary>
    public bool IsTransient { get; init; }

    /// <summary>The service refused because of too many requests (429) or overload (503): waiting a little usually helps.</summary>
    public bool IsRateLimited { get; init; }

    /// <summary>The answer reached the length limit and was cut off: what came before may be usable, but not as a whole.</summary>
    public bool IsCutOff { get; init; }

    public ModelProviderException()
    {
    }

    public ModelProviderException(string message)
        : base(message)
    {
    }

    public ModelProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
