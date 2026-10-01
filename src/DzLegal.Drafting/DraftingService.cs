using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Drafting;

/// <summary>
/// Builds a draft from the case inputs. Generated sections are rendered deterministically from
/// templates; narrative sections are written by the model from the lawyer's own notes. A narrative
/// section with no notes stays empty so the Form Engine flags it: the model is never asked to invent one.
/// Paragraphs copied from the prompt's worked example are removed. Output that is then empty or fails a hard
/// <see cref="NarrativeChecks"/> check is requested once more, and if it is still unusable the lawyer's notes
/// are used unchanged, so corrupted text never reaches the draft.
/// </summary>
public sealed partial class DraftingService
{
    private readonly IModelProvider _provider;
    private readonly TemplateRenderer _renderer = new();

    public DraftingService(IModelProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public string ProviderName => _provider.Name;

    public async Task<DraftDocument> DraftAsync(
        DocumentTypeDefinition definition,
        IReadOnlyDictionary<string, string> inputs,
        IProgress<SectionDefinition>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inputs);

        var sections = new List<DraftSection>();
        foreach (var section in IncludedSections(definition, inputs))
        {
            progress?.Report(section);
            sections.Add(section.Kind == SectionKind.Narrative
                ? await WriteNarrativeAsync(definition, section, inputs, cancellationToken)
                : new DraftSection { Id = section.Id, Text = RenderGenerated(definition, section, inputs) });
        }

        return new DraftDocument
        {
            DocumentTypeId = definition.Id,
            DefinitionVersion = definition.Version,
            Inputs = new Dictionary<string, string>(inputs, StringComparer.Ordinal),
            Sections = sections,
            GeneratedBy = _provider.Name,
        };
    }

    /// <summary>
    /// Applies new inputs to an existing draft without calling the model: generated sections are
    /// re-rendered, conditional sections added or removed, and the lawyer's narrative text kept as is.
    /// </summary>
    public DraftDocument Refresh(DocumentTypeDefinition definition, DraftDocument draft, IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(inputs);

        var sections = IncludedSections(definition, inputs)
            .Select(s => s.Kind == SectionKind.Narrative
                ? new DraftSection { Id = s.Id, Text = draft.FindSection(s.Id)?.Text ?? string.Empty, DraftingNotes = [.. draft.FindSection(s.Id)?.DraftingNotes ?? []] }
                : new DraftSection { Id = s.Id, Text = RenderGenerated(definition, s, inputs) })
            .ToList();

        return draft with { Inputs = new Dictionary<string, string>(inputs, StringComparer.Ordinal), Sections = sections };
    }

    public string RenderGenerated(DocumentTypeDefinition definition, SectionDefinition section, IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(section);
        return section.Template is null ? string.Empty : _renderer.RenderFile(definition, section.Template, inputs);
    }

    /// <summary>Builds the prompt pair for a narrative section. Exposed so prompts can be inspected and tested.</summary>
    public ModelRequest BuildRequest(DocumentTypeDefinition definition, SectionDefinition section, IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(inputs);
        var notes = inputs.GetValueOrDefault(section.SourceField ?? string.Empty)?.Trim() ?? string.Empty;
        var system = definition.SystemPrompt is null
            ? string.Empty
            : File.ReadAllText(Path.Combine(definition.SourceDirectory, definition.SystemPrompt)).Trim();
        var user = _renderer.RenderFile(definition, section.Prompt!, inputs, new Dictionary<string, object?> { ["section_notes"] = notes });

        // Worked examples are marked [[مثال]] … [[/مثال]] in prompt files: the model sees the content without the
        // markers, and the content is kept aside to detect output that copies it.
        var example = string.Join('\n', ExampleBlock().Matches(user).Select(m => m.Groups[1].Value.Trim()));
        user = TemplateRenderer.Normalize(ExampleMarker().Replace(user, string.Empty));
        return new ModelRequest(section.Id, system, user, notes, example);
    }

    private async Task<DraftSection> WriteNarrativeAsync(DocumentTypeDefinition definition, SectionDefinition section, IReadOnlyDictionary<string, string> inputs, CancellationToken cancellationToken)
    {
        var request = BuildRequest(definition, section, inputs);
        if (request.SourceText.Length == 0)
        {
            return new DraftSection { Id = section.Id };
        }

        var notes = new List<string>();
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var output = CleanModelOutput(await _provider.GenerateAsync(request, cancellationToken), section);
            var (text, removed) = NarrativeChecks.RemoveCopiedExample(output, request.ExampleText, request.SourceText);
            if (removed > 0 && !notes.Contains(DraftingNoteCodes.RemovedCopiedExample))
            {
                notes.Add(DraftingNoteCodes.RemovedCopiedExample);
            }

            if (text.Length > 0 && !NarrativeChecks.Check(text, request.SourceText, inputs).Any(i => i.IsHard))
            {
                return new DraftSection { Id = section.Id, Text = text, DraftingNotes = notes };
            }

            if (attempt == 1)
            {
                notes.Add(DraftingNoteCodes.Retried);
            }
        }

        notes.Add(DraftingNoteCodes.FellBackToNotes);
        return new DraftSection { Id = section.Id, Text = TemplateRenderer.Normalize(request.SourceText), DraftingNotes = notes };
    }

    private static IEnumerable<SectionDefinition> IncludedSections(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs) =>
        definition.Sections.Where(s => Condition.All(s.IncludeWhen, inputs));

    /// <summary>Removes reasoning blocks, Markdown fences/emphasis and a repeated section heading.</summary>
    internal static string CleanModelOutput(string output, SectionDefinition section)
    {
        var text = ThinkBlock().Replace(output ?? string.Empty, string.Empty);
        text = CodeFence().Replace(text, string.Empty);
        text = text.Replace("**", string.Empty, StringComparison.Ordinal);
        text = TemplateRenderer.Normalize(text);

        var lines = text.Split('\n').ToList();
        if (lines.Count > 0)
        {
            var first = lines[0].Trim().TrimStart('#').Trim().TrimEnd(':', '：').Trim();
            if (string.Equals(first, section.Title.Ar, StringComparison.Ordinal))
            {
                lines.RemoveAt(0);
            }
        }

        return TemplateRenderer.Normalize(string.Join('\n', lines));
    }

    [GeneratedRegex(@"\[\[مثال\]\](.*?)\[\[/مثال\]\]", RegexOptions.Singleline)]
    private static partial Regex ExampleBlock();

    [GeneratedRegex(@"\[\[/?مثال\]\]")]
    private static partial Regex ExampleMarker();

    [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline)]
    private static partial Regex ThinkBlock();

    [GeneratedRegex(@"^\s*```[a-zA-Z]*\s*$", RegexOptions.Multiline)]
    private static partial Regex CodeFence();
}
