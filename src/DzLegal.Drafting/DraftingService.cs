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

    public Task<DraftDocument> DraftAsync(
        DocumentTypeDefinition definition,
        IReadOnlyDictionary<string, string> inputs,
        IProgress<SectionDefinition>? progress = null,
        CancellationToken cancellationToken = default) =>
        DraftAsync(definition, inputs, new DraftOptions { Progress = progress }, cancellationToken);

    public async Task<DraftDocument> DraftAsync(
        DocumentTypeDefinition definition,
        IReadOnlyDictionary<string, string> inputs,
        DraftOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var progress = options.Progress;
        var keepNarrative = options.KeepNarrative;
        var onPartial = options.OnPartial;
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inputs);

        var sections = new List<DraftSection>();
        foreach (var section in IncludedSections(definition, inputs))
        {
            progress?.Report(section);
            if (section.Kind == SectionKind.Narrative && keepNarrative?.TryGetValue(section.Id, out var kept) == true)
            {
                sections.Add(new DraftSection { Id = section.Id, Text = kept });
                continue;
            }

            sections.Add(section.Kind == SectionKind.Narrative
                ? await WriteNarrativeAsync(definition, section, inputs, options.LegalTexts, onPartial, cancellationToken)
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
                : draft.FindSection(s.Id) is { Overridden: true } manual
                    ? new DraftSection { Id = s.Id, Text = manual.Text, Overridden = true }
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
    public ModelRequest BuildRequest(DocumentTypeDefinition definition, SectionDefinition section, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText>? legalTexts = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(inputs);
        var notes = inputs.GetValueOrDefault(section.SourceField ?? string.Empty)?.Trim() ?? string.Empty;
        var system = definition.SystemPrompt is null
            ? string.Empty
            : File.ReadAllText(Path.Combine(definition.SourceDirectory, definition.SystemPrompt)).Trim();
        var user = _renderer.RenderFile(definition, section.Prompt!, inputs, new Dictionary<string, object?>
        {
            ["section_notes"] = notes,
            ["legal_texts"] = LegalText.Block(legalTexts),
        });

        // Worked examples are marked [[مثال]] … [[/مثال]] in prompt files: the model sees the content without the
        // markers, and the content is kept aside to detect output that copies it.
        var example = string.Join('\n', ExampleBlock().Matches(user).Select(m => m.Groups[1].Value.Trim()));
        var instructions = TemplateRenderer.Normalize(ExampleBlock().Replace(user, string.Empty));
        user = TemplateRenderer.Normalize(ExampleMarker().Replace(user, string.Empty));
        return new ModelRequest(section.Id, system, user, notes, example, instructions) { MaxTokens = section.MaxTokens };
    }

    /// <summary>Redrafts one narrative section from the lawyer's notes (the ↻ action on a section).</summary>
    public Task<DraftSection> DraftSectionAsync(
        DocumentTypeDefinition definition,
        string sectionId,
        IReadOnlyDictionary<string, string> inputs,
        Action<string, string>? onPartial = null,
        IReadOnlyList<LegalText>? legalTexts = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var section = definition.FindSection(sectionId) is { Kind: SectionKind.Narrative } s
            ? s
            : throw new ArgumentException($"'{sectionId}' is not a narrative section.", nameof(sectionId));
        return WriteNarrativeAsync(definition, section, inputs, legalTexts, onPartial, cancellationToken);
    }

    /// <summary>Streams the model's answer, reporting the text so far, and returns the whole answer.</summary>
    private async Task<string> CollectAsync(ModelRequest request, Action<string, string>? onPartial, CancellationToken cancellationToken)
    {
        var text = new System.Text.StringBuilder();
        await foreach (var piece in _provider.StreamAsync(request, cancellationToken))
        {
            text.Append(piece);
            onPartial?.Invoke(request.SectionId, text.ToString());
        }

        return text.ToString();
    }

    private async Task<DraftSection> WriteNarrativeAsync(
        DocumentTypeDefinition definition,
        SectionDefinition section,
        IReadOnlyDictionary<string, string> inputs,
        IReadOnlyList<LegalText>? legalTexts,
        Action<string, string>? onPartial,
        CancellationToken cancellationToken)
    {
        var request = BuildRequest(definition, section, inputs, legalTexts);
        if (request.SourceText.Length == 0)
        {
            return new DraftSection { Id = section.Id };
        }

        var notes = new List<string>();
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var output = CleanModelOutput(await CollectAsync(request, onPartial, cancellationToken), section);
            var (text, removed) = NarrativeChecks.RemoveCopiedExample(output, request.ExampleText, request.SourceText + "\n" + request.InstructionText);
            text = ApplyCorrections(definition, text);
            if (removed > 0 && !notes.Contains(DraftingNoteCodes.RemovedCopiedExample))
            {
                notes.Add(DraftingNoteCodes.RemovedCopiedExample);
            }

            var issues = NarrativeChecks.Check(text, request.SourceText, inputs, definition.GroundedTerms, legalTexts);
            if (attempt == 2 && issues.Any(i => i.IsWrongArticleContent))
            {
                // Still attributing to an article what its text does not say: those paragraphs go, the rest stays.
                var kept = string.Join('\n', text.Split('\n').Where(p =>
                    !NarrativeChecks.Check(p, string.Empty, inputs, null, legalTexts).Any(i => i.IsWrongArticleContent)));
                if (kept.Trim().Length > 0 && kept != text)
                {
                    text = kept.Trim();
                    notes.Add(DraftingNoteCodes.RemovedWrongLaw);
                    issues = NarrativeChecks.Check(text, request.SourceText, inputs, definition.GroundedTerms, legalTexts);
                }
            }

            var invented = issues.Any(i => i.IsInventedLaw);

            // A possibly invented legal qualification, an article nobody selected or a misquoted article earns one
            // more try; a second answer that still has it is kept (the rest may be good) and shown as a review hint.
            if (text.Length > 0 && !issues.Any(i => i.IsHard) && (!invented || attempt == 2))
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

    /// <summary>Applies the definition's exact corrections. Used on model-written text only, never on the lawyer's words.</summary>
    internal static string ApplyCorrections(DocumentTypeDefinition definition, string text) =>
        definition.Corrections.Aggregate(text, (t, c) => t.Replace(c.Find, c.Replace, StringComparison.Ordinal));

    private static IEnumerable<SectionDefinition> IncludedSections(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs) =>
        definition.Sections.Where(s => Condition.All(s.IncludeWhen, inputs));

    /// <summary>
    /// Removes reasoning (closed or cut-off <c>&lt;think&gt;</c> blocks, and any non-Arabic preamble before the first
    /// Arabic line), Markdown fences/emphasis and a repeated section heading.
    /// </summary>
    internal static string CleanModelOutput(string output, SectionDefinition section)
    {
        var text = ThinkBlock().Replace(output ?? string.Empty, string.Empty);
        text = UnclosedThink().Replace(text, string.Empty);
        var lines0 = text.Split('\n');
        var firstArabic = Array.FindIndex(lines0, l => ArabicScript.Ratio(l) >= 0.5 && l.Any(ArabicScript.IsArabicLetter));
        if (firstArabic > 0)
        {
            text = string.Join('\n', lines0[firstArabic..]);
        }

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

    [GeneratedRegex(@"<think>.*", RegexOptions.Singleline)]
    private static partial Regex UnclosedThink();

    [GeneratedRegex(@"^\s*```[a-zA-Z]*\s*$", RegexOptions.Multiline)]
    private static partial Regex CodeFence();
}

/// <summary>Optional behaviour of <see cref="DraftingService.DraftAsync(DocumentTypeDefinition, IReadOnlyDictionary{string, string}, DraftOptions, CancellationToken)"/>.</summary>
public sealed record DraftOptions
{
    /// <summary>Reports each section as drafting reaches it.</summary>
    public IProgress<SectionDefinition>? Progress { get; init; }

    /// <summary>
    /// Narrative sections the lawyer has edited, by id: their text is kept and the model is not called for them,
    /// so redrafting never destroys the lawyer's work.
    /// </summary>
    public IReadOnlyDictionary<string, string>? KeepNarrative { get; init; }

    /// <summary>Receives (section id, text so far) while the model streams its answer.</summary>
    public Action<string, string>? OnPartial { get; init; }

    /// <summary>Official texts of the articles the lawyer selected: the only law the model may cite or quote.</summary>
    public IReadOnlyList<LegalText>? LegalTexts { get; init; }
}
