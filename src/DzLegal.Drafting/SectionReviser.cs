using DzLegal.Domain;

namespace DzLegal.Drafting;

/// <summary>A proposed new version of a section, for the lawyer to accept or reject. Never applied automatically.</summary>
/// <param name="Text">The proposed section text (already cleaned and corrected).</param>
/// <param name="Issues">Deterministic checks of the proposal (numbers, invented qualifications, script…).</param>
/// <param name="IsRefusal">The request was not an edit of this section (e.g. a legal question); nothing to apply.</param>
/// <param name="IsUnusable">Even after one retry the answer failed a hard check; it must not be accepted.</param>
public sealed record RevisionProposal(string Text, IReadOnlyList<NarrativeIssue> Issues, bool IsRefusal, bool IsUnusable);

/// <summary>
/// The section assistant: revises one narrative section according to the lawyer's instruction ("more formal",
/// "shorter", "tie art. 124 to the damages"…). Scoped on purpose: it edits wording, it does not answer legal
/// questions, and every proposal goes through the same guardrails as a first draft.
/// </summary>
public sealed class SectionReviser(IModelProvider provider)
{
    /// <summary>What the revision prompt tells the model to write when the request is not an edit of the section.</summary>
    public const string RefusalMarker = "خارج نطاق التعديل";

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly TemplateRenderer _renderer = new();

    public string ProviderName => _provider.Name;

    public async Task<RevisionProposal> ReviseAsync(
        DocumentTypeDefinition definition,
        string sectionId,
        string currentText,
        string instruction,
        IReadOnlyDictionary<string, string> inputs,
        Action<string>? onPartial = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inputs);
        var request = BuildRequest(definition, sectionId, currentText, instruction, inputs);
        var section = definition.FindSection(sectionId)!;

        // Numbers or terms the lawyer typed in the instruction itself are theirs, not invented.
        var known = new Dictionary<string, string>(inputs, StringComparer.Ordinal) { ["__instruction"] = instruction };
        RevisionProposal? last = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var raw = new System.Text.StringBuilder();
            await foreach (var piece in _provider.StreamAsync(request, cancellationToken))
            {
                raw.Append(piece);
                onPartial?.Invoke(raw.ToString());
            }

            var text = DraftingService.ApplyCorrections(definition, DraftingService.CleanModelOutput(raw.ToString(), section));
            if (text.Contains(RefusalMarker, StringComparison.Ordinal))
            {
                return new RevisionProposal(string.Empty, [], IsRefusal: true, IsUnusable: false);
            }

            var issues = NarrativeChecks.Check(text, request.SourceText, known, definition.GroundedTerms);
            last = new RevisionProposal(text, issues, IsRefusal: false, IsUnusable: text.Length == 0 || issues.Any(i => i.IsHard));
            if (!last.IsUnusable)
            {
                return last;
            }
        }

        return last!;
    }

    /// <summary>Builds the revision prompt from the definition's <c>revisionPrompt</c> file.</summary>
    public ModelRequest BuildRequest(DocumentTypeDefinition definition, string sectionId, string currentText, string instruction, IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var section = definition.FindSection(sectionId) ?? throw new ArgumentException($"Unknown section '{sectionId}'.", nameof(sectionId));
        if (definition.RevisionPrompt is null)
        {
            throw new InvalidOperationException($"Document type '{definition.Id}' has no revisionPrompt.");
        }

        var notes = inputs.GetValueOrDefault(section.SourceField ?? string.Empty)?.Trim() ?? string.Empty;
        var system = definition.SystemPrompt is null
            ? string.Empty
            : File.ReadAllText(Path.Combine(definition.SourceDirectory, definition.SystemPrompt)).Trim();
        var user = _renderer.RenderFile(definition, definition.RevisionPrompt, inputs, new Dictionary<string, object?>
        {
            ["section_title"] = section.Title.Ar,
            ["section_notes"] = notes,
            ["current_text"] = currentText ?? string.Empty,
            ["instruction"] = instruction ?? string.Empty,
        });
        return new ModelRequest(sectionId, system, user, notes);
    }
}
