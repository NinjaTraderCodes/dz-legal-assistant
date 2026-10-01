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
}

/// <param name="SectionId">The section being written.</param>
/// <param name="SystemPrompt">Shared drafting rules.</param>
/// <param name="UserPrompt">Section instructions with the lawyer's notes embedded.</param>
/// <param name="SourceText">The lawyer's raw notes for this section (the grounding material).</param>
/// <param name="ExampleText">The worked example shown in the prompt, used to detect output that copies it.</param>
public sealed record ModelRequest(string SectionId, string SystemPrompt, string UserPrompt, string SourceText, string ExampleText = "");

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
