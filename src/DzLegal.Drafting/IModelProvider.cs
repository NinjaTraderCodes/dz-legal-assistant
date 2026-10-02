namespace DzLegal.Drafting;

/// <summary>
/// A text-generation backend. Implementations must run on the user's machine: a provider must never
/// send the request to a remote host (see docs/OFFLINE.md).
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
public sealed record ModelRequest(string SectionId, string SystemPrompt, string UserPrompt, string SourceText, string ExampleText = "", string InstructionText = "");

public sealed class ModelProviderException : Exception
{
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
